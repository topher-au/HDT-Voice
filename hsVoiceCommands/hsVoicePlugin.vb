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
Public Class hsVoicePlugin
    ' Windows API Declarations
    Private Declare Function FindWindow Lib "user32" Alias "FindWindowA" (ByVal lpClassName As String, ByVal lpWindowName As String) As IntPtr
    Private Declare Function GetWindowRect Lib "user32" Alias "GetWindowRect" (ByVal hwnd As IntPtr, ByRef lpRect As RECT) As Integer
    Private Declare Sub mouse_event Lib "user32.dll" (ByVal dwFlags As Integer, ByVal dx As Integer, ByVal dy As Integer, ByVal cButtons As Integer, ByVal dwExtraInfo As IntPtr)
    Public Declare Function GetForegroundWindow Lib "user32" () As System.IntPtr
    Public Declare Auto Function GetWindowText Lib "user32" _
(ByVal hWnd As System.IntPtr,
ByVal lpString As System.Text.StringBuilder,
ByVal cch As Integer) As Integer

    Public WithEvents timerResetStatus As Timer
    Public lastCommand As New String("none")

    Const MOUSEEVENTF_LEFTDOWN As UInteger = &H2
    Const MOUSEEVENTF_LEFTUP As UInteger = &H4
    Const MOUSEEVENTF_RIGHTDOWN As UInteger = &H8
    Const MOUSEEVENTF_RIGHTUP As UInteger = &H10
    Structure RECT
        Public Left As Integer
        Public Top As Integer
        Public Right As Integer
        Public Bottom As Integer
    End Structure

    ' Speech recognition objects
    Public WithEvents hsRecog As SpeechRecognitionEngine
    Public WithEvents recogWorker As New BackgroundWorker

    Public hdtStatus As HearthstoneTextBlock

    Public handCards, boardOpponent, boardFriendly As New List(Of Entity)
    Public playerID, opponentID As Integer
    Public mulliganDone As Boolean
    Private ReadOnly Property Entities As Entity()
        Get
            ' Clone entities from game and return as array
            Return Helper.DeepClone(Game.Entities).Values.ToArray
        End Get
    End Property
    Private ReadOnly Property PlayerEntity As Entity
        Get
            ' Return the Entity representing the player
            Try
                Return Entities.First(Function(x) x.IsPlayer())
            Catch ex As Exception
                Return Nothing
            End Try
        End Get
    End Property
    Private ReadOnly Property OpponentEntity As Entity
        Get
            ' Return the Entity representing the player
            Try
                Return Entities.First(Function(x) x.IsOpponent())
            Catch ex As Exception
                Return Nothing
            End Try
        End Get
    End Property
    Public Sub Load()
        ' Initialize speech recogition object
        hsRecog = New SpeechRecognitionEngine
        hsRecog.SetInputToDefaultAudioDevice()
        hsRecog.BabbleTimeout = New TimeSpan(0, 0, 5)
        hsRecog.InitialSilenceTimeout = New TimeSpan(0, 0, 5)

        timerResetStatus = New Timer
        timerResetStatus.Enabled = True
        timerResetStatus.Interval = 2000

        'Initialize values
        playerID = 0
        opponentID = 0

        ' Set up output block
        hdtStatus = New HearthstoneTextBlock
        hdtStatus.Text = "HDT-Voice: Loading, please wait..."
        hdtStatus.FontSize = 16

        Dim overlayCanvas = Overlay.OverlayCanvas

        Canvas.SetTop(hdtStatus, 22)
        Canvas.SetLeft(hdtStatus, 4)

        overlayCanvas.Children.Add(hdtStatus)

        'Add event handlers
        GameEvents.OnGameStart.Add(New Action(AddressOf onNewGame))
        GameEvents.OnPlayerMulligan.Add(New Action(Of Card)(AddressOf onMulligan))
        GameEvents.OnTurnStart.Add(New Action(Of ActivePlayer)(AddressOf rebuildCardData))
        GameEvents.OnPlayerPlay.Add(New Action(Of Card)(AddressOf rebuildCardData))

        recogWorker.RunWorkerAsync()

    End Sub
    Public Sub onNewGame()
        ' Initialize controller IDs
        playerID = Nothing
        opponentID = Nothing
        mulliganDone = False

        If Not IsNothing(PlayerEntity) Then _
                playerID = PlayerEntity.GetTag(GAME_TAG.CONTROLLER)
        If Not IsNothing(OpponentEntity) Then _
                opponentID = OpponentEntity.GetTag(GAME_TAG.CONTROLLER)
    End Sub
    Public Sub onMulligan(Optional c As Card = Nothing)
        mulliganDone = True
    End Sub
    Public Sub onSpeechRecognized(sender As Object, e As SpeechRecognizedEventArgs) Handles hsRecog.SpeechRecognized

        If Not checkActiveWindow() Then Return

        'If below preset confidence threshold then exit
        If e.Result.Confidence < My.Settings.Threshold / 100 Then Return

        rebuildCardData()
        UpdateStatus("Executing """ & e.Result.Text & """...")

        If e.Result.Semantics.ContainsKey("action") Then
            Select Case e.Result.Semantics("action").Value
                Case "focus"
                    doFocus(e)

                Case "click"
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

                Case "choose"
                    doChoose(e)

                Case "cancel"
                    sendRightClick()

                Case "end"
                    moveCursor(91, 46) 'end turn button
                    sendLeftClick()

            End Select

        End If

        If e.Result.Semantics.ContainsKey("menu") And Game.IsInMenu Then
            doMenu(e)
        End If
        lastCommand = e.Result.Text
    End Sub

    Private Sub doChoose(e As SpeechRecognizedEventArgs)
        Dim optNum = e.Result.Semantics("option").Value
        Dim optmax = e.Result.Semantics("max").Value
        moveCursorToOption(optNum, optmax)
    End Sub

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
    End Sub

    Private Sub doClick(e As SpeechRecognizedEventArgs)
        If e.Result.Semantics.ContainsKey("heropower") Then 'focus hero or hero power
            Dim x, y
            If e.Result.Semantics("herotarget").Value = "friendly" Then y = 80 Else y = 20
            If e.Result.Semantics("heropower").Value = "hero" Then x = 50 Else x = 60
            moveCursor(x, y)
        End If
        If e.Result.Semantics.ContainsKey("card") Then
            Dim targetName = e.Result.Semantics("card").Value
            moveCursorToTarget(targetName)
        End If
        If e.Result.Semantics.ContainsKey("friendly") Then
            Dim targetName = e.Result.Semantics("friendly").Value
            moveCursorToTarget(targetName)
        End If
        If e.Result.Semantics.ContainsKey("opposing") Then
            Dim targetName = e.Result.Semantics("opposing").Value
            moveCursorToTarget(targetName)
        End If
        sendLeftClick()
    End Sub

    Private Sub onResetStatus() Handles timerResetStatus.Tick
        UpdateStatus("Listening...")
        timerResetStatus.Enabled = False
    End Sub
    Private Sub doMenu(e As SpeechRecognizedEventArgs)
        Select Case e.Result.Semantics("menu").Value
            Case "play"
                moveCursor(50, 31)
                sendLeftClick()
            Case "solo"
                moveCursor(50, 38)
                sendLeftClick()
            Case "arena"
                moveCursor(50, 45)
                sendLeftClick()
            Case "brawl"
                moveCursor(50, 52)
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
            Case "start brawl"
                moveCursor(65, 85)
                sendLeftClick()
            Case "cancel"
                moveCursor(52, 85)
                sendLeftClick()
            Case "back"
                moveCursor(92, 92)
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
            Dim friendlyName = e.Result.Semantics("friendly").Value
            moveCursor(60, 75)
            Sleep(100)
            startDrag()
            Sleep(100)
            moveCursorToTarget(friendlyName)
            Sleep(100)
            endDrag()
        ElseIf e.Result.Semantics.ContainsKey("opposing") Then
            Dim friendlyName = e.Result.Semantics("opposing").Value
            moveCursor(60, 75)
            Sleep(100)
            startDrag()
            Sleep(100)
            moveCursorToTarget(friendlyName)
            Sleep(100)
            endDrag()
        Else
            moveCursor(60, 75)
            Sleep(100)
            sendLeftClick()
        End If
    End Sub 'handle hero powers
    Private Sub doAttack(e As SpeechRecognizedEventArgs)
        If e.Result.Semantics.ContainsKey("opposing") Then
            'attack minion
            Dim friendlyName = e.Result.Semantics("friendly").Value
            Dim targetName = e.Result.Semantics("opposing").Value
            moveCursorToTarget(friendlyName)
            Sleep(100)
            startDrag()
            Sleep(100)
            moveCursorToTarget(targetName)
            Sleep(100)
            endDrag()
        Else
            'attack face
            Dim friendlyName = e.Result.Semantics("friendly").Value
            moveCursorToTarget(friendlyName)
            Sleep(100)
            startDrag()
            Sleep(100)
            moveCursor(50, 20)
            Sleep(100)
            endDrag()
        End If
    End Sub 'handle attacking
    Private Sub doPlay(e As SpeechRecognizedEventArgs)
        If e.Result.Semantics.ContainsKey("friendly") Then 'play to friendly target
            Dim cardName = e.Result.Semantics("card").Value
            Dim targetName = e.Result.Semantics("friendly").Value
            Dim targetnum = targetName.ToString.Substring(1)
            Dim newname = "f" & targetnum - 1

            dragTargetToTarget(cardName, newname)

        ElseIf e.Result.Semantics.ContainsKey("opposing") Then 'play to opposing target
            Dim cardName = e.Result.Semantics("card").Value
            Dim targetName = e.Result.Semantics("opposing").Value
            dragTargetToTarget(cardName, targetName)

        Else
            Dim cardName = e.Result.Semantics("card").Value 'play to board
            moveCursorToTarget(cardName)
            startDrag()
            moveCursor(85, 55)
            endDrag()

        End If
    End Sub 'handle playing cards
    Private Sub doMulligan(e As SpeechRecognizedEventArgs)
        If e.Result.Semantics.ContainsKey("card") Then
            Dim targetName = e.Result.Semantics("card").Value
            Dim targetNum = Convert.ToInt32(targetName.ToString.Substring(1))
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
    Private Sub doFocus(e As SpeechRecognizedEventArgs)
        If e.Result.Semantics.ContainsKey("heropower") Then 'focus hero or hero power
            Dim x, y
            If e.Result.Semantics("herotarget").Value = "friendly" Then y = 80 Else y = 20
            If e.Result.Semantics("heropower").Value = "hero" Then x = 50 Else x = 60
            moveCursor(x, y)
        End If
        If e.Result.Semantics.ContainsKey("card") Then
            Dim targetName = e.Result.Semantics("card").Value
            moveCursorToTarget(targetName)
        End If
        If e.Result.Semantics.ContainsKey("friendly") Then
            Dim targetName = e.Result.Semantics("friendly").Value
            moveCursorToTarget(targetName)
        End If
        If e.Result.Semantics.ContainsKey("opposing") Then
            Dim targetName = e.Result.Semantics("opposing").Value
            moveCursorToTarget(targetName)
        End If
    End Sub 'handle focusing cursor
    Public Sub sendLeftClick()
        mouse_event(MOUSEEVENTF_LEFTDOWN, Cursor.Position.X, Cursor.Position.Y, 0, 0)
        Sleep(50)
        mouse_event(MOUSEEVENTF_LEFTUP, Cursor.Position.X, Cursor.Position.Y, 0, 0)
        Sleep(50)
    End Sub
    Public Sub sendRightClick()
        mouse_event(MOUSEEVENTF_RIGHTDOWN, Cursor.Position.X, Cursor.Position.Y, 0, 0)
        Sleep(50)
        mouse_event(MOUSEEVENTF_RIGHTUP, Cursor.Position.X, Cursor.Position.Y, 0, 0)
        Sleep(50)
    End Sub
    Public Sub startDrag()
        mouse_event(MOUSEEVENTF_LEFTDOWN, Cursor.Position.X, Cursor.Position.Y, 0, 0)
        Sleep(50)
    End Sub
    Public Sub endDrag()
        mouse_event(MOUSEEVENTF_LEFTUP, Cursor.Position.X, Cursor.Position.Y, 0, 0)
    End Sub
    Public Sub dragTargetToTarget(startTarget As String, endTarget As String)
        moveCursorToTarget(startTarget)
        startDrag()
        moveCursorToTarget(endTarget)
        endDrag()
    End Sub
    Public Sub moveCursorToOption(optionNum As Integer, totalOptions As Integer)
        Dim optionsWidth As Integer = totalOptions * 20
        Dim myOption As Integer = (optionNum * 20) - 10
        Dim optionStart As Integer = 50 - (optionsWidth / 2)
        moveCursor(optionStart + myOption, 50)
    End Sub
    Public Sub moveCursorToTarget(TargetName As String)
        'Reads a target returned by the speech recognition semantic key
        Select Case TargetName.Substring(0, 1)
            Case "c" ' Target is a card
                Dim cardNum = Convert.ToInt32(TargetName.Substring(1))
                Dim totalCards = handCards.Count
                Dim cardWidth
                If totalCards > 3 Then
                    cardWidth = (36 / totalCards)
                Else
                    cardWidth = 11
                End If
                Dim cardsWidth = (totalCards * cardWidth)
                Dim myCardPos = (cardNum) * cardWidth
                Dim cardX As Integer = 45 - (cardsWidth / 2) + myCardPos - (cardWidth / 2) + 1
                moveCursor(cardX, 93)
            Case "f" ' Target is a friendly minion
                Dim tarMinion = Convert.ToInt32(TargetName.Substring(1))
                Dim allMinions = boardFriendly.Count

                Dim allWidth = allMinions * 10
                Dim myMinion = tarMinion * 10 - 5

                Dim minX = 50 - (allWidth / 2) + myMinion
                moveCursor(minX, 55)
            Case "e" ' Target is an enemy minion
                Dim tarMinion = Convert.ToInt32(TargetName.Substring(1))
                Dim allMinions = boardOpponent.Count

                Dim allWidth = allMinions * 10
                Dim myMinion = tarMinion * 10

                Dim minX = 50 - (allWidth / 2) + myMinion - 5
                moveCursor(minX, 40)
            Case "h" ' Target is a hero
                If TargetName.Substring(1) = 1 Then
                    moveCursor(50, 75)
                Else
                    moveCursor(50, 20)
                End If
        End Select
    End Sub
    Public Sub moveCursor(xPercent As Integer, yPercent As Integer)
        Dim hsWindow As IntPtr = FindWindow(Nothing, "Hearthstone")
        Dim hsRect As RECT
        GetWindowRect(hsWindow, hsRect)
        Dim windowWidth, windowHeight, uiWidth, uiOffset As Integer
        windowWidth = hsRect.Right - hsRect.Left
        windowHeight = hsRect.Bottom - hsRect.Top + 8

        uiWidth = windowHeight / 3 * 4
        uiOffset = (windowWidth - uiWidth) / 2
        Cursor.Position = New Point(hsRect.Left + uiOffset + (xPercent / 100 * uiWidth), hsRect.Top + (yPercent / 100 * windowHeight))
        Sleep(100)
    End Sub
    Public Function checkActiveWindow()
        Dim activeHwnd = GetForegroundWindow()
        Dim activeWindowText As New System.Text.StringBuilder(32)
        GetWindowText(activeHwnd, activeWindowText, activeWindowText.Capacity)
        If activeWindowText.ToString = "Hearthstone" Then
            Return True
        Else
            Return False
        End If
    End Function
    Public Sub recogWorker_DoWork() Handles recogWorker.DoWork

        Do While Not recogWorker.CancellationPending
            If Not Game.IsRunning Then
                System.Threading.Thread.Sleep(1000)
            Else
                UpdateStatus("Listening...")

                hsRecog.UnloadAllGrammars()
                hsRecog.LoadGrammar(buildGrammar)
                hsRecog.Recognize()
            End If

        Loop
    End Sub
    Public Sub UpdateStatus(Status As String)
        Try
            hdtStatus.Dispatcher.Invoke(Sub()
                                            Dim newStatus = "HDT-Voice: "
                                            newStatus &= Status
                                            If My.Settings.showLast Then
                                                newStatus &= vbNewLine & "Last executed: " & lastCommand
                                            End If
                                            hdtStatus.Text = newStatus
                                        End Sub)
        Catch ex As Exception
            Return
        End Try

    End Sub
    Public Function buildGrammar() As Grammar
        Debug.WriteLine("Building grammar")

        If Game.IsInMenu Then
            Dim menuGrammar As New GrammarBuilder
            Dim menuChoices As New Choices


            menuChoices.Add(New SemanticResultKey("menu", "play"))
            menuChoices.Add(New SemanticResultKey("menu", "solo"))
            menuChoices.Add(New SemanticResultKey("menu", "arena"))
            menuChoices.Add(New SemanticResultKey("menu", "brawl"))
            menuChoices.Add(New SemanticResultKey("menu", "casual"))
            menuChoices.Add(New SemanticResultKey("menu", "ranked"))
            menuChoices.Add(New SemanticResultKey("menu", "basic"))
            menuChoices.Add(New SemanticResultKey("menu", "custom"))
            menuChoices.Add(New SemanticResultKey("menu", "start game"))
            menuChoices.Add(New SemanticResultKey("menu", "start brawl"))
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

            Return New Grammar(menuGrammar)
        End If

        ' if the player or opponent entity is unknown, try initiate a new game
        If Game.IsRunning And playerID = 0 Or opponentID = 0 Then
            onNewGame()
        End If



        rebuildCardData()

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
                    Dim CardNum As Integer = CardInstances.IndexOf(CardInstances.Find(Function(x) x.Id = e.Id)) + 1
                    CardName &= " " & CardNum.ToString
                End If
                handGrammarNames.Add(New SemanticResultValue(CardName, "c" & (handCards.IndexOf(e) + 1).ToString))
                handGrammarNumbers.Add(New SemanticResultValue((handCards.IndexOf(e) + 1).ToString, "c" & (handCards.IndexOf(e) + 1).ToString))
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
                    Dim CardNum As Integer = CardInstances.IndexOf(CardInstances.Find(Function(x) x.Id = e.Id)) + 1
                    CardName &= " " & CardNum.ToString
                End If
                friendlyGrammarNames.Add(New SemanticResultValue(CardName, "f" & (boardFriendly.IndexOf(e) + 1).ToString))
                friendlyGrammarNumbers.Add(New SemanticResultValue("minion " & (boardFriendly.IndexOf(e) + 1).ToString, "f" & (boardFriendly.IndexOf(e) + 1).ToString))
            Next
            friendlyChoices.Add(friendlyGrammarNames, friendlyGrammarNumbers)
        End If

        Dim friendlyHero As New Choices
        friendlyHero.Add(New SemanticResultValue("hero", "h1"))
        friendlyHero.Add(New SemanticResultValue("face", "h1"))
        friendlyChoices.Add(friendlyHero)
        friendlyGrammar.Append(New SemanticResultKey("friendly", friendlyChoices))

        ' Build grammar for opposing minions and hero
        Dim opposingGrammar As New GrammarBuilder
        Dim opposingChoices As New Choices
        If boardOpponent.Count > 0 Then

            Dim opposingGrammarNames, opposingGrammarNumbers As New Choices
            For Each e In boardOpponent
                Dim CardName As New String(e.Card.Name)
                Dim CardInstances = boardOpponent.FindAll(Function(x) x.CardId = e.CardId)
                If CardInstances.Count > 1 Then
                    Dim CardNum As Integer = CardInstances.IndexOf(CardInstances.Find(Function(x) x.Id = e.Id)) + 1
                    CardName &= " " & CardNum.ToString
                End If
                opposingGrammarNames.Add(New SemanticResultValue(CardName, "e" & (boardOpponent.IndexOf(e) + 1).ToString))
                opposingGrammarNumbers.Add(New SemanticResultValue("minion " & (boardOpponent.IndexOf(e) + 1).ToString, "e" & (boardOpponent.IndexOf(e) + 1).ToString))
            Next
            opposingChoices.Add(opposingGrammarNames, opposingGrammarNumbers)
        End If
        Dim opposingHero As New Choices
        opposingHero.Add(New SemanticResultValue("hero", "h2"))
        opposingHero.Add(New SemanticResultValue("face", "h2"))
        opposingChoices.Add(opposingHero)
        opposingGrammar.Append(New SemanticResultKey("opposing", New Choices(opposingChoices)))

        ' Start building the final grammar
        Dim finalChoices As New Choices

        ' Check if we're at the mulligan, if so only the mulligan grammar will be returned
        If (Not mulliganDone) And (Not Game.IsMulliganDone) Then
            Dim mulliganBuilder As New GrammarBuilder
            mulliganBuilder.Append(New SemanticResultKey("action", New SemanticResultValue("click", "mulligan")))
            Dim mulliganChoices As New Choices
            mulliganChoices.Add("confirm")
            mulliganChoices.Add(cardGrammar)
            mulliganBuilder.Append(New Choices(mulliganChoices, "confirm"))

            finalChoices.Add(mulliganBuilder)
            Return New Grammar(New GrammarBuilder(finalChoices))
        End If

        'build grammar for card actions
        If cardGrammar.DebugShowPhrases.Count Then
            'focus card
            Dim focusCards As New GrammarBuilder
            focusCards.Append(New SemanticResultKey("action", "focus"))
            focusCards.Append("card")
            focusCards.Append(cardGrammar)

            'play card with no target
            Dim playCards As New GrammarBuilder
            playCards.Append(New SemanticResultKey("action", "play"))
            playCards.Append(cardGrammar)

            'play card with friendly target
            If friendlyGrammar.DebugShowPhrases.Count Then
                Dim playToFriendly As New GrammarBuilder
                playToFriendly.Append(New SemanticResultKey("action", "play"))
                playToFriendly.Append(cardGrammar)
                playToFriendly.Append("to")
                playToFriendly.Append(friendlyNames)
                playToFriendly.Append(friendlyGrammar)

                finalChoices.Add(playToFriendly)
            End If

            'play card to opposing target
            If opposingGrammar.DebugShowPhrases.Count Then
                Dim playToOpposing As New GrammarBuilder
                playToOpposing.Append(New SemanticResultKey("action", "play"))
                playToOpposing.Append(cardGrammar)
                playToOpposing.Append("to")
                playToOpposing.Append(opposingNames)
                playToOpposing.Append(opposingGrammar)

                finalChoices.Add(playToOpposing)
            End If

            'play card to position
            Dim playToPosition As New GrammarBuilder
            playToPosition.Append(New SemanticResultKey("action", "play"))
            playToPosition.Append(cardGrammar)
            playToPosition.Append("to position")
            Dim posNumbers As New Choices
            For i = 1 To boardFriendly.Count + 1
                posNumbers.Add(i.ToString)
            Next
            playToPosition.Append(posNumbers)

            finalChoices.Add(playToPosition)
            finalChoices.Add(playCards)
            finalChoices.Add(focusCards)
        End If

        'build grammar friendly minion actions
        If friendlyGrammar.DebugShowPhrases.Count Then
            'focus friendly minion
            Dim focusFriendly As New GrammarBuilder
            focusFriendly.Append(New SemanticResultKey("action", "focus"))
            focusFriendly.Append(friendlyNames)
            focusFriendly.Append(friendlyGrammar)
            finalChoices.Add(focusFriendly)

            'attack target with friendly minion
            Dim attackFriendly As New GrammarBuilder
            attackFriendly.Append(New SemanticResultKey("action", "attack"))
            attackFriendly.Append(opposingGrammar)
            attackFriendly.Append("with")
            attackFriendly.Append(friendlyGrammar)
            finalChoices.Add(attackFriendly)

            'use hero power on friendly target
            Dim heroFriendly As New GrammarBuilder
            heroFriendly.Append("use")
            heroFriendly.Append(New SemanticResultKey("action", "hero"))
            heroFriendly.Append("power on")
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
            Dim focusOpposing As New GrammarBuilder
            focusOpposing.Append(New SemanticResultKey("action", "focus"))
            focusOpposing.Append(opposingNames)
            focusOpposing.Append(opposingGrammar)
            finalChoices.Add(focusOpposing)

            Dim heroOpposing As New GrammarBuilder
            heroOpposing.Append("use")
            heroOpposing.Append(New SemanticResultKey("action", "hero"))
            heroOpposing.Append("power on")
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
        heroPower.Append("use")
        heroPower.Append(New SemanticResultKey("action", "hero"))
        heroPower.Append("power")
        finalChoices.Add(heroPower)

        Dim endTurn As New GrammarBuilder
        endTurn.Append(New SemanticResultKey("action", "end"))
        endTurn.Append("turn")
        finalChoices.Add(endTurn)

        Dim sayEmote As New GrammarBuilder
        sayEmote.Append(New SemanticResultKey("action", "say"))
        Dim sayChoices As New Choices
        sayChoices.Add(New SemanticResultValue("thanks", "thanks"))
        sayChoices.Add(New SemanticResultValue("well played", "well played"))
        sayChoices.Add(New SemanticResultValue("greetings", "greetings"))
        sayChoices.Add(New SemanticResultValue("sorry", "sorry"))
        sayChoices.Add(New SemanticResultValue("oops", "oops"))
        sayChoices.Add(New SemanticResultValue("threaten", "threaten"))
        sayEmote.append(New SemanticResultKey("emote", sayChoices))
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

        Try
            Return New Grammar(New GrammarBuilder(finalChoices))
        Catch ex As Exception
            Return New Grammar(New GrammarBuilder("error"))
        End Try


    End Function 'builds and returns grammar for the speech recognition engine
    Public Sub rebuildCardData()
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
        boardOpponent.Clear()

        For Each e In Entities
            If e.IsInPlay And e.IsMinion Then
                If e.IsControlledBy(playerID) Then
                    boardFriendly.Add(e)
                ElseIf e.IsControlledBy(opponentID) Then
                    boardOpponent.Add(e)
                End If
            End If
        Next

        ' sort by position on board
        boardFriendly.Sort(Function(e1 As Entity, e2 As Entity)
                               Return e1.GetTag(GAME_TAG.ZONE_POSITION).CompareTo(e2.GetTag(GAME_TAG.ZONE_POSITION))
                           End Function)

        boardOpponent.Sort(Function(e1 As Entity, e2 As Entity)
                               Return e1.GetTag(GAME_TAG.ZONE_POSITION).CompareTo(e2.GetTag(GAME_TAG.ZONE_POSITION))
                           End Function)
    End Sub 'rebuilds data for cards in hand and on board
End Class
