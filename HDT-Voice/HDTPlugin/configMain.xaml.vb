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

        checkSmoothMouse.IsChecked = My.Settings.boolSmoothCursor
        AddHandler checkSmoothMouse.Click, AddressOf SaveSettings

        checkDebugLog.IsChecked = My.Settings.boolDebugLog
        AddHandler checkDebugLog.Click, AddressOf SaveSettings

        AddHandler buttonUpdate.Click, AddressOf ClickUpdateButton

        Dim gh As New Github
        Dim Update = gh.CheckForUpdate("topher-au", "HDT-Voice", HDTVoicePlugin.PluginVersion)
        If Not IsNothing(Update) Then
            buttonUpdate.Content = String.Format("Version {0} Now Available", Update.tag_name)
            buttonUpdate.Visibility = System.Windows.Visibility.Visible
        Else
            buttonUpdate.Visibility = System.Windows.Visibility.Hidden
        End If

    End Sub
    Sub SaveSettings()
        My.Settings.boolListenAtStartup = checkAutoStart.IsChecked
        My.Settings.boolShowNotification = checkShowNotification.IsChecked
        My.Settings.intNotificationPos = comboNotificationPos.SelectedIndex
        My.Settings.boolSmoothCursor = checkSmoothMouse.IsChecked
        My.Settings.boolDebugLog = checkDebugLog.IsChecked
        My.Settings.Save()
    End Sub
    Public Sub ClickUpdateButton()
        Process.Start("https://www.github.com/topher-au/HDT-Voice/releases/latest")
    End Sub

End Class
