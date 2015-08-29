<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class formEnterCommand
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
        Me.textCommand = New System.Windows.Forms.TextBox()
        Me.buttonSend = New System.Windows.Forms.Button()
        Me.SuspendLayout()
        '
        'textCommand
        '
        Me.textCommand.Location = New System.Drawing.Point(12, 12)
        Me.textCommand.Name = "textCommand"
        Me.textCommand.Size = New System.Drawing.Size(415, 20)
        Me.textCommand.TabIndex = 0
        '
        'buttonSend
        '
        Me.buttonSend.DialogResult = System.Windows.Forms.DialogResult.OK
        Me.buttonSend.Location = New System.Drawing.Point(433, 12)
        Me.buttonSend.Name = "buttonSend"
        Me.buttonSend.Size = New System.Drawing.Size(53, 21)
        Me.buttonSend.TabIndex = 1
        Me.buttonSend.Text = "Send"
        Me.buttonSend.UseVisualStyleBackColor = True
        '
        'formEnterCommand
        '
        Me.AcceptButton = Me.buttonSend
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(498, 42)
        Me.Controls.Add(Me.buttonSend)
        Me.Controls.Add(Me.textCommand)
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.Name = "formEnterCommand"
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen
        Me.Text = "Enter command"
        Me.TopMost = True
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub

    Friend WithEvents textCommand As System.Windows.Forms.TextBox
    Friend WithEvents buttonSend As System.Windows.Forms.Button
End Class
