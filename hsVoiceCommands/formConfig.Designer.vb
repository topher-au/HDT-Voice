<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class formConfig
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()> _
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    'Required by the Windows Form Designer
    Private components As System.ComponentModel.IContainer

    'NOTE: The following procedure is required by the Windows Form Designer
    'It can be modified using the Windows Form Designer.  
    'Do not modify it using the code editor.
    <System.Diagnostics.DebuggerStepThrough()> _
    Private Sub InitializeComponent()
        Me.groupRecog = New System.Windows.Forms.GroupBox()
        Me.numThreshold = New System.Windows.Forms.NumericUpDown()
        Me.Label1 = New System.Windows.Forms.Label()
        Me.buttonSave = New System.Windows.Forms.Button()
        Me.checkLast = New System.Windows.Forms.CheckBox()
        Me.groupSettings = New System.Windows.Forms.GroupBox()
        Me.checkQuick = New System.Windows.Forms.CheckBox()
        Me.comboStatusPos = New System.Windows.Forms.ComboBox()
        Me.checkShowStatus = New System.Windows.Forms.CheckBox()
        Me.checkDebugLog = New System.Windows.Forms.CheckBox()
        Me.groupRecog.SuspendLayout()
        CType(Me.numThreshold, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.groupSettings.SuspendLayout()
        Me.SuspendLayout()
        '
        'groupRecog
        '
        Me.groupRecog.Controls.Add(Me.numThreshold)
        Me.groupRecog.Controls.Add(Me.Label1)
        Me.groupRecog.Location = New System.Drawing.Point(8, 100)
        Me.groupRecog.Name = "groupRecog"
        Me.groupRecog.Size = New System.Drawing.Size(218, 42)
        Me.groupRecog.TabIndex = 3
        Me.groupRecog.TabStop = False
        Me.groupRecog.Text = "Recognition Settings"
        '
        'numThreshold
        '
        Me.numThreshold.Location = New System.Drawing.Point(170, 14)
        Me.numThreshold.Minimum = New Decimal(New Integer() {50, 0, 0, 0})
        Me.numThreshold.Name = "numThreshold"
        Me.numThreshold.Size = New System.Drawing.Size(42, 20)
        Me.numThreshold.TabIndex = 1
        Me.numThreshold.Value = New Decimal(New Integer() {90, 0, 0, 0})
        '
        'Label1
        '
        Me.Label1.AutoSize = True
        Me.Label1.Location = New System.Drawing.Point(6, 16)
        Me.Label1.Name = "Label1"
        Me.Label1.Size = New System.Drawing.Size(125, 13)
        Me.Label1.TabIndex = 0
        Me.Label1.Text = "Recognition Threshold %"
        '
        'buttonSave
        '
        Me.buttonSave.Location = New System.Drawing.Point(151, 148)
        Me.buttonSave.Name = "buttonSave"
        Me.buttonSave.Size = New System.Drawing.Size(75, 23)
        Me.buttonSave.TabIndex = 4
        Me.buttonSave.Text = "Save"
        Me.buttonSave.UseVisualStyleBackColor = True
        '
        'checkLast
        '
        Me.checkLast.AutoSize = True
        Me.checkLast.Location = New System.Drawing.Point(6, 44)
        Me.checkLast.Name = "checkLast"
        Me.checkLast.Size = New System.Drawing.Size(168, 17)
        Me.checkLast.TabIndex = 5
        Me.checkLast.Text = "Show last command executed"
        Me.checkLast.UseVisualStyleBackColor = True
        '
        'groupSettings
        '
        Me.groupSettings.Controls.Add(Me.checkQuick)
        Me.groupSettings.Controls.Add(Me.comboStatusPos)
        Me.groupSettings.Controls.Add(Me.checkShowStatus)
        Me.groupSettings.Controls.Add(Me.checkLast)
        Me.groupSettings.Location = New System.Drawing.Point(8, 3)
        Me.groupSettings.Name = "groupSettings"
        Me.groupSettings.Size = New System.Drawing.Size(218, 91)
        Me.groupSettings.TabIndex = 6
        Me.groupSettings.TabStop = False
        Me.groupSettings.Text = "HDT-Voice Settings"
        '
        'checkQuick
        '
        Me.checkQuick.AutoSize = True
        Me.checkQuick.Location = New System.Drawing.Point(6, 67)
        Me.checkQuick.Name = "checkQuick"
        Me.checkQuick.Size = New System.Drawing.Size(167, 17)
        Me.checkQuick.TabIndex = 8
        Me.checkQuick.Text = "Enable Quick Play commands"
        Me.checkQuick.UseVisualStyleBackColor = True
        '
        'comboStatusPos
        '
        Me.comboStatusPos.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.comboStatusPos.FormattingEnabled = True
        Me.comboStatusPos.Items.AddRange(New Object() {"Top Left", "Bottom Left", "Top Right", "Bottom Right"})
        Me.comboStatusPos.Location = New System.Drawing.Point(135, 17)
        Me.comboStatusPos.Name = "comboStatusPos"
        Me.comboStatusPos.Size = New System.Drawing.Size(77, 21)
        Me.comboStatusPos.TabIndex = 7
        '
        'checkShowStatus
        '
        Me.checkShowStatus.AutoSize = True
        Me.checkShowStatus.Location = New System.Drawing.Point(6, 21)
        Me.checkShowStatus.Name = "checkShowStatus"
        Me.checkShowStatus.Size = New System.Drawing.Size(104, 17)
        Me.checkShowStatus.TabIndex = 7
        Me.checkShowStatus.Text = "Show status text"
        Me.checkShowStatus.UseVisualStyleBackColor = True
        '
        'checkDebugLog
        '
        Me.checkDebugLog.AutoSize = True
        Me.checkDebugLog.Location = New System.Drawing.Point(14, 152)
        Me.checkDebugLog.Name = "checkDebugLog"
        Me.checkDebugLog.Size = New System.Drawing.Size(108, 17)
        Me.checkDebugLog.TabIndex = 7
        Me.checkDebugLog.Text = "Output debug log"
        Me.checkDebugLog.UseVisualStyleBackColor = True
        '
        'formConfig
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(236, 180)
        Me.Controls.Add(Me.groupSettings)
        Me.Controls.Add(Me.buttonSave)
        Me.Controls.Add(Me.checkDebugLog)
        Me.Controls.Add(Me.groupRecog)
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.Name = "formConfig"
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent
        Me.Text = "HDT-Voice Configuration"
        Me.groupRecog.ResumeLayout(False)
        Me.groupRecog.PerformLayout()
        CType(Me.numThreshold, System.ComponentModel.ISupportInitialize).EndInit()
        Me.groupSettings.ResumeLayout(False)
        Me.groupSettings.PerformLayout()
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub
    Friend WithEvents groupRecog As System.Windows.Forms.GroupBox
    Friend WithEvents numThreshold As System.Windows.Forms.NumericUpDown
    Friend WithEvents Label1 As System.Windows.Forms.Label
    Friend WithEvents buttonSave As System.Windows.Forms.Button
    Friend WithEvents checkLast As System.Windows.Forms.CheckBox
    Friend WithEvents groupSettings As System.Windows.Forms.GroupBox
    Friend WithEvents checkDebugLog As System.Windows.Forms.CheckBox
    Friend WithEvents comboStatusPos As System.Windows.Forms.ComboBox
    Friend WithEvents checkShowStatus As System.Windows.Forms.CheckBox
    Friend WithEvents checkQuick As System.Windows.Forms.CheckBox
End Class
