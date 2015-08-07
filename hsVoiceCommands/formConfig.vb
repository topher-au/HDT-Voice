Public Class formConfig
    Private Sub buttonSave_Click(sender As Object, e As EventArgs) Handles buttonSave.Click
        My.Settings.Threshold = numThreshold.Value
        My.Settings.showLast = checkLast.Checked
        My.Settings.outputDebug = checkDebugLog.Checked
        My.Settings.Save()
        Me.Close()
    End Sub

    Private Sub formConfig_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        numThreshold.Value = My.Settings.Threshold
        checkLast.Checked = My.Settings.showLast
        checkDebugLog.Checked = My.Settings.showLast
    End Sub
End Class