using Proyecto_taller.Data;
using Proyecto_taller.Helpers;
using Proyecto_taller.Models;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;

namespace Proyecto_taller.Views
{
    public partial class RegistroRapidoWindow : Window
    {
        private Cliente _clienteResuelto = null;
        private Vehiculo _vehiculoResuelto = null;
        private bool _nuevoVehiculoParaClienteExistente = false;

        public RegistroRapidoWindow()
        {
            InitializeComponent();
            dpFechaEntrega.SelectedDate = DateTime.Now.AddDays(3);
            dpFechaEntrega.DisplayDateStart = DateTime.Today;
            SuscribirEventosValidacion();
            CargarEmpleados();
        }

        // ── NUEVO: cargar lista de empleados activos para el selector ────────
        private void CargarEmpleados()
        {
            using var db = new TallerDbContext();

            var empleados = db.Usuarios
                .Where(u => u.Activo)
                .OrderBy(u => u.NombreCompleto)
                .Select(u => new { u.UsuarioID, u.NombreCompleto, u.Rol })
                .ToList();

            cmbEmpleadoAsignado.ItemsSource = empleados
                .Select(u => new { u.UsuarioID, Texto = $"{u.NombreCompleto} ({u.Rol})" })
                .ToList();
            cmbEmpleadoAsignado.DisplayMemberPath = "Texto";
            cmbEmpleadoAsignado.SelectedValuePath = "UsuarioID";

            // Preseleccionar al usuario actualmente logueado, si corresponde
            if (SessionManager.EstaAutenticado)
                cmbEmpleadoAsignado.SelectedValue = SessionManager.UsuarioActual.UsuarioID;
        }

        private void SuscribirEventosValidacion()
        {
            txtNombre.LostFocus += (s, e) => AplicarTitleCase(txtNombre);
            txtApellido.LostFocus += (s, e) => AplicarTitleCase(txtApellido);
            txtDireccion.LostFocus += (s, e) => AplicarPrimeraLetra(txtDireccion);
            txtEmail.LostFocus += (s, e) => AplicarMinusculas(txtEmail);

            txtMarca.LostFocus += (s, e) => AplicarTitleCase(txtMarca);
            txtModelo.LostFocus += (s, e) => AplicarTitleCase(txtModelo);
            txtPlaca.TextChanged += (s, e) => AplicarMayusculasInline(txtPlaca);

            txtMarcaNuevo.LostFocus += (s, e) => AplicarTitleCase(txtMarcaNuevo);
            txtModeloNuevo.LostFocus += (s, e) => AplicarTitleCase(txtModeloNuevo);
            txtPlacaNuevo.TextChanged += (s, e) => AplicarMayusculasInline(txtPlacaNuevo);
        }

        private static void AplicarTitleCase(TextBox tb)
        {
            if (!string.IsNullOrWhiteSpace(tb.Text))
                tb.Text = ValidationHelper.AplicarTitleCase(tb.Text);
        }

        private static void AplicarPrimeraLetra(TextBox tb)
        {
            if (!string.IsNullOrWhiteSpace(tb.Text))
                tb.Text = ValidationHelper.AplicarPrimeraLetraMayuscula(tb.Text);
        }

        private static void AplicarMinusculas(TextBox tb)
        {
            if (!string.IsNullOrWhiteSpace(tb.Text))
                tb.Text = tb.Text.Trim().ToLower();
        }

        private static void AplicarMayusculasInline(TextBox tb)
        {
            var upper = tb.Text.ToUpper();
            if (tb.Text != upper)
            {
                int caret = tb.CaretIndex;
                tb.Text = upper;
                tb.CaretIndex = Math.Min(caret, upper.Length);
            }
        }

        // ── Búsqueda de cliente existente ─────────────────────────────────────

        private void TxtBuscarCliente_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (txtBuscarCliente.Text.Length >= 2)
                EjecutarBusqueda(txtBuscarCliente.Text.Trim());
            else
                panelResultadosBusqueda.Visibility = Visibility.Collapsed;
        }

        private void BuscarCliente_Click(object sender, RoutedEventArgs e)
            => EjecutarBusqueda(txtBuscarCliente.Text.Trim());

        private void EjecutarBusqueda(string termino)
        {
            if (string.IsNullOrWhiteSpace(termino)) return;

            using var db = new TallerDbContext();
            var lower = termino.ToLower();

            var resultados = db.Clientes
                .Include(c => c.Vehiculos)
                .Where(c =>
                    c.Nombre.ToLower().Contains(lower) ||
                    c.Apellido.ToLower().Contains(lower) ||
                    c.Telefono.Contains(termino) ||
                    (c.Correo != null && c.Correo.ToLower().Contains(lower)))
                .OrderBy(c => c.Apellido)
                .Take(10)
                .ToList();

            if (resultados.Count == 0)
            {
                panelResultadosBusqueda.Visibility = Visibility.Collapsed;
                MessageBox.Show(
                    $"No se encontró ningún cliente con '{termino}'.\n\nPuedes marcar 'Es un cliente nuevo' para registrarlo.",
                    "Sin resultados", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            lstResultadosBusqueda.ItemsSource = resultados;
            panelResultadosBusqueda.Visibility = Visibility.Visible;
        }

        private void LstResultados_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lstResultadosBusqueda.SelectedItem is not Cliente cliente) return;
            SeleccionarCliente(cliente);
            panelResultadosBusqueda.Visibility = Visibility.Collapsed;
        }

        private void SeleccionarCliente(Cliente cliente)
        {
            _clienteResuelto = cliente;

            txtClienteSeleccionadoNombre.Text = $"{cliente.Nombre} {cliente.Apellido}";
            txtClienteSeleccionadoTel.Text = cliente.Telefono;
            txtClienteSeleccionadoCorreo.Text = string.IsNullOrWhiteSpace(cliente.Correo)
                ? "Sin correo" : cliente.Correo;
            panelClienteSeleccionado.Visibility = Visibility.Visible;

            chkNuevoCliente.IsChecked = false;
            panelNuevoCliente.Visibility = Visibility.Collapsed;
            panelVehiculoNuevoCliente.Visibility = Visibility.Collapsed;

            CargarVehiculosCliente(cliente.ClienteID);
        }

        private void CargarVehiculosCliente(int clienteId)
        {
            _nuevoVehiculoParaClienteExistente = false;

            using var db = new TallerDbContext();
            var vehiculos = db.Vehiculos
                .Where(v => v.ClienteID == clienteId)
                .OrderBy(v => v.Marca)
                .ToList();

            if (vehiculos.Count > 0)
            {
                cmbVehiculosCliente.ItemsSource = vehiculos;
                cmbVehiculosCliente.SelectedIndex = 0;
                panelNuevoVehiculoInline.Visibility = Visibility.Collapsed;
                panelVehiculoConfirmado.Visibility = Visibility.Collapsed;
                panelVehiculoExistente.Visibility = Visibility.Visible;
            }
            else
            {
                panelVehiculoExistente.Visibility = Visibility.Visible;
                MostrarFormularioNuevoVehiculo();
                MessageBox.Show(
                    "Este cliente aún no tiene vehículos registrados.\nIngresa los datos del vehículo.",
                    "Sin vehículos", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void CmbVehiculosCliente_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbVehiculosCliente.SelectedItem is Vehiculo v)
            {
                _vehiculoResuelto = v;
                _nuevoVehiculoParaClienteExistente = false;

                txtVehiculoConfirmadoNombre.Text = $"{v.Marca} {v.Modelo}" +
                    (v.Anio.HasValue ? $"  ({v.Anio})" : "");
                txtVehiculoConfirmadoPlaca.Text = v.Placa;
                panelVehiculoConfirmado.Visibility = Visibility.Visible;
                panelNuevoVehiculoInline.Visibility = Visibility.Collapsed;
            }
        }

        private void LimpiarClienteSeleccionado_Click(object sender, RoutedEventArgs e)
        {
            _clienteResuelto = null;
            _vehiculoResuelto = null;
            _nuevoVehiculoParaClienteExistente = false;

            panelClienteSeleccionado.Visibility = Visibility.Collapsed;
            panelVehiculoExistente.Visibility = Visibility.Collapsed;
            panelNuevoVehiculoInline.Visibility = Visibility.Collapsed;
            panelVehiculoConfirmado.Visibility = Visibility.Collapsed;

            txtBuscarCliente.Clear();
            txtBuscarCliente.Focus();
        }

        private void OtroVehiculo_Click(object sender, RoutedEventArgs e)
        {
            _vehiculoResuelto = null;
            _nuevoVehiculoParaClienteExistente = true;
            MostrarFormularioNuevoVehiculo();
        }

        private void MostrarFormularioNuevoVehiculo()
        {
            panelNuevoVehiculoInline.Visibility = Visibility.Visible;
            panelVehiculoConfirmado.Visibility = Visibility.Collapsed;
            cmbVehiculosCliente.SelectedIndex = -1;
            txtMarcaNuevo.Focus();
        }

        private void ChkNuevoCliente_Changed(object sender, RoutedEventArgs e)
        {
            bool esNuevo = chkNuevoCliente.IsChecked == true;

            panelNuevoCliente.Visibility = esNuevo ? Visibility.Visible : Visibility.Collapsed;
            panelVehiculoNuevoCliente.Visibility = esNuevo ? Visibility.Visible : Visibility.Collapsed;

            if (esNuevo)
            {
                _clienteResuelto = null;
                _vehiculoResuelto = null;
                panelClienteSeleccionado.Visibility = Visibility.Collapsed;
                panelVehiculoExistente.Visibility = Visibility.Collapsed;
                txtNombre.Focus();
            }
        }

        // ── Registrar ─────────────────────────────────────────────────────────

        private void Registrar_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtDescripcion.Text))
            { Error("La descripción del trabajo es obligatoria."); return; }

            // ── NUEVO: validar el anticipo antes de continuar ──────────────────
            string anticipoStr = txtAnticipo.Text.Trim().Replace(",", ".");
            if (!decimal.TryParse(anticipoStr,
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out decimal anticipo) || anticipo < 0)
            {
                Error("El anticipo debe ser un número válido mayor o igual a 0.");
                txtAnticipo.Focus();
                return;
            }

            // Si hay precio estimado, advertir si el anticipo lo supera
            decimal precioEstimadoPreview = 0;
            decimal.TryParse(txtPrecio.Text.Trim().Replace(",", "."), out precioEstimadoPreview);
            if (anticipo > 0 && precioEstimadoPreview > 0 && anticipo > precioEstimadoPreview)
            {
                var continuar = MessageBox.Show(
                    $"El anticipo ingresado (Bs. {anticipo:N2}) es mayor al precio estimado " +
                    $"(Bs. {precioEstimadoPreview:N2}).\n\n¿Deseas continuar de todas formas?",
                    "Anticipo superior al estimado",
                    MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (continuar != MessageBoxResult.Yes) return;
            }

            try
            {
                using var db = new TallerDbContext();
                bool esNuevoCliente = chkNuevoCliente.IsChecked == true;

                // ── Resolver cliente ──────────────────────────────────────────
                if (esNuevoCliente)
                {
                    txtNombre.Text = ValidationHelper.AplicarTitleCase(txtNombre.Text);
                    txtApellido.Text = ValidationHelper.AplicarTitleCase(txtApellido.Text);
                    txtDireccion.Text = string.IsNullOrWhiteSpace(txtDireccion.Text)
                        ? "Sin dirección"
                        : ValidationHelper.AplicarPrimeraLetraMayuscula(txtDireccion.Text);
                    txtEmail.Text = txtEmail.Text.Trim().ToLower();

                    if (string.IsNullOrWhiteSpace(txtNombre.Text))
                    { Error("El nombre es obligatorio."); return; }
                    if (string.IsNullOrWhiteSpace(txtApellido.Text))
                    { Error("El apellido es obligatorio."); return; }
                    if (string.IsNullOrWhiteSpace(txtTelefono.Text))
                    { Error("El teléfono es obligatorio."); return; }
                    if (!ValidationHelper.EsTelefonoValido(txtTelefono.Text))
                    { Error(ValidationHelper.MsgTelefonoInvalido); return; }
                    if (!string.IsNullOrWhiteSpace(txtEmail.Text) &&
                        !ValidationHelper.EsCorreoValido(txtEmail.Text))
                    { Error(ValidationHelper.MsgCorreoInvalido); return; }

                    var existente = db.Clientes
                        .FirstOrDefault(c => c.Telefono == txtTelefono.Text.Trim());

                    if (existente != null)
                    {
                        var r = MessageBox.Show(
                            $"Ya existe un cliente con el teléfono '{txtTelefono.Text.Trim()}':\n\n" +
                            $"  {existente.Nombre} {existente.Apellido}\n\n" +
                            $"¿Usar ese cliente en lugar de crear uno nuevo?",
                            "Cliente duplicado", MessageBoxButton.YesNo, MessageBoxImage.Question);

                        if (r == MessageBoxResult.Yes)
                        {
                            SeleccionarCliente(existente);
                            esNuevoCliente = false;
                        }
                    }

                    if (esNuevoCliente)
                    {
                        var nuevo = new Cliente
                        {
                            Nombre = txtNombre.Text.Trim(),
                            Apellido = txtApellido.Text.Trim(),
                            Telefono = txtTelefono.Text.Trim(),
                            Correo = txtEmail.Text.Trim(),
                            Direccion = string.IsNullOrWhiteSpace(txtDireccion.Text)
                                            ? "Sin dirección" : txtDireccion.Text.Trim(),
                            FechaRegistro = DateTime.Now
                        };
                        db.Clientes.Add(nuevo);
                        db.SaveChanges();
                        _clienteResuelto = nuevo;
                    }
                }

                if (_clienteResuelto == null)
                { Error("Busca o registra un cliente antes de continuar."); return; }

                // ── Resolver vehículo ─────────────────────────────────────────
                if (esNuevoCliente || _nuevoVehiculoParaClienteExistente)
                {
                    var marcaTb = esNuevoCliente ? txtMarca : txtMarcaNuevo;
                    var modeloTb = esNuevoCliente ? txtModelo : txtModeloNuevo;
                    var placaTb = esNuevoCliente ? txtPlaca : txtPlacaNuevo;
                    var anioTxt = esNuevoCliente ? txtAnio.Text.Trim() : txtAnioNuevo.Text.Trim();

                    marcaTb.Text = ValidationHelper.AplicarTitleCase(marcaTb.Text);
                    modeloTb.Text = ValidationHelper.AplicarTitleCase(modeloTb.Text);

                    if (string.IsNullOrWhiteSpace(marcaTb.Text))
                    { Error("La marca del vehículo es obligatoria."); return; }
                    if (string.IsNullOrWhiteSpace(modeloTb.Text))
                    { Error("El modelo del vehículo es obligatorio."); return; }
                    if (string.IsNullOrWhiteSpace(placaTb.Text))
                    { Error("La placa del vehículo es obligatoria."); return; }
                    if (!ValidationHelper.EsPlacaValida(placaTb.Text))
                    { Error(ValidationHelper.MsgPlacaInvalida); return; }

                    string placa = ValidationHelper.FormatearPlaca(placaTb.Text);
                    placaTb.Text = placa;
                    int.TryParse(anioTxt, out int anio);

                    var vehiculoExistente = db.Vehiculos
                        .FirstOrDefault(v => v.Placa.ToUpper() == placa);

                    if (vehiculoExistente != null)
                    {
                        var r = MessageBox.Show(
                            $"La placa '{placa}' ya está registrada:\n\n" +
                            $"  {vehiculoExistente.Marca} {vehiculoExistente.Modelo}\n\n" +
                            $"¿Usar ese vehículo para este trabajo?",
                            "Placa duplicada", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                        _vehiculoResuelto = r == MessageBoxResult.Yes ? vehiculoExistente : null;
                        if (_vehiculoResuelto == null) return;
                    }
                    else
                    {
                        var nuevoVehiculo = new Vehiculo
                        {
                            ClienteID = _clienteResuelto.ClienteID,
                            Marca = marcaTb.Text,
                            Modelo = modeloTb.Text,
                            Placa = placa,
                            Anio = anio > 1900 && anio <= DateTime.Now.Year + 1 ? anio : null
                        };
                        db.Vehiculos.Add(nuevoVehiculo);
                        db.SaveChanges();
                        _vehiculoResuelto = nuevoVehiculo;
                    }
                }

                if (_vehiculoResuelto == null)
                { Error("Selecciona o registra un vehículo para el trabajo."); return; }

                // ── Crear trabajo ─────────────────────────────────────────────
                decimal.TryParse(txtPrecio.Text, out decimal precio);

                int? usuarioAsignadoId = cmbEmpleadoAsignado.SelectedValue as int?;

                var trabajo = new Trabajo
                {
                    VehiculoID = _vehiculoResuelto.VehiculoID,
                    FechaIngreso = DateTime.Now,
                    FechaEntrega = dpFechaEntrega.SelectedDate,
                    Descripcion = ValidationHelper.AplicarPrimeraLetraMayuscula(txtDescripcion.Text.Trim()),
                    Estado = (cmbEstado.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Pendiente",
                    TipoTrabajo = (cmbTipoTrabajo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Mecánica",
                    PrecioEstimado = precio > 0 ? precio : null,
                    PrecioFinal = null,
                    Anticipo = anticipo,
                    UsuarioAsignadoID = usuarioAsignadoId
                };

                db.Trabajos.Add(trabajo);
                db.SaveChanges();

                // ── NUEVO: registrar en auditoría ──────────────────────────────
                AuditoriaHelper.Registrar(
                    "Crear", "Trabajo", trabajo.TrabajoID,
                    $"Trabajo #{trabajo.TrabajoID} creado para {_clienteResuelto.Nombre} {_clienteResuelto.Apellido}" +
                    (anticipo > 0 ? $", anticipo Bs. {anticipo:N2}" : ""));

                string nombreEmpleado = "Sin asignar";
                if (usuarioAsignadoId.HasValue)
                {
                    var emp = db.Usuarios.Find(usuarioAsignadoId.Value);
                    if (emp != null) nombreEmpleado = emp.NombreCompleto;
                }

                MessageBox.Show(
                    $"✅  TRABAJO REGISTRADO EXITOSAMENTE\n\n" +
                    $"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n\n" +
                    $"📋  Trabajo #:    {trabajo.TrabajoID}\n" +
                    $"👤  Cliente:      {_clienteResuelto.Nombre} {_clienteResuelto.Apellido}\n" +
                    $"📞  Teléfono:     {_clienteResuelto.Telefono}\n" +
                    $"🚗  Vehículo:     {_vehiculoResuelto.Marca} {_vehiculoResuelto.Modelo}\n" +
                    $"🔑  Placa:        {_vehiculoResuelto.Placa}\n" +
                    $"🔧  Tipo:         {trabajo.TipoTrabajo}\n" +
                    $"⚙️   Estado:       {trabajo.Estado}\n" +
                    $"🧑‍🔧  Asignado a:   {nombreEmpleado}\n" +
                    (trabajo.PrecioEstimado.HasValue
                        ? $"💵  Estimado:     Bs. {trabajo.PrecioEstimado:N2}\n" : "") +
                    (anticipo > 0
                        ? $"💰  Anticipo:     Bs. {anticipo:N2}\n" : "") +
                    $"\n━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n\n" +
                    $"💡 Agrega servicios y repuestos desde el módulo Trabajos.",
                    "Trabajo Registrado", MessageBoxButton.OK, MessageBoxImage.Information);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al registrar el trabajo:\n{ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Error(string msg)
            => MessageBox.Show(msg, "Campo requerido",
                MessageBoxButton.OK, MessageBoxImage.Warning);

        private void Cancelar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
