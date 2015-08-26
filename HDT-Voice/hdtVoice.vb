Imports System.Speech.Recognition
Imports System.Threading.Thread
Imports System.Windows.Controls
Imports System.Windows.Forms
Imports System.ComponentModel
Imports System.Drawing

Imports Hearthstone_Deck_Tracker
Imports Hearthstone_Deck_Tracker.API
Imports Hearthstone_Deck_Tracker.Enums
Imports Hearthstone_Deck_Tracker.Hearthstone
Imports Hearthstone_Deck_Tracker.Hearthstone.Entities

Public Class HDTVoice
    ' Windows API Declarations
    Public Declare Function GetForegroundWindow Lib "user32" () As System.IntPtr
    Public Declare Auto Function GetWindowText Lib "user32" (ByVal hWnd As System.IntPtr, ByVal lpString As System.Text.StringBuilder, ByVal cch As Integer) As Integer
    Declare Function GetAsyncKeyState Lib "user32" (ByVal vkey As Integer) As Short

    ' Speech recognition objects
    Public WithEvents recogVoice As SpeechRecognitionEngine
    Public WithEvents workerHotkey As New BackgroundWorker
    Public sreListen As Boolean                             ' Should we be listening?
    Public boolUpdating As Boolean                          ' True when SRE update in progress, FALSE otherwise

    ' Action processor list and worker
    Public listActions As New List(Of SpeechRecognizedEventArgs)
    Public WithEvents workerActions As New BackgroundWorker


    'HDT-Voice data objects
    Public swDebugLog As IO.StreamWriter                    ' Debug log writer
    Public strLastCommand As New String("none")             ' Last command executed
    Public WithEvents timerReset As New Timer               ' Used to reset status text

    'Overlay elements
    Public canvasOverlay As Canvas = Core.OverlayCanvas     ' The main overlay object
    Public textStatus As HearthstoneTextBlock               ' Status text block


    Public intPlayerID As Integer = 0
    Public intOpponentID As Integer = 0

    Public Shared GrammarEngine As New GrammarEngine
    Public Mouse As New Mouse

    'Properties
    Private ReadOnly Property Entities As Entity()
        Get
            ' Clone entities from game and return as array
            Try
                Dim EntArray = Helper.DeepClone(Core.Game.Entities).Values.ToArray
                Return EntArray
            Catch ex As Exception
                Return Nothing
            End Try

        End Get
    End Property          ' The list of entities for the current game
    Private ReadOnly Property PlayerEntity As Entity
        Get
            Return Entities.FirstOrDefault(Function(x) x.IsPlayer())
        End Get
    End Property        ' The player's entity
    Private ReadOnly Property OpponentEntity As Entity
        Get
            ' Return the Entity representing the player
            Return Entities.FirstOrDefault(Function(x) x.IsOpponent())
        End Get
    End Property      ' The opponent entity

    'Main functions
    Public Sub Load()
        ' Run when the plugin is initialized by HDT

        ' Write basic system information to logfile

        writeLog("HDT-Voice {0}.{1} ({2}) | {3}x{4}", My.Application.Info.Version.Major, My.Application.Info.Version.Minor, My.Computer.Info.OSFullName, Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height)
        writeLog("Initializing speech recognition object")

        ' Initialize output displays
        textStatus = New HearthstoneTextBlock
        textStatus.Text = "HDT-Voice: Loading, please wait..."
        textStatus.FontSize = 16
        Canvas.SetTop(textStatus, 22)
        Canvas.SetLeft(textStatus, 4)
        canvasOverlay.Children.Add(textStatus)

        ' Attempt to initialize speech recognition
        Try

            Dim rec As IReadOnlyCollection(Of RecognizerInfo) = SpeechRecognitionEngine.InstalledRecognizers
            If rec.Count > 0 Then
                writeLog("Installed recognizers: {0}", rec.Count)
                For Each e In rec
                    writeLog(e.Culture.Name)
                Next
                writeLog("Initializing recognizer for: {0}", rec.First.Culture.Name)
                recogVoice = New SpeechRecognitionEngine(rec.First.Culture)
            Else
                writeLog("No speech recognizer found. Attempting to start engine anyway.")
                recogVoice = New SpeechRecognitionEngine()
            End If

            recogVoice.SetInputToDefaultAudioDevice()
            recogVoice.BabbleTimeout = New TimeSpan(0, 0, 3)
            recogVoice.InitialSilenceTimeout = New TimeSpan(0, 0, 3)
            recogVoice.EndSilenceTimeout = New TimeSpan(0, 0, 0, 0, 500)
            recogVoice.EndSilenceTimeoutAmbiguous = New TimeSpan(0, 0, 0, 0, 500)
            writeLog("Successfuly started speech recognition")
        Catch ex As Exception
            writeLog("Error initializing speech recognition: {0}", ex.Message)
            MsgBox("An error occurred initializing speech recognition: " & vbNewLine & ex.Message, vbOKOnly + vbCritical, "HDT-Voice")
        End Try

        'Add event handlers
        GameEvents.OnGameStart.Add(New Action(AddressOf onNewGame))

        'Add handlers to reload grammar when needed
        GameEvents.OnGameStart.Add(New Action(AddressOf updateRecognizer))
        GameEvents.OnTurnStart.Add(New Action(Of ActivePlayer)(AddressOf updateRecognizer))

        GameEvents.OnInMenu.Add(New Action(AddressOf updateRecognizer))
        GameEvents.OnPlayerDraw.Add(New Action(Of Card)(AddressOf updateRecognizer))
        GameEvents.OnPlayerGet.Add(New Action(Of Card)(AddressOf updateRecognizer))
        GameEvents.OnPlayerHandDiscard.Add(New Action(Of Card)(AddressOf updateRecognizer))
        GameEvents.OnPlayerHeroPower.Add(New Action(AddressOf updateRecognizer))
        GameEvents.OnPlayerPlay.Add(New Action(Of Card)(AddressOf updateRecognizer))
        GameEvents.OnPlayerPlayToHand.Add(New Action(Of Card)(AddressOf updateRecognizer))
        GameEvents.OnOpponentPlay.Add(New Action(Of Card)(AddressOf updateRecognizer))

        'Handlers for plugin settings and overlay size
        AddHandler My.Settings.PropertyChanged, AddressOf updateOverlay
        AddHandler Core.OverlayCanvas.SizeChanged, AddressOf updateOverlay

        timerReset.Interval = 2500
        timerReset.Enabled = True

        'Load default grammar and start recognition
        recogVoice.LoadGrammar(New Grammar(New GrammarBuilder("default")))
        recogVoice.RecognizeAsync(RecognizeMode.Multiple)

        workerHotkey.RunWorkerAsync()       ' Start listening for hotkey
        workerActions.RunWorkerAsync()      ' Start action processor

        ' Start listening if the option is enabled
        If My.Settings.autoListen And Not My.Settings.toggleOrPTT Then sreListen = True

    End Sub ' Run when the plugin is first initialized
    Public Function BuildGrammar() As Grammar

        If Core.Game.IsInMenu Then
            writeLog("Menu grammar active")
            Return New Grammar(GrammarEngine.BuildMenuGrammar)
        End If

        ' if the player or opponent entity is unknown, try initiate a new game
        If intPlayerID = 0 Or intOpponentID = 0 Then
            onNewGame()
        End If

        ' Check if we're at the mulligan, if so only the mulligan grammar will be returned
        If Not Core.Game.IsMulliganDone Then
            writeLog("Building mulligan Grammar...")

            Dim mg = GrammarEngine.BuildMulliganGrammar
            Return New Grammar(mg)
        End If


        Dim grammarGame = GrammarEngine.GameGrammar

        Try
            Return New Grammar(grammarGame)
        Catch ex As Exception
            writeLog("Exception when building grammar: " & ex.Message)
            Return New Grammar(New GrammarBuilder("GRAMMAR ERROR"))
        End Try

        Return Nothing

    End Function 'Builds and returns grammar for the speech recognition engine
    Public Sub onNewGame()
        writeLog("New Game detected")
        ' Reset controller IDs
        intPlayerID = Nothing
        intOpponentID = Nothing

        ' Update player and opponent entities
        If Not IsNothing(PlayerEntity) Then
            intPlayerID = PlayerEntity.GetTag(GAME_TAG.CONTROLLER)
            writeLog("Updated player ID to {0}", intPlayerID)
        End If

        If Not IsNothing(OpponentEntity) Then
            intOpponentID = OpponentEntity.GetTag(GAME_TAG.CONTROLLER)
            writeLog("Updated opponent ID to {0}", intOpponentID)
        End If


        GrammarEngine.StartNewGame()    ' Initialize GrammarEngine
        updateRecognizer()              ' Update recognizer
    End Sub ' Runs when a new game is started

    'Speech recognition
    Public Sub updateRecognizer(Optional e = Nothing)
        recogVoice.RequestRecognizerUpdate()
    End Sub ' Request the SpeechRecognitionEngine update asynchronously
    Public Sub onSpeechRecognized(sender As Object, e As SpeechRecognizedEventArgs) Handles recogVoice.SpeechRecognized
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
            listActions.Add(e)
        End If

    End Sub ' Handles processing recognized speech input
    Public Sub onRecognizerUpdateReached(sender As Object, e As RecognizerUpdateReachedEventArgs) Handles recogVoice.RecognizerUpdateReached
        If Not Core.Game.IsRunning Then Exit Sub ' do nothing if the game is not running
        boolUpdating = True
        recogVoice.UnloadAllGrammars()
        recogVoice.LoadGrammar(BuildGrammar)
        boolUpdating = False
    End Sub ' Handles updating the grammar between commands
    Public Sub onSpeechRecognitionRejected() Handles recogVoice.SpeechRecognitionRejected
        ' If recognition fails, refresh Grammar
        updateRecognizer()
    End Sub
    Public Sub hotkeyWorker_DoWork() Handles workerHotkey.DoWork

        Do
            Dim toggleHotkey = Keys.F12
            Dim pttHotkey = Keys.LShiftKey

            If My.Settings.toggleOrPTT Then ' Push-to-talk
                Dim hotkeyState As Short = GetAsyncKeyState(pttHotkey)
                If hotkeyState <> 0 Then
                    sreListen = True
                    updateStatusText("Listening...")
                    Do While hotkeyState <> 0
                        Sleep(5)
                        hotkeyState = GetAsyncKeyState(pttHotkey)
                    Loop
                    updateStatusText("Processing...")
                    Do While recogVoice.AudioState = AudioState.Speech
                        Sleep(5)
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
    Public Sub actionWorker_DoWork() Handles workerActions.DoWork
        Do
            Do While boolUpdating
                Sleep(10)            ' Wait if recognizer is updating
            Loop
            If listActions.Count > 0 Then
                Dim currentAction As String = listActions.Item(0).Result.Text
                writeLog("Processing action: ""{0}""", currentAction.ToUpper)
                ProcessAction(listActions.Item(0))       ' Process action
                listActions.Remove(listActions.Item(0))   ' Remove from list
                Sleep(100)
                updateRecognizer()                      ' Update recognizer
                Sleep(500)
            End If
            Sleep(50)
        Loop
    End Sub ' A background worker that loops, continuously processing any actions in the action list
    Public Sub ProcessAction(e As SpeechRecognizedEventArgs)
        ' First, check for debugger only commands

        ' Check if the action is a menu action, and if we're in the menu
        If e.Result.Semantics.ContainsKey("menu") And Core.Game.IsInMenu Then
            doMenu(e)
        End If

        ' Check if the action is a game action and invoke the appropriate subroutine to handle it
        If e.Result.Semantics.ContainsKey("action") Then
            Select Case e.Result.Semantics("action").Value
                Case "mulligan"     ' Handle mulligan stage commands
                    doMulligan(e)

                Case "hero"         ' Use hero power
                    doHero(e)

                Case "play"         ' Play a card from hand
                    doPlay(e)

                Case "attack"       ' Attack with a minion
                    doAttack(e)

                Case "click"        ' Send a click to an entity
                    doClick(e)

                Case "target"       ' Hover the cursor over a target
                    doTarget(e)

                Case "say"          ' Do an emote
                    doSay(e)

                Case "choose"       ' Choose an option (x of y)
                    doChoose(e)

                Case "cancel"       ' Send right click
                    Mouse.SendClick(Mouse.Buttons.Right)

                Case "end"          ' Click End Turn
                    Mouse.MoveTo(91, 46)
                    Mouse.SendClick(Mouse.Buttons.Left)
            End Select
        End If

        strLastCommand = e.Result.Text ' Set last command executed
    End Sub

    'Voice command handlers
    Private Sub doChoose(e As SpeechRecognizedEventArgs)
        Dim optNum = e.Result.Semantics("option").Value
        Dim optmax = e.Result.Semantics("max").Value
        Mouse.MoveToOption(optNum, optmax)
    End Sub 'handles selecting an option
    Private Sub doSay(e As SpeechRecognizedEventArgs)
        Dim emote = e.Result.Semantics("emote").Value
        Select Case emote
            Case "thanks"
                Mouse.MoveTo(50, 75)
                Mouse.SendClick(Mouse.Buttons.Right)
                Sleep(200)
                Mouse.MoveTo(40, 64)
                Mouse.SendClick(Mouse.Buttons.Left)
            Case "well played"
                Mouse.MoveTo(50, 75)
                Mouse.SendClick(Mouse.Buttons.Right)
                Sleep(200)
                Mouse.MoveTo(40, 72)
                Mouse.SendClick(Mouse.Buttons.Left)
            Case "greetings"
                Mouse.MoveTo(50, 75)
                Mouse.SendClick(Mouse.Buttons.Right)
                Sleep(200)
                Mouse.MoveTo(40, 80)
                Mouse.SendClick(Mouse.Buttons.Left)
            Case "sorry"
                Mouse.MoveTo(50, 75)
                Mouse.SendClick(Mouse.Buttons.Right)
                Sleep(200)
                Mouse.MoveTo(60, 64)
                Mouse.SendClick(Mouse.Buttons.Left)
            Case "oops"
                Mouse.MoveTo(50, 75)
                Mouse.SendClick(Mouse.Buttons.Right)
                Sleep(200)
                Mouse.MoveTo(60, 72)
                Mouse.SendClick(Mouse.Buttons.Left)
            Case "threaten"
                Mouse.MoveTo(50, 75)
                Mouse.SendClick(Mouse.Buttons.Right)
                Sleep(200)
                Mouse.MoveTo(60, 80)
                Mouse.SendClick(Mouse.Buttons.Left)
        End Select
    End Sub 'handles emotes
    Private Sub doClick(e As SpeechRecognizedEventArgs)
        If e.Result.Semantics.ContainsKey("heropower") Then 'target hero or hero power
            Dim x, y
            If e.Result.Semantics("herotarget").Value = "friendly" Then y = 80 Else y = 20
            If e.Result.Semantics("heropower").Value = "hero" Then x = 50 Else x = 60
            Mouse.MoveTo(x, y)
        End If
        If e.Result.Semantics.ContainsKey("card") Then
            Dim targetName = e.Result.Semantics("card").Value
            Mouse.MoveToEntity(targetName)
        End If
        If e.Result.Semantics.ContainsKey("friendly") Then
            Dim targetName = e.Result.Semantics("friendly").Value
            Mouse.MoveToEntity(targetName)
        End If
        If e.Result.Semantics.ContainsKey("opposing") Then
            Dim targetName = e.Result.Semantics("opposing").Value
            Mouse.MoveToEntity(targetName)
        End If
        Mouse.SendClick(Mouse.Buttons.Left)
    End Sub 'handle clicking mouse
    Private Sub doMenu(e As SpeechRecognizedEventArgs)
        Select Case e.Result.Semantics("menu").Value
            Case "play"
                Mouse.MoveTo(50, 31)
                Mouse.SendClick(Mouse.Buttons.Left)
            Case "casual mode"
                Mouse.MoveTo(75, 20)
                Mouse.SendClick(Mouse.Buttons.Left)
            Case "ranked mode"
                Mouse.MoveTo(85, 20)
                Mouse.SendClick(Mouse.Buttons.Left)
            Case "basic decks"
                Mouse.MoveTo(23, 90)
                Mouse.SendClick(Mouse.Buttons.Left)
            Case "custom decks"
                Mouse.MoveTo(45, 90)
                Mouse.SendClick(Mouse.Buttons.Left)
            Case "start game"
                Mouse.MoveTo(80, 85)
                Mouse.SendClick(Mouse.Buttons.Left)

            Case "solo"
                Mouse.MoveTo(50, 38)
                Mouse.SendClick(Mouse.Buttons.Left)

            Case "versus mage"
                Mouse.MoveTo(82, 12)
                Mouse.SendClick(Mouse.Buttons.Left)

            Case "versus hunter"
                Mouse.MoveTo(82, 18)
                Mouse.SendClick(Mouse.Buttons.Left)

            Case "versus warrior"
                Mouse.MoveTo(82, 24)
                Mouse.SendClick(Mouse.Buttons.Left)

            Case "versus shaman"
                Mouse.MoveTo(82, 30)
                Mouse.SendClick(Mouse.Buttons.Left)

            Case "versus druid"
                Mouse.MoveTo(82, 36)
                Mouse.SendClick(Mouse.Buttons.Left)

            Case "versus priest"
                Mouse.MoveTo(82, 42)
                Mouse.SendClick(Mouse.Buttons.Left)

            Case "versus rogue"
                Mouse.MoveTo(82, 48)
                Mouse.SendClick(Mouse.Buttons.Left)

            Case "versus paladin"
                Mouse.MoveTo(82, 54)
                Mouse.SendClick(Mouse.Buttons.Left)

            Case "versus warlock"
                Mouse.MoveTo(82, 60)
                Mouse.SendClick(Mouse.Buttons.Left)


            'arena commands
            Case "arena"
                Mouse.MoveTo(50, 45)
                Mouse.SendClick(Mouse.Buttons.Left)
            Case "buy arena with gold"
                Mouse.MoveTo(60, 62)
                Mouse.SendClick(Mouse.Buttons.Left)
            Case "cancel arena"
                Mouse.MoveTo(50, 75)
                Mouse.SendClick(Mouse.Buttons.Left)
            Case "start arena"
                Mouse.MoveTo(60, 75)
                Mouse.SendClick(Mouse.Buttons.Left)
            Case "hero 1"
                Mouse.MoveTo(20, 40)
                Mouse.SendClick(Mouse.Buttons.Left)
            Case "hero 2"
                Mouse.MoveTo(40, 40)
                Mouse.SendClick(Mouse.Buttons.Left)
            Case "hero 3"
                Mouse.MoveTo(55, 40)
                Mouse.SendClick(Mouse.Buttons.Left)
            Case "card 1"
                Mouse.MoveTo(20, 40)
                Mouse.SendClick(Mouse.Buttons.Left)
            Case "card 2"
                Mouse.MoveTo(40, 40)
                Mouse.SendClick(Mouse.Buttons.Left)
            Case "card 3"
                Mouse.MoveTo(55, 40)
                Mouse.SendClick(Mouse.Buttons.Left)
            Case "confirm"
                Mouse.MoveTo(50, 45)
                Mouse.SendClick(Mouse.Buttons.Left)

            Case "brawl"
                Mouse.MoveTo(50, 52)
                Mouse.SendClick(Mouse.Buttons.Left)
            Case "start brawl"
                Mouse.MoveTo(65, 85)
                Mouse.SendClick(Mouse.Buttons.Left)


            Case "open packs"
                Mouse.MoveTo(40, 85)
                Mouse.SendClick(Mouse.Buttons.Left)
            Case "open top pack"
                Mouse.MoveTo(12, 20)
                Mouse.StartDrag()
                Mouse.MoveTo(59, 49)
                Mouse.EndDrag()
            Case "open bottom pack"
                Mouse.MoveTo(12, 50)
                Mouse.StartDrag()
                Mouse.MoveTo(59, 49)
                Mouse.EndDrag()
            Case "open card 1"
                Mouse.MoveTo(60, 35)
                Mouse.SendClick(Mouse.Buttons.Left)
            Case "open card 2"
                Mouse.MoveTo(80, 35)
                Mouse.SendClick(Mouse.Buttons.Left)
            Case "open card 3"
                Mouse.MoveTo(70, 70)
                Mouse.SendClick(Mouse.Buttons.Left)
            Case "open card 4"
                Mouse.MoveTo(50, 70)
                Mouse.SendClick(Mouse.Buttons.Left)
            Case "open card 5"
                Mouse.MoveTo(40, 35)
                Mouse.SendClick(Mouse.Buttons.Left)
            Case "done"
                Mouse.MoveTo(60, 50)
                Mouse.SendClick(Mouse.Buttons.Left)

            Case "quest log"
                Mouse.MoveTo(21, 87)
                Mouse.SendClick(Mouse.Buttons.Left)

            Case "cancel"
                Mouse.MoveTo(52, 85)
                Mouse.SendClick(Mouse.Buttons.Left)
            Case "back"
                Mouse.MoveTo(92, 91)
                Mouse.SendClick(Mouse.Buttons.Left)

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
                Mouse.MoveTo(deckX, deckY)
                Mouse.SendClick(Mouse.Buttons.Left)
        End Select
    End Sub ' handle menu commands
    Private Sub doHero(e As SpeechRecognizedEventArgs)
        If e.Result.Semantics.ContainsKey("friendly") Then
            Dim friendlyID = e.Result.Semantics("friendly").Value
            Mouse.MoveTo(62, 76)
            Mouse.StartDrag()
            Mouse.MoveToEntity(friendlyID)
            Mouse.EndDrag()
        ElseIf e.Result.Semantics.ContainsKey("opposing") Then
            Dim opposingID = e.Result.Semantics("opposing").Value
            Mouse.MoveTo(62, 76)
            Mouse.StartDrag()
            Mouse.MoveToEntity(opposingID)
            Mouse.EndDrag()
        Else
            Mouse.MoveTo(62, 76)
            Mouse.SendClick(Mouse.Buttons.Left)
        End If
    End Sub 'handle hero powers
    Private Sub doAttack(e As SpeechRecognizedEventArgs)

        If e.Result.Semantics.ContainsKey("opposing") Then ' Target is a minion
            Dim myMinion = e.Result.Semantics("friendly").Value
            Dim targetMinion = e.Result.Semantics("opposing").Value
            Mouse.MoveToEntity(myMinion)
            Mouse.StartDrag()
            Mouse.MoveToEntity(targetMinion)
            Mouse.EndDrag()
        Else ' Not a minion, attack face
            Dim myMinion = e.Result.Semantics("friendly").Value
            Mouse.MoveToEntity(myMinion)
            Mouse.StartDrag()
            Mouse.MoveTo(50, 20)
            Mouse.EndDrag()
        End If
    End Sub 'handle attacking
    Private Sub doPlay(e As SpeechRecognizedEventArgs)

        Dim myCard = e.Result.Semantics("card").Value
        Dim cardType = GrammarEngine.GetEntityFromSemantic(myCard).Card.Type

        If e.Result.Semantics.ContainsKey("friendly") Then 'Play card to friendly target
            Dim destTarget = e.Result.Semantics("friendly").Value
            If cardType = "Minion" Then 'Card is a minion
                Mouse.DragToTarget(myCard, destTarget, -5) 'Play to the left of friendly target
            Else 'Card is a spell
                Mouse.DragToTarget(myCard, destTarget) 'Direct drag to target
            End If


        ElseIf e.Result.Semantics.ContainsKey("opposing") Then 'Play card to opposing target
            Dim targetName = e.Result.Semantics("opposing").Value
            Mouse.DragToTarget(myCard, targetName)
        Else 'Play card with no target
            If cardType = "Minion" Then
                Mouse.MoveToEntity(myCard)
                Mouse.StartDrag()
                Mouse.MoveTo(85, 55) 'play to right of board
                Mouse.EndDrag()
            Else
                Mouse.MoveToEntity(myCard)
                Mouse.StartDrag()
                Mouse.MoveTo(40, 75) 'play to board
                Mouse.EndDrag()
            End If

        End If
    End Sub
    Private Sub doMulligan(e As SpeechRecognizedEventArgs)
        If e.Result.Semantics.ContainsKey("card") Then
            Dim semanticValue = e.Result.Semantics("card").Value
            Dim targetNum = GrammarEngine.GetEntityFromSemantic(semanticValue).GetTag(GAME_TAG.ZONE_POSITION)
            Mouse.MoveToMulligan(targetNum)
            Mouse.SendClick(Mouse.Buttons.Left)
        Else
            Mouse.MoveTo(50, 80)
            Mouse.SendClick(Mouse.Buttons.Left)
        End If
    End Sub 'handle mulligan
    Private Sub doTarget(e As SpeechRecognizedEventArgs)
        If e.Result.Semantics.ContainsKey("heropower") Then 'target hero or hero power
            Dim x, y
            If e.Result.Semantics("herotarget").Value = "friendly" Then y = 80 Else y = 20
            If e.Result.Semantics("heropower").Value = "hero" Then x = 50 Else x = 60
            Mouse.MoveTo(x, y)
        End If
        If e.Result.Semantics.ContainsKey("card") Then 'target card
            Dim targetName = e.Result.Semantics("card").Value
            Mouse.MoveToEntity(targetName)
        End If
        If e.Result.Semantics.ContainsKey("friendly") Then 'target friendly
            Dim targetName = e.Result.Semantics("friendly").Value
            Mouse.MoveToEntity(targetName)
        End If
        If e.Result.Semantics.ContainsKey("opposing") Then 'target opposing
            Dim targetName = e.Result.Semantics("opposing").Value
            Mouse.MoveToEntity(targetName)
        End If
    End Sub 'handle targeting cursor

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
            textStatus.Visibility = System.Windows.Visibility.Hidden
        Else
            textStatus.Visibility = System.Windows.Visibility.Visible
        End If
        Select Case My.Settings.statusTextPos ' position status text
            Case 0 'Top left
                Canvas.SetTop(textStatus, 32)
                Canvas.SetLeft(textStatus, 8)
                textStatus.TextAlignment = System.Windows.TextAlignment.Left
            Case 1 'Bottom left
                Canvas.SetTop(textStatus, canvasOverlay.Height - 64)
                Canvas.SetLeft(textStatus, 8)
                textStatus.TextAlignment = System.Windows.TextAlignment.Left
            Case 2 'Top right
                Canvas.SetTop(textStatus, 8)
                Canvas.SetLeft(textStatus, canvasOverlay.Width - textStatus.ActualWidth - 8)
                textStatus.TextAlignment = System.Windows.TextAlignment.Right
            Case 3 'Bottom right
                Canvas.SetTop(textStatus, canvasOverlay.Height - 64)
                Canvas.SetLeft(textStatus, canvasOverlay.Width - textStatus.ActualWidth - 8)
                textStatus.TextAlignment = System.Windows.TextAlignment.Right
        End Select

        Return Nothing
    End Function 'Handles changing the overlay layout when it is resized
    Public Sub updateStatusText(Status As String)
        If Status = Nothing Then
            onResetTimer()
            Return
        End If
        Try
            textStatus.Dispatcher.Invoke(Sub()
                                             Dim newStatus = "HDT-Voice: "
                                             newStatus &= Status
                                             If My.Settings.showLast Then
                                                 newStatus &= vbNewLine & "Last executed: " & strLastCommand
                                             End If
                                             textStatus.Text = newStatus
                                             timerReset.Enabled = False  ' Reset interval
                                             timerReset.Enabled = True

                                         End Sub)
            canvasOverlay.UpdateLayout()
        Catch ex As Exception
            Return
        End Try

    End Sub 'Updates the text on the status text block
    Public Sub writeLog(LogLine As String, ParamArray args As Object())
        Dim formatLine As String = String.Format(LogLine, args)
        formatLine = String.Format("HDT-Voice: {0}", formatLine)
        Debug.WriteLine(formatLine)
        If My.Settings.outputDebug Then
            If IsNothing(swDebugLog) Then
                swDebugLog = New IO.StreamWriter("hdtvoicelog.txt")
            End If
            swDebugLog.WriteLine(formatLine)
            swDebugLog.Flush()
        Else
            swDebugLog = Nothing
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
