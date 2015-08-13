Imports System.Speech.Recognition
Imports System.Threading.Thread
Imports System.Windows.Controls
Imports System.Windows.Forms
Imports System.ComponentModel
Imports System.Drawing
Imports System.Windows.Media
Imports System.Windows.Shapes

Imports Hearthstone_Deck_Tracker
Imports Hearthstone_Deck_Tracker.API
Imports Hearthstone_Deck_Tracker.Enums
Imports Hearthstone_Deck_Tracker.Hearthstone
Imports Hearthstone_Deck_Tracker.Hearthstone.Entities
Public Class hsVoicePlugin
    ' Windows API Declarations
    Public Declare Function FindWindow Lib "user32" Alias "FindWindowA" (ByVal lpClassName As String, ByVal lpWindowName As String) As IntPtr
    Public Declare Function GetWindowRect Lib "user32" Alias "GetWindowRect" (ByVal hwnd As IntPtr, ByRef lpRect As RECT) As Integer
    Public Declare Function GetForegroundWindow Lib "user32" () As System.IntPtr
    Public Declare Auto Function GetWindowText Lib "user32" (ByVal hWnd As System.IntPtr, ByVal lpString As System.Text.StringBuilder, ByVal cch As Integer) As Integer
    Public Declare Sub mouse_event Lib "user32" (ByVal dwFlags As Integer, ByVal dx As Integer, ByVal dy As Integer, ByVal cButtons As Integer, ByVal dwExtraInfo As IntPtr)
    Declare Function GetAsyncKeyState Lib "user32" (ByVal vkey As Integer) As Short

    Const MOUSE_LEFTDOWN As UInteger = &H2
    Const MOUSE_LEFTUP As UInteger = &H4
    Const MOUSE_RIGHTDOWN As UInteger = &H8
    Const MOUSE_RIGHTUP As UInteger = &H10
    Structure RECT
        Public Left As Integer
        Public Top As Integer
        Public Right As Integer
        Public Bottom As Integer
    End Structure
    ' Speech recognition objects
    Public WithEvents hsRecog As SpeechRecognitionEngine
    Public WithEvents hotkeyWorker As New BackgroundWorker

    Public voiceLog As IO.StreamWriter
    Public lastCommand As New String("none")

    Public WithEvents timerReset As New Timer

    Public WithEvents grammarReloader As New BackgroundWorker

    'Overlay elements
    Public overlayCanvas As Canvas = Overlay.OverlayCanvas 'the main overlay object
    Public hdtStatus As HearthstoneTextBlock 'status text

    Public sreListen As Boolean ' Should we be listening?

    Public handCards, boardOpposing, boardFriendly As New List(Of Entity)
    Public playerID As Integer = 0
    Public opponentID As Integer = 0
    Public mulliganDone As Boolean
    Public actionInProgress As Boolean
    'Properties
    Private ReadOnly Property Entities As Entity()
        Get
            ' Clone entities from game and return as array
            Return Helper.DeepClone(Game.Entities).Values.ToArray
        End Get
    End Property ' The list of entities for the current game
    Private ReadOnly Property PlayerEntity As Entity
        Get
            ' Return the Entity representing the player
            Try
                Return Entities.First(Function(x) x.IsPlayer())
            Catch ex As Exception
                Return Nothing
            End Try
        End Get
    End Property ' The player's entity
    Private ReadOnly Property OpponentEntity As Entity
        Get
            ' Return the Entity representing the player
            Try
                Return Entities.First(Function(x) x.IsOpponent())
            Catch ex As Exception
                Return Nothing
            End Try
        End Get
    End Property ' The opponent entity

    'Main functions
    Public Sub Load()
        'Start loading HDT-Voice

        'Write basic system information to logfile
        writeLog("HDT-Voice running on " & My.Computer.Info.OSFullName & " " & My.Computer.Screen.WorkingArea.Width & "x" & My.Computer.Screen.WorkingArea.Height)
        writeLog("Initializing speech recognition object")

        'Attempt to initialize speech recognition
        Try

            Dim rec As IReadOnlyCollection(Of RecognizerInfo) = SpeechRecognitionEngine.InstalledRecognizers
            If rec.Count > 0 Then
                writeLog("Installed recognizers: " & rec.Count)
                For Each e In rec
                    writeLog(e.Culture.Name)
                Next
                writeLog("Initializing recognizer for: " & rec.First.Culture.Name)
                hsRecog = New SpeechRecognitionEngine(New Globalization.CultureInfo(rec.First.Culture.Name))
            Else
                writeLog("No speech recognizer found. Attempting to start engine anyway.")
                hsRecog = New SpeechRecognitionEngine()
            End If

            hsRecog.SetInputToDefaultAudioDevice()
            hsRecog.BabbleTimeout = New TimeSpan(0, 0, 3)
            hsRecog.InitialSilenceTimeout = New TimeSpan(0, 0, 3)
            writeLog("Successfuly started speech recognition")
        Catch ex As Exception
            writeLog("Error initializing speech recognition: " & ex.Message)
            MsgBox("An error occurred initializing speech recognition: " & vbNewLine & ex.Message, vbOKOnly + vbCritical, "HDT-Voice")
        End Try


        ' Initialize output displays
        hdtStatus = New HearthstoneTextBlock
        hdtStatus.Text = "HDT-Voice: Loading, please wait..."
        hdtStatus.FontSize = 16
        Canvas.SetTop(hdtStatus, 22)
        Canvas.SetLeft(hdtStatus, 4)
        overlayCanvas.Children.Add(hdtStatus)

        writeLog("Attaching event handlers...")
        'Add event handlers
        GameEvents.OnGameStart.Add(New Action(AddressOf onNewGame))
        GameEvents.OnPlayerMulligan.Add(New Action(Of Card)(AddressOf onMulligan))

        'Add handlers to reload grammar when needed
        GameEvents.OnGameStart.Add(New Action(AddressOf requestRecogUpdate))
        GameEvents.OnTurnStart.Add(New Action(Of ActivePlayer)(AddressOf requestRecogUpdate))

        GameEvents.OnInMenu.Add(New Action(AddressOf requestRecogUpdate))

        GameEvents.OnPlayerDeckDiscard.Add(New Action(Of Card)(AddressOf requestRecogUpdate))
        GameEvents.OnPlayerDraw.Add(New Action(Of Card)(AddressOf requestRecogUpdate))
        GameEvents.OnPlayerFatigue.Add(New Action(Of Integer)(AddressOf requestRecogUpdate))
        GameEvents.OnPlayerGet.Add(New Action(Of Card)(AddressOf requestRecogUpdate))
        GameEvents.OnPlayerHandDiscard.Add(New Action(Of Card)(AddressOf requestRecogUpdate))
        GameEvents.OnPlayerHeroPower.Add(New Action(AddressOf requestRecogUpdate))
        GameEvents.OnPlayerPlay.Add(New Action(Of Card)(AddressOf requestRecogUpdate))
        GameEvents.OnPlayerPlayToDeck.Add(New Action(Of Card)(AddressOf requestRecogUpdate))
        GameEvents.OnPlayerPlayToHand.Add(New Action(Of Card)(AddressOf requestRecogUpdate))

        GameEvents.OnOpponentPlay.Add(New Action(Of Card)(AddressOf requestRecogUpdate))
        GameEvents.OnOpponentDraw.Add(New Action(AddressOf requestRecogUpdate))
        GameEvents.OnOpponentHeroPower.Add(New Action(AddressOf requestRecogUpdate))

        'Handlers for plugin settings and overlay size
        AddHandler My.Settings.PropertyChanged, AddressOf doOverlayLayout
        AddHandler Overlay.OverlayCanvas.SizeChanged, AddressOf doOverlayLayout

        actionInProgress = False

        timerReset.Interval = 2000
        timerReset.Enabled = True

        hotkeyWorker.RunWorkerAsync() 'Start listening for hotkey
        grammarReloader.RunWorkerAsync()

        ToggleSpeech()
    End Sub ' Run when the plugin is first initialized

    'Speech recognition events
    Public Sub onSpeechRecognized(sender As Object, e As SpeechRecognizedEventArgs) Handles hsRecog.SpeechRecognized
        'If hearthstone is inactive, exit
        If Not checkActiveWindow() Then
            writeLog("Heard command """ & e.Result.Text & """ but Hearthstone was inactive")
            actionInProgress = False
            Return
        End If

        'If below preset confidence threshold then exit
        If e.Result.Confidence < My.Settings.Threshold / 100 Then
            writeLog("Heard command """ & e.Result.Text & """ but it was below the recognition threshold")
            actionInProgress = False
            Return
        End If
        updateStatusText("Heard """ & e.Result.Text & """")
        writeLog("Command recognized """ & e.Result.Text & """ - executing action")
        Do While actionInProgress
            'Loop if another command is executing or we get BUGS
            Sleep(1)
        Loop

        actionInProgress = True
        Dim sreReset = sreListen
        If sreReset Then ToggleSpeech()

        'start command processing

        If Debugger.IsAttached Then 'debug only commands
            If e.Result.Text = "debug show cards" Then
                For i = 1 To handCards.Count
                    moveCursorToEntity(handCards.Item(i - 1).Id)
                    Sleep(500)
                Next
            End If
            If e.Result.Text = "debug show friendlies" Then
                For i = 1 To boardFriendly.Count
                    moveCursorToEntity(boardFriendly.Item(i - 1).Id)
                    Sleep(500)
                Next
                moveCursorToEntity(PlayerEntity.Id)
            End If
            If e.Result.Text = "debug show enemies" Then
                For i = 1 To boardOpposing.Count
                    moveCursorToEntity(boardOpposing.Item(i - 1).Id)
                    Sleep(500)
                Next
                moveCursorToEntity(OpponentEntity.Id)
            End If
        End If

        'do menu processing
        If e.Result.Semantics.ContainsKey("menu") And Game.IsInMenu Then
            doMenu(e)
        End If

        'do game processing
        If e.Result.Semantics.ContainsKey("action") Then
            Select Case e.Result.Semantics("action").Value
                Case "target" 'move cursor to target
                    doTarget(e)

                Case "click" 'send a click
                    doClick(e)

                Case "mulligan" 'mulligan a card or confirm
                    doMulligan(e)

                Case "play" 'play a card
                    doPlay(e)

                Case "attack" 'Attack with minion
                    doAttack(e)

                Case "hero" ' Use hero power
                    doHero(e)

                Case "say" ' Do an emote
                    doSay(e)

                Case "choose" 'Choose an option of x
                    doChoose(e)

                Case "cancel" 'simply right click
                    sendRightClick()

                Case "end"
                    moveCursor(91, 46) 'end turn button
                    sendLeftClick()

            End Select

        End If

        lastCommand = e.Result.Text 'set last command executed
        hsRecog.RequestRecognizerUpdate() 'request grammar update
        actionInProgress = False 'end command processing
        If sreReset Then ToggleSpeech()
    End Sub ' Handles processing recognized speech input
    Public Sub onUpdateReached(sender As Object, e As RecognizerUpdateReachedEventArgs) Handles hsRecog.RecognizerUpdateReached
        Do While actionInProgress
            Sleep(1)
        Loop

        rebuildCardData()
        hsRecog.UnloadAllGrammars()
        hsRecog.LoadGrammar(buildGrammar)
    End Sub ' Handles updating the grammar between commands
    Public Sub requestRecogUpdate(Optional e = Nothing)
        Do While actionInProgress
            Sleep(1)
        Loop
        hsRecog.RequestRecognizerUpdate()
    End Sub ' Request the SpeechRecognitionEngine update asynchronously

    'Event handlers
    Public Sub onNewGame()
        writeLog("New Game detected")
        ' Initialize controller IDs
        playerID = Nothing
        opponentID = Nothing
        mulliganDone = False

        If Not IsNothing(PlayerEntity) Then _
                playerID = PlayerEntity.GetTag(GAME_TAG.CONTROLLER)
        If Not IsNothing(OpponentEntity) Then _
                opponentID = OpponentEntity.GetTag(GAME_TAG.CONTROLLER)

    End Sub ' Runs when a new game is started
    Public Sub onMulligan(Optional c As Card = Nothing)
        mulliganDone = True
    End Sub
    Public Sub onResetTimer() Handles timerReset.Tick
        If sreListen = True Then
            updateStatusText("Listening... (F12 to stop)")
        Else
            updateStatusText("Stopped (Press F12)")
        End If

        timerReset.Enabled = False
    End Sub ' Resets the 

    'Voice command handlers
    Private Sub doChoose(e As SpeechRecognizedEventArgs)
        Dim optNum = e.Result.Semantics("option").Value
        Dim optmax = e.Result.Semantics("max").Value
        moveCursorToOption(optNum, optmax)
    End Sub 'handles selecting an option
    Private Sub doSay(e As SpeechRecognizedEventArgs)
        Dim emote = e.Result.Semantics("emote").Value
        Select Case emote
            Case "thanks"
                moveCursor(50, 75)
                sendRightClick()
                Sleep(200)
                moveCursor(40, 64)
                sendLeftClick()
            Case "well played"
                moveCursor(50, 75)
                sendRightClick()
                Sleep(200)
                moveCursor(40, 72)
                sendLeftClick()
            Case "greetings"
                moveCursor(50, 75)
                sendRightClick()
                Sleep(200)
                moveCursor(40, 80)
                sendLeftClick()
            Case "sorry"
                moveCursor(50, 75)
                sendRightClick()
                Sleep(200)
                moveCursor(60, 64)
                sendLeftClick()
            Case "oops"
                moveCursor(50, 75)
                sendRightClick()
                Sleep(200)
                moveCursor(60, 72)
                sendLeftClick()
            Case "threaten"
                moveCursor(50, 75)
                sendRightClick()
                Sleep(200)
                moveCursor(60, 80)
                sendLeftClick()
        End Select
    End Sub 'handles emotes
    Private Sub doClick(e As SpeechRecognizedEventArgs)
        If e.Result.Semantics.ContainsKey("heropower") Then 'target hero or hero power
            Dim x, y
            If e.Result.Semantics("herotarget").Value = "friendly" Then y = 80 Else y = 20
            If e.Result.Semantics("heropower").Value = "hero" Then x = 50 Else x = 60
            moveCursor(x, y)
        End If
        If e.Result.Semantics.ContainsKey("card") Then
            Dim targetName = e.Result.Semantics("card").Value
            moveCursorToEntity(targetName)
        End If
        If e.Result.Semantics.ContainsKey("friendly") Then
            Dim targetName = e.Result.Semantics("friendly").Value
            moveCursorToEntity(targetName)
        End If
        If e.Result.Semantics.ContainsKey("opposing") Then
            Dim targetName = e.Result.Semantics("opposing").Value
            moveCursorToEntity(targetName)
        End If
        sendLeftClick()
        Sleep(100)
    End Sub 'handle clicking mouse
    Private Sub doMenu(e As SpeechRecognizedEventArgs)
        Select Case e.Result.Semantics("menu").Value
            Case "play"
                moveCursor(50, 31)
                sendLeftClick()
            Case "casual"
                moveCursor(75, 20)
                sendLeftClick()
            Case "ranked"
                moveCursor(85, 20)
                sendLeftClick()
            Case "basic"
                moveCursor(23, 90)
                sendLeftClick()
            Case "custom"
                moveCursor(45, 90)
                sendLeftClick()
            Case "start game"
                moveCursor(80, 85)
                sendLeftClick()

            Case "solo"
                moveCursor(50, 38)
                sendLeftClick()

            'arena commands
            Case "arena"
                moveCursor(50, 45)
                sendLeftClick()
            Case "buy arena with gold"
                moveCursor(60, 62)
                sendLeftClick()
            Case "cancel arena"
                moveCursor(50, 75)
                sendLeftClick()
            Case "start arena"
                moveCursor(60, 75)
                sendLeftClick()
            Case "hero 1"
                moveCursor(20, 40)
                sendLeftClick()
            Case "hero 2"
                moveCursor(40, 40)
                sendLeftClick()
            Case "hero 3"
                moveCursor(55, 40)
                sendLeftClick()
            Case "card 1"
                moveCursor(20, 40)
                sendLeftClick()
            Case "card 2"
                moveCursor(40, 40)
                sendLeftClick()
            Case "card 3"
                moveCursor(55, 40)
                sendLeftClick()
            Case "confirm"
                moveCursor(50, 45)
                sendLeftClick()

            Case "brawl"
                moveCursor(50, 52)
                sendLeftClick()
            Case "start brawl"
                moveCursor(65, 85)
                sendLeftClick()


            Case "open packs"
                moveCursor(40, 85)
                sendLeftClick()
            Case "open top pack"
                moveCursor(12, 20)
                startDrag()
                moveCursor(59, 49)
                endDrag()
            Case "open bottom pack"
                moveCursor(12, 50)
                startDrag()
                moveCursor(59, 49)
                endDrag()
            Case "open card 1"
                moveCursor(60, 35)
                sendLeftClick()
            Case "open card 2"
                moveCursor(80, 35)
                sendLeftClick()
            Case "open card 3"
                moveCursor(70, 70)
                sendLeftClick()
            Case "open card 4"
                moveCursor(50, 70)
                sendLeftClick()
            Case "open card 5"
                moveCursor(40, 35)
                sendLeftClick()
            Case "done"
                moveCursor(60, 50)
                sendLeftClick()

            Case "cancel"
                moveCursor(52, 85)
                sendLeftClick()
            Case "back"
                moveCursor(92, 91)
                sendLeftClick()

            Case "deck"
                Dim deckNum = e.Result.Semantics("deck").Value
                Dim deckRow = 1
                Dim deckCol = deckNum
                Do While deckCol > 3
                    deckCol -= 3
                Loop

                If deckNum > 3 Then deckRow = 2
                If deckNum > 6 Then deckRow = 3
                Dim deckX As Integer
                Dim deckY As Integer
                deckX = 2 + (deckCol * 16)
                deckY = 8 + (deckRow * 20)
                moveCursor(deckX, deckY)
                sendLeftClick()
        End Select
    End Sub ' handle menu commands
    Private Sub doHero(e As SpeechRecognizedEventArgs)
        If e.Result.Semantics.ContainsKey("friendly") Then
            Dim friendlyID = e.Result.Semantics("friendly").Value
            moveCursor(62, 76)
            startDrag()
            moveCursorToEntity(friendlyID)
            endDrag()
        ElseIf e.Result.Semantics.ContainsKey("opposing") Then
            Dim opposingID = e.Result.Semantics("opposing").Value
            moveCursor(62, 76)
            startDrag()
            moveCursorToEntity(opposingID)
            endDrag()
        Else
            moveCursor(62, 76)
            sendLeftClick()
        End If
        Sleep(100)
    End Sub 'handle hero powers
    Private Sub doAttack(e As SpeechRecognizedEventArgs)
        If e.Result.Semantics.ContainsKey("opposing") Then ' Target is a minion

            Dim myMinion = e.Result.Semantics("friendly").Value
            Dim targetMinion = e.Result.Semantics("opposing").Value
            moveCursorToEntity(myMinion)
            startDrag()
            moveCursorToEntity(targetMinion)
            endDrag()
        Else ' Not a minion, attack face
            Dim myMinion = e.Result.Semantics("friendly").Value
            moveCursorToEntity(myMinion)
            startDrag()
            moveCursor(50, 20)
            endDrag()
        End If
        Sleep(100)
    End Sub 'handle attacking
    Private Sub doPlay(e As SpeechRecognizedEventArgs)
        Dim myCard = e.Result.Semantics("card").Value
        Dim cardType = handCards.First(Function(x) x.Id = myCard).Card.Type

        If e.Result.Semantics.ContainsKey("friendly") Then 'Play card to friendly target
            Dim destTarget = e.Result.Semantics("friendly").Value
            If cardType = "Minion" Then 'Card is a minion
                Dim newTarget = destTarget.ToString.Substring(1) - 1
                dragTargetToTarget(myCard, "f" & newTarget.ToString.Trim) 'Play to the left of friendly target
            Else 'Card is a spell
                dragTargetToTarget(myCard, destTarget) 'Direct drag to target
            End If


        ElseIf e.Result.Semantics.ContainsKey("opposing") Then 'Play card to opposing target
            Dim targetName = e.Result.Semantics("opposing").Value
            dragTargetToTarget(myCard, targetName)
        Else 'Play card with no target
            If cardType = "Minion" Then
                moveCursorToEntity(myCard)
                startDrag()
                moveCursor(85, 55) 'play to right of board
                endDrag()
            Else
                moveCursorToEntity(myCard)
                startDrag()
                moveCursor(40, 75) 'play to board
                endDrag()
            End If

        End If
        Sleep(100)
    End Sub
    Private Sub doMulligan(e As SpeechRecognizedEventArgs)
        If e.Result.Semantics.ContainsKey("card") Then
            Dim targetID = e.Result.Semantics("card").Value
            Dim targetNum = Entities.First(Function(x) x.Id = targetID).GetTag(GAME_TAG.ZONE_POSITION)
            If Game.OpponentHasCoin Then
                moveCursorToOption(targetNum, 3)
                sendLeftClick()
            Else
                moveCursorToOption(targetNum, 4)
                sendLeftClick()
            End If
        Else
            moveCursor(50, 80)
            sendLeftClick()
        End If
    End Sub 'handle mulligan
    Private Sub doTarget(e As SpeechRecognizedEventArgs)
        If e.Result.Semantics.ContainsKey("heropower") Then 'target hero or hero power
            Dim x, y
            If e.Result.Semantics("herotarget").Value = "friendly" Then y = 80 Else y = 20
            If e.Result.Semantics("heropower").Value = "hero" Then x = 50 Else x = 60
            moveCursor(x, y)
        End If
        If e.Result.Semantics.ContainsKey("card") Then 'target card
            Dim targetName = e.Result.Semantics("card").Value
            moveCursorToEntity(targetName)
        End If
        If e.Result.Semantics.ContainsKey("friendly") Then 'target friendly
            Dim targetName = e.Result.Semantics("friendly").Value
            moveCursorToEntity(targetName)
        End If
        If e.Result.Semantics.ContainsKey("opposing") Then 'target opposing
            Dim targetName = e.Result.Semantics("opposing").Value
            moveCursorToEntity(targetName)
        End If
    End Sub 'handle targeting cursor

    'Mouse functions
    Public Sub sendLeftClick()
        mouse_event(MOUSE_LEFTDOWN, Cursor.Position.X, Cursor.Position.Y, 0, 0)
        Sleep(50)
        mouse_event(MOUSE_LEFTUP, Cursor.Position.X, Cursor.Position.Y, 0, 0)
        Sleep(100)
    End Sub
    Public Sub sendRightClick()
        mouse_event(MOUSE_RIGHTDOWN, Cursor.Position.X, Cursor.Position.Y, 0, 0)
        Sleep(50)
        mouse_event(MOUSE_RIGHTUP, Cursor.Position.X, Cursor.Position.Y, 0, 0)
        Sleep(100)
    End Sub
    Public Sub startDrag()
        mouse_event(MOUSE_LEFTDOWN, Cursor.Position.X, Cursor.Position.Y, 0, 0)
        Sleep(100)
    End Sub
    Public Sub endDrag()
        mouse_event(MOUSE_LEFTUP, Cursor.Position.X, Cursor.Position.Y, 0, 0)
        Sleep(100)
    End Sub
    Public Sub dragTargetToTarget(startEntity As Integer, endEntity As String)
        moveCursorToEntity(startEntity)
        startDrag()
        moveCursorToEntity(endEntity)
        endDrag()
    End Sub
    Public Sub moveCursorToOption(optionNum As Integer, totalOptions As Integer)
        Dim optionSize As Integer = 20
        Dim optionsWidth As Integer = totalOptions * optionSize
        Dim myOption As Integer = (optionNum * optionSize) - (optionSize / 2)
        Dim optionStart As Integer = 50 - (optionsWidth / 2)
        moveCursor(optionStart + myOption, 50)
    End Sub
    Public Sub moveCursor(xPercent As Integer, yPercent As Integer)
        'First, find the Hearthstone window and it's size
        Dim hWndHS As IntPtr = FindWindow(Nothing, "Hearthstone")
        Dim rectHS As RECT
        GetWindowRect(hWndHS, rectHS)

        Dim windowWidth = rectHS.Right - rectHS.Left
        Dim uiHeight = rectHS.Bottom - rectHS.Top
        Dim uiWidth = ((uiHeight) / 3) * 4 ' A 4:3 square in the center

        Dim xOffset = (windowWidth - uiWidth) / 2 ' The space on the side of the game UI

        Dim cursorX As Integer = (xPercent / 100) * uiWidth + xOffset + rectHS.Left
        Dim cursorY As Integer = (yPercent / 100) * uiHeight + rectHS.Top + 8

        Cursor.Position = New Point(cursorX, cursorY)
        Sleep(100)
    End Sub

    Public Function moveCursorToEntity(EntityID As Integer)
        Dim targetEntity As Entity

        Try 'Try and find the entity in the game
            targetEntity = Entities.First(Function(x) x.Id = EntityID)
        Catch ex As Exception
            Return False
        End Try

        'First, check if the entity is a card in our hand
        If targetEntity.IsInHand And targetEntity.GetTag(GAME_TAG.CONTROLLER) = playerID Then
            Dim cardNum As Integer = targetEntity.GetTag(GAME_TAG.ZONE_POSITION)
            Dim totalCards As Integer = handCards.Count
            Dim handwidth = If(totalCards < 4, 10 * totalCards, 38)

            Dim x = (((cardNum) / (totalCards)) * handwidth) - (handwidth / 2) - 0.8
            Dim y = (x * -0.16) ^ 2
            moveCursor(x + 44, y + 89)
            Return True
        End If

        'Next, check whether it is a friendly minion
        If targetEntity.IsInPlay And targetEntity.IsMinion And targetEntity.GetTag(GAME_TAG.CONTROLLER) = playerID Then
            Dim minionNum As Integer = targetEntity.GetTag(GAME_TAG.ZONE_POSITION)
            Dim totalMinions As Integer = boardFriendly.Count
            Dim totalWidth = totalMinions * 9
            Dim minionX = minionNum * 9

            Dim minX = 50 - (totalWidth / 2) + minionX - 6
            moveCursor(minX, 55)
            Return True
        End If

        'Then, check whether it is an opposing minion
        If targetEntity.IsInPlay And targetEntity.IsMinion And targetEntity.GetTag(GAME_TAG.CONTROLLER) = opponentID Then
            Dim minionNum As Integer = targetEntity.GetTag(GAME_TAG.ZONE_POSITION)
            Dim totalMinions As Integer = boardopposing.Count
            Dim totalWidth = totalMinions * 9
            Dim minionX = minionNum * 9

            Dim minX = 50 - (totalWidth / 2) + minionX - 5
            moveCursor(minX, 40)
            Return True

        End If

        'Finally, check whether it is a hero
        If targetEntity.IsPlayer Then
            moveCursor(50, 75)
            Return True
        End If

        If targetEntity.IsOpponent Then
            moveCursor(50, 20)
            Return True
        End If

        Return False 'failed to locate entity on the board

    End Function

    'Miscellaneous functions
    Public Function checkActiveWindow()
        Dim activeHwnd = GetForegroundWindow()
        Dim activeWindowText As New System.Text.StringBuilder(32)
        GetWindowText(activeHwnd, activeWindowText, activeWindowText.Capacity)
        If activeWindowText.ToString = "Hearthstone" Then
            Return True
        Else
            Return False
        End If
    End Function 'Checks if the hearthstone window is active
    Public Function doOverlayLayout() As System.Windows.SizeChangedEventHandler
        'Update positioning/visibility of status text
        If My.Settings.showStatusText = False Then
            hdtStatus.Visibility = System.Windows.Visibility.Hidden
        Else
            hdtStatus.Visibility = System.Windows.Visibility.Visible
        End If
        Select Case My.Settings.statusTextPos ' position status text
            Case 0 'Top left
                Canvas.SetTop(hdtStatus, 32)
                Canvas.SetLeft(hdtStatus, 8)
                hdtStatus.TextAlignment = System.Windows.TextAlignment.Left
            Case 1 'Bottom left
                Canvas.SetTop(hdtStatus, overlayCanvas.Height - 64)
                Canvas.SetLeft(hdtStatus, 8)
                hdtStatus.TextAlignment = System.Windows.TextAlignment.Left
            Case 2 'Top right
                Canvas.SetTop(hdtStatus, 8)
                Canvas.SetLeft(hdtStatus, overlayCanvas.Width - hdtStatus.ActualWidth - 8)
                hdtStatus.TextAlignment = System.Windows.TextAlignment.Right
            Case 3 'Bottom right
                Canvas.SetTop(hdtStatus, overlayCanvas.Height - 64)
                Canvas.SetLeft(hdtStatus, overlayCanvas.Width - hdtStatus.ActualWidth - 8)
                hdtStatus.TextAlignment = System.Windows.TextAlignment.Right
        End Select

        Return Nothing
    End Function 'Handles changing the overlay layout when it is resized
    Public Sub updateStatusText(Status As String)
        If Status = Nothing Then
            onResetTimer()
            return
        End If
        Try
            hdtStatus.Dispatcher.Invoke(Sub()
                                            Dim newStatus = "HDT-Voice: "
                                            newStatus &= Status
                                            If My.Settings.showLast Then
                                                newStatus &= vbNewLine & "Last executed: " & lastCommand
                                            End If
                                            hdtStatus.Text = newStatus
                                            timerReset.Enabled = True
                                        End Sub)
        Catch ex As Exception
            Return
        End Try

    End Sub 'Updates the text on the status text block
    Public Function buildGrammar() As Grammar

        If Game.IsInMenu Then
            writeLog("IsInMenu=True - Building menu grammar")
            Dim menuGrammar As New GrammarBuilder
            Dim menuChoices As New Choices


            menuChoices.Add(New SemanticResultKey("menu", "play"))
            menuChoices.Add(New SemanticResultKey("menu", "casual"))
            menuChoices.Add(New SemanticResultKey("menu", "ranked"))
            menuChoices.Add(New SemanticResultKey("menu", "basic"))
            menuChoices.Add(New SemanticResultKey("menu", "custom"))
            menuChoices.Add(New SemanticResultKey("menu", "start game"))

            menuChoices.Add(New SemanticResultKey("menu", "solo"))

            menuChoices.Add(New SemanticResultKey("menu", "arena"))
            menuChoices.Add(New SemanticResultKey("menu", "start arena"))
            menuChoices.Add(New SemanticResultKey("menu", "buy arena with gold"))
            menuChoices.Add(New Choices(New SemanticResultKey("menu", "cancel arena"),
                                        New SemanticResultKey("menu", New SemanticResultValue("OK", "cancel arena")),
                                        New SemanticResultKey("menu", New SemanticResultValue("choose", "cancel arena"))))
            menuChoices.Add(New SemanticResultKey("menu", "hero 1"))
            menuChoices.Add(New SemanticResultKey("menu", "hero 2"))
            menuChoices.Add(New SemanticResultKey("menu", "hero 3"))
            menuChoices.Add(New SemanticResultKey("menu", "card 1"))
            menuChoices.Add(New SemanticResultKey("menu", "card 2"))
            menuChoices.Add(New SemanticResultKey("menu", "card 3"))
            menuChoices.Add(New SemanticResultKey("menu", "confirm"))

            menuChoices.Add(New Choices(New SemanticResultKey("menu", "brawl"),
                                        New SemanticResultKey("menu", New SemanticResultValue("tavern brawl", "brawl"))))
            menuChoices.Add(New SemanticResultKey("menu", "start brawl"))

            menuChoices.Add(New SemanticResultKey("menu", "open packs"))
            menuChoices.Add(New SemanticResultKey("menu", "open top pack"))
            menuChoices.Add(New SemanticResultKey("menu", "open bottom pack"))
            menuChoices.Add(New SemanticResultKey("menu", "open card 1"))
            menuChoices.Add(New SemanticResultKey("menu", "open card 2"))
            menuChoices.Add(New SemanticResultKey("menu", "open card 3"))
            menuChoices.Add(New SemanticResultKey("menu", "open card 4"))
            menuChoices.Add(New SemanticResultKey("menu", "open card 5"))
            menuChoices.Add(New SemanticResultKey("menu", "done"))

            menuChoices.Add(New SemanticResultKey("menu", "cancel"))
            menuChoices.Add(New SemanticResultKey("menu", "back"))

            Dim deckGrammar As New GrammarBuilder
            Dim deckChoices As New Choices
            deckGrammar.Append(New SemanticResultKey("menu", "deck"))
            For i = 1 To 9
                deckChoices.Add(New SemanticResultKey("deck", i.ToString))
            Next
            deckGrammar.Append(deckChoices)
            menuChoices.Add(deckGrammar)
            menuGrammar.Append(menuChoices)
            writeLog("Grammar building complete")
            Return New Grammar(menuGrammar)
        End If

        ' if the player or opponent entity is unknown, try initiate a new game
        If Game.IsRunning And playerID = 0 Or opponentID = 0 Then
            onNewGame()
        End If

        'generate names and numbers for all targets
        Dim cardGrammar As New GrammarBuilder

        Dim friendlyNames As New Choices("friendly", "my")
        Dim opposingNames As New Choices("opposing", "enemy", "opponent")

        If handCards.Count > 0 Then
            ' build grammar for cards in hand
            Dim handGrammarNames, handGrammarNumbers As New Choices
            For Each e In handCards
                Dim CardName As New String(e.Card.Name)
                Dim CardInstances = handCards.FindAll(Function(x) x.CardId = e.CardId)
                If CardInstances.Count > 1 Then ' if we have multiple cards with the same name, add a numeric identifier
                    If Not handGrammarNames.ToGrammarBuilder.DebugShowPhrases.Contains(CardName) Then
                        handGrammarNames.Add(New SemanticResultValue(CardName, e.Id))
                        handGrammarNumbers.Add(New SemanticResultValue(e.GetTag(GAME_TAG.ZONE_POSITION).ToString.Trim, e.Id))
                    Else
                        Dim CardNum As Integer = CardInstances.IndexOf(CardInstances.Find(Function(x) x.Id = e.Id)) + 1
                        CardName &= " " & CardNum.ToString
                        handGrammarNames.Add(New SemanticResultValue(CardName, e.Id))
                        handGrammarNumbers.Add(New SemanticResultValue(e.GetTag(GAME_TAG.ZONE_POSITION).ToString.Trim, e.Id))
                    End If
                Else
                    handGrammarNames.Add(New SemanticResultValue(CardName, e.Id))
                    handGrammarNumbers.Add(New SemanticResultValue(e.GetTag(GAME_TAG.ZONE_POSITION).ToString.Trim, e.Id))
                End If

            Next
            cardGrammar.Append(New SemanticResultKey("card", New Choices(handGrammarNames, handGrammarNumbers)))
        End If

        ' Build the grammar for friendly minions and hero
        Dim friendlyGrammar As New GrammarBuilder ' Represents the names and numbers of minions, and the hero
        Dim friendlyChoices As New Choices

        If boardFriendly.Count > 0 Then
            Dim friendlyGrammarNames, friendlyGrammarNumbers As New Choices
            For Each e In boardFriendly
                Dim CardName As New String(e.Card.Name)
                Dim CardInstances = boardFriendly.FindAll(Function(x) x.CardId = e.CardId)

                If CardInstances.Count > 1 Then ' More than one instance of the card on the board, append a number
                    'If it's not in the grammar already, add an un-numbered minion
                    If Not friendlyGrammarNames.ToGrammarBuilder.DebugShowPhrases.Contains(CardName) Then
                        friendlyGrammarNames.Add(New SemanticResultValue(CardName, e.Id))
                        friendlyGrammarNumbers.Add(New SemanticResultValue("minion " & e.GetTag(GAME_TAG.ZONE_POSITION).ToString.Trim, e.Id))
                    End If
                    Dim CardNum As Integer = CardInstances.IndexOf(CardInstances.Find(Function(x) x.Id = e.Id)) + 1
                    CardName &= " " & CardNum.ToString

                End If
                friendlyGrammarNames.Add(New SemanticResultValue(CardName, e.Id))
                friendlyGrammarNumbers.Add(New SemanticResultValue("minion " & e.GetTag(GAME_TAG.ZONE_POSITION).ToString.Trim, e.Id))
            Next
            friendlyChoices.Add(friendlyGrammarNames, friendlyGrammarNumbers)
        End If

        If Not IsNothing(PlayerEntity) Then
            Dim friendlyHero As New Choices
            friendlyHero.Add(New SemanticResultValue("hero", PlayerEntity.Id))
            friendlyHero.Add(New SemanticResultValue("face", PlayerEntity.Id))
            friendlyChoices.Add(friendlyHero)
            friendlyGrammar.Append(New SemanticResultKey("friendly", friendlyChoices))
        End If


        ' Build grammar for opposing minions and hero
        Dim opposingGrammar As New GrammarBuilder
        Dim opposingChoices As New Choices
        If boardOpposing.Count > 0 Then

            Dim opposingGrammarNames, opposingGrammarNumbers As New Choices
            For Each e In boardOpposing
                Dim CardName As New String(e.Card.Name)
                Dim CardInstances = boardOpposing.FindAll(Function(x) x.CardId = e.CardId)
                If CardInstances.Count > 1 Then
                    If Not opposingGrammarNames.ToGrammarBuilder.DebugShowPhrases.Contains(CardName) Then
                        opposingGrammarNames.Add(New SemanticResultValue(CardName, e.Id))
                        opposingGrammarNumbers.Add(New SemanticResultValue("minion " & e.GetTag(GAME_TAG.ZONE_POSITION).ToString.Trim, e.Id))
                    End If
                    Dim CardNum As Integer = CardInstances.IndexOf(CardInstances.Find(Function(x) x.Id = e.Id)) + 1
                    CardName &= " " & CardNum.ToString
                End If
                opposingGrammarNames.Add(New SemanticResultValue(CardName, e.Id))
                opposingGrammarNumbers.Add(New SemanticResultValue("minion " & e.GetTag(GAME_TAG.ZONE_POSITION).ToString.Trim, e.Id))
            Next
            opposingChoices.Add(opposingGrammarNames, opposingGrammarNumbers)
        End If

        If Not IsNothing(OpponentEntity) Then
            Dim opposingHero As New Choices
            opposingHero.Add(New SemanticResultValue("hero", OpponentEntity.Id))
            opposingHero.Add(New SemanticResultValue("face", OpponentEntity.Id))
            opposingChoices.Add(opposingHero)
            opposingGrammar.Append(New SemanticResultKey("opposing", New Choices(opposingChoices)))
        End If


        ' Start building the final grammar
        Dim finalChoices As New Choices

        ' Check if we're at the mulligan, if so only the mulligan grammar will be returned
        If not Game.IsMulliganDone Then
            writeLog("IsMulliganDone=False - Building mulligan grammar")
            Dim mulliganBuilder As New GrammarBuilder
            mulliganBuilder.Append(New SemanticResultKey("action", New SemanticResultValue("click", "mulligan")))
            Dim mulliganChoices As New Choices
            mulliganChoices.Add("confirm")
            mulliganChoices.Add(cardGrammar)
            mulliganBuilder.Append(New Choices(mulliganChoices, "confirm"))

            finalChoices.Add(mulliganBuilder)
            writeLog("Grammar building complete")
            Return New Grammar(New GrammarBuilder(finalChoices))
        End If

        writeLog("Started building game grammar")

        Dim heroChoice = New Choices(New SemanticResultValue("hero power", "hero"))
        'Attempt to read active hero power name

        Dim heroPowerEntity As Entity = Nothing

        Try
            heroPowerEntity = Entities.First(Function(x)
                                                 Dim cardType = x.GetTag(GAME_TAG.CARDTYPE)
                                                 Dim cardTroller = x.GetTag(GAME_TAG.CONTROLLER)

                                                 If cardType = Hearthstone.TAG_CARDTYPE.HERO_POWER And cardTroller = playerID And x.IsInPlay = True Then
                                                     Return True
                                                 End If

                                                 Return False
                                             End Function)
        Catch ex As Exception
            writeLog("Hero power not found!")
        End Try
        If Not IsNothing(heroPowerEntity) Then
            Dim heroPowerName As String = heroPowerEntity.Card.Name
            heroChoice.Add(New SemanticResultValue(heroPowerName, "hero"))
        End If



        'build grammar for card actions
        If cardGrammar.DebugShowPhrases.Count Then
            'target card
            Dim targetCards As New GrammarBuilder
            targetCards.Append(New SemanticResultKey("action", "target"))
            targetCards.Append("card")
            targetCards.Append(cardGrammar)
            finalChoices.Add(targetCards)



            'play card to the left of friendly target
            If friendlyGrammar.DebugShowPhrases.Count Then
                Dim playToFriendly As New GrammarBuilder
                If Not My.Settings.quickPlay Then _
                    playToFriendly.Append("play")
                playToFriendly.Append(cardGrammar)
                playToFriendly.Append(New SemanticResultKey("action", New SemanticResultValue(New Choices("on", "to"), "play")))
                playToFriendly.Append(friendlyNames)
                playToFriendly.Append(friendlyGrammar)

                finalChoices.Add(playToFriendly)
            End If

            'play card to opposing target
            If opposingGrammar.DebugShowPhrases.Count Then
                Dim playToOpposing As New GrammarBuilder
                If Not My.Settings.quickPlay Then _
                    playToOpposing.Append("play")
                playToOpposing.Append(cardGrammar)
                playToOpposing.Append(New SemanticResultKey("action", New SemanticResultValue(New Choices("on", "to"), "play")))
                playToOpposing.Append(opposingNames)
                playToOpposing.Append(opposingGrammar)

                finalChoices.Add(playToOpposing)
            End If

            'play card with no target
            Dim playCards As New GrammarBuilder
            playCards.Append(New SemanticResultKey("action", "play"))
            playCards.Append(cardGrammar)
            finalChoices.Add(playCards)

        End If



        'build grammar friendly minion actions
        If friendlyGrammar.DebugShowPhrases.Count Then
            'target friendly minion
            Dim targetFriendly As New GrammarBuilder
            targetFriendly.Append(New SemanticResultKey("action", "target"))
            targetFriendly.Append(friendlyNames)
            targetFriendly.Append(friendlyGrammar)
            finalChoices.Add(targetFriendly)

            'attack target with friendly minion
            Dim attackFriendly As New GrammarBuilder
            attackFriendly.Append(New SemanticResultKey("action", "attack"))
            attackFriendly.Append(opposingGrammar)
            attackFriendly.Append("with")
            attackFriendly.Append(friendlyGrammar)
            finalChoices.Add(attackFriendly)

            'SHORTCUT attack target with minion
            Dim goTarget As New GrammarBuilder
            goTarget.Append(friendlyGrammar)
            goTarget.Append(New SemanticResultKey("action", New Choices(New SemanticResultValue("go", "attack"), New SemanticResultValue("attack", "attack"))))
            goTarget.Append(opposingGrammar)
            finalChoices.Add(goTarget)

            'use hero power on friendly target
            Dim heroFriendly As New GrammarBuilder
            If Not My.Settings.quickPlay Then _
                heroFriendly.Append("use")
            heroFriendly.Append(New SemanticResultkey("action", heroChoice))
            heroFriendly.Append(New Choices("on", "to"))
            heroFriendly.Append(friendlyNames)
            heroFriendly.Append(friendlyGrammar)
            finalChoices.Add(heroFriendly)

            'click friendly target
            Dim clickFriendly As New GrammarBuilder
            clickFriendly.Append(New SemanticResultKey("action", "click"))
            clickFriendly.Append(friendlyNames)
            clickFriendly.Append(friendlyGrammar)
            finalChoices.Add(clickFriendly)


        End If

        If opposingGrammar.DebugShowPhrases.Count Then
            Dim targetOpposing As New GrammarBuilder
            targetOpposing.Append(New SemanticResultKey("action", "target"))
            targetOpposing.Append(opposingNames)
            targetOpposing.Append(opposingGrammar)
            finalChoices.Add(targetOpposing)

            Dim heroOpposing As New GrammarBuilder
            If Not My.Settings.quickPlay Then _
                heroOpposing.Append("use")
            heroOpposing.Append(New SemanticResultKey("action", heroChoice))
            heroOpposing.Append(New Choices("on", "to"))
            heroOpposing.Append(opposingNames)
            heroOpposing.Append(opposingGrammar)
            finalChoices.Add(heroOpposing)

            'click opposing target
            Dim clickOpposing As New GrammarBuilder
            clickOpposing.Append(New SemanticResultKey("action", "click"))
            clickOpposing.Append(opposingNames)
            clickOpposing.Append(opposingGrammar)
            finalChoices.Add(clickOpposing)
        End If

        Dim heroPower As New GrammarBuilder
        If Not My.Settings.quickPlay Then _
            heroPower.Append("use") ' if quickplay is enabled, just say "hero power"
        heroPower.Append(New SemanticResultKey("action", heroChoice))
        finalChoices.Add(heroPower)

        Dim endTurn As New GrammarBuilder
        endTurn.Append(New SemanticResultKey("action", "end"))
        endTurn.Append("turn")
        finalChoices.Add(endTurn)

        Dim sayEmote As New GrammarBuilder
        sayEmote.Append(New SemanticResultKey("action", "say"))
        Dim sayChoices As New Choices
        sayChoices.Add(New SemanticResultValue("thanks", "thanks"))
        sayChoices.Add(New SemanticResultValue("thank you", "thanks"))
        sayChoices.Add(New SemanticResultValue("well played", "well played"))
        sayChoices.Add(New SemanticResultValue("greetings", "greetings"))
        sayChoices.Add(New SemanticResultValue("hello", "greetings"))
        sayChoices.Add(New SemanticResultValue("sorry", "sorry"))
        sayChoices.Add(New SemanticResultValue("oops", "oops"))
        sayChoices.Add(New SemanticResultValue("whoops", "oops"))
        sayChoices.Add(New SemanticResultValue("threaten", "threaten"))
        sayEmote.Append(New SemanticResultKey("emote", sayChoices))
        finalChoices.Add(sayEmote)

        Dim chooseOption As New GrammarBuilder
        Dim maxOptions = 4
        Dim optionChoices As New Choices
        For optMax = 1 To maxOptions
            optionChoices.Add(optMax.ToString)
        Next

        chooseOption.Append(New SemanticResultKey("action", "choose"))
        chooseOption.Append("option")
        chooseOption.Append(New SemanticResultKey("option", optionChoices))
        chooseOption.Append("of")
        chooseOption.Append(New SemanticResultKey("max", optionChoices))
        finalChoices.Add(chooseOption)

        finalChoices.Add(New SemanticResultKey("action", "click"))
        finalChoices.Add(New SemanticResultKey("action", "cancel"))

        If Debugger.IsAttached Then
            finalChoices.Add("debug show cards")
            finalChoices.Add("debug show friendlies")
            finalChoices.Add("debug show enemies")
        End If

        writeLog("Grammar building complete")
        actionInProgress = False
        Try
            Return New Grammar(New GrammarBuilder(finalChoices))
        Catch ex As Exception
            writeLog("Exception when building grammar: " & ex.Message)
            Return New Grammar(New GrammarBuilder("gramar error"))
        End Try


    End Function 'Builds and returns grammar for the speech recognition engine
    Public Sub rebuildCardData()

        writeLog("Rebuilding data")

        'build list of cards in hand
        handCards.Clear()

        For Each e In Entities
            If e.IsInHand And e.GetTag(GAME_TAG.CONTROLLER) = playerID Then
                handCards.Add(e)
            End If
        Next

        ' sort cards by position in hand
        handCards.Sort(Function(e1 As Entity, e2 As Entity)
                           Return e1.GetTag(GAME_TAG.ZONE_POSITION).CompareTo(e2.GetTag(GAME_TAG.ZONE_POSITION))
                       End Function)

        ' build list of minions on board
        boardFriendly.Clear()
        boardOpposing.Clear()

        For Each e In Entities
            If e.IsInPlay And e.IsMinion Then
                If e.IsControlledBy(playerID) Then
                    boardFriendly.Add(e)
                ElseIf e.IsControlledBy(opponentID) Then
                    boardOpposing.Add(e)
                End If
            End If
        Next

        ' sort by position on board
        boardFriendly.Sort(Function(e1 As Entity, e2 As Entity)
                               Return e1.GetTag(GAME_TAG.ZONE_POSITION).CompareTo(e2.GetTag(GAME_TAG.ZONE_POSITION))
                           End Function)

        boardOpposing.Sort(Function(e1 As Entity, e2 As Entity)
                               Return e1.GetTag(GAME_TAG.ZONE_POSITION).CompareTo(e2.GetTag(GAME_TAG.ZONE_POSITION))
                           End Function)

        writeLog("Complete")
    End Sub 'Rebuilds data for cards in hand and on board
    Public Sub writeLog(Line As String)
        Line = "HDT-Voice: " & Line
        Debug.WriteLine(Line)
        If My.Settings.outputDebug Then
            If IsNothing(voiceLog) Then
                voiceLog = New IO.StreamWriter("hdtvoicelog.txt")
            End If
            voiceLog.WriteLine(Line)
            voiceLog.Flush()
        Else
            voiceLog = Nothing
        End If
    End Sub 'Writes information to the debug output and the logfile if necessary
    Public Sub hotkeyWorker_DoWork() Handles hotkeyWorker.DoWork

        Do

            Dim hotkeyState As Short = GetAsyncKeyState(Keys.F12)
            If hotkeyState = 1 Or hotkeyState = Int16.MinValue Then
                ToggleSpeech()
                Sleep(500)
            End If
            Sleep(10)
        Loop

    End Sub
    Public Sub ToggleSpeech()
        If sreListen Then
            sreListen = False
            hsRecog.RecognizeAsyncCancel()
            updateStatusText(Nothing)
        Else
            sreListen = True
            updateStatusText(Nothing)
            If hsRecog.Grammars.Count = 0 Then
                hsRecog.LoadGrammar(buildGrammar)
            End If
            Try
                hsRecog.RecognizeAsync(RecognizeMode.Multiple)
            Catch ex As Exception

            End Try

        End If
    End Sub
End Class
