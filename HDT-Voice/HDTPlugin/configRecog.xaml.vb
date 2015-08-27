Public Class configRecog

    Sub New()
        InitializeComponent()

        sliderThreshold.Value = My.Settings.intThreshold
        AddHandler sliderThreshold.ValueChanged, AddressOf SliderChanged

        labelThreshold.Content = String.Format("{0}%", My.Settings.intThreshold)

        checkRecognizeSound.IsChecked = My.Settings.boolRecognizedAudio
        AddHandler checkRecognizeSound.Checked, AddressOf SaveSettings

        checkQuickPlay.IsChecked = My.Settings.boolQuickPlay
        AddHandler checkQuickPlay.Checked, AddressOf SaveSettings

        radioHotToggle.IsChecked = Not My.Settings.boolToggleOrPtt
        AddHandler radioHotToggle.Checked, AddressOf SaveSettings

        radioHotPush.IsChecked = My.Settings.boolToggleOrPtt
        AddHandler radioHotPush.Checked, AddressOf SaveSettings
    End Sub
    Sub SliderChanged()
        My.Settings.intThreshold = sliderThreshold.Value
        My.Settings.Save()
        labelThreshold.Content = String.Format("{0}%", My.Settings.intThreshold)
    End Sub
    Sub SaveSettings()
        My.Settings.boolToggleOrPtt = radioHotPush.IsChecked
        My.Settings.boolQuickPlay = checkQuickPlay.IsChecked
        My.Settings.boolRecognizedAudio = checkRecognizeSound.IsChecked
        My.Settings.intThreshold = sliderThreshold.Value
        My.Settings.Save()
    End Sub
End Class
