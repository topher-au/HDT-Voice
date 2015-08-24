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
Public Class hdtVoice
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

    ' Action processor list and worker
    Public actionList As New List(Of SpeechRecognizedEventArgs)
    Public WithEvents actionWorker As New BackgroundWorker

    Public voiceLog As IO.StreamWriter
    Public lastCommand As New String("none")

    Public WithEvents timerReset As New Timer

    'Overlay elements
    Public overlayCanvas As Canvas = Core.OverlayCanvas 'the main overlay object
    Public hdtStatus As HearthstoneTextBlock 'status text

    Public sreListen As Boolean ' Should we be listening?

    Public handCards, boardOpposing, boardFriendly As New List(Of Entity)
    Public playerID As Integer = 0
    Public opponentID As Integer = 0
    Public mulliganDone As Boolean
    Public updateInProgress As Boolean
    Public GrammarEngine As New HDTGrammarEngine
    'Properties
    Private ReadOnly Property Entities As Entity()
        Get
            ' Clone entities from game and return as array
            Dim EntArray = Helper.DeepClone(Core.Game.Entities).Values.ToArray
            Return EntArray
        End Get
    End Property ' The list of entities for the current game
    Private ReadOnly Property PlayerEntity As Entity
        Get
            Return Entities.FirstOrDefault(Function(x) x.IsPlayer())
        End Get
    End Property ' The player's entity
    Private ReadOnly Property OpponentEntity As Entity
        Get
            ' Return the Entity representing the player
            Return Entities.FirstOrDefault(Function(x) x.IsOpponent())
        End Get
    End Property ' The opponent entity

    'Main functions
    Public Sub Load()
        'Start loading HDT-Voice

        'Write basic system information to logfile

        writeLog("HDT-Voice {0}.{1} | {2}", {My.Application.Info.Version.Major, My.Application.Info.Version.Minor, My.Computer.Info.OSFullName})
        writeLog("--------------")
        writeLog("Current screen resolution: {0}x{1}", {Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height})
        writeLog("Initializing speech recognition object")

        'Attempt to initialize speech recognition
        Try

            Dim rec As IReadOnlyCollection(Of RecognizerInfo) = SpeechRecognitionEngine.InstalledRecognizers
            If rec.Count > 0 Then
                writeLog("Installed recognizers: {0}", rec.Count)
                For Each e In rec
                    writeLog(e.Culture.Name)
                Next
                writeLog("Initializing recognizer for: {0}", rec.First.Culture.Name)
                hsRecog = New SpeechRecognitionEngine(rec.First.Culture)
            Else
                writeLog("No speech recognizer found. Attempting to start engine anyway.")
                hsRecog = New SpeechRecognitionEngine()
            End If

            hsRecog.SetInputToDefaultAudioDevice()
            hsRecog.BabbleTimeout = New TimeSpan(0, 0, 3)
            hsRecog.InitialSilenceTimeout = New TimeSpan(0, 0, 3)
            hsRecog.EndSilenceTimeout = New TimeSpan(0, 0, 0, 0, 500)
            hsRecog.EndSilenceTimeoutAmbiguous = New TimeSpan(0, 0, 0, 0, 500)
            writeLog("Successfuly started speech recognition")
        Catch ex As Exception
            writeLog("Error initializing speech recognition: {0}", ex.Message)
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
        GameEvents.OnGameStart.Add(New Action(AddressOf updateRecognizer))
        GameEvents.OnTurnStart.Add(New Action(Of ActivePlayer)(AddressOf updateRecognizer))

        GameEvents.OnInMenu.Add(New Action(AddressOf updateRecognizer))

        'GameEvents.OnPlayerDeckDiscard.Add(New Action(Of Card)(AddressOf updateRecognizer))
        GameEvents.OnPlayerDraw.Add(New Action(Of Card)(AddressOf updateRecognizer))
        'GameEvents.OnPlayerFatigue.Add(New Action(Of Integer)(AddressOf updateRecognizer))
        GameEvents.OnPlayerGet.Add(New Action(Of Card)(AddressOf updateRecognizer))
        GameEvents.OnPlayerHandDiscard.Add(New Action(Of Card)(AddressOf updateRecognizer))
        'GameEvents.OnPlayerHeroPower.Add(New Action(AddressOf updateRecognizer))
        GameEvents.OnPlayerPlay.Add(New Action(Of Card)(AddressOf updateRecognizer))
        'GameEvents.OnPlayerPlayToDeck.Add(New Action(Of Card)(AddressOf updateRecognizer))
        GameEvents.OnPlayerPlayToHand.Add(New Action(Of Card)(AddressOf updateRecognizer))

        GameEvents.OnOpponentPlay.Add(New Action(Of Card)(AddressOf updateRecognizer))
        'GameEvents.OnOpponentDraw.Add(New Action(AddressOf updateRecognizer))
        'GameEvents.OnOpponentHeroPower.Add(New Action(AddressOf updateRecognizer))

        'Handlers for plugin settings and overlay size
        AddHandler My.Settings.PropertyChanged, AddressOf updateOverlay
        AddHandler Core.OverlayCanvas.SizeChanged, AddressOf updateOverlay

        timerReset.Interval = 3500
        timerReset.Enabled = True

        hotkeyWorker.RunWorkerAsync() 'Start listening for hotkey

        If My.Settings.autoListen And Not My.Settings.toggleOrPTT Then sreListen = True

        hsRecog.LoadGrammar(New Grammar(New GrammarBuilder("default")))
        hsRecog.RecognizeAsync(RecognizeMode.Multiple)

        actionWorker.RunWorkerAsync()
    End Sub ' Run when the plugin is first initialized
    Public Function BuildGrammar() As Grammar

        If Core.Game.IsInMenu Then
            writeLog("Menu grammar active")
            Return New Grammar(GrammarEngine.MenuGrammar)
        End If

        ' if the player or opponent entity is unknown, try initiate a new game
        If playerID = 0 Or opponentID = 0 Then
            onNewGame()
        End If

        GrammarEngine.RefreshGameData()

        ' Check if we're at the mulligan, if so only the mulligan grammar will be returned
        If Not Core.Game.IsMulliganDone Then
            writeLog("Building mulligan Grammar...")

            Dim mg = GrammarEngine.MulliganGrammar
            Return New Grammar(mg)
        End If


        ' Start building final Choices for the Grammar
        Dim finalChoices As New Choices

        If Debugger.IsAttached Then _
            finalChoices.Add(GrammarEngine.DebuggerGameCommands)

        finalChoices.Add(GrammarEngine.UseHeroPowerGrammar)
        finalChoices.Add(GrammarEngine.PlayCardGrammar)
        finalChoices.Add(GrammarEngine.AttackTargetGrammar)
        finalChoices.Add(GrammarEngine.ClickTargetGrammar)
        finalChoices.Add(GrammarEngine.TargetTargetGrammar)
        finalChoices.Add(GrammarEngine.SayEmote)
        finalChoices.Add(GrammarEngine.ChooseOptionGrammar(4))

        Dim endTurn As New GrammarBuilder
        endTurn.Append(New SemanticResultKey("action", "end"))
        endTurn.Append("turn")
        finalChoices.Add(endTurn)

        finalChoices.Add(New SemanticResultKey("action", "click"))
        finalChoices.Add(New SemanticResultKey("action", "cancel"))

        Try
            Return New Grammar(New GrammarBuilder(finalChoices))
        Catch ex As Exception
            writeLog("Exception when building grammar: " & ex.Message)
            Return New Grammar(New GrammarBuilder("GRAMMAR ERROR"))
        End Try

        Return Nothing

    End Function 'Builds and returns grammar for the speech recognition engine
    Public Sub RefreshGameData()

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
    End Sub 'Rebuilds data for cards in hand and on board


    'Speech recognition
    Public Sub updateRecognizer(Optional e = Nothing)
        hsRecog.RequestRecognizerUpdate()
    End Sub ' Request the SpeechRecognitionEngine update asynchronously
    Public Sub onSpeechRecognized(sender As Object, e As SpeechRecognizedEventArgs) Handles hsRecog.SpeechRecognized
        If Not Core.Game.IsRunning Then
            Return
        End If

        'If hearthstone is inactive, exit
        If Not IsHearthstoneActive() Then
            writeLog("Heard command """ & e.Result.Text & """ but Hearthstone was inactive")
            Return
        End If

        'If below preset confidence threshold then exit
        If e.Result.Confidence < My.Settings.Threshold / 100 Then
            writeLog("Heard command """ & e.Result.Text & """ but it was below the recognition threshold")
            Return
        End If

        If sreListen Then
            updateStatusText("Heard """ & e.Result.Text & """")

            ' Speech was recognized, play audio
            If My.Settings.playAudio Then _
                My.Computer.Audio.Play(My.Resources.sound, AudioPlayMode.Background)

            ' Add command to action list for processing
            actionList.Add(e)
        End If

    End Sub ' Handles processing recognized speech input
    Public Sub onRecognizerUpdateReached(sender As Object, e As RecognizerUpdateReachedEventArgs) Handles hsRecog.RecognizerUpdateReached
        If Not Core.Game.IsRunning Then Exit Sub ' do nothing if the game is not running
        updateInProgress = True
        hsRecog.UnloadAllGrammars()
        hsRecog.LoadGrammar(BuildGrammar)
        updateInProgress = False
    End Sub ' Handles updating the grammar between commands
    Public Sub onSpeechRecognitionRejected() Handles hsRecog.SpeechRecognitionRejected
        ' If recognition fails, refresh Grammar
        updateRecognizer()
    End Sub
    Public Sub hotkeyWorker_DoWork() Handles hotkeyWorker.DoWork

        Do
            Dim toggleHotkey = Keys.F12
            Dim pttHotkey = Keys.LShiftKey

            If My.Settings.toggleOrPTT Then ' Push-to-talk
                Dim hotkeyState As Short = GetAsyncKeyState(pttHotkey)
                If hotkeyState <> 0 Then
                    sreListen = True
                    updateStatusText("Listening...")
                    Do While hotkeyState <> 0
                        Sleep(1)
                        hotkeyState = GetAsyncKeyState(pttHotkey)
                    Loop
                    updateStatusText("Processing...")
                    Do While hsRecog.AudioState = AudioState.Speech
                        Sleep(1)
                    Loop
                    Sleep(500)
                    sreListen = False
                    updateStatusText(Nothing)
                End If
            Else ' Toggle
                Dim hotkeyState As Short = GetAsyncKeyState(toggleHotkey)
                If hotkeyState <> 0 Then
                    sreListen = Not sreListen
                    updateStatusText(Nothing)
                    Sleep(200)
                End If
            End If

            Sleep(50)
        Loop

    End Sub ' Listens for hotkey and toggles/enables PTT

    'Action processor
    Public Sub actionWorker_DoWork() Handles actionWorker.DoWork
        Do
            Do While updateInProgress
                Sleep(1)
            Loop
            RefreshGameData()
            If actionList.Count > 0 Then
                writeLog("Processing action from action list: " & actionList.Item(0).Result.Text & " (remaining actions: " & actionList.Count.ToString & ")")
                ProcessAction(actionList.Item(0))
                actionList.Remove(actionList.Item(0))
            End If
            Sleep(100)
        Loop
    End Sub ' A background loop that continuously processes the actions in the action list
    Public Sub ProcessAction(e As SpeechRecognizedEventArgs)
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
        If e.Result.Semantics.ContainsKey("menu") And Core.Game.IsInMenu Then
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
        updateRecognizer()
        Sleep(100)

    End Sub

    'Game event handlers
    Public Sub onNewGame()
        writeLog("New Game detected")
        ' Initialize controller IDs
        playerID = Nothing
        opponentID = Nothing
        mulliganDone = False

        If Not IsNothing(PlayerEntity) Then
            playerID = PlayerEntity.GetTag(GAME_TAG.CONTROLLER)
            writeLog("Updated player ID to {0}", playerID)
        End If

        If Not IsNothing(OpponentEntity) Then
            opponentID = OpponentEntity.GetTag(GAME_TAG.CONTROLLER)
            writeLog("Updated opponent ID to {0}", opponentID)
        End If

        GrammarEngine.InitializeGame()
    End Sub ' Runs when a new game is started
    Public Sub onMulligan(Optional c As Card = Nothing)
        mulliganDone = True
    End Sub

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
            Case "casual mode"
                moveCursor(75, 20)
                sendLeftClick()
            Case "ranked mode"
                moveCursor(85, 20)
                sendLeftClick()
            Case "basic decks"
                moveCursor(23, 90)
                sendLeftClick()
            Case "custom decks"
                moveCursor(45, 90)
                sendLeftClick()
            Case "start game"
                moveCursor(80, 85)
                sendLeftClick()

            Case "solo"
                moveCursor(50, 38)
                sendLeftClick()

            Case "versus mage"
                moveCursor(82, 12)
                sendLeftClick()

            Case "versus hunter"
                moveCursor(82, 18)
                sendLeftClick()

            Case "versus warrior"
                moveCursor(82, 24)
                sendLeftClick()

            Case "versus shaman"
                moveCursor(82, 30)
                sendLeftClick()

            Case "versus druid"
                moveCursor(82, 36)
                sendLeftClick()

            Case "versus priest"
                moveCursor(82, 42)
                sendLeftClick()

            Case "versus rogue"
                moveCursor(82, 48)
                sendLeftClick()

            Case "versus paladin"
                moveCursor(82, 54)
                sendLeftClick()

            Case "versus warlock"
                moveCursor(82, 60)
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

            Case "quest log"
                moveCursor(21, 87)
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
        If handCards.Count = 0 Then Exit Sub

        Dim myCard = e.Result.Semantics("card").Value
        Dim cardType = handCards.First(Function(x) x.Id = myCard).Card.Type

        If e.Result.Semantics.ContainsKey("friendly") Then 'Play card to friendly target
            Dim destTarget = e.Result.Semantics("friendly").Value
            If cardType = "Minion" Then 'Card is a minion
                dragTargetToTarget(myCard, destTarget, -5) 'Play to the left of friendly target
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
            If Core.Game.OpponentHasCoin Then
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
    Public Sub dragTargetToTarget(startEntity As Integer, endEntity As String, Optional endXOffset As Integer = 0)
        moveCursorToEntity(startEntity)
        startDrag()
        moveCursorToEntity(endEntity, endXOffset)
        endDrag()
    End Sub
    Public Sub moveCursorToEntity(EntityID As Integer, Optional xOffset As Integer = 0)
        Dim targetEntity As Entity

        Try 'Try and find the entity in the game
            targetEntity = Entities.First(Function(x) x.Id = EntityID)
        Catch ex As Exception
            Return
        End Try

        'First, check if the entity is a card in our hand
        If targetEntity.IsInHand And targetEntity.GetTag(GAME_TAG.CONTROLLER) = playerID Then
            Dim cardNum As Integer = targetEntity.GetTag(GAME_TAG.ZONE_POSITION)
            Dim totalCards As Integer = handCards.Count
            Dim handwidth = If(totalCards < 4, 10 * totalCards, 38)

            Dim x = (((cardNum) / (totalCards)) * handwidth) - (handwidth / 2) - 0.8 + xOffset
            Dim y = (x * -0.16) ^ 2
            moveCursor(x + 44, y + 89)
            Return
        End If

        'Next, check whether it is a friendly minion
        If targetEntity.IsInPlay And targetEntity.IsMinion And targetEntity.GetTag(GAME_TAG.CONTROLLER) = playerID Then
            Dim minionNum As Integer = targetEntity.GetTag(GAME_TAG.ZONE_POSITION)
            Dim totalMinions As Integer = boardFriendly.Count
            Dim totalWidth = totalMinions * 10
            Dim minionX = minionNum * 10

            Dim minX = 50 - (totalWidth / 2) + minionX - 7 + xOffset
            moveCursor(minX, 55)
            Return
        End If

        'Then, check whether it is an opposing minion
        If targetEntity.IsInPlay And targetEntity.IsMinion And targetEntity.GetTag(GAME_TAG.CONTROLLER) = opponentID Then
            Dim minionNum As Integer = targetEntity.GetTag(GAME_TAG.ZONE_POSITION)
            Dim totalMinions As Integer = boardOpposing.Count
            Dim totalWidth = totalMinions * 10
            Dim minionX = minionNum * 10

            Dim minX = 50 - (totalWidth / 2) + minionX - 5 + xOffset
            moveCursor(minX, 40)
            Return

        End If

        'Finally, check whether it is a hero
        If targetEntity.IsPlayer Then
            moveCursor(50, 75)
            Return
        End If

        If targetEntity.IsOpponent Then
            moveCursor(50, 20)
            Return
        End If

        Return 'failed to locate entity on the board

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

        Dim endX As Integer = (xPercent / 100) * uiWidth + xOffset + rectHS.Left
        Dim endY As Integer = (yPercent / 100) * uiHeight + rectHS.Top + 8
        Dim startY As Integer = Cursor.Position.Y
        Dim startX As Integer = Cursor.Position.X

        Dim duration = 50

        If My.Settings.smoothCursor Then
            ' Do smooth cursor movement
            Dim cursorX As Integer = Cursor.Position.X
            Dim cursorY As Integer = Cursor.Position.Y

            Dim distX = endX - cursorX
            Dim distY = endY - cursorY

            Dim distZ As Integer = Math.Sqrt(distX ^ 2 + distY ^ 2) 'a^2+b^2=c^2 - the distance the mouse will move

            Dim durationMod As Double = If(distZ < 500, 1, 0.5)

            duration *= durationMod

            For i = 1 To duration ' Interpolate over duration
                cursorX = startX + distX * (i / duration)
                cursorY = startY + distY * (i / duration)
                Cursor.Position = New Point(cursorX, cursorY)
                Sleep(1)
            Next

        Else
            ' Move cursor immediately to target
            Cursor.Position = New Point(endX, endY)
        End If

        Sleep(100)
    End Sub

    'Miscellaneous functions
    Public Function IsHearthstoneActive() As Boolean
        Dim activeHwnd = GetForegroundWindow()
        Dim activeWindowText As New System.Text.StringBuilder(32)
        GetWindowText(activeHwnd, activeWindowText, activeWindowText.Capacity)
        If activeWindowText.ToString = "Hearthstone" Then
            Return True
        Else
            Return False
        End If
    End Function 'Checks if the hearthstone window is active
    Public Function updateOverlay() As System.Windows.SizeChangedEventHandler
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
            Return
        End If
        Try
            hdtStatus.Dispatcher.Invoke(Sub()
                                            Dim newStatus = "HDT-Voice: "
                                            newStatus &= Status
                                            If My.Settings.showLast Then
                                                newStatus &= vbNewLine & "Last executed: " & lastCommand
                                            End If
                                            hdtStatus.Text = newStatus
                                            timerReset.Enabled = False  ' Reset interval
                                            timerReset.Enabled = True

                                        End Sub)
            overlayCanvas.UpdateLayout()
        Catch ex As Exception
            Return
        End Try

    End Sub 'Updates the text on the status text block
    Public Sub writeLog(LogLine As String, ParamArray args As Object())
        Dim formatLine As String = String.Format(LogLine, args)
        formatLine = String.Format("HDT-Voice: {0}", formatLine)
        Debug.WriteLine(formatLine)
        If My.Settings.outputDebug Then
            If IsNothing(voiceLog) Then
                voiceLog = New IO.StreamWriter("hdtvoicelog.txt")
            End If
            voiceLog.WriteLine(formatLine)
            voiceLog.Flush()
        Else
            voiceLog = Nothing
        End If
    End Sub 'Writes information to the debug output and the logfile if necessary
    Public Sub onResetTimer() Handles timerReset.Tick
        If sreListen = True Then
            updateStatusText("Listening...")
        Else
            updateStatusText("Stopped")
        End If

        timerReset.Enabled = False
    End Sub ' Resets the status text to default after a period
End Class
