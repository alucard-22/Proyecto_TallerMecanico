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
    public partial class NuevaReservaWindow : Window
    {
        public NuevaReservaWindow()
        {
            InitializeComponent();

            // Fecha mínima: hoy
            dpFecha.DisplayDateStart = DateTime.Today;
            dpFecha.SelectedDate = DateTime.Today;

            txtNombre.Focus();
        }

        // ══════════════════════════════════════════════════════
        //  GUARDAR
        // ══════════════════════════════════════════════════════

        private void Guardar_Click(object sender, RoutedEventArgs e)
        {
            // ── Validaciones ──────────────────────────────────
            if (string.IsNullOrWhiteSpace(txtNombre.Text))
            {
                MostrarError("El nombre del cliente es obligatorio.");
                txtNombre.Focus(); return;
            }
            if (string.IsNullOrWhiteSpace(txtApellido.Text))
            {
                MostrarError("El apellido del cliente es obligatorio.");
                txtApellido.Focus(); return;
            }
            if (string.IsNullOrWhiteSpace(txtTelefono.Text))
            {
                MostrarError("El teléfono es obligatorio.");
                txtTelefono.Focus(); return;
            }
            if (string.IsNullOrWhiteSpace(txtMarca.Text))
            {
                MostrarError("La marca del vehículo es obligatoria.");
                txtMarca.Focus(); return;
            }
            if (string.IsNullOrWhiteSpace(txtModelo.Text))
            {
                MostrarError("El modelo del vehículo es obligatorio.");
                txtModelo.Focus(); return;
            }
            if (string.IsNullOrWhiteSpace(txtPlaca.Text))
            {
                MostrarError("La placa es obligatoria.");
                txtPlaca.Focus(); return;
            }
            if (!dpFecha.SelectedDate.HasValue)
            {
                MostrarError("Selecciona una fecha para la cita.");
                return;
            }
            if (cmbHora.SelectedItem == null)
            {
                MostrarError("Selecciona una hora para la cita.");
                return;
            }
            if (cmbTipoServicio.SelectedItem == null)
            {
                MostrarError("Selecciona el tipo de servicio.");
                return;
            }

            //  Construir fecha + hora 
            var horaTexto = (cmbHora.SelectedItem as ComboBoxItem)?.Content.ToString()!;
            var fechaHora = dpFecha.SelectedDate.Value.Date + TimeSpan.Parse(horaTexto);

            if (fechaHora < DateTime.Now)
            {
                MostrarError("La fecha y hora de la cita no pueden estar en el pasado.");
                return;
            }

            //  Precio estimado opcional 
            decimal? precioEstimado = null;
            if (!string.IsNullOrWhiteSpace(txtPrecioEstimado.Text) &&
                decimal.TryParse(txtPrecioEstimado.Text, out decimal precio) && precio > 0)
                precioEstimado = precio;

            var tipoServicio = (cmbTipoServicio.SelectedItem as ComboBoxItem)?.Content.ToString()!;
            var prioridad = (cmbPrioridad.SelectedItem as ComboBoxItem)?.Tag.ToString() ?? "Normal";
            var placaNorm = txtPlaca.Text.Trim().ToUpper();

            try
            {
                using var db = new TallerDbContext();

                //  1. Buscar o crear CLIENTE 
                // Busca por teléfono — identificador más estable
                var cliente = db.Clientes.FirstOrDefault(c =>
                    c.Telefono == txtTelefono.Text.Trim());

                bool clienteNuevo = false;
                if (cliente == null)
                {
                    clienteNuevo = true;
                    cliente = new Cliente
                    {
                        Nombre = txtNombre.Text.Trim(),
                        Apellido = txtApellido.Text.Trim(),
                        Telefono = txtTelefono.Text.Trim(),
                        Correo = txtEmail.Text.Trim(),
                        Direccion = "Sin dirección",
                        FechaRegistro = DateTime.Now
                    };
                    db.Clientes.Add(cliente);
                    db.SaveChanges();
                }

                //  2. Buscar o crear VEHÍCULO 
                // Busca por placa — identificador único del vehículo
                var vehiculo = db.Vehiculos
                    .FirstOrDefault(v => v.Placa.ToUpper() == placaNorm);

                bool vehiculoNuevo = false;
                if (vehiculo == null)
                {
                    vehiculoNuevo = true;
                    int.TryParse(txtAnio.Text.Trim(), out int anio);

                    vehiculo = new Vehiculo
                    {
                        ClienteID = cliente.ClienteID,
                        Marca = txtMarca.Text.Trim(),
                        Modelo = txtModelo.Text.Trim(),
                        Placa = placaNorm,
                        Anio = anio > 1900 && anio <= DateTime.Now.Year + 1 ? anio : null
                    };
                    db.Vehiculos.Add(vehiculo);
                    db.SaveChanges();
                }
                else
                {
                    // El vehículo existe — si es de otro cliente, preguntar
                    if (vehiculo.ClienteID != cliente.ClienteID)
                    {
                        var propietario = db.Clientes.Find(vehiculo.ClienteID);
                        var resp = MessageBox.Show(
                            $"La placa {placaNorm} ya está registrada a nombre de:\n" +
                            $"  {propietario?.Nombre} {propietario?.Apellido}\n\n" +
                            $"¿Desea asignarlo al cliente actual " +
                            $"({cliente.Nombre} {cliente.Apellido})?",
                            "Vehículo existente",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Question);

                        if (resp == MessageBoxResult.Yes)
                        {
                            vehiculo.ClienteID = cliente.ClienteID;
                            db.SaveChanges();
                        }
                    }
                }

                //  3. Crear RESERVA 
                var reserva = new Reserva
                {
                    VehiculoID = vehiculo.VehiculoID,
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

                // 4. Confirmación al operador 
                string resumen =
                    $"✅ RESERVA CREADA EXITOSAMENTE\n\n" +
                    $"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n\n" +
                    $"📅 Fecha:     {fechaHora:dd/MM/yyyy HH:mm}\n" +
                    $"🛠️ Servicio:  {tipoServicio}\n" +
                    $"⭐ Prioridad: {prioridad}\n\n" +
                    $"👤 Cliente:   {cliente.Nombre} {cliente.Apellido}" +
                    (clienteNuevo ? "  ✨ nuevo" : "") + "\n" +
                    $"📞 Teléfono:  {cliente.Telefono}\n\n" +
                    $"🚗 Vehículo:  {vehiculo.Marca} {vehiculo.Modelo}" +
                    (vehiculo.Anio.HasValue ? $" ({vehiculo.Anio})" : "") + "\n" +
                    $"🔑 Placa:     {vehiculo.Placa}" +
                    (vehiculoNuevo ? "  ✨ nuevo" : "");

                MessageBox.Show(resumen, "Reserva Creada",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                // ── 5. Ofrecer WhatsApp de confirmación ────────
                EnviarWhatsAppConfirmacion(
                    cliente.Telefono,
                    $"{cliente.Nombre} {cliente.Apellido}",
                    fechaHora,
                    tipoServicio,
                    precioEstimado);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error al guardar la reserva:\n{ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ══════════════════════════════════════════════════════
        //  WHATSAPP
        // ══════════════════════════════════════════════════════

        private void EnviarWhatsAppConfirmacion(
            string telefono,
            string nombre,
            DateTime fechaHora,
            string tipoServicio,
            decimal? precioEstimado)
        {
            if (string.IsNullOrWhiteSpace(telefono)) return;

            var resp = MessageBox.Show(
                $"¿Enviar confirmación por WhatsApp al cliente?\n\n" +
                $"👤 {nombre}\n" +
                $"📞 {telefono}\n" +
                $"📅 {fechaHora:dd/MM/yyyy HH:mm}\n" +
                $"🛠️ {tipoServicio}",
                "Enviar WhatsApp",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (resp != MessageBoxResult.Yes) return;

            //try
            //{
            //    WhatsAppHelper.EnviarConfirmacionReserva(
            //        telefono, nombre, fechaHora, tipoServicio, precioEstimado);
            //}
            //catch (Exception ex)
            //{
            //    MessageBox.Show(
            //        $"No se pudo abrir WhatsApp Web:\n{ex.Message}",
            //        "Error WhatsApp", MessageBoxButton.OK, MessageBoxImage.Warning);
            //}
        }

        
        //  HELPERS
        
        private void MostrarError(string mensaje) =>
            MessageBox.Show(mensaje, "Campo requerido",
                MessageBoxButton.OK, MessageBoxImage.Warning);

        private void Cancelar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}