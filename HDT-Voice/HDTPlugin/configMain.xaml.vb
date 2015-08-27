Public Class configMain
    Sub New()
        InitializeComponent()

        checkAutoStart.IsChecked = My.Settings.boolListenAtStartup
        AddHandler checkAutoStart.Checked, AddressOf SaveSettings

        checkShowNotification.IsChecked = My.Settings.boolShowNotification
        AddHandler checkShowNotification.Checked, AddressOf SaveSettings

        comboNotificationPos.SelectedIndex = My.Settings.intNotificationPos
        AddHandler comboNotificationPos.SelectionChanged, AddressOf SaveSettings

        checkSmoothMouse.IsChecked = My.Settings.boolSmoothCursor
        AddHandler checkSmoothMouse.Checked, AddressOf SaveSettings

        checkDebugLog.IsChecked = My.Settings.boolDebugLog
        AddHandler checkDebugLog.Checked, AddressOf SaveSettings

    End Sub
    Sub SaveSettings()
        My.Settings.boolListenAtStartup = checkAutoStart.IsChecked
        My.Settings.boolShowNotification = checkShowNotification.IsChecked
        My.Settings.intNotificationPos = comboNotificationPos.SelectedIndex
        My.Settings.boolSmoothCursor = checkSmoothMouse.IsChecked
        My.Settings.boolDebugLog = checkDebugLog.IsChecked
        My.Settings.Save()
    End Sub
End Class
