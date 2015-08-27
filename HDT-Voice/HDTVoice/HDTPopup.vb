Imports System.Threading.Thread
Imports System.Windows.Controls
Imports System.Windows.Forms

Imports Hearthstone_Deck_Tracker
Imports Hearthstone_Deck_Tracker.API
Public Class HDTPopup
    Private popupText As String
    Private popupDuration As Integer
    Private hdtCanvas As Canvas
    Private boolFadeComplete As Boolean = False
    Public Sub New(Text As String, Duration As Integer)
        popupText = Text
        popupDuration = Duration

        hdtCanvas = Core.OverlayCanvas
        ' Invoke popup from main canvas
        hdtCanvas.Dispatcher.Invoke(AddressOf InvokePopup)
    End Sub
    Private Sub InvokePopup()
        'Spawn backgroundworker to avoid blocking HDT thread

        hdtCanvas.Dispatcher.Invoke(Sub()
                                        Dim fadeWorker As New System.ComponentModel.BackgroundWorker
                                        AddHandler fadeWorker.DoWork, AddressOf DoPopup
                                        fadeWorker.RunWorkerAsync()
                                    End Sub)
    End Sub
    Private Function CreatePopupCanvas(Text As String) As Canvas
        Dim canvasPopup As New Canvas
        canvasPopup.Tag = "HDTVoicePopup"

        'Create text and add to popup
        Dim htbPopupText As New HearthstoneTextBlock
        Select Case My.Settings.intNotificationSize
            Case 0
                htbPopupText.FontSize = 12
            Case 1
                htbPopupText.FontSize = 16
            Case 2
                htbPopupText.FontSize = 22
        End Select
        hdtCanvas.Children.Add(canvasPopup)
        htbPopupText.Text = Text
        Canvas.SetZIndex(htbPopupText, 1)
        Canvas.SetLeft(htbPopupText, 10)
        Canvas.SetTop(htbPopupText, 8)
        htbPopupText.UpdateLayout()
        canvasPopup.Children.Add(htbPopupText)
        canvasPopup.UpdateLayout()


        'Create background, size to text and add to popup
        Dim rectPopupBG As New System.Windows.Shapes.Rectangle
        rectPopupBG.RadiusX = 3
        rectPopupBG.RadiusY = 10
        rectPopupBG.Fill = New System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(110, 0, 0, 0))
        rectPopupBG.Height = htbPopupText.RenderSize.Height + 16
        rectPopupBG.Width = htbPopupText.RenderSize.Width + 20
        canvasPopup.Children.Add(rectPopupBG)
        canvasPopup.UpdateLayout()

        Select Case My.Settings.intNotificationPos
            Case 0 ' Top Left
                Canvas.SetTop(canvasPopup, 16)
                Canvas.SetLeft(canvasPopup, 16)
            Case 1 ' Bottom Left
                Canvas.SetTop(canvasPopup, hdtCanvas.ActualHeight * 0.93 - rectPopupBG.Height / 2)
                Canvas.SetLeft(canvasPopup, 8)
            Case 2 ' Top Right
                Canvas.SetTop(canvasPopup, 16)
                Canvas.SetLeft(canvasPopup, hdtCanvas.ActualWidth - rectPopupBG.ActualWidth - 16)
            Case 3 ' Bottom Right
                Canvas.SetTop(canvasPopup, hdtCanvas.ActualHeight * 0.93 - rectPopupBG.Height / 2)
                Canvas.SetLeft(canvasPopup, hdtCanvas.ActualWidth - rectPopupBG.ActualWidth - 16)
        End Select

        Return canvasPopup
    End Function
    Private Sub FadeIn(Popup As Canvas)
        hdtCanvas.Children.Add(Popup)
        For i = 0 To 1 Step 0.1
            Popup.Opacity = i
            Application.DoEvents()
            Sleep(10)
        Next
    End Sub
    Private Sub FadeOut(Popup As Canvas)
        For i = 1 To 0 Step -0.05
            Popup.Opacity = i
            Application.DoEvents()
            Sleep(15)
        Next
        hdtCanvas.Children.Remove(Popup)
    End Sub
    Private Sub DoPopup()

        Dim popupCanvas As Canvas = hdtCanvas.Dispatcher.Invoke(Function()
                                                                    Return CreatePopupCanvas(popupText)
                                                                End Function)

        hdtCanvas.Dispatcher.Invoke(Sub()
                                        RemoveAllPopups()
                                        FadeIn(popupCanvas)
                                    End Sub)
        Sleep(popupDuration)
        hdtCanvas.Dispatcher.Invoke(Sub()
                                        FadeOut(popupCanvas)
                                    End Sub)
    End Sub

    Private Sub RemoveAllPopups()
        Do
            For Each child In hdtCanvas.Children
                If TypeOf child Is Canvas Then
                    If child.Tag = "HDTVoicePopup" Then
                        hdtCanvas.Children.Remove(child)
                        Continue Do
                    End If
                End If
            Next
            Exit Do
        Loop
    End Sub
End Class
