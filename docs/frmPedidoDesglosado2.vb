Public Class frmPedidoDesglosado2
    Inherits DevExpress.XtraEditors.XtraForm
    Dim p(30) As Long
    Dim Pedidos_DetailsID As Guid
    Private mData As DataView

    Public BDesglosado As Boolean
    Friend WithEvents GridEX1 As Janus.Windows.GridEX.GridEX
    Friend WithEvents M21 As System.Windows.Forms.Label
    Friend WithEvents T21 As System.Windows.Forms.Label
    Friend WithEvents P21 As System.Windows.Forms.Label
    Friend WithEvents Label21 As System.Windows.Forms.Label
    Friend WithEvents M20 As System.Windows.Forms.Label
    Friend WithEvents T20 As System.Windows.Forms.Label
    Friend WithEvents P20 As System.Windows.Forms.Label
    Friend WithEvents Label20 As System.Windows.Forms.Label
    Friend WithEvents M19 As System.Windows.Forms.Label
    Friend WithEvents T19 As System.Windows.Forms.Label
    Friend WithEvents P19 As System.Windows.Forms.Label
    Friend WithEvents Label19 As System.Windows.Forms.Label
    Friend WithEvents M18 As System.Windows.Forms.Label
    Friend WithEvents T18 As System.Windows.Forms.Label
    Friend WithEvents P18 As System.Windows.Forms.Label
    Friend WithEvents Label18 As System.Windows.Forms.Label
    Friend WithEvents M17 As System.Windows.Forms.Label
    Friend WithEvents T17 As System.Windows.Forms.Label
    Friend WithEvents P17 As System.Windows.Forms.Label
    Friend WithEvents Label17 As System.Windows.Forms.Label
    Friend WithEvents M16 As System.Windows.Forms.Label
    Friend WithEvents T16 As System.Windows.Forms.Label
    Friend WithEvents P16 As System.Windows.Forms.Label
    Friend WithEvents Label16 As System.Windows.Forms.Label
    Friend WithEvents TCantidad As System.Windows.Forms.Label
    Friend WithEvents PCantidad As System.Windows.Forms.Label
    Friend WithEvents LabelCantidad As System.Windows.Forms.Label
    Friend WithEvents rbHormas As System.Windows.Forms.RadioButton
    Friend WithEvents chkPorTallas As RadioButton
    Public BExitoso As Boolean
#Region " Windows Form Designer generated code "

    Public Sub New()
        MyBase.New()

        'This call is required by the Windows Form Designer.
        InitializeComponent()

        'Add any initialization after the InitializeComponent() call
        Inicializar()
    End Sub

    'Form overrides dispose to clean up the component list.
    Protected Overloads Overrides Sub Dispose(ByVal disposing As Boolean)
        If disposing Then
            If Not (components Is Nothing) Then
                components.Dispose()
            End If
        End If
        MyBase.Dispose(disposing)
    End Sub

    'Required by the Windows Form Designer
    Private components As System.ComponentModel.IContainer

    'NOTE: The following procedure is required by the Windows Form Designer
    'It can be modified using the Windows Form Designer.  
    'Do not modify it using the code editor.
    Friend WithEvents M15 As System.Windows.Forms.Label
    Friend WithEvents M14 As System.Windows.Forms.Label
    Friend WithEvents M13 As System.Windows.Forms.Label
    Friend WithEvents M12 As System.Windows.Forms.Label
    Friend WithEvents M11 As System.Windows.Forms.Label
    Friend WithEvents M10 As System.Windows.Forms.Label
    Friend WithEvents M9 As System.Windows.Forms.Label
    Friend WithEvents M8 As System.Windows.Forms.Label
    Friend WithEvents M7 As System.Windows.Forms.Label
    Friend WithEvents M6 As System.Windows.Forms.Label
    Friend WithEvents M5 As System.Windows.Forms.Label
    Friend WithEvents M4 As System.Windows.Forms.Label
    Friend WithEvents M3 As System.Windows.Forms.Label
    Friend WithEvents M2 As System.Windows.Forms.Label
    Friend WithEvents M1 As System.Windows.Forms.Label
    Friend WithEvents T15 As System.Windows.Forms.Label
    Friend WithEvents T14 As System.Windows.Forms.Label
    Friend WithEvents T13 As System.Windows.Forms.Label
    Friend WithEvents T12 As System.Windows.Forms.Label
    Friend WithEvents T11 As System.Windows.Forms.Label
    Friend WithEvents T10 As System.Windows.Forms.Label
    Friend WithEvents T9 As System.Windows.Forms.Label
    Friend WithEvents T8 As System.Windows.Forms.Label
    Friend WithEvents T7 As System.Windows.Forms.Label
    Friend WithEvents T6 As System.Windows.Forms.Label
    Friend WithEvents T5 As System.Windows.Forms.Label
    Friend WithEvents T4 As System.Windows.Forms.Label
    Friend WithEvents T3 As System.Windows.Forms.Label
    Friend WithEvents T2 As System.Windows.Forms.Label
    Friend WithEvents T1 As System.Windows.Forms.Label
    Friend WithEvents GroupBox1 As System.Windows.Forms.GroupBox
    Friend WithEvents chkProgramacionProporcional As System.Windows.Forms.RadioButton
    Friend WithEvents chkProgramacionDistribuida As System.Windows.Forms.RadioButton
    Friend WithEvents chkCantidadDeLotes As System.Windows.Forms.RadioButton
    Friend WithEvents chkLoteTamaño As System.Windows.Forms.RadioButton
    Friend WithEvents cmdGenerar As DevExpress.XtraEditors.SimpleButton
    Friend WithEvents txtCantidadDeLotes As System.Windows.Forms.TextBox
    Friend WithEvents txtLoteTamaño As System.Windows.Forms.TextBox
    Friend WithEvents Label32 As System.Windows.Forms.Label
    Friend WithEvents Label31 As System.Windows.Forms.Label
    Friend WithEvents SimpleButton2 As DevExpress.XtraEditors.SimpleButton
    Friend WithEvents SimpleButton1 As DevExpress.XtraEditors.SimpleButton
    Friend WithEvents P15 As System.Windows.Forms.Label
    Friend WithEvents P14 As System.Windows.Forms.Label
    Friend WithEvents P13 As System.Windows.Forms.Label
    Friend WithEvents P12 As System.Windows.Forms.Label
    Friend WithEvents P11 As System.Windows.Forms.Label
    Friend WithEvents P10 As System.Windows.Forms.Label
    Friend WithEvents P9 As System.Windows.Forms.Label
    Friend WithEvents P8 As System.Windows.Forms.Label
    Friend WithEvents P7 As System.Windows.Forms.Label
    Friend WithEvents P6 As System.Windows.Forms.Label
    Friend WithEvents P5 As System.Windows.Forms.Label
    Friend WithEvents P4 As System.Windows.Forms.Label
    Friend WithEvents P3 As System.Windows.Forms.Label
    Friend WithEvents P2 As System.Windows.Forms.Label
    Friend WithEvents P1 As System.Windows.Forms.Label
    Friend WithEvents Label15 As System.Windows.Forms.Label
    Friend WithEvents Label14 As System.Windows.Forms.Label
    Friend WithEvents Label13 As System.Windows.Forms.Label
    Friend WithEvents Label12 As System.Windows.Forms.Label
    Friend WithEvents Label11 As System.Windows.Forms.Label
    Friend WithEvents Label10 As System.Windows.Forms.Label
    Friend WithEvents Label9 As System.Windows.Forms.Label
    Friend WithEvents Label8 As System.Windows.Forms.Label
    Friend WithEvents Label7 As System.Windows.Forms.Label
    Friend WithEvents Label6 As System.Windows.Forms.Label
    Friend WithEvents Label5 As System.Windows.Forms.Label
    Friend WithEvents Label4 As System.Windows.Forms.Label
    Friend WithEvents Label3 As System.Windows.Forms.Label
    Friend WithEvents Label2 As System.Windows.Forms.Label
    Friend WithEvents Label1 As System.Windows.Forms.Label
    Friend WithEvents SimpleButton3 As DevExpress.XtraEditors.SimpleButton
    Friend WithEvents SimpleButton4 As DevExpress.XtraEditors.SimpleButton
    Friend WithEvents SqlDataAdapter1 As System.Data.SqlClient.SqlDataAdapter
    Friend WithEvents SqlSelectCommand1 As System.Data.SqlClient.SqlCommand
    Friend WithEvents SqlInsertCommand1 As System.Data.SqlClient.SqlCommand
    Friend WithEvents SqlUpdateCommand1 As System.Data.SqlClient.SqlCommand
    Friend WithEvents SqlDeleteCommand1 As System.Data.SqlClient.SqlCommand
    Friend WithEvents SqlConnection1 As System.Data.SqlClient.SqlConnection
    Friend WithEvents Base1 As Janus.AdvancedSample.Base
    Friend WithEvents Label16_ As System.Windows.Forms.Label
    Friend WithEvents Label17_ As System.Windows.Forms.Label
    Friend WithEvents Label18_ As System.Windows.Forms.Label
    Friend WithEvents Label19_ As System.Windows.Forms.Label
    <System.Diagnostics.DebuggerStepThrough()> Private Sub InitializeComponent()
        Dim GridEXLayout1 As Janus.Windows.GridEX.GridEXLayout = New Janus.Windows.GridEX.GridEXLayout()
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(frmPedidoDesglosado2))
        Me.M15 = New System.Windows.Forms.Label()
        Me.M14 = New System.Windows.Forms.Label()
        Me.M13 = New System.Windows.Forms.Label()
        Me.M12 = New System.Windows.Forms.Label()
        Me.M11 = New System.Windows.Forms.Label()
        Me.M10 = New System.Windows.Forms.Label()
        Me.M9 = New System.Windows.Forms.Label()
        Me.M8 = New System.Windows.Forms.Label()
        Me.M7 = New System.Windows.Forms.Label()
        Me.M6 = New System.Windows.Forms.Label()
        Me.M5 = New System.Windows.Forms.Label()
        Me.M4 = New System.Windows.Forms.Label()
        Me.M3 = New System.Windows.Forms.Label()
        Me.M2 = New System.Windows.Forms.Label()
        Me.M1 = New System.Windows.Forms.Label()
        Me.T15 = New System.Windows.Forms.Label()
        Me.T14 = New System.Windows.Forms.Label()
        Me.T13 = New System.Windows.Forms.Label()
        Me.T12 = New System.Windows.Forms.Label()
        Me.T11 = New System.Windows.Forms.Label()
        Me.T10 = New System.Windows.Forms.Label()
        Me.T9 = New System.Windows.Forms.Label()
        Me.T8 = New System.Windows.Forms.Label()
        Me.T7 = New System.Windows.Forms.Label()
        Me.T6 = New System.Windows.Forms.Label()
        Me.T5 = New System.Windows.Forms.Label()
        Me.T4 = New System.Windows.Forms.Label()
        Me.T3 = New System.Windows.Forms.Label()
        Me.T2 = New System.Windows.Forms.Label()
        Me.T1 = New System.Windows.Forms.Label()
        Me.GroupBox1 = New System.Windows.Forms.GroupBox()
        Me.chkPorTallas = New System.Windows.Forms.RadioButton()
        Me.rbHormas = New System.Windows.Forms.RadioButton()
        Me.chkProgramacionProporcional = New System.Windows.Forms.RadioButton()
        Me.chkProgramacionDistribuida = New System.Windows.Forms.RadioButton()
        Me.chkCantidadDeLotes = New System.Windows.Forms.RadioButton()
        Me.chkLoteTamaño = New System.Windows.Forms.RadioButton()
        Me.cmdGenerar = New DevExpress.XtraEditors.SimpleButton()
        Me.txtCantidadDeLotes = New System.Windows.Forms.TextBox()
        Me.txtLoteTamaño = New System.Windows.Forms.TextBox()
        Me.Label32 = New System.Windows.Forms.Label()
        Me.Label31 = New System.Windows.Forms.Label()
        Me.SimpleButton2 = New DevExpress.XtraEditors.SimpleButton()
        Me.SimpleButton1 = New DevExpress.XtraEditors.SimpleButton()
        Me.P15 = New System.Windows.Forms.Label()
        Me.P14 = New System.Windows.Forms.Label()
        Me.P13 = New System.Windows.Forms.Label()
        Me.P12 = New System.Windows.Forms.Label()
        Me.P11 = New System.Windows.Forms.Label()
        Me.P10 = New System.Windows.Forms.Label()
        Me.P9 = New System.Windows.Forms.Label()
        Me.P8 = New System.Windows.Forms.Label()
        Me.P7 = New System.Windows.Forms.Label()
        Me.P6 = New System.Windows.Forms.Label()
        Me.P5 = New System.Windows.Forms.Label()
        Me.P4 = New System.Windows.Forms.Label()
        Me.P3 = New System.Windows.Forms.Label()
        Me.P2 = New System.Windows.Forms.Label()
        Me.P1 = New System.Windows.Forms.Label()
        Me.Label15 = New System.Windows.Forms.Label()
        Me.Label14 = New System.Windows.Forms.Label()
        Me.Label13 = New System.Windows.Forms.Label()
        Me.Label12 = New System.Windows.Forms.Label()
        Me.Label11 = New System.Windows.Forms.Label()
        Me.Label10 = New System.Windows.Forms.Label()
        Me.Label9 = New System.Windows.Forms.Label()
        Me.Label8 = New System.Windows.Forms.Label()
        Me.Label7 = New System.Windows.Forms.Label()
        Me.Label6 = New System.Windows.Forms.Label()
        Me.Label5 = New System.Windows.Forms.Label()
        Me.Label4 = New System.Windows.Forms.Label()
        Me.Label3 = New System.Windows.Forms.Label()
        Me.Label2 = New System.Windows.Forms.Label()
        Me.Label1 = New System.Windows.Forms.Label()
        Me.Base1 = New Janus.AdvancedSample.Base()
        Me.SimpleButton3 = New DevExpress.XtraEditors.SimpleButton()
        Me.SimpleButton4 = New DevExpress.XtraEditors.SimpleButton()
        Me.SqlDataAdapter1 = New System.Data.SqlClient.SqlDataAdapter()
        Me.SqlDeleteCommand1 = New System.Data.SqlClient.SqlCommand()
        Me.SqlConnection1 = New System.Data.SqlClient.SqlConnection()
        Me.SqlInsertCommand1 = New System.Data.SqlClient.SqlCommand()
        Me.SqlSelectCommand1 = New System.Data.SqlClient.SqlCommand()
        Me.SqlUpdateCommand1 = New System.Data.SqlClient.SqlCommand()
        Me.Label16_ = New System.Windows.Forms.Label()
        Me.Label17_ = New System.Windows.Forms.Label()
        Me.Label18_ = New System.Windows.Forms.Label()
        Me.Label19_ = New System.Windows.Forms.Label()
        Me.GridEX1 = New Janus.Windows.GridEX.GridEX()
        Me.M21 = New System.Windows.Forms.Label()
        Me.T21 = New System.Windows.Forms.Label()
        Me.P21 = New System.Windows.Forms.Label()
        Me.Label21 = New System.Windows.Forms.Label()
        Me.M20 = New System.Windows.Forms.Label()
        Me.T20 = New System.Windows.Forms.Label()
        Me.P20 = New System.Windows.Forms.Label()
        Me.Label20 = New System.Windows.Forms.Label()
        Me.M19 = New System.Windows.Forms.Label()
        Me.T19 = New System.Windows.Forms.Label()
        Me.P19 = New System.Windows.Forms.Label()
        Me.Label19 = New System.Windows.Forms.Label()
        Me.M18 = New System.Windows.Forms.Label()
        Me.T18 = New System.Windows.Forms.Label()
        Me.P18 = New System.Windows.Forms.Label()
        Me.Label18 = New System.Windows.Forms.Label()
        Me.M17 = New System.Windows.Forms.Label()
        Me.T17 = New System.Windows.Forms.Label()
        Me.P17 = New System.Windows.Forms.Label()
        Me.Label17 = New System.Windows.Forms.Label()
        Me.M16 = New System.Windows.Forms.Label()
        Me.T16 = New System.Windows.Forms.Label()
        Me.P16 = New System.Windows.Forms.Label()
        Me.Label16 = New System.Windows.Forms.Label()
        Me.TCantidad = New System.Windows.Forms.Label()
        Me.PCantidad = New System.Windows.Forms.Label()
        Me.LabelCantidad = New System.Windows.Forms.Label()
        Me.GroupBox1.SuspendLayout()
        CType(Me.Base1, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.GridEX1, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SuspendLayout()
        '
        'M15
        '
        Me.M15.BackColor = System.Drawing.Color.Gold
        Me.M15.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.M15.Font = New System.Drawing.Font("Arial Narrow", 8.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.M15.Location = New System.Drawing.Point(560, 64)
        Me.M15.Name = "M15"
        Me.M15.Size = New System.Drawing.Size(32, 24)
        Me.M15.TabIndex = 189
        Me.M15.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        '
        'M14
        '
        Me.M14.BackColor = System.Drawing.Color.Gold
        Me.M14.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.M14.Font = New System.Drawing.Font("Arial Narrow", 8.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.M14.Location = New System.Drawing.Point(528, 64)
        Me.M14.Name = "M14"
        Me.M14.Size = New System.Drawing.Size(32, 24)
        Me.M14.TabIndex = 188
        Me.M14.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        '
        'M13
        '
        Me.M13.BackColor = System.Drawing.Color.Gold
        Me.M13.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.M13.Font = New System.Drawing.Font("Arial Narrow", 8.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.M13.Location = New System.Drawing.Point(496, 64)
        Me.M13.Name = "M13"
        Me.M13.Size = New System.Drawing.Size(32, 24)
        Me.M13.TabIndex = 187
        Me.M13.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        '
        'M12
        '
        Me.M12.BackColor = System.Drawing.Color.Gold
        Me.M12.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.M12.Font = New System.Drawing.Font("Arial Narrow", 8.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.M12.Location = New System.Drawing.Point(464, 64)
        Me.M12.Name = "M12"
        Me.M12.Size = New System.Drawing.Size(32, 24)
        Me.M12.TabIndex = 186
        Me.M12.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        '
        'M11
        '
        Me.M11.BackColor = System.Drawing.Color.Gold
        Me.M11.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.M11.Font = New System.Drawing.Font("Arial Narrow", 8.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.M11.Location = New System.Drawing.Point(432, 64)
        Me.M11.Name = "M11"
        Me.M11.Size = New System.Drawing.Size(32, 24)
        Me.M11.TabIndex = 185
        Me.M11.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        '
        'M10
        '
        Me.M10.BackColor = System.Drawing.Color.Gold
        Me.M10.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.M10.Font = New System.Drawing.Font("Arial Narrow", 8.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.M10.Location = New System.Drawing.Point(400, 64)
        Me.M10.Name = "M10"
        Me.M10.Size = New System.Drawing.Size(32, 24)
        Me.M10.TabIndex = 184
        Me.M10.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        '
        'M9
        '
        Me.M9.BackColor = System.Drawing.Color.Gold
        Me.M9.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.M9.Font = New System.Drawing.Font("Arial Narrow", 8.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.M9.Location = New System.Drawing.Point(368, 64)
        Me.M9.Name = "M9"
        Me.M9.Size = New System.Drawing.Size(32, 24)
        Me.M9.TabIndex = 183
        Me.M9.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        '
        'M8
        '
        Me.M8.BackColor = System.Drawing.Color.Gold
        Me.M8.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.M8.Font = New System.Drawing.Font("Arial Narrow", 8.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.M8.Location = New System.Drawing.Point(336, 64)
        Me.M8.Name = "M8"
        Me.M8.Size = New System.Drawing.Size(32, 24)
        Me.M8.TabIndex = 182
        Me.M8.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        '
        'M7
        '
        Me.M7.BackColor = System.Drawing.Color.Gold
        Me.M7.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.M7.Font = New System.Drawing.Font("Arial Narrow", 8.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.M7.Location = New System.Drawing.Point(304, 64)
        Me.M7.Name = "M7"
        Me.M7.Size = New System.Drawing.Size(32, 24)
        Me.M7.TabIndex = 181
        Me.M7.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        '
        'M6
        '
        Me.M6.BackColor = System.Drawing.Color.Gold
        Me.M6.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.M6.Font = New System.Drawing.Font("Arial Narrow", 8.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.M6.Location = New System.Drawing.Point(272, 64)
        Me.M6.Name = "M6"
        Me.M6.Size = New System.Drawing.Size(32, 24)
        Me.M6.TabIndex = 180
        Me.M6.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        '
        'M5
        '
        Me.M5.BackColor = System.Drawing.Color.Gold
        Me.M5.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.M5.Font = New System.Drawing.Font("Arial Narrow", 8.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.M5.Location = New System.Drawing.Point(240, 64)
        Me.M5.Name = "M5"
        Me.M5.Size = New System.Drawing.Size(32, 24)
        Me.M5.TabIndex = 179
        Me.M5.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        '
        'M4
        '
        Me.M4.BackColor = System.Drawing.Color.Gold
        Me.M4.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.M4.Font = New System.Drawing.Font("Arial Narrow", 8.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.M4.Location = New System.Drawing.Point(208, 64)
        Me.M4.Name = "M4"
        Me.M4.Size = New System.Drawing.Size(32, 24)
        Me.M4.TabIndex = 178
        Me.M4.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        '
        'M3
        '
        Me.M3.BackColor = System.Drawing.Color.Gold
        Me.M3.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.M3.Font = New System.Drawing.Font("Arial Narrow", 8.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.M3.Location = New System.Drawing.Point(176, 64)
        Me.M3.Name = "M3"
        Me.M3.Size = New System.Drawing.Size(32, 24)
        Me.M3.TabIndex = 177
        Me.M3.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        '
        'M2
        '
        Me.M2.BackColor = System.Drawing.Color.Gold
        Me.M2.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.M2.Font = New System.Drawing.Font("Arial Narrow", 8.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.M2.Location = New System.Drawing.Point(144, 64)
        Me.M2.Name = "M2"
        Me.M2.Size = New System.Drawing.Size(32, 24)
        Me.M2.TabIndex = 176
        Me.M2.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        '
        'M1
        '
        Me.M1.BackColor = System.Drawing.Color.Gold
        Me.M1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.M1.Font = New System.Drawing.Font("Arial Narrow", 8.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.M1.Location = New System.Drawing.Point(112, 64)
        Me.M1.Name = "M1"
        Me.M1.Size = New System.Drawing.Size(32, 24)
        Me.M1.TabIndex = 175
        Me.M1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        '
        'T15
        '
        Me.T15.BackColor = System.Drawing.Color.White
        Me.T15.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.T15.Font = New System.Drawing.Font("Arial Narrow", 8.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.T15.Location = New System.Drawing.Point(560, 136)
        Me.T15.Name = "T15"
        Me.T15.Size = New System.Drawing.Size(32, 24)
        Me.T15.TabIndex = 173
        Me.T15.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        '
        'T14
        '
        Me.T14.BackColor = System.Drawing.Color.White
        Me.T14.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.T14.Font = New System.Drawing.Font("Arial Narrow", 8.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.T14.Location = New System.Drawing.Point(528, 136)
        Me.T14.Name = "T14"
        Me.T14.Size = New System.Drawing.Size(32, 24)
        Me.T14.TabIndex = 172
        Me.T14.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        '
        'T13
        '
        Me.T13.BackColor = System.Drawing.Color.White
        Me.T13.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.T13.Font = New System.Drawing.Font("Arial Narrow", 8.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.T13.Location = New System.Drawing.Point(496, 136)
        Me.T13.Name = "T13"
        Me.T13.Size = New System.Drawing.Size(32, 24)
        Me.T13.TabIndex = 171
        Me.T13.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        '
        'T12
        '
        Me.T12.BackColor = System.Drawing.Color.White
        Me.T12.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.T12.Font = New System.Drawing.Font("Arial Narrow", 8.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.T12.Location = New System.Drawing.Point(464, 136)
        Me.T12.Name = "T12"
        Me.T12.Size = New System.Drawing.Size(32, 24)
        Me.T12.TabIndex = 170
        Me.T12.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        '
        'T11
        '
        Me.T11.BackColor = System.Drawing.Color.White
        Me.T11.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.T11.Font = New System.Drawing.Font("Arial Narrow", 8.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.T11.Location = New System.Drawing.Point(432, 136)
        Me.T11.Name = "T11"
        Me.T11.Size = New System.Drawing.Size(32, 24)
        Me.T11.TabIndex = 169
        Me.T11.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        '
        'T10
        '
        Me.T10.BackColor = System.Drawing.Color.White
        Me.T10.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.T10.Font = New System.Drawing.Font("Arial Narrow", 8.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.T10.Location = New System.Drawing.Point(400, 136)
        Me.T10.Name = "T10"
        Me.T10.Size = New System.Drawing.Size(32, 24)
        Me.T10.TabIndex = 168
        Me.T10.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        '
        'T9
        '
        Me.T9.BackColor = System.Drawing.Color.White
        Me.T9.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.T9.Font = New System.Drawing.Font("Arial Narrow", 8.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.T9.Location = New System.Drawing.Point(368, 136)
        Me.T9.Name = "T9"
        Me.T9.Size = New System.Drawing.Size(32, 24)
        Me.T9.TabIndex = 167
        Me.T9.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        '
        'T8
        '
        Me.T8.BackColor = System.Drawing.Color.White
        Me.T8.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.T8.Font = New System.Drawing.Font("Arial Narrow", 8.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.T8.Location = New System.Drawing.Point(336, 136)
        Me.T8.Name = "T8"
        Me.T8.Size = New System.Drawing.Size(32, 24)
        Me.T8.TabIndex = 166
        Me.T8.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        '
        'T7
        '
        Me.T7.BackColor = System.Drawing.Color.White
        Me.T7.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.T7.Font = New System.Drawing.Font("Arial Narrow", 8.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.T7.Location = New System.Drawing.Point(304, 136)
        Me.T7.Name = "T7"
        Me.T7.Size = New System.Drawing.Size(32, 24)
        Me.T7.TabIndex = 165
        Me.T7.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        '
        'T6
        '
        Me.T6.BackColor = System.Drawing.Color.White
        Me.T6.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.T6.Font = New System.Drawing.Font("Arial Narrow", 8.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.T6.Location = New System.Drawing.Point(272, 136)
        Me.T6.Name = "T6"
        Me.T6.Size = New System.Drawing.Size(32, 24)
        Me.T6.TabIndex = 164
        Me.T6.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        '
        'T5
        '
        Me.T5.BackColor = System.Drawing.Color.White
        Me.T5.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.T5.Font = New System.Drawing.Font("Arial Narrow", 8.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.T5.Location = New System.Drawing.Point(240, 136)
        Me.T5.Name = "T5"
        Me.T5.Size = New System.Drawing.Size(32, 24)
        Me.T5.TabIndex = 163
        Me.T5.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        '
        'T4
        '
        Me.T4.BackColor = System.Drawing.Color.White
        Me.T4.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.T4.Font = New System.Drawing.Font("Arial Narrow", 8.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.T4.Location = New System.Drawing.Point(208, 136)
        Me.T4.Name = "T4"
        Me.T4.Size = New System.Drawing.Size(32, 24)
        Me.T4.TabIndex = 162
        Me.T4.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        '
        'T3
        '
        Me.T3.BackColor = System.Drawing.Color.White
        Me.T3.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.T3.Font = New System.Drawing.Font("Arial Narrow", 8.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.T3.Location = New System.Drawing.Point(176, 136)
        Me.T3.Name = "T3"
        Me.T3.Size = New System.Drawing.Size(32, 24)
        Me.T3.TabIndex = 161
        Me.T3.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        '
        'T2
        '
        Me.T2.BackColor = System.Drawing.Color.White
        Me.T2.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.T2.Font = New System.Drawing.Font("Arial Narrow", 8.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.T2.Location = New System.Drawing.Point(144, 136)
        Me.T2.Name = "T2"
        Me.T2.Size = New System.Drawing.Size(32, 24)
        Me.T2.TabIndex = 160
        Me.T2.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        '
        'T1
        '
        Me.T1.BackColor = System.Drawing.Color.White
        Me.T1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.T1.Font = New System.Drawing.Font("Arial Narrow", 8.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.T1.Location = New System.Drawing.Point(112, 136)
        Me.T1.Name = "T1"
        Me.T1.Size = New System.Drawing.Size(32, 24)
        Me.T1.TabIndex = 159
        Me.T1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        '
        'GroupBox1
        '
        Me.GroupBox1.BackColor = System.Drawing.Color.Orange
        Me.GroupBox1.Controls.Add(Me.chkPorTallas)
        Me.GroupBox1.Controls.Add(Me.rbHormas)
        Me.GroupBox1.Controls.Add(Me.chkProgramacionProporcional)
        Me.GroupBox1.Controls.Add(Me.chkProgramacionDistribuida)
        Me.GroupBox1.Controls.Add(Me.chkCantidadDeLotes)
        Me.GroupBox1.Controls.Add(Me.chkLoteTamaño)
        Me.GroupBox1.Controls.Add(Me.cmdGenerar)
        Me.GroupBox1.Location = New System.Drawing.Point(240, 8)
        Me.GroupBox1.Name = "GroupBox1"
        Me.GroupBox1.Size = New System.Drawing.Size(725, 48)
        Me.GroupBox1.TabIndex = 158
        Me.GroupBox1.TabStop = False
        Me.GroupBox1.Text = "Lotear por:"
        '
        'chkPorTallas
        '
        Me.chkPorTallas.Checked = True
        Me.chkPorTallas.Location = New System.Drawing.Point(540, 16)
        Me.chkPorTallas.Name = "chkPorTallas"
        Me.chkPorTallas.Size = New System.Drawing.Size(81, 24)
        Me.chkPorTallas.TabIndex = 82
        Me.chkPorTallas.TabStop = True
        Me.chkPorTallas.Text = "Por tallas"
        '
        'rbHormas
        '
        Me.rbHormas.Location = New System.Drawing.Point(433, 16)
        Me.rbHormas.Name = "rbHormas"
        Me.rbHormas.Size = New System.Drawing.Size(85, 24)
        Me.rbHormas.TabIndex = 81
        Me.rbHormas.Text = "Por hormas"
        '
        'chkProgramacionProporcional
        '
        Me.chkProgramacionProporcional.Location = New System.Drawing.Point(687, 17)
        Me.chkProgramacionProporcional.Name = "chkProgramacionProporcional"
        Me.chkProgramacionProporcional.Size = New System.Drawing.Size(32, 24)
        Me.chkProgramacionProporcional.TabIndex = 80
        Me.chkProgramacionProporcional.Text = "Programación proporcional"
        Me.chkProgramacionProporcional.Visible = False
        '
        'chkProgramacionDistribuida
        '
        Me.chkProgramacionDistribuida.Location = New System.Drawing.Point(272, 16)
        Me.chkProgramacionDistribuida.Name = "chkProgramacionDistribuida"
        Me.chkProgramacionDistribuida.Size = New System.Drawing.Size(152, 24)
        Me.chkProgramacionDistribuida.TabIndex = 79
        Me.chkProgramacionDistribuida.Text = "Programación distribuida"
        '
        'chkCantidadDeLotes
        '
        Me.chkCantidadDeLotes.Location = New System.Drawing.Point(136, 16)
        Me.chkCantidadDeLotes.Name = "chkCantidadDeLotes"
        Me.chkCantidadDeLotes.Size = New System.Drawing.Size(128, 24)
        Me.chkCantidadDeLotes.TabIndex = 78
        Me.chkCantidadDeLotes.Text = "Cantidad de lotes"
        '
        'chkLoteTamaño
        '
        Me.chkLoteTamaño.Location = New System.Drawing.Point(8, 16)
        Me.chkLoteTamaño.Name = "chkLoteTamaño"
        Me.chkLoteTamaño.Size = New System.Drawing.Size(104, 24)
        Me.chkLoteTamaño.TabIndex = 77
        Me.chkLoteTamaño.Text = "Tamaño del lote"
        '
        'cmdGenerar
        '
        Me.cmdGenerar.Location = New System.Drawing.Point(617, 17)
        Me.cmdGenerar.Name = "cmdGenerar"
        Me.cmdGenerar.Size = New System.Drawing.Size(64, 24)
        Me.cmdGenerar.TabIndex = 76
        Me.cmdGenerar.Text = "Generar"
        '
        'txtCantidadDeLotes
        '
        Me.txtCantidadDeLotes.Location = New System.Drawing.Point(160, 32)
        Me.txtCantidadDeLotes.Name = "txtCantidadDeLotes"
        Me.txtCantidadDeLotes.Size = New System.Drawing.Size(68, 21)
        Me.txtCantidadDeLotes.TabIndex = 157
        '
        'txtLoteTamaño
        '
        Me.txtLoteTamaño.Location = New System.Drawing.Point(160, 8)
        Me.txtLoteTamaño.Name = "txtLoteTamaño"
        Me.txtLoteTamaño.Size = New System.Drawing.Size(68, 21)
        Me.txtLoteTamaño.TabIndex = 156
        Me.txtLoteTamaño.Text = "0"
        Me.txtLoteTamaño.TextAlign = System.Windows.Forms.HorizontalAlignment.Center
        '
        'Label32
        '
        Me.Label32.ForeColor = System.Drawing.Color.Black
        Me.Label32.Location = New System.Drawing.Point(40, 32)
        Me.Label32.Name = "Label32"
        Me.Label32.Size = New System.Drawing.Size(104, 24)
        Me.Label32.TabIndex = 155
        Me.Label32.Text = "Cantidad de lotes:"
        '
        'Label31
        '
        Me.Label31.ForeColor = System.Drawing.Color.Black
        Me.Label31.Location = New System.Drawing.Point(40, 8)
        Me.Label31.Name = "Label31"
        Me.Label31.Size = New System.Drawing.Size(104, 24)
        Me.Label31.TabIndex = 154
        Me.Label31.Text = "Tamaño del lote:"
        '
        'SimpleButton2
        '
        Me.SimpleButton2.Location = New System.Drawing.Point(904, 115)
        Me.SimpleButton2.Name = "SimpleButton2"
        Me.SimpleButton2.Size = New System.Drawing.Size(64, 24)
        Me.SimpleButton2.TabIndex = 153
        Me.SimpleButton2.Text = "Lotear"
        Me.SimpleButton2.ToolTip = "Copia el último renglon agregado"
        Me.SimpleButton2.Visible = False
        '
        'SimpleButton1
        '
        Me.SimpleButton1.Location = New System.Drawing.Point(904, 83)
        Me.SimpleButton1.Name = "SimpleButton1"
        Me.SimpleButton1.Size = New System.Drawing.Size(64, 24)
        Me.SimpleButton1.TabIndex = 152
        Me.SimpleButton1.Text = "Copia"
        Me.SimpleButton1.ToolTip = "Copia el último renglon agregado"
        '
        'P15
        '
        Me.P15.BackColor = System.Drawing.Color.White
        Me.P15.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.P15.Font = New System.Drawing.Font("Arial Narrow", 8.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.P15.Location = New System.Drawing.Point(560, 112)
        Me.P15.Name = "P15"
        Me.P15.Size = New System.Drawing.Size(32, 24)
        Me.P15.TabIndex = 150
        Me.P15.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        '
        'P14
        '
        Me.P14.BackColor = System.Drawing.Color.White
        Me.P14.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.P14.Font = New System.Drawing.Font("Arial Narrow", 8.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.P14.Location = New System.Drawing.Point(528, 112)
        Me.P14.Name = "P14"
        Me.P14.Size = New System.Drawing.Size(32, 24)
        Me.P14.TabIndex = 149
        Me.P14.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        '
        'P13
        '
        Me.P13.BackColor = System.Drawing.Color.White
        Me.P13.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.P13.Font = New System.Drawing.Font("Arial Narrow", 8.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.P13.Location = New System.Drawing.Point(496, 112)
        Me.P13.Name = "P13"
        Me.P13.Size = New System.Drawing.Size(32, 24)
        Me.P13.TabIndex = 148
        Me.P13.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        '
        'P12
        '
        Me.P12.BackColor = System.Drawing.Color.White
        Me.P12.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.P12.Font = New System.Drawing.Font("Arial Narrow", 8.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.P12.Location = New System.Drawing.Point(464, 112)
        Me.P12.Name = "P12"
        Me.P12.Size = New System.Drawing.Size(32, 24)
        Me.P12.TabIndex = 147
        Me.P12.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        '
        'P11
        '
        Me.P11.BackColor = System.Drawing.Color.White
        Me.P11.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.P11.Font = New System.Drawing.Font("Arial Narrow", 8.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.P11.Location = New System.Drawing.Point(432, 112)
        Me.P11.Name = "P11"
        Me.P11.Size = New System.Drawing.Size(32, 24)
        Me.P11.TabIndex = 146
        Me.P11.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        '
        'P10
        '
        Me.P10.BackColor = System.Drawing.Color.White
        Me.P10.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.P10.Font = New System.Drawing.Font("Arial Narrow", 8.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.P10.Location = New System.Drawing.Point(400, 112)
        Me.P10.Name = "P10"
        Me.P10.Size = New System.Drawing.Size(32, 24)
        Me.P10.TabIndex = 145
        Me.P10.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        '
        'P9
        '
        Me.P9.BackColor = System.Drawing.Color.White
        Me.P9.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.P9.Font = New System.Drawing.Font("Arial Narrow", 8.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.P9.Location = New System.Drawing.Point(368, 112)
        Me.P9.Name = "P9"
        Me.P9.Size = New System.Drawing.Size(32, 24)
        Me.P9.TabIndex = 144
        Me.P9.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        '
        'P8
        '
        Me.P8.BackColor = System.Drawing.Color.White
        Me.P8.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.P8.Font = New System.Drawing.Font("Arial Narrow", 8.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.P8.Location = New System.Drawing.Point(336, 112)
        Me.P8.Name = "P8"
        Me.P8.Size = New System.Drawing.Size(32, 24)
        Me.P8.TabIndex = 143
        Me.P8.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        '
        'P7
        '
        Me.P7.BackColor = System.Drawing.Color.White
        Me.P7.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.P7.Font = New System.Drawing.Font("Arial Narrow", 8.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.P7.Location = New System.Drawing.Point(304, 112)
        Me.P7.Name = "P7"
        Me.P7.Size = New System.Drawing.Size(32, 24)
        Me.P7.TabIndex = 142
        Me.P7.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        '
        'P6
        '
        Me.P6.BackColor = System.Drawing.Color.White
        Me.P6.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.P6.Font = New System.Drawing.Font("Arial Narrow", 8.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.P6.Location = New System.Drawing.Point(272, 112)
        Me.P6.Name = "P6"
        Me.P6.Size = New System.Drawing.Size(32, 24)
        Me.P6.TabIndex = 141
        Me.P6.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        '
        'P5
        '
        Me.P5.BackColor = System.Drawing.Color.White
        Me.P5.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.P5.Font = New System.Drawing.Font("Arial Narrow", 8.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.P5.Location = New System.Drawing.Point(240, 112)
        Me.P5.Name = "P5"
        Me.P5.Size = New System.Drawing.Size(32, 24)
        Me.P5.TabIndex = 140
        Me.P5.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        '
        'P4
        '
        Me.P4.BackColor = System.Drawing.Color.White
        Me.P4.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.P4.Font = New System.Drawing.Font("Arial Narrow", 8.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.P4.Location = New System.Drawing.Point(208, 112)
        Me.P4.Name = "P4"
        Me.P4.Size = New System.Drawing.Size(32, 24)
        Me.P4.TabIndex = 139
        Me.P4.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        '
        'P3
        '
        Me.P3.BackColor = System.Drawing.Color.White
        Me.P3.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.P3.Font = New System.Drawing.Font("Arial Narrow", 8.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.P3.Location = New System.Drawing.Point(176, 112)
        Me.P3.Name = "P3"
        Me.P3.Size = New System.Drawing.Size(32, 24)
        Me.P3.TabIndex = 138
        Me.P3.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        '
        'P2
        '
        Me.P2.BackColor = System.Drawing.Color.White
        Me.P2.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.P2.Font = New System.Drawing.Font("Arial Narrow", 8.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.P2.Location = New System.Drawing.Point(144, 112)
        Me.P2.Name = "P2"
        Me.P2.Size = New System.Drawing.Size(32, 24)
        Me.P2.TabIndex = 137
        Me.P2.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        '
        'P1
        '
        Me.P1.BackColor = System.Drawing.Color.White
        Me.P1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.P1.Font = New System.Drawing.Font("Arial Narrow", 8.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.P1.Location = New System.Drawing.Point(112, 112)
        Me.P1.Name = "P1"
        Me.P1.Size = New System.Drawing.Size(32, 24)
        Me.P1.TabIndex = 136
        Me.P1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        '
        'Label15
        '
        Me.Label15.BackColor = System.Drawing.Color.White
        Me.Label15.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.Label15.Font = New System.Drawing.Font("Arial Narrow", 8.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label15.Location = New System.Drawing.Point(560, 88)
        Me.Label15.Name = "Label15"
        Me.Label15.Size = New System.Drawing.Size(32, 24)
        Me.Label15.TabIndex = 134
        Me.Label15.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        '
        'Label14
        '
        Me.Label14.BackColor = System.Drawing.Color.White
        Me.Label14.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.Label14.Font = New System.Drawing.Font("Arial Narrow", 8.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label14.Location = New System.Drawing.Point(528, 88)
        Me.Label14.Name = "Label14"
        Me.Label14.Size = New System.Drawing.Size(32, 24)
        Me.Label14.TabIndex = 133
        Me.Label14.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        '
        'Label13
        '
        Me.Label13.BackColor = System.Drawing.Color.White
        Me.Label13.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.Label13.Font = New System.Drawing.Font("Arial Narrow", 8.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label13.Location = New System.Drawing.Point(496, 88)
        Me.Label13.Name = "Label13"
        Me.Label13.Size = New System.Drawing.Size(32, 24)
        Me.Label13.TabIndex = 132
        Me.Label13.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        '
        'Label12
        '
        Me.Label12.BackColor = System.Drawing.Color.White
        Me.Label12.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.Label12.Font = New System.Drawing.Font("Arial Narrow", 8.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label12.Location = New System.Drawing.Point(464, 88)
        Me.Label12.Name = "Label12"
        Me.Label12.Size = New System.Drawing.Size(32, 24)
        Me.Label12.TabIndex = 131
        Me.Label12.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        '
        'Label11
        '
        Me.Label11.BackColor = System.Drawing.Color.White
        Me.Label11.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.Label11.Font = New System.Drawing.Font("Arial Narrow", 8.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label11.Location = New System.Drawing.Point(432, 88)
        Me.Label11.Name = "Label11"
        Me.Label11.Size = New System.Drawing.Size(32, 24)
        Me.Label11.TabIndex = 130
        Me.Label11.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        '
        'Label10
        '
        Me.Label10.BackColor = System.Drawing.Color.White
        Me.Label10.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.Label10.Font = New System.Drawing.Font("Arial Narrow", 8.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label10.Location = New System.Drawing.Point(400, 88)
        Me.Label10.Name = "Label10"
        Me.Label10.Size = New System.Drawing.Size(32, 24)
        Me.Label10.TabIndex = 129
        Me.Label10.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        '
        'Label9
        '
        Me.Label9.BackColor = System.Drawing.Color.White
        Me.Label9.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.Label9.Font = New System.Drawing.Font("Arial Narrow", 8.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label9.Location = New System.Drawing.Point(368, 88)
        Me.Label9.Name = "Label9"
        Me.Label9.Size = New System.Drawing.Size(32, 24)
        Me.Label9.TabIndex = 128
        Me.Label9.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        '
        'Label8
        '
        Me.Label8.BackColor = System.Drawing.Color.White
        Me.Label8.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.Label8.Font = New System.Drawing.Font("Arial Narrow", 8.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label8.Location = New System.Drawing.Point(336, 88)
        Me.Label8.Name = "Label8"
        Me.Label8.Size = New System.Drawing.Size(32, 24)
        Me.Label8.TabIndex = 127
        Me.Label8.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        '
        'Label7
        '
        Me.Label7.BackColor = System.Drawing.Color.White
        Me.Label7.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.Label7.Font = New System.Drawing.Font("Arial Narrow", 8.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label7.Location = New System.Drawing.Point(304, 88)
        Me.Label7.Name = "Label7"
        Me.Label7.Size = New System.Drawing.Size(32, 24)
        Me.Label7.TabIndex = 126
        Me.Label7.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        '
        'Label6
        '
        Me.Label6.BackColor = System.Drawing.Color.White
        Me.Label6.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.Label6.Font = New System.Drawing.Font("Arial Narrow", 8.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label6.Location = New System.Drawing.Point(272, 88)
        Me.Label6.Name = "Label6"
        Me.Label6.Size = New System.Drawing.Size(32, 24)
        Me.Label6.TabIndex = 125
        Me.Label6.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        '
        'Label5
        '
        Me.Label5.BackColor = System.Drawing.Color.White
        Me.Label5.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.Label5.Font = New System.Drawing.Font("Arial Narrow", 8.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label5.Location = New System.Drawing.Point(240, 88)
        Me.Label5.Name = "Label5"
        Me.Label5.Size = New System.Drawing.Size(32, 24)
        Me.Label5.TabIndex = 124
        Me.Label5.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        '
        'Label4
        '
        Me.Label4.BackColor = System.Drawing.Color.White
        Me.Label4.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.Label4.Font = New System.Drawing.Font("Arial Narrow", 8.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label4.Location = New System.Drawing.Point(208, 88)
        Me.Label4.Name = "Label4"
        Me.Label4.Size = New System.Drawing.Size(32, 24)
        Me.Label4.TabIndex = 123
        Me.Label4.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        '
        'Label3
        '
        Me.Label3.BackColor = System.Drawing.Color.White
        Me.Label3.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.Label3.Font = New System.Drawing.Font("Arial Narrow", 8.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label3.Location = New System.Drawing.Point(176, 88)
        Me.Label3.Name = "Label3"
        Me.Label3.Size = New System.Drawing.Size(32, 24)
        Me.Label3.TabIndex = 122
        Me.Label3.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        '
        'Label2
        '
        Me.Label2.BackColor = System.Drawing.Color.White
        Me.Label2.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.Label2.Font = New System.Drawing.Font("Arial Narrow", 8.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label2.Location = New System.Drawing.Point(144, 88)
        Me.Label2.Name = "Label2"
        Me.Label2.Size = New System.Drawing.Size(32, 24)
        Me.Label2.TabIndex = 121
        Me.Label2.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        '
        'Label1
        '
        Me.Label1.BackColor = System.Drawing.Color.White
        Me.Label1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.Label1.Font = New System.Drawing.Font("Arial Narrow", 8.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label1.Location = New System.Drawing.Point(112, 88)
        Me.Label1.Name = "Label1"
        Me.Label1.Size = New System.Drawing.Size(32, 24)
        Me.Label1.TabIndex = 120
        Me.Label1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        '
        'Base1
        '
        Me.Base1.DataSetName = "Base"
        Me.Base1.Locale = New System.Globalization.CultureInfo("en-US")
        Me.Base1.SchemaSerializationMode = System.Data.SchemaSerializationMode.IncludeSchema
        '
        'SimpleButton3
        '
        Me.SimpleButton3.Location = New System.Drawing.Point(328, 416)
        Me.SimpleButton3.Name = "SimpleButton3"
        Me.SimpleButton3.Size = New System.Drawing.Size(96, 40)
        Me.SimpleButton3.TabIndex = 190
        Me.SimpleButton3.Text = "&Cerrar"
        Me.SimpleButton3.ToolTip = "Copia el último renglon agregado"
        '
        'SimpleButton4
        '
        Me.SimpleButton4.Location = New System.Drawing.Point(528, 416)
        Me.SimpleButton4.Name = "SimpleButton4"
        Me.SimpleButton4.Size = New System.Drawing.Size(96, 40)
        Me.SimpleButton4.TabIndex = 191
        Me.SimpleButton4.Text = "Desglosar"
        Me.SimpleButton4.ToolTip = "Copia el último renglon agregado"
        '
        'SqlDataAdapter1
        '
        Me.SqlDataAdapter1.DeleteCommand = Me.SqlDeleteCommand1
        Me.SqlDataAdapter1.InsertCommand = Me.SqlInsertCommand1
        Me.SqlDataAdapter1.SelectCommand = Me.SqlSelectCommand1
        Me.SqlDataAdapter1.TableMappings.AddRange(New System.Data.Common.DataTableMapping() {New System.Data.Common.DataTableMapping("Table", "PPedidoDesglosadoSelect", New System.Data.Common.DataColumnMapping() {New System.Data.Common.DataColumnMapping("PedidoDesglosadoID", "PedidoDesglosadoID"), New System.Data.Common.DataColumnMapping("Pedidos_DetailsID", "Pedidos_DetailsID"), New System.Data.Common.DataColumnMapping("Renglon", "Renglon"), New System.Data.Common.DataColumnMapping("P1", "P1"), New System.Data.Common.DataColumnMapping("P2", "P2"), New System.Data.Common.DataColumnMapping("P3", "P3"), New System.Data.Common.DataColumnMapping("P4", "P4"), New System.Data.Common.DataColumnMapping("P5", "P5"), New System.Data.Common.DataColumnMapping("P6", "P6"), New System.Data.Common.DataColumnMapping("P7", "P7"), New System.Data.Common.DataColumnMapping("P8", "P8"), New System.Data.Common.DataColumnMapping("P9", "P9"), New System.Data.Common.DataColumnMapping("P10", "P10"), New System.Data.Common.DataColumnMapping("P11", "P11"), New System.Data.Common.DataColumnMapping("P12", "P12"), New System.Data.Common.DataColumnMapping("P13", "P13"), New System.Data.Common.DataColumnMapping("P14", "P14"), New System.Data.Common.DataColumnMapping("P15", "P15"), New System.Data.Common.DataColumnMapping("P16", "P16"), New System.Data.Common.DataColumnMapping("P17", "P17"), New System.Data.Common.DataColumnMapping("P18", "P18"), New System.Data.Common.DataColumnMapping("P19", "P19"), New System.Data.Common.DataColumnMapping("P20", "P20"), New System.Data.Common.DataColumnMapping("P21", "P21"), New System.Data.Common.DataColumnMapping("P22", "P22"), New System.Data.Common.DataColumnMapping("P23", "P23"), New System.Data.Common.DataColumnMapping("P24", "P24"), New System.Data.Common.DataColumnMapping("P25", "P25"), New System.Data.Common.DataColumnMapping("P26", "P26"), New System.Data.Common.DataColumnMapping("P27", "P27"), New System.Data.Common.DataColumnMapping("P28", "P28"), New System.Data.Common.DataColumnMapping("P29", "P29"), New System.Data.Common.DataColumnMapping("P30", "P30"), New System.Data.Common.DataColumnMapping("Numero", "Numero"), New System.Data.Common.DataColumnMapping("Folio", "Folio"), New System.Data.Common.DataColumnMapping("Cantidad", "Cantidad"), New System.Data.Common.DataColumnMapping("Notas", "Notas")})})
        Me.SqlDataAdapter1.UpdateCommand = Me.SqlUpdateCommand1
        '
        'SqlDeleteCommand1
        '
        Me.SqlDeleteCommand1.CommandText = "dbo.PPedidoDesglosadoDelete2"
        Me.SqlDeleteCommand1.CommandType = System.Data.CommandType.StoredProcedure
        Me.SqlDeleteCommand1.Connection = Me.SqlConnection1
        Me.SqlDeleteCommand1.Parameters.AddRange(New System.Data.SqlClient.SqlParameter() {New System.Data.SqlClient.SqlParameter("@PedidoDesglosadoID", System.Data.SqlDbType.UniqueIdentifier, 16, System.Data.ParameterDirection.Input, False, CType(0, Byte), CType(0, Byte), "PedidoDesglosadoID", System.Data.DataRowVersion.Original, Nothing)})
        '
        'SqlConnection1
        '
        Me.SqlConnection1.ConnectionString = "workstation id=SERVOLDGRINGO2;packet size=4096;user id=leonel;data source=""zeus2-" &
    "bogger"";persist security info=False;initial catalog=Orange"
        Me.SqlConnection1.FireInfoMessageEventOnUserErrors = False
        '
        'SqlInsertCommand1
        '
        Me.SqlInsertCommand1.CommandText = "dbo.PPedidoDesglosadoInsert"
        Me.SqlInsertCommand1.CommandType = System.Data.CommandType.StoredProcedure
        Me.SqlInsertCommand1.Connection = Me.SqlConnection1
        Me.SqlInsertCommand1.Parameters.AddRange(New System.Data.SqlClient.SqlParameter() {New System.Data.SqlClient.SqlParameter("@RETURN_VALUE", System.Data.SqlDbType.Int, 4, System.Data.ParameterDirection.ReturnValue, False, CType(0, Byte), CType(0, Byte), "", System.Data.DataRowVersion.Current, Nothing), New System.Data.SqlClient.SqlParameter("@PedidoDesglosadoID", System.Data.SqlDbType.UniqueIdentifier, 16, "PedidoDesglosadoID"), New System.Data.SqlClient.SqlParameter("@Pedidos_DetailsID", System.Data.SqlDbType.UniqueIdentifier, 16, "Pedidos_DetailsID"), New System.Data.SqlClient.SqlParameter("@Renglon", System.Data.SqlDbType.Int, 4, "Renglon"), New System.Data.SqlClient.SqlParameter("@P1", System.Data.SqlDbType.Float, 8, "P1"), New System.Data.SqlClient.SqlParameter("@P2", System.Data.SqlDbType.Float, 8, "P2"), New System.Data.SqlClient.SqlParameter("@P3", System.Data.SqlDbType.Float, 8, "P3"), New System.Data.SqlClient.SqlParameter("@P4", System.Data.SqlDbType.Float, 8, "P4"), New System.Data.SqlClient.SqlParameter("@P5", System.Data.SqlDbType.Float, 8, "P5"), New System.Data.SqlClient.SqlParameter("@P6", System.Data.SqlDbType.Float, 8, "P6"), New System.Data.SqlClient.SqlParameter("@P7", System.Data.SqlDbType.Float, 8, "P7"), New System.Data.SqlClient.SqlParameter("@P8", System.Data.SqlDbType.Float, 8, "P8"), New System.Data.SqlClient.SqlParameter("@P9", System.Data.SqlDbType.Float, 8, "P9"), New System.Data.SqlClient.SqlParameter("@P10", System.Data.SqlDbType.Float, 8, "P10"), New System.Data.SqlClient.SqlParameter("@P11", System.Data.SqlDbType.Float, 8, "P11"), New System.Data.SqlClient.SqlParameter("@P12", System.Data.SqlDbType.Float, 8, "P12"), New System.Data.SqlClient.SqlParameter("@P13", System.Data.SqlDbType.Float, 8, "P13"), New System.Data.SqlClient.SqlParameter("@P14", System.Data.SqlDbType.Float, 8, "P14"), New System.Data.SqlClient.SqlParameter("@P15", System.Data.SqlDbType.Float, 8, "P15"), New System.Data.SqlClient.SqlParameter("@P16", System.Data.SqlDbType.Float, 8, "P16"), New System.Data.SqlClient.SqlParameter("@P17", System.Data.SqlDbType.Float, 8, "P17"), New System.Data.SqlClient.SqlParameter("@P18", System.Data.SqlDbType.Float, 8, "P18"), New System.Data.SqlClient.SqlParameter("@P19", System.Data.SqlDbType.Float, 8, "P19"), New System.Data.SqlClient.SqlParameter("@P20", System.Data.SqlDbType.Float, 8, "P20"), New System.Data.SqlClient.SqlParameter("@P21", System.Data.SqlDbType.Float, 8, "P21"), New System.Data.SqlClient.SqlParameter("@P22", System.Data.SqlDbType.Float, 8, "P22"), New System.Data.SqlClient.SqlParameter("@P23", System.Data.SqlDbType.Float, 8, "P23"), New System.Data.SqlClient.SqlParameter("@P24", System.Data.SqlDbType.Float, 8, "P24"), New System.Data.SqlClient.SqlParameter("@P25", System.Data.SqlDbType.Float, 8, "P25"), New System.Data.SqlClient.SqlParameter("@P26", System.Data.SqlDbType.Float, 8, "P26"), New System.Data.SqlClient.SqlParameter("@P27", System.Data.SqlDbType.Float, 8, "P27"), New System.Data.SqlClient.SqlParameter("@P28", System.Data.SqlDbType.Float, 8, "P28"), New System.Data.SqlClient.SqlParameter("@P29", System.Data.SqlDbType.Float, 8, "P29"), New System.Data.SqlClient.SqlParameter("@P30", System.Data.SqlDbType.Float, 8, "P30"), New System.Data.SqlClient.SqlParameter("@Cantidad", System.Data.SqlDbType.Float, 8, "Cantidad"), New System.Data.SqlClient.SqlParameter("@Folio", System.Data.SqlDbType.Int, 4, "Folio")})
        '
        'SqlSelectCommand1
        '
        Me.SqlSelectCommand1.CommandText = "dbo.PPedidoDesglosadoSelect"
        Me.SqlSelectCommand1.CommandType = System.Data.CommandType.StoredProcedure
        Me.SqlSelectCommand1.Connection = Me.SqlConnection1
        Me.SqlSelectCommand1.Parameters.AddRange(New System.Data.SqlClient.SqlParameter() {New System.Data.SqlClient.SqlParameter("@RETURN_VALUE", System.Data.SqlDbType.Int, 4, System.Data.ParameterDirection.ReturnValue, False, CType(0, Byte), CType(0, Byte), "", System.Data.DataRowVersion.Current, Nothing), New System.Data.SqlClient.SqlParameter("@Pedidos_DetailsID", System.Data.SqlDbType.UniqueIdentifier, 16)})
        '
        'SqlUpdateCommand1
        '
        Me.SqlUpdateCommand1.CommandText = "dbo.PPedidoDesglosadoUpdate"
        Me.SqlUpdateCommand1.CommandType = System.Data.CommandType.StoredProcedure
        Me.SqlUpdateCommand1.Connection = Me.SqlConnection1
        Me.SqlUpdateCommand1.Parameters.AddRange(New System.Data.SqlClient.SqlParameter() {New System.Data.SqlClient.SqlParameter("@RETURN_VALUE", System.Data.SqlDbType.Int, 4, System.Data.ParameterDirection.ReturnValue, False, CType(0, Byte), CType(0, Byte), "", System.Data.DataRowVersion.Current, Nothing), New System.Data.SqlClient.SqlParameter("@PedidoDesglosadoID", System.Data.SqlDbType.UniqueIdentifier, 16, "PedidoDesglosadoID"), New System.Data.SqlClient.SqlParameter("@Pedidos_DetailsID", System.Data.SqlDbType.UniqueIdentifier, 16, "Pedidos_DetailsID"), New System.Data.SqlClient.SqlParameter("@Renglon", System.Data.SqlDbType.Int, 4, "Renglon"), New System.Data.SqlClient.SqlParameter("@P1", System.Data.SqlDbType.Float, 8, "P1"), New System.Data.SqlClient.SqlParameter("@P2", System.Data.SqlDbType.Float, 8, "P2"), New System.Data.SqlClient.SqlParameter("@P3", System.Data.SqlDbType.Float, 8, "P3"), New System.Data.SqlClient.SqlParameter("@P4", System.Data.SqlDbType.Float, 8, "P4"), New System.Data.SqlClient.SqlParameter("@P5", System.Data.SqlDbType.Float, 8, "P5"), New System.Data.SqlClient.SqlParameter("@P6", System.Data.SqlDbType.Float, 8, "P6"), New System.Data.SqlClient.SqlParameter("@P7", System.Data.SqlDbType.Float, 8, "P7"), New System.Data.SqlClient.SqlParameter("@P8", System.Data.SqlDbType.Float, 8, "P8"), New System.Data.SqlClient.SqlParameter("@P9", System.Data.SqlDbType.Float, 8, "P9"), New System.Data.SqlClient.SqlParameter("@P10", System.Data.SqlDbType.Float, 8, "P10"), New System.Data.SqlClient.SqlParameter("@P11", System.Data.SqlDbType.Float, 8, "P11"), New System.Data.SqlClient.SqlParameter("@P12", System.Data.SqlDbType.Float, 8, "P12"), New System.Data.SqlClient.SqlParameter("@P13", System.Data.SqlDbType.Float, 8, "P13"), New System.Data.SqlClient.SqlParameter("@P14", System.Data.SqlDbType.Float, 8, "P14"), New System.Data.SqlClient.SqlParameter("@P15", System.Data.SqlDbType.Float, 8, "P15"), New System.Data.SqlClient.SqlParameter("@P16", System.Data.SqlDbType.Float, 8, "P16"), New System.Data.SqlClient.SqlParameter("@P17", System.Data.SqlDbType.Float, 8, "P17"), New System.Data.SqlClient.SqlParameter("@P18", System.Data.SqlDbType.Float, 8, "P18"), New System.Data.SqlClient.SqlParameter("@P19", System.Data.SqlDbType.Float, 8, "P19"), New System.Data.SqlClient.SqlParameter("@P20", System.Data.SqlDbType.Float, 8, "P20"), New System.Data.SqlClient.SqlParameter("@P21", System.Data.SqlDbType.Float, 8, "P21"), New System.Data.SqlClient.SqlParameter("@P22", System.Data.SqlDbType.Float, 8, "P22"), New System.Data.SqlClient.SqlParameter("@P23", System.Data.SqlDbType.Float, 8, "P23"), New System.Data.SqlClient.SqlParameter("@P24", System.Data.SqlDbType.Float, 8, "P24"), New System.Data.SqlClient.SqlParameter("@P25", System.Data.SqlDbType.Float, 8, "P25"), New System.Data.SqlClient.SqlParameter("@P26", System.Data.SqlDbType.Float, 8, "P26"), New System.Data.SqlClient.SqlParameter("@P27", System.Data.SqlDbType.Float, 8, "P27"), New System.Data.SqlClient.SqlParameter("@P28", System.Data.SqlDbType.Float, 8, "P28"), New System.Data.SqlClient.SqlParameter("@P29", System.Data.SqlDbType.Float, 8, "P29"), New System.Data.SqlClient.SqlParameter("@P30", System.Data.SqlDbType.Float, 8, "P30"), New System.Data.SqlClient.SqlParameter("@Cantidad", System.Data.SqlDbType.Float, 8, "Cantidad"), New System.Data.SqlClient.SqlParameter("@Folio", System.Data.SqlDbType.Int, 4, "Folio")})
        '
        'Label16_
        '
        Me.Label16_.Location = New System.Drawing.Point(40, 64)
        Me.Label16_.Name = "Label16_"
        Me.Label16_.Size = New System.Drawing.Size(56, 24)
        Me.Label16_.TabIndex = 192
        Me.Label16_.Text = "Tallas:"
        '
        'Label17_
        '
        Me.Label17_.Location = New System.Drawing.Point(40, 88)
        Me.Label17_.Name = "Label17_"
        Me.Label17_.Size = New System.Drawing.Size(56, 24)
        Me.Label17_.TabIndex = 193
        Me.Label17_.Text = "Pedido:"
        '
        'Label18_
        '
        Me.Label18_.Location = New System.Drawing.Point(40, 136)
        Me.Label18_.Name = "Label18_"
        Me.Label18_.Size = New System.Drawing.Size(72, 24)
        Me.Label18_.TabIndex = 194
        Me.Label18_.Text = "Desglosados:"
        '
        'Label19_
        '
        Me.Label19_.Location = New System.Drawing.Point(40, 112)
        Me.Label19_.Name = "Label19_"
        Me.Label19_.Size = New System.Drawing.Size(56, 24)
        Me.Label19_.TabIndex = 195
        Me.Label19_.Text = "Faltantes:"
        '
        'GridEX1
        '
        Me.GridEX1.AllowAddNew = Janus.Windows.GridEX.InheritableBoolean.[True]
        Me.GridEX1.AllowDelete = Janus.Windows.GridEX.InheritableBoolean.[True]
        Me.GridEX1.AlternatingColors = True
        Me.GridEX1.DataMember = "PedidoDesglosado"
        Me.GridEX1.DataSource = Me.Base1
        GridEXLayout1.LayoutString = resources.GetString("GridEXLayout1.LayoutString")
        Me.GridEX1.DesignTimeLayout = GridEXLayout1
        Me.GridEX1.EditorsControlStyle.ButtonAppearance = Janus.Windows.GridEX.ButtonAppearance.Regular
        Me.GridEX1.FilterRowFormatStyle.BackColor = System.Drawing.Color.Azure
        Me.GridEX1.Font = New System.Drawing.Font("Microsoft Sans Serif", 8.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.GridEX1.GroupByBoxInfoText = "Arrastra una columna aquí pra agrupar por esa columna"
        Me.GridEX1.GroupByBoxVisible = False
        Me.GridEX1.Location = New System.Drawing.Point(43, 163)
        Me.GridEX1.Name = "GridEX1"
        Me.GridEX1.NewRowFormatStyle.BackColor = System.Drawing.Color.White
        Me.GridEX1.RecordNavigator = True
        Me.GridEX1.RecordNavigatorText = "Registro:|de"
        Me.GridEX1.RowHeaders = Janus.Windows.GridEX.InheritableBoolean.[True]
        Me.GridEX1.SelectionMode = Janus.Windows.GridEX.SelectionMode.MultipleSelection
        Me.GridEX1.Size = New System.Drawing.Size(893, 247)
        Me.GridEX1.TabIndex = 196
        '
        'M21
        '
        Me.M21.BackColor = System.Drawing.Color.Gold
        Me.M21.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.M21.Font = New System.Drawing.Font("Arial Narrow", 8.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.M21.Location = New System.Drawing.Point(752, 64)
        Me.M21.Name = "M21"
        Me.M21.Size = New System.Drawing.Size(32, 24)
        Me.M21.TabIndex = 223
        Me.M21.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        '
        'T21
        '
        Me.T21.BackColor = System.Drawing.Color.White
        Me.T21.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.T21.Font = New System.Drawing.Font("Arial Narrow", 8.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.T21.Location = New System.Drawing.Point(752, 136)
        Me.T21.Name = "T21"
        Me.T21.Size = New System.Drawing.Size(32, 24)
        Me.T21.TabIndex = 222
        Me.T21.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        '
        'P21
        '
        Me.P21.BackColor = System.Drawing.Color.White
        Me.P21.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.P21.Font = New System.Drawing.Font("Arial Narrow", 8.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.P21.Location = New System.Drawing.Point(752, 112)
        Me.P21.Name = "P21"
        Me.P21.Size = New System.Drawing.Size(32, 24)
        Me.P21.TabIndex = 221
        Me.P21.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        '
        'Label21
        '
        Me.Label21.BackColor = System.Drawing.Color.White
        Me.Label21.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.Label21.Font = New System.Drawing.Font("Arial Narrow", 8.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label21.Location = New System.Drawing.Point(752, 88)
        Me.Label21.Name = "Label21"
        Me.Label21.Size = New System.Drawing.Size(32, 24)
        Me.Label21.TabIndex = 220
        Me.Label21.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        '
        'M20
        '
        Me.M20.BackColor = System.Drawing.Color.Gold
        Me.M20.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.M20.Font = New System.Drawing.Font("Arial Narrow", 8.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.M20.Location = New System.Drawing.Point(720, 64)
        Me.M20.Name = "M20"
        Me.M20.Size = New System.Drawing.Size(32, 24)
        Me.M20.TabIndex = 219
        Me.M20.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        '
        'T20
        '
        Me.T20.BackColor = System.Drawing.Color.White
        Me.T20.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.T20.Font = New System.Drawing.Font("Arial Narrow", 8.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.T20.Location = New System.Drawing.Point(720, 136)
        Me.T20.Name = "T20"
        Me.T20.Size = New System.Drawing.Size(32, 24)
        Me.T20.TabIndex = 218
        Me.T20.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        '
        'P20
        '
        Me.P20.BackColor = System.Drawing.Color.White
        Me.P20.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.P20.Font = New System.Drawing.Font("Arial Narrow", 8.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.P20.Location = New System.Drawing.Point(720, 112)
        Me.P20.Name = "P20"
        Me.P20.Size = New System.Drawing.Size(32, 24)
        Me.P20.TabIndex = 217
        Me.P20.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        '
        'Label20
        '
        Me.Label20.BackColor = System.Drawing.Color.White
        Me.Label20.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.Label20.Font = New System.Drawing.Font("Arial Narrow", 8.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label20.Location = New System.Drawing.Point(720, 88)
        Me.Label20.Name = "Label20"
        Me.Label20.Size = New System.Drawing.Size(32, 24)
        Me.Label20.TabIndex = 216
        Me.Label20.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        '
        'M19
        '
        Me.M19.BackColor = System.Drawing.Color.Gold
        Me.M19.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.M19.Font = New System.Drawing.Font("Arial Narrow", 8.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.M19.Location = New System.Drawing.Point(688, 64)
        Me.M19.Name = "M19"
        Me.M19.Size = New System.Drawing.Size(32, 24)
        Me.M19.TabIndex = 215
        Me.M19.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        '
        'T19
        '
        Me.T19.BackColor = System.Drawing.Color.White
        Me.T19.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.T19.Font = New System.Drawing.Font("Arial Narrow", 8.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.T19.Location = New System.Drawing.Point(688, 136)
        Me.T19.Name = "T19"
        Me.T19.Size = New System.Drawing.Size(32, 24)
        Me.T19.TabIndex = 214
        Me.T19.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        '
        'P19
        '
        Me.P19.BackColor = System.Drawing.Color.White
        Me.P19.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.P19.Font = New System.Drawing.Font("Arial Narrow", 8.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.P19.Location = New System.Drawing.Point(688, 112)
        Me.P19.Name = "P19"
        Me.P19.Size = New System.Drawing.Size(32, 24)
        Me.P19.TabIndex = 213
        Me.P19.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        '
        'Label19
        '
        Me.Label19.BackColor = System.Drawing.Color.White
        Me.Label19.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.Label19.Font = New System.Drawing.Font("Arial Narrow", 8.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label19.Location = New System.Drawing.Point(688, 88)
        Me.Label19.Name = "Label19"
        Me.Label19.Size = New System.Drawing.Size(32, 24)
        Me.Label19.TabIndex = 212
        Me.Label19.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        '
        'M18
        '
        Me.M18.BackColor = System.Drawing.Color.Gold
        Me.M18.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.M18.Font = New System.Drawing.Font("Arial Narrow", 8.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.M18.Location = New System.Drawing.Point(656, 64)
        Me.M18.Name = "M18"
        Me.M18.Size = New System.Drawing.Size(32, 24)
        Me.M18.TabIndex = 211
        Me.M18.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        '
        'T18
        '
        Me.T18.BackColor = System.Drawing.Color.White
        Me.T18.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.T18.Font = New System.Drawing.Font("Arial Narrow", 8.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.T18.Location = New System.Drawing.Point(656, 136)
        Me.T18.Name = "T18"
        Me.T18.Size = New System.Drawing.Size(32, 24)
        Me.T18.TabIndex = 210
        Me.T18.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        '
        'P18
        '
        Me.P18.BackColor = System.Drawing.Color.White
        Me.P18.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.P18.Font = New System.Drawing.Font("Arial Narrow", 8.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.P18.Location = New System.Drawing.Point(656, 112)
        Me.P18.Name = "P18"
        Me.P18.Size = New System.Drawing.Size(32, 24)
        Me.P18.TabIndex = 209
        Me.P18.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        '
        'Label18
        '
        Me.Label18.BackColor = System.Drawing.Color.White
        Me.Label18.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.Label18.Font = New System.Drawing.Font("Arial Narrow", 8.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label18.Location = New System.Drawing.Point(656, 88)
        Me.Label18.Name = "Label18"
        Me.Label18.Size = New System.Drawing.Size(32, 24)
        Me.Label18.TabIndex = 208
        Me.Label18.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        '
        'M17
        '
        Me.M17.BackColor = System.Drawing.Color.Gold
        Me.M17.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.M17.Font = New System.Drawing.Font("Arial Narrow", 8.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.M17.Location = New System.Drawing.Point(624, 64)
        Me.M17.Name = "M17"
        Me.M17.Size = New System.Drawing.Size(32, 24)
        Me.M17.TabIndex = 207
        Me.M17.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        '
        'T17
        '
        Me.T17.BackColor = System.Drawing.Color.White
        Me.T17.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.T17.Font = New System.Drawing.Font("Arial Narrow", 8.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.T17.Location = New System.Drawing.Point(624, 136)
        Me.T17.Name = "T17"
        Me.T17.Size = New System.Drawing.Size(32, 24)
        Me.T17.TabIndex = 206
        Me.T17.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        '
        'P17
        '
        Me.P17.BackColor = System.Drawing.Color.White
        Me.P17.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.P17.Font = New System.Drawing.Font("Arial Narrow", 8.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.P17.Location = New System.Drawing.Point(624, 112)
        Me.P17.Name = "P17"
        Me.P17.Size = New System.Drawing.Size(32, 24)
        Me.P17.TabIndex = 205
        Me.P17.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        '
        'Label17
        '
        Me.Label17.BackColor = System.Drawing.Color.White
        Me.Label17.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.Label17.Font = New System.Drawing.Font("Arial Narrow", 8.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label17.Location = New System.Drawing.Point(624, 88)
        Me.Label17.Name = "Label17"
        Me.Label17.Size = New System.Drawing.Size(32, 24)
        Me.Label17.TabIndex = 204
        Me.Label17.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        '
        'M16
        '
        Me.M16.BackColor = System.Drawing.Color.Gold
        Me.M16.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.M16.Font = New System.Drawing.Font("Arial Narrow", 8.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.M16.Location = New System.Drawing.Point(592, 64)
        Me.M16.Name = "M16"
        Me.M16.Size = New System.Drawing.Size(32, 24)
        Me.M16.TabIndex = 203
        Me.M16.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        '
        'T16
        '
        Me.T16.BackColor = System.Drawing.Color.White
        Me.T16.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.T16.Font = New System.Drawing.Font("Arial Narrow", 8.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.T16.Location = New System.Drawing.Point(592, 136)
        Me.T16.Name = "T16"
        Me.T16.Size = New System.Drawing.Size(32, 24)
        Me.T16.TabIndex = 202
        Me.T16.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        '
        'P16
        '
        Me.P16.BackColor = System.Drawing.Color.White
        Me.P16.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.P16.Font = New System.Drawing.Font("Arial Narrow", 8.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.P16.Location = New System.Drawing.Point(592, 112)
        Me.P16.Name = "P16"
        Me.P16.Size = New System.Drawing.Size(32, 24)
        Me.P16.TabIndex = 201
        Me.P16.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        '
        'Label16
        '
        Me.Label16.BackColor = System.Drawing.Color.White
        Me.Label16.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.Label16.Font = New System.Drawing.Font("Arial Narrow", 8.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label16.Location = New System.Drawing.Point(592, 88)
        Me.Label16.Name = "Label16"
        Me.Label16.Size = New System.Drawing.Size(32, 24)
        Me.Label16.TabIndex = 200
        Me.Label16.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        '
        'TCantidad
        '
        Me.TCantidad.BackColor = System.Drawing.Color.White
        Me.TCantidad.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.TCantidad.Font = New System.Drawing.Font("Arial Narrow", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.TCantidad.Location = New System.Drawing.Point(780, 136)
        Me.TCantidad.Name = "TCantidad"
        Me.TCantidad.Size = New System.Drawing.Size(56, 24)
        Me.TCantidad.TabIndex = 199
        Me.TCantidad.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        '
        'PCantidad
        '
        Me.PCantidad.BackColor = System.Drawing.Color.White
        Me.PCantidad.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.PCantidad.Font = New System.Drawing.Font("Arial Narrow", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.PCantidad.Location = New System.Drawing.Point(780, 112)
        Me.PCantidad.Name = "PCantidad"
        Me.PCantidad.Size = New System.Drawing.Size(56, 24)
        Me.PCantidad.TabIndex = 198
        Me.PCantidad.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        '
        'LabelCantidad
        '
        Me.LabelCantidad.BackColor = System.Drawing.Color.White
        Me.LabelCantidad.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.LabelCantidad.Font = New System.Drawing.Font("Arial Narrow", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.LabelCantidad.Location = New System.Drawing.Point(780, 88)
        Me.LabelCantidad.Name = "LabelCantidad"
        Me.LabelCantidad.Size = New System.Drawing.Size(56, 24)
        Me.LabelCantidad.TabIndex = 197
        Me.LabelCantidad.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        '
        'frmPedidoDesglosado2
        '
        Me.Appearance.BackColor = System.Drawing.Color.SkyBlue
        Me.Appearance.Options.UseBackColor = True
        Me.AutoScaleBaseSize = New System.Drawing.Size(5, 14)
        Me.ClientSize = New System.Drawing.Size(1001, 490)
        Me.Controls.Add(Me.M21)
        Me.Controls.Add(Me.T21)
        Me.Controls.Add(Me.P21)
        Me.Controls.Add(Me.Label21)
        Me.Controls.Add(Me.M20)
        Me.Controls.Add(Me.T20)
        Me.Controls.Add(Me.P20)
        Me.Controls.Add(Me.Label20)
        Me.Controls.Add(Me.M19)
        Me.Controls.Add(Me.T19)
        Me.Controls.Add(Me.P19)
        Me.Controls.Add(Me.Label19)
        Me.Controls.Add(Me.M18)
        Me.Controls.Add(Me.T18)
        Me.Controls.Add(Me.P18)
        Me.Controls.Add(Me.Label18)
        Me.Controls.Add(Me.M17)
        Me.Controls.Add(Me.T17)
        Me.Controls.Add(Me.P17)
        Me.Controls.Add(Me.Label17)
        Me.Controls.Add(Me.M16)
        Me.Controls.Add(Me.T16)
        Me.Controls.Add(Me.P16)
        Me.Controls.Add(Me.Label16)
        Me.Controls.Add(Me.TCantidad)
        Me.Controls.Add(Me.PCantidad)
        Me.Controls.Add(Me.LabelCantidad)
        Me.Controls.Add(Me.GridEX1)
        Me.Controls.Add(Me.Label19_)
        Me.Controls.Add(Me.Label18_)
        Me.Controls.Add(Me.Label17_)
        Me.Controls.Add(Me.Label16_)
        Me.Controls.Add(Me.SimpleButton4)
        Me.Controls.Add(Me.SimpleButton3)
        Me.Controls.Add(Me.M15)
        Me.Controls.Add(Me.M14)
        Me.Controls.Add(Me.M13)
        Me.Controls.Add(Me.M12)
        Me.Controls.Add(Me.M11)
        Me.Controls.Add(Me.M10)
        Me.Controls.Add(Me.M9)
        Me.Controls.Add(Me.M8)
        Me.Controls.Add(Me.M7)
        Me.Controls.Add(Me.M6)
        Me.Controls.Add(Me.M5)
        Me.Controls.Add(Me.M4)
        Me.Controls.Add(Me.M3)
        Me.Controls.Add(Me.M2)
        Me.Controls.Add(Me.M1)
        Me.Controls.Add(Me.T15)
        Me.Controls.Add(Me.T14)
        Me.Controls.Add(Me.T13)
        Me.Controls.Add(Me.T12)
        Me.Controls.Add(Me.T11)
        Me.Controls.Add(Me.T10)
        Me.Controls.Add(Me.T9)
        Me.Controls.Add(Me.T8)
        Me.Controls.Add(Me.T7)
        Me.Controls.Add(Me.T6)
        Me.Controls.Add(Me.T5)
        Me.Controls.Add(Me.T4)
        Me.Controls.Add(Me.T3)
        Me.Controls.Add(Me.T2)
        Me.Controls.Add(Me.T1)
        Me.Controls.Add(Me.GroupBox1)
        Me.Controls.Add(Me.txtCantidadDeLotes)
        Me.Controls.Add(Me.txtLoteTamaño)
        Me.Controls.Add(Me.Label32)
        Me.Controls.Add(Me.Label31)
        Me.Controls.Add(Me.SimpleButton2)
        Me.Controls.Add(Me.SimpleButton1)
        Me.Controls.Add(Me.P15)
        Me.Controls.Add(Me.P14)
        Me.Controls.Add(Me.P13)
        Me.Controls.Add(Me.P12)
        Me.Controls.Add(Me.P11)
        Me.Controls.Add(Me.P10)
        Me.Controls.Add(Me.P9)
        Me.Controls.Add(Me.P8)
        Me.Controls.Add(Me.P7)
        Me.Controls.Add(Me.P6)
        Me.Controls.Add(Me.P5)
        Me.Controls.Add(Me.P4)
        Me.Controls.Add(Me.P3)
        Me.Controls.Add(Me.P2)
        Me.Controls.Add(Me.P1)
        Me.Controls.Add(Me.Label15)
        Me.Controls.Add(Me.Label14)
        Me.Controls.Add(Me.Label13)
        Me.Controls.Add(Me.Label12)
        Me.Controls.Add(Me.Label11)
        Me.Controls.Add(Me.Label10)
        Me.Controls.Add(Me.Label9)
        Me.Controls.Add(Me.Label8)
        Me.Controls.Add(Me.Label7)
        Me.Controls.Add(Me.Label6)
        Me.Controls.Add(Me.Label5)
        Me.Controls.Add(Me.Label4)
        Me.Controls.Add(Me.Label3)
        Me.Controls.Add(Me.Label2)
        Me.Controls.Add(Me.Label1)
        Me.Name = "frmPedidoDesglosado2"
        Me.Text = "Desglosar el renglón del pedido"
        Me.GroupBox1.ResumeLayout(False)
        CType(Me.Base1, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.GridEX1, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub

#End Region



    Public Sub Iniciar(ByVal miPedidos_DetailsID As Guid)
        Pedidos_DetailsID = miPedidos_DetailsID
        Restaurar()

        Me.txtLoteTamaño.Text = Parametros_Generales_TamañoDelLote()

        PonerCorrida(PoneproductoidDePedidos_Details)
        BDesglosado = False

        Me.GridEX1.AllowEdit = InheritableBoolean.True
        Me.GridEX1.AllowAddNew = InheritableBoolean.True
        Me.GridEX1.AllowDelete = InheritableBoolean.True

        Dim s As New GridEXSortKey

        s.Column = GridEX1.RootTable.Columns("Folio")
        s.SortOrder = Janus.Windows.GridEX.SortOrder.Ascending

        GridEX1.RootTable.SortKeys.Add(s)
        GridEX1.RootTable.Columns("Folio").EditType = Janus.Windows.GridEX.EditType.TextBox

    End Sub


    Private Function PoneproductoidDePedidos_Details() As Guid

        
        Dim cmd As SqlClient.SqlCommand
        Dim rs As SqlClient.SqlDataReader
        Dim V As clsValidar

        
        using cnn As New SqlClient.SqlConnection(scnn)
        
        cnn.Open()

        V = New clsValidar

        cmd = New SqlClient.SqlCommand("PPoneproductoidDePedidos_Details", cnn)
        cmd.CommandType = CommandType.StoredProcedure
        cmd.Parameters.Add(New System.Data.SqlClient.SqlParameter("@Pedidos_DetailsID", System.Data.SqlDbType.UniqueIdentifier, 16, "Pedidos_DetailsID"))
        cmd.Parameters("@Pedidos_DetailsID").Value = Pedidos_DetailsID
        Inicializar()

        rs = cmd.ExecuteReader()
        Do While rs.Read

            PoneproductoidDePedidos_Details = rs!ProductoID
            If rs!P1 > 0 Then
                Label1.Text = rs!P1
                GridEX1.RootTable.Columns("P1").EditType = EditType.TextBox
            Else
                GridEX1.RootTable.Columns("P1").EditType = EditType.NoEdit
            End If
            If rs!P2 > 0 Then
                Label2.Text = rs!P2
                GridEX1.RootTable.Columns("P2").EditType = EditType.TextBox
            Else
                GridEX1.RootTable.Columns("P2").EditType = EditType.NoEdit
            End If
            If rs!P3 > 0 Then
                Label3.Text = rs!P3
                GridEX1.RootTable.Columns("P3").EditType = EditType.TextBox
            Else
                GridEX1.RootTable.Columns("P3").EditType = EditType.NoEdit
            End If
            If rs!P4 > 0 Then
                Label4.Text = rs!P4
                GridEX1.RootTable.Columns("P4").EditType = EditType.TextBox
            Else
                GridEX1.RootTable.Columns("P4").EditType = EditType.NoEdit
            End If
            If rs!P5 > 0 Then
                Label5.Text = rs!P5
                GridEX1.RootTable.Columns("P5").EditType = EditType.TextBox
            Else
                GridEX1.RootTable.Columns("P5").EditType = EditType.NoEdit
            End If
            If rs!P6 > 0 Then
                Label6.Text = rs!P6
                GridEX1.RootTable.Columns("P6").EditType = EditType.TextBox
            Else
                GridEX1.RootTable.Columns("P6").EditType = EditType.NoEdit
            End If
            If rs!P7 > 0 Then
                Label7.Text = rs!P7
                GridEX1.RootTable.Columns("P7").EditType = EditType.TextBox
            Else
                GridEX1.RootTable.Columns("P7").EditType = EditType.NoEdit
            End If
            If rs!P8 > 0 Then
                Label8.Text = rs!P8
                GridEX1.RootTable.Columns("P8").EditType = EditType.TextBox
            Else
                GridEX1.RootTable.Columns("P8").EditType = EditType.NoEdit
            End If
            If rs!P9 > 0 Then
                Label9.Text = rs!P9
                GridEX1.RootTable.Columns("P9").EditType = EditType.TextBox
            Else
                GridEX1.RootTable.Columns("P9").EditType = EditType.NoEdit
            End If
            If rs!P10 > 0 Then
                Label10.Text = rs!P10
                GridEX1.RootTable.Columns("P10").EditType = EditType.TextBox
            Else
                GridEX1.RootTable.Columns("P10").EditType = EditType.NoEdit
            End If
            If rs!P11 > 0 Then
                Label11.Text = rs!P11
                GridEX1.RootTable.Columns("P11").EditType = EditType.TextBox
            Else
                GridEX1.RootTable.Columns("P11").EditType = EditType.NoEdit
            End If
            If rs!P12 > 0 Then
                Label12.Text = rs!P12
                GridEX1.RootTable.Columns("P12").EditType = EditType.TextBox
            Else
                GridEX1.RootTable.Columns("P12").EditType = EditType.NoEdit
            End If
            If rs!P13 > 0 Then
                Label13.Text = rs!P13
                GridEX1.RootTable.Columns("P13").EditType = EditType.TextBox
            Else
                GridEX1.RootTable.Columns("P13").EditType = EditType.NoEdit
            End If
            If rs!P14 > 0 Then
                Label14.Text = rs!P14
                GridEX1.RootTable.Columns("P14").EditType = EditType.TextBox
            Else
                GridEX1.RootTable.Columns("P14").EditType = EditType.NoEdit
            End If
            If rs!P15 > 0 Then
                Label15.Text = rs!P15
                GridEX1.RootTable.Columns("P15").EditType = EditType.TextBox
            Else
                GridEX1.RootTable.Columns("P15").EditType = EditType.NoEdit
            End If
            If rs!P16 > 0 Then
                Label16.Text = rs!P16
                GridEX1.RootTable.Columns("P16").EditType = EditType.TextBox
            Else
                GridEX1.RootTable.Columns("P16").EditType = EditType.NoEdit
            End If
            If rs!P17 > 0 Then
                Label17.Text = rs!P17
                GridEX1.RootTable.Columns("P17").EditType = EditType.TextBox
            Else
                GridEX1.RootTable.Columns("P17").EditType = EditType.NoEdit
            End If
            If rs!P18 > 0 Then
                Label18.Text = rs!P18
                GridEX1.RootTable.Columns("P18").EditType = EditType.TextBox
            Else
                GridEX1.RootTable.Columns("P18").EditType = EditType.NoEdit
            End If
            If rs!P19 > 0 Then
                Label19.Text = rs!P19
                GridEX1.RootTable.Columns("P19").EditType = EditType.TextBox
            Else
                GridEX1.RootTable.Columns("P19").EditType = EditType.NoEdit
            End If
            If rs!P20 > 0 Then
                Label20.Text = rs!P20
                GridEX1.RootTable.Columns("P20").EditType = EditType.TextBox
            Else
                GridEX1.RootTable.Columns("P20").EditType = EditType.NoEdit
            End If
            If rs!P21 > 0 Then
                Label21.Text = rs!P21
                GridEX1.RootTable.Columns("P21").EditType = EditType.TextBox
            Else
                GridEX1.RootTable.Columns("P21").EditType = EditType.NoEdit
            End If
            LabelCantidad.Text = rs!Cantidad
        Loop

        rs.Close()
        cmd = Nothing
       end using
        

        P1.Text = Label1.Text
        P2.Text = Label2.Text
        P3.Text = Label3.Text
        P4.Text = Label4.Text
        P5.Text = Label5.Text
        P6.Text = Label6.Text
        P7.Text = Label7.Text
        P8.Text = Label8.Text
        P9.Text = Label9.Text
        P10.Text = Label10.Text
        P11.Text = Label11.Text
        P12.Text = Label12.Text
        P13.Text = Label13.Text
        P14.Text = Label14.Text
        P15.Text = Label15.Text
        P16.Text = Label16.Text
        P17.Text = Label17.Text
        P18.Text = Label18.Text
        P19.Text = Label19.Text
        P20.Text = Label20.Text
        P21.Text = Label21.Text
        PCantidad.Text = LabelCantidad.Text



        'PonerDefaults()
        'GridEX1.RootTable.Columns("PCantidad").DefaultValue = LabelCantidad.Text

    End Function

    Private Sub GridEX1_FormattingRow(ByVal sender As System.Object, ByVal e As Janus.Windows.GridEX.RowLoadEventArgs)

    End Sub

    Private Sub UpdateData()
        Me.SqlDataAdapter1.Update(Me.Base1.PedidoDesglosado)
    End Sub





    Private Function ValidadoRowEnUpdate() As Boolean
        'Dim currentRow As GridEXRow = GridEX1.GetRow()
        '
        'Dim Comando As SqlClient.SqlCommand
        'Dim Lector As SqlClient.SqlDataReader
        'Dim Encontrado As Boolean

        'If Anterior = currentRow.Cells("Nombre").Value Then
        '    Return True
        '    Exit Function
        'End If

        'If IsDBNull(currentRow.Cells("Nombre").Value) Then
        '    MessageBox.Show("Introduce el nombre.")
        '    Me.GridEX1.CurrentColumn = Me.GridEX1.RootTable.Columns("Nombre")
        '    Me.GridEX1.Focus()
        'Else
        '    
        '    
        '    cnn.Open()

        '    Comando = New SqlClient.SqlCommand("PPedidoDesglosadoBuscaPorNombre", cnn)
        '    Comando.CommandType = CommandType.StoredProcedure
        '    Comando.Parameters.Add(New System.Data.SqlClient.SqlParameter("@GrupoID", System.Data.SqlDbType.UniqueIdentifier, 16, "GrupoID"))
        '    Comando.Parameters.Add("@Nombre", System.Data.SqlDbType.VarChar, 40, "Nombre")
        '    Comando.Parameters("@GrupoID").Value = GrupoID
        '    Comando.Parameters("@Nombre").Value = currentRow.Cells("Nombre").Value

        '    Lector = Comando.ExecuteReader
        '    Encontrado = False
        '    Do While Lector.Read
        '        Encontrado = True

        '    Loop
        '    Lector.Close()
        '    Comando = Nothing
        '


        '    If Encontrado Then
        '        MessageBox.Show("PedidoDesglosado ya existente !!!")
        '        Return False

        '    End If




        'End If

        Return True
    End Function


    Private Sub GridEX1_CellEdited(ByVal sender As Object, ByVal e As Janus.Windows.GridEX.ColumnActionEventArgs)
        'Anterior = AnteriorNombre()
    End Sub
    Private Function AnteriorNombre() As String
        AnteriorNombre = ""
        'Dim Row As GridEXRow = GridEX1.GetRow

        'AnteriorNombre = Row.Cells("Nombre").Value
    End Function

    Private Sub GridEX1_AddingRecord1(sender As Object, e As System.ComponentModel.CancelEventArgs) Handles GridEX1.AddingRecord
        Dim Row As GridEXRow = GridEX1.GetRow

        e.Cancel = Not ValidadoRow()

        Dim v As clsValidar

        v = New clsValidar

        If Row.Cells("P1").Value > v.IniDouble(P1.Text) Then
            MsgBox("No existen tantos para desglosar en la talla !!!" & GridEX1.RootTable.Columns("P1").Caption)
            e.Cancel = True
            Exit Sub
        End If
        If Row.Cells("P2").Value > v.IniDouble(P2.Text) Then
            MsgBox("No existen tantos para desglosar en la talla !!!" & GridEX1.RootTable.Columns("P2").Caption)
            e.Cancel = True
            Exit Sub
        End If
        If Row.Cells("P3").Value > v.IniDouble(P3.Text) Then
            MsgBox("No existen tantos para desglosar en la talla !!!" & GridEX1.RootTable.Columns("P3").Caption)
            e.Cancel = True
            Exit Sub
        End If
        If Row.Cells("P4").Value > v.IniDouble(P4.Text) Then
            MsgBox("No existen tantos para desglosar en la talla !!!" & GridEX1.RootTable.Columns("P4").Caption)
            e.Cancel = True
            Exit Sub
        End If
        If Row.Cells("P5").Value > v.IniDouble(P5.Text) Then
            MsgBox("No existen tantos para desglosar en la talla !!!" & GridEX1.RootTable.Columns("P5").Caption)
            e.Cancel = True
            Exit Sub
        End If
        If Row.Cells("P6").Value > v.IniDouble(P6.Text) Then
            MsgBox("No existen tantos para desglosar en la talla !!!" & GridEX1.RootTable.Columns("P6").Caption)
            e.Cancel = True
            Exit Sub
        End If
        If Row.Cells("P7").Value > v.IniDouble(P7.Text) Then
            MsgBox("No existen tantos para desglosar en la talla !!!" & GridEX1.RootTable.Columns("P7").Caption)
            e.Cancel = True
            Exit Sub
        End If
        If Row.Cells("P8").Value > v.IniDouble(P8.Text) Then
            MsgBox("No existen tantos para desglosar en la talla !!!" & GridEX1.RootTable.Columns("P8").Caption)
            e.Cancel = True
            Exit Sub
        End If
        If Row.Cells("P9").Value > v.IniDouble(P9.Text) Then
            MsgBox("No existen tantos para desglosar en la talla !!!" & GridEX1.RootTable.Columns("P9").Caption)
            e.Cancel = True
            Exit Sub
        End If
        If Row.Cells("P10").Value > v.IniDouble(P10.Text) Then
            MsgBox("No existen tantos para desglosar en la talla !!!" & GridEX1.RootTable.Columns("P10").Caption)
            e.Cancel = True
            Exit Sub
        End If
        If Row.Cells("P11").Value > v.IniDouble(P11.Text) Then
            MsgBox("No existen tantos para desglosar en la talla !!!" & GridEX1.RootTable.Columns("P11").Caption)
            e.Cancel = True
            Exit Sub
        End If
        If Row.Cells("P12").Value > v.IniDouble(P12.Text) Then
            MsgBox("No existen tantos para desglosar en la talla !!!" & GridEX1.RootTable.Columns("P12").Caption)
            e.Cancel = True
            Exit Sub
        End If
        If Row.Cells("P13").Value > v.IniDouble(P13.Text) Then
            MsgBox("No existen tantos para desglosar en la talla !!!" & GridEX1.RootTable.Columns("P13").Caption)
            e.Cancel = True
            Exit Sub
        End If
        If Row.Cells("P14").Value > v.IniDouble(P14.Text) Then
            MsgBox("No existen tantos para desglosar en la talla !!!" & GridEX1.RootTable.Columns("P14").Caption)
            e.Cancel = True
            Exit Sub
        End If
        If Row.Cells("P15").Value > v.IniDouble(P15.Text) Then
            MsgBox("No existen tantos para desglosar en la talla !!!" & GridEX1.RootTable.Columns("P15").Caption)
            e.Cancel = True
            Exit Sub
        End If
        If Row.Cells("P16").Value > v.IniDouble(P16.Text) Then
            MsgBox("No existen tantos para desglosar en la talla !!!" & GridEX1.RootTable.Columns("P16").Caption)
            e.Cancel = True
            Exit Sub
        End If
        If Row.Cells("P17").Value > v.IniDouble(P17.Text) Then
            MsgBox("No existen tantos para desglosar en la talla !!!" & GridEX1.RootTable.Columns("P17").Caption)
            e.Cancel = True
            Exit Sub
        End If
        If Row.Cells("P18").Value > v.IniDouble(P18.Text) Then
            MsgBox("No existen tantos para desglosar en la talla !!!" & GridEX1.RootTable.Columns("P18").Caption)
            e.Cancel = True
            Exit Sub
        End If
        If Row.Cells("P19").Value > v.IniDouble(P19.Text) Then
            MsgBox("No existen tantos para desglosar en la talla !!!" & GridEX1.RootTable.Columns("P19").Caption)
            e.Cancel = True
            Exit Sub
        End If
        If Row.Cells("P20").Value > v.IniDouble(P20.Text) Then
            MsgBox("No existen tantos para desglosar en la talla !!!" & GridEX1.RootTable.Columns("P20").Caption)
            e.Cancel = True
            Exit Sub
        End If
        If Row.Cells("P21").Value > v.IniDouble(P21.Text) Then
            MsgBox("No existen tantos para desglosar en la talla !!!" & GridEX1.RootTable.Columns("P21").Caption)
            e.Cancel = True
            Exit Sub
        End If


        GridEX1.SetValue("PedidoDesglosadoID", Guid.NewGuid)
        GridEX1.SetValue("Pedidos_DetailsID", Pedidos_DetailsID)
        GridEX1.SetValue("Renglon", Me.SiguientePedidoDesglosado)
        GridEX1.SetValue("Folio", SiguientePedidoDesglosado_Folio)

        'Me.GridEX1.CurrentColumn = Me.GridEX1.RootTable.Columns("Clave")

    End Sub

    Private Sub GridEX1_RecordUpdated(ByVal sender As Object, ByVal e As System.EventArgs) Handles GridEX1.RecordUpdated
        UpdateData()
        PonerPorDesglozar()
        'PonerDefaults()

    End Sub



    Private Function ValidadoRow() As Boolean
        'Dim currentRow As GridEXRow = GridEX1.GetRow()
        '
        'Dim Comando As SqlClient.SqlCommand
        'Dim Lector As SqlClient.SqlDataReader
        'Dim Encontrado As Boolean

        'If IsDBNull(currentRow.Cells("Clave").Value) Then
        '    Me.GridEX1.SetValue("Clave", SiguientePedidoDesglosado)
        'End If

        'If IsDBNull(currentRow.Cells("Nombre").Value) Then
        '    MessageBox.Show("Introduce el nombre.")
        '    Me.GridEX1.CurrentColumn = Me.GridEX1.RootTable.Columns("Nombre")
        '    Me.GridEX1.Focus()
        'Else
        '    
        '    
        '    cnn.Open()

        '    Comando = New SqlClient.SqlCommand("PPedidoDesglosadoBuscaPorNombre", cnn)
        '    Comando.CommandType = CommandType.StoredProcedure
        '    Comando.Parameters.Add(New System.Data.SqlClient.SqlParameter("@Pedidos_DetailsID", System.Data.SqlDbType.UniqueIdentifier, 16, "GrupoID"))
        '    Comando.Parameters.Add("@Nombre", System.Data.SqlDbType.VarChar, 40, "Nombre")
        '    Comando.Parameters("@GrupoID").Value = GrupoID
        '    Comando.Parameters("@Nombre").Value = currentRow.Cells("Nombre").Value

        '    Lector = Comando.ExecuteReader
        '    Encontrado = False
        '    Do While Lector.Read
        '        Encontrado = True
        '    Loop
        '    Lector.Close()
        '    Comando = Nothing
        '


        '    If Encontrado Then
        '        MessageBox.Show("PedidoDesglosado ya existente !!!")
        '        GridEX1.Row = -1
        '        Return False

        '    End If




        'End If

        Return True
    End Function


    Public Function SiguientePedidoDesglosado() As Integer
        
        Dim cmd As SqlClient.SqlCommand
        Dim rs As SqlClient.SqlDataReader
        Dim V As clsValidar

        
        using cnn As New SqlClient.SqlConnection(scnn)
        
        cnn.Open()

        V = New clsValidar

        cmd = New SqlClient.SqlCommand("PSiguientePedidoDesglosado", cnn)
        cmd.CommandType = CommandType.StoredProcedure
        cmd.Parameters.Add(New System.Data.SqlClient.SqlParameter("@Pedidos_DetailsID", System.Data.SqlDbType.UniqueIdentifier, 16, "Pedidos_DetailsID"))
        cmd.Parameters("@Pedidos_DetailsID").Value = Pedidos_DetailsID

        rs = cmd.ExecuteReader()
        SiguientePedidoDesglosado = 0
        Do While rs.Read
            If IsDBNull(rs!Siguiente) = False Then

                SiguientePedidoDesglosado = rs!Siguiente
            End If
        Loop
        SiguientePedidoDesglosado = SiguientePedidoDesglosado
        rs.Close()
        cmd = Nothing
       end using
        


    End Function

    Private Sub PonerCorrida(ByVal ProductoID As Guid)
        
        Dim cmd As SqlClient.SqlCommand
        Dim rs As SqlClient.SqlDataReader
        Dim Row As GridEXRow = GridEX1.GetRow()
        Dim CorridaID As Guid

        
        using cnn As New SqlClient.SqlConnection(scnn)
        
        cnn.Open()

        cmd = New SqlClient.SqlCommand("PfrmPedidos_GridEx1_SelectionChanged", cnn)
        cmd.CommandType = CommandType.StoredProcedure
        cmd.Parameters.Add(New System.Data.SqlClient.SqlParameter("@ProductoID", System.Data.SqlDbType.UniqueIdentifier, 16, "ProductoID"))
        cmd.Parameters("@ProductoID").Value = ProductoID


        rs = cmd.ExecuteReader()
        Do While rs.Read
            CorridaID = rs!CorridaID
        Loop
        rs.Close()
        cmd = Nothing

       end using
        


        PonerCorrida2(CorridaID)

    End Sub

    Private Sub PonerCorrida2(ByVal CorridaID As Guid)
        Dim v As clsValidar
        
        Dim Comando As SqlClient.SqlCommand
        Dim Lector As SqlClient.SqlDataReader

        
        using cnn As New SqlClient.SqlConnection(scnn)
        
        cnn.Open()

        v = New clsValidar



        If IsDBNull(CorridaID) Then

            Exit Sub
        End If
        Comando = New SqlClient.SqlCommand("PPonerCorrida2", cnn)
        Comando.CommandTimeout = 300
        Comando.CommandType = CommandType.StoredProcedure
        Comando.Parameters.Add(New System.Data.SqlClient.SqlParameter("@CorridaID", System.Data.SqlDbType.UniqueIdentifier, 16, "CorridaID"))
        Comando.Parameters("@CorridaID").Value = CorridaID

        Lector = Comando.ExecuteReader()
        Do While Lector.Read
            If IsDBNull(Lector!M1) = False Then
                GridEX1.RootTable.Columns("P1").Visible = True
                GridEX1.RootTable.Columns("P1").Caption = Lector!M1
                Me.M1.Visible = True
                Me.M1.Text = Lector!M1
            Else
                GridEX1.RootTable.Columns("P1").Visible = False
                Me.M1.Visible = False
            End If
            If IsDBNull(Lector!M2) = False Then
                GridEX1.RootTable.Columns("P2").Visible = True
                GridEX1.RootTable.Columns("P2").Caption = Lector!M2
                Me.M2.Visible = True
                Me.M2.Text = Lector!M2
            Else
                GridEX1.RootTable.Columns("P2").Visible = False
                Me.M2.Visible = False
            End If
            If IsDBNull(Lector!M3) = False Then
                GridEX1.RootTable.Columns("P3").Visible = True
                GridEX1.RootTable.Columns("P3").Caption = Lector!M3
                Me.M3.Visible = True
                Me.M3.Text = Lector!M3
            Else
                GridEX1.RootTable.Columns("P3").Visible = False
                Me.M3.Visible = False
            End If
            If IsDBNull(Lector!M4) = False Then
                GridEX1.RootTable.Columns("P4").Visible = True
                GridEX1.RootTable.Columns("P4").Caption = Lector!M4
                Me.M4.Visible = True
                Me.M4.Text = Lector!M4
            Else
                GridEX1.RootTable.Columns("P4").Visible = False
                Me.M4.Visible = False
            End If
            If IsDBNull(Lector!M5) = False Then
                GridEX1.RootTable.Columns("P5").Visible = True
                GridEX1.RootTable.Columns("P5").Caption = Lector!M5
                Me.M5.Visible = True
                Me.M5.Text = Lector!M5
            Else
                GridEX1.RootTable.Columns("P5").Visible = False
                Me.M5.Visible = False
            End If
            If IsDBNull(Lector!M6) = False Then
                GridEX1.RootTable.Columns("P6").Visible = True
                GridEX1.RootTable.Columns("P6").Caption = Lector!M6
                Me.M6.Visible = True
                Me.M6.Text = Lector!M6
            Else
                GridEX1.RootTable.Columns("P6").Visible = False
                Me.M6.Visible = False
            End If
            If IsDBNull(Lector!M7) = False Then
                GridEX1.RootTable.Columns("P7").Visible = True
                GridEX1.RootTable.Columns("P7").Caption = Lector!M7
                Me.M7.Visible = True
                Me.M7.Text = Lector!M7
            Else
                Me.M7.Visible = False
                GridEX1.RootTable.Columns("P7").Visible = False
            End If
            If IsDBNull(Lector!M8) = False Then
                GridEX1.RootTable.Columns("P8").Visible = True
                GridEX1.RootTable.Columns("P8").Caption = Lector!M8
                Me.M8.Visible = True
                Me.M8.Text = Lector!M8
            Else
                GridEX1.RootTable.Columns("P8").Visible = False
                Me.M8.Visible = False
            End If
            If IsDBNull(Lector!M9) = False Then
                GridEX1.RootTable.Columns("P9").Visible = True
                GridEX1.RootTable.Columns("P9").Caption = Lector!M9
                Me.M9.Visible = True
                Me.M9.Text = Lector!M9
            Else
                GridEX1.RootTable.Columns("P9").Visible = False
                Me.M9.Visible = False
            End If
            If IsDBNull(Lector!M10) = False Then
                GridEX1.RootTable.Columns("P10").Visible = True
                GridEX1.RootTable.Columns("P10").Caption = Lector!M10
                Me.M10.Visible = True
                Me.M10.Text = Lector!M10
            Else
                GridEX1.RootTable.Columns("P10").Visible = False
                Me.M10.Visible = False
            End If
            If IsDBNull(Lector!M11) = False Then
                GridEX1.RootTable.Columns("P11").Visible = True
                GridEX1.RootTable.Columns("P11").Caption = Lector!M11
                Me.M11.Visible = True
                Me.M11.Text = Lector!M11
            Else
                GridEX1.RootTable.Columns("P11").Visible = False
                Me.M11.Visible = False
            End If
            If IsDBNull(Lector!M12) = False Then
                GridEX1.RootTable.Columns("P12").Visible = True
                GridEX1.RootTable.Columns("P12").Caption = Lector!M12
                Me.M12.Visible = True
                Me.M12.Text = Lector!M12
            Else
                GridEX1.RootTable.Columns("P12").Visible = False
                Me.M12.Visible = False
            End If
            If IsDBNull(Lector!M13) = False Then
                GridEX1.RootTable.Columns("P13").Visible = True
                GridEX1.RootTable.Columns("P13").Caption = Lector!M13
                Me.M13.Visible = True
                Me.M13.Text = Lector!M13
            Else
                GridEX1.RootTable.Columns("P13").Visible = False
                Me.M13.Visible = False
            End If
            If IsDBNull(Lector!M14) = False Then
                GridEX1.RootTable.Columns("P14").Visible = True
                GridEX1.RootTable.Columns("P14").Caption = Lector!M14
                Me.M14.Visible = True
                Me.M14.Text = Lector!M14
            Else
                GridEX1.RootTable.Columns("P14").Visible = False
                Me.M14.Visible = False
            End If
            If IsDBNull(Lector!M15) = False Then
                GridEX1.RootTable.Columns("P15").Visible = True
                GridEX1.RootTable.Columns("P15").Caption = Lector!M15
                Me.M15.Visible = True
                Me.M15.Text = Lector!M15
            Else
                GridEX1.RootTable.Columns("P15").Visible = False
                Me.M15.Visible = False
            End If
            If IsDBNull(Lector!M16) = False Then
                GridEX1.RootTable.Columns("P16").Visible = True
                GridEX1.RootTable.Columns("P16").Caption = Lector!M16
                Me.M16.Visible = True
                Me.M16.Text = Lector!M16
            Else
                GridEX1.RootTable.Columns("P16").Visible = False
                Me.M16.Visible = False
            End If
            If IsDBNull(Lector!M17) = False Then
                GridEX1.RootTable.Columns("P17").Visible = True
                GridEX1.RootTable.Columns("P17").Caption = Lector!M17
                Me.M17.Visible = True
                Me.M17.Text = Lector!M17
            Else
                GridEX1.RootTable.Columns("P17").Visible = False
                Me.M17.Visible = False
            End If
            If IsDBNull(Lector!M18) = False Then
                GridEX1.RootTable.Columns("P18").Visible = True
                GridEX1.RootTable.Columns("P18").Caption = Lector!M18
                Me.M18.Visible = True
                Me.M18.Text = Lector!M18
            Else
                GridEX1.RootTable.Columns("P18").Visible = False
                Me.M18.Visible = False
            End If
            If IsDBNull(Lector!M19) = False Then
                GridEX1.RootTable.Columns("P19").Visible = True
                GridEX1.RootTable.Columns("P19").Caption = Lector!M19
                Me.M19.Visible = True
                Me.M19.Text = Lector!M19
            Else
                GridEX1.RootTable.Columns("P19").Visible = False
                Me.M19.Visible = False
            End If
            If IsDBNull(Lector!M20) = False Then
                GridEX1.RootTable.Columns("P20").Visible = True
                GridEX1.RootTable.Columns("P20").Caption = Lector!M20
                Me.M20.Visible = True
                Me.M20.Text = Lector!M20
            Else
                GridEX1.RootTable.Columns("P20").Visible = False
                Me.M20.Visible = False
            End If
            If IsDBNull(Lector!M21) = False Then
                GridEX1.RootTable.Columns("P21").Visible = True
                GridEX1.RootTable.Columns("P21").Caption = Lector!M21
                Me.M21.Visible = True
                Me.M21.Text = Lector!M21
            Else
                GridEX1.RootTable.Columns("P21").Visible = False
                Me.M21.Visible = False
            End If
        Loop
        Lector.Close()
        Comando = Nothing
       end using
        


    End Sub

    Private Sub PonerPorDesglozar()
        Dim v As clsValidar
        v = New clsValidar

        P1.Text = v.IniDouble(Label1.Text) - Me.GridEX1.GetTotal(Me.GridEX1.RootTable.Columns("P1"), AggregateFunction.Sum)
        P2.Text = v.IniDouble(Label2.Text) - Me.GridEX1.GetTotal(Me.GridEX1.RootTable.Columns("P2"), AggregateFunction.Sum)
        P3.Text = v.IniDouble(Label3.Text) - Me.GridEX1.GetTotal(Me.GridEX1.RootTable.Columns("P3"), AggregateFunction.Sum)
        P4.Text = v.IniDouble(Label4.Text) - Me.GridEX1.GetTotal(Me.GridEX1.RootTable.Columns("P4"), AggregateFunction.Sum)
        P5.Text = v.IniDouble(Label5.Text) - Me.GridEX1.GetTotal(Me.GridEX1.RootTable.Columns("P5"), AggregateFunction.Sum)
        P6.Text = v.IniDouble(Label6.Text) - Me.GridEX1.GetTotal(Me.GridEX1.RootTable.Columns("P6"), AggregateFunction.Sum)
        P7.Text = v.IniDouble(Label7.Text) - Me.GridEX1.GetTotal(Me.GridEX1.RootTable.Columns("P7"), AggregateFunction.Sum)
        P8.Text = v.IniDouble(Label8.Text) - Me.GridEX1.GetTotal(Me.GridEX1.RootTable.Columns("P8"), AggregateFunction.Sum)
        P9.Text = v.IniDouble(Label9.Text) - Me.GridEX1.GetTotal(Me.GridEX1.RootTable.Columns("P9"), AggregateFunction.Sum)
        P10.Text = v.IniDouble(Label10.Text) - Me.GridEX1.GetTotal(Me.GridEX1.RootTable.Columns("P10"), AggregateFunction.Sum)
        P11.Text = v.IniDouble(Label11.Text) - Me.GridEX1.GetTotal(Me.GridEX1.RootTable.Columns("P11"), AggregateFunction.Sum)
        P12.Text = v.IniDouble(Label12.Text) - Me.GridEX1.GetTotal(Me.GridEX1.RootTable.Columns("P12"), AggregateFunction.Sum)
        P13.Text = v.IniDouble(Label13.Text) - Me.GridEX1.GetTotal(Me.GridEX1.RootTable.Columns("P13"), AggregateFunction.Sum)
        P14.Text = v.IniDouble(Label14.Text) - Me.GridEX1.GetTotal(Me.GridEX1.RootTable.Columns("P14"), AggregateFunction.Sum)
        P15.Text = v.IniDouble(Label15.Text) - Me.GridEX1.GetTotal(Me.GridEX1.RootTable.Columns("P15"), AggregateFunction.Sum)
        P16.Text = v.IniDouble(Label16.Text) - Me.GridEX1.GetTotal(Me.GridEX1.RootTable.Columns("P16"), AggregateFunction.Sum)
        P17.Text = v.IniDouble(Label17.Text) - Me.GridEX1.GetTotal(Me.GridEX1.RootTable.Columns("P17"), AggregateFunction.Sum)
        P18.Text = v.IniDouble(Label18.Text) - Me.GridEX1.GetTotal(Me.GridEX1.RootTable.Columns("P18"), AggregateFunction.Sum)
        P19.Text = v.IniDouble(Label19.Text) - Me.GridEX1.GetTotal(Me.GridEX1.RootTable.Columns("P19"), AggregateFunction.Sum)
        P20.Text = v.IniDouble(Label20.Text) - Me.GridEX1.GetTotal(Me.GridEX1.RootTable.Columns("P20"), AggregateFunction.Sum)
        P21.Text = v.IniDouble(Label21.Text) - Me.GridEX1.GetTotal(Me.GridEX1.RootTable.Columns("P21"), AggregateFunction.Sum)
        PCantidad.Text = v.IniDouble(LabelCantidad.Text) - Me.GridEX1.GetTotal(Me.GridEX1.RootTable.Columns("Cantidad"), AggregateFunction.Sum)


        T1.Text = Me.GridEX1.GetTotal(Me.GridEX1.RootTable.Columns("P1"), AggregateFunction.Sum)
        T2.Text = Me.GridEX1.GetTotal(Me.GridEX1.RootTable.Columns("P2"), AggregateFunction.Sum)
        T3.Text = Me.GridEX1.GetTotal(Me.GridEX1.RootTable.Columns("P3"), AggregateFunction.Sum)
        T4.Text = Me.GridEX1.GetTotal(Me.GridEX1.RootTable.Columns("P4"), AggregateFunction.Sum)
        T5.Text = Me.GridEX1.GetTotal(Me.GridEX1.RootTable.Columns("P5"), AggregateFunction.Sum)
        T6.Text = Me.GridEX1.GetTotal(Me.GridEX1.RootTable.Columns("P6"), AggregateFunction.Sum)
        T7.Text = Me.GridEX1.GetTotal(Me.GridEX1.RootTable.Columns("P7"), AggregateFunction.Sum)
        T8.Text = Me.GridEX1.GetTotal(Me.GridEX1.RootTable.Columns("P8"), AggregateFunction.Sum)
        T9.Text = Me.GridEX1.GetTotal(Me.GridEX1.RootTable.Columns("P9"), AggregateFunction.Sum)
        T10.Text = Me.GridEX1.GetTotal(Me.GridEX1.RootTable.Columns("P10"), AggregateFunction.Sum)

        T11.Text = Me.GridEX1.GetTotal(Me.GridEX1.RootTable.Columns("P11"), AggregateFunction.Sum)
        T12.Text = Me.GridEX1.GetTotal(Me.GridEX1.RootTable.Columns("P12"), AggregateFunction.Sum)
        T13.Text = Me.GridEX1.GetTotal(Me.GridEX1.RootTable.Columns("P13"), AggregateFunction.Sum)
        T14.Text = Me.GridEX1.GetTotal(Me.GridEX1.RootTable.Columns("P14"), AggregateFunction.Sum)
        T15.Text = Me.GridEX1.GetTotal(Me.GridEX1.RootTable.Columns("P15"), AggregateFunction.Sum)

        T16.Text = Me.GridEX1.GetTotal(Me.GridEX1.RootTable.Columns("P16"), AggregateFunction.Sum)
        T17.Text = Me.GridEX1.GetTotal(Me.GridEX1.RootTable.Columns("P17"), AggregateFunction.Sum)
        T18.Text = Me.GridEX1.GetTotal(Me.GridEX1.RootTable.Columns("P18"), AggregateFunction.Sum)
        T19.Text = Me.GridEX1.GetTotal(Me.GridEX1.RootTable.Columns("P19"), AggregateFunction.Sum)
        T20.Text = Me.GridEX1.GetTotal(Me.GridEX1.RootTable.Columns("P20"), AggregateFunction.Sum)
        T21.Text = Me.GridEX1.GetTotal(Me.GridEX1.RootTable.Columns("P21"), AggregateFunction.Sum)
        '    TCantidad.Text = Me.GridEX1.GetTotal(Me.GridEX1.RootTable.Columns("Cantidad"), AggregateFunction.Sum)
        TCantidad.Text = LabelCantidad.Text - PCantidad.Text

    End Sub

    Private Sub GridEX1_CellUpdated(ByVal sender As Object, ByVal e As Windows.GridEX.ColumnActionEventArgs) Handles GridEX1.CellUpdated
        CalculateDetailTotal()
    End Sub
    Private Function RenglonDelPedido() As Integer
        
        Dim cmd As SqlClient.SqlCommand
        Dim rs As SqlClient.SqlDataReader

        
        using cnn As New SqlClient.SqlConnection(scnn)
        
        cnn.Open()



        cmd = New SqlClient.SqlCommand("PRenglonDelPedido", cnn)
        cmd.CommandType = CommandType.StoredProcedure
        cmd.Parameters.Add(New System.Data.SqlClient.SqlParameter("@Pedidos_DetailsID", System.Data.SqlDbType.UniqueIdentifier, 16, "Pedidos_DetailsID"))
        cmd.Parameters("@Pedidos_DetailsID").Value = Pedidos_DetailsID


        rs = cmd.ExecuteReader()
        RenglonDelPedido = 1
        Do While rs.Read
            RenglonDelPedido = rs!Ultimo
        Loop
        rs.Close()
        cmd = Nothing
       end using
        




    End Function
    Private Sub CalculateDetailTotal()
        Dim row As GridEXRow
        Dim v As clsValidar
        Dim Cantidad As Double

        v = New clsValidar
        row = Me.GridEX1.GetRow()




        Cantidad = CSng(v.IniDouble(row.Cells("P1").Value) + v.IniDouble(row.Cells("P2").Value) + v.IniDouble(row.Cells("P3").Value) + v.IniDouble(row.Cells("P4").Value) + v.IniDouble(row.Cells("P5").Value) + v.IniDouble(row.Cells("P6").Value) + v.IniDouble(row.Cells("P7").Value) + v.IniDouble(row.Cells("P8").Value) + v.IniDouble(row.Cells("P9").Value) + v.IniDouble(row.Cells("P10").Value) + v.IniDouble(row.Cells("P11").Value) + v.IniDouble(row.Cells("P12").Value) + v.IniDouble(row.Cells("P13").Value) + v.IniDouble(row.Cells("P14").Value) + v.IniDouble(row.Cells("P15").Value) + v.IniDouble(row.Cells("P16").Value) + v.IniDouble(row.Cells("P17").Value) + v.IniDouble(row.Cells("P18").Value) + v.IniDouble(row.Cells("P19").Value) + v.IniDouble(row.Cells("P20").Value) + v.IniDouble(row.Cells("P21").Value) + v.IniDouble(row.Cells("P22").Value) + v.IniDouble(row.Cells("P23").Value) + v.IniDouble(row.Cells("P24").Value) + v.IniDouble(row.Cells("P25").Value) + v.IniDouble(row.Cells("P26").Value) + v.IniDouble(row.Cells("P27").Value) + v.IniDouble(row.Cells("P28").Value) + v.IniDouble(row.Cells("P29").Value) + v.IniDouble(row.Cells("P30").Value))
        Me.GridEX1.SetValue("Cantidad", Cantidad)


    End Sub

    Private Sub txtLoteTamaño_Leave(ByVal sender As Object, ByVal e As System.EventArgs) Handles txtLoteTamaño.Leave
        Parametros_Generales_TamañoDelLote_Update(Me.txtLoteTamaño.Text)
    End Sub

    Private Sub Desgloce_ProgramacionProporcional_Generar()
        
        Dim Comando As SqlClient.SqlCommand

        If Me.txtLoteTamaño.Text = "0" Then
            MsgBox("Necesita indicar el tamaño del lote !!!")
            Exit Sub
        End If

        
        using cnn As New SqlClient.SqlConnection(scnn)
        
        cnn.Open()

        Comando = New SqlClient.SqlCommand("PDesgloce_ProgramacionProporcional_Generar", cnn)
        Comando.CommandType = CommandType.StoredProcedure
        Comando.CommandTimeout = 300
        Comando.Parameters.Add(New System.Data.SqlClient.SqlParameter("@Pedidos_DetailsID", System.Data.SqlDbType.UniqueIdentifier, 16, "Pedidos_DetailsID"))
        Comando.Parameters.Add(New System.Data.SqlClient.SqlParameter("@Tamaño", System.Data.SqlDbType.Int, 4, "Tamaño"))
        Comando.Parameters("@Pedidos_DetailsID").Value = Pedidos_DetailsID
        Comando.Parameters("@Tamaño").Value = Me.txtLoteTamaño.Text

        Comando.ExecuteNonQuery()
        Comando = Nothing
       end using
        





        Restaurar()

    End Sub

    Private Sub chkLoteTamaño_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles chkLoteTamaño.CheckedChanged
        If Me.chkLoteTamaño.Checked Then
            Me.txtLoteTamaño.Visible = True
            Me.txtCantidadDeLotes.Visible = False
        End If
    End Sub

    Private Sub chkCantidadDeLotes_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles chkCantidadDeLotes.CheckedChanged
        If Me.chkCantidadDeLotes.Visible Then
            Me.txtCantidadDeLotes.Visible = True
            Me.txtLoteTamaño.Visible = False
        End If
    End Sub

    Private Sub RadioButton1_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs)
        If Me.chkProgramacionDistribuida.Checked Then
            Me.txtLoteTamaño.Visible = True
            Me.txtCantidadDeLotes.Visible = False
        End If
    End Sub

    Private Sub chkProgramacionProporcional_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles chkProgramacionProporcional.CheckedChanged
        If Me.chkProgramacionProporcional.Checked Then
            Me.txtLoteTamaño.Visible = True
            Me.txtCantidadDeLotes.Visible = False
        End If
    End Sub


    Private Sub Desgloce_ProgramacionDistribuida_Generar()
        
        Dim Comando As SqlClient.SqlCommand
        Dim V As New clsValidar


        If Me.txtLoteTamaño.Text = "0" Then
            MsgBox("Necesita indicar el tamaño del lote !!!")
            Exit Sub
        End If
        
        using cnn As New SqlClient.SqlConnection(scnn)
        
        cnn.Open()

        Comando = New SqlClient.SqlCommand("PDesgloce_ProgramacionDistribuida_Generar", cnn)
        Comando.CommandType = CommandType.StoredProcedure
        Comando.CommandTimeout = 300
        Comando.Parameters.Add(New System.Data.SqlClient.SqlParameter("@Pedidos_DetailsID", System.Data.SqlDbType.UniqueIdentifier, 16, "Pedidos_DetailsID"))
        Comando.Parameters.Add(New System.Data.SqlClient.SqlParameter("@Tamaño", System.Data.SqlDbType.Int, 4, "Tamaño"))
        Comando.Parameters("@Pedidos_DetailsID").Value = Pedidos_DetailsID
        Comando.Parameters("@Tamaño").Value = Me.txtLoteTamaño.Text

        Comando.ExecuteNonQuery()
        Comando = Nothing
       end using
        





        Restaurar()

    End Sub

    Private Sub Restaurar()
        Me.Base1.PPedidoDesglosadoSelect.Clear()
        Me.Base1.PedidoDesglosado.Clear()
        Me.SqlSelectCommand1.Parameters("@Pedidos_DetailsID").Value = Pedidos_DetailsID
        Me.SqlDataAdapter1.Fill(Me.Base1.PPedidoDesglosadoSelect)
        App.DataManager.FillPedidoDesglosado(Me.Base1, Pedidos_DetailsID)

        mData = New DataView(Me.Base1.PedidoDesglosado)
        Me.GridEX1.SetDataBinding(mData, "")


    End Sub
    Private Sub SimpleButton2_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles SimpleButton2.Click
        
        Dim Comando As SqlClient.SqlCommand

        
        using cnn As New SqlClient.SqlConnection(scnn)
        
        cnn.Open()

        Comando = New SqlClient.SqlCommand("PPedido_Desglozar", cnn)
        Comando.CommandType = CommandType.StoredProcedure
        Comando.CommandTimeout = 300
        Comando.Parameters.Add(New System.Data.SqlClient.SqlParameter("@Pedidos_DetailsID", System.Data.SqlDbType.UniqueIdentifier, 16, "Pedidos_DetailsID"))
        Comando.Parameters("@Pedidos_DetailsID").Value = Pedidos_DetailsID
        Comando.ExecuteNonQuery()
        Comando = Nothing
       end using
        






        App.DataManager.EditPedidoDesglosado(Nothing)

    End Sub

    Private Sub cmdGenerar_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmdGenerar.Click
        If IsNumeric(Me.txtLoteTamaño.Text) = False Then
            Me.txtLoteTamaño.Text = 0

        End If
        If IsNumeric(Me.txtCantidadDeLotes.Text) = False Then
            Me.txtCantidadDeLotes.Text = 0
        End If
        If Me.chkProgramacionDistribuida.Checked Then
            Desgloce_ProgramacionDistribuida_Generar()
        Else
            If Me.chkProgramacionProporcional.Checked Then
                Me.Desgloce_ProgramacionProporcional_Generar()
            Else
                If Me.rbHormas.Checked Then
                    Desgloce_Horma_Generar()
                Else
                    If Me.chkPorTallas.Checked Then
                        Desgloce_PorTallas_Generar()
                    Else
                        Desgloce_Generar()
                    End If
                End If

            End If
        End If
        Restaurar()

        PonerPorDesglozar()
    End Sub

    Private Sub Desgloce_Horma_Generar()
        
        Dim Comando As SqlClient.SqlCommand
        Dim V As New clsValidar


        If Me.txtLoteTamaño.Text = "0" Then
            MsgBox("Necesita indicar el tamaño del lote !!!")
            Exit Sub
        End If
        
        using cnn As New SqlClient.SqlConnection(scnn)
        
        cnn.Open()

        Comando = New SqlClient.SqlCommand("PDesgloce_Horma_Generar", cnn)
        Comando.CommandType = CommandType.StoredProcedure
        Comando.CommandTimeout = 300
        Comando.Parameters.Add(New System.Data.SqlClient.SqlParameter("@Pedidos_DetailsID", System.Data.SqlDbType.UniqueIdentifier, 16, "Pedidos_DetailsID"))
        Comando.Parameters.Add(New System.Data.SqlClient.SqlParameter("@Tamaño", System.Data.SqlDbType.Int, 4, "Tamaño"))
        Comando.Parameters("@Pedidos_DetailsID").Value = Pedidos_DetailsID
        Comando.Parameters("@Tamaño").Value = Me.txtLoteTamaño.Text

        Comando.ExecuteNonQuery()
        Comando = Nothing
       end using
        





        Restaurar()

    End Sub


    Private Sub Desgloce_Generar()
        
        Dim Comando As SqlClient.SqlCommand

        
        using cnn As New SqlClient.SqlConnection(scnn)
        
        cnn.Open()

        Comando = New SqlClient.SqlCommand("PDesgloce_Generar", cnn)
        Comando.CommandType = CommandType.StoredProcedure
        Comando.CommandTimeout = 300
        Comando.Parameters.Add(New System.Data.SqlClient.SqlParameter("@Pedidos_DetailsID", System.Data.SqlDbType.UniqueIdentifier, 16, "Pedidos_DetailsID"))
        Comando.Parameters.Add(New System.Data.SqlClient.SqlParameter("@Tipo", System.Data.SqlDbType.Int, 4, "Tipo"))
        Comando.Parameters.Add(New System.Data.SqlClient.SqlParameter("@LoteTamaño", System.Data.SqlDbType.Float, 8, "LoteTamaño"))
        Comando.Parameters.Add(New System.Data.SqlClient.SqlParameter("@CantidadDeLotes", System.Data.SqlDbType.Int, 4, "CantidadDeLotes"))
        Comando.Parameters("@Pedidos_DetailsID").Value = Pedidos_DetailsID
        If Me.chkLoteTamaño.Checked Then
            Comando.Parameters("@Tipo").Value = 1
        Else
            If Me.chkCantidadDeLotes.Checked Then
                Comando.Parameters("@Tipo").Value = 2
            Else
                Comando.Parameters("@Tipo").Value = 3
            End If
        End If
        Comando.Parameters("@LoteTamaño").Value = Me.txtLoteTamaño.Text
        Comando.Parameters("@CantidadDeLotes").Value = Me.txtCantidadDeLotes.Text

        Comando.ExecuteNonQuery()
        Comando = Nothing
       end using
        






    End Sub

    Private Sub Exitoso()
        Dim v As New clsValidar

        If v.IniLong(PCantidad.Text) <> 0 Then
            BExitoso = False
        Else
            BExitoso = True
        End If
    End Sub




    Private Sub SimpleButton1_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles SimpleButton1.Click
        PonerDefaults()
        GridEX1.Focus()
        Me.GridEX1.CurrentColumn = Me.GridEX1.RootTable.Columns("P1")

    End Sub

    Private Sub Inicializar()
        Me.SqlConnection1.ConnectionString = Scnn
        Label1.Text = ""
        Label2.Text = ""
        Label3.Text = ""
        Label4.Text = ""
        Label5.Text = ""
        Label6.Text = ""
        Label7.Text = ""
        Label8.Text = ""
        Label9.Text = ""
        Label10.Text = ""
        Label11.Text = ""
        Label12.Text = ""
        Label13.Text = ""
        Label14.Text = ""
        Label15.Text = ""
        Label16.Text = ""
        Label17.Text = ""
        Label18.Text = ""
        Label19.Text = ""
        Label20.Text = ""
        Label21.Text = ""
        LabelCantidad.Text = ""

        P1.Text = ""
        P2.Text = ""
        P3.Text = ""
        P4.Text = ""
        P5.Text = ""
        P6.Text = ""
        P7.Text = ""
        P8.Text = ""
        P9.Text = ""
        P10.Text = ""
        P11.Text = ""
        P12.Text = ""
        P13.Text = ""
        P14.Text = ""
        P15.Text = ""
        P16.Text = ""
        P17.Text = ""
        P18.Text = ""
        P19.Text = ""
        P20.Text = ""
        P21.Text = ""
        PCantidad.Text = ""

        T1.Text = ""
        T2.Text = ""
        T3.Text = ""
        T4.Text = ""
        T5.Text = ""
        T6.Text = ""
        T7.Text = ""
        T8.Text = ""
        T9.Text = ""
        T10.Text = ""
        T11.Text = ""
        T12.Text = ""
        T13.Text = ""
        T14.Text = ""
        T15.Text = ""
        T16.Text = ""
        T17.Text = ""
        T18.Text = ""
        T19.Text = ""
        T20.Text = ""
        T21.Text = ""
        TCantidad.Text = ""

    End Sub

    Public Function SiguientePedidoDesglosado_Folio() As Integer
        
        Dim cmd As SqlClient.SqlCommand
        Dim rs As SqlClient.SqlDataReader
        Dim V As clsValidar

        
        using cnn As New SqlClient.SqlConnection(scnn)
        
        cnn.Open()

        V = New clsValidar

        cmd = New SqlClient.SqlCommand("PPedidoDesglosado_Folio", cnn)
        cmd.CommandType = CommandType.StoredProcedure
        cmd.Parameters.Add(New System.Data.SqlClient.SqlParameter("@Pedidos_DetailsID", System.Data.SqlDbType.UniqueIdentifier, 16, "Pedidos_DetailsID"))
        cmd.Parameters("@Pedidos_DetailsID").Value = Pedidos_DetailsID

        rs = cmd.ExecuteReader()
        SiguientePedidoDesglosado_Folio = 0
        Do While rs.Read
            If IsDBNull(rs!Folio) = False Then

                SiguientePedidoDesglosado_Folio = rs!Folio
            End If
        Loop
        rs.Close()
        cmd = Nothing
       end using
        


    End Function

    Private Sub SimpleButton4_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles SimpleButton4.Click
        If Me.PCantidad.Text > 0 Then
            MsgBox("No se puede desglozar por que faltan mas renglones todavía !!!")
        Else
            Desglosar()
            Me.Close()
        End If
    End Sub

    Private Sub SimpleButton3_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles SimpleButton3.Click
        Me.Close()
    End Sub

    Private Sub Desglosar()
        
        Dim cmd As SqlClient.SqlCommand
        Dim V As clsValidar

        
        using cnn As New SqlClient.SqlConnection(scnn)
        
        cnn.Open()

        V = New clsValidar

        cmd = New SqlClient.SqlCommand("PPedidoDesglosado_Generar", cnn)
        cmd.CommandType = CommandType.StoredProcedure
        cmd.Parameters.Add(New System.Data.SqlClient.SqlParameter("@Pedidos_DetailsID", System.Data.SqlDbType.UniqueIdentifier, 16, "Pedidos_DetailsID"))
        cmd.Parameters("@Pedidos_DetailsID").Value = Pedidos_DetailsID
        cmd.ExecuteNonQuery()
        cmd = Nothing
       end using
        


        BDesglosado = True

    End Sub

 
    Private Sub GridEX1_FormattingRow_1(sender As Object, e As RowLoadEventArgs) Handles GridEX1.FormattingRow

    End Sub
    Private Sub GridEX1_RecordAdded(ByVal sender As Object, ByVal e As System.EventArgs) Handles GridEX1.RecordAdded
        Dim Row As GridEXRow = GridEX1.GetRow


        p(1) = Row.Cells("P1").Value
        p(2) = Row.Cells("P2").Value
        p(3) = Row.Cells("P3").Value
        p(4) = Row.Cells("P4").Value
        p(5) = Row.Cells("P5").Value
        p(6) = Row.Cells("P6").Value
        p(7) = Row.Cells("P7").Value
        p(8) = Row.Cells("P8").Value
        p(9) = Row.Cells("P9").Value
        p(10) = Row.Cells("P10").Value
        p(11) = Row.Cells("P11").Value
        p(12) = Row.Cells("P12").Value
        p(13) = Row.Cells("P13").Value
        p(14) = Row.Cells("P14").Value
        p(15) = Row.Cells("P15").Value
        p(16) = Row.Cells("P16").Value
        p(17) = Row.Cells("P17").Value
        p(18) = Row.Cells("P18").Value
        p(19) = Row.Cells("P19").Value
        p(20) = Row.Cells("P20").Value
        p(21) = Row.Cells("P21").Value
       






        UpdateData()

        PonerPorDesglozar()

        GridEX1.Row = -1


    End Sub


    Private Sub GridEX1_RecordsDeleted(ByVal sender As Object, ByVal e As System.EventArgs) Handles GridEX1.RecordsDeleted
        UpdateData()
        PonerPorDesglozar()
        'PonerDefaults()

    End Sub

    Private Sub GridEX1_DeletingRecords(ByVal sender As Object, ByVal e As System.ComponentModel.CancelEventArgs) Handles GridEX1.DeletingRecords

        'If MsgBox("¿Quieres eliminar este registro?", MsgBoxStyle.YesNo) = MsgBoxResult.Yes Then
        'Else
        '    e.Cancel = True

        'End If
    End Sub

    Private Sub GridEX1_KeyDown(ByVal sender As Object, ByVal e As System.Windows.Forms.KeyEventArgs) Handles GridEX1.KeyDown
        If e.KeyCode = Keys.F12 Then
            PonerDefaults()
        End If
    End Sub
    Private Sub PonerDefaults()
        Dim v As clsValidar

        v = New clsValidar


        If p(1) > v.IniDouble(P1.Text) Then
            p(1) = v.IniDouble(P1.Text)
        End If
        If p(2) > v.IniDouble(P2.Text) Then
            p(2) = v.IniDouble(P2.Text)
        End If
        If p(3) > v.IniDouble(P3.Text) Then
            p(3) = v.IniDouble(P3.Text)
        End If
        If p(4) > v.IniDouble(P4.Text) Then
            p(4) = v.IniDouble(P4.Text)
        End If
        If p(5) > P5.Text Then
            p(5) = v.IniDouble(P5.Text)
        End If
        If p(6) > P6.Text Then
            p(6) = v.IniDouble(P6.Text)
        End If
        If p(7) > P7.Text Then
            p(7) = v.IniDouble(P7.Text)
        End If
        If p(8) > P8.Text Then
            p(8) = v.IniDouble(P8.Text)
        End If
        If p(9) > P9.Text Then
            p(9) = v.IniDouble(P9.Text)
        End If
        If p(10) > P10.Text Then
            p(10) = v.IniDouble(P10.Text)
        End If


        If p(11) > v.IniDouble(P11.Text) Then
            p(11) = v.IniDouble(P11.Text)
        End If
        If p(12) > v.IniDouble(P12.Text) Then
            p(12) = v.IniDouble(P12.Text)
        End If
        If p(13) > v.IniDouble(P13.Text) Then
            p(13) = v.IniDouble(P13.Text)
        End If
        If p(14) > v.IniDouble(P14.Text) Then
            p(14) = v.IniDouble(P14.Text)
        End If
        If p(15) > v.IniDouble(P15.Text) Then
            p(15) = v.IniDouble(P15.Text)
        End If

        If p(16) > v.IniDouble(P16.Text) Then
            p(16) = v.IniDouble(P16.Text)
        End If
        If p(17) > v.IniDouble(P17.Text) Then
            p(17) = v.IniDouble(P17.Text)
        End If
        If p(18) > v.IniDouble(P18.Text) Then
            p(18) = v.IniDouble(P18.Text)
        End If
        If p(19) > v.IniDouble(P19.Text) Then
            p(19) = v.IniDouble(P19.Text)
        End If
        If p(20) > v.IniDouble(P20.Text) Then
            p(20) = v.IniDouble(P20.Text)
        End If
        If p(21) > v.IniDouble(P21.Text) Then
            p(21) = v.IniDouble(P21.Text)
        End If


        GridEX1.SetValue("P1", p(1))
        GridEX1.SetValue("P2", p(2))
        GridEX1.SetValue("P3", p(3))
        GridEX1.SetValue("P4", p(4))
        GridEX1.SetValue("P5", p(5))
        GridEX1.SetValue("P6", p(6))
        GridEX1.SetValue("P7", p(7))
        GridEX1.SetValue("P8", p(8))
        GridEX1.SetValue("P9", p(9))
        GridEX1.SetValue("P10", p(10))

        GridEX1.SetValue("P11", p(11))
        GridEX1.SetValue("P12", p(12))
        GridEX1.SetValue("P13", p(13))
        GridEX1.SetValue("P14", p(14))
        GridEX1.SetValue("P15", p(15))

        GridEX1.SetValue("P16", p(16))
        GridEX1.SetValue("P17", p(17))
        GridEX1.SetValue("P18", p(18))
        GridEX1.SetValue("P19", p(19))
        GridEX1.SetValue("P20", p(20))
        GridEX1.SetValue("P21", p(21))




    End Sub
    Private Sub Desgloce_PorTallas_Generar()

        Dim Comando As SqlClient.SqlCommand
        Dim V As New clsValidar


        If Me.txtLoteTamaño.Text = "0" Then
            MsgBox("Necesita indicar el tamaño del lote !!!")
            Exit Sub
        End If

        Using cnn As New SqlClient.SqlConnection(Scnn)

            cnn.Open()

            Comando = New SqlClient.SqlCommand("PDesgloce_PorTalla_Generar", cnn)
            Comando.CommandType = CommandType.StoredProcedure
            Comando.CommandTimeout = 300
            Comando.Parameters.Add(New System.Data.SqlClient.SqlParameter("@Pedidos_DetailsID", System.Data.SqlDbType.UniqueIdentifier, 16, "Pedidos_DetailsID"))
            Comando.Parameters.Add(New System.Data.SqlClient.SqlParameter("@Tamaño", System.Data.SqlDbType.Int, 4, "Tamaño"))
            Comando.Parameters("@Pedidos_DetailsID").Value = Pedidos_DetailsID
            Comando.Parameters("@Tamaño").Value = Me.txtLoteTamaño.Text

            Comando.ExecuteNonQuery()
            Comando = Nothing
        End Using






        Restaurar()

    End Sub

End Class

