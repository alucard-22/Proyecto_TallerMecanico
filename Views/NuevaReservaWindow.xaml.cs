using Microsoft.EntityFrameworkCore;
using Proyecto_taller.Data;
using Proyecto_taller.Helpers;
using Proyecto_taller.Models;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Proyecto_taller.Views
{
    public partial class NuevaReservaWindow : Window
    {
        private Cliente _clienteResuelto = null;
        private Vehiculo _vehiculoResuelto = null;

        // Ventana de tolerancia para considerar dos reservas "en conflicto"
        // de horario. Una reserva exactamente a la misma hora, o muy cerca,
        // probablemente significa que el taller no puede atender a ambos
        // clientes simultáneamente con los mismos recursos.
        private static readonly TimeSpan VentanaConflicto = TimeSpan.FromMinutes(30);

        public NuevaReservaWindow()
        {
            InitializeComponent();
            dpFecha.DisplayDateStart = DateTime.Today;
            dpFecha.SelectedDate = DateTime.Today.AddDays(1);
            SuscribirEventosValidacion();
        }

        private void SuscribirEventosValidacion()
        {
            txtNombre.LostFocus += (s, e) => AplicarTitleCase(txtNombre);
            txtApellido.LostFocus += (s, e) => AplicarTitleCase(txtApellido);
            txtEmail.LostFocus += (s, e) => AplicarMinusculas(txtEmail);
            txtPlaca.TextChanged += (s, e) => AplicarMayusculasInline(txtPlaca);
            txtMarca.LostFocus += (s, e) => AplicarTitleCase(txtMarca);
            txtModelo.LostFocus += (s, e) => AplicarTitleCase(txtModelo);
        }

        private static void AplicarTitleCase(TextBox tb)
        {
            if (!string.IsNullOrWhiteSpace(tb.Text))
                tb.Text = ValidationHelper.AplicarTitleCase(tb.Text);
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

            using var db = new TallerDbContext();
            var vehiculos = db.Vehiculos
                .Where(v => v.ClienteID == cliente.ClienteID)
                .OrderBy(v => v.Marca)
                .ToList();

            if (vehiculos.Count > 0)
            {
                cmbVehiculosCliente.ItemsSource = vehiculos;
                cmbVehiculosCliente.SelectedIndex = 0;
                panelVehiculoExistente.Visibility = Visibility.Visible;
            }
            else
            {
                panelVehiculoExistente.Visibility = Visibility.Collapsed;
                _vehiculoResuelto = null;
            }
        }

        private void LimpiarClienteSeleccionado_Click(object sender, RoutedEventArgs e)
        {
            _clienteResuelto = null;
            _vehiculoResuelto = null;
            panelClienteSeleccionado.Visibility = Visibility.Collapsed;
            panelVehiculoExistente.Visibility = Visibility.Collapsed;
            txtBuscarCliente.Clear();
            txtBuscarCliente.Focus();
        }

        private void CmbVehiculosCliente_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _vehiculoResuelto = cmbVehiculosCliente.SelectedItem as Vehiculo;
        }

        private void ChkNuevoCliente_Changed(object sender, RoutedEventArgs e)
        {
            bool esNuevo = chkNuevoCliente.IsChecked == true;
            panelNuevoCliente.Visibility = esNuevo ? Visibility.Visible : Visibility.Collapsed;

            if (esNuevo)
            {
                _clienteResuelto = null;
                _vehiculoResuelto = null;
                panelClienteSeleccionado.Visibility = Visibility.Collapsed;
                panelVehiculoExistente.Visibility = Visibility.Collapsed;
            }
        }

        private void NuevoVehiculo_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new NuevoVehiculoDialog(_clienteResuelto);
            if (dlg.ShowDialog() == true)
            {
                using var db = new TallerDbContext();
                var vehiculos = db.Vehiculos
                    .Where(v => v.ClienteID == _clienteResuelto.ClienteID)
                    .OrderBy(v => v.Marca)
                    .ToList();

                cmbVehiculosCliente.ItemsSource = vehiculos;
                cmbVehiculosCliente.SelectedIndex = vehiculos.Count - 1;
            }
        }

        private void DpFecha_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (dpFecha.SelectedDate.HasValue && dpFecha.SelectedDate.Value.Date < DateTime.Today)
            {
                dpFecha.SelectedDate = DateTime.Today;
                MessageBox.Show("No se puede reservar en una fecha pasada.",
                    "Fecha inválida", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        // ── NUEVO: validar reservas duplicadas en la misma fecha/hora ─────────
        // Devuelve la reserva en conflicto si existe alguna otra reserva activa
        // (no cancelada) dentro de la ventana de tolerancia configurada.
        private Reserva BuscarConflictoHorario(DateTime fechaHora)
        {
            using var db = new TallerDbContext();

            var inicio = fechaHora - VentanaConflicto;
            var fin = fechaHora + VentanaConflicto;

            return db.Reservas
                .Include(r => r.Vehiculo).ThenInclude(v => v.Cliente)
                .Where(r => r.Estado != "Cancelada" && r.Estado != "No Asistió")
                .Where(r => r.FechaHoraCita >= inicio && r.FechaHoraCita <= fin)
                .OrderBy(r => r.FechaHoraCita)
                .FirstOrDefault();
        }

        // ── Guardar ───────────────────────────────────────────────────────────

        private void Guardar_Click(object sender, RoutedEventArgs e)
        {
            if (!dpFecha.SelectedDate.HasValue)
            { Error("Selecciona una fecha para la cita."); return; }

            if (cmbHora.SelectedItem == null)
            { Error("Selecciona una hora para la cita."); return; }

            if (cmbTipoServicio.SelectedItem == null)
            { Error("Selecciona el tipo de servicio."); return; }

            string horaStr = (cmbHora.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "09:00";
            DateTime fechaHora = dpFecha.SelectedDate.Value.Date + TimeSpan.Parse(horaStr);

            if (fechaHora < DateTime.Now)
            { Error("La fecha y hora de la cita no pueden estar en el pasado."); return; }

            // ── NUEVO: verificar conflicto de horario antes de continuar ───────
            var conflicto = BuscarConflictoHorario(fechaHora);
            if (conflicto != null)
            {
                var nombreConflicto = $"{conflicto.Vehiculo?.Cliente?.Nombre} {conflicto.Vehiculo?.Cliente?.Apellido}";
                var r = MessageBox.Show(
                    $"Ya existe una reserva muy cercana a este horario:\n\n" +
                    $"👤  {nombreConflicto}\n" +
                    $"📅  {conflicto.FechaHoraCita:dd/MM/yyyy HH:mm}\n" +
                    $"🔧  {conflicto.TipoServicio}\n\n" +
                    $"El taller podría no tener capacidad para atender ambas citas " +
                    $"al mismo tiempo.\n\n¿Deseas continuar y registrar esta reserva de todas formas?",
                    "Posible conflicto de horario",
                    MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (r != MessageBoxResult.Yes) return;
            }

            string tipoServicio = (cmbTipoServicio.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "";
            string prioridad = (cmbPrioridad.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "Normal";

            decimal? precioEstimado = null;
            if (decimal.TryParse(txtPrecioEstimado.Text.Trim(), out decimal pe) && pe > 0)
                precioEstimado = pe;

            try
            {
                using var db = new TallerDbContext();
                bool esNuevo = chkNuevoCliente.IsChecked == true;

                if (esNuevo)
                {
                    txtNombre.Text = ValidationHelper.AplicarTitleCase(txtNombre.Text);
                    txtApellido.Text = ValidationHelper.AplicarTitleCase(txtApellido.Text);
                    txtEmail.Text = txtEmail.Text.Trim().ToLower();
                    txtPlaca.Text = ValidationHelper.AplicarMayusculas(txtPlaca.Text);
                    txtMarca.Text = ValidationHelper.AplicarTitleCase(txtMarca.Text);
                    txtModelo.Text = ValidationHelper.AplicarTitleCase(txtModelo.Text);

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
                    if (string.IsNullOrWhiteSpace(txtMarca.Text))
                    { Error("La marca del vehículo es obligatoria."); return; }
                    if (string.IsNullOrWhiteSpace(txtModelo.Text))
                    { Error("El modelo del vehículo es obligatorio."); return; }
                    if (string.IsNullOrWhiteSpace(txtPlaca.Text))
                    { Error("La placa del vehículo es obligatoria."); return; }
                    if (!ValidationHelper.EsPlacaValida(txtPlaca.Text))
                    { Error(ValidationHelper.MsgPlacaInvalida); return; }

                    txtPlaca.Text = ValidationHelper.FormatearPlaca(txtPlaca.Text);

                    var clienteExistente = db.Clientes
                        .FirstOrDefault(c => c.Telefono == txtTelefono.Text.Trim());

                    if (clienteExistente != null)
                    {
                        var r2 = MessageBox.Show(
                            $"Ya existe un cliente con el teléfono '{txtTelefono.Text.Trim()}':\n\n" +
                            $"  {clienteExistente.Nombre} {clienteExistente.Apellido}\n\n" +
                            $"¿Usar ese cliente en lugar de crear uno nuevo?",
                            "Cliente duplicado", MessageBoxButton.YesNo, MessageBoxImage.Question);

                        if (r2 == MessageBoxResult.Yes)
                        {
                            SeleccionarCliente(clienteExistente);
                            esNuevo = false;
                        }
                    }

                    if (esNuevo)
                    {
                        var nuevoCliente = new Cliente
                        {
                            Nombre = txtNombre.Text.Trim(),
                            Apellido = txtApellido.Text.Trim(),
                            Telefono = txtTelefono.Text.Trim(),
                            Correo = txtEmail.Text.Trim(),
                            Direccion = "Sin dirección",
                            FechaRegistro = DateTime.Now
                        };
                        db.Clientes.Add(nuevoCliente);
                        db.SaveChanges();
                        _clienteResuelto = nuevoCliente;

                        int.TryParse(txtAnio.Text.Trim(), out int anio);
                        string placa = txtPlaca.Text;

                        var nuevoVehiculo = new Vehiculo
                        {
                            ClienteID = nuevoCliente.ClienteID,
                            Marca = txtMarca.Text.Trim(),
                            Modelo = txtModelo.Text.Trim(),
                            Placa = placa,
                            Anio = anio > 1900 && anio <= DateTime.Now.Year + 1 ? anio : null
                        };

                        var placaDup = db.Vehiculos.FirstOrDefault(v => v.Placa.ToUpper() == placa);
                        if (placaDup != null)
                        {
                            var r3 = MessageBox.Show(
                                $"La placa '{placa}' ya está registrada a nombre de otro cliente.\n\n¿Usar ese vehículo de todas formas?",
                                "Placa duplicada", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                            if (r3 == MessageBoxResult.Yes)
                                _vehiculoResuelto = placaDup;
                            else
                                return;
                        }
                        else
                        {
                            db.Vehiculos.Add(nuevoVehiculo);
                            db.SaveChanges();
                            _vehiculoResuelto = nuevoVehiculo;
                        }
                    }
                }

                if (_clienteResuelto == null)
                { Error("Busca o registra un cliente antes de guardar."); return; }

                if (_vehiculoResuelto == null)
                { Error("Selecciona o registra un vehículo para la reserva."); return; }

                var reserva = new Reserva
                {
                    VehiculoID = _vehiculoResuelto.VehiculoID,
                    FechaReserva = DateTime.Now,
                    FechaHoraCita = fechaHora,
                    TipoServicio = tipoServicio,
                    Observaciones = txtObservaciones.Text.Trim(),
                    Estado = "Pendiente",
                    Prioridad = prioridad,
                    PrecioEstimado = precioEstimado
                };

                db.Reservas.Add(reserva);
                db.SaveChanges();

                MessageBox.Show(
                    $"✅  RESERVA CREADA EXITOSAMENTE\n\n" +
                    $"👤  {_clienteResuelto.Nombre} {_clienteResuelto.Apellido}\n" +
                    $"🚗  {_vehiculoResuelto.Marca} {_vehiculoResuelto.Modelo} — {_vehiculoResuelto.Placa}\n" +
                    $"📅  {fechaHora:dd/MM/yyyy HH:mm}\n" +
                    $"🔧  {tipoServicio}\n" +
                    $"⭐  Prioridad: {prioridad}\n" +
                    (precioEstimado.HasValue ? $"💵  Estimado: Bs. {precioEstimado:N2}\n" : ""),
                    "Reserva Creada", MessageBoxButton.OK, MessageBoxImage.Information);

                if (chkEnviarWhatsApp.IsChecked == true &&
                    !string.IsNullOrWhiteSpace(_clienteResuelto.Telefono))
                {
                    WhatsAppHelper.EnviarConfirmacion(
                        _clienteResuelto.Telefono,
                        $"{_clienteResuelto.Nombre} {_clienteResuelto.Apellido}",
                        fechaHora,
                        tipoServicio,
                        precioEstimado);
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al guardar la reserva:\n{ex.Message}",
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
