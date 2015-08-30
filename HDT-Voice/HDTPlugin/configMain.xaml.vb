Imports System.Net
Imports Newtonsoft.Json

Public Class configMain
    Sub New()
        InitializeComponent()

        checkAutoStart.IsChecked = My.Settings.boolListenAtStartup
        AddHandler checkAutoStart.Click, AddressOf SaveSettings

        checkShowNotification.IsChecked = My.Settings.boolShowNotification
        AddHandler checkShowNotification.Click, AddressOf SaveSettings

        comboNotificationPos.SelectedIndex = My.Settings.intNotificationPos
        AddHandler comboNotificationPos.SelectionChanged, AddressOf SaveSettings

        comboNotificationSize.SelectedIndex = My.Settings.intNotificationSize
        AddHandler comboNotificationSize.SelectionChanged, AddressOf SaveSettings

        checkSmoothMouse.IsChecked = My.Settings.boolSmoothCursor
        AddHandler checkSmoothMouse.Click, AddressOf SaveSettings

        checkDebugLog.IsChecked = My.Settings.boolDebugLog
        AddHandler checkDebugLog.Click, AddressOf SaveSettings

        checkHearthstoneActive.IsChecked = My.Settings.boolHearthActive
        AddHandler checkHearthstoneActive.Click, AddressOf SaveSettings

        AddHandler buttonUpdate.Click, AddressOf ClickUpdateButton



    End Sub
    Sub SaveSettings()
        My.Settings.boolListenAtStartup = checkAutoStart.IsChecked
        My.Settings.boolShowNotification = checkShowNotification.IsChecked
        My.Settings.intNotificationPos = comboNotificationPos.SelectedIndex
        My.Settings.intNotificationSize = comboNotificationSize.SelectedIndex 
        My.Settings.boolSmoothCursor = checkSmoothMouse.IsChecked
        My.Settings.boolDebugLog = checkDebugLog.IsChecked
        My.Settings.boolHearthActive = checkHearthstoneActive.IsChecked
        My.Settings.Save()

        comboNotificationSize.IsEnabled = checkShowNotification.IsChecked
        comboNotificationPos.IsEnabled = checkShowNotification.IsChecked
    End Sub
    Public Sub ClickUpdateButton()
        Process.Start("https://www.github.com/topher-au/HDT-Voice/releases/latest")
    End Sub

End Class
