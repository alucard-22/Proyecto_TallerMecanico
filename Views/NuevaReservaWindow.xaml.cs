using Microsoft.EntityFrameworkCore;
using Proyecto_taller.Data;
using Proyecto_taller.Helpers;
using Proyecto_taller.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Proyecto_taller.Views
{
    public partial class NuevaReservaWindow : Window
    {
        // Cliente y vehículo resueltos (buscado o nuevo)
        private Cliente _clienteResuelto = null;
        private Vehiculo _vehiculoResuelto = null;

        public NuevaReservaWindow()
        {
            InitializeComponent();
            dpFecha.DisplayDateStart = DateTime.Today;
            dpFecha.SelectedDate = DateTime.Today.AddDays(1);
        }

        // ─────────────────────────────────────────────────────────
        //  BÚSQUEDA DE CLIENTE EXISTENTE
        // ─────────────────────────────────────────────────────────

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

            // Mostrar badge de cliente seleccionado
            txtClienteSeleccionadoNombre.Text = $"{cliente.Nombre} {cliente.Apellido}";
            txtClienteSeleccionadoTel.Text = cliente.Telefono;
            txtClienteSeleccionadoCorreo.Text = string.IsNullOrWhiteSpace(cliente.Correo) ? "Sin correo" : cliente.Correo;
            panelClienteSeleccionado.Visibility = Visibility.Visible;

            // Desactivar el formulario de nuevo cliente
            chkNuevoCliente.IsChecked = false;
            panelNuevoCliente.Visibility = Visibility.Collapsed;

            // Cargar vehículos del cliente
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

        // ─────────────────────────────────────────────────────────
        //  TOGGLE: NUEVO CLIENTE
        // ─────────────────────────────────────────────────────────

        private void ChkNuevoCliente_Changed(object sender, RoutedEventArgs e)
        {
            bool esNuevo = chkNuevoCliente.IsChecked == true;
            panelNuevoCliente.Visibility = esNuevo ? Visibility.Visible : Visibility.Collapsed;

            // Si cambia a nuevo cliente, limpiar el cliente seleccionado
            if (esNuevo)
            {
                _clienteResuelto = null;
                _vehiculoResuelto = null;
                panelClienteSeleccionado.Visibility = Visibility.Collapsed;
                panelVehiculoExistente.Visibility = Visibility.Collapsed;
            }
        }

        // ─────────────────────────────────────────────────────────
        //  NUEVO VEHÍCULO (para cliente existente sin vehículos)
        // ─────────────────────────────────────────────────────────

        private void NuevoVehiculo_Click(object sender, RoutedEventArgs e)
        {
            // Mostrar inline el formulario de vehículo del bloque "nuevo cliente"
            // La forma más simple: pedir los datos en un cuadro de diálogo pequeño
            var dlg = new NuevoVehiculoDialog(_clienteResuelto);
            if (dlg.ShowDialog() == true)
            {
                // Recargar vehículos del cliente
                using var db = new TallerDbContext();
                var vehiculos = db.Vehiculos
                    .Where(v => v.ClienteID == _clienteResuelto.ClienteID)
                    .OrderBy(v => v.Marca)
                    .ToList();

                cmbVehiculosCliente.ItemsSource = vehiculos;
                cmbVehiculosCliente.SelectedIndex = vehiculos.Count - 1;
            }
        }

        // ─────────────────────────────────────────────────────────
        //  FECHA MÍNIMA
        // ─────────────────────────────────────────────────────────

        private void DpFecha_Changed(object sender, SelectionChangedEventArgs e)
        {
            // No se permite seleccionar una fecha en el pasado
            if (dpFecha.SelectedDate.HasValue && dpFecha.SelectedDate.Value.Date < DateTime.Today)
            {
                dpFecha.SelectedDate = DateTime.Today;
                MessageBox.Show("No se puede reservar en una fecha pasada.",
                    "Fecha inválida", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        // ─────────────────────────────────────────────────────────
        //  GUARDAR
        // ─────────────────────────────────────────────────────────

        private void Guardar_Click(object sender, RoutedEventArgs e)
        {
            // ── Validaciones básicas ─────────────────────────────
            if (!dpFecha.SelectedDate.HasValue)
            { Error("Selecciona una fecha para la cita."); return; }

            if (cmbHora.SelectedItem == null)
            { Error("Selecciona una hora para la cita."); return; }

            if (cmbTipoServicio.SelectedItem == null)
            { Error("Selecciona el tipo de servicio."); return; }

            // ── Construir fecha+hora ─────────────────────────────
            string horaStr = (cmbHora.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "09:00";
            DateTime fechaHora = dpFecha.SelectedDate.Value.Date + TimeSpan.Parse(horaStr);

            if (fechaHora < DateTime.Now)
            { Error("La fecha y hora de la cita no pueden estar en el pasado."); return; }

            string tipoServicio = (cmbTipoServicio.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "";
            string prioridad = (cmbPrioridad.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "Normal";

            decimal? precioEstimado = null;
            if (decimal.TryParse(txtPrecioEstimado.Text.Trim(), out decimal pe) && pe > 0)
                precioEstimado = pe;

            // ── Resolver cliente y vehículo ──────────────────────
            try
            {
                using var db = new TallerDbContext();

                bool esNuevo = chkNuevoCliente.IsChecked == true;

                if (esNuevo)
                {
                    // Validar campos obligatorios del nuevo cliente
                    if (string.IsNullOrWhiteSpace(txtNombre.Text)) { Error("El nombre es obligatorio."); return; }
                    if (string.IsNullOrWhiteSpace(txtApellido.Text)) { Error("El apellido es obligatorio."); return; }
                    if (string.IsNullOrWhiteSpace(txtTelefono.Text)) { Error("El teléfono es obligatorio."); return; }
                    if (string.IsNullOrWhiteSpace(txtMarca.Text)) { Error("La marca del vehículo es obligatoria."); return; }
                    if (string.IsNullOrWhiteSpace(txtModelo.Text)) { Error("El modelo del vehículo es obligatorio."); return; }
                    if (string.IsNullOrWhiteSpace(txtPlaca.Text)) { Error("La placa del vehículo es obligatoria."); return; }

                    // Buscar por teléfono por si ya existe
                    var clienteExistente = db.Clientes
                        .FirstOrDefault(c => c.Telefono == txtTelefono.Text.Trim());

                    if (clienteExistente != null)
                    {
                        var r = MessageBox.Show(
                            $"Ya existe un cliente con el teléfono '{txtTelefono.Text.Trim()}':\n\n" +
                            $"  {clienteExistente.Nombre} {clienteExistente.Apellido}\n\n" +
                            $"¿Usar ese cliente en lugar de crear uno nuevo?",
                            "Cliente duplicado",
                            MessageBoxButton.YesNo, MessageBoxImage.Question);

                        if (r == MessageBoxResult.Yes)
                        {
                            SeleccionarCliente(clienteExistente);
                            // Ahora _clienteResuelto está asignado, continuar
                            esNuevo = false;
                        }
                    }

                    if (esNuevo)
                    {
                        // Crear cliente
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

                        // Crear vehículo
                        int.TryParse(txtAnio.Text.Trim(), out int anio);
                        var nuevoVehiculo = new Vehiculo
                        {
                            ClienteID = nuevoCliente.ClienteID,
                            Marca = txtMarca.Text.Trim(),
                            Modelo = txtModelo.Text.Trim(),
                            Placa = txtPlaca.Text.Trim().ToUpper(),
                            Anio = anio > 1900 && anio <= DateTime.Now.Year + 1 ? anio : null
                        };

                        // Verificar placa duplicada
                        var placaDup = db.Vehiculos
                            .FirstOrDefault(v => v.Placa.ToUpper() == nuevoVehiculo.Placa);
                        if (placaDup != null)
                        {
                            var r2 = MessageBox.Show(
                                $"La placa '{nuevoVehiculo.Placa}' ya está registrada a nombre de otro cliente.\n\n¿Usar ese vehículo de todas formas?",
                                "Placa duplicada", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                            if (r2 == MessageBoxResult.Yes)
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

                // Verificar que tenemos cliente y vehículo
                if (_clienteResuelto == null)
                { Error("Busca o registra un cliente antes de guardar."); return; }

                if (_vehiculoResuelto == null)
                { Error("Selecciona o registra un vehículo para la reserva."); return; }

                // ── Crear la reserva ─────────────────────────────
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

                // ── Mensaje de éxito ─────────────────────────────
                MessageBox.Show(
                    $"✅  RESERVA CREADA EXITOSAMENTE\n\n" +
                    $"👤  {_clienteResuelto.Nombre} {_clienteResuelto.Apellido}\n" +
                    $"🚗  {_vehiculoResuelto.Marca} {_vehiculoResuelto.Modelo} — {_vehiculoResuelto.Placa}\n" +
                    $"📅  {fechaHora:dd/MM/yyyy HH:mm}\n" +
                    $"🔧  {tipoServicio}\n" +
                    $"⭐  Prioridad: {prioridad}\n" +
                    (precioEstimado.HasValue ? $"💵  Estimado: Bs. {precioEstimado:N2}\n" : ""),
                    "Reserva Creada", MessageBoxButton.OK, MessageBoxImage.Information);

                // ── WhatsApp de confirmación ──────────────────────
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

        // ─────────────────────────────────────────────────────────
        //  HELPERS
        // ─────────────────────────────────────────────────────────

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