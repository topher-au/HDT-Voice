Public Class formConfig
    Private Sub buttonSave_Click(sender As Object, e As EventArgs) Handles buttonSave.Click
        'HDT-Voice Settings
        My.Settings.showStatusText = checkShowStatus.Checked
        My.Settings.statusTextPos = comboStatusPos.SelectedIndex
        My.Settings.showLast = checkLast.Checked
        My.Settings.outputDebug = checkDebugLog.Checked
        My.Settings.showStatusLight = checkShowLight.Checked
        My.Settings.quickPlay = checkQuick.Checked

        'Recognition settings
        My.Settings.Threshold = numThreshold.Value

        My.Settings.Save()
        Me.Close()
    End Sub

    Private Sub formConfig_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        checkShowStatus.Checked = My.Settings.showStatusText
        comboStatusPos.SelectedIndex = My.Settings.statusTextPos
        comboStatusPos.Enabled = checkShowStatus.Checked

        checkLast.Checked = My.Settings.showLast
        checkLast.Enabled = checkShowStatus.Checked

        checkLast.Enabled = checkShowStatus.Checked
        comboStatusPos.Enabled = checkShowStatus.Checked

        checkDebugLog.Checked = My.Settings.showLast

        checkShowLight.Checked = My.Settings.showStatusLight

        checkQuick.Checked = My.Settings.quickPlay

        numThreshold.Value = My.Settings.Threshold
    End Sub

    Private Sub checkShowStatus_CheckedChanged(sender As Object, e As EventArgs) Handles checkShowStatus.CheckedChanged
        checkLast.Enabled = checkShowStatus.Checked
        comboStatusPos.Enabled = checkShowStatus.Checked
    End Sub
End Class