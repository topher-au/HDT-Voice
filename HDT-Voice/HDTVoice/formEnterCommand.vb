Imports System.Windows.Forms

Public Class formEnterCommand

    Private Sub debugInput_Shown(sender As Object, e As EventArgs) Handles Me.Shown
        AppActivate("Enter command")
    End Sub

    Private Sub TextBox1_KeyPress(sender As Object, e As KeyPressEventArgs) Handles textCommand.KeyPress
        If e.KeyChar = Chr(Keys.Enter) Then
            buttonSend.PerformClick()
        End If
        If e.KeyChar = Chr(Keys.Back) Then
            Me.DialogResult = DialogResult.Cancel
            AppActivate("Hearthstone")
            Me.Close()
        End If
    End Sub

End Class