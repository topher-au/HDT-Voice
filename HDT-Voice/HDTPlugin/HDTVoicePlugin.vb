Imports System.Windows
Imports System.Windows.Controls
Imports System.Windows.Media

Imports Hearthstone_Deck_Tracker
Imports Hearthstone_Deck_Tracker.Plugins

Imports MahApps.Metro.Controls
Public Class HDTVoicePlugin
    Implements IPlugin

    Private createdSettings As Boolean = False
    Private configRecog As configRecog
    Private configMain As configMain

    ' HDT Plugin Implementation
    Public ReadOnly Property Author As String Implements IPlugin.Author
        Get
            Return "Chris Sheridan"
        End Get
    End Property
    Public ReadOnly Property ButtonText As String Implements IPlugin.ButtonText
        Get
            Return "Settings"
        End Get
    End Property
    Public ReadOnly Property Description As String Implements IPlugin.Description
        Get
            Return "Control Hearthstone using simple voice commands"
        End Get
    End Property
    Public ReadOnly Property MenuItem As MenuItem Implements IPlugin.MenuItem
        Get
            Dim pluginMenu As New MenuItem
            AddHandler pluginMenu.Click, AddressOf ClickMenuItem
            pluginMenu.Header = New String("HDT-Voice Settings")
            Return pluginMenu
        End Get
    End Property
    Public ReadOnly Property Name As String Implements IPlugin.Name
        Get
            Return "HDT-Voice"
        End Get
    End Property
    Public ReadOnly Property Version As Version Implements IPlugin.Version
        Get
            Return New Version(0, 7, 0)
        End Get
    End Property
    Public Sub OnButtonPress() Implements IPlugin.OnButtonPress

        Dim tV As TreeView = Nothing
        For Each t As TreeView In FindVisualChildren(Of TreeView)(Helper.OptionsMain)
            For Each ti As TreeViewItem In t.Items
                For Each tic As TreeViewItem In ti.Items
                    If Not tic.Tag Is Nothing Then
                        If tic.Tag.ToString = "HDTVOICESETTINGS" Then
                            ti.Items.Cast(Of TreeViewItem).First().IsSelected = True
                            ti.ExpandSubtree()
                            Return
                        End If
                    End If
                Next
            Next
        Next
    End Sub
    Public Sub OnLoad() Implements IPlugin.OnLoad
        createdSettings = False
        configMain = New configMain
        configRecog = New configRecog

        Dim voicePlugin As New HDTVoice
        voicePlugin.Load()
    End Sub
    Public Sub OnUnload() Implements IPlugin.OnUnload
        Return
    End Sub
    Public Sub OnUpdate() Implements IPlugin.OnUpdate
        If Not createdSettings Then
            CreateSettings()
        End If
    End Sub
    Public Sub ClickMenuItem()
        Dim optionsFlyout As Flyout
        For Each f As Flyout In FindVisualChildren(Of Flyout)(Helper.MainWindow)
            If Not f Is Nothing Then
                If f.Name = "FlyoutOptions" Then
                    optionsFlyout = f
                    f.IsOpen = True
                    OnButtonPress()
                    Return
                End If
            End If
        Next
    End Sub
    Public Sub CreateSettings()
        If IsNothing(Helper.OptionsMain) Then _
            Return

        Dim hdtMenuTree As TreeView = Nothing

        For Each t In FindVisualChildren(Of TreeView)(Helper.OptionsMain)
            If TypeOf t Is TreeView Then
                hdtMenuTree = t
                Exit For
            End If
        Next

        If hdtMenuTree Is Nothing Then Exit Sub

        Dim itemRoot As New TreeViewItem
        itemRoot.Header = "Voice Control"
        itemRoot.IsExpanded = True

        Dim itemSettings As New TreeViewItem
        itemSettings.Header = "Settings"
        itemSettings.Tag = "HDTVOICESETTINGS"
        AddHandler itemSettings.Selected, AddressOf ShowSettings
        itemRoot.Items.Add(itemSettings)

        Dim itemRecog As New TreeViewItem
        itemRecog.Header = "Speech Recognition"
        AddHandler itemRecog.Selected, AddressOf ShowRecog
        itemRoot.Items.Add(itemRecog)

        hdtMenuTree.Items.Add(itemRoot)

        createdSettings = True

    End Sub
    Public Sub ShowPane(pane As Object)
        Dim cc As ContentControl = Nothing
        For Each c As ContentControl In FindVisualChildren(Of ContentControl)(Helper.OptionsMain)
            Try
                If c.Name = "ContentControlOptions" Then
                    cc = c
                End If
            Catch ex As Exception

            End Try
            If cc IsNot Nothing Then
                cc.Content = pane
            End If
        Next

    End Sub
    Public Sub ShowSettings
        ShowPane(configMain)
    End Sub
    Public Sub ShowRecog()
        ShowPane(configRecog)
    End Sub

    Private Iterator Function FindVisualChildren(Of T As DependencyObject)(ByVal depObj As DependencyObject) As IEnumerable(Of T)
        If Not IsNothing(depObj) Then
            For i = 0 To VisualTreeHelper.GetChildrenCount(depObj) - 1
                Dim child = VisualTreeHelper.GetChild(depObj, i)
                If Not IsNothing(child) And TypeOf child Is T Then
                    Yield child
                End If

                For Each childOfChild As T In FindVisualChildren(Of T)(child)
                    Yield childOfChild
                Next
            Next
        End If
    End Function

End Class