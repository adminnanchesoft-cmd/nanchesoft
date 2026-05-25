
Public Class frmPedidos
    Inherits System.Windows.Forms.Form



#Region " Windows Form Designer generated code "

    Friend WithEvents SqlConnection1 As System.Data.SqlClient.SqlConnection
    Friend WithEvents daPrecio As System.Data.SqlClient.SqlDataAdapter
    Friend WithEvents SqlSelectCommand1_ As System.Data.SqlClient.SqlCommand
    Friend WithEvents daUnidad As System.Data.SqlClient.SqlDataAdapter
    Friend WithEvents SqlSelectCommand3 As System.Data.SqlClient.SqlCommand
    Friend WithEvents daPDD_Producto As System.Data.SqlClient.SqlDataAdapter
    Friend WithEvents sqlPDD_Producto As System.Data.SqlClient.SqlCommand

    Friend WithEvents sqlPDD_ClienteCadena_Enviar As System.Data.SqlClient.SqlCommand
    Friend WithEvents daPDD_ClienteCadena_Enviar As System.Data.SqlClient.SqlDataAdapter

    Friend WithEvents sqlPDD_ClienteCadena As System.Data.SqlClient.SqlCommand
    Friend WithEvents daPDD_ClienteCadena As System.Data.SqlClient.SqlDataAdapter

    Friend WithEvents daPCalendarioProduccionPedidos As System.Data.SqlClient.SqlDataAdapter
    Friend WithEvents MenuItem21 As System.Windows.Forms.MenuItem
    Friend WithEvents MenuItem22 As System.Windows.Forms.MenuItem
    Friend WithEvents MenuItem23 As System.Windows.Forms.MenuItem
    Friend WithEvents GroupBoxCliente As System.Windows.Forms.GroupBox
    Friend WithEvents Label23 As System.Windows.Forms.Label
    Friend WithEvents Label24 As System.Windows.Forms.Label
    Friend WithEvents txtGastosDeEnvio As System.Windows.Forms.TextBox
    Friend WithEvents ComboBox1 As System.Windows.Forms.ComboBox
    Friend WithEvents txtObservaciones3 As System.Windows.Forms.TextBox
    Friend WithEvents Label21 As System.Windows.Forms.Label
    Friend WithEvents txtObservaciones2 As System.Windows.Forms.TextBox
    Friend WithEvents Label20 As System.Windows.Forms.Label
    Friend WithEvents ComboEnviarDireccion As Janus.Windows.GridEX.EditControls.MultiColumnCombo
    Friend WithEvents ComboTemporada_Pedido As Janus.Windows.GridEX.EditControls.MultiColumnCombo
    Friend WithEvents txtDeposito As System.Windows.Forms.TextBox
    Friend WithEvents Label15 As System.Windows.Forms.Label
    Friend WithEvents jsdtFechaCancelacion As Janus.Windows.CalendarCombo.CalendarCombo
    Friend WithEvents Label14 As System.Windows.Forms.Label
    Friend WithEvents jsdtFCancelacionCliente As Janus.Windows.CalendarCombo.CalendarCombo
    Friend WithEvents Label11 As System.Windows.Forms.Label
    Friend WithEvents Label10 As System.Windows.Forms.Label
    Friend WithEvents txtObsGenerales As System.Windows.Forms.TextBox
    Friend WithEvents Label8 As System.Windows.Forms.Label
    Friend WithEvents Label7 As System.Windows.Forms.Label
    Friend WithEvents Label5 As System.Windows.Forms.Label
    Friend WithEvents GridEX2 As Janus.Windows.GridEX.GridEX
    Friend WithEvents Label1 As System.Windows.Forms.Label
    Friend WithEvents btnModificarPrecios As DevExpress.XtraEditors.SimpleButton
    Friend WithEvents txtReferencia As System.Windows.Forms.TextBox
    Friend WithEvents Label4 As System.Windows.Forms.Label
    Friend WithEvents LabDiasCredito As System.Windows.Forms.Label
    Friend WithEvents LabTransporte As System.Windows.Forms.Label
    Friend WithEvents txtNumero As System.Windows.Forms.TextBox
    Friend WithEvents Label3 As System.Windows.Forms.Label
    Friend WithEvents jsdtFechaRecepcion As Janus.Windows.CalendarCombo.CalendarCombo
    Friend WithEvents LabFechaRecepcion As System.Windows.Forms.Label
    Friend WithEvents jsdtFecha As Janus.Windows.CalendarCombo.CalendarCombo
    Friend WithEvents LabFecha As System.Windows.Forms.Label
    Friend WithEvents ComboVendedor As Janus.Windows.GridEX.EditControls.MultiColumnCombo
    Friend WithEvents LabelVendedor As System.Windows.Forms.Label
    Friend WithEvents LabelVenderA As System.Windows.Forms.Label
    Friend WithEvents ComboTransporte As Janus.Windows.GridEX.EditControls.MultiColumnCombo
    Friend WithEvents SqlDataAdapter1 As System.Data.SqlClient.SqlDataAdapter
    Friend WithEvents SqlCommand2 As System.Data.SqlClient.SqlCommand
    Friend WithEvents DsCuentaBancaria1BindingSource As System.Windows.Forms.BindingSource
    Friend WithEvents LookCuentaBancaria As DevExpress.XtraEditors.LookUpEdit
    Friend WithEvents daPDD_CuentaBancaria As System.Data.SqlClient.SqlDataAdapter
    Friend WithEvents sqlPDD_CuentaBancaria As System.Data.SqlClient.SqlCommand
    Friend WithEvents DsNuevo1 As Janus.AdvancedSample.dsNuevo
    Friend WithEvents PDDCuentaBancariaBindingSource As System.Windows.Forms.BindingSource
    Friend WithEvents Button3 As System.Windows.Forms.Button
    Friend WithEvents Label26 As System.Windows.Forms.Label
    Friend WithEvents cmdEmpacado As DevExpress.XtraEditors.ComboBoxEdit
    Friend WithEvents txtUsoDelCFDI As DevExpress.XtraEditors.TextEdit
    Friend WithEvents Label25 As System.Windows.Forms.Label
    Friend WithEvents ComboBoxEdit2 As DevExpress.XtraEditors.ComboBoxEdit
    Friend WithEvents CheckEdit1 As DevExpress.XtraEditors.CheckEdit
    Friend WithEvents MenuItem24 As System.Windows.Forms.MenuItem
    Friend WithEvents MenuItem25 As System.Windows.Forms.MenuItem
    Friend WithEvents DockPanel3 As DevExpress.XtraBars.Docking.DockPanel
    Friend WithEvents DockPanel3_Container As DevExpress.XtraBars.Docking.ControlContainer
    Friend WithEvents PicFoto As System.Windows.Forms.PictureBox
    Friend WithEvents DockManager4 As DevExpress.XtraBars.Docking.DockManager
    Friend WithEvents LabelCondiciones As System.Windows.Forms.Label
    Friend WithEvents Label27 As System.Windows.Forms.Label
    Friend WithEvents ComboManiobras As DevExpress.XtraEditors.ComboBoxEdit
    Friend WithEvents Label28 As System.Windows.Forms.Label
    Friend WithEvents LabelCategoria As System.Windows.Forms.Label
    Friend WithEvents LabelDiasCredito As System.Windows.Forms.Label
    Friend WithEvents GridEX1 As Janus.Windows.GridEX.GridEX
    Friend WithEvents LookCliente As DevExpress.XtraEditors.LookUpEdit
    Friend WithEvents daCliente As System.Data.SqlClient.SqlDataAdapter
    Friend WithEvents sqlCliente As System.Data.SqlClient.SqlCommand
    Friend WithEvents PPedidosSelect02BindingSource As System.Windows.Forms.BindingSource
    Friend WithEvents PPedidos2SelectBindingSource As System.Windows.Forms.BindingSource
    Friend WithEvents DsPedidos2 As Janus.AdvancedSample.dsPedidos2
    Friend WithEvents PClienteListBindingSource As System.Windows.Forms.BindingSource
    Friend WithEvents PClienteListBindingSource1 As System.Windows.Forms.BindingSource
    Friend WithEvents DsPedidoZ21 As Janus.AdvancedSample.dsPedidoZ2
    Friend WithEvents Label18 As System.Windows.Forms.Label
    Friend WithEvents daPDD_EnvioA As System.Data.SqlClient.SqlDataAdapter
    Friend WithEvents SqlCommand1 As System.Data.SqlClient.SqlCommand
    Friend WithEvents DsPDD_EnvioA1 As Janus.AdvancedSample.dsPDD_EnvioA
    Friend WithEvents PDDEnvioABindingSource As System.Windows.Forms.BindingSource
    Friend WithEvents LookEnvioA As DevExpress.XtraEditors.LookUpEdit
    Friend WithEvents PDDEnvioABindingSource1 As System.Windows.Forms.BindingSource
    Friend WithEvents txtOcurreA As DevExpress.XtraEditors.TextEdit
    Friend WithEvents LabelOcurreA As System.Windows.Forms.Label
    Friend WithEvents Label19 As System.Windows.Forms.Label
    Friend WithEvents daPDD_AlmacenesPorUsuario As System.Data.SqlClient.SqlDataAdapter
    Friend WithEvents sqlPDD_AlmacenesPorUsuario As System.Data.SqlClient.SqlCommand
    Friend WithEvents DsPDD_Almacenes1 As Janus.AdvancedSample.dsPDD_Almacenes
    Friend WithEvents LookAlmacen As DevExpress.XtraEditors.LookUpEdit
    Friend WithEvents PDDAlmacenesPorUsuarioBindingSource1 As System.Windows.Forms.BindingSource
    Friend WithEvents DsPDD_AlmacenesPorUsuario1 As Janus.AdvancedSample.dsPDD_AlmacenesPorUsuario
    Friend WithEvents DsPDD_Almacen As Janus.AdvancedSample.dsPDD_Almacen
    Friend WithEvents PDDAlmacenBindingSource As System.Windows.Forms.BindingSource
    Friend WithEvents PDDAlmacenBindingSource1 As System.Windows.Forms.BindingSource
    Friend WithEvents PDDAlmacenesPorUsuarioBindingSource As System.Windows.Forms.BindingSource
    Friend WithEvents LookPaqueteria As DevExpress.XtraEditors.LookUpEdit
    Friend WithEvents PDDPaqueteriaBindingSource As System.Windows.Forms.BindingSource
    Friend WithEvents Ds_PDDPaqueteria1 As Janus.AdvancedSample.Ds_PDDPaqueteria
    Friend WithEvents Label22 As System.Windows.Forms.Label
    Friend WithEvents SqlDataAdapter2 As System.Data.SqlClient.SqlDataAdapter
    Friend WithEvents SqlCommand3 As System.Data.SqlClient.SqlCommand
    Friend WithEvents DsPDDPaqueteria1BindingSource As System.Windows.Forms.BindingSource
    Friend WithEvents hideContainerRight As DevExpress.XtraBars.Docking.AutoHideContainer
    Friend WithEvents sqlPCalendarioProduccionPedidos As System.Data.SqlClient.SqlCommand

    Public Sub New()
        MyBase.New()
        'This call is required by the Windows Form Designer.
        InitializeComponent()

        'Add any initialization after the InitializeComponent() call
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

    Friend WithEvents imlGridImages As System.Windows.Forms.ImageList
    Friend WithEvents ImageList1 As System.Windows.Forms.ImageList
    Friend WithEvents txtClienteDireccion As System.Windows.Forms.TextBox
    Friend WithEvents txtClienteNombreCorto As System.Windows.Forms.TextBox
    Friend WithEvents txtShipCountry As System.Windows.Forms.TextBox
    Friend WithEvents txtClienteCiudad As System.Windows.Forms.TextBox
    Friend WithEvents txtShipAddress As System.Windows.Forms.TextBox
    Friend WithEvents Label6 As System.Windows.Forms.Label
    Friend WithEvents jsmcVendedor As Janus.Windows.GridEX.EditControls.MultiColumnCombo
    Friend WithEvents Panel1 As System.Windows.Forms.Panel
    Friend WithEvents Label2 As System.Windows.Forms.Label
    Friend WithEvents LabImpuestoDinero As System.Windows.Forms.Label
    Friend WithEvents LabT2 As System.Windows.Forms.Label
    Friend WithEvents LabDescuentoDinero As System.Windows.Forms.Label
    Friend WithEvents LabTotal As System.Windows.Forms.Label
    Friend WithEvents btnImprimir As System.Windows.Forms.Button
    Friend WithEvents txtSubTotal As System.Windows.Forms.TextBox
    Friend WithEvents btnDelete As System.Windows.Forms.Button
    Friend WithEvents btnCancel As System.Windows.Forms.Button
    Friend WithEvents dsDataset2 As Janus.AdvancedSample.Dataset2
    Friend WithEvents dsDataset3 As Janus.AdvancedSample.Dataset3
    Friend WithEvents BtnEliminar As System.Windows.Forms.Button
    Friend WithEvents btnUpdate As System.Windows.Forms.Button
    Friend WithEvents dsBase As Janus.AdvancedSample.Base
    Friend WithEvents Dataset51 As Janus.AdvancedSample.Dataset5
    Friend WithEvents DockManager1 As DevExpress.XtraBars.Docking.DockManager
    Friend WithEvents DockPanel1 As DevExpress.XtraBars.Docking.DockPanel
    Friend WithEvents DockPanel1_Container As DevExpress.XtraBars.Docking.ControlContainer
    Friend WithEvents MainMenu1 As System.Windows.Forms.MainMenu
    Friend WithEvents MenuItem1 As System.Windows.Forms.MenuItem
    Friend WithEvents MenuItem2 As System.Windows.Forms.MenuItem
    'Friend WithEvents PedidoDesglosadoView1 As Janus.AdvancedSample.PedidoDesglosadoView
    Friend WithEvents Pedidos_Details_VerConcentradoView1 As Pedidos_Details_VerConcentradoView
    Friend WithEvents MenuItem3 As System.Windows.Forms.MenuItem
    Friend WithEvents MenuItem4 As System.Windows.Forms.MenuItem
    Friend WithEvents MenuItem5 As System.Windows.Forms.MenuItem
    Friend WithEvents MenuItem6 As System.Windows.Forms.MenuItem
    Friend WithEvents MenuItem9 As System.Windows.Forms.MenuItem
    Friend WithEvents MenuItem10 As System.Windows.Forms.MenuItem
    Friend WithEvents MenuItem7 As System.Windows.Forms.MenuItem
    Friend WithEvents MenuItem8 As System.Windows.Forms.MenuItem
    Friend WithEvents MenuItem11 As System.Windows.Forms.MenuItem
    Friend WithEvents txtCantidad As System.Windows.Forms.Label
    Friend WithEvents txtDescuentoDinero As System.Windows.Forms.Label
    Friend WithEvents DockManager2 As DevExpress.XtraBars.Docking.DockManager
    Friend WithEvents DockManager3 As DevExpress.XtraBars.Docking.DockManager
    Friend WithEvents DockPanel2 As DevExpress.XtraBars.Docking.DockPanel
    Friend WithEvents DockPanel2_Container As DevExpress.XtraBars.Docking.ControlContainer
    Friend WithEvents MenuItem12 As System.Windows.Forms.MenuItem
    Friend WithEvents MenuItem13 As System.Windows.Forms.MenuItem
    Friend WithEvents MenuItem14 As System.Windows.Forms.MenuItem
    Friend WithEvents Pedidos_Details_VerInven1 As Janus.AdvancedSample.Pedidos_Details_VerInven
    Friend WithEvents daPDD_Corrida_Atado As System.Data.SqlClient.SqlDataAdapter
    Friend WithEvents SqlConnection2 As System.Data.SqlClient.SqlConnection
    Friend WithEvents sqlPDD_Corrida_Atado As System.Data.SqlClient.SqlCommand
    Friend WithEvents Guava_Data1 As Janus.AdvancedSample.Guava_Data
    Friend WithEvents Label9 As System.Windows.Forms.Label
    Friend WithEvents Label12 As System.Windows.Forms.Label
    Friend WithEvents jsdtFechaElaboracion As Janus.Windows.CalendarCombo.CalendarCombo
    Friend WithEvents jsdtFechaFabrica As Janus.Windows.CalendarCombo.CalendarCombo
    Friend WithEvents Label13 As System.Windows.Forms.Label
    Friend WithEvents GroupBox2 As System.Windows.Forms.GroupBox
    Friend WithEvents Label16 As System.Windows.Forms.Label
    Friend WithEvents Label17 As System.Windows.Forms.Label
    Friend WithEvents daPDD_Temporada_Pedido As System.Data.SqlClient.SqlDataAdapter
    Friend WithEvents sqlPDD_Temporada_Pedido As System.Data.SqlClient.SqlCommand
    Friend WithEvents daPDD_EnviarDireccion As System.Data.SqlClient.SqlDataAdapter
    Friend WithEvents sqlPDD_EnviarDireccion As System.Data.SqlClient.SqlCommand
    Friend WithEvents MenuItem15 As System.Windows.Forms.MenuItem
    Friend WithEvents MenuItem16 As System.Windows.Forms.MenuItem
    Friend WithEvents txtTotal1 As System.Windows.Forms.Label
    Friend WithEvents txtImpuestoDinero1 As System.Windows.Forms.Label
    Friend WithEvents txtT21 As System.Windows.Forms.Label
    Friend WithEvents MenuItem17 As System.Windows.Forms.MenuItem
    Friend WithEvents ProgressBar1 As System.Windows.Forms.ProgressBar
    Friend WithEvents MenuItem18 As System.Windows.Forms.MenuItem
    Friend WithEvents MenuItem19 As System.Windows.Forms.MenuItem
    Friend WithEvents MenuItem20 As System.Windows.Forms.MenuItem
    <System.Diagnostics.DebuggerStepThrough()> Private Sub InitializeComponent()
        Me.components = New System.ComponentModel.Container()
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(frmPedidos))
        Dim GridEXLayout1 As Janus.Windows.GridEX.GridEXLayout = New Janus.Windows.GridEX.GridEXLayout()
        Dim GridEXLayout5 As Janus.Windows.GridEX.GridEXLayout = New Janus.Windows.GridEX.GridEXLayout()
        Dim GridEXLayout4 As Janus.Windows.GridEX.GridEXLayout = New Janus.Windows.GridEX.GridEXLayout()
        Dim GridEXLayout3 As Janus.Windows.GridEX.GridEXLayout = New Janus.Windows.GridEX.GridEXLayout()
        Dim GridEXLayout2 As Janus.Windows.GridEX.GridEXLayout = New Janus.Windows.GridEX.GridEXLayout()
        Dim GridEXLayout6 As Janus.Windows.GridEX.GridEXLayout = New Janus.Windows.GridEX.GridEXLayout()
        Me.imlGridImages = New System.Windows.Forms.ImageList(Me.components)
        Me.ImageList1 = New System.Windows.Forms.ImageList(Me.components)
        Me.txtClienteDireccion = New System.Windows.Forms.TextBox()
        Me.txtClienteNombreCorto = New System.Windows.Forms.TextBox()
        Me.txtShipCountry = New System.Windows.Forms.TextBox()
        Me.txtClienteCiudad = New System.Windows.Forms.TextBox()
        Me.txtShipAddress = New System.Windows.Forms.TextBox()
        Me.Label6 = New System.Windows.Forms.Label()
        Me.jsmcVendedor = New Janus.Windows.GridEX.EditControls.MultiColumnCombo()
        Me.dsBase = New Janus.AdvancedSample.Base()
        Me.Dataset51 = New Janus.AdvancedSample.Dataset5()
        Me.dsDataset2 = New Janus.AdvancedSample.Dataset2()
        Me.dsDataset3 = New Janus.AdvancedSample.Dataset3()
        Me.GroupBox2 = New System.Windows.Forms.GroupBox()
        Me.Label17 = New System.Windows.Forms.Label()
        Me.Label16 = New System.Windows.Forms.Label()
        Me.Panel1 = New System.Windows.Forms.Panel()
        Me.ProgressBar1 = New System.Windows.Forms.ProgressBar()
        Me.txtT21 = New System.Windows.Forms.Label()
        Me.txtImpuestoDinero1 = New System.Windows.Forms.Label()
        Me.txtTotal1 = New System.Windows.Forms.Label()
        Me.jsdtFechaFabrica = New Janus.Windows.CalendarCombo.CalendarCombo()
        Me.Label13 = New System.Windows.Forms.Label()
        Me.jsdtFechaElaboracion = New Janus.Windows.CalendarCombo.CalendarCombo()
        Me.Label12 = New System.Windows.Forms.Label()
        Me.txtDescuentoDinero = New System.Windows.Forms.Label()
        Me.txtCantidad = New System.Windows.Forms.Label()
        Me.btnUpdate = New System.Windows.Forms.Button()
        Me.BtnEliminar = New System.Windows.Forms.Button()
        Me.Label2 = New System.Windows.Forms.Label()
        Me.LabImpuestoDinero = New System.Windows.Forms.Label()
        Me.LabT2 = New System.Windows.Forms.Label()
        Me.LabDescuentoDinero = New System.Windows.Forms.Label()
        Me.LabTotal = New System.Windows.Forms.Label()
        Me.btnImprimir = New System.Windows.Forms.Button()
        Me.txtSubTotal = New System.Windows.Forms.TextBox()
        Me.btnDelete = New System.Windows.Forms.Button()
        Me.btnCancel = New System.Windows.Forms.Button()
        Me.DockManager1 = New DevExpress.XtraBars.Docking.DockManager(Me.components)
        Me.DockPanel1 = New DevExpress.XtraBars.Docking.DockPanel()
        Me.DockPanel1_Container = New DevExpress.XtraBars.Docking.ControlContainer()
        Me.Pedidos_Details_VerConcentradoView1 = New Janus.AdvancedSample.Pedidos_Details_VerConcentradoView()
        Me.MainMenu1 = New System.Windows.Forms.MainMenu(Me.components)
        Me.MenuItem3 = New System.Windows.Forms.MenuItem()
        Me.MenuItem4 = New System.Windows.Forms.MenuItem()
        Me.MenuItem6 = New System.Windows.Forms.MenuItem()
        Me.MenuItem5 = New System.Windows.Forms.MenuItem()
        Me.MenuItem1 = New System.Windows.Forms.MenuItem()
        Me.MenuItem2 = New System.Windows.Forms.MenuItem()
        Me.MenuItem12 = New System.Windows.Forms.MenuItem()
        Me.MenuItem22 = New System.Windows.Forms.MenuItem()
        Me.MenuItem23 = New System.Windows.Forms.MenuItem()
        Me.MenuItem24 = New System.Windows.Forms.MenuItem()
        Me.MenuItem25 = New System.Windows.Forms.MenuItem()
        Me.MenuItem9 = New System.Windows.Forms.MenuItem()
        Me.MenuItem10 = New System.Windows.Forms.MenuItem()
        Me.MenuItem20 = New System.Windows.Forms.MenuItem()
        Me.MenuItem8 = New System.Windows.Forms.MenuItem()
        Me.MenuItem7 = New System.Windows.Forms.MenuItem()
        Me.MenuItem11 = New System.Windows.Forms.MenuItem()
        Me.MenuItem14 = New System.Windows.Forms.MenuItem()
        Me.MenuItem13 = New System.Windows.Forms.MenuItem()
        Me.MenuItem15 = New System.Windows.Forms.MenuItem()
        Me.MenuItem16 = New System.Windows.Forms.MenuItem()
        Me.MenuItem17 = New System.Windows.Forms.MenuItem()
        Me.MenuItem19 = New System.Windows.Forms.MenuItem()
        Me.MenuItem18 = New System.Windows.Forms.MenuItem()
        Me.MenuItem21 = New System.Windows.Forms.MenuItem()
        Me.DockManager2 = New DevExpress.XtraBars.Docking.DockManager(Me.components)
        Me.DockManager3 = New DevExpress.XtraBars.Docking.DockManager(Me.components)
        Me.DockPanel2 = New DevExpress.XtraBars.Docking.DockPanel()
        Me.DockPanel2_Container = New DevExpress.XtraBars.Docking.ControlContainer()
        Me.Pedidos_Details_VerInven1 = New Janus.AdvancedSample.Pedidos_Details_VerInven()
        Me.daPDD_Corrida_Atado = New System.Data.SqlClient.SqlDataAdapter()
        Me.sqlPDD_Corrida_Atado = New System.Data.SqlClient.SqlCommand()
        Me.SqlConnection2 = New System.Data.SqlClient.SqlConnection()
        Me.Guava_Data1 = New Janus.AdvancedSample.Guava_Data()
        Me.daPDD_Temporada_Pedido = New System.Data.SqlClient.SqlDataAdapter()
        Me.sqlPDD_Temporada_Pedido = New System.Data.SqlClient.SqlCommand()
        Me.daPDD_EnviarDireccion = New System.Data.SqlClient.SqlDataAdapter()
        Me.sqlPDD_EnviarDireccion = New System.Data.SqlClient.SqlCommand()
        Me.ComboTransporte = New Janus.Windows.GridEX.EditControls.MultiColumnCombo()
        Me.LabelVenderA = New System.Windows.Forms.Label()
        Me.LabelVendedor = New System.Windows.Forms.Label()
        Me.ComboVendedor = New Janus.Windows.GridEX.EditControls.MultiColumnCombo()
        Me.LabFecha = New System.Windows.Forms.Label()
        Me.jsdtFecha = New Janus.Windows.CalendarCombo.CalendarCombo()
        Me.LabFechaRecepcion = New System.Windows.Forms.Label()
        Me.jsdtFechaRecepcion = New Janus.Windows.CalendarCombo.CalendarCombo()
        Me.Label3 = New System.Windows.Forms.Label()
        Me.txtNumero = New System.Windows.Forms.TextBox()
        Me.LabTransporte = New System.Windows.Forms.Label()
        Me.LabDiasCredito = New System.Windows.Forms.Label()
        Me.Label4 = New System.Windows.Forms.Label()
        Me.txtReferencia = New System.Windows.Forms.TextBox()
        Me.btnModificarPrecios = New DevExpress.XtraEditors.SimpleButton()
        Me.Label1 = New System.Windows.Forms.Label()
        Me.GridEX2 = New Janus.Windows.GridEX.GridEX()
        Me.Label5 = New System.Windows.Forms.Label()
        Me.Label7 = New System.Windows.Forms.Label()
        Me.Label8 = New System.Windows.Forms.Label()
        Me.txtObsGenerales = New System.Windows.Forms.TextBox()
        Me.Label10 = New System.Windows.Forms.Label()
        Me.Label11 = New System.Windows.Forms.Label()
        Me.jsdtFCancelacionCliente = New Janus.Windows.CalendarCombo.CalendarCombo()
        Me.Label14 = New System.Windows.Forms.Label()
        Me.jsdtFechaCancelacion = New Janus.Windows.CalendarCombo.CalendarCombo()
        Me.Label15 = New System.Windows.Forms.Label()
        Me.txtDeposito = New System.Windows.Forms.TextBox()
        Me.ComboTemporada_Pedido = New Janus.Windows.GridEX.EditControls.MultiColumnCombo()
        Me.ComboEnviarDireccion = New Janus.Windows.GridEX.EditControls.MultiColumnCombo()
        Me.Label20 = New System.Windows.Forms.Label()
        Me.txtObservaciones2 = New System.Windows.Forms.TextBox()
        Me.Label21 = New System.Windows.Forms.Label()
        Me.txtObservaciones3 = New System.Windows.Forms.TextBox()
        Me.ComboBox1 = New System.Windows.Forms.ComboBox()
        Me.GroupBoxCliente = New System.Windows.Forms.GroupBox()
        Me.LookPaqueteria = New DevExpress.XtraEditors.LookUpEdit()
        Me.PDDPaqueteriaBindingSource = New System.Windows.Forms.BindingSource(Me.components)
        Me.Ds_PDDPaqueteria1 = New Janus.AdvancedSample.Ds_PDDPaqueteria()
        Me.Label22 = New System.Windows.Forms.Label()
        Me.LookAlmacen = New DevExpress.XtraEditors.LookUpEdit()
        Me.PDDAlmacenesPorUsuarioBindingSource1 = New System.Windows.Forms.BindingSource(Me.components)
        Me.DsPDD_AlmacenesPorUsuario1 = New Janus.AdvancedSample.dsPDD_AlmacenesPorUsuario()
        Me.Label19 = New System.Windows.Forms.Label()
        Me.txtOcurreA = New DevExpress.XtraEditors.TextEdit()
        Me.LabelOcurreA = New System.Windows.Forms.Label()
        Me.LookEnvioA = New DevExpress.XtraEditors.LookUpEdit()
        Me.PDDEnvioABindingSource1 = New System.Windows.Forms.BindingSource(Me.components)
        Me.DsPDD_EnvioA1 = New Janus.AdvancedSample.dsPDD_EnvioA()
        Me.Label18 = New System.Windows.Forms.Label()
        Me.LookCliente = New DevExpress.XtraEditors.LookUpEdit()
        Me.PClienteListBindingSource1 = New System.Windows.Forms.BindingSource(Me.components)
        Me.DsPedidoZ21 = New Janus.AdvancedSample.dsPedidoZ2()
        Me.LabelDiasCredito = New System.Windows.Forms.Label()
        Me.LabelCategoria = New System.Windows.Forms.Label()
        Me.ComboManiobras = New DevExpress.XtraEditors.ComboBoxEdit()
        Me.Label28 = New System.Windows.Forms.Label()
        Me.LabelCondiciones = New System.Windows.Forms.Label()
        Me.Label27 = New System.Windows.Forms.Label()
        Me.txtUsoDelCFDI = New DevExpress.XtraEditors.TextEdit()
        Me.Label25 = New System.Windows.Forms.Label()
        Me.cmdEmpacado = New DevExpress.XtraEditors.ComboBoxEdit()
        Me.Label26 = New System.Windows.Forms.Label()
        Me.Button3 = New System.Windows.Forms.Button()
        Me.LookCuentaBancaria = New DevExpress.XtraEditors.LookUpEdit()
        Me.PDDCuentaBancariaBindingSource = New System.Windows.Forms.BindingSource(Me.components)
        Me.DsNuevo1 = New Janus.AdvancedSample.dsNuevo()
        Me.Label23 = New System.Windows.Forms.Label()
        Me.Label24 = New System.Windows.Forms.Label()
        Me.txtGastosDeEnvio = New System.Windows.Forms.TextBox()
        Me.PClienteListBindingSource = New System.Windows.Forms.BindingSource(Me.components)
        Me.PPedidos2SelectBindingSource = New System.Windows.Forms.BindingSource(Me.components)
        Me.DsPedidos2 = New Janus.AdvancedSample.dsPedidos2()
        Me.SqlDataAdapter1 = New System.Data.SqlClient.SqlDataAdapter()
        Me.SqlCommand2 = New System.Data.SqlClient.SqlCommand()
        Me.DsCuentaBancaria1BindingSource = New System.Windows.Forms.BindingSource(Me.components)
        Me.daPDD_CuentaBancaria = New System.Data.SqlClient.SqlDataAdapter()
        Me.sqlPDD_CuentaBancaria = New System.Data.SqlClient.SqlCommand()
        Me.ComboBoxEdit2 = New DevExpress.XtraEditors.ComboBoxEdit()
        Me.CheckEdit1 = New DevExpress.XtraEditors.CheckEdit()
        Me.DockManager4 = New DevExpress.XtraBars.Docking.DockManager(Me.components)
        Me.hideContainerRight = New DevExpress.XtraBars.Docking.AutoHideContainer()
        Me.DockPanel3 = New DevExpress.XtraBars.Docking.DockPanel()
        Me.DockPanel3_Container = New DevExpress.XtraBars.Docking.ControlContainer()
        Me.PicFoto = New System.Windows.Forms.PictureBox()
        Me.GridEX1 = New Janus.Windows.GridEX.GridEX()
        Me.daCliente = New System.Data.SqlClient.SqlDataAdapter()
        Me.sqlCliente = New System.Data.SqlClient.SqlCommand()
        Me.PPedidosSelect02BindingSource = New System.Windows.Forms.BindingSource(Me.components)
        Me.daPDD_EnvioA = New System.Data.SqlClient.SqlDataAdapter()
        Me.SqlCommand1 = New System.Data.SqlClient.SqlCommand()
        Me.PDDEnvioABindingSource = New System.Windows.Forms.BindingSource(Me.components)
        Me.daPDD_AlmacenesPorUsuario = New System.Data.SqlClient.SqlDataAdapter()
        Me.sqlPDD_AlmacenesPorUsuario = New System.Data.SqlClient.SqlCommand()
        Me.DsPDD_Almacenes1 = New Janus.AdvancedSample.dsPDD_Almacenes()
        Me.DsPDD_Almacen = New Janus.AdvancedSample.dsPDD_Almacen()
        Me.PDDAlmacenBindingSource = New System.Windows.Forms.BindingSource(Me.components)
        Me.PDDAlmacenBindingSource1 = New System.Windows.Forms.BindingSource(Me.components)
        Me.PDDAlmacenesPorUsuarioBindingSource = New System.Windows.Forms.BindingSource(Me.components)
        Me.SqlDataAdapter2 = New System.Data.SqlClient.SqlDataAdapter()
        Me.SqlCommand3 = New System.Data.SqlClient.SqlCommand()
        Me.DsPDDPaqueteria1BindingSource = New System.Windows.Forms.BindingSource(Me.components)
        CType(Me.dsBase, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.Dataset51, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.dsDataset2, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.dsDataset3, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.GroupBox2.SuspendLayout()
        Me.Panel1.SuspendLayout()
        CType(Me.DockManager1, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.DockPanel1.SuspendLayout()
        Me.DockPanel1_Container.SuspendLayout()
        CType(Me.DockManager2, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.DockManager3, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.DockPanel2.SuspendLayout()
        Me.DockPanel2_Container.SuspendLayout()
        CType(Me.Guava_Data1, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.GridEX2, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.GroupBoxCliente.SuspendLayout()
        CType(Me.LookPaqueteria.Properties, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.PDDPaqueteriaBindingSource, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.Ds_PDDPaqueteria1, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.LookAlmacen.Properties, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.PDDAlmacenesPorUsuarioBindingSource1, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.DsPDD_AlmacenesPorUsuario1, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.txtOcurreA.Properties, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.LookEnvioA.Properties, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.PDDEnvioABindingSource1, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.DsPDD_EnvioA1, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.LookCliente.Properties, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.PClienteListBindingSource1, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.DsPedidoZ21, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.ComboManiobras.Properties, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.txtUsoDelCFDI.Properties, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.cmdEmpacado.Properties, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.LookCuentaBancaria.Properties, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.PDDCuentaBancariaBindingSource, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.DsNuevo1, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.PClienteListBindingSource, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.PPedidos2SelectBindingSource, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.DsPedidos2, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.DsCuentaBancaria1BindingSource, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.ComboBoxEdit2.Properties, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.CheckEdit1.Properties, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.DockManager4, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.hideContainerRight.SuspendLayout()
        Me.DockPanel3.SuspendLayout()
        Me.DockPanel3_Container.SuspendLayout()
        CType(Me.PicFoto, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.GridEX1, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.PPedidosSelect02BindingSource, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.PDDEnvioABindingSource, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.DsPDD_Almacenes1, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.DsPDD_Almacen, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.PDDAlmacenBindingSource, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.PDDAlmacenBindingSource1, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.PDDAlmacenesPorUsuarioBindingSource, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.DsPDDPaqueteria1BindingSource, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SuspendLayout()
        '
        'imlGridImages
        '
        Me.imlGridImages.ImageStream = CType(resources.GetObject("imlGridImages.ImageStream"), System.Windows.Forms.ImageListStreamer)
        Me.imlGridImages.TransparentColor = System.Drawing.Color.Transparent
        Me.imlGridImages.Images.SetKeyName(0, "")
        Me.imlGridImages.Images.SetKeyName(1, "")
        Me.imlGridImages.Images.SetKeyName(2, "")
        Me.imlGridImages.Images.SetKeyName(3, "")
        Me.imlGridImages.Images.SetKeyName(4, "")
        '
        'ImageList1
        '
        Me.ImageList1.ImageStream = CType(resources.GetObject("ImageList1.ImageStream"), System.Windows.Forms.ImageListStreamer)
        Me.ImageList1.TransparentColor = System.Drawing.Color.Transparent
        Me.ImageList1.Images.SetKeyName(0, "")
        Me.ImageList1.Images.SetKeyName(1, "")
        Me.ImageList1.Images.SetKeyName(2, "")
        Me.ImageList1.Images.SetKeyName(3, "")
        Me.ImageList1.Images.SetKeyName(4, "")
        '
        'txtClienteDireccion
        '
        Me.txtClienteDireccion.Location = New System.Drawing.Point(16, 56)
        Me.txtClienteDireccion.Multiline = True
        Me.txtClienteDireccion.Name = "txtClienteDireccion"
        Me.txtClienteDireccion.ReadOnly = True
        Me.txtClienteDireccion.Size = New System.Drawing.Size(254, 34)
        Me.txtClienteDireccion.TabIndex = 46
        Me.txtClienteDireccion.TabStop = False
        '
        'txtClienteNombreCorto
        '
        Me.txtClienteNombreCorto.Location = New System.Drawing.Point(104, 8)
        Me.txtClienteNombreCorto.MaxLength = 5
        Me.txtClienteNombreCorto.Name = "txtClienteNombreCorto"
        Me.txtClienteNombreCorto.Size = New System.Drawing.Size(110, 20)
        Me.txtClienteNombreCorto.TabIndex = 38
        '
        'txtShipCountry
        '
        Me.txtShipCountry.Location = New System.Drawing.Point(320, 112)
        Me.txtShipCountry.Name = "txtShipCountry"
        Me.txtShipCountry.Size = New System.Drawing.Size(168, 20)
        Me.txtShipCountry.TabIndex = 48
        '
        'txtClienteCiudad
        '
        Me.txtClienteCiudad.Location = New System.Drawing.Point(16, 88)
        Me.txtClienteCiudad.Name = "txtClienteCiudad"
        Me.txtClienteCiudad.ReadOnly = True
        Me.txtClienteCiudad.Size = New System.Drawing.Size(82, 20)
        Me.txtClienteCiudad.TabIndex = 51
        Me.txtClienteCiudad.TabStop = False
        '
        'txtShipAddress
        '
        Me.txtShipAddress.Location = New System.Drawing.Point(320, 56)
        Me.txtShipAddress.Multiline = True
        Me.txtShipAddress.Name = "txtShipAddress"
        Me.txtShipAddress.Size = New System.Drawing.Size(254, 34)
        Me.txtShipAddress.TabIndex = 43
        '
        'Label6
        '
        Me.Label6.Font = New System.Drawing.Font("Tahoma", 8.25!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label6.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft
        Me.Label6.Location = New System.Drawing.Point(8, 144)
        Me.Label6.Name = "Label6"
        Me.Label6.Size = New System.Drawing.Size(83, 20)
        Me.Label6.TabIndex = 68
        Me.Label6.Text = "Vendedor:"
        Me.Label6.TextAlign = System.Drawing.ContentAlignment.MiddleLeft
        '
        'jsmcVendedor
        '
        Me.jsmcVendedor.BackColor = System.Drawing.SystemColors.Window
        Me.jsmcVendedor.BorderStyle = Janus.Windows.GridEX.BorderStyle.SunkenLight3D
        Me.jsmcVendedor.ControlStyle.ButtonAppearance = Janus.Windows.GridEX.ButtonAppearance.Regular
        Me.jsmcVendedor.DataMember = "Vendedor"
        GridEXLayout1.LayoutString = resources.GetString("GridEXLayout1.LayoutString")
        Me.jsmcVendedor.DesignTimeLayout = GridEXLayout1
        Me.jsmcVendedor.DisplayMember = "Name"
        Me.jsmcVendedor.ForeColor = System.Drawing.SystemColors.WindowText
        Me.jsmcVendedor.HasImage = True
        Me.jsmcVendedor.ImageList = Me.ImageList1
        Me.jsmcVendedor.ImageVerticalAlignment = Janus.Windows.GridEX.ImageVerticalAlignment.Center
        Me.jsmcVendedor.Location = New System.Drawing.Point(96, 144)
        Me.jsmcVendedor.Name = "jsmcVendedor"
        Me.jsmcVendedor.Size = New System.Drawing.Size(185, 22)
        Me.jsmcVendedor.TabIndex = 50
        Me.jsmcVendedor.ValueMember = "VendedorID"
        '
        'dsBase
        '
        Me.dsBase.DataSetName = "Base"
        Me.dsBase.Locale = New System.Globalization.CultureInfo("en-US")
        Me.dsBase.SchemaSerializationMode = System.Data.SchemaSerializationMode.IncludeSchema
        '
        'Dataset51
        '
        Me.Dataset51.DataSetName = "Dataset5"
        Me.Dataset51.Locale = New System.Globalization.CultureInfo("en-US")
        Me.Dataset51.SchemaSerializationMode = System.Data.SchemaSerializationMode.IncludeSchema
        '
        'dsDataset2
        '
        Me.dsDataset2.DataSetName = "Dataset2"
        Me.dsDataset2.Locale = New System.Globalization.CultureInfo("en-US")
        '
        'dsDataset3
        '
        Me.dsDataset3.DataSetName = "Dataset3"
        Me.dsDataset3.Locale = New System.Globalization.CultureInfo("en-US")
        '
        'GroupBox2
        '
        Me.GroupBox2.Controls.Add(Me.Label17)
        Me.GroupBox2.Controls.Add(Me.Label16)
        Me.GroupBox2.Location = New System.Drawing.Point(942, 6)
        Me.GroupBox2.Name = "GroupBox2"
        Me.GroupBox2.Size = New System.Drawing.Size(232, 56)
        Me.GroupBox2.TabIndex = 189
        Me.GroupBox2.TabStop = False
        Me.GroupBox2.Text = "Ayuda:"
        '
        'Label17
        '
        Me.Label17.Location = New System.Drawing.Point(8, 32)
        Me.Label17.Name = "Label17"
        Me.Label17.Size = New System.Drawing.Size(224, 32)
        Me.Label17.TabIndex = 1
        Me.Label17.Text = "F12 Copiar todos los datos de un renglón"
        '
        'Label16
        '
        Me.Label16.Location = New System.Drawing.Point(8, 16)
        Me.Label16.Name = "Label16"
        Me.Label16.Size = New System.Drawing.Size(168, 24)
        Me.Label16.TabIndex = 0
        Me.Label16.Text = "F11 Copiar tallas de un renglón "
        '
        'Panel1
        '
        Me.Panel1.BackColor = System.Drawing.Color.PowderBlue
        Me.Panel1.Controls.Add(Me.ProgressBar1)
        Me.Panel1.Controls.Add(Me.txtT21)
        Me.Panel1.Controls.Add(Me.txtImpuestoDinero1)
        Me.Panel1.Controls.Add(Me.txtTotal1)
        Me.Panel1.Controls.Add(Me.jsdtFechaFabrica)
        Me.Panel1.Controls.Add(Me.Label13)
        Me.Panel1.Controls.Add(Me.jsdtFechaElaboracion)
        Me.Panel1.Controls.Add(Me.Label12)
        Me.Panel1.Controls.Add(Me.txtDescuentoDinero)
        Me.Panel1.Controls.Add(Me.txtCantidad)
        Me.Panel1.Controls.Add(Me.btnUpdate)
        Me.Panel1.Controls.Add(Me.BtnEliminar)
        Me.Panel1.Controls.Add(Me.Label2)
        Me.Panel1.Controls.Add(Me.LabImpuestoDinero)
        Me.Panel1.Controls.Add(Me.LabT2)
        Me.Panel1.Controls.Add(Me.LabDescuentoDinero)
        Me.Panel1.Controls.Add(Me.LabTotal)
        Me.Panel1.Controls.Add(Me.btnImprimir)
        Me.Panel1.Controls.Add(Me.txtSubTotal)
        Me.Panel1.Controls.Add(Me.btnDelete)
        Me.Panel1.Controls.Add(Me.btnCancel)
        Me.Panel1.Controls.Add(Me.GroupBox2)
        Me.Panel1.Dock = System.Windows.Forms.DockStyle.Bottom
        Me.Panel1.Location = New System.Drawing.Point(0, 605)
        Me.Panel1.Name = "Panel1"
        Me.Panel1.Size = New System.Drawing.Size(1263, 75)
        Me.Panel1.TabIndex = 123
        '
        'ProgressBar1
        '
        Me.ProgressBar1.Dock = System.Windows.Forms.DockStyle.Bottom
        Me.ProgressBar1.Location = New System.Drawing.Point(0, 46)
        Me.ProgressBar1.Name = "ProgressBar1"
        Me.ProgressBar1.Size = New System.Drawing.Size(1263, 29)
        Me.ProgressBar1.TabIndex = 338
        Me.ProgressBar1.Visible = False
        '
        'txtT21
        '
        Me.txtT21.Font = New System.Drawing.Font("Microsoft Sans Serif", 9.75!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.txtT21.Location = New System.Drawing.Point(680, 56)
        Me.txtT21.Name = "txtT21"
        Me.txtT21.RightToLeft = System.Windows.Forms.RightToLeft.Yes
        Me.txtT21.Size = New System.Drawing.Size(88, 16)
        Me.txtT21.TabIndex = 337
        Me.txtT21.Text = "0"
        '
        'txtImpuestoDinero1
        '
        Me.txtImpuestoDinero1.Font = New System.Drawing.Font("Microsoft Sans Serif", 9.75!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.txtImpuestoDinero1.Location = New System.Drawing.Point(466, 6)
        Me.txtImpuestoDinero1.Name = "txtImpuestoDinero1"
        Me.txtImpuestoDinero1.RightToLeft = System.Windows.Forms.RightToLeft.Yes
        Me.txtImpuestoDinero1.Size = New System.Drawing.Size(88, 16)
        Me.txtImpuestoDinero1.TabIndex = 336
        Me.txtImpuestoDinero1.Text = "0"
        Me.txtImpuestoDinero1.Visible = False
        '
        'txtTotal1
        '
        Me.txtTotal1.Font = New System.Drawing.Font("Microsoft Sans Serif", 9.75!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.txtTotal1.Location = New System.Drawing.Point(680, 24)
        Me.txtTotal1.Name = "txtTotal1"
        Me.txtTotal1.RightToLeft = System.Windows.Forms.RightToLeft.Yes
        Me.txtTotal1.Size = New System.Drawing.Size(88, 16)
        Me.txtTotal1.TabIndex = 335
        Me.txtTotal1.Text = "0"
        '
        'jsdtFechaFabrica
        '
        Me.jsdtFechaFabrica.BindableValue = New Date(2002, 8, 13, 0, 0, 0, 0)
        Me.jsdtFechaFabrica.BorderStyle = Janus.Windows.CalendarCombo.BorderStyle.SunkenLight3D
        Me.jsdtFechaFabrica.ButtonsStyle.ButtonAppearance = Janus.Windows.CalendarCombo.ButtonAppearance.Light3D
        '
        '
        '
        Me.jsdtFechaFabrica.DropDownCalendar.Location = New System.Drawing.Point(0, 0)
        Me.jsdtFechaFabrica.DropDownCalendar.Name = ""
        Me.jsdtFechaFabrica.DropDownCalendar.Size = New System.Drawing.Size(166, 169)
        Me.jsdtFechaFabrica.DropDownCalendar.TabIndex = 0
        Me.jsdtFechaFabrica.Enabled = False
        Me.jsdtFechaFabrica.Location = New System.Drawing.Point(96, 40)
        Me.jsdtFechaFabrica.Name = "jsdtFechaFabrica"
        Me.jsdtFechaFabrica.Size = New System.Drawing.Size(104, 20)
        Me.jsdtFechaFabrica.TabIndex = 334
        Me.jsdtFechaFabrica.Value = New Date(2002, 8, 13, 0, 0, 0, 0)
        Me.jsdtFechaFabrica.Visible = False
        '
        'Label13
        '
        Me.Label13.Font = New System.Drawing.Font("Tahoma", 8.25!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label13.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft
        Me.Label13.Location = New System.Drawing.Point(8, 40)
        Me.Label13.Name = "Label13"
        Me.Label13.Size = New System.Drawing.Size(72, 24)
        Me.Label13.TabIndex = 333
        Me.Label13.Text = "F. Entrega de fabrica:"
        Me.Label13.TextAlign = System.Drawing.ContentAlignment.MiddleLeft
        Me.Label13.Visible = False
        '
        'jsdtFechaElaboracion
        '
        Me.jsdtFechaElaboracion.BindableValue = New Date(2002, 8, 13, 0, 0, 0, 0)
        Me.jsdtFechaElaboracion.BorderStyle = Janus.Windows.CalendarCombo.BorderStyle.SunkenLight3D
        Me.jsdtFechaElaboracion.ButtonsStyle.ButtonAppearance = Janus.Windows.CalendarCombo.ButtonAppearance.Light3D
        '
        '
        '
        Me.jsdtFechaElaboracion.DropDownCalendar.Location = New System.Drawing.Point(0, 0)
        Me.jsdtFechaElaboracion.DropDownCalendar.Name = ""
        Me.jsdtFechaElaboracion.DropDownCalendar.Size = New System.Drawing.Size(166, 169)
        Me.jsdtFechaElaboracion.DropDownCalendar.TabIndex = 0
        Me.jsdtFechaElaboracion.Enabled = False
        Me.jsdtFechaElaboracion.Location = New System.Drawing.Point(96, 8)
        Me.jsdtFechaElaboracion.Name = "jsdtFechaElaboracion"
        Me.jsdtFechaElaboracion.Size = New System.Drawing.Size(104, 20)
        Me.jsdtFechaElaboracion.TabIndex = 332
        Me.jsdtFechaElaboracion.Value = New Date(2002, 8, 13, 0, 0, 0, 0)
        Me.jsdtFechaElaboracion.Visible = False
        '
        'Label12
        '
        Me.Label12.Font = New System.Drawing.Font("Tahoma", 8.25!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label12.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft
        Me.Label12.Location = New System.Drawing.Point(8, 8)
        Me.Label12.Name = "Label12"
        Me.Label12.Size = New System.Drawing.Size(96, 20)
        Me.Label12.TabIndex = 331
        Me.Label12.Text = "F. Elaboración:"
        Me.Label12.TextAlign = System.Drawing.ContentAlignment.MiddleLeft
        Me.Label12.Visible = False
        '
        'txtDescuentoDinero
        '
        Me.txtDescuentoDinero.Font = New System.Drawing.Font("Microsoft Sans Serif", 9.75!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.txtDescuentoDinero.ForeColor = System.Drawing.Color.RoyalBlue
        Me.txtDescuentoDinero.Location = New System.Drawing.Point(256, 56)
        Me.txtDescuentoDinero.Name = "txtDescuentoDinero"
        Me.txtDescuentoDinero.Size = New System.Drawing.Size(80, 16)
        Me.txtDescuentoDinero.TabIndex = 327
        Me.txtDescuentoDinero.Text = "0"
        Me.txtDescuentoDinero.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        Me.txtDescuentoDinero.Visible = False
        '
        'txtCantidad
        '
        Me.txtCantidad.Font = New System.Drawing.Font("Microsoft Sans Serif", 20.25!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.txtCantidad.ForeColor = System.Drawing.Color.RoyalBlue
        Me.txtCantidad.Location = New System.Drawing.Point(320, 8)
        Me.txtCantidad.Name = "txtCantidad"
        Me.txtCantidad.Size = New System.Drawing.Size(112, 40)
        Me.txtCantidad.TabIndex = 325
        Me.txtCantidad.Text = "0"
        Me.txtCantidad.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        '
        'btnUpdate
        '
        Me.btnUpdate.BackColor = System.Drawing.Color.Gold
        Me.btnUpdate.Font = New System.Drawing.Font("Microsoft Sans Serif", 8.25!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.btnUpdate.ForeColor = System.Drawing.Color.Black
        Me.btnUpdate.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft
        Me.btnUpdate.ImageIndex = 4
        Me.btnUpdate.ImageList = Me.ImageList1
        Me.btnUpdate.Location = New System.Drawing.Point(528, 24)
        Me.btnUpdate.Name = "btnUpdate"
        Me.btnUpdate.Size = New System.Drawing.Size(88, 40)
        Me.btnUpdate.TabIndex = 324
        Me.btnUpdate.Text = "&Guardar y cerrar"
        Me.btnUpdate.TextAlign = System.Drawing.ContentAlignment.MiddleRight
        Me.btnUpdate.UseVisualStyleBackColor = False
        '
        'BtnEliminar
        '
        Me.BtnEliminar.BackColor = System.Drawing.Color.Teal
        Me.BtnEliminar.Font = New System.Drawing.Font("Microsoft Sans Serif", 9.75!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.BtnEliminar.ForeColor = System.Drawing.Color.White
        Me.BtnEliminar.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft
        Me.BtnEliminar.ImageIndex = 7
        Me.BtnEliminar.Location = New System.Drawing.Point(432, 24)
        Me.BtnEliminar.Name = "BtnEliminar"
        Me.BtnEliminar.Size = New System.Drawing.Size(88, 40)
        Me.BtnEliminar.TabIndex = 323
        Me.BtnEliminar.Text = "&Regresar"
        Me.BtnEliminar.TextAlign = System.Drawing.ContentAlignment.MiddleRight
        Me.BtnEliminar.UseVisualStyleBackColor = False
        '
        'Label2
        '
        Me.Label2.AutoSize = True
        Me.Label2.Font = New System.Drawing.Font("Tahoma", 12.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label2.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft
        Me.Label2.Location = New System.Drawing.Point(216, 8)
        Me.Label2.Name = "Label2"
        Me.Label2.Size = New System.Drawing.Size(88, 19)
        Me.Label2.TabIndex = 139
        Me.Label2.Text = "Cantidad:"
        Me.Label2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft
        '
        'LabImpuestoDinero
        '
        Me.LabImpuestoDinero.AutoSize = True
        Me.LabImpuestoDinero.Font = New System.Drawing.Font("Tahoma", 8.25!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.LabImpuestoDinero.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft
        Me.LabImpuestoDinero.Location = New System.Drawing.Point(360, 64)
        Me.LabImpuestoDinero.Name = "LabImpuestoDinero"
        Me.LabImpuestoDinero.Size = New System.Drawing.Size(36, 13)
        Me.LabImpuestoDinero.TabIndex = 133
        Me.LabImpuestoDinero.Text = "I.V.A."
        Me.LabImpuestoDinero.TextAlign = System.Drawing.ContentAlignment.MiddleLeft
        Me.LabImpuestoDinero.Visible = False
        '
        'LabT2
        '
        Me.LabT2.AutoSize = True
        Me.LabT2.Font = New System.Drawing.Font("Tahoma", 8.25!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.LabT2.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft
        Me.LabT2.Location = New System.Drawing.Point(624, 56)
        Me.LabT2.Name = "LabT2"
        Me.LabT2.Size = New System.Drawing.Size(39, 13)
        Me.LabT2.TabIndex = 131
        Me.LabT2.Text = "Total:"
        Me.LabT2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft
        '
        'LabDescuentoDinero
        '
        Me.LabDescuentoDinero.AutoSize = True
        Me.LabDescuentoDinero.Font = New System.Drawing.Font("Tahoma", 8.25!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.LabDescuentoDinero.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft
        Me.LabDescuentoDinero.Location = New System.Drawing.Point(296, 35)
        Me.LabDescuentoDinero.Name = "LabDescuentoDinero"
        Me.LabDescuentoDinero.Size = New System.Drawing.Size(40, 13)
        Me.LabDescuentoDinero.TabIndex = 130
        Me.LabDescuentoDinero.Text = "Desc.:"
        Me.LabDescuentoDinero.TextAlign = System.Drawing.ContentAlignment.MiddleLeft
        Me.LabDescuentoDinero.Visible = False
        '
        'LabTotal
        '
        Me.LabTotal.AutoSize = True
        Me.LabTotal.Font = New System.Drawing.Font("Tahoma", 8.25!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.LabTotal.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft
        Me.LabTotal.Location = New System.Drawing.Point(624, 26)
        Me.LabTotal.Name = "LabTotal"
        Me.LabTotal.Size = New System.Drawing.Size(58, 13)
        Me.LabTotal.TabIndex = 129
        Me.LabTotal.Text = "Subtotal:"
        Me.LabTotal.TextAlign = System.Drawing.ContentAlignment.MiddleLeft
        '
        'btnImprimir
        '
        Me.btnImprimir.Location = New System.Drawing.Point(32, 104)
        Me.btnImprimir.Name = "btnImprimir"
        Me.btnImprimir.Size = New System.Drawing.Size(72, 24)
        Me.btnImprimir.TabIndex = 136
        Me.btnImprimir.Text = "&Imprimir"
        Me.btnImprimir.Visible = False
        '
        'txtSubTotal
        '
        Me.txtSubTotal.BackColor = System.Drawing.SystemColors.Window
        Me.txtSubTotal.Font = New System.Drawing.Font("Tahoma", 8.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.txtSubTotal.Location = New System.Drawing.Point(256, 72)
        Me.txtSubTotal.Name = "txtSubTotal"
        Me.txtSubTotal.ReadOnly = True
        Me.txtSubTotal.Size = New System.Drawing.Size(114, 21)
        Me.txtSubTotal.TabIndex = 134
        Me.txtSubTotal.TabStop = False
        Me.txtSubTotal.Text = "0"
        Me.txtSubTotal.TextAlign = System.Windows.Forms.HorizontalAlignment.Right
        Me.txtSubTotal.Visible = False
        '
        'btnDelete
        '
        Me.btnDelete.FlatStyle = System.Windows.Forms.FlatStyle.System
        Me.btnDelete.Location = New System.Drawing.Point(112, 104)
        Me.btnDelete.Name = "btnDelete"
        Me.btnDelete.Size = New System.Drawing.Size(80, 23)
        Me.btnDelete.TabIndex = 125
        Me.btnDelete.Text = "&Borrar"
        Me.btnDelete.Visible = False
        '
        'btnCancel
        '
        Me.btnCancel.FlatStyle = System.Windows.Forms.FlatStyle.System
        Me.btnCancel.Location = New System.Drawing.Point(416, 104)
        Me.btnCancel.Name = "btnCancel"
        Me.btnCancel.Size = New System.Drawing.Size(75, 23)
        Me.btnCancel.TabIndex = 124
        Me.btnCancel.Text = "&Cancelar"
        Me.btnCancel.Visible = False
        '
        'DockManager1
        '
        Me.DockManager1.Form = Me
        Me.DockManager1.HiddenPanels.AddRange(New DevExpress.XtraBars.Docking.DockPanel() {Me.DockPanel1})
        Me.DockManager1.TopZIndexControls.AddRange(New String() {"DevExpress.XtraBars.BarDockControl", "System.Windows.Forms.StatusBar"})
        '
        'DockPanel1
        '
        Me.DockPanel1.Controls.Add(Me.DockPanel1_Container)
        Me.DockPanel1.Dock = DevExpress.XtraBars.Docking.DockingStyle.Float
        Me.DockPanel1.FloatLocation = New System.Drawing.Point(545, 384)
        Me.DockPanel1.FloatSize = New System.Drawing.Size(460, 439)
        Me.DockPanel1.ID = New System.Guid("51696a1e-46ee-4098-8a06-10b6f49dc814")
        Me.DockPanel1.Location = New System.Drawing.Point(-32768, -32768)
        Me.DockPanel1.Name = "DockPanel1"
        Me.DockPanel1.SavedIndex = 0
        Me.DockPanel1.Size = New System.Drawing.Size(460, 439)
        Me.DockPanel1.Text = "Concentrado de productos"
        Me.DockPanel1.Visibility = DevExpress.XtraBars.Docking.DockVisibility.Hidden
        '
        'DockPanel1_Container
        '
        Me.DockPanel1_Container.Controls.Add(Me.Pedidos_Details_VerConcentradoView1)
        Me.DockPanel1_Container.Location = New System.Drawing.Point(3, 20)
        Me.DockPanel1_Container.Name = "DockPanel1_Container"
        Me.DockPanel1_Container.Size = New System.Drawing.Size(454, 416)
        Me.DockPanel1_Container.TabIndex = 0
        '
        'Pedidos_Details_VerConcentradoView1
        '
        Me.Pedidos_Details_VerConcentradoView1.Dock = System.Windows.Forms.DockStyle.Fill
        Me.Pedidos_Details_VerConcentradoView1.Location = New System.Drawing.Point(0, 0)
        Me.Pedidos_Details_VerConcentradoView1.LookAndFeel.SkinName = "Money Twins"
        Me.Pedidos_Details_VerConcentradoView1.LookAndFeel.UseDefaultLookAndFeel = False
        Me.Pedidos_Details_VerConcentradoView1.Name = "Pedidos_Details_VerConcentradoView1"
        Me.Pedidos_Details_VerConcentradoView1.Size = New System.Drawing.Size(454, 416)
        Me.Pedidos_Details_VerConcentradoView1.TabIndex = 0
        '
        'MainMenu1
        '
        Me.MainMenu1.MenuItems.AddRange(New System.Windows.Forms.MenuItem() {Me.MenuItem3, Me.MenuItem1, Me.MenuItem9})
        '
        'MenuItem3
        '
        Me.MenuItem3.Index = 0
        Me.MenuItem3.MenuItems.AddRange(New System.Windows.Forms.MenuItem() {Me.MenuItem4, Me.MenuItem6, Me.MenuItem5})
        Me.MenuItem3.Text = "Arc&hivo"
        '
        'MenuItem4
        '
        Me.MenuItem4.Index = 0
        Me.MenuItem4.Text = "&Guardar y cerrar"
        '
        'MenuItem6
        '
        Me.MenuItem6.Index = 1
        Me.MenuItem6.Text = "-"
        '
        'MenuItem5
        '
        Me.MenuItem5.Index = 2
        Me.MenuItem5.Text = "&Cerrar"
        '
        'MenuItem1
        '
        Me.MenuItem1.Index = 1
        Me.MenuItem1.MenuItems.AddRange(New System.Windows.Forms.MenuItem() {Me.MenuItem2, Me.MenuItem12, Me.MenuItem22, Me.MenuItem23, Me.MenuItem24, Me.MenuItem25})
        Me.MenuItem1.Text = "&Ver"
        '
        'MenuItem2
        '
        Me.MenuItem2.Index = 0
        Me.MenuItem2.Shortcut = System.Windows.Forms.Shortcut.Ctrl4
        Me.MenuItem2.Text = "&Concentrado de productos"
        '
        'MenuItem12
        '
        Me.MenuItem12.Index = 1
        Me.MenuItem12.Shortcut = System.Windows.Forms.Shortcut.Ctrl5
        Me.MenuItem12.Text = "Inventario"
        '
        'MenuItem22
        '
        Me.MenuItem22.Index = 2
        Me.MenuItem22.Text = "&Alta de cliente"
        '
        'MenuItem23
        '
        Me.MenuItem23.Index = 3
        Me.MenuItem23.Text = "&Modificar datos del cliente"
        '
        'MenuItem24
        '
        Me.MenuItem24.Index = 4
        Me.MenuItem24.Text = "Ocultar foto del producto"
        '
        'MenuItem25
        '
        Me.MenuItem25.Index = 5
        Me.MenuItem25.Text = "Ver foto del producto"
        '
        'MenuItem9
        '
        Me.MenuItem9.Index = 2
        Me.MenuItem9.MenuItems.AddRange(New System.Windows.Forms.MenuItem() {Me.MenuItem10, Me.MenuItem20, Me.MenuItem8, Me.MenuItem7, Me.MenuItem11, Me.MenuItem14, Me.MenuItem13, Me.MenuItem15, Me.MenuItem16, Me.MenuItem17, Me.MenuItem19, Me.MenuItem18, Me.MenuItem21})
        Me.MenuItem9.Text = "&Herramientas"
        '
        'MenuItem10
        '
        Me.MenuItem10.Index = 0
        Me.MenuItem10.Text = "&Desglosar el renglón"
        '
        'MenuItem20
        '
        Me.MenuItem20.Index = 1
        Me.MenuItem20.Text = "Desglosar el pedido completo"
        '
        'MenuItem8
        '
        Me.MenuItem8.Index = 2
        Me.MenuItem8.Text = "-"
        '
        'MenuItem7
        '
        Me.MenuItem7.Index = 3
        Me.MenuItem7.Text = "Concentrar el pedido por producto"
        '
        'MenuItem11
        '
        Me.MenuItem11.Index = 4
        Me.MenuItem11.Text = "Restaurar el pedido concentrado"
        '
        'MenuItem14
        '
        Me.MenuItem14.Index = 5
        Me.MenuItem14.Text = "-"
        '
        'MenuItem13
        '
        Me.MenuItem13.Index = 6
        Me.MenuItem13.Shortcut = System.Windows.Forms.Shortcut.F9
        Me.MenuItem13.Text = "Pedir a bodega"
        '
        'MenuItem15
        '
        Me.MenuItem15.Index = 7
        Me.MenuItem15.Text = "Poder modificar la fecha del pedido"
        '
        'MenuItem16
        '
        Me.MenuItem16.Index = 8
        Me.MenuItem16.Text = "Poder modificar un renglón desglosado"
        '
        'MenuItem17
        '
        Me.MenuItem17.Index = 9
        Me.MenuItem17.Text = "Cancelar un renglón"
        '
        'MenuItem19
        '
        Me.MenuItem19.Index = 10
        Me.MenuItem19.Text = "-"
        '
        'MenuItem18
        '
        Me.MenuItem18.Index = 11
        Me.MenuItem18.Text = "Importación de un archivo de texto"
        '
        'MenuItem21
        '
        Me.MenuItem21.Index = 12
        Me.MenuItem21.Text = "Importación de un pedido existente"
        '
        'DockManager2
        '
        Me.DockManager2.Form = Me
        Me.DockManager2.HiddenPanels.AddRange(New DevExpress.XtraBars.Docking.DockPanel() {Me.DockPanel1})
        Me.DockManager2.TopZIndexControls.AddRange(New String() {"DevExpress.XtraBars.BarDockControl", "System.Windows.Forms.StatusBar"})
        '
        'DockManager3
        '
        Me.DockManager3.Form = Me
        Me.DockManager3.HiddenPanels.AddRange(New DevExpress.XtraBars.Docking.DockPanel() {Me.DockPanel2})
        Me.DockManager3.TopZIndexControls.AddRange(New String() {"DevExpress.XtraBars.BarDockControl", "System.Windows.Forms.StatusBar"})
        '
        'DockPanel2
        '
        Me.DockPanel2.Controls.Add(Me.DockPanel2_Container)
        Me.DockPanel2.Dock = DevExpress.XtraBars.Docking.DockingStyle.Float
        Me.DockPanel2.FloatLocation = New System.Drawing.Point(236, 497)
        Me.DockPanel2.FloatSize = New System.Drawing.Size(835, 94)
        Me.DockPanel2.ID = New System.Guid("b465e9db-e9ec-484b-a702-49828642e552")
        Me.DockPanel2.Location = New System.Drawing.Point(-32768, -32768)
        Me.DockPanel2.Name = "DockPanel2"
        Me.DockPanel2.SavedIndex = 0
        Me.DockPanel2.Size = New System.Drawing.Size(835, 94)
        Me.DockPanel2.Text = "Inventario"
        Me.DockPanel2.Visibility = DevExpress.XtraBars.Docking.DockVisibility.Hidden
        '
        'DockPanel2_Container
        '
        Me.DockPanel2_Container.Controls.Add(Me.Pedidos_Details_VerInven1)
        Me.DockPanel2_Container.Location = New System.Drawing.Point(3, 20)
        Me.DockPanel2_Container.Name = "DockPanel2_Container"
        Me.DockPanel2_Container.Size = New System.Drawing.Size(829, 71)
        Me.DockPanel2_Container.TabIndex = 0
        '
        'Pedidos_Details_VerInven1
        '
        Me.Pedidos_Details_VerInven1.Appearance.BackColor = System.Drawing.Color.White
        Me.Pedidos_Details_VerInven1.Appearance.Options.UseBackColor = True
        Me.Pedidos_Details_VerInven1.Dock = System.Windows.Forms.DockStyle.Fill
        Me.Pedidos_Details_VerInven1.Location = New System.Drawing.Point(0, 0)
        Me.Pedidos_Details_VerInven1.Name = "Pedidos_Details_VerInven1"
        Me.Pedidos_Details_VerInven1.Size = New System.Drawing.Size(829, 71)
        Me.Pedidos_Details_VerInven1.TabIndex = 125
        '
        'daPDD_Corrida_Atado
        '
        Me.daPDD_Corrida_Atado.SelectCommand = Me.sqlPDD_Corrida_Atado
        Me.daPDD_Corrida_Atado.TableMappings.AddRange(New System.Data.Common.DataTableMapping() {New System.Data.Common.DataTableMapping("Table", "Corrida_Atado", New System.Data.Common.DataColumnMapping() {New System.Data.Common.DataColumnMapping("Corrida_AtadoID", "Corrida_AtadoID"), New System.Data.Common.DataColumnMapping("CorridaID", "CorridaID"), New System.Data.Common.DataColumnMapping("Nombre", "Nombre"), New System.Data.Common.DataColumnMapping("P1", "P1"), New System.Data.Common.DataColumnMapping("P2", "P2"), New System.Data.Common.DataColumnMapping("P3", "P3"), New System.Data.Common.DataColumnMapping("P4", "P4"), New System.Data.Common.DataColumnMapping("P5", "P5"), New System.Data.Common.DataColumnMapping("P6", "P6"), New System.Data.Common.DataColumnMapping("P7", "P7"), New System.Data.Common.DataColumnMapping("P8", "P8"), New System.Data.Common.DataColumnMapping("P9", "P9"), New System.Data.Common.DataColumnMapping("P10", "P10"), New System.Data.Common.DataColumnMapping("P11", "P11"), New System.Data.Common.DataColumnMapping("P12", "P12"), New System.Data.Common.DataColumnMapping("P13", "P13"), New System.Data.Common.DataColumnMapping("P14", "P14"), New System.Data.Common.DataColumnMapping("P15", "P15"), New System.Data.Common.DataColumnMapping("P16", "P16"), New System.Data.Common.DataColumnMapping("P17", "P17"), New System.Data.Common.DataColumnMapping("P18", "P18"), New System.Data.Common.DataColumnMapping("P19", "P19"), New System.Data.Common.DataColumnMapping("P20", "P20"), New System.Data.Common.DataColumnMapping("P21", "P21"), New System.Data.Common.DataColumnMapping("P22", "P22"), New System.Data.Common.DataColumnMapping("P23", "P23"), New System.Data.Common.DataColumnMapping("P24", "P24"), New System.Data.Common.DataColumnMapping("P25", "P25"), New System.Data.Common.DataColumnMapping("P26", "P26"), New System.Data.Common.DataColumnMapping("P27", "P27"), New System.Data.Common.DataColumnMapping("P28", "P28"), New System.Data.Common.DataColumnMapping("P29", "P29"), New System.Data.Common.DataColumnMapping("P30", "P30")})})
        '
        'sqlPDD_Corrida_Atado
        '
        Me.sqlPDD_Corrida_Atado.CommandText = "[PDD_Corrida_Atado]"
        Me.sqlPDD_Corrida_Atado.CommandType = System.Data.CommandType.StoredProcedure
        Me.sqlPDD_Corrida_Atado.Connection = Me.SqlConnection2
        Me.sqlPDD_Corrida_Atado.Parameters.AddRange(New System.Data.SqlClient.SqlParameter() {New System.Data.SqlClient.SqlParameter("@RETURN_VALUE", System.Data.SqlDbType.Int, 4, System.Data.ParameterDirection.ReturnValue, False, CType(0, Byte), CType(0, Byte), "", System.Data.DataRowVersion.Current, Nothing)})
        '
        'SqlConnection2
        '
        Me.SqlConnection2.ConnectionString = "Data Source=HP6200-4\Valare;Initial Catalog=Orange;User ID=Leonel"
        Me.SqlConnection2.FireInfoMessageEventOnUserErrors = False
        '
        'Guava_Data1
        '
        Me.Guava_Data1.DataSetName = "Guava_Data"
        Me.Guava_Data1.Locale = New System.Globalization.CultureInfo("en-US")
        Me.Guava_Data1.SchemaSerializationMode = System.Data.SchemaSerializationMode.IncludeSchema
        '
        'daPDD_Temporada_Pedido
        '
        Me.daPDD_Temporada_Pedido.SelectCommand = Me.sqlPDD_Temporada_Pedido
        Me.daPDD_Temporada_Pedido.TableMappings.AddRange(New System.Data.Common.DataTableMapping() {New System.Data.Common.DataTableMapping("Table", "PDD_Temporada_Pedido", New System.Data.Common.DataColumnMapping() {New System.Data.Common.DataColumnMapping("EnviarNombre", "EnviarNombre")})})
        '
        'sqlPDD_Temporada_Pedido
        '
        Me.sqlPDD_Temporada_Pedido.CommandText = "[PDD_Temporada_Pedido]"
        Me.sqlPDD_Temporada_Pedido.CommandType = System.Data.CommandType.StoredProcedure
        Me.sqlPDD_Temporada_Pedido.Connection = Me.SqlConnection2
        Me.sqlPDD_Temporada_Pedido.Parameters.AddRange(New System.Data.SqlClient.SqlParameter() {New System.Data.SqlClient.SqlParameter("@RETURN_VALUE", System.Data.SqlDbType.Int, 4, System.Data.ParameterDirection.ReturnValue, False, CType(0, Byte), CType(0, Byte), "", System.Data.DataRowVersion.Current, Nothing), New System.Data.SqlClient.SqlParameter("@EmpresaID", System.Data.SqlDbType.UniqueIdentifier, 16)})
        '
        'daPDD_EnviarDireccion
        '
        Me.daPDD_EnviarDireccion.SelectCommand = Me.sqlPDD_EnviarDireccion
        Me.daPDD_EnviarDireccion.TableMappings.AddRange(New System.Data.Common.DataTableMapping() {New System.Data.Common.DataTableMapping("Table", "PDD_EnviarDireccion", New System.Data.Common.DataColumnMapping() {New System.Data.Common.DataColumnMapping("EnviarDireccion", "EnviarDireccion")}), New System.Data.Common.DataTableMapping("Table1", "Table1", New System.Data.Common.DataColumnMapping() {New System.Data.Common.DataColumnMapping("EnviarDireccion", "EnviarDireccion")})})
        '
        'sqlPDD_EnviarDireccion
        '
        Me.sqlPDD_EnviarDireccion.CommandText = "[PDD_EnviarDireccion]"
        Me.sqlPDD_EnviarDireccion.CommandType = System.Data.CommandType.StoredProcedure
        Me.sqlPDD_EnviarDireccion.Connection = Me.SqlConnection2
        Me.sqlPDD_EnviarDireccion.Parameters.AddRange(New System.Data.SqlClient.SqlParameter() {New System.Data.SqlClient.SqlParameter("@RETURN_VALUE", System.Data.SqlDbType.Int, 4, System.Data.ParameterDirection.ReturnValue, False, CType(0, Byte), CType(0, Byte), "", System.Data.DataRowVersion.Current, Nothing), New System.Data.SqlClient.SqlParameter("@EmpresaID", System.Data.SqlDbType.UniqueIdentifier, 16), New System.Data.SqlClient.SqlParameter("@DocumentoID", System.Data.SqlDbType.TinyInt, 1)})
        '
        'ComboTransporte
        '
        Me.ComboTransporte.BackColor = System.Drawing.SystemColors.Window
        Me.ComboTransporte.BorderStyle = Janus.Windows.GridEX.BorderStyle.SunkenLight3D
        Me.ComboTransporte.DataMember = "PDD_Transporte"
        Me.ComboTransporte.DataSource = Me.dsDataset3
        GridEXLayout5.LayoutString = resources.GetString("GridEXLayout5.LayoutString")
        Me.ComboTransporte.DesignTimeLayout = GridEXLayout5
        Me.ComboTransporte.DisplayMember = "Nombre"
        Me.ComboTransporte.ForeColor = System.Drawing.SystemColors.WindowText
        Me.ComboTransporte.HasImage = True
        Me.ComboTransporte.ImageList = Me.imlGridImages
        Me.ComboTransporte.ImageVerticalAlignment = Janus.Windows.GridEX.ImageVerticalAlignment.Center
        Me.ComboTransporte.Location = New System.Drawing.Point(1131, 45)
        Me.ComboTransporte.Name = "ComboTransporte"
        Me.ComboTransporte.Size = New System.Drawing.Size(141, 22)
        Me.ComboTransporte.TabIndex = 9
        Me.ComboTransporte.ValueMember = "TransporteID"
        Me.ComboTransporte.Visible = False
        '
        'LabelVenderA
        '
        Me.LabelVenderA.Font = New System.Drawing.Font("Tahoma", 8.25!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.LabelVenderA.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft
        Me.LabelVenderA.ImageIndex = 2
        Me.LabelVenderA.ImageList = Me.imlGridImages
        Me.LabelVenderA.Location = New System.Drawing.Point(8, 40)
        Me.LabelVenderA.Name = "LabelVenderA"
        Me.LabelVenderA.Size = New System.Drawing.Size(72, 21)
        Me.LabelVenderA.TabIndex = 145
        Me.LabelVenderA.Text = "Cliente:"
        Me.LabelVenderA.TextAlign = System.Drawing.ContentAlignment.MiddleRight
        '
        'LabelVendedor
        '
        Me.LabelVendedor.Font = New System.Drawing.Font("Tahoma", 8.25!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.LabelVendedor.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft
        Me.LabelVendedor.Location = New System.Drawing.Point(368, 63)
        Me.LabelVendedor.Name = "LabelVendedor"
        Me.LabelVendedor.Size = New System.Drawing.Size(72, 20)
        Me.LabelVendedor.TabIndex = 161
        Me.LabelVendedor.Text = "Vendedor:"
        Me.LabelVendedor.TextAlign = System.Drawing.ContentAlignment.MiddleLeft
        '
        'ComboVendedor
        '
        Me.ComboVendedor.BackColor = System.Drawing.SystemColors.Window
        Me.ComboVendedor.BorderStyle = Janus.Windows.GridEX.BorderStyle.SunkenLight3D
        Me.ComboVendedor.DataMember = "PDropDownVendedor"
        Me.ComboVendedor.DataSource = Me.dsDataset3
        GridEXLayout4.LayoutString = resources.GetString("GridEXLayout4.LayoutString")
        Me.ComboVendedor.DesignTimeLayout = GridEXLayout4
        Me.ComboVendedor.DisplayMember = "Nombre"
        Me.ComboVendedor.ForeColor = System.Drawing.SystemColors.WindowText
        Me.ComboVendedor.HasImage = True
        Me.ComboVendedor.ImageList = Me.imlGridImages
        Me.ComboVendedor.ImageVerticalAlignment = Janus.Windows.GridEX.ImageVerticalAlignment.Center
        Me.ComboVendedor.Location = New System.Drawing.Point(432, 63)
        Me.ComboVendedor.Name = "ComboVendedor"
        Me.ComboVendedor.Size = New System.Drawing.Size(163, 22)
        Me.ComboVendedor.TabIndex = 4
        Me.ComboVendedor.ValueMember = "VendedorID"
        '
        'LabFecha
        '
        Me.LabFecha.Font = New System.Drawing.Font("Tahoma", 8.25!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.LabFecha.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft
        Me.LabFecha.Location = New System.Drawing.Point(680, 12)
        Me.LabFecha.Name = "LabFecha"
        Me.LabFecha.Size = New System.Drawing.Size(48, 20)
        Me.LabFecha.TabIndex = 164
        Me.LabFecha.Text = "Fecha:"
        Me.LabFecha.TextAlign = System.Drawing.ContentAlignment.MiddleLeft
        '
        'jsdtFecha
        '
        Me.jsdtFecha.BindableValue = New Date(2002, 8, 13, 0, 0, 0, 0)
        Me.jsdtFecha.BorderStyle = Janus.Windows.CalendarCombo.BorderStyle.SunkenLight3D
        Me.jsdtFecha.ButtonsStyle.ButtonAppearance = Janus.Windows.CalendarCombo.ButtonAppearance.Light3D
        '
        '
        '
        Me.jsdtFecha.DropDownCalendar.Location = New System.Drawing.Point(0, 0)
        Me.jsdtFecha.DropDownCalendar.Name = ""
        Me.jsdtFecha.DropDownCalendar.Size = New System.Drawing.Size(166, 169)
        Me.jsdtFecha.DropDownCalendar.TabIndex = 0
        Me.jsdtFecha.Location = New System.Drawing.Point(742, 15)
        Me.jsdtFecha.Name = "jsdtFecha"
        Me.jsdtFecha.Size = New System.Drawing.Size(112, 20)
        Me.jsdtFecha.TabIndex = 2
        Me.jsdtFecha.Value = New Date(2002, 8, 13, 0, 0, 0, 0)
        '
        'LabFechaRecepcion
        '
        Me.LabFechaRecepcion.Font = New System.Drawing.Font("Tahoma", 8.25!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.LabFechaRecepcion.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft
        Me.LabFechaRecepcion.Location = New System.Drawing.Point(369, 113)
        Me.LabFechaRecepcion.Name = "LabFechaRecepcion"
        Me.LabFechaRecepcion.Size = New System.Drawing.Size(112, 20)
        Me.LabFechaRecepcion.TabIndex = 166
        Me.LabFechaRecepcion.Text = "Fecha de entrega:"
        Me.LabFechaRecepcion.TextAlign = System.Drawing.ContentAlignment.MiddleLeft
        '
        'jsdtFechaRecepcion
        '
        Me.jsdtFechaRecepcion.BindableValue = New Date(2002, 8, 13, 0, 0, 0, 0)
        Me.jsdtFechaRecepcion.BorderStyle = Janus.Windows.CalendarCombo.BorderStyle.SunkenLight3D
        Me.jsdtFechaRecepcion.ButtonsStyle.ButtonAppearance = Janus.Windows.CalendarCombo.ButtonAppearance.Light3D
        '
        '
        '
        Me.jsdtFechaRecepcion.DropDownCalendar.Location = New System.Drawing.Point(0, 0)
        Me.jsdtFechaRecepcion.DropDownCalendar.Name = ""
        Me.jsdtFechaRecepcion.DropDownCalendar.Size = New System.Drawing.Size(166, 169)
        Me.jsdtFechaRecepcion.DropDownCalendar.TabIndex = 0
        Me.jsdtFechaRecepcion.Location = New System.Drawing.Point(493, 116)
        Me.jsdtFechaRecepcion.Name = "jsdtFechaRecepcion"
        Me.jsdtFechaRecepcion.Size = New System.Drawing.Size(104, 20)
        Me.jsdtFechaRecepcion.TabIndex = 5
        Me.jsdtFechaRecepcion.Value = New Date(2002, 8, 13, 0, 0, 0, 0)
        '
        'Label3
        '
        Me.Label3.Font = New System.Drawing.Font("Tahoma", 8.25!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label3.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft
        Me.Label3.Location = New System.Drawing.Point(8, 11)
        Me.Label3.Name = "Label3"
        Me.Label3.Size = New System.Drawing.Size(76, 20)
        Me.Label3.TabIndex = 167
        Me.Label3.Text = "Pedido No.:"
        Me.Label3.TextAlign = System.Drawing.ContentAlignment.MiddleLeft
        '
        'txtNumero
        '
        Me.txtNumero.Enabled = False
        Me.txtNumero.Location = New System.Drawing.Point(90, 12)
        Me.txtNumero.Name = "txtNumero"
        Me.txtNumero.Size = New System.Drawing.Size(73, 20)
        Me.txtNumero.TabIndex = 0
        '
        'LabTransporte
        '
        Me.LabTransporte.Font = New System.Drawing.Font("Tahoma", 8.25!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.LabTransporte.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft
        Me.LabTransporte.Location = New System.Drawing.Point(1056, 8)
        Me.LabTransporte.Name = "LabTransporte"
        Me.LabTransporte.Size = New System.Drawing.Size(72, 20)
        Me.LabTransporte.TabIndex = 170
        Me.LabTransporte.Text = "Transporte:"
        Me.LabTransporte.TextAlign = System.Drawing.ContentAlignment.MiddleLeft
        Me.LabTransporte.Visible = False
        '
        'LabDiasCredito
        '
        Me.LabDiasCredito.Font = New System.Drawing.Font("Tahoma", 8.25!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.LabDiasCredito.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft
        Me.LabDiasCredito.Location = New System.Drawing.Point(8, 64)
        Me.LabDiasCredito.Name = "LabDiasCredito"
        Me.LabDiasCredito.Size = New System.Drawing.Size(80, 20)
        Me.LabDiasCredito.TabIndex = 171
        Me.LabDiasCredito.Text = "Condiciones:"
        Me.LabDiasCredito.TextAlign = System.Drawing.ContentAlignment.MiddleLeft
        '
        'Label4
        '
        Me.Label4.Font = New System.Drawing.Font("Tahoma", 8.25!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label4.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft
        Me.Label4.Location = New System.Drawing.Point(169, 12)
        Me.Label4.Name = "Label4"
        Me.Label4.Size = New System.Drawing.Size(72, 20)
        Me.Label4.TabIndex = 173
        Me.Label4.Text = "Referencia:"
        Me.Label4.TextAlign = System.Drawing.ContentAlignment.MiddleLeft
        '
        'txtReferencia
        '
        Me.txtReferencia.Location = New System.Drawing.Point(239, 13)
        Me.txtReferencia.MaxLength = 50
        Me.txtReferencia.Name = "txtReferencia"
        Me.txtReferencia.Size = New System.Drawing.Size(123, 20)
        Me.txtReferencia.TabIndex = 1
        '
        'btnModificarPrecios
        '
        Me.btnModificarPrecios.Location = New System.Drawing.Point(1024, 15)
        Me.btnModificarPrecios.Name = "btnModificarPrecios"
        Me.btnModificarPrecios.Size = New System.Drawing.Size(104, 24)
        Me.btnModificarPrecios.TabIndex = 174
        Me.btnModificarPrecios.Text = "Modificar precios"
        '
        'Label1
        '
        Me.Label1.Location = New System.Drawing.Point(744, 0)
        Me.Label1.Name = "Label1"
        Me.Label1.Size = New System.Drawing.Size(128, 16)
        Me.Label1.TabIndex = 175
        Me.Label1.Text = "Semana de entrega"
        Me.Label1.Visible = False
        '
        'GridEX2
        '
        Me.GridEX2.DataMember = "PCalendarioProduccionPedidos"
        Me.GridEX2.DataSource = Me.Dataset51
        Me.GridEX2.EditorsControlStyle.ButtonAppearance = Janus.Windows.GridEX.ButtonAppearance.Regular
        Me.GridEX2.Font = New System.Drawing.Font("Microsoft Sans Serif", 8.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.GridEX2.GroupByBoxVisible = False
        Me.GridEX2.Location = New System.Drawing.Point(1104, 88)
        Me.GridEX2.Name = "GridEX2"
        Me.GridEX2.Size = New System.Drawing.Size(48, 48)
        Me.GridEX2.TabIndex = 700
        Me.GridEX2.Visible = False
        '
        'Label5
        '
        Me.Label5.Font = New System.Drawing.Font("Tahoma", 8.25!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label5.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft
        Me.Label5.Location = New System.Drawing.Point(8, 88)
        Me.Label5.Name = "Label5"
        Me.Label5.Size = New System.Drawing.Size(80, 20)
        Me.Label5.TabIndex = 177
        Me.Label5.Text = "Temporada:"
        Me.Label5.TextAlign = System.Drawing.ContentAlignment.MiddleLeft
        '
        'Label7
        '
        Me.Label7.Font = New System.Drawing.Font("Tahoma", 8.25!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label7.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft
        Me.Label7.Location = New System.Drawing.Point(8, 112)
        Me.Label7.Name = "Label7"
        Me.Label7.Size = New System.Drawing.Size(80, 20)
        Me.Label7.TabIndex = 178
        Me.Label7.Text = "Evento:"
        Me.Label7.TextAlign = System.Drawing.ContentAlignment.MiddleLeft
        '
        'Label8
        '
        Me.Label8.Font = New System.Drawing.Font("Tahoma", 8.25!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label8.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft
        Me.Label8.Location = New System.Drawing.Point(369, 136)
        Me.Label8.Name = "Label8"
        Me.Label8.Size = New System.Drawing.Size(43, 20)
        Me.Label8.TabIndex = 181
        Me.Label8.Text = "Obs.:"
        Me.Label8.TextAlign = System.Drawing.ContentAlignment.MiddleLeft
        '
        'txtObsGenerales
        '
        Me.txtObsGenerales.Location = New System.Drawing.Point(408, 137)
        Me.txtObsGenerales.Multiline = True
        Me.txtObsGenerales.Name = "txtObsGenerales"
        Me.txtObsGenerales.Size = New System.Drawing.Size(189, 22)
        Me.txtObsGenerales.TabIndex = 7
        '
        'Label10
        '
        Me.Label10.Font = New System.Drawing.Font("Tahoma", 8.25!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label10.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft
        Me.Label10.Location = New System.Drawing.Point(1101, 61)
        Me.Label10.Name = "Label10"
        Me.Label10.Size = New System.Drawing.Size(80, 20)
        Me.Label10.TabIndex = 182
        Me.Label10.Text = "Transporte:"
        Me.Label10.TextAlign = System.Drawing.ContentAlignment.MiddleLeft
        Me.Label10.Visible = False
        '
        'Label11
        '
        Me.Label11.Font = New System.Drawing.Font("Tahoma", 8.25!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label11.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft
        Me.Label11.Location = New System.Drawing.Point(1121, 61)
        Me.Label11.Name = "Label11"
        Me.Label11.Size = New System.Drawing.Size(144, 20)
        Me.Label11.TabIndex = 184
        Me.Label11.Text = "F.Cancelación cliente:"
        Me.Label11.TextAlign = System.Drawing.ContentAlignment.MiddleLeft
        Me.Label11.Visible = False
        '
        'jsdtFCancelacionCliente
        '
        Me.jsdtFCancelacionCliente.BindableValue = New Date(2002, 8, 13, 0, 0, 0, 0)
        Me.jsdtFCancelacionCliente.BorderStyle = Janus.Windows.CalendarCombo.BorderStyle.SunkenLight3D
        Me.jsdtFCancelacionCliente.ButtonsStyle.ButtonAppearance = Janus.Windows.CalendarCombo.ButtonAppearance.Light3D
        '
        '
        '
        Me.jsdtFCancelacionCliente.DropDownCalendar.Location = New System.Drawing.Point(0, 0)
        Me.jsdtFCancelacionCliente.DropDownCalendar.Name = ""
        Me.jsdtFCancelacionCliente.DropDownCalendar.Size = New System.Drawing.Size(166, 169)
        Me.jsdtFCancelacionCliente.DropDownCalendar.TabIndex = 0
        Me.jsdtFCancelacionCliente.Location = New System.Drawing.Point(1124, 84)
        Me.jsdtFCancelacionCliente.Name = "jsdtFCancelacionCliente"
        Me.jsdtFCancelacionCliente.Size = New System.Drawing.Size(112, 20)
        Me.jsdtFCancelacionCliente.TabIndex = 600
        Me.jsdtFCancelacionCliente.Value = New Date(2002, 8, 13, 0, 0, 0, 0)
        Me.jsdtFCancelacionCliente.Visible = False
        '
        'Label14
        '
        Me.Label14.Font = New System.Drawing.Font("Tahoma", 8.25!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label14.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft
        Me.Label14.Location = New System.Drawing.Point(602, 116)
        Me.Label14.Name = "Label14"
        Me.Label14.Size = New System.Drawing.Size(136, 20)
        Me.Label14.TabIndex = 186
        Me.Label14.Text = "Fecha de cancelación:"
        Me.Label14.TextAlign = System.Drawing.ContentAlignment.MiddleLeft
        '
        'jsdtFechaCancelacion
        '
        Me.jsdtFechaCancelacion.BindableValue = New Date(2002, 8, 13, 0, 0, 0, 0)
        Me.jsdtFechaCancelacion.BorderStyle = Janus.Windows.CalendarCombo.BorderStyle.SunkenLight3D
        Me.jsdtFechaCancelacion.ButtonsStyle.ButtonAppearance = Janus.Windows.CalendarCombo.ButtonAppearance.Light3D
        '
        '
        '
        Me.jsdtFechaCancelacion.DropDownCalendar.Location = New System.Drawing.Point(0, 0)
        Me.jsdtFechaCancelacion.DropDownCalendar.Name = ""
        Me.jsdtFechaCancelacion.DropDownCalendar.Size = New System.Drawing.Size(166, 169)
        Me.jsdtFechaCancelacion.DropDownCalendar.TabIndex = 0
        Me.jsdtFechaCancelacion.Location = New System.Drawing.Point(742, 113)
        Me.jsdtFechaCancelacion.Name = "jsdtFechaCancelacion"
        Me.jsdtFechaCancelacion.Size = New System.Drawing.Size(112, 20)
        Me.jsdtFechaCancelacion.TabIndex = 13
        Me.jsdtFechaCancelacion.Value = New Date(2002, 8, 13, 0, 0, 0, 0)
        '
        'Label15
        '
        Me.Label15.Font = New System.Drawing.Font("Tahoma", 8.25!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label15.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft
        Me.Label15.Location = New System.Drawing.Point(600, 40)
        Me.Label15.Name = "Label15"
        Me.Label15.Size = New System.Drawing.Size(128, 20)
        Me.Label15.TabIndex = 188
        Me.Label15.Text = "Anticipo:"
        Me.Label15.TextAlign = System.Drawing.ContentAlignment.MiddleLeft
        '
        'txtDeposito
        '
        Me.txtDeposito.Location = New System.Drawing.Point(709, 40)
        Me.txtDeposito.Name = "txtDeposito"
        Me.txtDeposito.Size = New System.Drawing.Size(145, 20)
        Me.txtDeposito.TabIndex = 10
        Me.txtDeposito.TextAlign = System.Windows.Forms.HorizontalAlignment.Right
        '
        'ComboTemporada_Pedido
        '
        Me.ComboTemporada_Pedido.BackColor = System.Drawing.SystemColors.Window
        Me.ComboTemporada_Pedido.BorderStyle = Janus.Windows.GridEX.BorderStyle.SunkenLight3D
        Me.ComboTemporada_Pedido.DataMember = "PDD_Temporada_Pedido"
        Me.ComboTemporada_Pedido.DataSource = Me.dsBase
        GridEXLayout3.LayoutString = resources.GetString("GridEXLayout3.LayoutString")
        Me.ComboTemporada_Pedido.DesignTimeLayout = GridEXLayout3
        Me.ComboTemporada_Pedido.DisplayMember = "EnviarNombre"
        Me.ComboTemporada_Pedido.ForeColor = System.Drawing.SystemColors.WindowText
        Me.ComboTemporada_Pedido.HasImage = True
        Me.ComboTemporada_Pedido.ImageList = Me.imlGridImages
        Me.ComboTemporada_Pedido.ImageVerticalAlignment = Janus.Windows.GridEX.ImageVerticalAlignment.Center
        Me.ComboTemporada_Pedido.Location = New System.Drawing.Point(90, 88)
        Me.ComboTemporada_Pedido.Name = "ComboTemporada_Pedido"
        Me.ComboTemporada_Pedido.Size = New System.Drawing.Size(272, 22)
        Me.ComboTemporada_Pedido.TabIndex = 6
        Me.ComboTemporada_Pedido.ValueMember = "EnviarNombre"
        '
        'ComboEnviarDireccion
        '
        Me.ComboEnviarDireccion.BackColor = System.Drawing.SystemColors.Window
        Me.ComboEnviarDireccion.BorderStyle = Janus.Windows.GridEX.BorderStyle.SunkenLight3D
        Me.ComboEnviarDireccion.DataMember = "PDD_EnviarDireccion"
        Me.ComboEnviarDireccion.DataSource = Me.dsBase
        GridEXLayout2.LayoutString = resources.GetString("GridEXLayout2.LayoutString")
        Me.ComboEnviarDireccion.DesignTimeLayout = GridEXLayout2
        Me.ComboEnviarDireccion.DisplayMember = "EnviarDireccion"
        Me.ComboEnviarDireccion.ForeColor = System.Drawing.SystemColors.WindowText
        Me.ComboEnviarDireccion.HasImage = True
        Me.ComboEnviarDireccion.ImageList = Me.imlGridImages
        Me.ComboEnviarDireccion.ImageVerticalAlignment = Janus.Windows.GridEX.ImageVerticalAlignment.Center
        Me.ComboEnviarDireccion.Location = New System.Drawing.Point(90, 112)
        Me.ComboEnviarDireccion.Name = "ComboEnviarDireccion"
        Me.ComboEnviarDireccion.Size = New System.Drawing.Size(272, 22)
        Me.ComboEnviarDireccion.TabIndex = 8
        Me.ComboEnviarDireccion.ValueMember = "EnviarDireccion"
        '
        'Label20
        '
        Me.Label20.Font = New System.Drawing.Font("Tahoma", 8.25!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label20.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft
        Me.Label20.Location = New System.Drawing.Point(1023, 15)
        Me.Label20.Name = "Label20"
        Me.Label20.Size = New System.Drawing.Size(104, 20)
        Me.Label20.TabIndex = 190
        Me.Label20.Text = "Planear:"
        Me.Label20.TextAlign = System.Drawing.ContentAlignment.MiddleLeft
        '
        'txtObservaciones2
        '
        Me.txtObservaciones2.Location = New System.Drawing.Point(1026, 38)
        Me.txtObservaciones2.MaxLength = 60
        Me.txtObservaciones2.Name = "txtObservaciones2"
        Me.txtObservaciones2.Size = New System.Drawing.Size(120, 20)
        Me.txtObservaciones2.TabIndex = 589
        '
        'Label21
        '
        Me.Label21.Font = New System.Drawing.Font("Tahoma", 8.25!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label21.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft
        Me.Label21.Location = New System.Drawing.Point(1042, 120)
        Me.Label21.Name = "Label21"
        Me.Label21.Size = New System.Drawing.Size(104, 20)
        Me.Label21.TabIndex = 192
        Me.Label21.Text = "Observaciones 3:"
        Me.Label21.TextAlign = System.Drawing.ContentAlignment.MiddleLeft
        Me.Label21.Visible = False
        '
        'txtObservaciones3
        '
        Me.txtObservaciones3.Location = New System.Drawing.Point(1032, 136)
        Me.txtObservaciones3.MaxLength = 60
        Me.txtObservaciones3.Name = "txtObservaciones3"
        Me.txtObservaciones3.Size = New System.Drawing.Size(120, 20)
        Me.txtObservaciones3.TabIndex = 590
        Me.txtObservaciones3.Visible = False
        '
        'ComboBox1
        '
        Me.ComboBox1.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.ComboBox1.Items.AddRange(New Object() {"NO PLANEAR", " "})
        Me.ComboBox1.Location = New System.Drawing.Point(1026, 39)
        Me.ComboBox1.Name = "ComboBox1"
        Me.ComboBox1.Size = New System.Drawing.Size(120, 21)
        Me.ComboBox1.TabIndex = 517
        Me.ComboBox1.Visible = False
        '
        'GroupBoxCliente
        '
        Me.GroupBoxCliente.BackColor = System.Drawing.Color.Khaki
        Me.GroupBoxCliente.Controls.Add(Me.LookPaqueteria)
        Me.GroupBoxCliente.Controls.Add(Me.Label22)
        Me.GroupBoxCliente.Controls.Add(Me.LookAlmacen)
        Me.GroupBoxCliente.Controls.Add(Me.Label19)
        Me.GroupBoxCliente.Controls.Add(Me.txtOcurreA)
        Me.GroupBoxCliente.Controls.Add(Me.LabelOcurreA)
        Me.GroupBoxCliente.Controls.Add(Me.LookEnvioA)
        Me.GroupBoxCliente.Controls.Add(Me.Label18)
        Me.GroupBoxCliente.Controls.Add(Me.LookCliente)
        Me.GroupBoxCliente.Controls.Add(Me.LabelDiasCredito)
        Me.GroupBoxCliente.Controls.Add(Me.LabelCategoria)
        Me.GroupBoxCliente.Controls.Add(Me.ComboManiobras)
        Me.GroupBoxCliente.Controls.Add(Me.Label28)
        Me.GroupBoxCliente.Controls.Add(Me.LabelCondiciones)
        Me.GroupBoxCliente.Controls.Add(Me.Label27)
        Me.GroupBoxCliente.Controls.Add(Me.txtUsoDelCFDI)
        Me.GroupBoxCliente.Controls.Add(Me.Label25)
        Me.GroupBoxCliente.Controls.Add(Me.cmdEmpacado)
        Me.GroupBoxCliente.Controls.Add(Me.Label26)
        Me.GroupBoxCliente.Controls.Add(Me.Button3)
        Me.GroupBoxCliente.Controls.Add(Me.LookCuentaBancaria)
        Me.GroupBoxCliente.Controls.Add(Me.Label23)
        Me.GroupBoxCliente.Controls.Add(Me.Label24)
        Me.GroupBoxCliente.Controls.Add(Me.txtGastosDeEnvio)
        Me.GroupBoxCliente.Controls.Add(Me.ComboBox1)
        Me.GroupBoxCliente.Controls.Add(Me.txtObservaciones3)
        Me.GroupBoxCliente.Controls.Add(Me.Label21)
        Me.GroupBoxCliente.Controls.Add(Me.txtObservaciones2)
        Me.GroupBoxCliente.Controls.Add(Me.Label20)
        Me.GroupBoxCliente.Controls.Add(Me.ComboEnviarDireccion)
        Me.GroupBoxCliente.Controls.Add(Me.ComboTemporada_Pedido)
        Me.GroupBoxCliente.Controls.Add(Me.txtDeposito)
        Me.GroupBoxCliente.Controls.Add(Me.Label15)
        Me.GroupBoxCliente.Controls.Add(Me.jsdtFechaCancelacion)
        Me.GroupBoxCliente.Controls.Add(Me.Label14)
        Me.GroupBoxCliente.Controls.Add(Me.jsdtFCancelacionCliente)
        Me.GroupBoxCliente.Controls.Add(Me.Label11)
        Me.GroupBoxCliente.Controls.Add(Me.Label10)
        Me.GroupBoxCliente.Controls.Add(Me.txtObsGenerales)
        Me.GroupBoxCliente.Controls.Add(Me.Label8)
        Me.GroupBoxCliente.Controls.Add(Me.Label7)
        Me.GroupBoxCliente.Controls.Add(Me.Label5)
        Me.GroupBoxCliente.Controls.Add(Me.GridEX2)
        Me.GroupBoxCliente.Controls.Add(Me.Label1)
        Me.GroupBoxCliente.Controls.Add(Me.btnModificarPrecios)
        Me.GroupBoxCliente.Controls.Add(Me.txtReferencia)
        Me.GroupBoxCliente.Controls.Add(Me.Label4)
        Me.GroupBoxCliente.Controls.Add(Me.LabDiasCredito)
        Me.GroupBoxCliente.Controls.Add(Me.LabTransporte)
        Me.GroupBoxCliente.Controls.Add(Me.txtNumero)
        Me.GroupBoxCliente.Controls.Add(Me.Label3)
        Me.GroupBoxCliente.Controls.Add(Me.jsdtFechaRecepcion)
        Me.GroupBoxCliente.Controls.Add(Me.LabFechaRecepcion)
        Me.GroupBoxCliente.Controls.Add(Me.jsdtFecha)
        Me.GroupBoxCliente.Controls.Add(Me.LabFecha)
        Me.GroupBoxCliente.Controls.Add(Me.ComboVendedor)
        Me.GroupBoxCliente.Controls.Add(Me.LabelVendedor)
        Me.GroupBoxCliente.Controls.Add(Me.LabelVenderA)
        Me.GroupBoxCliente.Controls.Add(Me.ComboTransporte)
        Me.GroupBoxCliente.Location = New System.Drawing.Point(0, 0)
        Me.GroupBoxCliente.Name = "GroupBoxCliente"
        Me.GroupBoxCliente.Size = New System.Drawing.Size(1152, 173)
        Me.GroupBoxCliente.TabIndex = 117
        Me.GroupBoxCliente.TabStop = False
        Me.GroupBoxCliente.Visible = False
        '
        'LookPaqueteria
        '
        Me.LookPaqueteria.Location = New System.Drawing.Point(90, 142)
        Me.LookPaqueteria.Name = "LookPaqueteria"
        Me.LookPaqueteria.Properties.Buttons.AddRange(New DevExpress.XtraEditors.Controls.EditorButton() {New DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)})
        Me.LookPaqueteria.Properties.Columns.AddRange(New DevExpress.XtraEditors.Controls.LookUpColumnInfo() {New DevExpress.XtraEditors.Controls.LookUpColumnInfo("PaqueteriaID", "Paqueteria ID", 89, DevExpress.Utils.FormatType.None, "", False, DevExpress.Utils.HorzAlignment.Near, DevExpress.Data.ColumnSortOrder.None, DevExpress.Utils.DefaultBoolean.[Default]), New DevExpress.XtraEditors.Controls.LookUpColumnInfo("Nombre", "Nombre", 200, DevExpress.Utils.FormatType.None, "", True, DevExpress.Utils.HorzAlignment.Near, DevExpress.Data.ColumnSortOrder.None, DevExpress.Utils.DefaultBoolean.[Default])})
        Me.LookPaqueteria.Properties.DataSource = Me.PDDPaqueteriaBindingSource
        Me.LookPaqueteria.Properties.DisplayMember = "Nombre"
        Me.LookPaqueteria.Properties.NullText = ""
        Me.LookPaqueteria.Properties.ValueMember = "PaqueteriaID"
        Me.LookPaqueteria.Size = New System.Drawing.Size(110, 20)
        Me.LookPaqueteria.TabIndex = 9
        '
        'PDDPaqueteriaBindingSource
        '
        Me.PDDPaqueteriaBindingSource.DataMember = "PDD_Paqueteria"
        Me.PDDPaqueteriaBindingSource.DataSource = Me.Ds_PDDPaqueteria1
        '
        'Ds_PDDPaqueteria1
        '
        Me.Ds_PDDPaqueteria1.DataSetName = "Ds_PDDPaqueteria"
        Me.Ds_PDDPaqueteria1.SchemaSerializationMode = System.Data.SchemaSerializationMode.IncludeSchema
        '
        'Label22
        '
        Me.Label22.Font = New System.Drawing.Font("Tahoma", 8.25!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label22.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft
        Me.Label22.Location = New System.Drawing.Point(8, 142)
        Me.Label22.Name = "Label22"
        Me.Label22.Size = New System.Drawing.Size(112, 20)
        Me.Label22.TabIndex = 722
        Me.Label22.Text = "Paqueterîa:"
        Me.Label22.TextAlign = System.Drawing.ContentAlignment.MiddleLeft
        '
        'LookAlmacen
        '
        Me.LookAlmacen.Location = New System.Drawing.Point(432, 16)
        Me.LookAlmacen.Name = "LookAlmacen"
        Me.LookAlmacen.Properties.Buttons.AddRange(New DevExpress.XtraEditors.Controls.EditorButton() {New DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)})
        Me.LookAlmacen.Properties.Columns.AddRange(New DevExpress.XtraEditors.Controls.LookUpColumnInfo() {New DevExpress.XtraEditors.Controls.LookUpColumnInfo("AlmacenID", "Almacen ID", 77, DevExpress.Utils.FormatType.None, "", False, DevExpress.Utils.HorzAlignment.Near, DevExpress.Data.ColumnSortOrder.None, DevExpress.Utils.DefaultBoolean.[Default]), New DevExpress.XtraEditors.Controls.LookUpColumnInfo("Nombre", "Nombre", 250, DevExpress.Utils.FormatType.None, "", True, DevExpress.Utils.HorzAlignment.Near, DevExpress.Data.ColumnSortOrder.None, DevExpress.Utils.DefaultBoolean.[Default])})
        Me.LookAlmacen.Properties.DataSource = Me.PDDAlmacenesPorUsuarioBindingSource1
        Me.LookAlmacen.Properties.DisplayMember = "Nombre"
        Me.LookAlmacen.Properties.NullText = ""
        Me.LookAlmacen.Properties.ValueMember = "AlmacenID"
        Me.LookAlmacen.Size = New System.Drawing.Size(242, 20)
        Me.LookAlmacen.TabIndex = 721
        '
        'PDDAlmacenesPorUsuarioBindingSource1
        '
        Me.PDDAlmacenesPorUsuarioBindingSource1.DataMember = "PDD_Almacenes_PorUsuario"
        Me.PDDAlmacenesPorUsuarioBindingSource1.DataSource = Me.DsPDD_AlmacenesPorUsuario1
        '
        'DsPDD_AlmacenesPorUsuario1
        '
        Me.DsPDD_AlmacenesPorUsuario1.DataSetName = "dsPDD_AlmacenesPorUsuario"
        Me.DsPDD_AlmacenesPorUsuario1.SchemaSerializationMode = System.Data.SchemaSerializationMode.IncludeSchema
        '
        'Label19
        '
        Me.Label19.Font = New System.Drawing.Font("Tahoma", 8.25!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label19.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft
        Me.Label19.Location = New System.Drawing.Point(368, 13)
        Me.Label19.Name = "Label19"
        Me.Label19.Size = New System.Drawing.Size(72, 20)
        Me.Label19.TabIndex = 720
        Me.Label19.Text = "Almacén:"
        Me.Label19.TextAlign = System.Drawing.ContentAlignment.MiddleLeft
        '
        'txtOcurreA
        '
        Me.txtOcurreA.Location = New System.Drawing.Point(873, 138)
        Me.txtOcurreA.Name = "txtOcurreA"
        Me.txtOcurreA.Size = New System.Drawing.Size(278, 20)
        Me.txtOcurreA.TabIndex = 719
        '
        'LabelOcurreA
        '
        Me.LabelOcurreA.Font = New System.Drawing.Font("Tahoma", 8.25!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.LabelOcurreA.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft
        Me.LabelOcurreA.Location = New System.Drawing.Point(870, 112)
        Me.LabelOcurreA.Name = "LabelOcurreA"
        Me.LabelOcurreA.Size = New System.Drawing.Size(131, 20)
        Me.LabelOcurreA.TabIndex = 718
        Me.LabelOcurreA.Text = "Ocurre a la sucursal:"
        Me.LabelOcurreA.TextAlign = System.Drawing.ContentAlignment.MiddleLeft
        '
        'LookEnvioA
        '
        Me.LookEnvioA.Location = New System.Drawing.Point(873, 86)
        Me.LookEnvioA.Name = "LookEnvioA"
        Me.LookEnvioA.Properties.Buttons.AddRange(New DevExpress.XtraEditors.Controls.EditorButton() {New DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)})
        Me.LookEnvioA.Properties.Columns.AddRange(New DevExpress.XtraEditors.Controls.LookUpColumnInfo() {New DevExpress.XtraEditors.Controls.LookUpColumnInfo("EnvioAID", "Envio AID", 70, DevExpress.Utils.FormatType.None, "", False, DevExpress.Utils.HorzAlignment.Near, DevExpress.Data.ColumnSortOrder.None, DevExpress.Utils.DefaultBoolean.[Default]), New DevExpress.XtraEditors.Controls.LookUpColumnInfo("Clave", "Clave", 37, DevExpress.Utils.FormatType.Numeric, "", False, DevExpress.Utils.HorzAlignment.Far, DevExpress.Data.ColumnSortOrder.None, DevExpress.Utils.DefaultBoolean.[Default]), New DevExpress.XtraEditors.Controls.LookUpColumnInfo("Nombre", "Nombre", 200, DevExpress.Utils.FormatType.None, "", True, DevExpress.Utils.HorzAlignment.Near, DevExpress.Data.ColumnSortOrder.None, DevExpress.Utils.DefaultBoolean.[Default])})
        Me.LookEnvioA.Properties.DataSource = Me.PDDEnvioABindingSource1
        Me.LookEnvioA.Properties.DisplayMember = "Nombre"
        Me.LookEnvioA.Properties.NullText = ""
        Me.LookEnvioA.Properties.ValueMember = "EnvioAID"
        Me.LookEnvioA.Size = New System.Drawing.Size(278, 20)
        Me.LookEnvioA.TabIndex = 717
        '
        'PDDEnvioABindingSource1
        '
        Me.PDDEnvioABindingSource1.DataMember = "PDD_EnvioA"
        Me.PDDEnvioABindingSource1.DataSource = Me.DsPDD_EnvioA1
        '
        'DsPDD_EnvioA1
        '
        Me.DsPDD_EnvioA1.DataSetName = "dsPDD_EnvioA"
        Me.DsPDD_EnvioA1.SchemaSerializationMode = System.Data.SchemaSerializationMode.IncludeSchema
        '
        'Label18
        '
        Me.Label18.Font = New System.Drawing.Font("Tahoma", 8.25!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label18.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft
        Me.Label18.Location = New System.Drawing.Point(869, 63)
        Me.Label18.Name = "Label18"
        Me.Label18.Size = New System.Drawing.Size(131, 20)
        Me.Label18.TabIndex = 716
        Me.Label18.Text = "Datos de envío:"
        Me.Label18.TextAlign = System.Drawing.ContentAlignment.MiddleLeft
        '
        'LookCliente
        '
        Me.LookCliente.Location = New System.Drawing.Point(93, 42)
        Me.LookCliente.Name = "LookCliente"
        Me.LookCliente.Properties.Buttons.AddRange(New DevExpress.XtraEditors.Controls.EditorButton() {New DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)})
        Me.LookCliente.Properties.Columns.AddRange(New DevExpress.XtraEditors.Controls.LookUpColumnInfo() {New DevExpress.XtraEditors.Controls.LookUpColumnInfo("ClienteID", "Cliente ID", 70, DevExpress.Utils.FormatType.None, "", False, DevExpress.Utils.HorzAlignment.Near, DevExpress.Data.ColumnSortOrder.None, DevExpress.Utils.DefaultBoolean.[Default]), New DevExpress.XtraEditors.Controls.LookUpColumnInfo("Nombre", "Nombre", 270, DevExpress.Utils.FormatType.None, "", True, DevExpress.Utils.HorzAlignment.Near, DevExpress.Data.ColumnSortOrder.None, DevExpress.Utils.DefaultBoolean.[Default]), New DevExpress.XtraEditors.Controls.LookUpColumnInfo("PrecioID", "Precio ID", 53, DevExpress.Utils.FormatType.None, "", False, DevExpress.Utils.HorzAlignment.Near, DevExpress.Data.ColumnSortOrder.None, DevExpress.Utils.DefaultBoolean.[Default]), New DevExpress.XtraEditors.Controls.LookUpColumnInfo("Precio", "Precio", 70, DevExpress.Utils.FormatType.None, "", True, DevExpress.Utils.HorzAlignment.Near, DevExpress.Data.ColumnSortOrder.None, DevExpress.Utils.DefaultBoolean.[Default]), New DevExpress.XtraEditors.Controls.LookUpColumnInfo("Saldo", "Saldo", 70, DevExpress.Utils.FormatType.Numeric, "", True, DevExpress.Utils.HorzAlignment.Far, DevExpress.Data.ColumnSortOrder.None, DevExpress.Utils.DefaultBoolean.[Default]), New DevExpress.XtraEditors.Controls.LookUpColumnInfo("DiasCredito", "Dias Credito", 70, DevExpress.Utils.FormatType.Numeric, "", True, DevExpress.Utils.HorzAlignment.Far, DevExpress.Data.ColumnSortOrder.None, DevExpress.Utils.DefaultBoolean.[Default]), New DevExpress.XtraEditors.Controls.LookUpColumnInfo("LimiteCredito", "Limite Credito", 70, DevExpress.Utils.FormatType.Numeric, "", True, DevExpress.Utils.HorzAlignment.Far, DevExpress.Data.ColumnSortOrder.None, DevExpress.Utils.DefaultBoolean.[Default])})
        Me.LookCliente.Properties.DataSource = Me.PClienteListBindingSource1
        Me.LookCliente.Properties.DisplayMember = "Nombre"
        Me.LookCliente.Properties.NullText = ""
        Me.LookCliente.Properties.ValueMember = "ClienteID"
        Me.LookCliente.Size = New System.Drawing.Size(272, 20)
        Me.LookCliente.TabIndex = 715
        '
        'PClienteListBindingSource1
        '
        Me.PClienteListBindingSource1.DataMember = "PClienteList"
        Me.PClienteListBindingSource1.DataSource = Me.DsPedidoZ21
        '
        'DsPedidoZ21
        '
        Me.DsPedidoZ21.DataSetName = "dsPedidoZ2"
        Me.DsPedidoZ21.SchemaSerializationMode = System.Data.SchemaSerializationMode.IncludeSchema
        '
        'LabelDiasCredito
        '
        Me.LabelDiasCredito.Font = New System.Drawing.Font("Tahoma", 8.25!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.LabelDiasCredito.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft
        Me.LabelDiasCredito.Location = New System.Drawing.Point(316, 65)
        Me.LabelDiasCredito.Name = "LabelDiasCredito"
        Me.LabelDiasCredito.Size = New System.Drawing.Size(46, 20)
        Me.LabelDiasCredito.TabIndex = 714
        Me.LabelDiasCredito.TextAlign = System.Drawing.ContentAlignment.MiddleLeft
        '
        'LabelCategoria
        '
        Me.LabelCategoria.Font = New System.Drawing.Font("Tahoma", 8.25!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.LabelCategoria.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft
        Me.LabelCategoria.Location = New System.Drawing.Point(371, 39)
        Me.LabelCategoria.Name = "LabelCategoria"
        Me.LabelCategoria.Size = New System.Drawing.Size(226, 20)
        Me.LabelCategoria.TabIndex = 712
        Me.LabelCategoria.Text = "Clasificación:"
        Me.LabelCategoria.TextAlign = System.Drawing.ContentAlignment.MiddleLeft
        '
        'ComboManiobras
        '
        Me.ComboManiobras.Location = New System.Drawing.Point(528, 150)
        Me.ComboManiobras.Name = "ComboManiobras"
        Me.ComboManiobras.Properties.Buttons.AddRange(New DevExpress.XtraEditors.Controls.EditorButton() {New DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)})
        Me.ComboManiobras.Properties.Items.AddRange(New Object() {"CON COSTO", "SIN COSTO"})
        Me.ComboManiobras.Size = New System.Drawing.Size(97, 20)
        Me.ComboManiobras.TabIndex = 711
        Me.ComboManiobras.Visible = False
        '
        'Label28
        '
        Me.Label28.Font = New System.Drawing.Font("Tahoma", 8.25!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label28.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft
        Me.Label28.Location = New System.Drawing.Point(471, 150)
        Me.Label28.Name = "Label28"
        Me.Label28.Size = New System.Drawing.Size(80, 20)
        Me.Label28.TabIndex = 710
        Me.Label28.Text = "Maniobras:"
        Me.Label28.TextAlign = System.Drawing.ContentAlignment.MiddleLeft
        Me.Label28.Visible = False
        '
        'LabelCondiciones
        '
        Me.LabelCondiciones.Font = New System.Drawing.Font("Tahoma", 8.25!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.LabelCondiciones.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft
        Me.LabelCondiciones.Location = New System.Drawing.Point(91, 63)
        Me.LabelCondiciones.Name = "LabelCondiciones"
        Me.LabelCondiciones.Size = New System.Drawing.Size(109, 20)
        Me.LabelCondiciones.TabIndex = 709
        Me.LabelCondiciones.TextAlign = System.Drawing.ContentAlignment.MiddleLeft
        '
        'Label27
        '
        Me.Label27.Font = New System.Drawing.Font("Tahoma", 8.25!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label27.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft
        Me.Label27.Location = New System.Drawing.Point(236, 65)
        Me.Label27.Name = "Label27"
        Me.Label27.Size = New System.Drawing.Size(80, 20)
        Me.Label27.TabIndex = 708
        Me.Label27.Text = "Días Crédito:"
        Me.Label27.TextAlign = System.Drawing.ContentAlignment.MiddleLeft
        '
        'txtUsoDelCFDI
        '
        Me.txtUsoDelCFDI.Location = New System.Drawing.Point(690, 139)
        Me.txtUsoDelCFDI.Name = "txtUsoDelCFDI"
        Me.txtUsoDelCFDI.Size = New System.Drawing.Size(164, 20)
        Me.txtUsoDelCFDI.TabIndex = 17
        '
        'Label25
        '
        Me.Label25.Font = New System.Drawing.Font("Tahoma", 8.25!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label25.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft
        Me.Label25.Location = New System.Drawing.Point(602, 139)
        Me.Label25.Name = "Label25"
        Me.Label25.Size = New System.Drawing.Size(123, 20)
        Me.Label25.TabIndex = 707
        Me.Label25.Text = "Uso del cfdi:"
        Me.Label25.TextAlign = System.Drawing.ContentAlignment.MiddleLeft
        '
        'cmdEmpacado
        '
        Me.cmdEmpacado.Location = New System.Drawing.Point(268, 142)
        Me.cmdEmpacado.Name = "cmdEmpacado"
        Me.cmdEmpacado.Properties.Buttons.AddRange(New DevExpress.XtraEditors.Controls.EditorButton() {New DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)})
        Me.cmdEmpacado.Properties.Items.AddRange(New Object() {"Empacado", "Flejado"})
        Me.cmdEmpacado.Size = New System.Drawing.Size(94, 20)
        Me.cmdEmpacado.TabIndex = 15
        '
        'Label26
        '
        Me.Label26.Font = New System.Drawing.Font("Tahoma", 8.25!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label26.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft
        Me.Label26.Location = New System.Drawing.Point(202, 141)
        Me.Label26.Name = "Label26"
        Me.Label26.Size = New System.Drawing.Size(74, 20)
        Me.Label26.TabIndex = 702
        Me.Label26.Tag = ""
        Me.Label26.Text = "Empaque:"
        Me.Label26.TextAlign = System.Drawing.ContentAlignment.MiddleLeft
        '
        'Button3
        '
        Me.Button3.Location = New System.Drawing.Point(872, 16)
        Me.Button3.Name = "Button3"
        Me.Button3.Size = New System.Drawing.Size(116, 25)
        Me.Button3.TabIndex = 526
        Me.Button3.Text = "Ver foto"
        Me.Button3.UseVisualStyleBackColor = True
        '
        'LookCuentaBancaria
        '
        Me.LookCuentaBancaria.Location = New System.Drawing.Point(709, 88)
        Me.LookCuentaBancaria.Name = "LookCuentaBancaria"
        Me.LookCuentaBancaria.Properties.Buttons.AddRange(New DevExpress.XtraEditors.Controls.EditorButton() {New DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)})
        Me.LookCuentaBancaria.Properties.Columns.AddRange(New DevExpress.XtraEditors.Controls.LookUpColumnInfo() {New DevExpress.XtraEditors.Controls.LookUpColumnInfo("CuentaBancariaID", "Cuenta Bancaria ID", 116, DevExpress.Utils.FormatType.None, "", False, DevExpress.Utils.HorzAlignment.Near, DevExpress.Data.ColumnSortOrder.None, DevExpress.Utils.DefaultBoolean.[Default]), New DevExpress.XtraEditors.Controls.LookUpColumnInfo("GrupoID", "Grupo ID", 53, DevExpress.Utils.FormatType.None, "", False, DevExpress.Utils.HorzAlignment.Near, DevExpress.Data.ColumnSortOrder.None, DevExpress.Utils.DefaultBoolean.[Default]), New DevExpress.XtraEditors.Controls.LookUpColumnInfo("BancoID", "Banco ID", 53, DevExpress.Utils.FormatType.None, "", False, DevExpress.Utils.HorzAlignment.Near, DevExpress.Data.ColumnSortOrder.None, DevExpress.Utils.DefaultBoolean.[Default]), New DevExpress.XtraEditors.Controls.LookUpColumnInfo("CuentaContableID", "Cuenta Contable ID", 105, DevExpress.Utils.FormatType.None, "", False, DevExpress.Utils.HorzAlignment.Near, DevExpress.Data.ColumnSortOrder.None, DevExpress.Utils.DefaultBoolean.[Default]), New DevExpress.XtraEditors.Controls.LookUpColumnInfo("NúmeroCuenta", "Número Cuenta", 85, DevExpress.Utils.FormatType.None, "", True, DevExpress.Utils.HorzAlignment.Near, DevExpress.Data.ColumnSortOrder.None, DevExpress.Utils.DefaultBoolean.[Default]), New DevExpress.XtraEditors.Controls.LookUpColumnInfo("Sucursal", "Sucursal", 50, DevExpress.Utils.FormatType.Numeric, "", False, DevExpress.Utils.HorzAlignment.Far, DevExpress.Data.ColumnSortOrder.None, DevExpress.Utils.DefaultBoolean.[Default]), New DevExpress.XtraEditors.Controls.LookUpColumnInfo("SiguienteCheque", "Siguiente Cheque", 94, DevExpress.Utils.FormatType.None, "", False, DevExpress.Utils.HorzAlignment.Near, DevExpress.Data.ColumnSortOrder.None, DevExpress.Utils.DefaultBoolean.[Default]), New DevExpress.XtraEditors.Controls.LookUpColumnInfo("MonedaID", "Moneda ID", 62, DevExpress.Utils.FormatType.None, "", False, DevExpress.Utils.HorzAlignment.Near, DevExpress.Data.ColumnSortOrder.None, DevExpress.Utils.DefaultBoolean.[Default]), New DevExpress.XtraEditors.Controls.LookUpColumnInfo("SaldoInicial", "Saldo Inicial", 66, DevExpress.Utils.FormatType.Numeric, "", False, DevExpress.Utils.HorzAlignment.Far, DevExpress.Data.ColumnSortOrder.None, DevExpress.Utils.DefaultBoolean.[Default]), New DevExpress.XtraEditors.Controls.LookUpColumnInfo("SaldoConciliado", "Saldo Conciliado", 87, DevExpress.Utils.FormatType.Numeric, "", False, DevExpress.Utils.HorzAlignment.Far, DevExpress.Data.ColumnSortOrder.None, DevExpress.Utils.DefaultBoolean.[Default]), New DevExpress.XtraEditors.Controls.LookUpColumnInfo("Status", "Status", 41, DevExpress.Utils.FormatType.None, "", False, DevExpress.Utils.HorzAlignment.Near, DevExpress.Data.ColumnSortOrder.None, DevExpress.Utils.DefaultBoolean.[Default]), New DevExpress.XtraEditors.Controls.LookUpColumnInfo("Tipo", "Tipo", 30, DevExpress.Utils.FormatType.None, "", False, DevExpress.Utils.HorzAlignment.Near, DevExpress.Data.ColumnSortOrder.None, DevExpress.Utils.DefaultBoolean.[Default]), New DevExpress.XtraEditors.Controls.LookUpColumnInfo("FechaDeApertura", "Fecha De Apertura", 101, DevExpress.Utils.FormatType.DateTime, "dd/MM/yyyy", False, DevExpress.Utils.HorzAlignment.Near, DevExpress.Data.ColumnSortOrder.None, DevExpress.Utils.DefaultBoolean.[Default]), New DevExpress.XtraEditors.Controls.LookUpColumnInfo("DiaDeCorte", "Dia De Corte", 71, DevExpress.Utils.FormatType.Numeric, "", False, DevExpress.Utils.HorzAlignment.Far, DevExpress.Data.ColumnSortOrder.None, DevExpress.Utils.DefaultBoolean.[Default]), New DevExpress.XtraEditors.Controls.LookUpColumnInfo("Formato", "Formato", 50, DevExpress.Utils.FormatType.None, "", False, DevExpress.Utils.HorzAlignment.Near, DevExpress.Data.ColumnSortOrder.None, DevExpress.Utils.DefaultBoolean.[Default]), New DevExpress.XtraEditors.Controls.LookUpColumnInfo("LimiteDeCredito", "Limite De Credito", 91, DevExpress.Utils.FormatType.Numeric, "", False, DevExpress.Utils.HorzAlignment.Far, DevExpress.Data.ColumnSortOrder.None, DevExpress.Utils.DefaultBoolean.[Default]), New DevExpress.XtraEditors.Controls.LookUpColumnInfo("PagoMinimo", "Pago Minimo", 69, DevExpress.Utils.FormatType.Numeric, "", False, DevExpress.Utils.HorzAlignment.Far, DevExpress.Data.ColumnSortOrder.None, DevExpress.Utils.DefaultBoolean.[Default]), New DevExpress.XtraEditors.Controls.LookUpColumnInfo("Ejecutivo", "Ejecutivo", 54, DevExpress.Utils.FormatType.None, "", False, DevExpress.Utils.HorzAlignment.Near, DevExpress.Data.ColumnSortOrder.None, DevExpress.Utils.DefaultBoolean.[Default]), New DevExpress.XtraEditors.Controls.LookUpColumnInfo("Telefono", "Telefono", 52, DevExpress.Utils.FormatType.None, "", False, DevExpress.Utils.HorzAlignment.Near, DevExpress.Data.ColumnSortOrder.None, DevExpress.Utils.DefaultBoolean.[Default]), New DevExpress.XtraEditors.Controls.LookUpColumnInfo("SaldoReal", "Saldo Real", 60, DevExpress.Utils.FormatType.Numeric, "", False, DevExpress.Utils.HorzAlignment.Far, DevExpress.Data.ColumnSortOrder.None, DevExpress.Utils.DefaultBoolean.[Default]), New DevExpress.XtraEditors.Controls.LookUpColumnInfo("DepSBC", "Dep SBC", 51, DevExpress.Utils.FormatType.Numeric, "", False, DevExpress.Utils.HorzAlignment.Far, DevExpress.Data.ColumnSortOrder.None, DevExpress.Utils.DefaultBoolean.[Default]), New DevExpress.XtraEditors.Controls.LookUpColumnInfo("Disponible", "Disponible", 58, DevExpress.Utils.FormatType.Numeric, "", False, DevExpress.Utils.HorzAlignment.Far, DevExpress.Data.ColumnSortOrder.None, DevExpress.Utils.DefaultBoolean.[Default]), New DevExpress.XtraEditors.Controls.LookUpColumnInfo("Posfechados", "Posfechados", 71, DevExpress.Utils.FormatType.Numeric, "", False, DevExpress.Utils.HorzAlignment.Far, DevExpress.Data.ColumnSortOrder.None, DevExpress.Utils.DefaultBoolean.[Default]), New DevExpress.XtraEditors.Controls.LookUpColumnInfo("EnTransito", "En Transito", 64, DevExpress.Utils.FormatType.Numeric, "", False, DevExpress.Utils.HorzAlignment.Far, DevExpress.Data.ColumnSortOrder.None, DevExpress.Utils.DefaultBoolean.[Default]), New DevExpress.XtraEditors.Controls.LookUpColumnInfo("XCubrirMartes", "XCubrir Martes", 81, DevExpress.Utils.FormatType.Numeric, "", False, DevExpress.Utils.HorzAlignment.Far, DevExpress.Data.ColumnSortOrder.None, DevExpress.Utils.DefaultBoolean.[Default]), New DevExpress.XtraEditors.Controls.LookUpColumnInfo("XCubrirMiercoles", "XCubrir Miercoles", 92, DevExpress.Utils.FormatType.Numeric, "", False, DevExpress.Utils.HorzAlignment.Far, DevExpress.Data.ColumnSortOrder.None, DevExpress.Utils.DefaultBoolean.[Default]), New DevExpress.XtraEditors.Controls.LookUpColumnInfo("XCubrirJueves", "XCubrir Jueves", 82, DevExpress.Utils.FormatType.Numeric, "", False, DevExpress.Utils.HorzAlignment.Far, DevExpress.Data.ColumnSortOrder.None, DevExpress.Utils.DefaultBoolean.[Default]), New DevExpress.XtraEditors.Controls.LookUpColumnInfo("XCubrirViernes", "XCubrir Viernes", 83, DevExpress.Utils.FormatType.Numeric, "", False, DevExpress.Utils.HorzAlignment.Far, DevExpress.Data.ColumnSortOrder.None, DevExpress.Utils.DefaultBoolean.[Default]), New DevExpress.XtraEditors.Controls.LookUpColumnInfo("EmpresaID", "Empresa ID", 65, DevExpress.Utils.FormatType.None, "", False, DevExpress.Utils.HorzAlignment.Near, DevExpress.Data.ColumnSortOrder.None, DevExpress.Utils.DefaultBoolean.[Default]), New DevExpress.XtraEditors.Controls.LookUpColumnInfo("CuentaID", "Cuenta ID", 59, DevExpress.Utils.FormatType.None, "", False, DevExpress.Utils.HorzAlignment.Near, DevExpress.Data.ColumnSortOrder.None, DevExpress.Utils.DefaultBoolean.[Default]), New DevExpress.XtraEditors.Controls.LookUpColumnInfo("SaldoInicialReal", "Saldo Inicial Real", 90, DevExpress.Utils.FormatType.Numeric, "", False, DevExpress.Utils.HorzAlignment.Far, DevExpress.Data.ColumnSortOrder.None, DevExpress.Utils.DefaultBoolean.[Default]), New DevExpress.XtraEditors.Controls.LookUpColumnInfo("SaldoFinalReal", "Saldo Final Real", 85, DevExpress.Utils.FormatType.Numeric, "", False, DevExpress.Utils.HorzAlignment.Far, DevExpress.Data.ColumnSortOrder.None, DevExpress.Utils.DefaultBoolean.[Default]), New DevExpress.XtraEditors.Controls.LookUpColumnInfo("SaldoCreditoRevolvente", "Saldo Credito Revolvente", 132, DevExpress.Utils.FormatType.Numeric, "", False, DevExpress.Utils.HorzAlignment.Far, DevExpress.Data.ColumnSortOrder.None, DevExpress.Utils.DefaultBoolean.[Default]), New DevExpress.XtraEditors.Controls.LookUpColumnInfo("CreditoPorUtilizar", "Credito Por Utilizar", 99, DevExpress.Utils.FormatType.Numeric, "", False, DevExpress.Utils.HorzAlignment.Far, DevExpress.Data.ColumnSortOrder.None, DevExpress.Utils.DefaultBoolean.[Default]), New DevExpress.XtraEditors.Controls.LookUpColumnInfo("SaldoEnCheques", "Saldo En Cheques", 96, DevExpress.Utils.FormatType.Numeric, "", False, DevExpress.Utils.HorzAlignment.Far, DevExpress.Data.ColumnSortOrder.None, DevExpress.Utils.DefaultBoolean.[Default]), New DevExpress.XtraEditors.Controls.LookUpColumnInfo("CantidadDeCheques", "Cantidad De Cheques", 114, DevExpress.Utils.FormatType.Numeric, "", False, DevExpress.Utils.HorzAlignment.Far, DevExpress.Data.ColumnSortOrder.None, DevExpress.Utils.DefaultBoolean.[Default]), New DevExpress.XtraEditors.Controls.LookUpColumnInfo("SaldoVencido", "Saldo Vencido", 76, DevExpress.Utils.FormatType.Numeric, "", False, DevExpress.Utils.HorzAlignment.Far, DevExpress.Data.ColumnSortOrder.None, DevExpress.Utils.DefaultBoolean.[Default]), New DevExpress.XtraEditors.Controls.LookUpColumnInfo("SaldoPorVencer", "Saldo Por Vencer", 91, DevExpress.Utils.FormatType.Numeric, "", False, DevExpress.Utils.HorzAlignment.Far, DevExpress.Data.ColumnSortOrder.None, DevExpress.Utils.DefaultBoolean.[Default]), New DevExpress.XtraEditors.Controls.LookUpColumnInfo("LM", "LM", 23, DevExpress.Utils.FormatType.None, "", False, DevExpress.Utils.HorzAlignment.Near, DevExpress.Data.ColumnSortOrder.None, DevExpress.Utils.DefaultBoolean.[Default]), New DevExpress.XtraEditors.Controls.LookUpColumnInfo("Codigo", "Codigo", 43, DevExpress.Utils.FormatType.None, "", False, DevExpress.Utils.HorzAlignment.Near, DevExpress.Data.ColumnSortOrder.None, DevExpress.Utils.DefaultBoolean.[Default]), New DevExpress.XtraEditors.Controls.LookUpColumnInfo("Numero", "Numero", 47, DevExpress.Utils.FormatType.Numeric, "", False, DevExpress.Utils.HorzAlignment.Far, DevExpress.Data.ColumnSortOrder.None, DevExpress.Utils.DefaultBoolean.[Default]), New DevExpress.XtraEditors.Controls.LookUpColumnInfo("CuentaID2", "Cuenta ID2", 65, DevExpress.Utils.FormatType.None, "", False, DevExpress.Utils.HorzAlignment.Near, DevExpress.Data.ColumnSortOrder.None, DevExpress.Utils.DefaultBoolean.[Default]), New DevExpress.XtraEditors.Controls.LookUpColumnInfo("Nomina", "Nomina", 45, DevExpress.Utils.FormatType.None, "", False, DevExpress.Utils.HorzAlignment.Near, DevExpress.Data.ColumnSortOrder.None, DevExpress.Utils.DefaultBoolean.[Default]), New DevExpress.XtraEditors.Controls.LookUpColumnInfo("CuentaBancariaIDCaja", "Cuenta Bancaria ID Caja", 128, DevExpress.Utils.FormatType.None, "", False, DevExpress.Utils.HorzAlignment.Near, DevExpress.Data.ColumnSortOrder.None, DevExpress.Utils.DefaultBoolean.[Default]), New DevExpress.XtraEditors.Controls.LookUpColumnInfo("MontoCh", "Monto Ch", 56, DevExpress.Utils.FormatType.Numeric, "", False, DevExpress.Utils.HorzAlignment.Far, DevExpress.Data.ColumnSortOrder.None, DevExpress.Utils.DefaultBoolean.[Default]), New DevExpress.XtraEditors.Controls.LookUpColumnInfo("ExtraccionCH", "Extraccion CH", 77, DevExpress.Utils.FormatType.Numeric, "", False, DevExpress.Utils.HorzAlignment.Far, DevExpress.Data.ColumnSortOrder.None, DevExpress.Utils.DefaultBoolean.[Default]), New DevExpress.XtraEditors.Controls.LookUpColumnInfo("SaldoCH", "Saldo CH", 53, DevExpress.Utils.FormatType.Numeric, "", False, DevExpress.Utils.HorzAlignment.Far, DevExpress.Data.ColumnSortOrder.None, DevExpress.Utils.DefaultBoolean.[Default]), New DevExpress.XtraEditors.Controls.LookUpColumnInfo("NominaF", "Nomina F", 54, DevExpress.Utils.FormatType.None, "", False, DevExpress.Utils.HorzAlignment.Near, DevExpress.Data.ColumnSortOrder.None, DevExpress.Utils.DefaultBoolean.[Default]), New DevExpress.XtraEditors.Controls.LookUpColumnInfo("Orden", "Orden", 40, DevExpress.Utils.FormatType.Numeric, "", False, DevExpress.Utils.HorzAlignment.Far, DevExpress.Data.ColumnSortOrder.None, DevExpress.Utils.DefaultBoolean.[Default]), New DevExpress.XtraEditors.Controls.LookUpColumnInfo("SaldoTeorico", "Saldo Teorico", 74, DevExpress.Utils.FormatType.Numeric, "", False, DevExpress.Utils.HorzAlignment.Far, DevExpress.Data.ColumnSortOrder.None, DevExpress.Utils.DefaultBoolean.[Default]), New DevExpress.XtraEditors.Controls.LookUpColumnInfo("CantidadDeMovimientos", "Cantidad De Movimientos", 131, DevExpress.Utils.FormatType.Numeric, "", False, DevExpress.Utils.HorzAlignment.Far, DevExpress.Data.ColumnSortOrder.None, DevExpress.Utils.DefaultBoolean.[Default]), New DevExpress.XtraEditors.Controls.LookUpColumnInfo("FUltimaImportacion", "FUltima Importacion", 105, DevExpress.Utils.FormatType.DateTime, "dd/MM/yyyy", False, DevExpress.Utils.HorzAlignment.Near, DevExpress.Data.ColumnSortOrder.None, DevExpress.Utils.DefaultBoolean.[Default]), New DevExpress.XtraEditors.Controls.LookUpColumnInfo("FUltimaCaptura", "FUltima Captura", 87, DevExpress.Utils.FormatType.DateTime, "dd/MM/yyyy", False, DevExpress.Utils.HorzAlignment.Near, DevExpress.Data.ColumnSortOrder.None, DevExpress.Utils.DefaultBoolean.[Default]), New DevExpress.XtraEditors.Controls.LookUpColumnInfo("Incidencia", "Incidencia", 58, DevExpress.Utils.FormatType.None, "", False, DevExpress.Utils.HorzAlignment.Near, DevExpress.Data.ColumnSortOrder.None, DevExpress.Utils.DefaultBoolean.[Default]), New DevExpress.XtraEditors.Controls.LookUpColumnInfo("PDD_CuentaBancaria.CuentaBancariaID", "PDD_Cuenta Bancaria.Cuenta Bancaria ID", 212, DevExpress.Utils.FormatType.None, "", False, DevExpress.Utils.HorzAlignment.Near, DevExpress.Data.ColumnSortOrder.None, DevExpress.Utils.DefaultBoolean.[Default]), New DevExpress.XtraEditors.Controls.LookUpColumnInfo("PDD_CuentaBancaria.NúmeroCuenta", "PDD_Cuenta Bancaria.Número Cuenta", 194, DevExpress.Utils.FormatType.None, "", False, DevExpress.Utils.HorzAlignment.Near, DevExpress.Data.ColumnSortOrder.None, DevExpress.Utils.DefaultBoolean.[Default])})
        Me.LookCuentaBancaria.Properties.DataSource = Me.PDDCuentaBancariaBindingSource
        Me.LookCuentaBancaria.Properties.DisplayMember = "NúmeroCuenta"
        Me.LookCuentaBancaria.Properties.NullText = ""
        Me.LookCuentaBancaria.Properties.ValueMember = "CuentaBancariaID"
        Me.LookCuentaBancaria.Size = New System.Drawing.Size(145, 20)
        Me.LookCuentaBancaria.TabIndex = 12
        '
        'PDDCuentaBancariaBindingSource
        '
        Me.PDDCuentaBancariaBindingSource.DataMember = "PDD_CuentaBancaria"
        Me.PDDCuentaBancariaBindingSource.DataSource = Me.DsNuevo1
        '
        'DsNuevo1
        '
        Me.DsNuevo1.DataSetName = "dsNuevo"
        Me.DsNuevo1.SchemaSerializationMode = System.Data.SchemaSerializationMode.IncludeSchema
        '
        'Label23
        '
        Me.Label23.Font = New System.Drawing.Font("Tahoma", 8.25!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label23.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft
        Me.Label23.Location = New System.Drawing.Point(600, 63)
        Me.Label23.Name = "Label23"
        Me.Label23.Size = New System.Drawing.Size(109, 20)
        Me.Label23.TabIndex = 522
        Me.Label23.Text = "Gastos de envío:"
        Me.Label23.TextAlign = System.Drawing.ContentAlignment.MiddleLeft
        '
        'Label24
        '
        Me.Label24.Font = New System.Drawing.Font("Tahoma", 8.25!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label24.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft
        Me.Label24.Location = New System.Drawing.Point(602, 90)
        Me.Label24.Name = "Label24"
        Me.Label24.Size = New System.Drawing.Size(107, 20)
        Me.Label24.TabIndex = 524
        Me.Label24.Text = "Cuenta bancaria:"
        Me.Label24.TextAlign = System.Drawing.ContentAlignment.MiddleLeft
        '
        'txtGastosDeEnvio
        '
        Me.txtGastosDeEnvio.Location = New System.Drawing.Point(709, 64)
        Me.txtGastosDeEnvio.Name = "txtGastosDeEnvio"
        Me.txtGastosDeEnvio.Size = New System.Drawing.Size(145, 20)
        Me.txtGastosDeEnvio.TabIndex = 11
        Me.txtGastosDeEnvio.TextAlign = System.Windows.Forms.HorizontalAlignment.Right
        '
        'PPedidos2SelectBindingSource
        '
        Me.PPedidos2SelectBindingSource.DataMember = "PPedidos2_Select"
        Me.PPedidos2SelectBindingSource.DataSource = Me.DsPedidos2
        '
        'DsPedidos2
        '
        Me.DsPedidos2.DataSetName = "dsPedidos2"
        Me.DsPedidos2.SchemaSerializationMode = System.Data.SchemaSerializationMode.IncludeSchema
        '
        'SqlDataAdapter1
        '
        Me.SqlDataAdapter1.SelectCommand = Me.SqlCommand2
        Me.SqlDataAdapter1.TableMappings.AddRange(New System.Data.Common.DataTableMapping() {New System.Data.Common.DataTableMapping("Table", "PDD_CuentaBancaria", New System.Data.Common.DataColumnMapping() {New System.Data.Common.DataColumnMapping("CuentaBancariaID", "CuentaBancariaID"), New System.Data.Common.DataColumnMapping("GrupoID", "GrupoID"), New System.Data.Common.DataColumnMapping("BancoID", "BancoID"), New System.Data.Common.DataColumnMapping("CuentaContableID", "CuentaContableID"), New System.Data.Common.DataColumnMapping("NúmeroCuenta", "NúmeroCuenta"), New System.Data.Common.DataColumnMapping("Sucursal", "Sucursal"), New System.Data.Common.DataColumnMapping("SiguienteCheque", "SiguienteCheque"), New System.Data.Common.DataColumnMapping("MonedaID", "MonedaID"), New System.Data.Common.DataColumnMapping("SaldoInicial", "SaldoInicial"), New System.Data.Common.DataColumnMapping("SaldoConciliado", "SaldoConciliado"), New System.Data.Common.DataColumnMapping("Status", "Status"), New System.Data.Common.DataColumnMapping("Tipo", "Tipo"), New System.Data.Common.DataColumnMapping("FechaDeApertura", "FechaDeApertura"), New System.Data.Common.DataColumnMapping("DiaDeCorte", "DiaDeCorte"), New System.Data.Common.DataColumnMapping("Formato", "Formato"), New System.Data.Common.DataColumnMapping("LimiteDeCredito", "LimiteDeCredito"), New System.Data.Common.DataColumnMapping("PagoMinimo", "PagoMinimo"), New System.Data.Common.DataColumnMapping("Ejecutivo", "Ejecutivo"), New System.Data.Common.DataColumnMapping("Telefono", "Telefono"), New System.Data.Common.DataColumnMapping("SaldoReal", "SaldoReal"), New System.Data.Common.DataColumnMapping("DepSBC", "DepSBC"), New System.Data.Common.DataColumnMapping("Disponible", "Disponible"), New System.Data.Common.DataColumnMapping("Posfechados", "Posfechados"), New System.Data.Common.DataColumnMapping("EnTransito", "EnTransito"), New System.Data.Common.DataColumnMapping("XCubrirMartes", "XCubrirMartes"), New System.Data.Common.DataColumnMapping("XCubrirMiercoles", "XCubrirMiercoles"), New System.Data.Common.DataColumnMapping("XCubrirJueves", "XCubrirJueves"), New System.Data.Common.DataColumnMapping("XCubrirViernes", "XCubrirViernes"), New System.Data.Common.DataColumnMapping("EmpresaID", "EmpresaID"), New System.Data.Common.DataColumnMapping("CuentaID", "CuentaID"), New System.Data.Common.DataColumnMapping("SaldoInicialReal", "SaldoInicialReal"), New System.Data.Common.DataColumnMapping("SaldoFinalReal", "SaldoFinalReal"), New System.Data.Common.DataColumnMapping("SaldoCreditoRevolvente", "SaldoCreditoRevolvente"), New System.Data.Common.DataColumnMapping("CreditoPorUtilizar", "CreditoPorUtilizar"), New System.Data.Common.DataColumnMapping("SaldoEnCheques", "SaldoEnCheques"), New System.Data.Common.DataColumnMapping("CantidadDeCheques", "CantidadDeCheques"), New System.Data.Common.DataColumnMapping("SaldoVencido", "SaldoVencido"), New System.Data.Common.DataColumnMapping("SaldoPorVencer", "SaldoPorVencer"), New System.Data.Common.DataColumnMapping("LM", "LM"), New System.Data.Common.DataColumnMapping("Codigo", "Codigo"), New System.Data.Common.DataColumnMapping("Numero", "Numero"), New System.Data.Common.DataColumnMapping("CuentaID2", "CuentaID2"), New System.Data.Common.DataColumnMapping("Nomina", "Nomina"), New System.Data.Common.DataColumnMapping("CuentaBancariaIDCaja", "CuentaBancariaIDCaja"), New System.Data.Common.DataColumnMapping("MontoCh", "MontoCh"), New System.Data.Common.DataColumnMapping("ExtraccionCH", "ExtraccionCH"), New System.Data.Common.DataColumnMapping("SaldoCH", "SaldoCH"), New System.Data.Common.DataColumnMapping("NominaF", "NominaF"), New System.Data.Common.DataColumnMapping("Orden", "Orden"), New System.Data.Common.DataColumnMapping("SaldoTeorico", "SaldoTeorico"), New System.Data.Common.DataColumnMapping("CantidadDeMovimientos", "CantidadDeMovimientos"), New System.Data.Common.DataColumnMapping("FUltimaImportacion", "FUltimaImportacion"), New System.Data.Common.DataColumnMapping("FUltimaCaptura", "FUltimaCaptura"), New System.Data.Common.DataColumnMapping("Incidencia", "Incidencia")})})
        '
        'SqlCommand2
        '
        Me.SqlCommand2.CommandText = "dbo.PDD_CuentaBancaria"
        Me.SqlCommand2.CommandType = System.Data.CommandType.StoredProcedure
        Me.SqlCommand2.Connection = Me.SqlConnection2
        Me.SqlCommand2.Parameters.AddRange(New System.Data.SqlClient.SqlParameter() {New System.Data.SqlClient.SqlParameter("@RETURN_VALUE", System.Data.SqlDbType.Int, 4, System.Data.ParameterDirection.ReturnValue, False, CType(0, Byte), CType(0, Byte), "", System.Data.DataRowVersion.Current, Nothing), New System.Data.SqlClient.SqlParameter("@GrupoID", System.Data.SqlDbType.UniqueIdentifier, 16)})
        '
        'daPDD_CuentaBancaria
        '
        Me.daPDD_CuentaBancaria.SelectCommand = Me.sqlPDD_CuentaBancaria
        Me.daPDD_CuentaBancaria.TableMappings.AddRange(New System.Data.Common.DataTableMapping() {New System.Data.Common.DataTableMapping("Table", "PDD_CuentaBancaria", New System.Data.Common.DataColumnMapping() {New System.Data.Common.DataColumnMapping("CuentaBancariaID", "CuentaBancariaID"), New System.Data.Common.DataColumnMapping("GrupoID", "GrupoID"), New System.Data.Common.DataColumnMapping("BancoID", "BancoID"), New System.Data.Common.DataColumnMapping("CuentaContableID", "CuentaContableID"), New System.Data.Common.DataColumnMapping("NúmeroCuenta", "NúmeroCuenta"), New System.Data.Common.DataColumnMapping("Sucursal", "Sucursal"), New System.Data.Common.DataColumnMapping("SiguienteCheque", "SiguienteCheque"), New System.Data.Common.DataColumnMapping("MonedaID", "MonedaID"), New System.Data.Common.DataColumnMapping("SaldoInicial", "SaldoInicial"), New System.Data.Common.DataColumnMapping("SaldoConciliado", "SaldoConciliado"), New System.Data.Common.DataColumnMapping("Status", "Status"), New System.Data.Common.DataColumnMapping("Tipo", "Tipo"), New System.Data.Common.DataColumnMapping("FechaDeApertura", "FechaDeApertura"), New System.Data.Common.DataColumnMapping("DiaDeCorte", "DiaDeCorte"), New System.Data.Common.DataColumnMapping("Formato", "Formato"), New System.Data.Common.DataColumnMapping("LimiteDeCredito", "LimiteDeCredito"), New System.Data.Common.DataColumnMapping("PagoMinimo", "PagoMinimo"), New System.Data.Common.DataColumnMapping("Ejecutivo", "Ejecutivo"), New System.Data.Common.DataColumnMapping("Telefono", "Telefono"), New System.Data.Common.DataColumnMapping("SaldoReal", "SaldoReal"), New System.Data.Common.DataColumnMapping("DepSBC", "DepSBC"), New System.Data.Common.DataColumnMapping("Disponible", "Disponible"), New System.Data.Common.DataColumnMapping("Posfechados", "Posfechados"), New System.Data.Common.DataColumnMapping("EnTransito", "EnTransito"), New System.Data.Common.DataColumnMapping("XCubrirMartes", "XCubrirMartes"), New System.Data.Common.DataColumnMapping("XCubrirMiercoles", "XCubrirMiercoles"), New System.Data.Common.DataColumnMapping("XCubrirJueves", "XCubrirJueves"), New System.Data.Common.DataColumnMapping("XCubrirViernes", "XCubrirViernes"), New System.Data.Common.DataColumnMapping("EmpresaID", "EmpresaID"), New System.Data.Common.DataColumnMapping("CuentaID", "CuentaID"), New System.Data.Common.DataColumnMapping("SaldoInicialReal", "SaldoInicialReal"), New System.Data.Common.DataColumnMapping("SaldoFinalReal", "SaldoFinalReal"), New System.Data.Common.DataColumnMapping("SaldoCreditoRevolvente", "SaldoCreditoRevolvente"), New System.Data.Common.DataColumnMapping("CreditoPorUtilizar", "CreditoPorUtilizar"), New System.Data.Common.DataColumnMapping("SaldoEnCheques", "SaldoEnCheques"), New System.Data.Common.DataColumnMapping("CantidadDeCheques", "CantidadDeCheques"), New System.Data.Common.DataColumnMapping("SaldoVencido", "SaldoVencido"), New System.Data.Common.DataColumnMapping("SaldoPorVencer", "SaldoPorVencer"), New System.Data.Common.DataColumnMapping("LM", "LM"), New System.Data.Common.DataColumnMapping("Codigo", "Codigo"), New System.Data.Common.DataColumnMapping("Numero", "Numero"), New System.Data.Common.DataColumnMapping("CuentaID2", "CuentaID2"), New System.Data.Common.DataColumnMapping("Nomina", "Nomina"), New System.Data.Common.DataColumnMapping("CuentaBancariaIDCaja", "CuentaBancariaIDCaja"), New System.Data.Common.DataColumnMapping("MontoCh", "MontoCh"), New System.Data.Common.DataColumnMapping("ExtraccionCH", "ExtraccionCH"), New System.Data.Common.DataColumnMapping("SaldoCH", "SaldoCH"), New System.Data.Common.DataColumnMapping("NominaF", "NominaF"), New System.Data.Common.DataColumnMapping("Orden", "Orden"), New System.Data.Common.DataColumnMapping("SaldoTeorico", "SaldoTeorico"), New System.Data.Common.DataColumnMapping("CantidadDeMovimientos", "CantidadDeMovimientos"), New System.Data.Common.DataColumnMapping("FUltimaImportacion", "FUltimaImportacion"), New System.Data.Common.DataColumnMapping("FUltimaCaptura", "FUltimaCaptura"), New System.Data.Common.DataColumnMapping("Incidencia", "Incidencia")})})
        '
        'sqlPDD_CuentaBancaria
        '
        Me.sqlPDD_CuentaBancaria.CommandText = "dbo.PDD_CuentaBancaria"
        Me.sqlPDD_CuentaBancaria.CommandType = System.Data.CommandType.StoredProcedure
        Me.sqlPDD_CuentaBancaria.Connection = Me.SqlConnection2
        Me.sqlPDD_CuentaBancaria.Parameters.AddRange(New System.Data.SqlClient.SqlParameter() {New System.Data.SqlClient.SqlParameter("@RETURN_VALUE", System.Data.SqlDbType.Int, 4, System.Data.ParameterDirection.ReturnValue, False, CType(0, Byte), CType(0, Byte), "", System.Data.DataRowVersion.Current, Nothing), New System.Data.SqlClient.SqlParameter("@GrupoID", System.Data.SqlDbType.UniqueIdentifier, 16)})
        '
        'ComboBoxEdit2
        '
        Me.ComboBoxEdit2.Location = New System.Drawing.Point(483, 141)
        Me.ComboBoxEdit2.Name = "ComboBoxEdit2"
        Me.ComboBoxEdit2.Properties.Buttons.AddRange(New DevExpress.XtraEditors.Controls.EditorButton() {New DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)})
        Me.ComboBoxEdit2.Properties.Items.AddRange(New Object() {"Contado", "Crédito"})
        Me.ComboBoxEdit2.Size = New System.Drawing.Size(101, 20)
        Me.ComboBoxEdit2.TabIndex = 706
        '
        'CheckEdit1
        '
        Me.CheckEdit1.Location = New System.Drawing.Point(6, 140)
        Me.CheckEdit1.Name = "CheckEdit1"
        Me.CheckEdit1.Properties.Appearance.Font = New System.Drawing.Font("Tahoma", 8.25!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.CheckEdit1.Properties.Appearance.Options.UseFont = True
        Me.CheckEdit1.Properties.Caption = "Maniobras"
        Me.CheckEdit1.Properties.GlyphAlignment = DevExpress.Utils.HorzAlignment.Far
        Me.CheckEdit1.RightToLeft = System.Windows.Forms.RightToLeft.Yes
        Me.CheckEdit1.Size = New System.Drawing.Size(82, 19)
        Me.CheckEdit1.TabIndex = 704
        '
        'DockManager4
        '
        Me.DockManager4.AutoHideContainers.AddRange(New DevExpress.XtraBars.Docking.AutoHideContainer() {Me.hideContainerRight})
        Me.DockManager4.Form = Me
        Me.DockManager4.TopZIndexControls.AddRange(New String() {"DevExpress.XtraBars.BarDockControl", "DevExpress.XtraBars.StandaloneBarDockControl", "System.Windows.Forms.StatusBar", "System.Windows.Forms.MenuStrip", "System.Windows.Forms.StatusStrip", "DevExpress.XtraBars.Ribbon.RibbonStatusBar", "DevExpress.XtraBars.Ribbon.RibbonControl"})
        '
        'hideContainerRight
        '
        Me.hideContainerRight.BackColor = System.Drawing.Color.White
        Me.hideContainerRight.Controls.Add(Me.DockPanel3)
        Me.hideContainerRight.Dock = System.Windows.Forms.DockStyle.Right
        Me.hideContainerRight.Location = New System.Drawing.Point(1263, 0)
        Me.hideContainerRight.Name = "hideContainerRight"
        Me.hideContainerRight.Size = New System.Drawing.Size(21, 680)
        '
        'DockPanel3
        '
        Me.DockPanel3.Controls.Add(Me.DockPanel3_Container)
        Me.DockPanel3.Dock = DevExpress.XtraBars.Docking.DockingStyle.Right
        Me.DockPanel3.ID = New System.Guid("382207de-610e-40bc-b93a-da87089eea93")
        Me.DockPanel3.Location = New System.Drawing.Point(723, 0)
        Me.DockPanel3.Name = "DockPanel3"
        Me.DockPanel3.OriginalSize = New System.Drawing.Size(542, 200)
        Me.DockPanel3.SavedDock = DevExpress.XtraBars.Docking.DockingStyle.Right
        Me.DockPanel3.SavedIndex = 0
        Me.DockPanel3.Size = New System.Drawing.Size(542, 720)
        Me.DockPanel3.Text = "Foto del producto"
        Me.DockPanel3.Visibility = DevExpress.XtraBars.Docking.DockVisibility.AutoHide
        '
        'DockPanel3_Container
        '
        Me.DockPanel3_Container.Controls.Add(Me.PicFoto)
        Me.DockPanel3_Container.Location = New System.Drawing.Point(4, 23)
        Me.DockPanel3_Container.Name = "DockPanel3_Container"
        Me.DockPanel3_Container.Size = New System.Drawing.Size(534, 693)
        Me.DockPanel3_Container.TabIndex = 0
        '
        'PicFoto
        '
        Me.PicFoto.BackColor = System.Drawing.Color.White
        Me.PicFoto.Dock = System.Windows.Forms.DockStyle.Fill
        Me.PicFoto.Location = New System.Drawing.Point(0, 0)
        Me.PicFoto.Name = "PicFoto"
        Me.PicFoto.Size = New System.Drawing.Size(534, 693)
        Me.PicFoto.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage
        Me.PicFoto.TabIndex = 5
        Me.PicFoto.TabStop = False
        Me.PicFoto.Visible = False
        '
        'GridEX1
        '
        Me.GridEX1.AllowAddNew = Janus.Windows.GridEX.InheritableBoolean.[True]
        Me.GridEX1.AllowDelete = Janus.Windows.GridEX.InheritableBoolean.[True]
        Me.GridEX1.AlternatingRowFormatStyle.BackColor = System.Drawing.Color.LightCyan
        Me.GridEX1.BackgroundImage = CType(resources.GetObject("GridEX1.BackgroundImage"), System.Drawing.Image)
        Me.GridEX1.DataMember = "Pedidos.PedidosPedidos_Details"
        Me.GridEX1.DataSource = Me.dsBase
        GridEXLayout6.LayoutString = resources.GetString("GridEXLayout6.LayoutString")
        Me.GridEX1.DesignTimeLayout = GridEXLayout6
        Me.GridEX1.EditorsControlStyle.ButtonAppearance = Janus.Windows.GridEX.ButtonAppearance.Regular
        Me.GridEX1.Font = New System.Drawing.Font("Microsoft Sans Serif", 8.25!)
        Me.GridEX1.GroupByBoxVisible = False
        Me.GridEX1.Location = New System.Drawing.Point(17, 173)
        Me.GridEX1.Name = "GridEX1"
        Me.GridEX1.PreviewRowFormatStyle.BackColor = System.Drawing.Color.FromArgb(CType(CType(192, Byte), Integer), CType(CType(255, Byte), Integer), CType(CType(192, Byte), Integer))
        Me.GridEX1.RecordNavigator = True
        Me.GridEX1.RecordNavigatorText = "Producto:|de"
        Me.GridEX1.RowHeaders = Janus.Windows.GridEX.InheritableBoolean.[True]
        Me.GridEX1.SelectionMode = Janus.Windows.GridEX.SelectionMode.MultipleSelection
        Me.GridEX1.Size = New System.Drawing.Size(1337, 342)
        Me.GridEX1.TabIndex = 126
        Me.GridEX1.WatermarkImage.Size = New System.Drawing.Size(400, 400)
        '
        'daCliente
        '
        Me.daCliente.SelectCommand = Me.sqlCliente
        Me.daCliente.TableMappings.AddRange(New System.Data.Common.DataTableMapping() {New System.Data.Common.DataTableMapping("Table", "PClienteList", New System.Data.Common.DataColumnMapping() {New System.Data.Common.DataColumnMapping("ClienteID", "ClienteID"), New System.Data.Common.DataColumnMapping("Nombre", "Nombre"), New System.Data.Common.DataColumnMapping("PrecioID", "PrecioID"), New System.Data.Common.DataColumnMapping("Precio", "Precio"), New System.Data.Common.DataColumnMapping("Saldo", "Saldo"), New System.Data.Common.DataColumnMapping("DiasCredito", "DiasCredito"), New System.Data.Common.DataColumnMapping("LimiteCredito", "LimiteCredito")})})
        '
        'sqlCliente
        '
        Me.sqlCliente.CommandText = "dbo.PClienteList"
        Me.sqlCliente.CommandType = System.Data.CommandType.StoredProcedure
        Me.sqlCliente.Connection = Me.SqlConnection2
        Me.sqlCliente.Parameters.AddRange(New System.Data.SqlClient.SqlParameter() {New System.Data.SqlClient.SqlParameter("@RETURN_VALUE", System.Data.SqlDbType.Int, 4, System.Data.ParameterDirection.ReturnValue, False, CType(0, Byte), CType(0, Byte), "", System.Data.DataRowVersion.Current, Nothing), New System.Data.SqlClient.SqlParameter("@GrupoID", System.Data.SqlDbType.UniqueIdentifier, 16)})
        '
        'daPDD_EnvioA
        '
        Me.daPDD_EnvioA.SelectCommand = Me.SqlCommand1
        Me.daPDD_EnvioA.TableMappings.AddRange(New System.Data.Common.DataTableMapping() {New System.Data.Common.DataTableMapping("Table", "PDD_EnvioA", New System.Data.Common.DataColumnMapping() {New System.Data.Common.DataColumnMapping("EnvioAID", "EnvioAID"), New System.Data.Common.DataColumnMapping("Clave", "Clave"), New System.Data.Common.DataColumnMapping("Nombre", "Nombre")})})
        '
        'SqlCommand1
        '
        Me.SqlCommand1.CommandText = "dbo.PDD_EnvioA"
        Me.SqlCommand1.CommandType = System.Data.CommandType.StoredProcedure
        Me.SqlCommand1.Connection = Me.SqlConnection2
        Me.SqlCommand1.Parameters.AddRange(New System.Data.SqlClient.SqlParameter() {New System.Data.SqlClient.SqlParameter("@RETURN_VALUE", System.Data.SqlDbType.[Variant], 0, System.Data.ParameterDirection.ReturnValue, False, CType(0, Byte), CType(0, Byte), "", System.Data.DataRowVersion.Current, Nothing)})
        '
        'PDDEnvioABindingSource
        '
        Me.PDDEnvioABindingSource.DataMember = "PDD_EnvioA"
        Me.PDDEnvioABindingSource.DataSource = Me.DsPDD_EnvioA1
        '
        'daPDD_AlmacenesPorUsuario
        '
        Me.daPDD_AlmacenesPorUsuario.SelectCommand = Me.sqlPDD_AlmacenesPorUsuario
        Me.daPDD_AlmacenesPorUsuario.TableMappings.AddRange(New System.Data.Common.DataTableMapping() {New System.Data.Common.DataTableMapping("Table", "PDD_Almacenes_PorUsuario", New System.Data.Common.DataColumnMapping() {New System.Data.Common.DataColumnMapping("AlmacenID", "AlmacenID"), New System.Data.Common.DataColumnMapping("Nombre", "Nombre")}), New System.Data.Common.DataTableMapping("Table1", "Table1", New System.Data.Common.DataColumnMapping() {New System.Data.Common.DataColumnMapping("AlmacenID", "AlmacenID"), New System.Data.Common.DataColumnMapping("Nombre", "Nombre")})})
        '
        'sqlPDD_AlmacenesPorUsuario
        '
        Me.sqlPDD_AlmacenesPorUsuario.CommandText = "dbo.PDD_Almacenes_PorUsuario"
        Me.sqlPDD_AlmacenesPorUsuario.CommandType = System.Data.CommandType.StoredProcedure
        Me.sqlPDD_AlmacenesPorUsuario.Connection = Me.SqlConnection2
        Me.sqlPDD_AlmacenesPorUsuario.Parameters.AddRange(New System.Data.SqlClient.SqlParameter() {New System.Data.SqlClient.SqlParameter("@RETURN_VALUE", System.Data.SqlDbType.Int, 4, System.Data.ParameterDirection.ReturnValue, False, CType(0, Byte), CType(0, Byte), "", System.Data.DataRowVersion.Current, Nothing), New System.Data.SqlClient.SqlParameter("@EmpresaID", System.Data.SqlDbType.UniqueIdentifier, 16), New System.Data.SqlClient.SqlParameter("@UsuarioID", System.Data.SqlDbType.UniqueIdentifier, 16)})
        '
        'DsPDD_Almacenes1
        '
        Me.DsPDD_Almacenes1.DataSetName = "dsPDD_Almacenes"
        Me.DsPDD_Almacenes1.SchemaSerializationMode = System.Data.SchemaSerializationMode.IncludeSchema
        '
        'DsPDD_Almacen
        '
        Me.DsPDD_Almacen.DataSetName = "dsPDD_Almacen"
        Me.DsPDD_Almacen.SchemaSerializationMode = System.Data.SchemaSerializationMode.IncludeSchema
        '
        'PDDAlmacenBindingSource
        '
        Me.PDDAlmacenBindingSource.DataMember = "PDD_Almacen"
        Me.PDDAlmacenBindingSource.DataSource = Me.DsPDD_Almacen
        '
        'PDDAlmacenBindingSource1
        '
        Me.PDDAlmacenBindingSource1.DataMember = "PDD_Almacen"
        Me.PDDAlmacenBindingSource1.DataSource = Me.DsPDD_Almacen
        '
        'PDDAlmacenesPorUsuarioBindingSource
        '
        Me.PDDAlmacenesPorUsuarioBindingSource.DataMember = "PDD_Almacenes_PorUsuario"
        Me.PDDAlmacenesPorUsuarioBindingSource.DataSource = Me.DsPDD_Almacenes1
        '
        'SqlDataAdapter2
        '
        Me.SqlDataAdapter2.SelectCommand = Me.SqlCommand3
        Me.SqlDataAdapter2.TableMappings.AddRange(New System.Data.Common.DataTableMapping() {New System.Data.Common.DataTableMapping("Table", "PDD_Paqueteria", New System.Data.Common.DataColumnMapping() {New System.Data.Common.DataColumnMapping("PaqueteriaID", "PaqueteriaID"), New System.Data.Common.DataColumnMapping("Nombre", "Nombre")})})
        '
        'SqlCommand3
        '
        Me.SqlCommand3.CommandText = "dbo.PDD_Paqueteria"
        Me.SqlCommand3.CommandType = System.Data.CommandType.StoredProcedure
        Me.SqlCommand3.Connection = Me.SqlConnection2
        Me.SqlCommand3.Parameters.AddRange(New System.Data.SqlClient.SqlParameter() {New System.Data.SqlClient.SqlParameter("@RETURN_VALUE", System.Data.SqlDbType.Int, 4, System.Data.ParameterDirection.ReturnValue, False, CType(0, Byte), CType(0, Byte), "", System.Data.DataRowVersion.Current, Nothing)})
        '
        'DsPDDPaqueteria1BindingSource
        '
        Me.DsPDDPaqueteria1BindingSource.DataSource = Me.Ds_PDDPaqueteria1
        Me.DsPDDPaqueteria1BindingSource.Position = 0
        '
        'frmPedidos
        '
        Me.AutoScaleBaseSize = New System.Drawing.Size(5, 13)
        Me.BackColor = System.Drawing.Color.White
        Me.ClientSize = New System.Drawing.Size(1284, 680)
        Me.ControlBox = False
        Me.Controls.Add(Me.Panel1)
        Me.Controls.Add(Me.GroupBoxCliente)
        Me.Controls.Add(Me.GridEX1)
        Me.Controls.Add(Me.hideContainerRight)
        Me.Icon = CType(resources.GetObject("$this.Icon"), System.Drawing.Icon)
        Me.Menu = Me.MainMenu1
        Me.Name = "frmPedidos"
        Me.Text = "Pedidos de venta para producción"
        Me.WindowState = System.Windows.Forms.FormWindowState.Maximized
        CType(Me.dsBase, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.Dataset51, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.dsDataset2, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.dsDataset3, System.ComponentModel.ISupportInitialize).EndInit()
        Me.GroupBox2.ResumeLayout(False)
        Me.Panel1.ResumeLayout(False)
        Me.Panel1.PerformLayout()
        CType(Me.DockManager1, System.ComponentModel.ISupportInitialize).EndInit()
        Me.DockPanel1.ResumeLayout(False)
        Me.DockPanel1_Container.ResumeLayout(False)
        CType(Me.DockManager2, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.DockManager3, System.ComponentModel.ISupportInitialize).EndInit()
        Me.DockPanel2.ResumeLayout(False)
        Me.DockPanel2_Container.ResumeLayout(False)
        CType(Me.Guava_Data1, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.GridEX2, System.ComponentModel.ISupportInitialize).EndInit()
        Me.GroupBoxCliente.ResumeLayout(False)
        Me.GroupBoxCliente.PerformLayout()
        CType(Me.LookPaqueteria.Properties, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.PDDPaqueteriaBindingSource, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.Ds_PDDPaqueteria1, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.LookAlmacen.Properties, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.PDDAlmacenesPorUsuarioBindingSource1, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.DsPDD_AlmacenesPorUsuario1, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.txtOcurreA.Properties, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.LookEnvioA.Properties, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.PDDEnvioABindingSource1, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.DsPDD_EnvioA1, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.LookCliente.Properties, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.PClienteListBindingSource1, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.DsPedidoZ21, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.ComboManiobras.Properties, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.txtUsoDelCFDI.Properties, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.cmdEmpacado.Properties, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.LookCuentaBancaria.Properties, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.PDDCuentaBancariaBindingSource, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.DsNuevo1, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.PClienteListBindingSource, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.PPedidos2SelectBindingSource, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.DsPedidos2, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.DsCuentaBancaria1BindingSource, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.ComboBoxEdit2.Properties, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.CheckEdit1.Properties, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.DockManager4, System.ComponentModel.ISupportInitialize).EndInit()
        Me.hideContainerRight.ResumeLayout(False)
        Me.DockPanel3.ResumeLayout(False)
        Me.DockPanel3_Container.ResumeLayout(False)
        CType(Me.PicFoto, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.GridEX1, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.PPedidosSelect02BindingSource, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.PDDEnvioABindingSource, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.DsPDD_Almacenes1, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.DsPDD_Almacen, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.PDDAlmacenBindingSource, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.PDDAlmacenBindingSource1, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.PDDAlmacenesPorUsuarioBindingSource, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.DsPDDPaqueteria1BindingSource, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ResumeLayout(False)

    End Sub

#End Region

    Private MyImage As Bitmap

    Public LM As Boolean
    Public PrecioID As Guid
    Public DescuentoCliente As Double
    Public Corridas As Boolean
    Public SoloScanear As Boolean
    Public PedidosID As Guid
    Public EmpresaID As Guid
    Public DocumentoID As Integer
    Private mActiveView As IView
    Dim BDesglosarPedido As Boolean
    Dim FAnterior As DateTime
    Dim BVenderAutorizadamente As Boolean
    Dim ChecarTamañoDelLote As Boolean
    Dim Fila As Integer
    Dim YaAutorizado As Boolean
    Dim PoderEliminar As Boolean
    Dim UOrdenCliente As String
    Dim UClienteCadenaID As Guid
    Dim BProgramado As Boolean
    Dim UltimoEnviarDireccion As String
    Dim DiasAdelanteEnPedidos As Integer
    Dim BFecha As Boolean
    Private mData2 As DataView
    Dim ObsCliente As String
    Dim AnteriorCorridaID As Guid
    Dim BPoderModificarFechaDelPedido As Boolean
    Dim PrecioLista As Decimal
    Dim BPrecioAutorizado As Boolean
    Dim IVA As Double
    Dim BExigirPrecioEnPedido As Boolean

    Dim FSembrado As DateTime
    Dim BManejoDeComercializadora As Boolean
    Dim Posicion As Boolean


    Dim BDesglosar As Boolean

    Public Property ActiveView() As IView
        Get
            Return mActiveView
        End Get
        Set(ByVal Value As IView)
            If Not mActiveView Is Value Then
                If Not mActiveView Is Nothing Then
                    mActiveView.UpdateData()
                End If
                '  Me.ActiveControl = panRight
                ' Me.panRight.Controls.Clear()
                If Not mActiveView Is Nothing Then
                    mActiveView.ViewControl.Dispose()
                End If
                mActiveView = Value
                If Not mActiveView Is Nothing Then
                    mActiveView.ViewControl.Dock = DockStyle.Fill
                    '    Me.panRight.Controls.Add(mActiveView.ViewControl)
                    '  ActiveControl = Me.ActiveView.ViewControl()
                End If
            End If

            OnActiveViewChanged()
        End Set
    End Property
    Private Sub OnActiveViewChanged()
        Dim viewsVisible As Boolean
        If Not mActiveView Is Nothing Then
        Else
            viewsVisible = False

        End If
    End Sub


    Dim Nuevo As Boolean
    Dim DocumentoNombre As String

    Dim mCodigo As String
    Dim mProductoID As Guid
    Dim mCantidad As Double
    Dim mP1 As Double
    Dim mP2 As Double
    Dim mP3 As Double
    Dim mP4 As Double
    Dim mP5 As Double
    Dim mP6 As Double
    Dim mP7 As Double
    Dim mP8 As Double
    Dim mP9 As Double
    Dim mP10 As Double
    Dim mP11 As Double
    Dim mP12 As Double
    Dim mP13 As Double
    Dim mP14 As Double
    Dim mP15 As Double
    Dim mP16 As Double
    Dim mP17 As Double
    Dim mP18 As Double
    Dim mP19 As Double
    Dim mP20 As Double
    Dim mP21 As Double
    Dim mP22 As Double
    Dim mP23 As Double
    Dim mP24 As Double
    Dim mP25 As Double
    Dim mP26 As Double
    Dim mP27 As Double
    Dim mP28 As Double
    Dim mP29 As Double
    Dim mP30 As Double
    Dim mUnidadID As Guid
    Dim mPrecio As Decimal
    Dim mDescuento As Decimal
    Dim mIVA As Double
    Dim mNeto As Decimal
    Dim mImporte As Decimal
    Dim mObs As String
    Dim mPrecioID As Guid
    Dim mFechaEntrega As Date
    Dim mFechaEmbarque As Date
    Dim mOrdenCliente As String
    Dim AntFechaEmbarque As Date
    Dim mClienteCadenaID As Guid
    Dim mClienteCadenaIDEnviar As Guid

    Dim mObs1 As String
    Dim mObs2 As String
    Dim mObs3 As String
    Dim mSemana As String

    Dim mExplosion As Integer
    Dim mPlaneacion As String
    Dim mLugar As Integer
    Dim mFPlaneacion As Date
    Dim mPlanear As Boolean
    Dim mFasesID As Guid
    Dim mExplosionar As Boolean
    Dim mFExplosion As Date


    Dim PPrecioTope As Decimal

    Private mDataRow As DataRowView
    Public ReadOnly Property DataRow() As DataRowView
        Get
            Return mDataRow
        End Get
    End Property
    Public Sub Edit(ByVal row As DataRowView)

        Dim Comando As SqlClient.SqlCommand
        Dim Lector As SqlClient.SqlDataReader

        Me.SqlConnection2.ConnectionString = Scnn

        Me.SqlConnection1 = New System.Data.SqlClient.SqlConnection


        Me.daPrecio = New System.Data.SqlClient.SqlDataAdapter
        Me.SqlSelectCommand1_ = New System.Data.SqlClient.SqlCommand
        Me.daUnidad = New System.Data.SqlClient.SqlDataAdapter
        Me.SqlSelectCommand3 = New System.Data.SqlClient.SqlCommand
        Me.daPDD_Producto = New System.Data.SqlClient.SqlDataAdapter
        Me.sqlPDD_Producto = New System.Data.SqlClient.SqlCommand
        Me.sqlPDD_ClienteCadena = New System.Data.SqlClient.SqlCommand
        Me.daPDD_ClienteCadena = New System.Data.SqlClient.SqlDataAdapter

        Me.sqlPDD_ClienteCadena_Enviar = New System.Data.SqlClient.SqlCommand
        Me.daPDD_ClienteCadena_Enviar = New System.Data.SqlClient.SqlDataAdapter

        Me.daPCalendarioProduccionPedidos = New System.Data.SqlClient.SqlDataAdapter
        Me.sqlPCalendarioProduccionPedidos = New System.Data.SqlClient.SqlCommand
        'daPrecio
        '
        Me.daPrecio.SelectCommand = Me.SqlSelectCommand1_
        Me.daPrecio.TableMappings.AddRange(New System.Data.Common.DataTableMapping() {New System.Data.Common.DataTableMapping("Table", "PDropDownPrecio", New System.Data.Common.DataColumnMapping() {New System.Data.Common.DataColumnMapping("PrecioID", "PrecioID"), New System.Data.Common.DataColumnMapping("Nombre", "Nombre")})})
        '
        'SqlSelectCommand1_
        '
        Me.SqlSelectCommand1_.CommandText = "[PDropDownPrecio]"
        Me.SqlSelectCommand1_.CommandTimeout = 300
        Me.SqlSelectCommand1_.CommandType = System.Data.CommandType.StoredProcedure
        Me.SqlSelectCommand1_.Connection = Me.SqlConnection1
        Me.SqlSelectCommand1_.Parameters.Add(New System.Data.SqlClient.SqlParameter("@RETURN_VALUE", System.Data.SqlDbType.Int, 4, System.Data.ParameterDirection.ReturnValue, False, CType(10, Byte), CType(0, Byte), "", System.Data.DataRowVersion.Current, Nothing))
        Me.SqlSelectCommand1_.Parameters.Add(New System.Data.SqlClient.SqlParameter("@GrupoID", System.Data.SqlDbType.UniqueIdentifier, 16))
        '
        'daUnidad
        '
        Me.daUnidad.SelectCommand = Me.SqlSelectCommand3
        Me.daUnidad.TableMappings.AddRange(New System.Data.Common.DataTableMapping() {New System.Data.Common.DataTableMapping("Table", "PDropDownUnidad", New System.Data.Common.DataColumnMapping() {New System.Data.Common.DataColumnMapping("UnidadID", "UnidadID"), New System.Data.Common.DataColumnMapping("Nombre", "Nombre")})})
        '
        'SqlSelectCommand3
        '
        Me.SqlSelectCommand3.CommandText = "[PDropDownUnidad]"
        Me.SqlSelectCommand3.CommandTimeout = 300
        Me.SqlSelectCommand3.CommandType = System.Data.CommandType.StoredProcedure
        Me.SqlSelectCommand3.Connection = Me.SqlConnection1
        Me.SqlSelectCommand3.Parameters.Add(New System.Data.SqlClient.SqlParameter("@RETURN_VALUE", System.Data.SqlDbType.Int, 4, System.Data.ParameterDirection.ReturnValue, False, CType(10, Byte), CType(0, Byte), "", System.Data.DataRowVersion.Current, Nothing))
        Me.SqlSelectCommand3.Parameters.Add(New System.Data.SqlClient.SqlParameter("@GrupoID", System.Data.SqlDbType.UniqueIdentifier, 16))
        '
        'daPDD_Producto
        '
        Me.daPDD_Producto.SelectCommand = Me.sqlPDD_Producto
        Me.daPDD_Producto.TableMappings.AddRange(New System.Data.Common.DataTableMapping() {New System.Data.Common.DataTableMapping("Table", "PDD_ProductoParaPedido", New System.Data.Common.DataColumnMapping() {New System.Data.Common.DataColumnMapping("ProductoID", "ProductoID"), New System.Data.Common.DataColumnMapping("Codigo", "Codigo"), New System.Data.Common.DataColumnMapping("Nombre", "Nombre"), New System.Data.Common.DataColumnMapping("Estilo", "Estilo"), New System.Data.Common.DataColumnMapping("Color", "Color"), New System.Data.Common.DataColumnMapping("Piel", "Piel"), New System.Data.Common.DataColumnMapping("Forro", "Forro"), New System.Data.Common.DataColumnMapping("Troquel", "Troquel"), New System.Data.Common.DataColumnMapping("Suela", "Suela"), New System.Data.Common.DataColumnMapping("ProdCaja", "ProdCaja"), New System.Data.Common.DataColumnMapping("Corrida", "Corrida"), New System.Data.Common.DataColumnMapping("EstiloID", "EstiloID"), New System.Data.Common.DataColumnMapping("ColorID", "ColorID"), New System.Data.Common.DataColumnMapping("PielID", "PielID"), New System.Data.Common.DataColumnMapping("ForroID", "ForroID"), New System.Data.Common.DataColumnMapping("TroquelID", "TroquelID"), New System.Data.Common.DataColumnMapping("SuelaID", "SuelaID"), New System.Data.Common.DataColumnMapping("ProdCajaID", "ProdCajaID"), New System.Data.Common.DataColumnMapping("CorridaID", "CorridaID")})})
        '
        'sqlPDD_Producto
        '
        Me.sqlPDD_Producto.CommandText = "[PDD_ProductoParaPedido]"
        Me.sqlPDD_Producto.CommandTimeout = 300
        Me.sqlPDD_Producto.CommandType = System.Data.CommandType.StoredProcedure
        Me.sqlPDD_Producto.Connection = Me.SqlConnection1
        Me.sqlPDD_Producto.Parameters.Add(New System.Data.SqlClient.SqlParameter("@RETURN_VALUE", System.Data.SqlDbType.Int, 4, System.Data.ParameterDirection.ReturnValue, False, CType(0, Byte), CType(0, Byte), "", System.Data.DataRowVersion.Current, Nothing))
        Me.sqlPDD_Producto.Parameters.Add(New System.Data.SqlClient.SqlParameter("@GrupoID", System.Data.SqlDbType.UniqueIdentifier, 16))
        Me.sqlPDD_Producto.Parameters.Add(New System.Data.SqlClient.SqlParameter("@Nuevo", System.Data.SqlDbType.Bit, 1, "Nuevo"))
        Me.sqlPDD_Producto.Parameters.Add(New System.Data.SqlClient.SqlParameter("@ClienteID", System.Data.SqlDbType.UniqueIdentifier, 16))
        '


        'daPDD_ClienteCadena
        '
        Me.daPDD_ClienteCadena.SelectCommand = Me.sqlPDD_ClienteCadena
        Me.daPDD_ClienteCadena.TableMappings.AddRange(New System.Data.Common.DataTableMapping() {New System.Data.Common.DataTableMapping("Table", "PDD_ClienteCadena", New System.Data.Common.DataColumnMapping() {New System.Data.Common.DataColumnMapping("ClienteCadenaID", "ClienteCadenaID"), New System.Data.Common.DataColumnMapping("Nombre", "Nombre"), New System.Data.Common.DataColumnMapping("RazonSocial", "RazonSocial")})})
        '
        'sqlPDD_ClienteCadena
        '
        Me.sqlPDD_ClienteCadena.CommandText = "[PDD_ClienteCadena]"
        Me.sqlPDD_ClienteCadena.CommandTimeout = 300
        Me.sqlPDD_ClienteCadena.CommandType = System.Data.CommandType.StoredProcedure
        Me.sqlPDD_ClienteCadena.Connection = Me.SqlConnection1
        Me.sqlPDD_ClienteCadena.Parameters.Add(New System.Data.SqlClient.SqlParameter("@RETURN_VALUE", System.Data.SqlDbType.Int, 4, System.Data.ParameterDirection.ReturnValue, False, CType(0, Byte), CType(0, Byte), "", System.Data.DataRowVersion.Current, Nothing))
        Me.sqlPDD_ClienteCadena.Parameters.Add(New System.Data.SqlClient.SqlParameter("@ClienteID", System.Data.SqlDbType.UniqueIdentifier, 16))


        'daPDD_ClienteCadena_Enviar
        '
        Me.daPDD_ClienteCadena_Enviar.SelectCommand = Me.sqlPDD_ClienteCadena_Enviar
        Me.daPDD_ClienteCadena_Enviar.TableMappings.AddRange(New System.Data.Common.DataTableMapping() {New System.Data.Common.DataTableMapping("Table", "PDD_ClienteCadena_Enviar", New System.Data.Common.DataColumnMapping() {New System.Data.Common.DataColumnMapping("ClienteCadenaID", "ClienteCadenaID"), New System.Data.Common.DataColumnMapping("Nombre", "Nombre"), New System.Data.Common.DataColumnMapping("RazonSocial", "RazonSocial")})})
        '
        'sqlPDD_ClienteCadena_Enviar
        '
        Me.sqlPDD_ClienteCadena_Enviar.CommandText = "[PDD_ClienteCadena_Enviar]"
        Me.sqlPDD_ClienteCadena_Enviar.CommandTimeout = 300
        Me.sqlPDD_ClienteCadena_Enviar.CommandType = System.Data.CommandType.StoredProcedure
        Me.sqlPDD_ClienteCadena_Enviar.Connection = Me.SqlConnection1
        Me.sqlPDD_ClienteCadena_Enviar.Parameters.Add(New System.Data.SqlClient.SqlParameter("@RETURN_VALUE", System.Data.SqlDbType.Int, 4, System.Data.ParameterDirection.ReturnValue, False, CType(0, Byte), CType(0, Byte), "", System.Data.DataRowVersion.Current, Nothing))
        Me.sqlPDD_ClienteCadena_Enviar.Parameters.Add(New System.Data.SqlClient.SqlParameter("@ClienteID", System.Data.SqlDbType.UniqueIdentifier, 16))



        'SqlConnection1
        '
        Me.SqlConnection1.ConnectionString = Scnn
        '
        'daPCalendarioProduccionPedidos
        '
        Me.daPCalendarioProduccionPedidos.SelectCommand = Me.sqlPCalendarioProduccionPedidos
        Me.daPCalendarioProduccionPedidos.TableMappings.AddRange(New System.Data.Common.DataTableMapping() {New System.Data.Common.DataTableMapping("Table", "PCalendarioProduccionPedidos", New System.Data.Common.DataColumnMapping() {New System.Data.Common.DataColumnMapping("Clave", "Clave"), New System.Data.Common.DataColumnMapping("FechaInicial", "FechaInicial"), New System.Data.Common.DataColumnMapping("FechaFinal", "FechaFinal"), New System.Data.Common.DataColumnMapping("Cantidad", "Cantidad")})})
        '
        'sqlPCalendarioProduccionPedidos
        '
        Me.sqlPCalendarioProduccionPedidos.CommandText = "[PCalendarioProduccionPedidos]"
        Me.sqlPCalendarioProduccionPedidos.CommandTimeout = 300
        Me.sqlPCalendarioProduccionPedidos.CommandType = System.Data.CommandType.StoredProcedure
        Me.sqlPCalendarioProduccionPedidos.Connection = Me.SqlConnection1
        Me.sqlPCalendarioProduccionPedidos.Parameters.Add(New System.Data.SqlClient.SqlParameter("@RETURN_VALUE", System.Data.SqlDbType.Int, 4, System.Data.ParameterDirection.ReturnValue, False, CType(0, Byte), CType(0, Byte), "", System.Data.DataRowVersion.Current, Nothing))
        Me.sqlPCalendarioProduccionPedidos.Parameters.Add(New System.Data.SqlClient.SqlParameter("@PedidosID", System.Data.SqlDbType.UniqueIdentifier, 16))
        '

        '



        ChecarTamañoDelLote = False
        PrecioLista = 0
        BPrecioAutorizado = False
        BFecha = False
        'VerFSembrado()

        BDesglosar = False
        'Me.GroupBox1.Top = 0
        'Me.GroupBox1.Left = 0
        If row Is Nothing Then
            Dim dv As DataView
            dv = New DataView(Me.dsBase.Pedidos)
            row = dv.AddNew()

        End If
        mDataRow = row

        Dim PedidosRow As Base.PedidosRow
        PedidosRow = mDataRow.Row
        If mDataRow.IsNew Then
            Me.Text = DocumentoNombre & " - Nuevo"
            Nuevo = True
        Else

            Me.Text = DocumentoNombre & " -  # " & PedidosRow.Numero
            Nuevo = False
            Me.GridEX1.FilterMode = FilterMode.Automatic

        End If
        Fila = 1
        Posicion = False
        AnteriorCorridaID = EmpresaID
        SetBindings(mDataRow)

        Dim cnn2 As New SqlClient.SqlConnection
        cnn2.ConnectionString = Scnn
        cnn2.Open()



        Comando = New SqlClient.SqlCommand("PSelectDocumentoNombre", cnn2)
        Comando.CommandType = CommandType.StoredProcedure
        Comando.CommandTimeout = 300
        Comando.Parameters.Add(New System.Data.SqlClient.SqlParameter("@DocumentoID", System.Data.SqlDbType.Int, 4, "DocumentoID"))
        Comando.Parameters("@DocumentoID").Value = DocumentoID

        Lector = Comando.ExecuteReader
        Do While Lector.Read
            DocumentoNombre = Lector!Nombre

        Loop


        Lector.Close()
        cnn2.Close()
        cnn2 = Nothing
        'Me.btnDelete.Visible = Not mDataRow.IsNew

        If mDataRow.IsNew Then
            UpdateChanges()
            Me.txtNumero.DataBindings.Clear()
            Me.txtNumero.DataBindings.Add("Text", row, "Numero")
            Me.txtReferencia.DataBindings.Clear()
            Me.txtReferencia.DataBindings.Add("Text", row, "Referencia")



            Me.txtObsGenerales.DataBindings.Clear()
            Me.txtObsGenerales.DataBindings.Add("Text", row, "ObsGenerales")
            Me.txtObservaciones2.DataBindings.Clear()
            Me.txtObservaciones2.DataBindings.Add("Text", row, "Observaciones2")
            Me.txtObservaciones3.DataBindings.Clear()
            Me.txtObservaciones3.DataBindings.Add("Text", row, "Observaciones3")

        Else
            Me.txtObservaciones2.Text = PedidosRow.Observaciones2
            Me.txtObservaciones3.Text = PedidosRow.Observaciones3

            PonerCorrida()
        End If


        'Show()

        IrAPonerCorrida()

        PoderEliminar = False
        BProgramado = BuscarPedidoProgramado(PedidosRow.PedidosID)

        If BProgramado Then
            Me.MenuItem7.Visible = False
            Me.MenuItem11.Visible = False
            Me.MenuItem20.Visible = False

            'Me.GridEX1.AllowAddNew = InheritableBoolean.True   

            'MsgBox("Este pedido ya ha sido planeado cuando menos una parte y no se podrán modificar algunos renglones !!!")

        End If

        DockPanel2.Visibility = DevExpress.XtraBars.Docking.DockVisibility.Visible


        If GridEX1.RowCount > 0 Then
            AnteriorCorridaID = GrupoID
            Dim Row2 As GridEXRow = GridEX1.GetRow

            PonerCorrida2(BuscaCorrida, Row2.Cells("ProductoID").Value)

            Me.LookCliente.Enabled = False

        End If

        If PedidoSembrado() Then
            Me.LookCliente.Enabled = False
            Me.jsdtFecha.Enabled = False
            Me.jsdtFechaRecepcion.Enabled = False
            Me.jsdtFechaCancelacion.Enabled = False
            Me.ComboVendedor.Enabled = False
            Me.GridEX1.RootTable.Columns("FechaEntrega").EditType = EditType.NoEdit
            Me.GridEX1.RootTable.Columns("FechaEmbarque").EditType = EditType.NoEdit
            Me.jsdtFCancelacionCliente.Enabled = False

        End If
        Me.PicFoto.Visible = True
        If YaAutorizado = False Then
            Me.txtReferencia.Focus()
        End If

        If Negocio = "VITALIA" Then
            Me.ComboBox1.Visible = True
            Me.ComboBox1.Text = Me.txtObservaciones2.Text
        End If
        If PoderCambiarPrecios Then
            Me.GridEX1.RootTable.Columns("Precio").EditType = EditType.TextBox
        Else
            Me.GridEX1.RootTable.Columns("Precio").EditType = EditType.NoEdit
        End If

    End Sub



    Private Sub SetBindings(ByVal row As DataRowView)
        YaAutorizado = False
        If Not IsDBNull(row("PedidosID")) Then
            App.DataManager.FillPedidos_Details(row("PedidosID"), row.Row.Table.DataSet)
            PedidosID = row("PedidosID")
            '            If IsDBNull(row("UsuarioIDAutorizado")) Then
            ' YaAutorizado = False
            '        Else
            '           YaAutorizado = True
            '      End If
            Me.txtReferencia.Focus()
            If row("Status") = "C" Then
                YaAutorizado = True
            End If

        Else
            ' YaAutorizado = False
            PedidosID = Guid.NewGuid
            row("PedidosID") = PedidosID

        End If

        If BuscarPedidoYaConcentrado() Then
            Me.MenuItem7.Visible = False
            Me.MenuItem11.Visible = True
            Me.MenuItem10.Visible = False
        Else
            Me.MenuItem7.Visible = True
            Me.MenuItem11.Visible = False
            Me.MenuItem10.Visible = True
        End If

        If BuscarPedidoYaDesglosado() Then
            Me.MenuItem20.Visible = False
        Else
            Me.MenuItem20.Visible = True
        End If


        Habilita()

        FillUnidad()
        FillPrecio()
        'App.DataManager.fill
        '(Me.dsBase)
        Me.sqlPDD_Temporada_Pedido.Parameters("@EmpresaID").Value = EmpresaID
        Me.daPDD_Temporada_Pedido.Fill(Me.dsBase.PDD_Temporada_Pedido)
        App.DataManager.FillDropDownVendedor(Me.dsDataset3)
        App.DataManager.FillDDTransporte(Me.dsDataset3)
        Me.sqlCliente.Parameters("@GrupoID").Value = GrupoID
        Me.daCliente.Fill(Me.DsPedidoZ21.PClienteList)

        Me.DsPDD_EnvioA1.PDD_EnvioA.Clear()
        Me.daPDD_EnvioA.Fill(Me.DsPDD_EnvioA1.PDD_EnvioA)

        Me.Ds_PDDPaqueteria1.PDD_Paqueteria.Clear()
        Me.SqlDataAdapter2.Fill(Me.Ds_PDDPaqueteria1.PDD_Paqueteria)

        ' Me.txtDiasCredito.DataBindings.Add("Text", row, "DiasCredito")
        '       Me.txtCliNombreCorto.DataBindings.Add("Text", row, "NombreCorto")
        'Me.txtCliNombre.DataBindings.Add("Text", row, "Nombre")
        '        Me.txtCliDireccion.DataBindings.Add("Text", row, "Direccion")
        'Me.txtCliCiudad.DataBindings.Add("Text", row, "Ciudad")
        'Me.txtCliEstado.DataBindings.Add("Text", row, "Estado")
        'Me.txtCliCodPostal.DataBindings.Add("Text", row, "CodPostal")
        'Me.txtCliPais.DataBindings.Add("Text", row, "Pais")

        Me.sqlPDD_CuentaBancaria.Parameters("@GrupoID").Value = GrupoID

        If Negocio <> "CLIFF" Then
            Me.daPDD_CuentaBancaria.Fill(Me.DsNuevo1.PDD_CuentaBancaria)
        End If
        'App.DataManager.FillEmployeeList(Me.dsNorthWind)
        'App.DataManager.FillShippers(Me.dsNorthWind)
        'Me.jsudShipVia.ValueList.PopulateValueList(Me.dsNorthWind.ShippeLector.DefaultView, "ShipperID", "CompanyName", "SmallIcon", Color.Magenta, New Size(16, 16))
        'Me.txtClienteID.DataBindings.Add("Text", row, "ClienteID")
        'Me.txtShipName.DataBindings.Add("Text", row, "ShipName")
        'Me.txtShipAddress.DataBindings.Add("Text", row, "ShipAddress")
        'Me.txtShipCity.DataBindings.Add("Text", row, "ShipCity")
        'Me.txtShipRegion.DataBindings.Add("Text", row, "ShipRegion")
        'Me.txtShipPostalCode.DataBindings.Add("Text", row, "ShipPostalCode")
        'Me.txtShipCountry.DataBindings.Add("Text", row, "ShipCountry")
        'Me.jsmcEmployeeID.DataBindings.Add("Value", row, "EmployeeID")
        Me.txtNumero.DataBindings.Add("Text", row, "Numero")
        Me.txtReferencia.DataBindings.Add("Text", row, "Referencia")

        Me.ComboEnviarDireccion.DataBindings.Add("Value", row, "EnviarDireccion")

        Me.txtObsGenerales.DataBindings.Add("Text", row, "ObsGenerales")
        Me.txtObservaciones2.DataBindings.Add("Text", row, "Observaciones2")
        Me.txtObservaciones3.DataBindings.Add("Text", row, "Observaciones3")


        Me.ComboVendedor.DataBindings.Add("Value", row, "VendedorID")
        Me.ComboTemporada_Pedido.DataBindings.Add("Value", row, "EnviarNombre")
        Me.ComboTransporte.DataBindings.Add("Value", row, "TransporteID")


        'Me.jsudShipVia.DataBindings.Add("Value", row, "ShipVia")
        'Me.jsdtOrderDate.DataBindings.Add("BindableValue", row, "OrderDate")
        'Me.jsdtRequiredDate.DataBindings.Add("BindableValue", row, "RequiredDate")
        'Me.jsdtShippedDate.DataBindings.Add("BindableValue", row, "ShippedDate")
        'Me.nedtFreight.DataBindings.Add("Value", row, "Freight")

        'Me.GridEX1.SetDataBinding(Nothing, "")
        'App.DataManager.FillPedidos_Details(Me.dsDataset1)
        'mData = New DataView(Me.dsDataset1.Pedidos_Details)
        'Me.GridEX1.SetDataBinding(mData, "")

        'If Nuevo Then
        'M() 'e.GridEX1.SetDataBinding(row.CreateChildView("Pedidos_Details"), "")
        'Else
        Me.GridEX1.SetDataBinding(row.CreateChildView("PedidosPedidos_Details"), "")
        'End If
        '
        'Me.GridEX1.SetDataBinding(row("Pedidos_Details"), "")
        'row("Fecha") = Date.Today
        'row("FechaRecepcion") = Date.Today

        Me.jsdtFecha.DataBindings.Add("BindableValue", row, "Fecha")
        Me.jsdtFechaRecepcion.DataBindings.Add("BindableValue", row, "FechaRecepcion")
        Me.jsdtFechaCancelacion.DataBindings.Add("BindableValue", row, "FechaCancelacion")

        If Nuevo Then
            row("FechaRecepcion") = FechaParaPedidos()
            Me.jsdtFechaRecepcion.Value = FechaParaPedidos()
        End If

        Me.jsdtFechaElaboracion.Value = Me.jsdtFecha.Value
        Me.jsdtFCancelacionCliente.Value = Me.jsdtFechaRecepcion.Value
        Me.jsdtFechaFabrica.Value = Me.jsdtFecha.Value


        Me.txtSubTotal.DataBindings.Add("Text", row, "SubTotal")
        Me.txtTotal1.DataBindings.Add("Text", row, "Total")
        Me.txtT21.DataBindings.Add("Text", row, "T2")


        Me.txtImpuestoDinero1.DataBindings.Add("Text", row, "ImpuestoDinero")
        Me.txtDescuentoDinero.DataBindings.Add("Text", row, "DescuentoDinero")
        Me.txtCantidad.DataBindings.Add("Text", row, "Cantidad")

        Me.txtDeposito.DataBindings.Add("Text", row, "Deposito")


        Me.CalculateTotals()


        If PoderCambiarPrecios Then
            Me.GridEX1.RootTable.Columns("Precio").EditType = EditType.TextBox
        Else
            Me.GridEX1.RootTable.Columns("Precio").EditType = EditType.NoEdit
        End If

        Configuración()

        Me.Label5.Visible = BVer
        Me.Label7.Visible = BVer
        Me.ComboTemporada_Pedido.Visible = BVer
        Me.ComboEnviarDireccion.Visible = BVer



        ConfigurarFases()
        txtReferencia.Focus()

        PoderEliminar = False

        Me.jsdtFecha.Enabled = Me.BPoderModificarFechaDelPedido

        Me.daPDD_Corrida_Atado.Fill(Me.Guava_Data1.Corrida_Atado)
        Me.GridEX1.DropDowns("Corrida_Atado").SetDataBinding(Me.Guava_Data1, "Corrida_Atado")

        FillPDD_EnviarDireccion(Me.dsBase)


        Configuracion()

        If YaAutorizado = False Then
            Me.txtReferencia.Focus()
        End If

        Me.CalculateTotals()


        Pedidos_Busca()

        If Nuevo = False Then
            Me.LookCliente.EditValue = row("ClienteID")
            Restaurar_ClienteCadena(Me.LookCliente.EditValue)
            Restaurar_ClienteCadena_Enviar(Me.LookCliente.EditValue)

            FillCatalogos(Me.LookCliente.EditValue)
        End If

        Me.DsPDD_AlmacenesPorUsuario1.PDD_Almacenes_PorUsuario.Clear()
        Me.sqlPDD_AlmacenesPorUsuario.Parameters("@EmpresaID").Value = EmpresaID
        Me.sqlPDD_AlmacenesPorUsuario.Parameters("@UsuarioID").Value = UsuarioID

        Me.daPDD_AlmacenesPorUsuario.Fill(Me.DsPDD_AlmacenesPorUsuario1.PDD_Almacenes_PorUsuario)



    End Sub
    Private Sub ClearBindings()
        Me.LabelDiasCredito.Text = 0
        '     Me.txtCliNombreCorto.DataBindings.Clear()
        '     Me.txtCliNombre.DataBindings.Clear()
        '     Me.txtCliDireccion.DataBindings.Clear()
        '     Me.txtCliCiudad.DataBindings.Clear()
        '     Me.txtCliEstado.DataBindings.Clear()
        '     Me.txtCliCodPostal.DataBindings.Clear()
        '     Me.txtCliPais.DataBindings.Clear()

        '    Me.txtEnviarDireccion.DataBindings.Clear()
        '    Me.txtEnviarCiudad.DataBindings.Clear()
        '    Me.txtEnviarEstado.DataBindings.Clear()
        '    Me.txtEnviarCodPostal.DataBindings.Clear()
        '    Me.txtEnviarPais.DataBindings.Clear()

        'Me.txtClienteID.DataBindings.Clear()
        'Me.txtShipName.DataBindings.Clear()
        'Me.txtShipAddress.DataBindings.Clear()
        'Me.txtShipCity.DataBindings.Clear()
        'Me.txtShipRegion.DataBindings.Clear()
        'Me.txtShipPostalCode.DataBindings.Clear()
        'Me.txtShipCountry.DataBindings.Clear()

        Me.ComboVendedor.DataBindings.Clear()
        Me.ComboTemporada_Pedido.DataBindings.Clear()
        Me.ComboTransporte.DataBindings.Clear()
        Me.jsdtFecha.DataBindings.Clear()
        Me.jsdtFecha.DataBindings.Clear()
        Me.txtSubTotal.DataBindings.Clear()
        Me.txtImpuestoDinero1.DataBindings.Clear()
        Me.txtTotal1.DataBindings.Clear()
        Me.txtDescuentoDinero.Text = 0
        Me.txtT21.Text = "$0.00"
        Me.ComboEnviarDireccion.DataBindings.Clear()

        'Me.jsudShipVia.DataBindings.Clear()
        'Me.jsdtOrderDate.DataBindings.Clear()
        'Me.jsdtRequiredDate.DataBindings.Clear()
        'Me.jsdtShippedDate.DataBindings.Clear()
        'Me.nedtFreight.DataBindings.Clear()
        Me.GridEX1.SetDataBinding(Nothing, "")

    End Sub







    Private Sub btnDelete_Click(ByVal sender As System.Object, ByVal e As System.EventArgs)
        If MessageBox.Show("¿Estas seguro de querer borrar este documento?", App.MessageCaption, MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) = DialogResult.Yes Then
            Try
                'If TypeOf MainForm.ActiveView Is PedidosView Then
                '    mDataRow.CancelEdit()
                '    MainForm.ActiveView.Delete(mDataRow("PedidosID"))
                'Else
                mDataRow.Delete()
                If Not App.DataManager.UpdatePedidos(mDataRow) Then
                    Exit Sub
                End If
                'End If
                mDataRow = Nothing
                Close()
            Catch exc As Exception
                MessageBox.Show(exc.Message, App.MessageCaption, MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
            End Try
        End If
    End Sub

    Private Sub btnCancel_Click(ByVal sender As System.Object, ByVal e As System.EventArgs)
        mDataRow.CancelEdit()
        CType(mDataRow.Row.Table.DataSet, Base).Pedidos_Details.RejectChanges()
        Close()

    End Sub

    Private Sub btnUpdate_Click(ByVal sender As System.Object, ByVal e As System.EventArgs)
        If UpdateChanges() Then
            Close()
        End If
    End Sub
    Private Function UpdateChanges() As Boolean
        'Try
        Dim V As clsValidar
        V = New clsValidar

        If GridEX1.RowCount > 0 Then
            Me.LookCliente.Enabled = False
        Else
            Me.LookCliente.Enabled = True
        End If



        If Not mDataRow Is Nothing Then
            If mDataRow.IsNew Then

                mDataRow("Numero") = SiguienteNumero()
                mDataRow("Referencia") = mDataRow("Numero")



                mDataRow("Hora") = Format(Date.Today, "hh:mm")
                mDataRow("PedidosID") = PedidosID
                mDataRow("EmpresaID") = EmpresaID
                mDataRow("DocumentoID") = DocumentoID
                mDataRow("Status") = "A"
                mDataRow("UsuarioID") = UsuarioID
                Me.txtReferencia.Focus()
            Else
                mDataRow("Referencia") = txtReferencia.Text




            End If
            mDataRow("EnviarDireccion") = Me.ComboEnviarDireccion.Text
            mDataRow("ObsGenerales") = txtObsGenerales.Text


            If Me.ComboBox1.Visible Then
                Me.txtObservaciones2.Text = ComboBox1.Text
                mDataRow("Observaciones2") = Me.txtObservaciones2.Text
            Else
                mDataRow("Observaciones2") = Me.txtObservaciones2.Text

            End If



            mDataRow("Observaciones3") = Me.txtObservaciones3.Text

            mDataRow("Impuesto") = 0
            mDataRow("Descuento") = 0
            mDataRow("DescuentoFinanciero") = 0
            If IsNumeric(txtSubTotal.Text) = False Then
                txtSubTotal.Text = 0
            End If
            mDataRow("SubTotal") = CSng(txtSubTotal.Text)
            If IsNumeric(txtDescuentoDinero.Text) = False Then
                txtDescuentoDinero.Text = 0
            End If
            If IsNumeric(Me.txtDeposito.Text) = False Then
                Me.txtDeposito.Text = 0
            End If
            mDataRow("Deposito") = Me.txtDeposito.Text
            mDataRow("DescuentoDinero") = CSng(txtDescuentoDinero.Text)
            mDataRow("DescuentoFinancieroDinero") = 0
            If IsNumeric(txtImpuestoDinero1.Text) = False Then
                txtImpuestoDinero1.Text = 0
            End If
            If IsDBNull(Me.LookCliente.EditValue) = False And IsNothing(Me.LookCliente.EditValue) = False Then
                mDataRow("ClienteID") = LookCliente.EditValue

            End If
            mDataRow("ImpuestoDinero") = CSng(txtImpuestoDinero1.Text)
            If IsNumeric(txtTotal1.Text) = False Then
                txtTotal1.Text = 0
            End If
            mDataRow("Total") = CSng(txtTotal1.Text)
            mDataRow("Indirectos") = 0


            If IsNumeric(txtCantidad.Text) = False Then
                txtCantidad.Text = 0
            End If
            mDataRow("Cantidad") = CDbl(txtCantidad.Text)
            mDataRow("Piezas") = 0
            mDataRow("Cargo") = 0
            mDataRow("Abono") = 0
            mDataRow("Saldo") = 0
            mDataRow("DiasCredito") = 0
            If IsNumeric(txtT21.Text) = False Then
                txtT21.Text = "$0.00"
            End If
            mDataRow("T2") = CSng(txtT21.Text)
            mDataRow("Deposito") = V.IniDouble(Me.txtDeposito.Text)
            mDataRow("Efectivo") = 0
            mDataRow("Monedero") = 0
            mDataRow("Cambio") = 0
            mDataRow("ImporteCheque") = 0
            mDataRow("TCredito") = 0
            mDataRow("Dev") = 0
            mDataRow("DevDinero") = 0
            mDataRow("Nota") = 0
            mDataRow("Receta") = 0
            mDataRow("Siguiente") = 0
            mDataRow("Recibidos") = 0
            mDataRow("Comision") = 0
            mDataRow("FormaPago") = "E"
            mDataRow("EnviarCodPostal") = 0
            mDataRow("TipoDeCambio") = 0

            If IsDBNull(mDataRow("Fecha")) Then
                mDataRow("Fecha") = Date.Today
            End If

            If IsDBNull(mDataRow("FechaRecepcion")) Then
                mDataRow("FechaRecepcion") = Date.Today
            End If
            If IsDBNull(mDataRow("FechaCancelacion")) Then
                mDataRow("FechaCancelacion") = Date.Today
            End If



            If IsDBNull(mDataRow("FechaCaptura")) Then
                mDataRow("FechaCaptura") = Date.Today
            End If
            If IsDBNull(mDataRow("Concentrado")) Then
                mDataRow("Concentrado") = "N"
            End If
            If IsDBNull(mDataRow("Surtir")) Then
                mDataRow("Surtir") = 0
            End If
            If IsDBNull(mDataRow("Existencia")) Then
                mDataRow("Existencia") = 0
            End If
            If IsDBNull(mDataRow("Faltante")) Then
                mDataRow("Faltante") = 0
            End If
            If IsDBNull(mDataRow("Entregado")) Then
                mDataRow("Entregado") = "N"
            End If
            If IsDBNull(mDataRow("EntregadoTotalmente")) Then
                mDataRow("EntregadoTotalmente") = "N"
            End If

            If Not Me.GridEX1.UpdateData() Then
                ' Me.GridEX1.Focus()
                Return False
            End If

            mDataRow.EndEdit()
            If Not App.DataManager.UpdatePedidos(mDataRow) Then
                Return False
            End If
        End If
        CalculateTotals()
        Return True
        'Catch exc As Exception
        '    MessageBox.Show(exc.Message, App.MessageCaption, MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
        '    Return False
        'End Try
    End Function
    ' Private Sub btnSelectCliente_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnSelectCliente.Click
    'Dim ClienteDialog As New frmSelectCliente()
    'Dim Cliente As NorthWind.ClientesRow
    'Dim prevValue As String = Me.txtClienteID.Text
    'Dim fillShipInfo As Boolean = False
    'Dim ClienteID As String = ""
    'If Not IsDBNull(mDataRow("ClienteID")) Then
    '    ClienteID = mDataRow("ClienteID")
    'End If
    'Cliente = ClienteDialog.SelectCliente(ClienteID)

    'If Not Cliente Is Nothing Then
    '    If Cliente.ClienteID <> prevValue Then
    '        fillShipInfo = True
    '    End If
    'End If
    'FillClienteData(Cliente, fillShipInfo)

    'End Sub
    Private Sub FillClienteData(ByVal Cliente As Dataset1.ClienteRow, ByVal fillShipInfo As Boolean)
        If Not Cliente Is Nothing Then
            'Me.txtCliNombre.Text = Cliente.Nombre
            'If Not Cliente.IsDireccionNull Then
            '    Me.txtCliDireccion.Text = Cliente.Direccion
            'Else
            '    Me.txtCliDireccion.Text = ""
            'End If
            '        If Not Cliente.IsAddressNull Then
            '            Me.txtCustAddress.Text = Cliente.Address
            '        Else
            '            Me.txtCustAddress.Text = ""
            '        End If
            '        If Not Cliente.IsCityNull Then
            '            Me.txtCustCity.Text = Cliente.City
            '        Else
            '            Me.txtCustCity.Text = ""
            '        End If
            '        If Not Cliente.IsCompanyNameNull Then
            '            Me.txtCustCompanyName.Text = Cliente.CompanyName
            '        Else
            '            Me.txtCustCompanyName.Text = ""
            '        End If
            '        If Not Cliente.IsCountryNull Then
            '            Me.txtCustCountry.Text = Cliente.Country
            '        Else
            '            Me.txtCustCountry.Text = ""
            '        End If
            '        If Not Cliente.IsPostalCodeNull Then
            '            Me.txtCustPostalCode.Text = Cliente.PostalCode
            '        Else
            '            Me.txtCustPostalCode.Text = ""
            '        End If
            '        If Not Cliente.Is_RegionNull Then
            '            Me.txtCustRegion.Text = Cliente._Region
            '        Else
            '            Me.txtCustRegion.Text = ""
            '        End If
            '        If fillShipInfo Then
            '            Me.BindingContext(mDataRow).SuspendBinding()
            '            Dim order As NorthWind.OrdersRow
            '            order = CType(mDataRow.Row, NorthWind.OrdersRow)
            '            Dim ClienteView As DataRowView
            '            order("ClienteID") = Cliente("ClienteID")
            '            order("ShipAddress") = Cliente("Address")
            '            order("ShipCity") = Cliente("City")
            '            order("ShipCountry") = Cliente("Country")
            '            order("ShipName") = Cliente("CompanyName")
            '            order("ShipRegion") = Cliente("Region")
            '            order("ShipPostalCode") = Cliente("PostalCode")
            '            Me.BindingContext(mDataRow).ResumeBinding()
            '        End If

        End If
    End Sub


    'Private Sub txtClienteID_Enter(ByVal sender As Object, ByVal e As System.EventArgs) Handles txtClienteID.Enter
    '    txtClienteID.Tag = txtClienteID.Text
    'End Sub


    'Private Sub btnShipppers_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnShipppeLector.Click
    '    Dim frm As frmShippers
    '    frm = New frmShippers()
    '    frm.Data = New DataView(Me.dsNorthWind.Shippers)
    '    If frm.ShowDialog() = DialogResult.OK Then
    '        Me.jsudShipVia.ValueList.Clear()
    '        Me.jsudShipVia.ValueList.PopulateValueList(Me.dsNorthWind.ShippeLector.DefaultView, "ShipperID", "CompanyName", "SmallIcon", Color.Magenta, New Size(16, 16))
    '    End If

    'End Sub
    Private Function ValidateDetailRow() As Boolean
        Dim currentRow As GridEXRow = GridEX1.GetRow()


        If IsDBNull(currentRow.Cells("ProductoID").Value) Then
            currentRow.Cells("ProductoID").Value = DBNull.Value
            GridEX1.SetValue("ProductoID", DBNull.Value)
            '    MessageBox.Show("Producto no puede ser vacio.")
            '    Me.GridEX1.CurrentColumn = Me.GridEX1.RootTable.Columns("ProductoID")
            '    Me.GridEX1.Focus()
            '    Return False
        End If





        'If IsDBNull(currentRow.Cells("Cantidad").Value) Then
        '    MessageBox.Show("Introduzca la cantidad.")
        '    Me.GridEx1.CurrentColumn = Me.GridEx1.RootTable.Columns("P1")
        '    Me.GridEx1.Focus()
        '    Return False
        'End If
        'If IsDBNull(currentRow.Cells("UnitPrice").Value) Then
        '    MessageBox.Show("Enter the Unit Price.")
        '    Me.GridEx1.CurrentColumn = Me.GridEx1.RootTable.Columns("UnitPrice")
        '    Me.GridEx1.Focus()
        '    Return False
        'End If
        'If IsDBNull(currentRow.Cells("Discount").Value) Then
        '    Me.GridEx1.SetValue("Discount", 0)
        '    Return False
        'End If
        Return True
    End Function




    'Private Sub nedtFreight_Validated(ByVal sender As Object, ByVal e As System.EventArgs) Handles nedtFreight.Validated
    '    Me.CalculateTotals()
    'End Sub



    Protected Overrides Sub OnClosing(ByVal e As System.ComponentModel.CancelEventArgs)
        'If Not mDataRow Is Nothing Then
        '    Dim hasChanges As Boolean = (mDataRow.Row.RowState <> DataRowState.Unchanged)
        '    If Not hasChanges Then
        '        'check if there are changes in order details
        '        Dim ds As Base = CType(mDataRow.DataView.Table.DataSet, Base)
        '        hasChanges = (Not ds.Pedidos_Details.GetChanges() Is Nothing)
        '    End If
        '    If hasChanges Then
        '        Dim result As DialogResult = MessageBox.Show("¿Guardar los cambios?", "Expoert Shoes", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1)
        '        Select Case result
        '            Case DialogResult.Yes
        '                If Not UpdateChanges() Then
        '                    e.Cancel = True
        '                End If
        '            Case DialogResult.No
        '                mDataRow.CancelEdit()
        '            Case DialogResult.Cancel
        '                e.Cancel = True
        '        End Select
        '    End If
        'End If
        MyBase.OnClosing(e)
    End Sub
    Protected Overrides Sub OnClosed(ByVal e As System.EventArgs)
        ClearBindings()
        MyBase.OnClosed(e)
    End Sub




    Private Sub FillUnidad()
        Me.SqlSelectCommand3.Parameters("@GrupoID").Value = GrupoID
        daUnidad.Fill(dsBase.PDropDownUnidad)
        GridEX1.DropDowns("Unidad").SetDataBinding(dsBase, "PDropDownUnidad")

    End Sub


    Private Sub GridEX1_CellUpdated(ByVal sender As System.Object, ByVal e As Janus.Windows.GridEX.ColumnActionEventArgs) Handles GridEX1.CellUpdated
        Dim row As GridEXRow = GridEX1.GetRow


        If IsDBNull(row.Cells("FechaEntrega").Value) Or IsNothing(row.Cells("FechaEntrega").Value) Then
            GridEX1.SetValue("FechaEntrega", Me.jsdtFechaRecepcion.Value)
            GridEX1.SetValue("FechaEmbarque", Me.jsdtFechaRecepcion.Value)
        End If

        Select Case e.Column.Key

            Case "Corrida_AtadoID"

                Dim Row10 As GridEXRow = Me.GridEX1.DropDowns("Corrida_Atado").GetRow

                Me.GridEX1.SetValue("P1", Row10.Cells("P1").Value)
                Me.GridEX1.SetValue("P2", Row10.Cells("P2").Value)
                Me.GridEX1.SetValue("P3", Row10.Cells("P3").Value)
                Me.GridEX1.SetValue("P4", Row10.Cells("P4").Value)
                Me.GridEX1.SetValue("P5", Row10.Cells("P5").Value)
                Me.GridEX1.SetValue("P6", Row10.Cells("P6").Value)
                Me.GridEX1.SetValue("P7", Row10.Cells("P7").Value)
                Me.GridEX1.SetValue("P8", Row10.Cells("P8").Value)
                Me.GridEX1.SetValue("P9", Row10.Cells("P9").Value)
                Me.GridEX1.SetValue("P10", Row10.Cells("P10").Value)

                Me.GridEX1.SetValue("P11", Row10.Cells("P11").Value)
                Me.GridEX1.SetValue("P12", Row10.Cells("P12").Value)
                Me.GridEX1.SetValue("P13", Row10.Cells("P13").Value)
                Me.GridEX1.SetValue("P14", Row10.Cells("P14").Value)
                Me.GridEX1.SetValue("P15", Row10.Cells("P15").Value)
                Me.GridEX1.SetValue("P16", Row10.Cells("P16").Value)
                Me.GridEX1.SetValue("P17", Row10.Cells("P17").Value)
                Me.GridEX1.SetValue("P18", Row10.Cells("P18").Value)
                Me.GridEX1.SetValue("P19", Row10.Cells("P19").Value)
                Me.GridEX1.SetValue("P20", Row10.Cells("P20").Value)

                Me.GridEX1.SetValue("P21", Row10.Cells("P21").Value)
                Me.GridEX1.SetValue("P22", Row10.Cells("P22").Value)
                Me.GridEX1.SetValue("P23", Row10.Cells("P23").Value)
                Me.GridEX1.SetValue("P24", Row10.Cells("P24").Value)
                Me.GridEX1.SetValue("P25", Row10.Cells("P25").Value)
                Me.GridEX1.SetValue("P26", Row10.Cells("P26").Value)
                Me.GridEX1.SetValue("P27", Row10.Cells("P27").Value)
                Me.GridEX1.SetValue("P28", Row10.Cells("P28").Value)
                Me.GridEX1.SetValue("P29", Row10.Cells("P29").Value)
                Me.GridEX1.SetValue("P30", Row10.Cells("P30").Value)
                CalculateDetailTotal()
            Case "P1", "P2", "P3", "P4", "P5", "P6", "P7", "P8", "P9", "P10", "P11", "P12", "P13", "P14", "P15", "P16", "P17", "P18", "P19", "P20", "P21", "P22", "P23", "P24", "P25", "P26", "P27", "P28", "P29", "P30", "Precio", "PrecioCIva", "Descuento", "IVA"

                If IsNumeric(row.Cells(e.Column.Key).Value) = False Then
                    row.Cells(e.Column.Key).Value = 0
                    GridEX1.SetValue(e.Column.Key, 0)
                End If
                GridEX1.SetValue("Importe", (row.Cells("P1").Value + row.Cells("P2").Value + row.Cells("P3").Value + row.Cells("P4").Value + row.Cells("P5").Value + row.Cells("P6").Value + row.Cells("P7").Value + row.Cells("P8").Value + row.Cells("P9").Value + row.Cells("P10").Value + row.Cells("P11").Value + row.Cells("P12").Value + row.Cells("P13").Value + row.Cells("P14").Value + row.Cells("P15").Value + row.Cells("P16").Value + row.Cells("P17").Value + row.Cells("P18").Value + row.Cells("P19").Value + row.Cells("P20").Value + row.Cells("P21").Value + row.Cells("P22").Value + row.Cells("P23").Value + row.Cells("P24").Value + row.Cells("P25").Value + row.Cells("P26").Value + row.Cells("P27").Value + row.Cells("P28").Value + row.Cells("P29").Value + row.Cells("P30").Value) * row.Cells("Precio").Value)
                GridEX1.SetValue("Cantidad", (row.Cells("P1").Value + row.Cells("P2").Value + row.Cells("P3").Value + row.Cells("P4").Value + row.Cells("P5").Value + row.Cells("P6").Value + row.Cells("P7").Value + row.Cells("P8").Value + row.Cells("P9").Value + row.Cells("P10").Value + row.Cells("P11").Value + row.Cells("P12").Value + row.Cells("P13").Value + row.Cells("P14").Value + row.Cells("P15").Value + row.Cells("P16").Value + row.Cells("P17").Value + row.Cells("P18").Value + row.Cells("P19").Value + row.Cells("P20").Value + row.Cells("P21").Value + row.Cells("P22").Value + row.Cells("P23").Value + row.Cells("P24").Value + row.Cells("P25").Value + row.Cells("P26").Value + row.Cells("P27").Value + row.Cells("P28").Value + row.Cells("P29").Value + row.Cells("P30").Value))



            Case "ProductoID"
                Me.GridEX1.SetValue("P1", 0)
                Me.GridEX1.SetValue("P2", 0)
                Me.GridEX1.SetValue("P3", 0)
                Me.GridEX1.SetValue("P4", 0)
                Me.GridEX1.SetValue("P5", 0)
                Me.GridEX1.SetValue("P6", 0)
                Me.GridEX1.SetValue("P7", 0)
                Me.GridEX1.SetValue("P8", 0)
                Me.GridEX1.SetValue("P9", 0)
                Me.GridEX1.SetValue("P10", 0)
                Me.GridEX1.SetValue("P11", 0)
                Me.GridEX1.SetValue("P12", 0)
                Me.GridEX1.SetValue("P13", 0)
                Me.GridEX1.SetValue("P14", 0)
                Me.GridEX1.SetValue("P15", 0)
                Me.GridEX1.SetValue("P16", 0)
                Me.GridEX1.SetValue("P17", 0)
                Me.GridEX1.SetValue("P18", 0)
                Me.GridEX1.SetValue("P19", 0)
                Me.GridEX1.SetValue("P20", 0)
                Me.GridEX1.SetValue("P21", 0)
                Me.GridEX1.SetValue("P22", 0)
                Me.GridEX1.SetValue("P23", 0)
                Me.GridEX1.SetValue("P24", 0)
                Me.GridEX1.SetValue("P25", 0)
                Me.GridEX1.SetValue("P26", 0)
                Me.GridEX1.SetValue("P27", 0)
                Me.GridEX1.SetValue("P28", 0)
                Me.GridEX1.SetValue("P29", 0)
                Me.GridEX1.SetValue("P30", 0)

                PonerProducto()
                Me.BuscaProducto()
                BuscaObs1()

                CalculateDetailTotal()


            Case "Codigo"
                VerFoto()
                Me.BuscaProducto()
                BuscaObs1()
                'PonerProducto()
                CalculateDetailTotal()
            Case "FechaEntrega"
                If row.Cells(e.Column.Key).Value < Me.jsdtFecha.Value Then
                    MsgBox("La fecha de entrega no puede ser menor a la fecha del pedido !!!")
                    GridEX1.SetValue("FechaEntrega", Me.jsdtFechaRecepcion.Value)
                    Me.GridEX1.CurrentColumn = Me.GridEX1.RootTable.Columns("FechaEntrega")
                End If
            Case "FechaEmbarque"
                If row.Cells(e.Column.Key).Value < Me.jsdtFecha.Value Then
                    MsgBox("La fecha de embarque no puede ser menor a la fecha del pedido !!!")
                    GridEX1.SetValue("FechaEmbarque", Me.jsdtFechaRecepcion.Value)

                    Me.GridEX1.CurrentColumn = Me.GridEX1.RootTable.Columns("FechaEmbarque")
                End If
                If Me.BManejoDeComercializadora Then
                    If row.Cells(e.Column.Key).Value <= Me.FSembrado Then
                        MsgBox("La fecha de embarque debe ser mayor a la fecha del último pedido sembrado del " + CStr(FSembrado) + " !!!")
                        'GridEX1.SetValue("FechaEmbarque", DateAdd(DateInterval.Day, 1, FSembrado))
                        GridEX1.SetValue("FechaEmbarque", Me.jsdtFechaRecepcion.Value)
                    End If
                End If
            Case "ClienteCadenaID"
                GridEX1.SetValue("ClienteCadenaIDEnviar", row.Cells("ClienteCadenaID").Value)

        End Select


    End Sub



    Private Sub PonerCorrida2(ByVal CorridaID As Guid, ByVal ProductoID As Guid)
        Dim v As clsValidar

        Dim Comando As SqlClient.SqlCommand
        Dim Lector As SqlClient.SqlDataReader
        Dim Posicion1 As Boolean
        Dim Posicion2 As Boolean
        Dim Posicion3 As Boolean
        Dim Posicion4 As Boolean
        Dim Posicion5 As Boolean
        Dim Posicion6 As Boolean
        Dim Posicion7 As Boolean
        Dim Posicion8 As Boolean
        Dim Posicion9 As Boolean
        Dim Posicion10 As Boolean
        Dim Posicion11 As Boolean
        Dim Posicion12 As Boolean
        Dim Posicion13 As Boolean
        Dim Posicion14 As Boolean
        Dim Posicion15 As Boolean
        Dim Posicion16 As Boolean
        Dim Posicion17 As Boolean
        Dim Posicion18 As Boolean
        Dim Posicion19 As Boolean
        Dim Posicion20 As Boolean
        Dim Posicion21 As Boolean
        Dim Posicion22 As Boolean
        Dim Posicion23 As Boolean
        Dim Posicion24 As Boolean
        Dim Posicion25 As Boolean
        Dim Posicion26 As Boolean
        Dim Posicion27 As Boolean
        Dim Posicion28 As Boolean
        Dim Posicion29 As Boolean
        Dim Posicion30 As Boolean



        Dim cmd As SqlClient.SqlCommand
        Dim rs As SqlClient.SqlDataReader
        Dim cnn2 As New SqlClient.SqlConnection
        cnn2.ConnectionString = Scnn
        cnn2.Open()


        cmd = New SqlClient.SqlCommand("PProducto_NoAplica_Posicion", cnn2)
        cmd.CommandType = CommandType.StoredProcedure
        cmd.Parameters.Add(New System.Data.SqlClient.SqlParameter("@ProductoID", System.Data.SqlDbType.UniqueIdentifier, 16, "ProductoID"))
        cmd.Parameters("@ProductoID").Value = ProductoID

        Posicion1 = False
        Posicion2 = False
        Posicion3 = False
        Posicion4 = False
        Posicion5 = False
        Posicion6 = False
        Posicion7 = False
        Posicion8 = False
        Posicion9 = False
        Posicion10 = False
        Posicion11 = False
        Posicion12 = False
        Posicion13 = False
        Posicion14 = False
        Posicion15 = False
        Posicion16 = False
        Posicion17 = False
        Posicion18 = False
        Posicion19 = False
        Posicion20 = False
        Posicion21 = False
        Posicion22 = False
        Posicion23 = False
        Posicion24 = False
        Posicion25 = False
        Posicion26 = False
        Posicion27 = False
        Posicion28 = False
        Posicion29 = False
        Posicion30 = False

        rs = cmd.ExecuteReader()
        Do While rs.Read
            Posicion1 = rs!Posicion1
            Posicion2 = rs!Posicion2
            Posicion3 = rs!Posicion3
            Posicion4 = rs!Posicion4
            Posicion5 = rs!Posicion5
            Posicion6 = rs!Posicion6
            Posicion7 = rs!Posicion7
            Posicion8 = rs!Posicion8
            Posicion9 = rs!Posicion9
            Posicion10 = rs!Posicion10
            Posicion11 = rs!Posicion11
            Posicion12 = rs!Posicion12
            Posicion13 = rs!Posicion13
            Posicion14 = rs!Posicion14
            Posicion15 = rs!Posicion15
            Posicion16 = rs!Posicion16
            Posicion17 = rs!Posicion17
            Posicion18 = rs!Posicion18
            Posicion19 = rs!Posicion19
            Posicion20 = rs!Posicion20
            Posicion21 = rs!Posicion21
            Posicion22 = rs!Posicion22
            Posicion23 = rs!Posicion23
            Posicion24 = rs!Posicion24
            Posicion25 = rs!Posicion25
            Posicion26 = rs!Posicion26
            Posicion27 = rs!Posicion27
            Posicion28 = rs!Posicion28
            Posicion29 = rs!Posicion29
            Posicion30 = rs!Posicion30

        Loop
        rs.Close()
        cmd = Nothing

        cnn2.Close()




        AnteriorCorridaID = CorridaID
        InicializarTallas()




        cnn2.ConnectionString = Scnn
        cnn2.Open()


        v = New clsValidar

        If Corridas Then


            If IsDBNull(CorridaID) Then

                Exit Sub
            End If
            Comando = New SqlClient.SqlCommand("PPonerCorrida2", cnn2)
            Comando.CommandType = CommandType.StoredProcedure
            Comando.CommandTimeout = 300
            Comando.Parameters.Add(New System.Data.SqlClient.SqlParameter("@CorridaID", System.Data.SqlDbType.UniqueIdentifier, 16, "CorridaID"))
            Comando.Parameters("@CorridaID").Value = CorridaID

            Lector = Comando.ExecuteReader()
            Do While Lector.Read
                If IsDBNull(Lector!M1) = False And Posicion1 = False Then
                    GridEX1.RootTable.Columns("P1").Visible = True
                    GridEX1.RootTable.Columns("P1").Caption = Lector!M1
                Else
                    GridEX1.RootTable.Columns("P1").Visible = False
                End If
                If IsDBNull(Lector!M2) = False And Posicion2 = False Then
                    GridEX1.RootTable.Columns("P2").Visible = True
                    GridEX1.RootTable.Columns("P2").Caption = Lector!M2
                Else
                    GridEX1.RootTable.Columns("P2").Visible = False
                End If
                If IsDBNull(Lector!M3) = False And Posicion3 = False Then
                    GridEX1.RootTable.Columns("P3").Visible = True
                    GridEX1.RootTable.Columns("P3").Caption = Lector!M3
                Else
                    GridEX1.RootTable.Columns("P3").Visible = False
                End If
                If IsDBNull(Lector!M4) = False And Posicion4 = False Then
                    GridEX1.RootTable.Columns("P4").Visible = True
                    GridEX1.RootTable.Columns("P4").Caption = Lector!M4
                Else
                    GridEX1.RootTable.Columns("P4").Visible = False
                End If
                If IsDBNull(Lector!M5) = False And Posicion5 = False Then
                    GridEX1.RootTable.Columns("P5").Visible = True
                    GridEX1.RootTable.Columns("P5").Caption = Lector!M5
                Else
                    GridEX1.RootTable.Columns("P5").Visible = False
                End If
                If IsDBNull(Lector!M6) = False And Posicion6 = False Then
                    GridEX1.RootTable.Columns("P6").Visible = True
                    GridEX1.RootTable.Columns("P6").Caption = Lector!M6
                Else
                    GridEX1.RootTable.Columns("P6").Visible = False
                End If
                If IsDBNull(Lector!M7) = False And Posicion7 = False Then
                    GridEX1.RootTable.Columns("P7").Visible = True
                    GridEX1.RootTable.Columns("P7").Caption = Lector!M7
                Else
                    GridEX1.RootTable.Columns("P7").Visible = False
                End If
                If IsDBNull(Lector!M8) = False And Posicion8 = False Then
                    GridEX1.RootTable.Columns("P8").Visible = True
                    GridEX1.RootTable.Columns("P8").Caption = Lector!M8
                Else
                    GridEX1.RootTable.Columns("P8").Visible = False
                End If
                If IsDBNull(Lector!M9) = False And Posicion9 = False Then
                    GridEX1.RootTable.Columns("P9").Visible = True
                    GridEX1.RootTable.Columns("P9").Caption = Lector!M9
                Else
                    GridEX1.RootTable.Columns("P9").Visible = False
                End If
                If IsDBNull(Lector!M10) = False And Posicion10 = False Then
                    GridEX1.RootTable.Columns("P10").Visible = True
                    GridEX1.RootTable.Columns("P10").Caption = Lector!M10
                Else
                    GridEX1.RootTable.Columns("P10").Visible = False
                End If
                If IsDBNull(Lector!M11) = False And Posicion11 = False Then
                    GridEX1.RootTable.Columns("P11").Visible = True
                    GridEX1.RootTable.Columns("P11").Caption = Lector!M11
                Else
                    GridEX1.RootTable.Columns("P11").Visible = False
                End If
                If IsDBNull(Lector!M12) = False And Posicion12 = False Then
                    GridEX1.RootTable.Columns("P12").Visible = True
                    GridEX1.RootTable.Columns("P12").Caption = Lector!M12
                Else
                    GridEX1.RootTable.Columns("P12").Visible = False
                End If
                If IsDBNull(Lector!M13) = False And Posicion13 = False Then
                    GridEX1.RootTable.Columns("P13").Visible = True
                    GridEX1.RootTable.Columns("P13").Caption = Lector!M13
                Else
                    GridEX1.RootTable.Columns("P13").Visible = False
                End If
                If IsDBNull(Lector!M14) = False And Posicion14 = False Then
                    GridEX1.RootTable.Columns("P14").Visible = True
                    GridEX1.RootTable.Columns("P14").Caption = Lector!M14
                Else
                    GridEX1.RootTable.Columns("P14").Visible = False
                End If
                If IsDBNull(Lector!M15) = False And Posicion15 = False Then
                    GridEX1.RootTable.Columns("P15").Visible = True
                    GridEX1.RootTable.Columns("P15").Caption = Lector!M15
                Else
                    GridEX1.RootTable.Columns("P15").Visible = False
                End If
                If IsDBNull(Lector!M16) = False And Posicion16 = False Then
                    GridEX1.RootTable.Columns("P16").Visible = True
                    GridEX1.RootTable.Columns("P16").Caption = Lector!M16
                Else
                    GridEX1.RootTable.Columns("P16").Visible = False
                End If
                If IsDBNull(Lector!M17) = False And Posicion17 = False Then
                    GridEX1.RootTable.Columns("P17").Visible = True
                    GridEX1.RootTable.Columns("P17").Caption = Lector!M17
                Else
                    GridEX1.RootTable.Columns("P17").Visible = False
                End If
                If IsDBNull(Lector!M18) = False And Posicion18 = False Then
                    GridEX1.RootTable.Columns("P18").Visible = True
                    GridEX1.RootTable.Columns("P18").Caption = Lector!M18
                Else
                    GridEX1.RootTable.Columns("P18").Visible = False
                End If
                If IsDBNull(Lector!M19) = False And Posicion19 = False Then
                    GridEX1.RootTable.Columns("P19").Visible = True
                    GridEX1.RootTable.Columns("P19").Caption = Lector!M19
                Else
                    GridEX1.RootTable.Columns("P19").Visible = False
                End If
                If IsDBNull(Lector!M20) = False And Posicion20 = False Then
                    GridEX1.RootTable.Columns("P20").Visible = True
                    GridEX1.RootTable.Columns("P20").Caption = Lector!M20
                Else
                    GridEX1.RootTable.Columns("P20").Visible = False
                End If
                If IsDBNull(Lector!M21) = False And Posicion21 = False Then
                    GridEX1.RootTable.Columns("P21").Visible = True
                    GridEX1.RootTable.Columns("P21").Caption = Lector!M21
                Else
                    GridEX1.RootTable.Columns("P21").Visible = False
                End If
                If IsDBNull(Lector!M22) = False And Posicion22 = False Then
                    GridEX1.RootTable.Columns("P22").Visible = True
                    GridEX1.RootTable.Columns("P22").Caption = Lector!M2
                Else
                    GridEX1.RootTable.Columns("P22").Visible = False
                End If
                If IsDBNull(Lector!M23) = False And Posicion23 = False Then
                    GridEX1.RootTable.Columns("P23").Visible = True
                    GridEX1.RootTable.Columns("P23").Caption = Lector!M23
                Else
                    GridEX1.RootTable.Columns("P23").Visible = False
                End If
                If IsDBNull(Lector!M24) = False And Posicion24 = False Then
                    GridEX1.RootTable.Columns("P24").Visible = True
                    GridEX1.RootTable.Columns("P24").Caption = Lector!M24
                Else
                    GridEX1.RootTable.Columns("P24").Visible = False
                End If
                If IsDBNull(Lector!M25) = False And Posicion25 = False Then
                    GridEX1.RootTable.Columns("P25").Visible = True
                    GridEX1.RootTable.Columns("P25").Caption = Lector!M25
                Else
                    GridEX1.RootTable.Columns("P25").Visible = False
                End If
                If IsDBNull(Lector!M26) = False And Posicion26 = False Then
                    GridEX1.RootTable.Columns("P26").Visible = True
                    GridEX1.RootTable.Columns("P26").Caption = Lector!M26
                Else
                    GridEX1.RootTable.Columns("P26").Visible = False
                End If
                If IsDBNull(Lector!M27) = False And Posicion27 = False Then
                    GridEX1.RootTable.Columns("P27").Visible = True
                    GridEX1.RootTable.Columns("P27").Caption = Lector!M27
                Else
                    GridEX1.RootTable.Columns("P27").Visible = False
                End If
                If IsDBNull(Lector!M28) = False And Posicion28 = False Then
                    GridEX1.RootTable.Columns("P28").Visible = True
                    GridEX1.RootTable.Columns("P28").Caption = Lector!M15
                Else
                    GridEX1.RootTable.Columns("P28").Visible = False
                End If
                If IsDBNull(Lector!M29) = False And Posicion29 = False Then
                    GridEX1.RootTable.Columns("P29").Visible = True
                    GridEX1.RootTable.Columns("P29").Caption = Lector!M29
                Else
                    GridEX1.RootTable.Columns("P29").Visible = False
                End If
                If IsDBNull(Lector!M30) = False And Posicion30 = False Then
                    GridEX1.RootTable.Columns("P30").Visible = True
                    GridEX1.RootTable.Columns("P30").Caption = Lector!M30
                Else
                    GridEX1.RootTable.Columns("P30").Visible = False
                End If
                If SoloScanear = False Then
                    If IsDBNull(Lector!M1) = False And Posicion1 = False Then
                        GridEX1.RootTable.Columns("P1").Visible = True
                    Else
                        GridEX1.RootTable.Columns("P1").Visible = False
                    End If

                    If Corridas Then
                        If IsDBNull(Lector!M2) = False And Posicion2 = False Then
                            GridEX1.RootTable.Columns("P2").Visible = True
                        Else
                            GridEX1.RootTable.Columns("P2").Visible = False
                        End If

                        If IsDBNull(Lector!M3) = False And Posicion3 = False Then
                            GridEX1.RootTable.Columns("P3").Visible = True
                        Else
                            GridEX1.RootTable.Columns("P3").Visible = False
                        End If
                        If IsDBNull(Lector!M4) = False And Posicion4 = False Then
                            GridEX1.RootTable.Columns("P4").Visible = True
                        Else
                            GridEX1.RootTable.Columns("P4").Visible = False
                        End If
                        If IsDBNull(Lector!M5) = False And Posicion5 = False Then
                            GridEX1.RootTable.Columns("P5").Visible = True
                        Else
                            GridEX1.RootTable.Columns("P5").Visible = False
                        End If
                        If IsDBNull(Lector!M6) = False And Posicion6 = False Then
                            GridEX1.RootTable.Columns("P6").Visible = True
                        Else
                            GridEX1.RootTable.Columns("P6").Visible = False
                        End If
                        If IsDBNull(Lector!M7) = False And Posicion7 = False Then
                            GridEX1.RootTable.Columns("P7").Visible = True
                        Else
                            GridEX1.RootTable.Columns("P7").Visible = False
                        End If
                        If IsDBNull(Lector!M8) = False And Posicion8 = False Then
                            GridEX1.RootTable.Columns("P8").Visible = True
                        Else
                            GridEX1.RootTable.Columns("P8").Visible = False
                        End If
                        If IsDBNull(Lector!M9) = False And Posicion9 = False Then
                            GridEX1.RootTable.Columns("P9").Visible = True
                        Else
                            GridEX1.RootTable.Columns("P9").Visible = False
                        End If
                        If IsDBNull(Lector!M10) = False And Posicion10 = False Then
                            GridEX1.RootTable.Columns("P10").Visible = True
                        Else
                            GridEX1.RootTable.Columns("P10").Visible = False
                        End If
                        If IsDBNull(Lector!M11) = False And Posicion11 = False Then
                            GridEX1.RootTable.Columns("P11").Visible = True
                        Else
                            GridEX1.RootTable.Columns("P11").Visible = False
                        End If
                        If IsDBNull(Lector!M12) = False And Posicion12 = False Then
                            GridEX1.RootTable.Columns("P12").Visible = True
                        Else
                            GridEX1.RootTable.Columns("P12").Visible = False
                        End If
                        If IsDBNull(Lector!M13) = False And Posicion13 = False Then
                            GridEX1.RootTable.Columns("P13").Visible = True
                        Else
                            GridEX1.RootTable.Columns("P13").Visible = False
                        End If
                        If IsDBNull(Lector!M14) = False And Posicion14 = False Then
                            GridEX1.RootTable.Columns("P14").Visible = True
                        Else
                            GridEX1.RootTable.Columns("P14").Visible = False
                        End If
                        If IsDBNull(Lector!M15) = False And Posicion15 = False Then
                            GridEX1.RootTable.Columns("P15").Visible = True
                        Else
                            GridEX1.RootTable.Columns("P15").Visible = False
                        End If
                        If IsDBNull(Lector!M16) = False Then
                            GridEX1.RootTable.Columns("P16").Visible = True
                        Else
                            GridEX1.RootTable.Columns("P16").Visible = False
                        End If
                        If IsDBNull(Lector!M17) = False Then
                            GridEX1.RootTable.Columns("P17").Visible = True
                        Else
                            GridEX1.RootTable.Columns("P17").Visible = False
                        End If
                        If IsDBNull(Lector!M18) = False Then
                            GridEX1.RootTable.Columns("P18").Visible = True
                        Else
                            GridEX1.RootTable.Columns("P18").Visible = False
                        End If
                        If IsDBNull(Lector!M19) = False Then
                            GridEX1.RootTable.Columns("P19").Visible = True
                        Else
                            GridEX1.RootTable.Columns("P19").Visible = False
                        End If
                        If IsDBNull(Lector!M20) = False Then
                            GridEX1.RootTable.Columns("P20").Visible = True
                        Else
                            GridEX1.RootTable.Columns("P20").Visible = False
                        End If
                        If IsDBNull(Lector!M21) = False Then
                            GridEX1.RootTable.Columns("P21").Visible = True
                        Else
                            GridEX1.RootTable.Columns("P21").Visible = False
                        End If
                        If IsDBNull(Lector!M22) = False Then
                            GridEX1.RootTable.Columns("P22").Visible = True
                        Else
                            GridEX1.RootTable.Columns("P22").Visible = False
                        End If
                        If IsDBNull(Lector!M23) = False Then
                            GridEX1.RootTable.Columns("P23").Visible = True
                        Else
                            GridEX1.RootTable.Columns("P23").Visible = False
                        End If
                        If IsDBNull(Lector!M24) = False Then
                            GridEX1.RootTable.Columns("P24").Visible = True
                        Else
                            GridEX1.RootTable.Columns("P24").Visible = False
                        End If
                        If IsDBNull(Lector!M25) = False Then
                            GridEX1.RootTable.Columns("P25").Visible = True
                        Else
                            GridEX1.RootTable.Columns("P25").Visible = False
                        End If
                        If IsDBNull(Lector!M26) = False Then
                            GridEX1.RootTable.Columns("P26").Visible = True
                        Else
                            GridEX1.RootTable.Columns("P26").Visible = False
                        End If
                        If IsDBNull(Lector!M27) = False Then
                            GridEX1.RootTable.Columns("P27").Visible = True
                        Else
                            GridEX1.RootTable.Columns("P27").Visible = False
                        End If
                        If IsDBNull(Lector!M28) = False Then
                            GridEX1.RootTable.Columns("P28").Visible = True
                        Else
                            GridEX1.RootTable.Columns("P28").Visible = False
                        End If
                        If IsDBNull(Lector!M29) = False Then
                            GridEX1.RootTable.Columns("P29").Visible = True
                        Else
                            GridEX1.RootTable.Columns("P29").Visible = False
                        End If
                        If IsDBNull(Lector!M30) = False Then
                            GridEX1.RootTable.Columns("P30").Visible = True
                        Else
                            GridEX1.RootTable.Columns("P30").Visible = False
                        End If
                    End If
                End If
            Loop
            Lector.Close()
            Comando = Nothing

        Else
            'GridEX1.RootTable.Columns("P1").Caption = "Cantidad"
            'GridEX1.RootTable.Columns("P1").Width = GridEX1.RootTable.Columns("Cantidad").Width
            GridEX1.RootTable.Columns("P2").Visible = False
            GridEX1.RootTable.Columns("P3").Visible = False
            GridEX1.RootTable.Columns("P4").Visible = False
            GridEX1.RootTable.Columns("P5").Visible = False
            GridEX1.RootTable.Columns("P6").Visible = False
            GridEX1.RootTable.Columns("P7").Visible = False
            GridEX1.RootTable.Columns("P8").Visible = False
            GridEX1.RootTable.Columns("P9").Visible = False
            GridEX1.RootTable.Columns("P10").Visible = False
            GridEX1.RootTable.Columns("P11").Visible = False
            GridEX1.RootTable.Columns("P12").Visible = False
            GridEX1.RootTable.Columns("P13").Visible = False
            GridEX1.RootTable.Columns("P14").Visible = False
            GridEX1.RootTable.Columns("P15").Visible = False
            GridEX1.RootTable.Columns("P16").Visible = False
            GridEX1.RootTable.Columns("P17").Visible = False
            GridEX1.RootTable.Columns("P18").Visible = False
            GridEX1.RootTable.Columns("P19").Visible = False
            GridEX1.RootTable.Columns("P20").Visible = False
            GridEX1.RootTable.Columns("P21").Visible = False
            GridEX1.RootTable.Columns("P22").Visible = False
            GridEX1.RootTable.Columns("P23").Visible = False
            GridEX1.RootTable.Columns("P24").Visible = False
            GridEX1.RootTable.Columns("P25").Visible = False
            GridEX1.RootTable.Columns("P26").Visible = False
            GridEX1.RootTable.Columns("P27").Visible = False
            GridEX1.RootTable.Columns("P28").Visible = False
            GridEX1.RootTable.Columns("P29").Visible = False
            GridEX1.RootTable.Columns("P30").Visible = False


        End If





    End Sub


    Private Function SiguienteNumero() As Long

        Dim cmd As SqlClient.SqlCommand
        Dim rs As SqlClient.SqlDataReader
        Dim V As clsValidar

        'SiguienteNumero = 1
        'Exit Function

        using cnn As New SqlClient.SqlConnection(scnn)
        
        cnn.Open()

        V = New clsValidar

        cmd = New SqlClient.SqlCommand("PfrmPedidos_SiguienteNumero", cnn)
        cmd.CommandType = CommandType.StoredProcedure
        cmd.Parameters.Add(New System.Data.SqlClient.SqlParameter("@EmpresaID", System.Data.SqlDbType.UniqueIdentifier, 16, "EmpresaID"))
        cmd.Parameters.Add(New System.Data.SqlClient.SqlParameter("@DocumentoID", System.Data.SqlDbType.Int, 4, "DocumentoID"))
        cmd.Parameters("@EmpresaID").Value = EmpresaID
        cmd.Parameters("@DocumentoID").Value = DocumentoID

        rs = cmd.ExecuteReader()
        SiguienteNumero = 1
        Do While rs.Read
            If IsDBNull(rs!Maximo) = False Then
                SiguienteNumero = V.IniLong(rs!Maximo)
            End If
        Loop
        rs.Close()
        cmd = Nothing

       end using
        

    End Function





    Private Sub GridEX1_DeletingRecords(ByVal sender As System.Object, ByVal e As System.ComponentModel.CancelEventArgs) Handles GridEX1.DeletingRecords
        Dim Row As GridEXRow = GridEX1.GetRow
        Dim Cantidad As Integer


        If IsDBNull(Row.Cells("Lote").Value) = False Then
            e.Cancel = True
            MsgBox("No se puede eliminar este renglon porque ya ha sido programado !!!")

        Else
            If PoderEliminar = False Then
                Cantidad = Parcializacion(Row.Cells("PedidosID").Value, Row.Cells("Renglon").Value)
                If Cantidad > 1 Then
                    If MsgBox("¿Quieres eliminar la parcialización completa?", MsgBoxStyle.YesNo) = MsgBoxResult.Yes Then
                        ParcializacionEliminar(Row.Cells("PedidosID").Value, Row.Cells("Renglon").Value)
                    Else
                        If MsgBox("¿Eliminar este registro nada más?", MsgBoxStyle.YesNo) = MsgBoxResult.Yes Then
                        Else
                            e.Cancel = True

                        End If

                    End If

                Else

                    If MsgBox("¿Quieres eliminar este registro?", MsgBoxStyle.YesNo) = MsgBoxResult.Yes Then
                    Else
                        e.Cancel = True

                    End If
                End If
            End If
        End If
    End Sub




    Private Sub GridEX1_UpdatingRecord(ByVal sender As System.Object, ByVal e As System.ComponentModel.CancelEventArgs) Handles GridEX1.UpdatingRecord

        Dim Row As GridEXRow = GridEX1.GetRow
        If IsDBNull(Row.Cells("ProductoID").Value) Then
            e.Cancel = True
            Me.GridEX1.CurrentColumn = Me.GridEX1.RootTable.Columns("ProductoID")


            Exit Sub

        End If
        If BPrecioAutorizado = False Then
            BuscaProductoPrecioLista()
            If Negocio <> "CLIFF" Then
                If (Row.Cells("Precio").Value) >= PrecioLista And Row.Cells("Precio").Value <= (PrecioLista + (PrecioLista * PPrecioTope / 100)) Then
                Else
                    e.Cancel = True
                    Me.GridEX1.CurrentColumn = Me.GridEX1.RootTable.Columns("Precio")

                    MsgBox("Necesitas indicar el Precio mayor al precio de la lista")
                    Exit Sub

                End If
            End If
        End If
        GridEX1.SetValue("Corrida_AtadoID", UsuarioID)


        Me.CalculateTotals()

    End Sub


    Private Sub GridEX1_AddingRecord(ByVal sender As System.Object, ByVal e As System.ComponentModel.CancelEventArgs) Handles GridEX1.AddingRecord
        Dim CurrentRow As GridEXRow = GridEX1.GetRow


        If Me.BExigirPrecioEnPedido Then
            If (CurrentRow.Cells("Precio").Value) = 0 And IsDBNull(CurrentRow.Cells("ProductoID").Value) = False And LookCliente.Text <> "COMERCIALIZADORA BOGGER" Then

                If Cortesia(CurrentRow.Cells("ProductoID").Value) = False Then

                    e.Cancel = True
                    Me.GridEX1.CurrentColumn = Me.GridEX1.RootTable.Columns("Precio")

                    MsgBox("Necesitas indicar el Precio")
                    Exit Sub
                End If

            End If
        End If
        If BPrecioAutorizado = False Then
            BuscaProductoPrecioLista()
            If ((CurrentRow.Cells("Precio").Value) >= PrecioLista And CurrentRow.Cells("Precio").Value <= (PrecioLista + (PrecioLista * PPrecioTope / 100)) Or LookCliente.Text = "COMERCIALIZADORA BOGGER") Then
            Else
                e.Cancel = True
                Me.GridEX1.CurrentColumn = Me.GridEX1.RootTable.Columns("Precio")

                MsgBox("Necesitas indicar el Precio mayor al precio de la lista")
                Exit Sub

            End If
        End If

        If IsDBNull(CurrentRow.Cells("OrdenCliente").Value) = True Then
            e.Cancel = True
            Me.GridEX1.CurrentColumn = Me.GridEX1.RootTable.Columns("OrdenCliente")

            MsgBox("Necesitas indicar el P.O.")
            Exit Sub

        End If

        If IsDBNull(CurrentRow.Cells("ClienteCadenaID").Value) = True Then
            e.Cancel = True
            Me.GridEX1.CurrentColumn = Me.GridEX1.RootTable.Columns("ClienteCadenaID")

            MsgBox("Necesitas indicar a donde se va a facturar")
            Exit Sub

        End If

        If Me.LookAlmacen.Text = "" Or IsDBNull(LookAlmacen.EditValue) = True Then
            e.Cancel = True
            Me.LookAlmacen.Focus()

            MsgBox("Necesitas indicar el almacén primero !!!")
            Exit Sub

        End If

        'If IsDBNull(CurrentRow.Cells("ClienteCadenaIDEnviar").Value) = True Then
        '    e.Cancel = True
        '    Me.GridEX1.CurrentColumn = Me.GridEX1.RootTable.Columns("ClienteCadenaIDEnviar")

        '    MsgBox("Necesitas indicar a donde se va a enviar")
        '    Exit Sub

        'End If
        'Me.ComboTransporte.Value = Me.BuscarTransporte(CurrentRow.Cells("ClienteCadenaIDEnviar").Value)

        If IsDBNull(CurrentRow.Cells("UnidadID").Value) Then
            CurrentRow.Cells("UnidadID").Value = BuscaUnidad()
        End If

        GridEX1.SetValue("Corrida_AtadoID", UsuarioID)   'Es Correcto

        GridEX1.SetValue("PrecioID", PrecioID)
        GridEX1.SetValue("Pedidos_DetailsID", Guid.NewGuid)
        If Nuevo Then
            GridEX1.SetValue("PedidosID", PedidosID)
        End If
        If Me.BDesglosar = False Then
            GridEX1.SetValue("Renglon", SiguienteRenglon)

        End If
        GridEX1.SetValue("NC", SiguienteNC)
        GridEX1.SetValue("Lugar", 99)

        If IsDBNull(CurrentRow.Cells("OrdenCliente")) Then
            If IsDBNull(Me.txtReferencia.Text) Then
                GridEX1.SetValue("OrdenCliente", Me.txtNumero.Text)
            Else
                GridEX1.SetValue("OrdenCliente", Me.txtReferencia.Text)
            End If
        End If

        Me.CalculateTotals()
        Me.GridEX1.CurrentColumn = Me.GridEX1.RootTable.Columns("Codigo")
        AsignarRegistro()

    End Sub

    Private Sub GridEX1_SelectionChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles GridEX1.SelectionChanged
        Dim Row As GridEXRow = GridEX1.GetRow
        If GridEX1.Row <= -1 Then
            Habilitar(True)
            Me.GridEX1.RootTable.Columns("Renglon").DefaultValue = Pedidos_Details_Renglon()
        Else
            If YaAutorizado Then
                Habilitar(False)
            Else

                Habilitar(True)
            End If
            Me.GridEX1.RootTable.Columns("Codigo").EditType = EditType.NoEdit
            Me.GridEX1.RootTable.Columns("ProductoID").EditType = EditType.NoEdit

            'If IsDBNull(Row.Cells("Notas").Value) Then
            '    Habilitar(True)
            'Else
            'End If
            If IsDBNull(Row.Cells("Lote").Value) = False Then
                Habilitar(False)
            End If
        End If
        IrAPonerCorrida()
        RestaurarInventario()
        VerFoto()
    End Sub



    Private Sub VerCliente()
        GroupBoxCliente.Visible = True


    End Sub

    Private Sub VerProveedor()

    End Sub






    Private Sub btnSelectCliente_Click(ByVal sender As System.Object, ByVal e As System.EventArgs)

    End Sub


    Private Sub Configuración()


        LM = False
        'PrecioID = ""
        DescuentoCliente = False
        Corridas = True
        SoloScanear = False


        LabFechaRecepcion.Visible = True
        jsdtFechaRecepcion.Visible = True
        VerCliente()

        Configura2()

        Me.GridEX1.RootTable.Columns("Descuento").Visible = True


    End Sub


    Private Sub btnImprimir_Click(ByVal sender As System.Object, ByVal e As System.EventArgs)

        Dim rs As SqlClient.SqlDataReader
        Dim Comando As SqlClient.SqlCommand

        Dim ClaveEmpresa As Integer


        using cnn As New SqlClient.SqlConnection(scnn)
        
        cnn.Open()

        Comando = New SqlClient.SqlCommand("PSelectClaveEmpresa", cnn)
        Comando.CommandType = CommandType.StoredProcedure
        Comando.CommandTimeout = 300

        Comando.Parameters.Add(New System.Data.SqlClient.SqlParameter("@EmpresaID", System.Data.SqlDbType.UniqueIdentifier, 16, "EmpresaID"))
        Comando.Parameters("@EmpresaID").Value = EmpresaID


        rs = Comando.ExecuteReader
        ClaveEmpresa = 0

        Do While rs.Read()


            ClaveEmpresa = rs!ClaveEmpresa

        Loop


        rs.Close()


       end using
        

        'f = New frmRepDoc()

        'f.Formula = "{Empresa.ClaveEmpresa} = " & ClaveEmpresa & " AND {Pedidos.DocumentoID} = " & mDataRow("DocumentoID") & " AND {Pedidos.Numero} = " & mDataRow("Numero")
        'Select Case DocumentoID

        'End Select
        'Select Case DocumentoID
        '    Case 1 : f.NombreDelReporte = "CrysOrdenDeCompra"
        '    Case 2 : f.NombreDelReporte = "CrysRecepcionDeMercancia"
        '    Case 3 : f.NombreDelReporte = "CrysDevolucionDeCompra"
        '    Case 11 : f.NombreDelReporte = "CrysFactura"
        '    Case 12 : f.NombreDelReporte = "CrysRemision"
        '    Case 13 : f.NombreDelReporte = "CrysPedido"
        '    Case 14 : f.NombreDelReporte = "CrysCotizacion"
        '    Case 15 : f.NombreDelReporte = "CrysDevolucionDeFactura"
        '    Case 16 : f.NombreDelReporte = "CrysNotasDeVenta"
        '    Case 17 : f.NombreDelReporte = "CrysDevolucionDeNotaDeMostrador"
        '    Case 18 : f.NombreDelReporte = "CrysProduccionDeEtiquetas"
        '    Case 19 : f.NombreDelReporte = "CrysPresupuesto"
        '    Case 20 : f.NombreDelReporte = "CrysPedidoEnTransito"
        '    Case 21 : f.NombreDelReporte = "CrysTransferenciaEntrada"
        '    Case 22 : f.NombreDelReporte = "CrysTransferenciaSalida"
        '    Case 23 : f.NombreDelReporte = "CrysRequisicionDeCompra"
        '    Case 27 : f.NombreDelReporte = "CrysAjusteDeMercancia"
        '    Case 28 : f.NombreDelReporte = "CrysInventarioFisico"


        'End Select
        'f.Show()
    End Sub

    Private Sub FillPrecio()
        Dim rs As SqlClient.SqlDataReader
        Dim Comando As SqlClient.SqlCommand

        Dim i As Long

        using cnn As New SqlClient.SqlConnection(scnn)
        
        cnn.Open()

        Comando = New SqlClient.SqlCommand("PSelectPrecio", cnn)
        Comando.CommandType = CommandType.StoredProcedure
        Comando.CommandTimeout = 300

        Comando.Parameters.Add(New System.Data.SqlClient.SqlParameter("@GrupoID", System.Data.SqlDbType.UniqueIdentifier, 16, "GrupoID"))
        Comando.Parameters("@GrupoID").Value = GrupoID


        rs = Comando.ExecuteReader
        i = 0
        Do While rs.Read()
            i = i + 1
            PrecioID = rs!PrecioID
        Loop

        If i = 1 Then
            GridEX1.RootTable.Columns("PrecioID").Visible = False
        End If

        rs.Close()
        Comando = Nothing
       end using
        



        Me.SqlSelectCommand1_.Parameters("@GrupoID").Value = GrupoID
        daPrecio.Fill(dsBase.PDropDownPrecio)
        GridEX1.DropDowns("Precio").SetDataBinding(dsBase, "PDropDownPrecio")

    End Sub

    Private Sub CalculateDetailTotal()
        Dim row As GridEXRow
        Dim v As clsValidar
        Dim Cantidad As Double
        Dim neto As Decimal
        Dim Importe As Decimal
        Dim PrecioCIva As Decimal

        v = New clsValidar
        row = Me.GridEX1.GetRow()




        Cantidad = CSng(v.IniDouble(row.Cells("P1").Value) + v.IniDouble(row.Cells("P2").Value) + v.IniDouble(row.Cells("P3").Value) + v.IniDouble(row.Cells("P4").Value) + v.IniDouble(row.Cells("P5").Value) + v.IniDouble(row.Cells("P6").Value) + v.IniDouble(row.Cells("P7").Value) + v.IniDouble(row.Cells("P8").Value) + v.IniDouble(row.Cells("P9").Value) + v.IniDouble(row.Cells("P10").Value) + v.IniDouble(row.Cells("P11").Value) + v.IniDouble(row.Cells("P12").Value) + v.IniDouble(row.Cells("P13").Value) + v.IniDouble(row.Cells("P14").Value) + v.IniDouble(row.Cells("P15").Value) + v.IniDouble(row.Cells("P16").Value) + v.IniDouble(row.Cells("P17").Value) + v.IniDouble(row.Cells("P18").Value) + v.IniDouble(row.Cells("P19").Value) + v.IniDouble(row.Cells("P20").Value) + v.IniDouble(row.Cells("P21").Value) + v.IniDouble(row.Cells("P22").Value) + v.IniDouble(row.Cells("P23").Value) + v.IniDouble(row.Cells("P24").Value) + v.IniDouble(row.Cells("P25").Value) + v.IniDouble(row.Cells("P26").Value) + v.IniDouble(row.Cells("P27").Value) + v.IniDouble(row.Cells("P28").Value) + v.IniDouble(row.Cells("P29").Value) + v.IniDouble(row.Cells("P30").Value))
        'Me.GridEX1.SetValue("Cantidad", Cantidad)

        PrecioCIva = row.Cells("Precio").Value + row.Cells("Precio").Value * row.Cells("IVA").Value / 100
        Me.GridEX1.SetValue("PrecioCIva", PrecioCIva)

        Importe = CSng(Cantidad * row.Cells("Precio").Value)
        Me.GridEX1.SetValue("Importe", Importe)


        neto = Cantidad * PrecioCIva - Cantidad * PrecioCIva * v.IniDouble(row.Cells("Descuento").Value) / 100
        Me.GridEX1.SetValue("Neto", neto)


    End Sub


    Private Sub IrAPonerCorrida()
        Dim Row As GridEXRow = GridEX1.GetRow
        If GridEX1.RowCount > 0 Then
            If IsDBNull(Row.Cells("ProductoID").Value) Or IsNothing(Row.Cells("ProductoID").Value) Then
            Else
                PonerCorrida2(BuscaCorrida, Row.Cells("ProductoID").Value)
            End If
        End If


    End Sub
    Private Sub GridEX1_RecordUpdated(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles GridEX1.RecordUpdated
        Me.CalculateTotals()
        UpdateChanges()
        RestaurarConcentrado()
    End Sub

    Private Sub GridEX1_RecordAdded(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles GridEX1.RecordAdded
        Dim Row As GridEXRow = GridEX1.GetRow

        UOrdenCliente = Row.Cells("OrdenCliente").Value
        GridEX1.RootTable.Columns("OrdenCliente").DefaultValue = UOrdenCliente

        UClienteCadenaID = Row.Cells("ClienteCadenaID").Value
        GridEX1.RootTable.Columns("ClienteCadenaID").DefaultValue = UClienteCadenaID


        Me.CalculateTotals()
        UpdateChanges()

        ChecarPrecios()

        RestaurarConcentrado()
        If BDesglosarPedido Then
            Desglosar()
        End If

        'If Posicion Then
        '    GridEX1.Row = Fila
        'Else
        '    GridEX1.Row = -1
        'End If
    End Sub

    Private Sub GridEX1_Validated(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles GridEX1.Validated
        Me.CalculateTotals()
    End Sub

    Private Sub GridEX1_RecordsDeleted(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles GridEX1.RecordsDeleted
        Me.CalculateTotals()
        UpdateChanges()
        RestaurarConcentrado()
        CalculateTotals()
        Me.PoderEliminar = False
    End Sub




    Private Sub GridEX1_KeyDown(ByVal sender As System.Object, ByVal e As System.Windows.Forms.KeyEventArgs) Handles GridEX1.KeyDown
        Select Case e.KeyCode

            Case Keys.F11
                If GridEX1.Row > -1 Then
                    Me.AsignarRegistro()
                End If
                CopiarRegistroTallas()
                Me.GridEX1.Row = -1

            Case Keys.F12
                If GridEX1.Row > -1 Then
                    Me.AsignarRegistro()
                End If
                CopiarRegistro()
                Me.GridEX1.Row = -1
                Me.GridEX1.CurrentColumn = Me.GridEX1.RootTable.Columns("ProductoID")

            Case Keys.F5
                Me.GridEX1.Row = -1
                Me.GridEX1.CurrentColumn = Me.GridEX1.RootTable.Columns("ProductoID")


        End Select

    End Sub






    Private Function TraeDelMaletin() As Long
        Dim ProductoID As Guid
        Dim ClaveProducto As Long

        Dim Comando As SqlClient.SqlCommand
        Dim Lector As SqlClient.SqlDataReader



        ProductoID = MaletinSacarProductoID()



        using cnn As New SqlClient.SqlConnection(scnn)
        
        cnn.Open()

        Comando = New SqlClient.SqlCommand("PBuscaProducto_ClaveProducto", cnn)
        Comando.CommandType = CommandType.StoredProcedure
        Comando.CommandTimeout = 300
        Comando.Parameters.Add(New System.Data.SqlClient.SqlParameter("@ProductoID", System.Data.SqlDbType.UniqueIdentifier, 16, "ProductoID"))
        Comando.Parameters("@ProductoID").Value = ProductoID

        Lector = Comando.ExecuteReader
        Do While Lector.Read

            ClaveProducto = Lector!ClaveProducto

        Loop
        Lector.Close()

        Lector = Nothing
        Comando = Nothing
       end using
        




        TraeDelMaletin = ClaveProducto

    End Function

    Private Function MaletinSacarProductoID() As Guid

        Dim Comando As SqlClient.SqlCommand
        Dim Lector As SqlClient.SqlDataReader



        using cnn As New SqlClient.SqlConnection(scnn)
        
        cnn.Open()

        Comando = New SqlClient.SqlCommand("PMaletinSacarProductoID", cnn)
        Comando.CommandType = CommandType.StoredProcedure
        Comando.CommandTimeout = 300

        Lector = Comando.ExecuteReader
        Do While Lector.Read
            MaletinSacarProductoID = Lector!ProductoID

        Loop
        Lector.Close()

        Lector = Nothing
        Comando = Nothing
       end using
        




    End Function

    Private Sub BuscaProducto()

        Dim Codigo As String
        Dim ProductoID As Guid
        Dim IVA As Double

        Dim i As Integer
        Dim Temp As String
        Dim v As clsValidar
        Dim clsDow As clsDow
        Dim Row As GridEXRow = GridEX1.GetRow()

        Dim Comando As SqlClient.SqlCommand
        Dim Lector As SqlClient.SqlDataReader

        Dim UltimaUnidad As Guid
        Dim Comando2 As SqlClient.SqlCommand
        Dim Lector2 As SqlClient.SqlDataReader


        Dim CorridaID As Guid
        Dim UnidadIDCosto As Guid
        Dim Cantidad As Double
        Dim Precio As Decimal
        Dim PrecioCIva As Decimal



        v = New clsValidar

        If v.vFieldVal(Row.Cells("Codigo").Value) = "" Then
            v = Nothing
            Exit Sub
        End If
        BuscarPrecio()

        Temp = Row.Cells("Codigo").Value
        Codigo = Temp


        using cnn As New SqlClient.SqlConnection(scnn)
        
        cnn.Open()

        Comando = Nothing
        Comando = New SqlClient.SqlCommand("PfrmPedidos_BuscaProducto2", cnn)
        Comando.CommandType = CommandType.StoredProcedure
        Comando.Parameters.Add(New System.Data.SqlClient.SqlParameter("@GrupoID", System.Data.SqlDbType.UniqueIdentifier, 16, "GrupoID"))
        Comando.Parameters.Add("@Codigo", System.Data.SqlDbType.VarChar, 16, "Codigo")

        Comando.Parameters("@GrupoID").Value = GrupoID
        Comando.Parameters("@Codigo").Value = Codigo
        Lector = Comando.ExecuteReader()
        Do While Lector.Read
            i = i + 1
            ProductoID = Lector!ProductoID
            CorridaID = Lector!CorridaID
            If LM Then
                IVA = 0
            Else
                IVA = Lector!IVA
            End If
            UnidadIDCosto = Lector!UnidadIDCosto

            'If IsDBNull(Lector!FUltVenta) = False Then
            '    Me.jsdtFechaRecepcion.Value = Lector!FUltVenta
            '    Me.GridEX1.SetValue("FechaEntrega", Lector!FUltVenta)
            '    Me.GridEX1.SetValue("FechaEmbarque", Lector!FUltVenta)
            'End If

            If Lector!Status = False Then
                MsgBox("Producto con Status desactivado !!!")
                Exit Sub
            End If


        Loop
        If i = 0 Then
            MessageBox.Show("Producto no existente", "", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly)
            If GridEX1.RootTable.Columns("PrecioID").Visible Then
                GridEX1.CurrentColumn = GridEX1.RootTable.Columns("PrecioID")
            Else
                GridEX1.CurrentColumn = GridEX1.RootTable.Columns("Codigo")
            End If
            Lector.Close()

            Comando = Nothing

            Exit Sub
        Else
            Lector.Close()
            Comando = Nothing
        End If
        Me.GridEX1.SetValue("ProductoID", ProductoID)
        Me.GridEX1.SetValue("Impuesto", IVA)


        Comando = New SqlClient.SqlCommand("PBuscaProducto3", cnn)
        Comando.CommandType = CommandType.StoredProcedure
        Comando.CommandTimeout = 300
        Comando.Parameters.Add(New System.Data.SqlClient.SqlParameter("@EmpresaID", System.Data.SqlDbType.UniqueIdentifier, 16, "EmpresaID"))
        Comando.Parameters.Add(New System.Data.SqlClient.SqlParameter("@ProductoID", System.Data.SqlDbType.UniqueIdentifier, 16, "ProductoID"))
        Comando.Parameters.Add(New System.Data.SqlClient.SqlParameter("@PrecioID", System.Data.SqlDbType.UniqueIdentifier, 16, "PrecioID"))

        Comando.Parameters("@EmpresaID").Value = EmpresaID
        Comando.Parameters("@ProductoID").Value = ProductoID
        Comando.Parameters("@PrecioID").Value = PrecioID

        Lector = Comando.ExecuteReader()
        Do While Lector.Read
            CorridaID = Lector!CorridaID
            Precio = Lector!Precio
            PrecioLista = Precio
            PrecioCIva = Lector!PrecioCIva
            GridEX1.SetValue("PRECIO", Lector!Precio)
            clsDow = New clsDow
            GridEX1.SetValue("PRECIOCIVA", Lector!PrecioCIva)
            If Lector!Remate Then
                GridEX1.SetValue("DESCUENTO", 0)
                GridEX1.RootTable.Columns("DESCUENTO").EditType = EditType.NoEdit
            Else
                GridEX1.RootTable.Columns("DESCUENTO").EditType = EditType.TextBox
                If DescuentoCliente > Lector!Descuento Then
                    GridEX1.SetValue("DESCUENTO", DescuentoCliente)
                Else
                    GridEX1.SetValue("DESCUENTO", Lector!Descuento)
                End If
            End If
            If Lector!UltimaUnidad = False Then
                GridEX1.SetValue("UNIDADID", Lector!UnidadIDPrecio)
            Else

                Comando2 = New SqlClient.SqlCommand("PBuscaProductoUltimaUnidad", cnn)
                Comando2.CommandType = CommandType.StoredProcedure
                Comando2.Parameters.Add(New System.Data.SqlClient.SqlParameter("@ProductoID", System.Data.SqlDbType.UniqueIdentifier, 16, "ProductoID"))
                Comando2.Parameters("@ProductoID").Value = ProductoID
                Lector2 = Comando2.ExecuteReader
                i = 0
                Do While Lector2.Read
                    i = i + 1
                    UltimaUnidad = Lector2!UnidadID
                Loop
                Lector2.Close()
                Comando2 = Nothing
                If i > 0 Then
                    GridEX1.SetValue("UNIDADID", UltimaUnidad)
                End If
                If IsDBNull(Row.Cells("UNIDADID").Value) Then
                    GridEX1.SetValue("UNIDADID", Lector2!UnidadIDPrecio)
                End If
                Lector2.Close()

            End If

            Comando2 = Nothing
        Loop
        Lector.Close()
        Comando = Nothing



        GridEX1.SetValue("CANTIDAD", 0)
        PonerCorrida2(CorridaID, ProductoID)

        If IsDBNull(Row.Cells("UNIDADID").Value) Then

            Exit Sub
        End If

        Comando = New SqlClient.SqlCommand("PBuscaProductoConUni", cnn)
        Comando.CommandType = CommandType.StoredProcedure
        Comando.CommandTimeout = 300
        Comando.Parameters.Add(New System.Data.SqlClient.SqlParameter("@UnidadID", System.Data.SqlDbType.UniqueIdentifier, 16, "UnidadID"))
        Comando.Parameters.Add(New System.Data.SqlClient.SqlParameter("@UnidadIDCosto", System.Data.SqlDbType.UniqueIdentifier, 16, "UnidadIDCosto"))

        Comando.Parameters("@UnidadID").Value = UnidadIDCosto
        Comando.Parameters("@UnidadIDCosto").Value = UnidadIDCosto

        Lector = Comando.ExecuteReader()
        i = 0
        Do While Lector.Read
            i = i + 1
            Cantidad = Lector!Cantidad
        Loop
        Lector.Close()

        Comando = Nothing
       end using
        

        If i = 0 Then
            Cantidad = 1
            'MessageBox.Show("Necesitas dar de alta la conversión de unidad correspondiente", "", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly)
            '()
            'Exit Sub

        End If

        GridEX1.SetValue("PRECIO", Precio / Cantidad)
        GridEX1.SetValue("PRECIOCIVA", PrecioCIva / Cantidad)


        If GridEX1.RootTable.Columns("P1").Caption = "U" Or Corridas = False Then
            GridEX1.RootTable.Columns("P2").Visible = False
            GridEX1.RootTable.Columns("P3").Visible = False
            GridEX1.RootTable.Columns("P4").Visible = False
            GridEX1.RootTable.Columns("P5").Visible = False
            GridEX1.RootTable.Columns("P6").Visible = False
            GridEX1.RootTable.Columns("P7").Visible = False
            GridEX1.RootTable.Columns("P8").Visible = False
            GridEX1.RootTable.Columns("P9").Visible = False
            GridEX1.RootTable.Columns("P10").Visible = False
            GridEX1.RootTable.Columns("P11").Visible = False
            GridEX1.RootTable.Columns("P12").Visible = False
            GridEX1.RootTable.Columns("P13").Visible = False
            GridEX1.RootTable.Columns("P14").Visible = False
            GridEX1.RootTable.Columns("P15").Visible = False
            GridEX1.RootTable.Columns("P16").Visible = False
            GridEX1.RootTable.Columns("P17").Visible = False
            GridEX1.RootTable.Columns("P18").Visible = False
            GridEX1.RootTable.Columns("P19").Visible = False
            GridEX1.RootTable.Columns("P20").Visible = False
            GridEX1.RootTable.Columns("P21").Visible = False
            GridEX1.RootTable.Columns("P22").Visible = False
            GridEX1.RootTable.Columns("P23").Visible = False
            GridEX1.RootTable.Columns("P24").Visible = False
            GridEX1.RootTable.Columns("P25").Visible = False
            GridEX1.RootTable.Columns("P26").Visible = False
            GridEX1.RootTable.Columns("P27").Visible = False
            GridEX1.RootTable.Columns("P28").Visible = False
            GridEX1.RootTable.Columns("P29").Visible = False
            GridEX1.RootTable.Columns("P30").Visible = False

            GridEX1.RootTable.Columns("P1").Caption = "Cantidad"
            'GridEX1.SetValue("P1", 1)

            Exit Sub

        End If
        Me.GridEX1.SetValue("IMPORTE", Me.GridEX1.GetValue("CANTIDAD") * Me.GridEX1.GetValue("PRECIO"))
        Me.GridEX1.SetValue("NETO", Me.GridEX1.GetValue("IMPORTE") - Me.GridEX1.GetValue("IMPORTE") * Me.GridEX1.GetValue("DESCUENTO") / 100)

        Me.CalculateTotals()



    End Sub

    Private Sub PonerCorrida()

        Dim cmd As SqlClient.SqlCommand
        Dim rs As SqlClient.SqlDataReader
        Dim Row As GridEXRow = GridEX1.GetRow()
        Dim CorridaID As Guid

        If IsDBNull(Row.Cells("ProductoID").Value) Or GridEX1.Row = -1 Then
            Exit Sub
        End If


        using cnn As New SqlClient.SqlConnection(scnn)
        
        cnn.Open()


        cmd = New SqlClient.SqlCommand("PfrmPedidos_GridEx1_SelectionChanged", cnn)
        cmd.CommandType = CommandType.StoredProcedure
        cmd.Parameters.Add(New System.Data.SqlClient.SqlParameter("@ProductoID", System.Data.SqlDbType.UniqueIdentifier, 16, "ProductoID"))
        cmd.Parameters("@ProductoID").Value = Row.Cells("ProductoID").Value


        rs = cmd.ExecuteReader()
        Do While rs.Read
            CorridaID = rs!CorridaID
        Loop
        rs.Close()
        cmd = Nothing

       end using
        


        PonerCorrida2(CorridaID, Row.Cells("ProductoID").Value)

    End Sub

    Private Sub FillCatalogos(ByVal ClienteID As Guid)
        Me.dsBase.PDD_ProductoParaPedido.Clear()
        Me.sqlPDD_Producto.Parameters("@GrupoID").Value = GrupoID
        Me.sqlPDD_Producto.Parameters("@Nuevo").Value = Nuevo
        Me.sqlPDD_Producto.Parameters("@ClienteID").Value = ClienteID

        daPDD_Producto.Fill(dsBase.PDD_ProductoParaPedido)
        GridEX1.DropDowns("Producto").SetDataBinding(dsBase, "PDD_ProductoParaPedido")



    End Sub

    Private Sub Restaurar_ClienteCadena(ByVal ClienteID As Guid)
        Me.dsBase.PDD_ClienteCadena.Clear()
        Me.sqlPDD_ClienteCadena.Parameters("@ClienteID").Value = ClienteID
        Me.daPDD_ClienteCadena.Fill(Me.dsBase.PDD_ClienteCadena)
        Me.GridEX1.DropDowns("ClienteCadena").SetDataBinding(dsBase, "PDD_ClienteCadena")


    End Sub


    Private Sub Restaurar_ClienteCadena_Enviar(ByVal ClienteID As Guid)
        Me.dsBase.PDD_ClienteCadena_Enviar.Clear()
        Me.sqlPDD_ClienteCadena_Enviar.Parameters("@ClienteID").Value = ClienteID
        Me.daPDD_ClienteCadena_Enviar.Fill(Me.dsBase.PDD_ClienteCadena_Enviar)
        Me.GridEX1.DropDowns("ClienteCadena_Enviar").SetDataBinding(dsBase, "PDD_ClienteCadena_Enviar")
    End Sub

    Private Function BuscaUnidad() As Guid

        Dim Comando As SqlClient.SqlCommand
        Dim Lector As SqlClient.SqlDataReader






        using cnn As New SqlClient.SqlConnection(scnn)
        
        cnn.Open()

        Comando = New SqlClient.SqlCommand("PfrmPedidos_BuscaUnidad", cnn)
        Comando.CommandType = CommandType.StoredProcedure
        Comando.CommandTimeout = 300
        Comando.Parameters.Add(New System.Data.SqlClient.SqlParameter("@GrupoID", System.Data.SqlDbType.UniqueIdentifier, 16, "GrupoID"))
        Comando.Parameters("@GrupoID").Value = GrupoID

        Lector = Comando.ExecuteReader
        Do While Lector.Read

            BuscaUnidad = Lector!UnidadID

        Loop
        Lector.Close()

        Lector = Nothing
        Comando = Nothing
       end using
        




    End Function

    Private Sub txtNumero_TextChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles txtNumero.TextChanged

    End Sub

    Private Sub txtNumero_Leave(ByVal sender As Object, ByVal e As System.EventArgs) Handles txtNumero.Leave
        If Existe(PedidosID) Then
            MsgBox("Pedido ya existente !!!")
            txtNumero.Focus()

        Else
            Me.Text = DocumentoNombre & " -  # " & txtNumero.Text

        End If
    End Sub

    Private Function Existe(ByVal mPedidoID As Guid) As Boolean

        Dim Comando As SqlClient.SqlCommand
        Dim Lector As SqlClient.SqlDataReader


        using cnn As New SqlClient.SqlConnection(scnn)
        
        cnn.Open()

        Comando = New SqlClient.SqlCommand("PfrmPedidos_Existe", cnn)
        Comando.CommandType = CommandType.StoredProcedure
        Comando.CommandTimeout = 300
        Comando.Parameters.Add(New System.Data.SqlClient.SqlParameter("@PedidosID", System.Data.SqlDbType.UniqueIdentifier, 16, "PedidosID"))
        Comando.Parameters.Add(New System.Data.SqlClient.SqlParameter("@Numero", System.Data.SqlDbType.BigInt, 8, "Numero"))
        Comando.Parameters("@PedidosID").Value = PedidosID
        Comando.Parameters("@Numero").Value = txtNumero.Text

        Lector = Comando.ExecuteReader
        Existe = False
        Do While Lector.Read
            Existe = True

        Loop
        Lector.Close()
        Comando = Nothing
       end using
        





    End Function

    Private Function BuscarVendedor(ByVal ClienteID As Guid) As Guid

        Dim Comando As SqlClient.SqlCommand
        Dim Lector As SqlClient.SqlDataReader




        using cnn As New SqlClient.SqlConnection(scnn)
        
        cnn.Open()

        Comando = New SqlClient.SqlCommand("PfrmMovi_BuscarVendedor", cnn)
        Comando.CommandType = CommandType.StoredProcedure
        Comando.CommandTimeout = 300
        Comando.Parameters.Add(New System.Data.SqlClient.SqlParameter("@ClienteID", System.Data.SqlDbType.UniqueIdentifier, 16, "ClienteID"))
        Comando.Parameters("@ClienteID").Value = ClienteID


        Lector = Comando.ExecuteReader()
        Do While Lector.Read
            If IsDBNull(Lector!VendedorID) = False Then
                BuscarVendedor = Lector!VendedorID
            End If

        Loop
        Lector.Close()
        Comando = Nothing
       end using
        





    End Function


    Private Function Cliente_Bloqueado() As String

        Dim Comando As SqlClient.SqlCommand
        Dim Lector As SqlClient.SqlDataReader
        Dim v As clsValidar





        v = New clsValidar

        using cnn As New SqlClient.SqlConnection(scnn)
        
        cnn.Open()

        Comando = New SqlClient.SqlCommand("PCliente_Bloqueado", cnn)
        Comando.Parameters.Add(New System.Data.SqlClient.SqlParameter("@ClienteID", System.Data.SqlDbType.UniqueIdentifier, 16, "ClienteID"))
        Comando.CommandType = CommandType.StoredProcedure
        Comando.CommandTimeout = 300
        Comando.Parameters("@ClienteID").Value = Me.LookCliente.EditValue

        Lector = Comando.ExecuteReader
        Cliente_Bloqueado = "N"
        Do While Lector.Read
            Cliente_Bloqueado = Lector!RFC

        Loop
        Lector.Close()
        Comando = Nothing
       end using
        






    End Function

    Private Function ChecaClienteStatus() As Boolean

        Dim Comando As SqlClient.SqlCommand
        Dim Lector As SqlClient.SqlDataReader

        Dim v As clsValidar
        v = New clsValidar
        If v.vFieldVal(LookCliente.Text) = "" Then
            Exit Function
        End If

        using cnn As New SqlClient.SqlConnection(scnn)
        
        cnn.Open()

        ChecaClienteStatus = True
        Comando = New SqlClient.SqlCommand("PChecaClienteStatus", cnn)
        Comando.Parameters.Add(New System.Data.SqlClient.SqlParameter("@ClienteID", System.Data.SqlDbType.UniqueIdentifier, 16, "ClienteID"))
        Comando.CommandType = CommandType.StoredProcedure
        Comando.CommandTimeout = 300
        Comando.Parameters("@ClienteID").Value = LookCliente.EditValue

        Lector = Comando.ExecuteReader
        Do While Lector.Read
            ChecaClienteStatus = Lector!Status
            ObsCliente = v.vFieldVal(Lector!Obs)
        Loop

        Lector.Close()
        Comando = Nothing
       end using
        






    End Function



    Private Function GuavaChecarCobranza() As Boolean

        Dim Comando As SqlClient.SqlCommand
        Dim Lector As SqlClient.SqlDataReader

        Dim v As clsValidar
        v = New clsValidar
        If v.vFieldVal(LookCliente.Text) = "" Then
            Exit Function
        End If


        using cnn As New SqlClient.SqlConnection(scnn)
        
        cnn.Open()

        GuavaChecarCobranza = True
        Comando = New SqlClient.SqlCommand("PGuavaChecarCobranza3", cnn)
        Comando.Parameters.Add(New System.Data.SqlClient.SqlParameter("@ClienteID", System.Data.SqlDbType.UniqueIdentifier, 16, "ClienteID"))
        Comando.Parameters.Add(New System.Data.SqlClient.SqlParameter("@EmpresaID", System.Data.SqlDbType.UniqueIdentifier, 16, "EmpresaID"))
        Comando.CommandType = CommandType.StoredProcedure
        Comando.CommandTimeout = 300
        Comando.Parameters("@ClienteID").Value = LookCliente.EditValue
        Comando.Parameters("@EmpresaID").Value = EmpresaID

        Lector = Comando.ExecuteReader
        Do While Lector.Read

            MsgBox(Lector!Letrero)

            GuavaChecarCobranza = False
        Loop

        Lector.Close()
        Comando = Nothing
       end using
        






    End Function


    Private Sub BuscarPrecio()

        Dim Comando As SqlClient.SqlCommand
        Dim Lector As SqlClient.SqlDataReader
        Dim i As Integer
        Dim v As clsValidar
        v = New clsValidar
        If v.vFieldVal(LookCliente.Text) = "" Then
            Exit Sub
        End If

        using cnn As New SqlClient.SqlConnection(scnn)
        
        cnn.Open()

        Comando = New SqlClient.SqlCommand("PBuscaPrecio", cnn)
        Comando.Parameters.Add(New System.Data.SqlClient.SqlParameter("@ClienteID", System.Data.SqlDbType.UniqueIdentifier, 16, "ClienteID"))
        Comando.CommandType = CommandType.StoredProcedure
        Comando.CommandTimeout = 300
        Comando.Parameters("@ClienteID").Value = LookCliente.EditValue

        Lector = Comando.ExecuteReader
        i = 0
        Do While Lector.Read
            PrecioID = Lector!PrecioID
            Me.LabelDiasCredito.Text = Lector!DiasCredito
            If Lector!DiasCredito = 0 Then
                Me.LabelCondiciones.Text = "CONTADO"
            Else
                Me.LabelCondiciones.Text = "CREDITO"
            End If
            Me.LabelCategoria.Text = "CATEGORIA:" & Lector!Categoria
            i = i + 1
        Loop

        Lector.Close()
        Comando = Nothing
       end using
        






    End Sub


    Private Sub AsignarRegistro()
        Dim Row As GridEXRow = GridEX1.GetRow
        Dim v As clsValidar

        v = New clsValidar

        If IsDBNull(Row.Cells("ProductoID").Value) = False Then
            mProductoID = Row.Cells("ProductoID").Value
        Else
            mProductoID = GrupoID
        End If
        mCantidad = Row.Cells("Cantidad").Value
        mP1 = Row.Cells("P1").Value
        mP2 = Row.Cells("P2").Value
        mP3 = Row.Cells("P3").Value
        mP4 = Row.Cells("P4").Value
        mP5 = Row.Cells("P5").Value
        mP6 = Row.Cells("P6").Value
        mP7 = Row.Cells("P7").Value
        mP8 = Row.Cells("P8").Value
        mP9 = Row.Cells("P9").Value
        mP10 = Row.Cells("P10").Value
        mP11 = Row.Cells("P11").Value
        mP12 = Row.Cells("P12").Value
        mP13 = Row.Cells("P13").Value
        mP14 = Row.Cells("P14").Value
        mP15 = Row.Cells("P15").Value
        mP16 = Row.Cells("P16").Value
        mP17 = Row.Cells("P17").Value
        mP18 = Row.Cells("P18").Value
        mP19 = Row.Cells("P19").Value
        mP20 = Row.Cells("P20").Value
        mP21 = Row.Cells("P21").Value
        mP22 = Row.Cells("P22").Value
        mP23 = Row.Cells("P23").Value
        mP24 = Row.Cells("P24").Value
        mP25 = Row.Cells("P25").Value
        mP26 = Row.Cells("P26").Value
        mP27 = Row.Cells("P27").Value
        mP28 = Row.Cells("P28").Value
        mP29 = Row.Cells("P29").Value
        mP30 = Row.Cells("P30").Value

        If IsDBNull(Row.Cells("UnidadID").Value) = False Then
            mUnidadID = Row.Cells("UnidadID").Value
        Else
            mUnidadID = GrupoID
        End If
        mPrecio = Row.Cells("Precio").Value
        mDescuento = Row.Cells("Descuento").Value
        mIVA = Row.Cells("IVA").Value
        mNeto = Row.Cells("Neto").Value
        mImporte = Row.Cells("Importe").Value
        If IsDBNull(Row.Cells("Obs").Value) = False Then
            mObs = Row.Cells("Obs").Value
        Else
            mObs = ""
        End If
        'mPrecioID = DBNull.Value

        'mPrecioID = Row.Cells("PrecioID").Value
        mFechaEntrega = Row.Cells("FechaEntrega").Value
        mFechaEmbarque = Row.Cells("FechaEmbarque").Value
        mOrdenCliente = v.vFieldVal(Row.Cells("OrdenCliente").Value)
        mObs1 = v.vFieldVal(Row.Cells("Obs1").Value)
        mObs2 = v.vFieldVal(Row.Cells("Obs2").Value)
        mObs3 = v.vFieldVal(Row.Cells("Obs3").Value)
        mSemana = v.vFieldVal(Row.Cells("Semana").Value)

        If IsDBNull(Row.Cells("ClienteCadenaID").Value) = False Then
            mClienteCadenaID = Row.Cells("ClienteCadenaID").Value
        Else
            mClienteCadenaID = GrupoID
        End If

        If IsDBNull(Row.Cells("ClienteCadenaIDEnviar").Value) = False Then
            mClienteCadenaIDEnviar = Row.Cells("ClienteCadenaIDEnviar").Value
        Else
            mClienteCadenaIDEnviar = GrupoID
        End If

        mExplosion = Row.Cells("Explosion").Value
        mPlaneacion = v.vFieldVal(Row.Cells("Planeacion").Value)
        mLugar = Row.Cells("Lugar").Value

        If IsDBNull(Row.Cells("Planeacion").Value) = False Then
            mFPlaneacion = Row.Cells("FPlaneacion").Value
        End If
        mPlanear = Row.Cells("Planear").Value
        mExplosionar = Row.Cells("Explosionar").Value

        If Row.Cells("Explosion").Value > 0 Then
            mFExplosion = Row.Cells("FExplosion").Value
        End If

        If IsDBNull(Row.Cells("FasesID").Value) = False Then
            mFasesID = Row.Cells("FasesID").Value
        Else
            mFasesID = GrupoID
        End If


        mCodigo = Row.Cells("Codigo").Value

    End Sub

    Private Sub CopiarRegistro()
        Me.GridEX1.Focus()
        GridEX1.Row = -1




        GridEX1.SetValue("Codigo", mCodigo)
        GridEX1.SetValue("Cantidad", mCantidad)
        GridEX1.SetValue("P1", mP1)
        GridEX1.SetValue("P2", mP2)
        GridEX1.SetValue("P3", mP3)
        GridEX1.SetValue("P4", mP4)
        GridEX1.SetValue("P5", mP5)
        GridEX1.SetValue("P6", mP6)
        GridEX1.SetValue("P7", mP7)
        GridEX1.SetValue("P8", mP8)
        GridEX1.SetValue("P9", mP9)
        GridEX1.SetValue("P10", mP10)
        GridEX1.SetValue("P11", mP11)
        GridEX1.SetValue("P12", mP12)
        GridEX1.SetValue("P13", mP13)
        GridEX1.SetValue("P14", mP14)
        GridEX1.SetValue("P15", mP15)
        GridEX1.SetValue("P16", mP16)
        GridEX1.SetValue("P17", mP17)
        GridEX1.SetValue("P18", mP18)
        GridEX1.SetValue("P19", mP19)
        GridEX1.SetValue("P20", mP20)
        GridEX1.SetValue("P21", mP21)
        GridEX1.SetValue("P22", mP22)
        GridEX1.SetValue("P23", mP23)
        GridEX1.SetValue("P24", mP24)
        GridEX1.SetValue("P25", mP25)
        GridEX1.SetValue("P26", mP26)
        GridEX1.SetValue("P27", mP27)
        GridEX1.SetValue("P28", mP28)
        GridEX1.SetValue("P29", mP29)
        GridEX1.SetValue("P30", mP30)
        GridEX1.SetValue("Precio", mPrecio)
        GridEX1.SetValue("Descuento", mDescuento)
        GridEX1.SetValue("IVA", mIVA)
        GridEX1.SetValue("Neto", mNeto)
        GridEX1.SetValue("Importe", mImporte)
        GridEX1.SetValue("Obs", mObs)
        GridEX1.SetValue("Obs1", mObs1)
        GridEX1.SetValue("Obs2", mObs2)
        GridEX1.SetValue("Obs3", mObs3)
        GridEX1.SetValue("Semana", mSemana)


        Dim Row As GridEXRow = GridEX1.GetRow

        If RTrim(Row.Cells("ProductoID").Text) = "" Then
            If IsDBNull(mProductoID) = False And mProductoID.CompareTo(GrupoID) <> 0 Then
                GridEX1.SetValue("ProductoID", mProductoID)
            End If
        End If
        Row = Nothing

        If IsDBNull(mUnidadID) = False And mUnidadID.CompareTo(GrupoID) <> 0 Then
            GridEX1.SetValue("UnidadID", mUnidadID)
        End If
        'If IsDBNull(mPrecioID) = False And mPrecioID.CompareTo(GrupoID) <> 0 Then
        '    GridEX1.SetValue("PrecioID", mPrecioID)
        'End If
        If IsDBNull(mFechaEntrega) = False Then
            GridEX1.SetValue("FechaEntrega", mFechaEntrega)
        End If
        If IsDBNull(mFechaEmbarque) = False Then
            GridEX1.SetValue("FechaEmbarque", mFechaEmbarque)
        End If
        GridEX1.SetValue("OrdenCliente", mOrdenCliente)
        If IsDBNull(mClienteCadenaID) = False And mClienteCadenaID.CompareTo(GrupoID) <> 0 Then
            GridEX1.SetValue("ClienteCadenaID", mClienteCadenaID)
        End If

        If IsDBNull(mClienteCadenaIDEnviar) = False And mClienteCadenaIDEnviar.CompareTo(GrupoID) <> 0 Then
            GridEX1.SetValue("ClienteCadenaIDEnviar", mClienteCadenaIDEnviar)
        End If

        GridEX1.SetValue("Explosion", mExplosion)
        If IsDBNull(mPlaneacion) = False And IsNothing(mPlaneacion) = False Then
            GridEX1.SetValue("Planeacion", mPlaneacion)
        End If

        GridEX1.SetValue("Lugar", mLugar)
        If IsDBNull(mPlaneacion) = False And IsNothing(mPlaneacion) = False Then
            GridEX1.SetValue("FPlaneacion", mFPlaneacion)
        End If
        GridEX1.SetValue("Planear", mPlanear)


        If IsDBNull(mFasesID) = False And mFasesID.CompareTo(GrupoID) <> 0 Then
            GridEX1.SetValue("FasesID", mFasesID)
        End If


        GridEX1.SetValue("Explosionar", mExplosionar)

        If mExplosion > 0 Then
            GridEX1.SetValue("FExplosion", mFExplosion)
        End If


        Me.GridEX1.CurrentColumn = Me.GridEX1.RootTable.Columns("Codigo")

        CalculateTotals()
    End Sub

    Private Sub QuitarDinero()
        txtSubTotal.Visible = False
        LabDescuentoDinero.Visible = False
        txtDescuentoDinero.Visible = False
        LabImpuestoDinero.Visible = False
        txtImpuestoDinero1.Visible = False
        LabT2.Visible = False
        txtT21.Visible = False

    End Sub

    Private Sub Configura2()

        Dim Comando As SqlClient.SqlCommand
        Dim Lector As SqlClient.SqlDataReader
        Dim VerDinero As Boolean

        Dim cnn2 As New SqlClient.SqlConnection
        cnn2.ConnectionString = Scnn
        cnn2.Open()

        Comando = New SqlClient.SqlCommand("PfrmMovi_ConsultaEmpresa_Documento", cnn2)
        Comando.CommandType = CommandType.StoredProcedure
        Comando.CommandTimeout = 300
        Comando.Parameters.Add(New System.Data.SqlClient.SqlParameter("@EmpresaID", System.Data.SqlDbType.UniqueIdentifier, 16, "EmpresaID"))
        Comando.Parameters.Add(New System.Data.SqlClient.SqlParameter("@DocumentoID", System.Data.SqlDbType.TinyInt, 1, "DocumentoID"))
        Comando.Parameters("@EmpresaID").Value = EmpresaID
        Comando.Parameters("@DocumentoID").Value = DocumentoID


        Lector = Comando.ExecuteReader()
        Do While Lector.Read
            VerDinero = Lector!VerDinero
            IVA = Lector!IVA

        Loop
        Lector.Close()
        Comando = Nothing

        cnn2.Close()
        cnn2 = Nothing


        GridEX1.RootTable.Columns("Renglon").Visible = True

        GridEX1.RootTable.Columns("Precio").Visible = True
        GridEX1.RootTable.Columns("Descuento").Visible = True
        GridEX1.RootTable.Columns("IVA").Visible = True
        GridEX1.RootTable.Columns("Neto").Visible = True
        'GridEX1.RootTable.Columns("PrecioID").Visible = True

        LabTotal.Visible = True
        txtTotal1.Visible = True



    End Sub


    Protected Overrides Sub OnResize(ByVal e As System.EventArgs)
        Me.GridEX1.Height = Me.Height - Panel1.Height - GroupBoxCliente.Height - 60
    End Sub

    Private Sub PonerProducto()
        Dim RowD As GridEXRow = GridEX1.DropDowns("Producto").GetRow


        GridEX1.SetValue("Codigo", RowD.Cells("Codigo").Value)
        IrAPonerCorrida()
    End Sub

    Private Function BuscaCorrida() As Guid
        Dim Row As GridEXRow = GridEX1.GetRow
        Dim rs As SqlClient.SqlDataReader
        Dim Comando As SqlClient.SqlCommand


        Dim cnn2 As New SqlClient.SqlConnection
        cnn2.ConnectionString = Scnn
        cnn2.Open()


        Comando = New SqlClient.SqlCommand("PBuscaCorrida", cnn2)
        Comando.CommandType = CommandType.StoredProcedure
        Comando.CommandTimeout = 300

        Comando.Parameters.Add(New System.Data.SqlClient.SqlParameter("@ProductoID", System.Data.SqlDbType.UniqueIdentifier, 16, "ProductoID"))
        Comando.Parameters("@ProductoID").Value = Row.Cells("ProductoID").Value


        rs = Comando.ExecuteReader

        Do While rs.Read()


            BuscaCorrida = rs!CorridaID

        Loop

        Comando = Nothing
        cnn2.Close()
        cnn2 = Nothing



    End Function

    Private Function SiguienteRenglon() As Long

        Dim cmd As SqlClient.SqlCommand
        Dim rs As SqlClient.SqlDataReader
        Dim V As clsValidar
        Dim Row As GridEXRow = GridEX1.GetRow


        using cnn As New SqlClient.SqlConnection(scnn)
        
        cnn.Open()

        V = New clsValidar

        cmd = New SqlClient.SqlCommand("PfrmPedidos_SiguienteRenglon", cnn)
        cmd.CommandType = CommandType.StoredProcedure
        cmd.Parameters.Add(New System.Data.SqlClient.SqlParameter("@PedidosID", System.Data.SqlDbType.UniqueIdentifier, 16, "PedidosID"))
        cmd.Parameters("@PedidosID").Value = PedidosID

        rs = cmd.ExecuteReader()
        SiguienteRenglon = 1
        Do While rs.Read
            If IsDBNull(rs!Maximo) = False Then
                SiguienteRenglon = V.IniLong(rs!Maximo)
            End If
        Loop
        rs.Close()
        cmd = Nothing
       end using
        


    End Function

    Private Function SiguienteNC() As Long

        Dim cmd As SqlClient.SqlCommand
        Dim rs As SqlClient.SqlDataReader
        Dim V As clsValidar
        Dim Row As GridEXRow = GridEX1.GetRow


        using cnn As New SqlClient.SqlConnection(scnn)
        
        cnn.Open()

        V = New clsValidar

        cmd = New SqlClient.SqlCommand("PfrmPedidos_SiguienteNC", cnn)
        cmd.CommandType = CommandType.StoredProcedure
        cmd.Parameters.Add(New System.Data.SqlClient.SqlParameter("@EmpresaID", System.Data.SqlDbType.UniqueIdentifier, 16, "EmpresaID"))
        cmd.Parameters("@EmpresaID").Value = EmpresaID

        rs = cmd.ExecuteReader()
        SiguienteNC = 1
        Do While rs.Read
            If IsDBNull(rs!Maximo) = False Then
                SiguienteNC = V.IniLong(rs!Maximo)
            End If
        Loop
        rs.Close()
        cmd = Nothing
       end using
        


    End Function



    Private Sub BtnEliminar_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles BtnEliminar.Click
        Regresar()


    End Sub

    Private Sub btnUpdate_Click_1(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnUpdate.Click
        If Me.LookAlmacen.Text = "" Or IsDBNull(LookAlmacen.EditValue) = True Then
            Me.LookAlmacen.Focus()

            MsgBox("Necesitas indicar el almacén primero !!!")
            Exit Sub

        End If
        Adios()

    End Sub

    Private Sub Finalizar()
        CalculateTotals()
        If Nuevo Then
            If ExistePedidoReferencia() Then
                Me.txtReferencia.Focus()
                Exit Sub
            End If

        End If

        AfectarOtrosCampos()

        UpdateChanges()


        If BDesglosar Then
            ArreglarRenglonDelPedido()
        End If
        Pedidos_Actualiza()

        Dim f As New frmPedido_Imprimir
        f.Iniciar(Me.txtNumero.Text, Me.LookCliente.EditValue)
        f.ShowDialog()

        If f.Continuar = True Then
            Close()
        End If



    End Sub

    Private Sub ConfigurarFases()
        Dim Row As GridEXRow = GridEX1.GetRow

        Dim Comando As SqlClient.SqlCommand
        Dim Lector As SqlClient.SqlDataReader
        Dim v As clsValidar
        Dim cnn2 As New SqlClient.SqlConnection
        cnn2.ConnectionString = Scnn
        cnn2.Open()


        v = New clsValidar


        Comando = New SqlClient.SqlCommand("PfrmParametrosGenerales", cnn2)
        Comando.CommandType = CommandType.StoredProcedure
        Comando.CommandTimeout = 300
        Comando.Parameters.Add(New System.Data.SqlClient.SqlParameter("@EmpresaID", System.Data.SqlDbType.UniqueIdentifier, 16, "EmpresaID"))
        Comando.Parameters("@EmpresaID").Value = EmpresaID

        Lector = Comando.ExecuteReader
        BPoderModificarFechaDelPedido = True
        PPrecioTope = 0

        Do While (Lector.Read)
            Me.BExigirPrecioEnPedido = Lector!ExigirPrecioEnPedido
            Me.BPoderModificarFechaDelPedido = Lector!PoderModificarFechaDelPedido
            Me.BManejoDeComercializadora = Lector!ManejoDeComercializadora
            'PPrecioTope = Lector!PPrecioTope
            Me.DiasAdelanteEnPedidos = Lector!DiasAdelanteEnPedidos

            If IsDBNull(Lector!Fase1) Then
                GridEX1.RootTable.Columns("fase1").Visible = False
            Else
                If v.vFieldVal(Lector!Fase1) = "" Then
                    GridEX1.RootTable.Columns("Fase1").Visible = False
                Else
                    GridEX1.RootTable.Columns("Fase1").Caption = Lector!Fase1
                    GridEX1.RootTable.Columns("Fase1").Visible = True
                End If
            End If
            If IsDBNull(Lector!Fase2) Then
                GridEX1.RootTable.Columns("fase2").Visible = False
            Else
                If v.vFieldVal(Lector!fase2) = "" Then
                    GridEX1.RootTable.Columns("fase2").Visible = False
                Else
                    GridEX1.RootTable.Columns("fase2").Caption = Lector!fase2
                    GridEX1.RootTable.Columns("fase2").Visible = True
                End If
            End If
            If IsDBNull(Lector!Fase3) Then
                GridEX1.RootTable.Columns("fase3").Visible = False
            Else
                If v.vFieldVal(Lector!Fase3) = "" Then
                    GridEX1.RootTable.Columns("Fase3").Visible = False
                Else
                    GridEX1.RootTable.Columns("Fase3").Caption = Lector!Fase3
                    GridEX1.RootTable.Columns("Fase3").Visible = True
                End If
            End If
            If IsDBNull(Lector!Fase4) Then
                GridEX1.RootTable.Columns("fase4").Visible = False
            Else
                If v.vFieldVal(Lector!Fase4) = "" Then
                    GridEX1.RootTable.Columns("Fase4").Visible = False
                Else
                    GridEX1.RootTable.Columns("Fase4").Caption = Lector!Fase4
                    GridEX1.RootTable.Columns("Fase4").Visible = True
                End If
            End If
            If IsDBNull(Lector!Fase5) Then
                GridEX1.RootTable.Columns("fase5").Visible = False
            Else
                If v.vFieldVal(Lector!Fase5) = "" Then
                    GridEX1.RootTable.Columns("Fase5").Visible = False
                Else
                    GridEX1.RootTable.Columns("Fase5").Caption = Lector!Fase5
                    GridEX1.RootTable.Columns("Fase5").Visible = True
                End If
            End If
            If IsDBNull(Lector!Fase6) Then
                GridEX1.RootTable.Columns("fase6").Visible = False
            Else
                If v.vFieldVal(Lector!Fase6) = "" Then
                    GridEX1.RootTable.Columns("Fase6").Visible = False
                Else
                    GridEX1.RootTable.Columns("Fase6").Caption = Lector!Fase6
                    GridEX1.RootTable.Columns("Fase6").Visible = True
                End If
            End If
            If IsDBNull(Lector!Fase7) Then
                GridEX1.RootTable.Columns("fase7").Visible = False
            Else
                If v.vFieldVal(Lector!Fase7) = "" Then
                    GridEX1.RootTable.Columns("Fase7").Visible = False
                Else
                    GridEX1.RootTable.Columns("Fase7").Caption = Lector!Fase7
                    GridEX1.RootTable.Columns("Fase7").Visible = True
                End If
            End If
            If IsDBNull(Lector!Fase8) Then
                GridEX1.RootTable.Columns("fase8").Visible = False
            Else
                If v.vFieldVal(Lector!Fase8) = "" Then
                    GridEX1.RootTable.Columns("Fase8").Visible = False
                Else
                    GridEX1.RootTable.Columns("Fase8").Caption = Lector!Fase8
                    GridEX1.RootTable.Columns("Fase8").Visible = True
                End If
            End If
            If IsDBNull(Lector!Fase9) Then
                GridEX1.RootTable.Columns("fase9").Visible = False
            Else
                If v.vFieldVal(Lector!Fase9) = "" Then
                    GridEX1.RootTable.Columns("Fase9").Visible = False
                Else
                    GridEX1.RootTable.Columns("Fase9").Caption = Lector!Fase9
                    GridEX1.RootTable.Columns("Fase9").Visible = True
                End If
            End If
            If IsDBNull(Lector!Fase10) Then
                GridEX1.RootTable.Columns("fase10").Visible = False
            Else
                If v.vFieldVal(Lector!Fase10) = "" Then
                    GridEX1.RootTable.Columns("Fase10").Visible = False
                Else
                    GridEX1.RootTable.Columns("Fase10").Caption = Lector!Fase10
                    GridEX1.RootTable.Columns("Fase10").Visible = True
                End If
            End If
            Me.BDesglosarPedido = Lector!DesglosarEnPedido
        Loop
        Lector.Close()
        Comando = Nothing


        cnn2.Close()
        cnn2 = Nothing


    End Sub

    Private Function ExistePedidoReferencia() As Boolean
        Dim Row As GridEXRow = GridEX1.GetRow
        Dim rs As SqlClient.SqlDataReader
        Dim Comando As SqlClient.SqlCommand



        using cnn As New SqlClient.SqlConnection(scnn)
        
        cnn.Open()

        Comando = New SqlClient.SqlCommand("PExistePedidoReferencia", cnn)
        Comando.CommandType = CommandType.StoredProcedure
        Comando.CommandTimeout = 300

        Comando.Parameters.Add(New System.Data.SqlClient.SqlParameter("@EmpresaID", System.Data.SqlDbType.UniqueIdentifier, 16, "EmpresaID"))
        Comando.Parameters.Add(New System.Data.SqlClient.SqlParameter("@ClienteID", System.Data.SqlDbType.UniqueIdentifier, 16, "ClienteID"))
        Comando.Parameters.Add(New System.Data.SqlClient.SqlParameter("@Referencia", System.Data.SqlDbType.VarChar, 50, "Referencia"))
        Comando.Parameters("@EmpresaID").Value = EmpresaID
        Comando.Parameters("@ClienteID").Value = LookCliente.EditValue
        Comando.Parameters("@Referencia").Value = Me.txtReferencia.Text



        rs = Comando.ExecuteReader
        ExistePedidoReferencia = False
        Do While rs.Read()
            If Me.txtNumero.Text <> rs!Numero Then
                MsgBox("Pedido ya existente, capturado el : " & rs!Fecha)
                ExistePedidoReferencia = True
            End If

        Loop
        rs.Close()
        Comando = Nothing
       end using
        



    End Function


    Private Sub Button1_Click(ByVal sender As System.Object, ByVal e As System.EventArgs)
        Dim Row As GridEXRow = GridEX1.GetRow
        If Row.Cells("Renglon").Value = 0 Then
            GridEX1.SetValue("Renglon", SiguienteRenglon)
            GridEX1.UpdateData()
        End If

        '''' Me.GroupBox1.Visible = False
        Me.GridEX1.Focus()
        Me.GridEX1.Row = -1
        Me.GridEX1.CurrentColumn = Me.GridEX1.RootTable.Columns("ProductoID")

    End Sub

    Private Sub GroupBox1_Enter(ByVal sender As System.Object, ByVal e As System.EventArgs)

    End Sub

    Private Sub Button2_Click(ByVal sender As System.Object, ByVal e As System.EventArgs)

        If Me.ActiveView.Exitoso = False Then
            MsgBox("Necesitas desglosar la cantidad total para poder continuar !!!")
        Else
            BDesglosar = True
            '        If Me.PedidoDesglosadoView1.Desglozado Then
            JalarDesglose()

            ''''   Me.GroupBox1.Visible = False
            '       Else
            '          MsgBox("Necesitas desglozar todas las tallas primero !!!")

            '     End If
            Me.GridEX1.Focus()
            If Posicion Then
                Me.GridEX1.Row = Fila
            Else
                Me.GridEX1.Row = -1
            End If
            Me.GridEX1.CurrentColumn = Me.GridEX1.RootTable.Columns("ProductoID")

        End If

    End Sub


    Private Sub BorrarPedidoDesglosado()

        Dim Comando As SqlClient.SqlCommand
        Dim v As clsValidar
        v = New clsValidar

        using cnn As New SqlClient.SqlConnection(scnn)
        
        cnn.Open()

        Comando = New SqlClient.SqlCommand("PBorrarPedidoDesglosado", cnn)
        Comando.CommandType = CommandType.StoredProcedure
        Comando.CommandTimeout = 300
        Comando.Parameters.Add(New System.Data.SqlClient.SqlParameter("@Pedidos_DetailsID", System.Data.SqlDbType.UniqueIdentifier, 16, "Pedidos_DetailsID"))
        Comando.Parameters("@Pedidos_DetailsID").Value = Pedidos_DetailsID
        Comando.ExecuteNonQuery()

        Comando = Nothing
       end using
        





    End Sub





    Private Function BuscarPedidoProgramado(ByVal PedidosID As Guid) As Boolean
        Dim Row As GridEXRow = GridEX1.GetRow
        Dim rs As SqlClient.SqlDataReader
        Dim Comando As SqlClient.SqlCommand


        Dim cnn2 As New SqlClient.SqlConnection
        cnn2.ConnectionString = Scnn
        cnn2.Open()


        Comando = New SqlClient.SqlCommand("PBuscarPedidoProgramado", cnn2)
        Comando.CommandType = CommandType.StoredProcedure
        Comando.CommandTimeout = 300

        Comando.Parameters.Add(New System.Data.SqlClient.SqlParameter("@PedidosID", System.Data.SqlDbType.UniqueIdentifier, 16, "PedidosID"))
        Comando.Parameters("@PedidosID").Value = PedidosID



        rs = Comando.ExecuteReader
        BuscarPedidoProgramado = False
        Do While rs.Read()
            BuscarPedidoProgramado = True

        Loop
        rs.Close()
        Comando = Nothing
        cnn2.Close()
        cnn2 = Nothing


    End Function

    Private Sub GroupBoxCliente_Enter(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles GroupBoxCliente.Enter

    End Sub

    Private Sub txtReferencia_TextChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles txtReferencia.TextChanged

    End Sub


    Private Sub btnModificarPrecios_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnModificarPrecios.Click
        Dim fAutorizar As frmAutorizar

        fAutorizar = New frmAutorizar
        fAutorizar.Opcion = "CambiarPrecios"
        fAutorizar.ShowDialog()

        If fAutorizar.Respuesta = False Then
            MsgBox("Password no existente !!!")
        Else
            If fAutorizar.CambiarPrecios Then
                Me.GridEX1.RootTable.Columns("Precio").EditType = EditType.TextBox
                BPrecioAutorizado = True
            Else
                BPrecioAutorizado = False
                MsgBox("Password sin autorización !!!")
            End If
        End If




    End Sub


    Private Sub RestaurarCalendarioProduccion()
        Me.Dataset51.PCalendarioProduccionPedidos.Clear()

        Me.GridEX2.SetDataBinding(Nothing, "")
        Me.sqlPCalendarioProduccionPedidos.Parameters("@PedidosID").Value = PedidosID
        Me.daPCalendarioProduccionPedidos.Fill(Me.Dataset51.PCalendarioProduccionPedidos)
        mData2 = New DataView(Me.Dataset51.PCalendarioProduccionPedidos)
        Me.GridEX2.SetDataBinding(mData2, "")



    End Sub



    Private Sub Desglosar()
        Dim Row As GridEXRow = GridEX1.GetRow
        If PoderDesglosar Then
            Me.ChecarTamañoDelLote = True

            ''''    Me.GroupBox1.Visible = True
            '''' Me.Label19.Text = Row.Cells("ProductoID").Text

            '''' Me.LabHorma.Text = PHorma_Busca_Por_Producto(Row.Cells("ProductoID").Value)


            Pedidos_DetailsID = Row.Cells("Pedidos_DetailsID").Value
            Me.ActiveView = App.DataManager.CreateView(CatalogType.PedidoDesglosado)
            ''''Me.Button1.Focus()
            CalculateTotals()
        End If


    End Sub








    Private Sub ArreglarRenglonDelPedido()

        Dim Comando As SqlClient.SqlCommand

        Dim v As clsValidar
        v = New clsValidar

        using cnn As New SqlClient.SqlConnection(scnn)
        
        cnn.Open()

        Comando = New SqlClient.SqlCommand("PArreglarRenglonDelPedido", cnn)
        Comando.CommandType = CommandType.StoredProcedure
        Comando.CommandTimeout = 300
        Comando.Parameters.Add(New System.Data.SqlClient.SqlParameter("@PedidosID", System.Data.SqlDbType.UniqueIdentifier, 16, "PedidosID"))
        Comando.Parameters("@PedidosID").Value = PedidosID
        Comando.ExecuteNonQuery()

        Comando = Nothing
       end using
        





    End Sub



    Private Sub RestaurarConcentrado()
        If Me.DockPanel1.Visibility = DevExpress.XtraBars.Docking.DockVisibility.Visible Then
            Me.Pedidos_Details_VerConcentradoView1.Iniciar(Scnn, GrupoID, PedidosID)

        End If

    End Sub
    Private Sub RestaurarInventario()
        Dim Row As GridEXRow = GridEX1.GetRow

        If Me.DockPanel2.Visibility = DevExpress.XtraBars.Docking.DockVisibility.Visible Then
            If IsDBNull(Row.Cells("Pedidos_DetailsID").Value) = False Then
                Me.Pedidos_Details_VerInven1.Iniciar(Scnn, GrupoID, Row.Cells("Pedidos_DetailsID").Value)
            End If
        End If

    End Sub


    Private Sub MenuItem2_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MenuItem2.Click
        Me.DockPanel1.Visibility = DevExpress.XtraBars.Docking.DockVisibility.Visible
        RestaurarConcentrado()
    End Sub

    Private Sub MenuItem7_Click(ByVal sender As System.Object, ByVal e As System.EventArgs)

    End Sub

    Private Sub MenuItem10_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MenuItem10.Click
        Dim CurrentRow As GridEXRow = GridEX1.GetRow

        If GridEX1.Row >= 0 Then
            If PoderDesglosar Then
                If Me.BExigirPrecioEnPedido Then

                    If CurrentRow.Cells("Precio").Value <= 1 Then
                        MsgBox("Necesita indicar el precio !!!")
                        Exit Sub
                    End If
                End If

                Posicion = True
                Fila = GridEX1.Row
                AsignarRegistro()
                Desglosar()

            Else
                MsgBox("Necesitas el permiso para poder desglosar pedidos !!!")
            End If
        End If
    End Sub


    Private Function BuscarPedidoYaConcentrado() As Boolean
        Dim Row As GridEXRow = GridEX1.GetRow
        Dim rs As SqlClient.SqlDataReader
        Dim Comando As SqlClient.SqlCommand



        using cnn As New SqlClient.SqlConnection(scnn)
        
        cnn.Open()

        Comando = New SqlClient.SqlCommand("PBuscarPedidoYaConcentrado", cnn)
        Comando.CommandType = CommandType.StoredProcedure
        Comando.CommandTimeout = 300

        Comando.Parameters.Add(New System.Data.SqlClient.SqlParameter("@PedidosID", System.Data.SqlDbType.UniqueIdentifier, 16, "PedidosID"))
        Comando.Parameters("@PedidosID").Value = PedidosID



        rs = Comando.ExecuteReader
        BuscarPedidoYaConcentrado = False
        Do While rs.Read()
            If rs!Concentrado = "S" Then
                BuscarPedidoYaConcentrado = True
            Else
                BuscarPedidoYaConcentrado = False
            End If


        Loop
        rs.Close()
        Comando = Nothing
       end using
        



    End Function
    Private Function BuscarPedidoYaDesglosado() As Boolean
        Dim Row As GridEXRow = GridEX1.GetRow
        Dim rs As SqlClient.SqlDataReader
        Dim Comando As SqlClient.SqlCommand



        using cnn As New SqlClient.SqlConnection(scnn)
        
        cnn.Open()

        Comando = New SqlClient.SqlCommand("PBuscarPedidoYaDesglosado", cnn)
        Comando.CommandType = CommandType.StoredProcedure
        Comando.CommandTimeout = 300

        Comando.Parameters.Add(New System.Data.SqlClient.SqlParameter("@PedidosID", System.Data.SqlDbType.UniqueIdentifier, 16, "PedidosID"))
        Comando.Parameters("@PedidosID").Value = PedidosID



        rs = Comando.ExecuteReader
        BuscarPedidoYaDesglosado = False
        Do While rs.Read()
            If rs!Concentrado = "S" Then
                BuscarPedidoYaDesglosado = True
            Else
                BuscarPedidoYaDesglosado = False
            End If


        Loop
        rs.Close()
        Comando = Nothing
       end using
        



    End Function

    Private Sub MenuItem7_Click_1(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MenuItem7.Click

        Dim Comando As SqlClient.SqlCommand
        Dim v As clsValidar
        v = New clsValidar

        using cnn As New SqlClient.SqlConnection(scnn)
        
        cnn.Open()

        Comando = New SqlClient.SqlCommand("PPedidos_DetailsYaConcentrar", cnn)
        Comando.CommandType = CommandType.StoredProcedure
        Comando.CommandTimeout = 300
        Comando.Parameters.Add(New System.Data.SqlClient.SqlParameter("@PedidosID", System.Data.SqlDbType.UniqueIdentifier, 16, "PedidosID"))
        Comando.Parameters("@PedidosID").Value = PedidosID
        Comando.ExecuteNonQuery()

        Comando = Nothing
       end using
        





        MsgBox("Pedido concentrado exitosamente !!!")
        Adios()
    End Sub


    Private Sub MenuItem11_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MenuItem11.Click

        Dim Comando As SqlClient.SqlCommand
        Dim v As clsValidar
        v = New clsValidar

        using cnn As New SqlClient.SqlConnection(scnn)
        
        cnn.Open()

        Comando = New SqlClient.SqlCommand("PPedidos_DetailsYaRestaurar", cnn)
        Comando.CommandType = CommandType.StoredProcedure
        Comando.Parameters.Add(New System.Data.SqlClient.SqlParameter("@PedidosID", System.Data.SqlDbType.UniqueIdentifier, 16, "PedidosID"))
        Comando.Parameters("@PedidosID").Value = PedidosID
        Comando.ExecuteNonQuery()

        Comando = Nothing
       end using
        




        MsgBox("Pedido restaurado exitosamente !!!")
        Adios()
    End Sub



    Private Sub Adios()
        Dim V As clsValidar
        Dim f As New frmPedido_TamañoDelLote
        Dim BTamañoDelLote As Boolean

        Dim Row As GridEXRow = GridEX1.GetRow
        Dim rs As SqlClient.SqlDataReader
        Dim Comando As SqlClient.SqlCommand


        If BDesglosarPedido Then
            If Me.ChecarTamañoDelLote Then

                using cnn As New SqlClient.SqlConnection(scnn)
                
                cnn.Open()

                Comando = New SqlClient.SqlCommand("PPedidos_TamañoDelLote", cnn)
                Comando.CommandType = CommandType.StoredProcedure
                Comando.CommandTimeout = 300

                Comando.Parameters.Add(New System.Data.SqlClient.SqlParameter("@PedidosID", System.Data.SqlDbType.UniqueIdentifier, 16, "PedidosID"))
                Comando.Parameters("@PedidosID").Value = PedidosID


                rs = Comando.ExecuteReader
                BTamañoDelLote = True
                Do While rs.Read()
                    BTamañoDelLote = False

                Loop
                rs.Close()
                Comando = Nothing
               end using
                


                If BTamañoDelLote = False Then
                    f.Iniciar(PedidosID)
                    f.ShowDialog()
                    If f.Continuar = False Then

                        Exit Sub
                    End If
                End If

            End If

        End If
        V = New clsValidar



        If Me.GridEX1.RowCount = 0 Then
            MsgBox("Necesitas capturar los productos !!!")

            'Me.SplashPanel1.PopUpOnTray()
            Me.GridEX1.Focus()
            Me.GridEX1.CurrentColumn = Me.GridEX1.RootTable.Columns("Codigo")

            Exit Sub

        End If
        If V.vFieldVal(Me.txtReferencia.Text) = "" Then
            'Me.SplashPanel1.ToString()
            MsgBox("Necesitas capturar la referencia !!!")

            'Me.SplashPanel1.PopUpOnTray()
            Me.txtReferencia.Focus()
            Exit Sub
        End If
        If V.vFieldVal(Me.LookCliente.Text) = "" Then
            MsgBox("Necesitas capturar el cliente !!!")

            Me.LookCliente.Focus()
            Exit Sub
        End If
        If V.vFieldVal(Me.ComboVendedor.Text) = "" Then
            MsgBox("Necesitas capturar al vendedor !!!")

            Me.ComboVendedor.Focus()
            Exit Sub
        End If

        If V.vFieldVal(Me.cmdEmpacado.Text) = "" Then
            MsgBox("Necesitas indicar el empaque !!!")

            Me.cmdEmpacado.Focus()
            Exit Sub
        End If

        If V.vFieldVal(Me.LookEnvioA.Text) = "" Or Len(Me.LookEnvioA.Text) <= 7 Then
            MsgBox("Necesitas indicar los datos de envío !!!")

            Me.LookEnvioA.Focus()
            Exit Sub
        End If

        If V.vFieldVal(Me.LookAlmacen.Text) = "[Vacío]" Or V.vFieldVal(Me.LookAlmacen.Text) = "" Then
            MsgBox("Necesitas indicar el almacén !!!")

            Me.LookAlmacen.Focus()
            Exit Sub
        End If

        Finalizar()

    End Sub






    Private Sub MenuItem4_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MenuItem4.Click
        Adios()
    End Sub

    Private Sub Regresar()
        If Nuevo Then
            If GridEX1.RecordCount = 0 Then
                ' MainForm.ActiveView.Delete(mDataRow("PedidosID"))
                ' MainForm.ActiveView.UpdateData()
                ' mDataRow = Nothing
                Close()
            Else
                MsgBox("Necesitas eliminar los productos primero !!!")
                'Me.SplashPanel1.PopUpOnTray()

            End If
        Else
            If mDataRow("Numero") = (SiguienteNumero() - 1) And GridEX1.RecordCount = 0 Then
                MainForm.ActiveView.Delete(mDataRow("PedidosID"))
                MainForm.ActiveView.UpdateData()
                mDataRow = Nothing

            End If
            Close()
        End If

    End Sub

    Private Sub MenuItem5_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MenuItem5.Click
        Regresar()
    End Sub

    Private Sub GridEX2_FormattingRow(ByVal sender As System.Object, ByVal e As Janus.Windows.GridEX.RowLoadEventArgs) Handles GridEX2.FormattingRow

    End Sub

    Private Sub MenuItem12_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MenuItem12.Click
        Me.DockPanel2.Visibility = DevExpress.XtraBars.Docking.DockVisibility.Visible
        RestaurarInventario()

    End Sub

    Private Sub MenuItem13_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MenuItem13.Click

        Dim Comando As SqlClient.SqlCommand
        Dim Lector As SqlClient.SqlDataReader
        Dim v As clsValidar
        Dim Row As GridEXRow = GridEX1.GetRow
        Dim Renglon As Long


        If GridEX1.Row <= -1 Then
            Exit Sub
        End If
        v = New clsValidar

        using cnn As New SqlClient.SqlConnection(scnn)
        
        cnn.Open()

        Comando = New SqlClient.SqlCommand("PInven_PedirABodega", cnn)
        Comando.CommandType = CommandType.StoredProcedure
        Comando.Parameters.Add(New System.Data.SqlClient.SqlParameter("@Pedidos_DetailsID", System.Data.SqlDbType.UniqueIdentifier, 16, "Pedidos_DetailsID"))
        Comando.CommandTimeout = 300
        Comando.Parameters("@Pedidos_DetailsID").Value = Row.Cells("Pedidos_DetailsID").Value


        Lector = Comando.ExecuteReader()
        Do While Lector.Read
            If Lector!EnBodega = 0 Then
                If Lector!e > 0 Then
                    AsignarRegistro()


                    Me.GridEX1.SetValue("P1", Lector!e1)
                    Me.GridEX1.SetValue("P2", Lector!e2)
                    Me.GridEX1.SetValue("P3", Lector!e3)
                    Me.GridEX1.SetValue("P4", Lector!e4)
                    Me.GridEX1.SetValue("P5", Lector!e5)
                    Me.GridEX1.SetValue("P6", Lector!e6)
                    Me.GridEX1.SetValue("P7", Lector!e7)
                    Me.GridEX1.SetValue("P8", Lector!e8)
                    Me.GridEX1.SetValue("P9", Lector!e9)
                    Me.GridEX1.SetValue("P10", Lector!e10)
                    Me.GridEX1.SetValue("P11", Lector!e11)
                    Me.GridEX1.SetValue("P12", Lector!e12)
                    Me.GridEX1.SetValue("P13", Lector!e13)
                    Me.GridEX1.SetValue("P14", Lector!e14)
                    Me.GridEX1.SetValue("P15", Lector!e15)
                    Me.GridEX1.SetValue("P16", Lector!e16)
                    Me.GridEX1.SetValue("P17", Lector!e17)
                    Me.GridEX1.SetValue("P18", Lector!e18)
                    Me.GridEX1.SetValue("P19", Lector!e19)
                    Me.GridEX1.SetValue("P20", Lector!e20)
                    Me.GridEX1.SetValue("P21", Lector!e21)
                    Me.GridEX1.SetValue("P22", Lector!e22)
                    Me.GridEX1.SetValue("P23", Lector!e23)
                    Me.GridEX1.SetValue("P24", Lector!e24)
                    Me.GridEX1.SetValue("P25", Lector!e25)
                    Me.GridEX1.SetValue("P26", Lector!e26)
                    Me.GridEX1.SetValue("P27", Lector!e27)
                    Me.GridEX1.SetValue("P28", Lector!e28)
                    Me.GridEX1.SetValue("P29", Lector!e29)
                    Me.GridEX1.SetValue("P30", Lector!e30)
                    Me.GridEX1.SetValue("Cantidad", Lector!e)
                    Me.GridEX1.UpdateData()

                    Me.GridEX1.Row = -1

                    Renglon = SiguienteNumero()

                    CopiarRegistro()
                    Me.GridEX1.SetValue("P1", Lector!f1)
                    Me.GridEX1.SetValue("P2", Lector!f2)
                    Me.GridEX1.SetValue("P3", Lector!f3)
                    Me.GridEX1.SetValue("P4", Lector!f4)
                    Me.GridEX1.SetValue("P5", Lector!f5)
                    Me.GridEX1.SetValue("P6", Lector!f6)
                    Me.GridEX1.SetValue("P7", Lector!f7)
                    Me.GridEX1.SetValue("P8", Lector!f8)
                    Me.GridEX1.SetValue("P9", Lector!f9)
                    Me.GridEX1.SetValue("P10", Lector!f10)
                    Me.GridEX1.SetValue("P11", Lector!f11)
                    Me.GridEX1.SetValue("P12", Lector!f12)
                    Me.GridEX1.SetValue("P13", Lector!f13)
                    Me.GridEX1.SetValue("P14", Lector!f14)
                    Me.GridEX1.SetValue("P15", Lector!f15)
                    Me.GridEX1.SetValue("P16", Lector!f16)
                    Me.GridEX1.SetValue("P17", Lector!f17)
                    Me.GridEX1.SetValue("P18", Lector!f18)
                    Me.GridEX1.SetValue("P19", Lector!f19)
                    Me.GridEX1.SetValue("P20", Lector!f20)
                    Me.GridEX1.SetValue("P21", Lector!f21)
                    Me.GridEX1.SetValue("P22", Lector!f22)
                    Me.GridEX1.SetValue("P23", Lector!f23)
                    Me.GridEX1.SetValue("P24", Lector!f24)
                    Me.GridEX1.SetValue("P25", Lector!f25)
                    Me.GridEX1.SetValue("P26", Lector!f26)
                    Me.GridEX1.SetValue("P27", Lector!f27)
                    Me.GridEX1.SetValue("P28", Lector!f28)
                    Me.GridEX1.SetValue("P29", Lector!f29)
                    Me.GridEX1.SetValue("P30", Lector!f30)
                    Me.GridEX1.SetValue("Cantidad", Lector!f)

                    Me.GridEX1.SetValue("Renglon", Renglon)
                    Renglon = Renglon + 1
                    Me.GridEX1.UpdateData()


                    BorrarPedidoDesglosado()
                    '                   MsgBox("Renglon del pedido parcial a bodega exitosamente")

                Else
                    MsgBox("No hay en bodega disponibles !!!")

                End If
            Else

                '                MsgBox("Renglon del Pedido a bodega exitosamente")

            End If
        Loop
        Lector.Close()
        Comando = Nothing
       end using
        





        Me.RestaurarInventario()


    End Sub



    Private Sub JalarDesglose()
        Dim Row As GridEXRow = GridEX1.GetRow
        Dim rs As SqlClient.SqlDataReader
        Dim Comando As SqlClient.SqlCommand


        Dim Renglon As Long

        Dim m As String

        Me.ProgressBar1.Minimum = 0
        Me.ProgressBar1.Value = 0
        Me.ProgressBar1.Visible = True

        using cnn As New SqlClient.SqlConnection(scnn)
        
        cnn.Open()

        Comando = New SqlClient.SqlCommand("PJalarDesglose", cnn)
        Comando.CommandType = CommandType.StoredProcedure
        Comando.CommandTimeout = 300

        Comando.Parameters.Add(New System.Data.SqlClient.SqlParameter("@Pedidos_DetailsID", System.Data.SqlDbType.UniqueIdentifier, 16, "Pedidos_DetailsID"))
        Comando.Parameters("@Pedidos_DetailsID").Value = Pedidos_DetailsID

        PoderEliminar = True
        Me.GridEX1.Delete()

        If Nuevo = False Then
            GridEX1.AllowAddNew = InheritableBoolean.True
        End If
        Me.GridEX1.Row = -1

        rs = Comando.ExecuteReader
        Renglon = SiguienteRenglon()

        Do While rs.Read()
            CopiarRegistro()
            Me.GridEX1.SetValue("P1", rs!P1)
            Me.GridEX1.SetValue("P2", rs!P2)
            Me.GridEX1.SetValue("P3", rs!P3)
            Me.GridEX1.SetValue("P4", rs!P4)
            Me.GridEX1.SetValue("P5", rs!P5)
            Me.GridEX1.SetValue("P6", rs!P6)
            Me.GridEX1.SetValue("P7", rs!P7)
            Me.GridEX1.SetValue("P8", rs!P8)
            Me.GridEX1.SetValue("P9", rs!P9)
            Me.GridEX1.SetValue("P10", rs!P10)
            Me.GridEX1.SetValue("P11", rs!P11)
            Me.GridEX1.SetValue("P12", rs!P12)
            Me.GridEX1.SetValue("P13", rs!P13)
            Me.GridEX1.SetValue("P14", rs!P14)
            Me.GridEX1.SetValue("P15", rs!P15)
            Me.GridEX1.SetValue("P16", rs!P16)
            Me.GridEX1.SetValue("P17", rs!P17)
            Me.GridEX1.SetValue("P18", rs!P18)
            Me.GridEX1.SetValue("P19", rs!P19)
            Me.GridEX1.SetValue("P20", rs!P20)
            Me.GridEX1.SetValue("P21", rs!P21)
            Me.GridEX1.SetValue("P22", rs!P22)
            Me.GridEX1.SetValue("P23", rs!P23)
            Me.GridEX1.SetValue("P24", rs!P24)
            Me.GridEX1.SetValue("P25", rs!P25)
            Me.GridEX1.SetValue("P26", rs!P26)
            Me.GridEX1.SetValue("P27", rs!P27)
            Me.GridEX1.SetValue("P28", rs!P28)
            Me.GridEX1.SetValue("P29", rs!P29)
            Me.GridEX1.SetValue("P30", rs!P30)
            Me.GridEX1.SetValue("Cantidad", rs!Cantidad)
            If Len(CStr(rs!Cuenta)) = 1 Then
                m = "0"
            Else
                If Len(CStr(rs!Cuenta)) = 2 Then
                    m = "00"
                Else
                    If Len(CStr(rs!Cuenta)) = 3 Then
                        m = "000"
                    Else
                        m = "0000"
                    End If
                End If
            End If

            'Me.ProgressBar1.Maximum = rs!Cuenta
            'Me.ProgressBar1.Value = rs!Folio

            Me.GridEX1.SetValue("Notas", CStr(Format(rs!Folio, m)) & " de " & CStr(rs!Cuenta))


            If rs!Renglon > 0 Then
                Me.GridEX1.SetValue("Renglon", rs!Renglon)
            Else
                Me.GridEX1.SetValue("Renglon", Renglon)
                Renglon = Renglon + 1
            End If
            Me.GridEX1.UpdateData()


        Loop
        rs.Close()
        Comando = Nothing
       end using
        



        If Nuevo = False Then
            GridEX1.AllowAddNew = InheritableBoolean.False
        End If

        BorrarPedidoDesglosado()
        '        Me.GridEX1.Row = GridEX1.RowCount - 1
        'i = 0
        'While i < GridEX1.RowCount
        '    GridEX1.Row = i
        '    Row = GridEX1.GetRow
        '    GridEX1.SetValue("Status", Row.Cells("Status").Value)
        '    GridEX1.UpdateData()
        '    i = i + 1
        'End While

        Me.ProgressBar1.Visible = False
    End Sub


    Private Sub InicializarTallas()
        If GridEX1.Row = -1 Then
            GridEX1.SetValue("P1", 0)
            GridEX1.SetValue("P2", 0)
            GridEX1.SetValue("P3", 0)
            GridEX1.SetValue("P4", 0)
            GridEX1.SetValue("P5", 0)
            GridEX1.SetValue("P6", 0)
            GridEX1.SetValue("P7", 0)
            GridEX1.SetValue("P8", 0)
            GridEX1.SetValue("P9", 0)
            GridEX1.SetValue("P10", 0)
            GridEX1.SetValue("P11", 0)
            GridEX1.SetValue("P12", 0)
            GridEX1.SetValue("P13", 0)
            GridEX1.SetValue("P14", 0)
            GridEX1.SetValue("P15", 0)
            GridEX1.SetValue("P16", 0)
            GridEX1.SetValue("P17", 0)
            GridEX1.SetValue("P18", 0)
            GridEX1.SetValue("P19", 0)
            GridEX1.SetValue("P20", 0)
            GridEX1.SetValue("P21", 0)
            GridEX1.SetValue("P22", 0)
            GridEX1.SetValue("P23", 0)
            GridEX1.SetValue("P24", 0)
            GridEX1.SetValue("P25", 0)
            GridEX1.SetValue("P26", 0)
            GridEX1.SetValue("P27", 0)
            GridEX1.SetValue("P28", 0)
            GridEX1.SetValue("P29", 0)
            GridEX1.SetValue("P30", 0)
        End If
    End Sub

    Private Function PedidoSembrado() As Boolean

        Dim Comando As SqlClient.SqlCommand
        Dim Lector As SqlClient.SqlDataReader

        Dim cnn2 As New SqlClient.SqlConnection
        cnn2.ConnectionString = Scnn
        cnn2.Open()


        Comando = New SqlClient.SqlCommand("PPedidoSembrado", cnn2)
        Comando.CommandType = CommandType.StoredProcedure
        Comando.CommandTimeout = 300
        Comando.Parameters.Add(New System.Data.SqlClient.SqlParameter("@PedidosID", System.Data.SqlDbType.UniqueIdentifier, 16, "PedidosID"))
        Comando.Parameters("@PedidosID").Value = PedidosID

        Lector = Comando.ExecuteReader()


        Do While Lector.Read
            PedidoSembrado = Lector!PedidoSembrado
        Loop
        Lector.Close()
        Comando = Nothing
        cnn2.Close()
        cnn2 = Nothing

    End Function


    Private Sub VerFSembrado()

        Dim Comando As SqlClient.SqlCommand
        Dim Lector As SqlClient.SqlDataReader


        using cnn As New SqlClient.SqlConnection(scnn)
        
        cnn.Open()

        Comando = New SqlClient.SqlCommand("PVerFSembrado", cnn)
        Comando.Parameters.Add(New System.Data.SqlClient.SqlParameter("@EmpresaID", System.Data.SqlDbType.UniqueIdentifier, 16, "EmpresaID"))
        Comando.CommandType = CommandType.StoredProcedure
        Comando.CommandTimeout = 300
        Comando.Parameters("@EmpresaID").Value = EmpresaID

        Lector = Comando.ExecuteReader
        FSembrado = Date.Today
        Do While Lector.Read
            If IsDBNull(Lector!Fecha) = False Then
                FSembrado = Lector!Fecha
            End If
        Loop
        Lector.Close()
        Comando = Nothing
       end using
        



    End Sub

    Private Sub Habilitar(ByVal Tipo As Boolean)

        If Tipo Then
            Me.GridEX1.AllowDelete = InheritableBoolean.True
            Me.GridEX1.AllowEdit = InheritableBoolean.True

            Me.GridEX1.RootTable.Columns("Codigo").EditType = EditType.TextBox
            Me.GridEX1.RootTable.Columns("ProductoID").EditType = EditType.MultiColumnCombo
            Me.GridEX1.RootTable.Columns("ClienteCadenaID").EditType = EditType.MultiColumnCombo
            Me.GridEX1.RootTable.Columns("ClienteCadenaIDEnviar").EditType = EditType.MultiColumnCombo
            Me.GridEX1.RootTable.Columns("OrdenCliente").EditType = EditType.TextBox
            Me.GridEX1.RootTable.Columns("Clasificacion").EditType = EditType.TextBox
            If PoderCambiarPrecios Then
                Me.GridEX1.RootTable.Columns("Precio").EditType = EditType.TextBox
            Else
                Me.GridEX1.RootTable.Columns("Precio").EditType = EditType.NoEdit
            End If
            Me.GridEX1.RootTable.Columns("FechaEntrega").EditType = EditType.CalendarCombo
            Me.GridEX1.RootTable.Columns("FechaEmbarque").EditType = EditType.CalendarCombo
            Me.GridEX1.RootTable.Columns("P1").EditType = EditType.TextBox
            Me.GridEX1.RootTable.Columns("P2").EditType = EditType.TextBox
            Me.GridEX1.RootTable.Columns("P3").EditType = EditType.TextBox
            Me.GridEX1.RootTable.Columns("P4").EditType = EditType.TextBox
            Me.GridEX1.RootTable.Columns("P5").EditType = EditType.TextBox
            Me.GridEX1.RootTable.Columns("P6").EditType = EditType.TextBox
            Me.GridEX1.RootTable.Columns("P7").EditType = EditType.TextBox
            Me.GridEX1.RootTable.Columns("P8").EditType = EditType.TextBox
            Me.GridEX1.RootTable.Columns("P9").EditType = EditType.TextBox
            Me.GridEX1.RootTable.Columns("P10").EditType = EditType.TextBox
            Me.GridEX1.RootTable.Columns("P11").EditType = EditType.TextBox
            Me.GridEX1.RootTable.Columns("P12").EditType = EditType.TextBox
            Me.GridEX1.RootTable.Columns("P13").EditType = EditType.TextBox
            Me.GridEX1.RootTable.Columns("P14").EditType = EditType.TextBox
            Me.GridEX1.RootTable.Columns("P15").EditType = EditType.TextBox
            Me.GridEX1.RootTable.Columns("P16").EditType = EditType.TextBox
            Me.GridEX1.RootTable.Columns("P17").EditType = EditType.TextBox
            Me.GridEX1.RootTable.Columns("P18").EditType = EditType.TextBox
            Me.GridEX1.RootTable.Columns("P19").EditType = EditType.TextBox
            Me.GridEX1.RootTable.Columns("P20").EditType = EditType.TextBox
            Me.GridEX1.RootTable.Columns("P21").EditType = EditType.TextBox
            Me.GridEX1.RootTable.Columns("P22").EditType = EditType.TextBox
            Me.GridEX1.RootTable.Columns("P23").EditType = EditType.TextBox
            Me.GridEX1.RootTable.Columns("P24").EditType = EditType.TextBox
            Me.GridEX1.RootTable.Columns("P25").EditType = EditType.TextBox
            Me.GridEX1.RootTable.Columns("P26").EditType = EditType.TextBox
            Me.GridEX1.RootTable.Columns("P27").EditType = EditType.TextBox
            Me.GridEX1.RootTable.Columns("P28").EditType = EditType.TextBox
            Me.GridEX1.RootTable.Columns("P29").EditType = EditType.TextBox
            Me.GridEX1.RootTable.Columns("P30").EditType = EditType.TextBox
            Me.GridEX1.RootTable.Columns("Notas").EditType = EditType.TextBox

        Else
            Me.GridEX1.AllowDelete = InheritableBoolean.False
            Me.GridEX1.AllowEdit = InheritableBoolean.False

            Me.GridEX1.RootTable.Columns("Codigo").EditType = EditType.NoEdit
            Me.GridEX1.RootTable.Columns("ProductoID").EditType = EditType.NoEdit
            Me.GridEX1.RootTable.Columns("ClienteCadenaID").EditType = EditType.NoEdit
            Me.GridEX1.RootTable.Columns("ClienteCadenaIDEnviar").EditType = EditType.NoEdit
            Me.GridEX1.RootTable.Columns("OrdenCliente").EditType = EditType.NoEdit
            Me.GridEX1.RootTable.Columns("Clasificacion").EditType = EditType.NoEdit
            Me.GridEX1.RootTable.Columns("Precio").EditType = EditType.NoEdit
            Me.GridEX1.RootTable.Columns("FechaEntrega").EditType = EditType.NoEdit
            Me.GridEX1.RootTable.Columns("FechaEmbarque").EditType = EditType.NoEdit
            Me.GridEX1.RootTable.Columns("P1").EditType = EditType.NoEdit
            Me.GridEX1.RootTable.Columns("P2").EditType = EditType.NoEdit
            Me.GridEX1.RootTable.Columns("P3").EditType = EditType.NoEdit
            Me.GridEX1.RootTable.Columns("P4").EditType = EditType.NoEdit
            Me.GridEX1.RootTable.Columns("P5").EditType = EditType.NoEdit
            Me.GridEX1.RootTable.Columns("P6").EditType = EditType.NoEdit
            Me.GridEX1.RootTable.Columns("P7").EditType = EditType.NoEdit
            Me.GridEX1.RootTable.Columns("P8").EditType = EditType.NoEdit
            Me.GridEX1.RootTable.Columns("P9").EditType = EditType.NoEdit
            Me.GridEX1.RootTable.Columns("P10").EditType = EditType.NoEdit
            Me.GridEX1.RootTable.Columns("P11").EditType = EditType.NoEdit
            Me.GridEX1.RootTable.Columns("P12").EditType = EditType.NoEdit
            Me.GridEX1.RootTable.Columns("P13").EditType = EditType.NoEdit
            Me.GridEX1.RootTable.Columns("P14").EditType = EditType.NoEdit
            Me.GridEX1.RootTable.Columns("P15").EditType = EditType.NoEdit
            Me.GridEX1.RootTable.Columns("P16").EditType = EditType.NoEdit
            Me.GridEX1.RootTable.Columns("P17").EditType = EditType.NoEdit
            Me.GridEX1.RootTable.Columns("P18").EditType = EditType.NoEdit
            Me.GridEX1.RootTable.Columns("P19").EditType = EditType.NoEdit
            Me.GridEX1.RootTable.Columns("P20").EditType = EditType.NoEdit
            Me.GridEX1.RootTable.Columns("P21").EditType = EditType.NoEdit
            Me.GridEX1.RootTable.Columns("P22").EditType = EditType.NoEdit
            Me.GridEX1.RootTable.Columns("P23").EditType = EditType.NoEdit
            Me.GridEX1.RootTable.Columns("P24").EditType = EditType.NoEdit
            Me.GridEX1.RootTable.Columns("P25").EditType = EditType.NoEdit
            Me.GridEX1.RootTable.Columns("P26").EditType = EditType.NoEdit
            Me.GridEX1.RootTable.Columns("P27").EditType = EditType.NoEdit
            Me.GridEX1.RootTable.Columns("P28").EditType = EditType.NoEdit
            Me.GridEX1.RootTable.Columns("P29").EditType = EditType.NoEdit
            Me.GridEX1.RootTable.Columns("P30").EditType = EditType.NoEdit
            Me.GridEX1.RootTable.Columns("Notas").EditType = EditType.NoEdit

        End If
    End Sub



    Private Sub Pedidos_Details_VerInven1_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Pedidos_Details_VerInven1.Load

    End Sub


    Private Sub Configuracion()

        Dim Comando As SqlClient.SqlCommand
        Dim Lector As SqlClient.SqlDataReader
        Dim v As clsValidar
        Dim cnn2 As New SqlClient.SqlConnection
        cnn2.ConnectionString = Scnn
        cnn2.Open()

        Me.GridEX1.RootTable.Columns("Clasificacion").DefaultValue = "F"



        v = New clsValidar


        Comando = New SqlClient.SqlCommand("PEmpresa_Configuracion", cnn2)
        Comando.Parameters.Add(New System.Data.SqlClient.SqlParameter("@EmpresaID", System.Data.SqlDbType.UniqueIdentifier, 16, "EmpresaID"))
        Comando.Parameters.Add(New System.Data.SqlClient.SqlParameter("@ForzarActualizacion", System.Data.SqlDbType.SmallInt, 4, "ForzarActualizacion"))
        Comando.CommandType = CommandType.StoredProcedure
        Comando.CommandTimeout = 300
        Comando.Parameters("@EmpresaID").Value = EmpresaID
        Comando.Parameters("@ForzarActualizacion").Value = 1

        Lector = Comando.ExecuteReader

        Do While Lector.Read

            If Lector!ManejarLM = False Then

                Me.GridEX1.RootTable.Columns("Clasificacion").Visible = False

            End If
        Loop
        Lector.Close()
        Comando = Nothing
        cnn2.Close()

        cnn2 = Nothing




    End Sub


    Private Sub txtReferencia_GotFocus(ByVal sender As Object, ByVal e As System.EventArgs) Handles txtReferencia.GotFocus
        Me.txtReferencia.SelectionStart = 0
        Me.CalculateTotals()
    End Sub





    Private Sub txtObsGenerales_GotFocus(ByVal sender As Object, ByVal e As System.EventArgs) Handles txtObsGenerales.GotFocus
        Me.txtObsGenerales.SelectionStart = 0
    End Sub

    Private Sub txtobservaciones2_GotFocus(ByVal sender As Object, ByVal e As System.EventArgs) Handles txtObservaciones2.GotFocus
        Me.txtObservaciones2.SelectionStart = 0
    End Sub
    Private Sub txtobservaciones3_GotFocus(ByVal sender As Object, ByVal e As System.EventArgs) Handles txtObservaciones3.GotFocus
        Me.txtObservaciones3.SelectionStart = 0
    End Sub
    Public Sub CalculateTotals()
        Dim subTotal As Decimal
        Dim ImpuestoDinero As Decimal
        Dim Total As Decimal

        Dim T2 As Decimal
        Dim Cantidad As Double


        Cantidad = 0
        Cantidad = Cantidad + Me.GridEX1.GetTotal(Me.GridEX1.RootTable.Columns("P1"), AggregateFunction.Sum)
        Cantidad = Cantidad + Me.GridEX1.GetTotal(Me.GridEX1.RootTable.Columns("P2"), AggregateFunction.Sum)
        Cantidad = Cantidad + Me.GridEX1.GetTotal(Me.GridEX1.RootTable.Columns("P3"), AggregateFunction.Sum)
        Cantidad = Cantidad + Me.GridEX1.GetTotal(Me.GridEX1.RootTable.Columns("P4"), AggregateFunction.Sum)
        Cantidad = Cantidad + Me.GridEX1.GetTotal(Me.GridEX1.RootTable.Columns("P5"), AggregateFunction.Sum)
        Cantidad = Cantidad + Me.GridEX1.GetTotal(Me.GridEX1.RootTable.Columns("P6"), AggregateFunction.Sum)
        Cantidad = Cantidad + Me.GridEX1.GetTotal(Me.GridEX1.RootTable.Columns("P7"), AggregateFunction.Sum)
        Cantidad = Cantidad + Me.GridEX1.GetTotal(Me.GridEX1.RootTable.Columns("P8"), AggregateFunction.Sum)
        Cantidad = Cantidad + Me.GridEX1.GetTotal(Me.GridEX1.RootTable.Columns("P9"), AggregateFunction.Sum)
        Cantidad = Cantidad + Me.GridEX1.GetTotal(Me.GridEX1.RootTable.Columns("P10"), AggregateFunction.Sum)


        Cantidad = Cantidad + Me.GridEX1.GetTotal(Me.GridEX1.RootTable.Columns("P11"), AggregateFunction.Sum)
        Cantidad = Cantidad + Me.GridEX1.GetTotal(Me.GridEX1.RootTable.Columns("P12"), AggregateFunction.Sum)
        Cantidad = Cantidad + Me.GridEX1.GetTotal(Me.GridEX1.RootTable.Columns("P13"), AggregateFunction.Sum)
        Cantidad = Cantidad + Me.GridEX1.GetTotal(Me.GridEX1.RootTable.Columns("P14"), AggregateFunction.Sum)
        Cantidad = Cantidad + Me.GridEX1.GetTotal(Me.GridEX1.RootTable.Columns("P15"), AggregateFunction.Sum)
        Cantidad = Cantidad + Me.GridEX1.GetTotal(Me.GridEX1.RootTable.Columns("P16"), AggregateFunction.Sum)
        Cantidad = Cantidad + Me.GridEX1.GetTotal(Me.GridEX1.RootTable.Columns("P17"), AggregateFunction.Sum)
        Cantidad = Cantidad + Me.GridEX1.GetTotal(Me.GridEX1.RootTable.Columns("P18"), AggregateFunction.Sum)
        Cantidad = Cantidad + Me.GridEX1.GetTotal(Me.GridEX1.RootTable.Columns("P19"), AggregateFunction.Sum)
        Cantidad = Cantidad + Me.GridEX1.GetTotal(Me.GridEX1.RootTable.Columns("P20"), AggregateFunction.Sum)



        Cantidad = Cantidad + Me.GridEX1.GetTotal(Me.GridEX1.RootTable.Columns("P21"), AggregateFunction.Sum)
        Cantidad = Cantidad + Me.GridEX1.GetTotal(Me.GridEX1.RootTable.Columns("P22"), AggregateFunction.Sum)
        Cantidad = Cantidad + Me.GridEX1.GetTotal(Me.GridEX1.RootTable.Columns("P23"), AggregateFunction.Sum)
        Cantidad = Cantidad + Me.GridEX1.GetTotal(Me.GridEX1.RootTable.Columns("P24"), AggregateFunction.Sum)
        Cantidad = Cantidad + Me.GridEX1.GetTotal(Me.GridEX1.RootTable.Columns("P25"), AggregateFunction.Sum)
        Cantidad = Cantidad + Me.GridEX1.GetTotal(Me.GridEX1.RootTable.Columns("P26"), AggregateFunction.Sum)
        Cantidad = Cantidad + Me.GridEX1.GetTotal(Me.GridEX1.RootTable.Columns("P27"), AggregateFunction.Sum)
        Cantidad = Cantidad + Me.GridEX1.GetTotal(Me.GridEX1.RootTable.Columns("P28"), AggregateFunction.Sum)
        Cantidad = Cantidad + Me.GridEX1.GetTotal(Me.GridEX1.RootTable.Columns("P29"), AggregateFunction.Sum)
        Cantidad = Cantidad + Me.GridEX1.GetTotal(Me.GridEX1.RootTable.Columns("P30"), AggregateFunction.Sum)



        subTotal = Me.GridEX1.GetTotal(Me.GridEX1.RootTable.Columns("Importe"), AggregateFunction.Sum)

        ImpuestoDinero = subTotal * IVA / 100

        Total = subTotal

        If IsNumeric(Me.txtGastosDeEnvio.Text) = False Then
            Me.txtGastosDeEnvio.Text = 0
        End If
        T2 = subTotal + ImpuestoDinero + Me.txtGastosDeEnvio.Text

        Me.txtCantidad.Text = Cantidad
        Me.txtSubTotal.Text = Format(subTotal, "$##,##0.00")
        Me.txtImpuestoDinero1.Text = Format(ImpuestoDinero, "$##,##0.00")
        Me.txtTotal1.Text = Format(Total, "$##,##0.00")
        Me.txtT21.Text = Format(T2, "$##,##0.00")


        'RestaurarCalendarioProduccion()
    End Sub


    Protected Overrides Sub OnLoad(ByVal e As System.EventArgs)
        If YaAutorizado = False Then
            Me.txtReferencia.Focus()
        End If

    End Sub

    Private Sub Habilita()

        If YaAutorizado Then
            Me.txtNumero.Enabled = False

        End If
        Me.txtReferencia.Enabled = Not YaAutorizado

        Me.ComboVendedor.Enabled = Not YaAutorizado
        Me.ComboTemporada_Pedido.Enabled = Not YaAutorizado
        Me.LookCliente.Enabled = Not YaAutorizado
        Me.jsdtFecha.Enabled = Not YaAutorizado
        Me.jsdtFechaRecepcion.Enabled = Not YaAutorizado
        Me.jsdtFechaCancelacion.Enabled = Not YaAutorizado

        Me.ComboEnviarDireccion.Enabled = Not YaAutorizado
        Me.txtObsGenerales.Enabled = Not YaAutorizado
        Me.txtObservaciones2.Enabled = Not YaAutorizado
        Me.txtObservaciones3.Enabled = Not YaAutorizado

        Me.txtDeposito.Enabled = Not YaAutorizado
        Me.txtGastosDeEnvio.Enabled = Not YaAutorizado
        Me.LookCuentaBancaria.Enabled = Not YaAutorizado



        If YaAutorizado Then
            Me.btnUpdate.Visible = False
            Me.GridEX1.RootTable.AllowAddNew = InheritableBoolean.False
            Me.GridEX1.RootTable.AllowDelete = InheritableBoolean.False
            Me.GridEX1.RootTable.AllowEdit = InheritableBoolean.False
            Me.GridEX1.RootTable.AllowGroup = True
            Me.MenuItem9.Visible = False

        Else


            Me.GridEX1.RootTable.AllowAddNew = InheritableBoolean.True
            Me.GridEX1.RootTable.AllowDelete = InheritableBoolean.True
            Me.GridEX1.RootTable.AllowEdit = InheritableBoolean.True
            Me.btnUpdate.Visible = True
            Me.GridEX1.RootTable.AllowGroup = False
            Me.MenuItem9.Visible = True

        End If





    End Sub


    Private Sub Panel1_Paint(ByVal sender As System.Object, ByVal e As System.Windows.Forms.PaintEventArgs) Handles Panel1.Paint

    End Sub

    Private Sub GridEX1_LostFocus(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles GridEX1.LostFocus
        If GridEX1.Row = -1 Then
            Dim Row As GridEXRow = GridEX1.GetRow
            If IsDBNull(Row.Cells("ProductoID").Value) Then

                GridEX1.EditMode = EditMode.EditOff
            End If
        End If
    End Sub


    Private Function BuscarTransporte(ByVal ClienteCadenaID As Guid) As Guid

        Dim Comando As SqlClient.SqlCommand
        Dim Lector As SqlClient.SqlDataReader




        using cnn As New SqlClient.SqlConnection(scnn)
        
        cnn.Open()

        Comando = New SqlClient.SqlCommand("PfrmMovi_BuscarTransporte", cnn)
        Comando.CommandType = CommandType.StoredProcedure
        Comando.CommandTimeout = 300
        Comando.Parameters.Add(New System.Data.SqlClient.SqlParameter("@ClienteCadenaID", System.Data.SqlDbType.UniqueIdentifier, 16, "ClienteCadenaID"))
        Comando.Parameters("@ClienteCadenaID").Value = ClienteCadenaID


        Lector = Comando.ExecuteReader()
        Do While Lector.Read
            If IsDBNull(Lector!TransporteID) = False Then
                BuscarTransporte = Lector!TransporteID
            End If

        Loop
        Lector.Close()
        Comando = Nothing
       end using
        





    End Function


    Private Sub CopiarRegistroTallas()
        Me.GridEX1.Focus()
        GridEX1.Row = -1




        GridEX1.SetValue("Cantidad", mCantidad)
        GridEX1.SetValue("P1", mP1)
        GridEX1.SetValue("P2", mP2)
        GridEX1.SetValue("P3", mP3)
        GridEX1.SetValue("P4", mP4)
        GridEX1.SetValue("P5", mP5)
        GridEX1.SetValue("P6", mP6)
        GridEX1.SetValue("P7", mP7)
        GridEX1.SetValue("P8", mP8)
        GridEX1.SetValue("P9", mP9)
        GridEX1.SetValue("P10", mP10)
        GridEX1.SetValue("P11", mP11)
        GridEX1.SetValue("P12", mP12)
        GridEX1.SetValue("P13", mP13)
        GridEX1.SetValue("P14", mP14)
        GridEX1.SetValue("P15", mP15)
        GridEX1.SetValue("P16", mP16)
        GridEX1.SetValue("P17", mP17)
        GridEX1.SetValue("P18", mP18)
        GridEX1.SetValue("P19", mP19)
        GridEX1.SetValue("P20", mP20)
        GridEX1.SetValue("P21", mP21)
        GridEX1.SetValue("P22", mP22)
        GridEX1.SetValue("P23", mP23)
        GridEX1.SetValue("P24", mP24)
        GridEX1.SetValue("P25", mP25)
        GridEX1.SetValue("P26", mP26)
        GridEX1.SetValue("P27", mP27)
        GridEX1.SetValue("P28", mP28)
        GridEX1.SetValue("P29", mP29)
        GridEX1.SetValue("P30", mP30)


    End Sub
    Public Sub FillPDD_EnviarDireccion(ByVal ds As Base)
        Try

            Me.sqlPDD_EnviarDireccion.Parameters("@EmpresaID").Value = EmpresaID
            Me.sqlPDD_EnviarDireccion.Parameters("@DocumentoID").Value = 0
            Me.daPDD_EnviarDireccion.Fill(ds.PDD_EnviarDireccion)
        Catch exc As Exception
            MsgBox(exc.Message, App.MessageCaption)
        End Try
    End Sub
    Private Sub ComboEnviarDireccion_Leave(ByVal sender As Object, ByVal e As System.EventArgs) Handles ComboEnviarDireccion.Leave

        UltimoEnviarDireccion = ComboEnviarDireccion.Text
    End Sub


    Private Sub jsdtFechaRecepcion_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles jsdtFechaRecepcion.Click

    End Sub

    Private Sub jsdtFechaRecepcion_Leave(ByVal sender As Object, ByVal e As System.EventArgs) Handles jsdtFechaRecepcion.Leave
        Dim Respu As String
        Dim i As Integer
        If Me.jsdtFechaRecepcion.Value < Me.FechaParaPedidos And Nuevo = True Then
            MsgBox("Necesitas indicar una fecha mayor a la autorizada que es a partir del : " & CStr(Me.FechaParaPedidos))
            Exit Sub
        End If
        mDataRow("FechaCancelacion") = Me.jsdtFechaRecepcion.Value
        Me.jsdtFechaCancelacion.Value = jsdtFechaRecepcion.Value

        If GridEX1.RowCount > 0 And FAnterior <> Format(Me.jsdtFechaRecepcion.Value, "dd/MM/yyyy") Then
            Respu = Mid(UCase(InputBox("¿Cambiar a todos los renglones del pedido por esta fecha de entrega S/N?", "Cambiar fecha de entrega", "Si")), 1, 1)
            If Respu = "S" Then
                For i = 0 To GridEX1.RecordCount - 1
                    GridEX1.Row = i
                    GridEX1.SetValue("FechaEntrega", Me.jsdtFechaRecepcion.Value)
                    GridEX1.SetValue("FechaEmbarque", Me.jsdtFechaRecepcion.Value)
                    GridEX1.UpdateData()

                Next i
            End If
        End If

    End Sub

    Private Sub jsdtFCancelacionCliente_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles jsdtFCancelacionCliente.Click

    End Sub

    Private Sub jsdtFechaCancelacion_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles jsdtFechaCancelacion.Click

    End Sub

    Private Function FechaParaPedidos() As DateTime

        Dim Comando As SqlClient.SqlCommand
        Dim Lector As SqlClient.SqlDataReader


        using cnn As New SqlClient.SqlConnection(scnn)
        
        cnn.Open()


        Comando = New SqlClient.SqlCommand("PFechaParaPedidos", cnn)
        Comando.CommandType = CommandType.StoredProcedure
        Comando.CommandTimeout = 300
        Comando.Parameters.Add(New System.Data.SqlClient.SqlParameter("@EmpresaID", System.Data.SqlDbType.UniqueIdentifier, 16, "EmpresaID"))
        Comando.Parameters("@EmpresaID").Value = EmpresaID


        Lector = Comando.ExecuteReader()
        Do While Lector.Read
            FechaParaPedidos = Lector!Fecha


        Loop
        Lector.Close()
        Comando = Nothing
       end using
        





    End Function



    Private Sub jsdtFecha_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles jsdtFecha.Click

    End Sub



    Private Sub MenuItem15_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MenuItem15.Click
        Dim fAutorizar As frmAutorizar

        fAutorizar = New frmAutorizar
        fAutorizar.Opcion = ""
        fAutorizar.ShowDialog()

        If fAutorizar.Respuesta = False Then
            MsgBox("Password no existente !!!")
        Else
            If fAutorizar.Guava_Modificar_FechaEntrega Then
                Me.jsdtFecha.Enabled = True
                BFecha = True
            Else
                BFecha = False
                MsgBox("Password sin autorización !!!")
            End If
        End If





    End Sub




    Private Sub MenuItem16_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MenuItem16.Click
        Dim fAutorizar As frmAutorizar

        fAutorizar = New frmAutorizar
        fAutorizar.Opcion = ""
        fAutorizar.ShowDialog()

        If fAutorizar.Respuesta = False Then
            MsgBox("Password no existente !!!")
        Else
            If fAutorizar.Guava_Modificar_FechaEntrega Then
                Me.Habilitar(True)
            Else
                MsgBox("Password sin autorización !!!")
            End If
        End If



    End Sub

    Private Sub frmPedidos_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles MyBase.Load

    End Sub

    Private Sub txtObsGenerales_TextChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles txtObsGenerales.TextChanged

    End Sub

    Private Function Parcializacion(ByVal PedidosID As Guid, ByVal Renglon As Integer) As Integer

        Dim Comando As SqlClient.SqlCommand
        Dim Lector As SqlClient.SqlDataReader




        using cnn As New SqlClient.SqlConnection(scnn)
        
        cnn.Open()

        Comando = New SqlClient.SqlCommand("PPedidos_Details_Parcializacion", cnn)
        Comando.CommandType = CommandType.StoredProcedure
        Comando.CommandTimeout = 300
        Comando.Parameters.Add(New System.Data.SqlClient.SqlParameter("@PedidosID", System.Data.SqlDbType.UniqueIdentifier, 16, "PedidosID"))
        Comando.Parameters.Add(New System.Data.SqlClient.SqlParameter("@Renglon", System.Data.SqlDbType.Int, 4, "Renglon"))
        Comando.Parameters("@PedidosID").Value = PedidosID
        Comando.Parameters("@Renglon").Value = Renglon


        Lector = Comando.ExecuteReader()
        Do While Lector.Read
            Parcializacion = Lector!Cantidad


        Loop
        Lector.Close()
        Comando = Nothing
       end using
        




    End Function


    Private Sub ParcializacionEliminar(ByVal PedidosID As Guid, ByVal Renglon As Integer)




        Dim Comando As SqlClient.SqlCommand


        using cnn As New SqlClient.SqlConnection(scnn)
        
        cnn.Open()

        Comando = New SqlClient.SqlCommand("PPedidos_Details_Parcializacion_Delete", cnn)
        Comando.CommandType = CommandType.StoredProcedure
        Comando.Parameters.Add(New System.Data.SqlClient.SqlParameter("@PedidosID", System.Data.SqlDbType.UniqueIdentifier, 16, "PedidosID"))
        Comando.Parameters.Add(New System.Data.SqlClient.SqlParameter("@Renglon", System.Data.SqlDbType.Int, 4, "Renglon"))
        Comando.CommandTimeout = 300
        Comando.Parameters("@PedidosID").Value = PedidosID
        Comando.Parameters("@Renglon").Value = Renglon
        Comando.ExecuteNonQuery()

        Comando = Nothing
       end using
        





        SetBindings2(mDataRow)


    End Sub
    Private Sub SetBindings2(ByVal row As DataRowView)

        App.DataManager.FillPedidos_Details(row("PedidosID"), row.Row.Table.DataSet)

        Me.CalculateTotals()
        UpdateChanges()

        Me.CalculateTotals()




    End Sub

    Private Function Pedidos_Details_Renglon() As Integer

        Dim Comando As SqlClient.SqlCommand
        Dim Lector As SqlClient.SqlDataReader




        using cnn As New SqlClient.SqlConnection(scnn)
        
        cnn.Open()

        Comando = New SqlClient.SqlCommand("PPedidos_Details_Renglon", cnn)
        Comando.CommandType = CommandType.StoredProcedure
        Comando.CommandTimeout = 300
        Comando.Parameters.Add(New System.Data.SqlClient.SqlParameter("@PedidosID", System.Data.SqlDbType.UniqueIdentifier, 16, "PedidosID"))
        Comando.Parameters("@PedidosID").Value = PedidosID


        Lector = Comando.ExecuteReader()
        Do While Lector.Read
            Pedidos_Details_Renglon = Lector!Renglon


        Loop
        Lector.Close()
        Comando = Nothing
       end using
        




    End Function


    Private Sub MenuItem17_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MenuItem17.Click
        Dim Row As GridEXRow = GridEX1.GetRow

        Dim Motivo As String

        Dim Comando As SqlClient.SqlCommand

        Dim Renglon As Integer
        Dim NC As Integer
        Dim Pedidos_DetailsID As Guid

        Renglon = Row.Cells("Renglon").Value
        NC = Row.Cells("NC").Value
        Pedidos_DetailsID = Row.Cells("Pedidos_DetailsID").Value

        If Row.Cells("Status").Value = False Then
            MsgBox("Renglón ya cancelado anteriormente !!!")
            Exit Sub
        End If



        If MsgBox("¿Quieres cancelar este renglón?", MsgBoxStyle.YesNo) = MsgBoxResult.Yes Then
            If IsDBNull(Row.Cells("Lote").Value) Or IsNothing(Row.Cells("lote").Value) Then
                Dim fMotivo As New frmMotivoCancelacion
                fMotivo.Iniciar()
                fMotivo.ShowDialog()
                Motivo = fMotivo.Motivo
                fMotivo = Nothing
                If Motivo <> "" Then


                    using cnn As New SqlClient.SqlConnection(scnn)
                    
                    cnn.Open()

                    Comando = New SqlClient.SqlCommand("PVitacora_Insert", cnn)
                    Comando.CommandType = CommandType.StoredProcedure
                    Comando.CommandTimeout = 300
                    Comando.Parameters.Add(New System.Data.SqlClient.SqlParameter("@Modulo", System.Data.SqlDbType.VarChar, 100, "Modulo"))
                    Comando.Parameters.Add(New System.Data.SqlClient.SqlParameter("@Motivo", System.Data.SqlDbType.VarChar, 200, "Motivo"))
                    Comando.Parameters.Add(New System.Data.SqlClient.SqlParameter("@UsuarioID", System.Data.SqlDbType.UniqueIdentifier, 16, "UsuarioID"))
                    Comando.Parameters.Add(New System.Data.SqlClient.SqlParameter("@Sistema", System.Data.SqlDbType.VarChar, 16, "Sistema"))
                    Comando.Parameters.Add(New System.Data.SqlClient.SqlParameter("@Texto1", System.Data.SqlDbType.VarChar, 140, "Texto1"))
                    Comando.Parameters.Add(New System.Data.SqlClient.SqlParameter("@Texto2", System.Data.SqlDbType.VarChar, 140, "Texto2"))
                    Comando.Parameters.Add(New System.Data.SqlClient.SqlParameter("@Texto3", System.Data.SqlDbType.VarChar, 140, "Texto3"))
                    Comando.Parameters("@Modulo").Value = "Cancelación del pedido"
                    Comando.Parameters("@Motivo").Value = Motivo
                    Comando.Parameters("@UsuarioID").Value = UsuarioID
                    Comando.Parameters("@Sistema").Value = "Guava"
                    Comando.Parameters("@Texto1").Value = Me.txtNumero.Text
                    Comando.Parameters("@Texto2").Value = Renglon
                    Comando.Parameters("@Texto3").Value = NC
                    Comando.ExecuteNonQuery()

                    Comando = Nothing
                   end using
                    








                End If
                Me.GridEX1.SetValue("Status", False)


                Me.GridEX1.UpdateData()
                Me.UpdateChanges()

                Explosion_Renenerar_Update(Pedidos_DetailsID)

                MsgBox("Documento cancelado exitosamente !!!")

            Else
                If Lote_PoderCancelar(Row.Cells("Lote").Value) Then
                    Dim fMotivo As New frmMotivoCancelacion
                    fMotivo.Iniciar()
                    fMotivo.ShowDialog()
                    Motivo = fMotivo.Motivo
                    fMotivo = Nothing
                    If Motivo <> "" Then


                        using cnn As New SqlClient.SqlConnection(scnn)
                        
                        cnn.Open()

                        Comando = New SqlClient.SqlCommand("PVitacora_Insert", cnn)
                        Comando.CommandType = CommandType.StoredProcedure
                        Comando.CommandTimeout = 300
                        Comando.Parameters.Add(New System.Data.SqlClient.SqlParameter("@Modulo", System.Data.SqlDbType.VarChar, 100, "Modulo"))
                        Comando.Parameters.Add(New System.Data.SqlClient.SqlParameter("@Motivo", System.Data.SqlDbType.VarChar, 200, "Motivo"))
                        Comando.Parameters.Add(New System.Data.SqlClient.SqlParameter("@UsuarioID", System.Data.SqlDbType.UniqueIdentifier, 16, "UsuarioID"))
                        Comando.Parameters.Add(New System.Data.SqlClient.SqlParameter("@Sistema", System.Data.SqlDbType.VarChar, 16, "Sistema"))
                        Comando.Parameters.Add(New System.Data.SqlClient.SqlParameter("@Texto1", System.Data.SqlDbType.VarChar, 140, "Texto1"))
                        Comando.Parameters.Add(New System.Data.SqlClient.SqlParameter("@Texto2", System.Data.SqlDbType.VarChar, 140, "Texto2"))
                        Comando.Parameters.Add(New System.Data.SqlClient.SqlParameter("@Texto3", System.Data.SqlDbType.VarChar, 140, "Texto3"))
                        Comando.Parameters("@Modulo").Value = "Cancelación del pedido"
                        Comando.Parameters("@Motivo").Value = Motivo
                        Comando.Parameters("@UsuarioID").Value = UsuarioID
                        Comando.Parameters("@Sistema").Value = "Guava"
                        Comando.Parameters("@Texto1").Value = Me.txtNumero.Text
                        Comando.Parameters("@Texto2").Value = Renglon
                        Comando.Parameters("@Texto3").Value = NC
                        Comando.ExecuteNonQuery()

                        Comando = Nothing
                       end using
                        








                    End If

                    Me.GridEX1.SetValue("Status", False)


                    Me.GridEX1.UpdateData()
                    Me.UpdateChanges()
                    Explosion_Renenerar_Update(Pedidos_DetailsID)
                    MsgBox("Documento cancelado exitosamente !!!")
                Else
                    MsgBox("No se puede cancelar este lote por que ya ha sido enviado !!!")
                    Exit Sub
                End If

            End If


        End If

    End Sub




    Private Function Lote_PoderCancelar(ByVal Lote As String) As Boolean

        Dim cmd As SqlClient.SqlCommand
        Dim V As clsValidar
        Dim rs As SqlClient.SqlDataReader

        'SiguienteNumero = 1
        'Exit Function

        using cnn As New SqlClient.SqlConnection(scnn)
        
        cnn.Open()

        V = New clsValidar

        cmd = New SqlClient.SqlCommand("PLote_PoderCancelar", cnn)
        cmd.CommandType = CommandType.StoredProcedure
        cmd.Parameters.Add(New System.Data.SqlClient.SqlParameter("@EmpresaID", System.Data.SqlDbType.UniqueIdentifier, 16, "EmpresaID"))
        cmd.Parameters.Add(New System.Data.SqlClient.SqlParameter("@Lote", System.Data.SqlDbType.VarChar, 16, "Lote"))
        cmd.Parameters("@EmpresaID").Value = EmpresaID
        cmd.Parameters("@Lote").Value = Lote

        rs = cmd.ExecuteReader()
        Do While rs.Read
            If rs!Cantidad = 0 Then
                Lote_PoderCancelar = True
            Else
                Lote_PoderCancelar = False
            End If

        Loop
        rs.Close()
        cmd = Nothing

       end using
        


    End Function


    Private Sub txtDeposito_TextChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles txtDeposito.TextChanged

    End Sub

    Private Sub MenuItem18_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MenuItem18.Click
        Dim f As New frmLayOut_Maquilas

        GridEX1.UpdateData()
        Me.CalculateTotals()
        UpdateChanges()


        f.Iniciar(Me.LookCliente.EditValue, Me.LookCliente.Text, PedidosID)
        f.ShowDialog()
        f.Close()

        App.DataManager.FillPedidos_Details(PedidosID, Me.dsBase)

        GridEX1.UpdateData()
        Me.CalculateTotals()
        UpdateChanges()
    End Sub

    Private Sub Explosion_Renenerar_Update(ByVal Pedidos_DetailsID As Guid)



        Dim Comando As SqlClient.SqlCommand


        using cnn As New SqlClient.SqlConnection(scnn)
        
        cnn.Open()

        Comando = New SqlClient.SqlCommand("PExplosion_Renenerar_Update", cnn)
        Comando.CommandType = CommandType.StoredProcedure
        Comando.Parameters.Add(New System.Data.SqlClient.SqlParameter("@Pedidos_DetailsID", System.Data.SqlDbType.UniqueIdentifier, 16, "Pedidos_DetailsID"))
        Comando.CommandTimeout = 300
        Comando.Parameters("@Pedidos_DetailsID").Value = Pedidos_DetailsID
        Comando.ExecuteNonQuery()

        Comando = Nothing
       end using
        





    End Sub


    Private Sub TextBox1_TextChanged(ByVal sender As System.Object, ByVal e As System.EventArgs)

    End Sub

    Private Sub TextBox1_KeyPress(ByVal sender As System.Object, ByVal e As System.Windows.Forms.KeyPressEventArgs)

    End Sub




    Private Sub MenuItem20_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MenuItem20.Click
        Dim Respu As String
        Dim Tamaño As Integer


        Respu = InputBox("Tamaño del lote:", "", Parametros_Generales_TamañoDelLote())

        If IsNumeric(Respu) = False Then
            MsgBox("Necesita indicar un valor numérico !!!")
            Exit Sub
        End If
        Tamaño = Respu


        Dim Comando As SqlClient.SqlCommand
        Dim v As clsValidar
        v = New clsValidar

        using cnn As New SqlClient.SqlConnection(scnn)
        
        cnn.Open()

        Comando = New SqlClient.SqlCommand("PPedidos_DetailsYaDesglosar", cnn)
        Comando.CommandType = CommandType.StoredProcedure
        Comando.CommandTimeout = 300
        Comando.Parameters.Add(New System.Data.SqlClient.SqlParameter("@PedidosID", System.Data.SqlDbType.UniqueIdentifier, 16, "PedidosID"))
        Comando.Parameters.Add(New System.Data.SqlClient.SqlParameter("@Tamaño", System.Data.SqlDbType.Int, 16, "Tamaño"))
        Comando.Parameters("@PedidosID").Value = PedidosID
        Comando.Parameters("@Tamaño").Value = Tamaño
        Comando.ExecuteNonQuery()

        Comando = Nothing
       end using
        




        Parametros_Generales_TamañoDelLote_Update(Tamaño)
        MsgBox("Pedido desglosado exitosamente !!!")
        Adios()

    End Sub


    Private Sub GridEX1_GotFocus(ByVal sender As Object, ByVal e As System.EventArgs) Handles GridEX1.GotFocus
        FAnterior = Format(Me.jsdtFechaRecepcion.Value, "dd/MM/yyyy")
    End Sub


    Private Sub AfectarOtrosCampos()


        mDataRow("UsuarioIDAutorizado") = UsuarioID
        Me.UpdateChanges()

    End Sub



    Private Function PHorma_Busca_Por_Producto(ByVal ProductoID As Guid) As String

        Dim cmd As SqlClient.SqlCommand
        Dim V As clsValidar
        Dim rs As SqlClient.SqlDataReader

        'SiguienteNumero = 1
        'Exit Function

        using cnn As New SqlClient.SqlConnection(scnn)
        
        cnn.Open()

        V = New clsValidar

        cmd = New SqlClient.SqlCommand("PHorma_Busca_Por_Producto", cnn)
        cmd.CommandType = CommandType.StoredProcedure
        cmd.Parameters.Add(New System.Data.SqlClient.SqlParameter("@ProductoID", System.Data.SqlDbType.UniqueIdentifier, 16, "ProductoID"))
        cmd.Parameters("@ProductoId").Value = ProductoID

        rs = cmd.ExecuteReader()

        PHorma_Busca_Por_Producto = ""
        Do While rs.Read
            PHorma_Busca_Por_Producto = rs!Horma

        Loop
        rs.Close()
        cmd = Nothing
       end using
        



    End Function


    Private Sub PedidoDesglosadoView1_Load(sender As Object, e As EventArgs)

    End Sub


    Private Sub MenuItem21_Click(sender As Object, e As EventArgs) Handles MenuItem21.Click
        GridEX1.UpdateData()
        Me.CalculateTotals()
        UpdateChanges()


        Importacion_Pedido(PedidosID)


        App.DataManager.FillPedidos_Details(PedidosID, Me.dsBase)

        GridEX1.UpdateData()
        Me.CalculateTotals()
        UpdateChanges()

    End Sub

    Private Sub Importacion_Pedido(PedidosID As Guid)



        Dim Comando As SqlClient.SqlCommand
        Dim Numero As Integer


        Numero = InputBox("Número de pedido a importar ?")



        using cnn As New SqlClient.SqlConnection(scnn)
        
        cnn.Open()

        Comando = New SqlClient.SqlCommand("PPedidos_Importacion", cnn)
        Comando.CommandType = CommandType.StoredProcedure
        Comando.Parameters.Add(New System.Data.SqlClient.SqlParameter("@PedidosID", System.Data.SqlDbType.UniqueIdentifier, 16, "PedidosID"))
        Comando.Parameters.Add(New System.Data.SqlClient.SqlParameter("@Numero", System.Data.SqlDbType.Int, 4, "Numero"))
        Comando.CommandTimeout = 300
        Comando.Parameters("@PedidosID").Value = PedidosID
        Comando.Parameters("@Numero").Value = Numero
        Comando.ExecuteNonQuery()

        Comando = Nothing
       end using
        





    End Sub




    Private Sub txtObservaciones3_TextChanged(sender As Object, e As EventArgs) Handles txtObservaciones3.TextChanged

    End Sub

    Private Sub MenuItem22_Click(sender As Object, e As EventArgs)


    End Sub

    Private Sub NuevoCliente(BEntrada As String)
        Dim f As New frmClienteWeb

        If BEntrada = "N" Then
            f.Iniciar(BEntrada, LookCliente.Text, EmpresaID)
        Else
            f.Iniciar(BEntrada, LookCliente.Text, LookCliente.EditValue)
        End If


        f.ShowDialog()







        If f.BGrabado = True Then
            App.DataManager.FillClienteList(Me.dsDataset2)

            Me.LookCliente.EditValue = f.ClienteID

        End If

        'Me.ComboVendedor.Focus()

        '        f = Nothing

    End Sub


    Private Sub MenuItem22_Click_1(sender As Object, e As EventArgs) Handles MenuItem22.Click
        NuevoCliente("N")
    End Sub

    Private Sub MenuItem23_Click(sender As Object, e As EventArgs) Handles MenuItem23.Click
        NuevoCliente("M")

    End Sub


    Private Sub BuscaObs1()

        Dim cmd As SqlClient.SqlCommand
        Dim V As clsValidar
        Dim rs As SqlClient.SqlDataReader


        using cnn As New SqlClient.SqlConnection(scnn)
        
        cnn.Open()
        V = New clsValidar

        cmd = New SqlClient.SqlCommand("PPedidos_BuscaObs1", cnn)
        cmd.CommandType = CommandType.StoredProcedure
        cmd.Parameters.Add(New System.Data.SqlClient.SqlParameter("@PedidosID", System.Data.SqlDbType.UniqueIdentifier, 16, "PedidosID"))
        cmd.Parameters.Add(New System.Data.SqlClient.SqlParameter("@ClienteID", System.Data.SqlDbType.UniqueIdentifier, 16, "ClienteID"))
        cmd.Parameters("@PedidosID").Value = PedidosID
        cmd.Parameters("@ClienteID").Value = Me.LookCliente.EditValue

        rs = cmd.ExecuteReader()


        Do While rs.Read

            GridEX1.SetValue("Obs1", rs!Obs1)

        Loop
        rs.Close()
        cmd = Nothing
       end using
        



    End Sub

    Private Sub Pedidos_Busca()

        Dim cmd As SqlClient.SqlCommand
        Dim V As clsValidar
        Dim rs As SqlClient.SqlDataReader

        Dim cnn2 As New SqlClient.SqlConnection
        cnn2.ConnectionString = Scnn
        cnn2.Open()

        V = New clsValidar

        cmd = New SqlClient.SqlCommand("PPedidos_Busca", cnn2)
        cmd.CommandType = CommandType.StoredProcedure
        cmd.Parameters.Add(New System.Data.SqlClient.SqlParameter("@PedidosID", System.Data.SqlDbType.UniqueIdentifier, 16, "PedidosID"))
        cmd.Parameters("@PedidosID").Value = PedidosID

        rs = cmd.ExecuteReader()


        Do While rs.Read

            Me.LookCuentaBancaria.EditValue = rs!CuentaBancariaID
            Me.txtGastosDeEnvio.Text = rs!GastosDeEnvio



            If V.vFieldVal(rs!Empacado) <> "" Then
                If rs!Empacado = "E" Then
                    Me.cmdEmpacado.EditValue = "Empacado"
                Else
                    Me.cmdEmpacado.EditValue = "Flejado"
                End If
            End If

            If V.vFieldVal(rs!UsoDelCFDI) <> "" Then
                Me.txtUsoDelCFDI.Text = rs!UsoDelCFDI
            End If

            If rs!DiasCredito = 0 Then
                Me.LabelCondiciones.Text = "CONTADO"
            Else
                Me.LabelCondiciones.Text = "CREDITO"
            End If
            Me.LabelDiasCredito.Text = rs!DiasCredito

            Me.LookEnvioA.EditValue = rs!EnvioAID

            Me.LookPaqueteria.EditValue = rs!PaqueteriaID




            Me.txtOcurreA.Text = rs!OcurreA

            BuscaOcurreA()

            Me.LookAlmacen.EditValue = rs!AlmacenID
        Loop
        rs.Close()
        cmd = Nothing


        cnn2.Close()
        cnn2 = Nothing

    End Sub

    Private Sub Pedidos_Actualiza()

        Dim Comando As SqlClient.SqlCommand
        using cnn As New SqlClient.SqlConnection(scnn)
        
        cnn.Open()


        Comando = New SqlClient.SqlCommand("PPedidos_Actualiza", cnn)
        Comando.CommandType = CommandType.StoredProcedure
        Comando.Parameters.Add(New System.Data.SqlClient.SqlParameter("@PedidosID", System.Data.SqlDbType.UniqueIdentifier, 16, "PedidosID"))
        Comando.Parameters.Add(New System.Data.SqlClient.SqlParameter("@CuentaBancariaID", System.Data.SqlDbType.UniqueIdentifier, 16, "CuentaBancariaID"))
        Comando.Parameters.Add(New System.Data.SqlClient.SqlParameter("@GastosDeEnvio", System.Data.SqlDbType.Money, 8, "GastosDeEnvio"))
        Comando.Parameters.Add(New System.Data.SqlClient.SqlParameter("@Empacado", System.Data.SqlDbType.Char, 1, "Empacado"))
        Comando.Parameters.Add(New System.Data.SqlClient.SqlParameter("@UsoDelCFDI", System.Data.SqlDbType.VarChar, 30, "UsoDelCFDI"))
        Comando.Parameters.Add(New System.Data.SqlClient.SqlParameter("@EnvioAID", System.Data.SqlDbType.UniqueIdentifier, 16, "EnvioAID"))
        Comando.Parameters.Add(New System.Data.SqlClient.SqlParameter("@OcurreA", System.Data.SqlDbType.VarChar, 200, "OcurreA"))
        Comando.Parameters.Add(New System.Data.SqlClient.SqlParameter("@AlmacenID", System.Data.SqlDbType.UniqueIdentifier, 16, "AlmacenID"))
        Comando.Parameters.Add(New System.Data.SqlClient.SqlParameter("@PaqueteriaID", System.Data.SqlDbType.UniqueIdentifier, 16, "PaqueteriaID"))
        Comando.CommandTimeout = 300
        Comando.Parameters("@PedidosID").Value = PedidosID
        Comando.Parameters("@CuentaBancariaID").Value = Me.LookCuentaBancaria.EditValue
        Comando.Parameters("@GastosDeEnvio").Value = Me.txtGastosDeEnvio.Text

        Comando.Parameters("@Empacado").Value = Mid(Me.cmdEmpacado.EditValue, 1, 1)

        Comando.Parameters("@UsoDelCFDI").Value = Me.txtUsoDelCFDI.Text
        Comando.Parameters("@EnvioAID").Value = Me.LookEnvioA.EditValue
        Comando.Parameters("@OcurreA").Value = Me.txtOcurreA.Text
        Comando.Parameters("@Almacenid").Value = Me.LookAlmacen.EditValue
        Comando.Parameters("@PaqueteriaID").Value = Me.LookPaqueteria.EditValue
        Comando.ExecuteNonQuery()

        Comando = Nothing
       end using
        





    End Sub

    Private Sub txtGastosDeEnvio_Leave(sender As Object, e As EventArgs) Handles txtGastosDeEnvio.Leave
        Me.CalculateTotals()
    End Sub

    Private Sub txtGastosDeEnvio_TextChanged(sender As Object, e As EventArgs) Handles txtGastosDeEnvio.TextChanged

    End Sub

    Private Sub LookCuentaBancaria_EditValueChanged(sender As Object, e As EventArgs) Handles LookCuentaBancaria.EditValueChanged

    End Sub

    Private Sub Button3_Click(sender As Object, e As EventArgs) Handles Button3.Click
        Dim f As New frmPedidos_Foto
        Dim Row As GridEXRow = GridEX1.GetRow

        f.Iniciar(Row.Cells("ProductoID").Value)
        f.Show()
    End Sub

    Private Sub jsdtFechaCancelacion_LostFocus(sender As Object, e As EventArgs) Handles jsdtFechaCancelacion.LostFocus
        Me.GridEX1.Focus()
    End Sub


    Private Sub VerFoto()

        Dim fso As Object
        Dim Archivo As String
        Dim Row As GridEXRow = GridEX1.GetRow
        Dim Codigo As String
        On Error GoTo Errores

        Codigo = Row.Cells(4).Value



        Archivo = CarpetaSilvasoft() & "\_Silvasoft\Config\Grupo" & ClaveGrupo & "\Fotos\Productos\" & Codigo & ".jpg"
        fso = CreateObject("Scripting.FileSystemObject")
        If (fso.FileExists(Archivo)) Then
            ShowMyImage(Archivo)
            Me.PicFoto.Visible = True

        End If

Errores:


    End Sub

    Public Sub ShowMyImage(ByVal fileToDisplay As String)
        'On Error GoTo errores
        If Not (MyImage Is Nothing) Then
            MyImage.Dispose()
        End If

        Me.PicFoto.SizeMode = PictureBoxSizeMode.StretchImage
        MyImage = New Bitmap(fileToDisplay)
        PicFoto.Image = CType(MyImage, Image)
errores:
    End Sub

    Private Sub daPProducto_Foto_Existe_RowUpdated(sender As Object, e As SqlClient.SqlRowUpdatedEventArgs)

    End Sub

    Private Sub Panel2_Paint(sender As Object, e As PaintEventArgs)

    End Sub


    Private Sub LookCliente_Leave(sender As Object, e As EventArgs) Handles LookCliente.Leave
        Dim g As Guid
        g = Me.LookCliente.EditValue

        If RTrim(Me.LookCliente.Text) = "" Then
            Exit Sub
        End If
        If Cliente_Bloqueado() = "S" Then
            MsgBox("Necesita pedir autorización porque este cliente esta bloqueado en el catálogo de clientes !!!")
            Dim fAutorizar As frmAutorizar
            fAutorizar = New frmAutorizar
            fAutorizar.Opcion = ""
            fAutorizar.ShowDialog()

            If fAutorizar.PoderDesautorizarCobranza = False Then
                fAutorizar.Close()
                fAutorizar = Nothing
                Me.LookCliente.Focus()
            End If
            fAutorizar.Close()
            fAutorizar = Nothing
            BVenderAutorizadamente = True
            Exit Sub
        End If
        If ChecaClienteStatus() = False Then
            MsgBox("Tiene status de cancelado el cliente " & LookCliente.Text & " " & ObsCliente)
            Me.LookCliente.Focus()
            Exit Sub
        End If
        If GuavaChecarCobranza() Then
            BuscarPrecio()
            mDataRow("VendedorID") = BuscarVendedor(LookCliente.EditValue)
            If IsDBNull(mDataRow("VendedorID")) = False Then
                ComboVendedor.Value = mDataRow("VendedorID")
            End If



        Else
            'MsgBox("El cliente " & LookCliente.Text & " no ha pagado recientemente por lo tanto no se le puede fabricar !!!")
            Dim fAutorizar As frmAutorizar
            fAutorizar = New frmAutorizar
            fAutorizar.Opcion = ""
            fAutorizar.ShowDialog()

            If fAutorizar.PoderDesautorizarCobranza = False Then
                fAutorizar.Close()
                fAutorizar = Nothing
                Me.LookCliente.Focus()
                Exit Sub
            End If
            fAutorizar.Close()
            fAutorizar = Nothing
            BVenderAutorizadamente = True

            BuscarPrecio()
            mDataRow("VendedorID") = BuscarVendedor(LookCliente.EditValue)
            If IsDBNull(mDataRow("VendedorID")) = False Then
                ComboVendedor.Value = mDataRow("VendedorID")
            End If
        End If

        Restaurar_ClienteCadena(Me.LookCliente.EditValue)
        Restaurar_ClienteCadena_Enviar(Me.LookCliente.EditValue)
        BuscarPrecio()
        FillCatalogos(Me.LookCliente.EditValue)

        Exit Sub
errores:
        MsgBox("Este cliente no esta dado de alta !!!")
        Me.LookCliente.Focus()

    End Sub


    Private Sub LookEnvioA_Leave(sender As Object, e As EventArgs) Handles LookEnvioA.Leave

        BuscaOcurreA()

    End Sub


    Private Sub BuscaOcurreA()

        Dim cmd As SqlClient.SqlCommand
        Dim V As clsValidar
        Dim rs As SqlClient.SqlDataReader

        Dim cnn2 As New SqlClient.SqlConnection
        cnn2.ConnectionString = Scnn
        cnn2.Open()

        V = New clsValidar

        cmd = New SqlClient.SqlCommand("POcurreA", cnn2)
        cmd.CommandType = CommandType.StoredProcedure
        cmd.Parameters.Add(New System.Data.SqlClient.SqlParameter("@EnvioAID", System.Data.SqlDbType.UniqueIdentifier, 16, "EnvioAID"))
        cmd.Parameters("@EnvioAID").Value = Me.LookEnvioA.EditValue

        rs = cmd.ExecuteReader()


        Do While rs.Read

            If rs!Clave = 3 Then

                Me.LabelOcurreA.Visible = True
                Me.txtOcurreA.Visible = True
            Else

                Me.LabelOcurreA.Visible = False
                Me.txtOcurreA.Visible = False


            End If

        Loop
        rs.Close()
        cmd = Nothing


        cnn2.Close()
        cnn2 = Nothing

    End Sub

    Private Sub LookCliente_EditValueChanged(sender As Object, e As EventArgs) Handles LookCliente.EditValueChanged

    End Sub

    Private Sub BuscaProductoPrecioLista()

        '   Dim ProductoID As Guid
        Dim Row As GridEXRow = GridEX1.GetRow()

        Dim Comando As SqlClient.SqlCommand
        Dim Lector As SqlClient.SqlDataReader

        Dim Precio As Decimal
        Dim PrecioCIva As Decimal


        BuscarPrecio()


        using cnn As New SqlClient.SqlConnection(scnn)
        
        cnn.Open()

        Comando = New SqlClient.SqlCommand("PBuscaProducto3", cnn)
        Comando.CommandType = CommandType.StoredProcedure
        Comando.CommandTimeout = 300
        Comando.Parameters.Add(New System.Data.SqlClient.SqlParameter("@EmpresaID", System.Data.SqlDbType.UniqueIdentifier, 16, "EmpresaID"))
        Comando.Parameters.Add(New System.Data.SqlClient.SqlParameter("@ProductoID", System.Data.SqlDbType.UniqueIdentifier, 16, "ProductoID"))
        Comando.Parameters.Add(New System.Data.SqlClient.SqlParameter("@PrecioID", System.Data.SqlDbType.UniqueIdentifier, 16, "PrecioID"))

        Comando.Parameters("@EmpresaID").Value = EmpresaID
        Comando.Parameters("@ProductoID").Value = Row.Cells("ProductoID").Value
        Comando.Parameters("@PrecioID").Value = PrecioID

        Lector = Comando.ExecuteReader()
        Do While Lector.Read
            Precio = Lector!Precio
            PrecioLista = Precio
            PrecioCIva = Lector!PrecioCIva

        Loop
        Lector.Close()
        Comando = Nothing

       end using
        



    End Sub

    Private Function ChecarPrecios() As Boolean

        Dim Comando As SqlClient.SqlCommand
        Dim Lector As SqlClient.SqlDataReader

        Dim Row As GridEXRow = GridEX1.GetRow

        Using cnn As New SqlClient.SqlConnection(Scnn)

            cnn.Open()
            Comando = New SqlClient.SqlCommand("PClavePrecioMostrador_EnPedido", cnn)
            Comando.Parameters.Add(New System.Data.SqlClient.SqlParameter("@PedidosID", System.Data.SqlDbType.UniqueIdentifier, 16, "PedidosID"))
            Comando.CommandType = CommandType.StoredProcedure
            Comando.CommandTimeout = 300
            Comando.Parameters("@PedidosID").Value = PedidosID
            Lector = Comando.ExecuteReader

            ChecarPrecios = False
            Do While Lector.Read

                If Lector!Letrero <> "" Then
                    ChecarPrecios = True
                    App.DataManager.FillPedidos_Details(PedidosID, mDataRow.Row.Table.DataSet)


                End If

            Loop
            Lector.Close()
            Comando = Nothing

        End Using



    End Function




    Private Sub LookEnvioA_EditValueChanged(sender As Object, e As EventArgs) Handles LookEnvioA.EditValueChanged

    End Sub

    Private Sub LookUpEdit1_EditValueChanged(sender As Object, e As EventArgs) Handles LookPaqueteria.EditValueChanged

    End Sub


    Private Function Cortesia(ProductoID As Guid) As Boolean

        Dim Comando As SqlClient.SqlCommand
        Dim Lector As SqlClient.SqlDataReader

        Dim Row As GridEXRow = GridEX1.GetRow

        Using cnn As New SqlClient.SqlConnection(Scnn)

            cnn.Open()
            Comando = New SqlClient.SqlCommand("PCortesia", cnn)
            Comando.Parameters.Add(New System.Data.SqlClient.SqlParameter("@ProductoID", System.Data.SqlDbType.UniqueIdentifier, 16, "ProductoID"))
            Comando.CommandType = CommandType.StoredProcedure
            Comando.CommandTimeout = 300
            Comando.Parameters("@ProductoID").Value = ProductoID
            Lector = Comando.ExecuteReader

            Cortesia = False
            Do While Lector.Read

                Cortesia = Lector!Cortesia

            Loop
            Lector.Close()
            Comando = Nothing

        End Using



    End Function


    Private Sub MenuItem26_Click(sender As Object, e As EventArgs)
    End Sub




    Private Sub GridEX1_FormattingRow(sender As Object, e As RowLoadEventArgs) Handles GridEX1.FormattingRow

    End Sub
End Class




