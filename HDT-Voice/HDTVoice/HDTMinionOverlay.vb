Imports Hearthstone_Deck_Tracker
Imports Hearthstone_Deck_Tracker.API
Imports Hearthstone_Deck_Tracker.Enums
Imports Hearthstone_Deck_Tracker.Hearthstone.Entities
Imports System.Windows.Controls
Imports System.Threading
Imports System.Threading.Thread
Imports System.Windows.Threading
Imports System.ComponentModel

Public Class HDTMinionOverlay
    Public friendlyID As Integer = 0
    Public opposingID As Integer = 0
    Private workerOverlay As BackgroundWorker
    Private canvasHDT As Canvas
    Public Sub New()
        StartNewGame()
        canvasHDT = Core.OverlayCanvas

        workerOverlay = New BackgroundWorker
        workerOverlay.WorkerSupportsCancellation = True
        'AddHandler workerOverlay.DoWork, AddressOf workerOverlay_DoWork

        GameEvents.OnGameStart.Add(New Action(AddressOf StartNewGame))
        GameEvents.OnGameEnd.Add(New Action(AddressOf EndGame))
        'workerOverlay.RunWorkerAsync()

        'Dim overlayThread As New Thread(New ThreadStart(Sub()
        'workerOverlay_DoWork()
        'End Sub))
        'overlayThread.SetApartmentState(Threading.ApartmentState.STA)
        'overlayThread.Start()

    End Sub
    Public Sub Unload()
        If Not IsNothing(workerOverlay) Then
            workerOverlay.CancelAsync()
        End If
    End Sub

    Private Sub workerOverlay_DoWork()

        Dim canvasMinions As New Canvas
        canvasMinions = New Canvas
        Canvas.SetTop(canvasMinions, 200)
        Canvas.SetLeft(canvasMinions, 200)
        canvasHDT.Dispatcher.Invoke(Sub(cm)
                                        canvasHDT.Children.Add(cm)
                                    End Sub, canvasMinions)
        Do
            Sleep(200)

            ' If there is no current game in progress
            If friendlyID = 0 Then
                StartNewGame()
                Continue Do
            End If


            ' Clear all existing minions from overlay
            Do
                For Each child In canvasMinions.Children
                    canvasHDT.Children.Remove(child)
                    Continue Do
                Next
                Exit Do
            Loop


            Dim Friendlys = GetFriendlyMinions()

            If Friendlys.Count = 0 Then _
                Continue Do

            Dim minionBlocks As New List(Of HearthstoneTextBlock)
            For Each e In Friendlys
                Dim fBlock As New HearthstoneTextBlock
                fBlock.Text = e.Card.Name
                minionBlocks.Add(fBlock)
            Next

            Dim UISize = HDTVoice.Mouse.HSUISize

            Dim minionWidth As Integer = UISize.Width * 0.09
            canvasMinions.Width = minionWidth * (minionBlocks.Count + 1)

            For Each m In minionBlocks
                Dim intMinNum As Integer = minionBlocks.IndexOf(m)
                Dim min = m
                Canvas.SetTop(min, 0)
                Canvas.SetLeft(min, minionWidth * intMinNum)
                canvasMinions.Children.Add(min)
            Next



        Loop While workerOverlay.CancellationPending = False
    End Sub

    Private Sub StartNewGame()
        friendlyID = Nothing
        opposingID = Nothing

        If Not IsNothing(PlayerEntity) And Not IsNothing(OpponentEntity) Then
            friendlyID = PlayerEntity.GetTag(GAME_TAG.CONTROLLER)
            opposingID = OpponentEntity.GetTag(GAME_TAG.CONTROLLER)
        End If
    End Sub                                     ' Re-initalizes controller IDs
    Private Sub EndGame()
        friendlyID = 0
        opposingID = 0
        workerOverlay.CancelAsync()
        Do While workerOverlay.IsBusy
            Sleep(10)
        Loop
        workerOverlay.Dispose()
    End Sub
    Private Function GetCardsInHand() As List(Of Entity)
        Dim CardsInHand As New List(Of Entity)

        For Each e In Entities
            If e.IsInHand And e.GetTag(GAME_TAG.CONTROLLER) = friendlyID Then
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
            If e.IsInPlay And e.IsMinion And e.IsControlledBy(friendlyID) Then
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
            If e.IsInPlay And e.IsMinion And e.IsControlledBy(opposingID) Then
                OpposingMinions.Add(e)
            End If
        Next

        OpposingMinions.Sort(Function(e1 As Entity, e2 As Entity)
                                 Return e1.GetTag(GAME_TAG.ZONE_POSITION).CompareTo(e2.GetTag(GAME_TAG.ZONE_POSITION))
                             End Function)

        Return OpposingMinions
    End Function       ' Returns an ordered list of the current opposing minions
    Private ReadOnly Property Entities As Entity()
        Get
            ' Clone entities from game and return as array
            Return Helper.DeepClone(Core.Game.Entities).Values.ToArray
        End Get
    End Property                ' Clones Entites from HDT and creates an array
    Private ReadOnly Property PlayerEntity As Entity
        Get
            ' Return the Entity representing the player
            Return Entities.FirstOrDefault(Function(x) x.IsPlayer())
        End Get
    End Property              ' Gets the player's current Entity
    Private ReadOnly Property OpponentEntity As Entity
        Get
            ' Return the Entity representing the player
            Return Entities.FirstOrDefault(Function(x) x.IsOpponent())
        End Get
    End Property            ' Gets the opponent's current Entity
End Class
