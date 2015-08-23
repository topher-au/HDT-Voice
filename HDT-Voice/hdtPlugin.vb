Imports System
Imports System.Windows.Controls
Imports MahApps.Metro
Imports Hearthstone_Deck_Tracker.Plugins
Public Class hdtPlugin
    Implements IPlugin

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
            AddHandler pluginMenu.Click, AddressOf OnButtonPress
            pluginMenu.Header = New String("HDT-Voice settings...")
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
        Dim fmc = New metroConfig
        fmc.ShowDialog() ' Show configuration dialog
        Return
    End Sub

    Public Sub OnLoad() Implements IPlugin.OnLoad
        Dim voicePlugin As New hdtVoice
        voicePlugin.Load()
    End Sub

    Public Sub OnUnload() Implements IPlugin.OnUnload
        Return
    End Sub

    Public Sub OnUpdate() Implements IPlugin.OnUpdate
        Return
    End Sub
End Class
