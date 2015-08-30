Imports System.Windows
Imports System.Windows.Controls
Imports System.Windows.Media

Imports Hearthstone_Deck_Tracker
Imports Hearthstone_Deck_Tracker.Plugins
Imports System.Windows.Controls.Primitives
Imports System.Windows.Documents



Imports MahApps.Metro.Controls
Public Class HDTVoicePlugin
    Implements IPlugin

    'Some code is based on code found in HDT Compatibility Window

    Public Shared PluginVersion As New Version(0, 8, 0)

    Private createdSettings, createdUpdateNews As Boolean
    Private configRecog As configRecog
    Private configMain As configMain
    Public voicePlugin As HDTVoice
    Private hdtNewsPanel As Object

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
            Return PluginVersion
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
        createdUpdateNews = False
        configMain = New configMain
        configRecog = New configRecog
        voicePlugin = New HDTVoice
        voicePlugin.Load()
    End Sub
    Public Sub OnUnload() Implements IPlugin.OnUnload
        My.Settings.Save()
        If Not voicePlugin Is Nothing Then
            voicePlugin.Unload()
            voicePlugin = Nothing
        End If

        For Each t In FindVisualChildren(Of TreeView)(Helper.OptionsMain)
            For Each ti As TreeViewItem In FindVisualChildren(Of TreeViewItem)(t)
                If ti.Header = "Voice Control" Then
                    t.Items.Remove(ti)
                    Return
                End If
            Next
        Next
    End Sub
    Public Sub OnUpdate() Implements IPlugin.OnUpdate
        If Not createdSettings Then
            CreateSettings()
        End If
        If Not createdUpdateNews Then
            CreateUpdateNews()

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
    Public Sub CreateUpdateNews()
        ' Checks for an update and adds a news item if there wasn't one
        Dim gh As New Github
        Logger.WriteLine("Checking for HDT-Voice update...")
        Dim newVer = gh.CheckForUpdate("topher-au", "HDT-Voice", HDTVoicePlugin.PluginVersion)
        If Not IsNothing(newVer) Then
            Logger.WriteLine("New version found!")
            Dim newsBar As StatusBar = Nothing

            Dim TopRow As RowDefinition = Nothing
            Dim StatusBarNews As StatusBar = Nothing

            For Each item In FindVisualChildren(Of Grid)(Helper.MainWindow)
                For Each rd In item.RowDefinitions
                    If Not IsNothing(rd.Name) Then
                        If Not rd.Name.Trim = String.Empty Then
                            If rd.Name = "TopRow" Then
                                TopRow = rd
                            End If
                        End If
                    End If
                Next

            Next

            For Each item In FindVisualChildren(Of StatusBar)(Helper.MainWindow)
                If Not IsNothing(item.Name) Then
                    If item.Name = "StatusBarNews" Then
                        StatusBarNews = item
                    End If
                End If
            Next

            If Not IsNothing(TopRow) And Not IsNothing(StatusBarNews) Then
                If StatusBarNews.Visibility = Visibility.Collapsed Then
                    TopRow.Height = New GridLength(30)
                    StatusBarNews.Visibility = Visibility.Visible

                    Dim newsLink As New Hyperlink
                    newsLink.NavigateUri = New Uri("https://www.github.com/topher-au/HDT-Voice/releases/latest")
                    newsLink.Inlines.Add(New Run(String.Format("HDT-Voice version {0} is available now!", newVer.tag_name)))
                    newsLink.Foreground = New SolidColorBrush(Colors.White)
                    AddHandler newsLink.RequestNavigate, Sub()
                                                             Process.Start(newsLink.NavigateUri.ToString)
                                                             Dim buttonClose As Button = StatusBarNews.Items.Item(5).Content
                                                             Dim clickEvent As New RoutedEventArgs
                                                             clickEvent.RoutedEvent = Button.ClickEvent
                                                             buttonClose.RaiseEvent(clickEvent)
                                                         End Sub

                    Dim voiceBlock As New TextBlock
                    voiceBlock.Inlines.Add(newsLink)

                    StatusBarNews.Items.Item(1) = Nothing
                    StatusBarNews.Items.Item(2).Content = voiceBlock
                    StatusBarNews.Items.Item(3) = Nothing
                    StatusBarNews.Items.Item(4) = Nothing
                    StatusBarNews.Margin = New Thickness(1)
                End If
                createdUpdateNews = True
            End If

            configMain.buttonUpdate.Content = String.Format("Version {0} Now Available", newVer.tag_name)
            configMain.buttonUpdate.Visibility = System.Windows.Visibility.Visible
        Else
            Logger.WriteLine("No newer version found.")
            configMain.buttonUpdate.Visibility = System.Windows.Visibility.Hidden
            createdUpdateNews = True
        End If


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