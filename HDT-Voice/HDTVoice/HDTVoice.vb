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
    Private Declare Function GetForegroundWindow Lib "user32" () As System.IntPtr
    Private Declare Auto Function GetWindowText Lib "user32" (ByVal hWnd As System.IntPtr, ByVal lpString As System.Text.StringBuilder, ByVal cch As Integer) As Integer
    Private Declare Function GetAsyncKeyState Lib "user32" (ByVal vkey As Integer) As Short

    ' Speech recognition objects
    Private WithEvents recogVoice As SpeechRecognitionEngine
    Private WithEvents workerHotkey As New BackgroundWorker
    Private sreListen As Boolean                             ' Should we be listening?
    Private boolUpdating As Boolean                          ' True when SRE update in progress, FALSE otherwise

    ' Action processor list and worker
    Private listActions As New List(Of SpeechRecognizedEventArgs)
    Private WithEvents workerActions As New BackgroundWorker


    'HDT-Voice data objects
    Private swDebugLog As IO.StreamWriter                    ' Debug log writer

    'Overlay elements
    Private canvasOverlay As Canvas = Core.OverlayCanvas     ' The main overlay object
    Private rectStatusBG As Rectangle = Nothing

    Private intPlayerID As Integer = 0
    Private intOpponentID As Integer = 0

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

        'Load default grammar and start recognition
        recogVoice.LoadGrammar(GrammarEngine.MenuGrammar)
        recogVoice.RecognizeAsync(RecognizeMode.Multiple)

        workerHotkey.RunWorkerAsync()       ' Start listening for hotkey
        workerActions.RunWorkerAsync()      ' Start action processor

        If My.Settings.boolToggleOrPtt Then ' Push to talk enabled, don't start listening
            sreListen = False
            PopupNotification("HDT-Voice loaded (hold Left Shift to speak)", 4000)
        ElseIf My.Settings.boolListenAtStartup Then ' Start listening
            sreListen = True
            PopupNotification("HDT-Voice loaded and enabled (F12 to disable)", 4000)
        Else 'Don't start listening
            sreListen = False
            PopupNotification("HDT-Voice loaded (F12 to enable)", 4000)
        End If
    End Sub ' Run when the plugin is first initialized
    Public Sub Unload()
        If Not swDebugLog Is Nothing Then
            swDebugLog.Flush()
            swDebugLog.Dispose()
        End If
        recogVoice.RecognizeAsyncCancel()
        recogVoice.Dispose()
    End Sub
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
        If e.Result.Confidence < My.Settings.intThreshold / 100 Then
            writeLog("Heard command """ & e.Result.Text & """ but it was below the recognition threshold")
            Return
        End If

        If sreListen Then

            ' Speech was recognized, play audio
            If My.Settings.boolRecognizedAudio Then _
                My.Computer.Audio.Play(My.Resources.sound, AudioPlayMode.Background)

            ' Add command to action list for processing
            listActions.Add(e)
        End If

    End Sub ' Handles processing recognized speech input
    Public Sub onRecognizerUpdateReached(sender As Object, e As RecognizerUpdateReachedEventArgs) Handles recogVoice.RecognizerUpdateReached
        If Not Core.Game.IsRunning Then
            writeLog("Tried to update recog but game not running!")
            Exit Sub ' do nothing if the game is not running
        End If

        boolUpdating = True
        recogVoice.UnloadAllGrammars()

        If Core.Game.IsInMenu Then
            Dim mG = GrammarEngine.MenuGrammar
            recogVoice.LoadGrammar(mG)
            boolUpdating = False
            Return
        End If

        If intPlayerID = 0 Or intOpponentID = 0 Then
            onNewGame()
        End If

        If Not Core.Game.IsMulliganDone Then

            recogVoice.LoadGrammar(GrammarEngine.MulliganGrammar)
        Else
            recogVoice.LoadGrammar(GrammarEngine.GameGrammar)
        End If

        boolUpdating = False
    End Sub ' Handles updating the grammar between commands
    Public Sub onSpeechRecognitionRejected() Handles recogVoice.SpeechRecognitionRejected
        ' If recognition fails, refresh Grammar
        updateRecognizer()
        PopupNotification("Command not recognized", 3000)
    End Sub
    Public Sub hotkeyWorker_DoWork() Handles workerHotkey.DoWork
        Dim toggleHotkey = Keys.F12
        Dim pttHotkey = Keys.LShiftKey
        Do
            If Not Core.Game.IsRunning Then
                writeLog("Hearthstone not running, stopping recognizer...")
                sreListen = False
                recogVoice.RecognizeAsyncCancel()
                Do Until Core.Game.IsRunning
                    Sleep(1000)
                Loop
                writeLog("Hearthstone started, starting recognizer...")
                updateRecognizer()
                recogVoice.RecognizeAsync(RecognizeMode.Multiple)
                If My.Settings.boolListenAtStartup Then
                    sreListen = True
                End If
            End If
            If IsHearthstoneActive() Then
                If My.Settings.boolToggleOrPtt Then ' Push-to-talk
                    Dim hotkeyState As Short = GetAsyncKeyState(pttHotkey)
                    If hotkeyState <> 0 Then
                        sreListen = True
                        PopupNotification("Listening...", 10000)
                        Do While hotkeyState <> 0
                            Sleep(5)
                            hotkeyState = GetAsyncKeyState(pttHotkey)
                        Loop
                        PopupNotification("Processing...", 800)
                        Sleep(100)
                        Do While recogVoice.AudioState = AudioState.Speech
                            Sleep(5)
                        Loop
                        Sleep(700)
                        sreListen = False
                    End If
                Else ' Toggle

                    Dim hotkeyState As Short = GetAsyncKeyState(toggleHotkey)
                    If hotkeyState <> 0 Then
                        sreListen = Not sreListen
                        Select Case sreListen
                            Case False
                                PopupNotification("Voice control disabled. Press F12 to enable.", 4000)
                            Case True
                                PopupNotification("Voice control enabled. Press F12 to disable.", 4000)
                        End Select
                        Sleep(200)
                    End If
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
                currentAction = currentAction.Substring(0, 1).ToUpper & currentAction.Substring(1)
                writeLog("Processing action: ""{0}""", currentAction)
                PopupNotification(String.Format("""{0}""", currentAction))
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
    Public Sub writeLog(LogLine As String, ParamArray args As Object())
        Dim formatLine As String = String.Format(LogLine, args)
        formatLine = String.Format("HDT-Voice: {0}", formatLine)
        Debug.WriteLine(formatLine)
        If My.Settings.boolDebugLog Then
            If IsNothing(swDebugLog) Then
                swDebugLog = New IO.StreamWriter("hdtvoicelog.txt")
            End If
            swDebugLog.WriteLine(formatLine)
            swDebugLog.Flush()
        Else
            If Not swDebugLog Is Nothing Then
                swDebugLog.Dispose()
            Else
                swDebugLog = Nothing
            End If
        End If
    End Sub 'Writes information to the debug output and the logfile if necessary
    Public Sub PopupNotification(Text As String, Optional Duration As Integer = 2000)
        If My.Settings.boolShowNotification Then
            Dim fadeWorker As New BackgroundWorker
            ' Spawn backgroundworker to avoid blocking thread with popup
            AddHandler fadeWorker.DoWork, Sub()
                                              Dim myPopup As New HDTPopup(Text, Duration)
                                          End Sub
            fadeWorker.RunWorkerAsync()
        End If
    End Sub
End Class
