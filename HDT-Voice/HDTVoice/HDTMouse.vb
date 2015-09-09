Imports Hearthstone_Deck_Tracker
Imports Hearthstone_Deck_Tracker.API
Imports Hearthstone_Deck_Tracker.Enums
Imports Hearthstone_Deck_Tracker.Hearthstone.Entities
Imports System.Threading.Thread
Imports System.Windows.Forms
Imports System.Drawing

Public Class Mouse
    Private Declare Function GetForegroundWindow Lib "user32" () As System.IntPtr
    Private Declare Function FindWindow Lib "user32" Alias "FindWindowA" (ByVal lpClassName As String, ByVal lpWindowName As String) As Double
    Private Declare Function GetWindowRect Lib "user32" Alias "GetWindowRect" (ByVal hwnd As IntPtr, ByRef lpRect As RECT) As Integer
    Private Declare Sub mouse_event Lib "user32" (ByVal dwFlags As Integer, ByVal dx As Integer, ByVal dy As Integer, ByVal cButtons As Integer, ByVal dwExtraInfo As IntPtr)
    Public Declare Auto Function GetWindowThreadProcessId Lib "user32" (ByVal hwnd As IntPtr, ByRef lpdwProcessId As Integer) As Integer
    Public Declare Function OpenProcess Lib "kernel32" (dwDesiredAccess As Integer, bInheritHandle As Boolean, dwProcessId As Integer) As Long
    Public Declare Function GetProcessImageFileName Lib "psapi.dll" Alias "GetProcessImageFileNameA" (hProcess As Integer, lpImageFileName As String, nSize As Integer) As Integer
    Public Declare Function CloseHandle Lib "kernel32" (hObject As Integer) As Integer
    Const MOUSE_LEFTDOWN As UInteger = &H2
    Const MOUSE_LEFTUP As UInteger = &H4
    Const MOUSE_RIGHTDOWN As UInteger = &H8
    Const MOUSE_RIGHTUP As UInteger = &H10
    Private PlayerID, OpponentID As Integer
    Public Structure RECT
        Public Left As Integer
        Public Top As Integer
        Public Right As Integer
        Public Bottom As Integer
    End Structure                                                 ' Window rectangle for API
    Public Enum Buttons
        Left = &H2
        Right = &H8
    End Enum                                            ' Enum representing mouse buttons for API call
    Public Sub New()
        GameEvents.OnGameStart.Add(New Action(AddressOf NewGame))
    End Sub
    Private Sub NewGame()
        PlayerID = Nothing
        OpponentID = Nothing

        If Not IsNothing(player) Then _
            PlayerID = player.GetTag(GAME_TAG.CONTROLLER)
        If Not IsNothing(opponent) Then _
            OpponentID = opponent.GetTag(GAME_TAG.CONTROLLER)
    End Sub
    Private ReadOnly Property Entities As Entity()
        Get
            ' Clone entities from game and return as array
            Dim EntArray = Helper.DeepClone(Core.Game.Entities).Values.ToArray
            Return EntArray
        End Get
    End Property                 ' The list of entities for the current game
    Private ReadOnly Property player As Entity
        Get
            Return Entities.FirstOrDefault(Function(x) x.IsPlayer())
        End Get
    End Property               ' The player's entity
    Private ReadOnly Property opponent As Entity
        Get
            ' Return the Entity representing the player
            Return Entities.FirstOrDefault(Function(x) x.IsOpponent())
        End Get
    End Property             ' The opponent entity
    Private Function GetCardsInHand() As List(Of Entity)
        Dim CardsInHand As New List(Of Entity)

        For Each e In Entities
            If e.IsInHand And e.GetTag(GAME_TAG.CONTROLLER) = PlayerID Then
                ' If entity is in player hand then add to list
                CardsInHand.Add(e)
            End If
        Next

        CardsInHand.Sort(Function(e1 As Entity, e2 As Entity)
                             Return e1.GetTag(GAME_TAG.ZONE_POSITION).CompareTo(e2.GetTag(GAME_TAG.ZONE_POSITION))
                         End Function)

        Return CardsInHand
    End Function           ' Returns an ordered list of the current cards in hand
    Private Function GetFriendlyMinions() As List(Of Entity)
        Dim FriendlyMinions As New List(Of Entity)

        For Each e In Entities
            If e.IsInPlay And e.IsMinion And e.IsControlledBy(PlayerID) Then
                FriendlyMinions.Add(e)
            End If
        Next

        FriendlyMinions.Sort(Function(e1 As Entity, e2 As Entity)
                                 Return e1.GetTag(GAME_TAG.ZONE_POSITION).CompareTo(e2.GetTag(GAME_TAG.ZONE_POSITION))
                             End Function)

        Return FriendlyMinions
    End Function       ' Returns an ordered list of the current friendly minions
    Private Function GetOpposingMinions() As List(Of Entity)
        Dim OpposingMinions As New List(Of Entity)

        For Each e In Entities
            If e.IsInPlay And e.IsMinion And e.IsControlledBy(OpponentID) Then
                OpposingMinions.Add(e)
            End If
        Next

        OpposingMinions.Sort(Function(e1 As Entity, e2 As Entity)
                                 Return e1.GetTag(GAME_TAG.ZONE_POSITION).CompareTo(e2.GetTag(GAME_TAG.ZONE_POSITION))
                             End Function)

        Return OpposingMinions
    End Function       ' Returns an ordered list of the current opposing minions
    Public Sub SendClick(Optional button As Buttons = Buttons.Left)
        mouse_event(button, Cursor.Position.X, Cursor.Position.Y, 0, 0)
        Sleep(20)
        mouse_event(button * 2, Cursor.Position.X, Cursor.Position.Y, 0, 0)
        Sleep(50)
    End Sub

    Public Sub StartDrag()
        mouse_event(Buttons.Left, Cursor.Position.X, Cursor.Position.Y, 0, 0)
        Sleep(100)
    End Sub
    Public Sub EndDrag()
        mouse_event(Buttons.Left + 2, Cursor.Position.X, Cursor.Position.Y, 0, 0)
        Sleep(100)
    End Sub
    Public Sub DragToTarget(StartEntity As String, EndEntity As String, Optional DropXOffset As Integer = 0)
        MoveToEntity(StartEntity)
        StartDrag()
        MoveToEntity(EndEntity, DropXOffset)
        EndDrag()
    End Sub
    Public Sub MoveToEntity(Entity As String, Optional XOffset As Integer = 0)
        If PlayerID = 0 Then NewGame()

        Dim targetEntity As Entity = HDTVoice.GrammarEngine.GetEntityFromSemantic(Entity)

        If IsNothing(targetEntity) Then Exit Sub

        'Sort entities into local variables
        Dim handCards = GetCardsInHand()
        Dim boardFriendly = GetFriendlyMinions()
        Dim boardOpposing = GetOpposingMinions()

        'First, check if the entity is a card in our hand
        If targetEntity.IsInHand And targetEntity.GetTag(GAME_TAG.CONTROLLER) = PlayerID Then
            Dim cardNum As Integer = targetEntity.GetTag(GAME_TAG.ZONE_POSITION)
            Dim totalCards As Integer = handCards.Count
            Dim handwidth = If(totalCards < 4, 10 * totalCards, 38)

            Dim x = (((cardNum) / (totalCards)) * handwidth) - (handwidth / 2) - 0.8 + XOffset
            Dim y = (x * -0.16) ^ 2
            MoveTo(x + 44, y + 89)
            Return
        End If

        'Next, check whether it is a friendly minion
        If targetEntity.IsInPlay And targetEntity.IsMinion And targetEntity.GetTag(GAME_TAG.CONTROLLER) = PlayerID Then
            Dim minionNum As Integer = targetEntity.GetTag(GAME_TAG.ZONE_POSITION)
            Dim totalMinions As Integer = boardFriendly.Count
            Dim minionWidth As Double = 29 / 3
            Dim totalWidth As Double = totalMinions * minionWidth
            Dim minionX As Double = (minionNum * minionWidth) - (minionWidth / 2)

            Dim cursorX As Integer = 50 - (totalWidth / 2) + minionX + XOffset
            MoveTo(cursorX, 55)
            Return
        End If

        'Then, check whether it is an opposing minion
        If targetEntity.IsInPlay And targetEntity.IsMinion And targetEntity.GetTag(GAME_TAG.CONTROLLER) = OpponentID Then
            Dim minionNum As Integer = targetEntity.GetTag(GAME_TAG.ZONE_POSITION)
            Dim totalMinions As Integer = boardOpposing.Count
            Dim minionWidth As Double = 29 / 3
            Dim totalWidth As Double = totalMinions * minionWidth
            Dim minionX As Double = (minionNum * minionWidth) - (minionWidth / 2)

            Dim cursorX As Integer = 50 - (totalWidth / 2) + minionX + XOffset
            MoveTo(cursorX, 38)
            Return
        End If

        'Finally, check whether it is a hero
        If targetEntity.IsPlayer Then
            MoveTo(50, 75)
            Return
        End If

        If targetEntity.IsOpponent Then
            MoveTo(50, 20)
            Return
        End If

        Return 'failed to locate entity on the board

    End Sub
    Public Sub MoveToMulligan(cardNum As Integer)
        If Core.Game.OpponentHasCoin Then
            Dim optionSize As Integer = 24.5
            Dim optionsWidth As Integer = 3 * optionSize
            Dim myOption As Double = (cardNum * optionSize) - (optionSize / 2)
            Dim optionStart As Double = 50 - (optionsWidth / 2)
            MoveTo(optionStart + myOption, 50)
        Else
            Dim optionSize As Integer = 17.5
            Dim optionsWidth As Integer = 4 * optionSize
            Dim myOption As Double = (cardNum * optionSize) - (optionSize / 2)
            Dim optionStart As Double = 50 - (optionsWidth / 2)
            MoveTo(optionStart + myOption, 50)
        End If

    End Sub
    Public Sub MoveToOption(OptionNumber As Integer, TotalOptions As Integer)
        Dim optionSize As Integer = 20
        Dim optionsWidth As Integer = TotalOptions * optionSize
        Dim myOption As Integer = (OptionNumber * optionSize) - (optionSize / 2)
        Dim optionStart As Integer = 50 - (optionsWidth / 2)
        MoveTo(optionStart + myOption, 50)
    End Sub
    Public Sub MoveTo(X As Integer, Y As Integer)
        Dim rectHS As RECT = GetHSRect()

        Dim windowWidth = rectHS.Right - rectHS.Left
        Dim uiHeight = rectHS.Bottom - rectHS.Top
        Dim uiWidth = ((uiHeight) / 3) * 4 ' A 4:3 square in the center

        Dim xOffset = (windowWidth - uiWidth) / 2 ' The space on the side of the game UI

        Dim endX As Integer = (X / 100) * uiWidth + xOffset + rectHS.Left
        Dim endY As Integer = (Y / 100) * uiHeight + rectHS.Top + 8
        Dim startY As Integer = Cursor.Position.Y
        Dim startX As Integer = Cursor.Position.X

        Dim duration = 50

        If My.Settings.boolSmoothCursor Then
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
    Public Function GetHSRect() As RECT
        Dim fgHwnd As IntPtr = GetForegroundWindow()
        Dim fgProcessName As String = GetWindowProcessName(fgHwnd)
        If fgProcessName = "Hearthstone.exe" Then
            Dim rectHS As RECT
            GetWindowRect(fgHwnd, rectHS)
            Return rectHS
        Else
            Return Nothing
        End If
    End Function
    Private Function GetWindowProcessName(WindowHandle As Double) As String
        GetWindowProcessName = Nothing
        Const MAX_PATH = 32768
        Const PROCESS_QUERY_INFORMATION = &H400
        Const PROCESS_VM_READ = &H10

        Dim strBuffer As String
        Dim bufferLength As Integer, processHandle As Double
        strBuffer = New String(Chr(0), MAX_PATH)
        Dim windowProcessID As Integer = Nothing
        GetWindowThreadProcessId(WindowHandle, windowProcessID)
        processHandle = OpenProcess(PROCESS_QUERY_INFORMATION Or PROCESS_VM_READ, 0, windowProcessID)
        If processHandle Then
            bufferLength = GetProcessImageFileName(processHandle, strBuffer, MAX_PATH)
            If bufferLength Then
                strBuffer = Left$(strBuffer, bufferLength)
                GetWindowProcessName = strBuffer.Substring(strBuffer.LastIndexOf("\") + 1)
            End If
            CloseHandle(processHandle)
        End If
        Return GetWindowProcessName
    End Function
    Public Function IsHearthstoneActive() As Boolean
        If Not My.Settings.boolHearthActive Then Return True
        Try
            Dim fgHwnd As Double = GetForegroundWindow()
            Dim fgProcessName As String = GetWindowProcessName(fgHwnd)
            If fgProcessName = "Hearthstone.exe" Then
                Return True
            End If
        Catch ex As Exception
            Return True
        End Try
        Return False
    End Function 'Checks if the hearthstone window is active
End Class

