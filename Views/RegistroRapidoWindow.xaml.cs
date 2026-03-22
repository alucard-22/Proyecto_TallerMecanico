using Proyecto_taller.Data;
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
using Microsoft.EntityFrameworkCore;

namespace Proyecto_taller.Views
{
    public partial class RegistroRapidoWindow : Window
    {
        // ── Estado resuelto ───────────────────────────────────────
        private Cliente _clienteResuelto = null;
        private Vehiculo _vehiculoResuelto = null;

        // Indica si el usuario eligió "Otro vehículo" para un cliente existente
        private bool _nuevoVehiculoParaClienteExistente = false;

        public RegistroRapidoWindow()
        {
            InitializeComponent();
            dpFechaEntrega.SelectedDate = DateTime.Now.AddDays(3);
            dpFechaEntrega.DisplayDateStart = DateTime.Today;
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

            // Mostrar badge verde
            txtClienteSeleccionadoNombre.Text = $"{cliente.Nombre} {cliente.Apellido}";
            txtClienteSeleccionadoTel.Text = cliente.Telefono;
            txtClienteSeleccionadoCorreo.Text = string.IsNullOrWhiteSpace(cliente.Correo)
                ? "Sin correo" : cliente.Correo;
            panelClienteSeleccionado.Visibility = Visibility.Visible;

            // Desactivar formulario de nuevo cliente
            chkNuevoCliente.IsChecked = false;
            panelNuevoCliente.Visibility = Visibility.Collapsed;
            panelVehiculoNuevoCliente.Visibility = Visibility.Collapsed;

            // Cargar vehículos del cliente
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

                // Ocultar formulario de vehículo nuevo inline si estaba visible
                panelNuevoVehiculoInline.Visibility = Visibility.Collapsed;
                panelVehiculoConfirmado.Visibility = Visibility.Collapsed;

                panelVehiculoExistente.Visibility = Visibility.Visible;
            }
            else
            {
                // El cliente no tiene vehículos — mostrar directo el formulario inline
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

                // Mostrar badge de vehículo seleccionado
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

        // ─────────────────────────────────────────────────────────
        //  "OTRO VEHÍCULO" para cliente existente
        // ─────────────────────────────────────────────────────────

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

        // ─────────────────────────────────────────────────────────
        //  TOGGLE: NUEVO CLIENTE
        // ─────────────────────────────────────────────────────────

        private void ChkNuevoCliente_Changed(object sender, RoutedEventArgs e)
        {
            bool esNuevo = chkNuevoCliente.IsChecked == true;

            panelNuevoCliente.Visibility = esNuevo ? Visibility.Visible : Visibility.Collapsed;
            panelVehiculoNuevoCliente.Visibility = esNuevo ? Visibility.Visible : Visibility.Collapsed;

            if (esNuevo)
            {
                // Limpiar cliente seleccionado previo
                _clienteResuelto = null;
                _vehiculoResuelto = null;
                panelClienteSeleccionado.Visibility = Visibility.Collapsed;
                panelVehiculoExistente.Visibility = Visibility.Collapsed;
                txtNombre.Focus();
            }
        }

        // ─────────────────────────────────────────────────────────
        //  REGISTRAR
        // ─────────────────────────────────────────────────────────

        private void Registrar_Click(object sender, RoutedEventArgs e)
        {
            // ── Validar datos del trabajo ────────────────────────
            if (string.IsNullOrWhiteSpace(txtDescripcion.Text))
            { Error("La descripción del trabajo es obligatoria."); return; }

            try
            {
                using var db = new TallerDbContext();
                bool esNuevoCliente = chkNuevoCliente.IsChecked == true;

                // ─────────────────────────────────────────────────
                //  RESOLVER CLIENTE
                // ─────────────────────────────────────────────────
                if (esNuevoCliente)
                {
                    if (string.IsNullOrWhiteSpace(txtNombre.Text)) { Error("El nombre es obligatorio."); return; }
                    if (string.IsNullOrWhiteSpace(txtApellido.Text)) { Error("El apellido es obligatorio."); return; }
                    if (string.IsNullOrWhiteSpace(txtTelefono.Text)) { Error("El teléfono es obligatorio."); return; }

                    // Verificar si ya existe por teléfono
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

                // ─────────────────────────────────────────────────
                //  RESOLVER VEHÍCULO
                // ─────────────────────────────────────────────────
                if (esNuevoCliente || _nuevoVehiculoParaClienteExistente)
                {
                    // Determinar qué campos usar
                    var marca = esNuevoCliente ? txtMarca.Text.Trim() : txtMarcaNuevo.Text.Trim();
                    var modelo = esNuevoCliente ? txtModelo.Text.Trim() : txtModeloNuevo.Text.Trim();
                    var placa = esNuevoCliente ? txtPlaca.Text.Trim() : txtPlacaNuevo.Text.Trim();
                    var anioTxt = esNuevoCliente ? txtAnio.Text.Trim() : txtAnioNuevo.Text.Trim();

                    if (string.IsNullOrWhiteSpace(marca)) { Error("La marca del vehículo es obligatoria."); return; }
                    if (string.IsNullOrWhiteSpace(modelo)) { Error("El modelo del vehículo es obligatorio."); return; }
                    if (string.IsNullOrWhiteSpace(placa)) { Error("La placa del vehículo es obligatoria."); return; }

                    placa = placa.ToUpper();
                    int.TryParse(anioTxt, out int anio);

                    // Verificar placa duplicada
                    var vehiculoExistente = db.Vehiculos
                        .FirstOrDefault(v => v.Placa.ToUpper() == placa);

                    if (vehiculoExistente != null)
                    {
                        var r = MessageBox.Show(
                            $"La placa '{placa}' ya está registrada:\n\n" +
                            $"  {vehiculoExistente.Marca} {vehiculoExistente.Modelo}\n\n" +
                            $"¿Usar ese vehículo para este trabajo?",
                            "Placa duplicada", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                        _vehiculoResuelto = r == MessageBoxResult.Yes
                            ? vehiculoExistente
                            : null;

                        if (_vehiculoResuelto == null) return;
                    }
                    else
                    {
                        var nuevoVehiculo = new Vehiculo
                        {
                            ClienteID = _clienteResuelto.ClienteID,
                            Marca = marca,
                            Modelo = modelo,
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

                // ─────────────────────────────────────────────────
                //  CREAR TRABAJO
                // ─────────────────────────────────────────────────
                decimal.TryParse(txtPrecio.Text, out decimal precio);

                var trabajo = new Trabajo
                {
                    VehiculoID = _vehiculoResuelto.VehiculoID,
                    FechaIngreso = DateTime.Now,
                    FechaEntrega = dpFechaEntrega.SelectedDate,
                    Descripcion = txtDescripcion.Text.Trim(),
                    Estado = (cmbEstado.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Pendiente",
                    TipoTrabajo = (cmbTipoTrabajo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Mecánica",
                    PrecioEstimado = precio > 0 ? precio : null,
                    PrecioFinal = null
                };

                db.Trabajos.Add(trabajo);
                db.SaveChanges();

                // ── Mensaje de éxito ─────────────────────────────
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
                    (trabajo.PrecioEstimado.HasValue
                        ? $"💵  Estimado:     Bs. {trabajo.PrecioEstimado:N2}\n" : "") +
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
