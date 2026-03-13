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
            CargarVehiculos();

            // Configurar fecha mínima (hoy)
            dpFecha.DisplayDateStart = DateTime.Today;
            dpFecha.SelectedDate = DateTime.Today;
        }

        private void CargarVehiculos()
        {
            try
            {
                using var db = new TallerDbContext();
                // ⭐ Cargar vehículos con información del cliente
                var vehiculos = db.Vehiculos
                    .Include(v => v.Cliente)
                    .OrderBy(v => v.Cliente.Nombre)
                    .Select(v => new
                    {
                        v.VehiculoID,
                        Descripcion = v.Cliente.Nombre + " " + v.Cliente.Apellido +
                                    " - " + v.Marca + " " + v.Modelo +
                                    " (" + v.Placa + ")"
                    })
                    .ToList();

                cmbVehiculo.ItemsSource = vehiculos;
                cmbVehiculo.DisplayMemberPath = "Descripcion";
                cmbVehiculo.SelectedValuePath = "VehiculoID";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar vehículos:\n{ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Guardar_Click(object sender, RoutedEventArgs e)
        {
            // Validaciones
            if (cmbVehiculo.SelectedValue == null)
            {
                MessageBox.Show("Seleccione un vehículo.", "Validación",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!dpFecha.SelectedDate.HasValue)
            {
                MessageBox.Show("Seleccione una fecha.", "Validación",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (cmbHora.SelectedItem == null)
            {
                MessageBox.Show("Seleccione una hora.", "Validación",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (cmbTipoServicio.SelectedItem == null)
            {
                MessageBox.Show("Seleccione un tipo de servicio.", "Validación",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using var db = new TallerDbContext();

                // Crear fecha y hora completa
                var fecha = dpFecha.SelectedDate.Value;
                var horaTexto = (cmbHora.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content.ToString();
                var hora = TimeSpan.Parse(horaTexto);
                var fechaHoraCita = fecha.Date + hora;

                // Validar que la fecha no sea en el pasado
                if (fechaHoraCita < DateTime.Now)
                {
                    MessageBox.Show("No puede crear una reserva en el pasado.",
                        "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // ⭐ Crear reserva solo con VehiculoID
                var nuevaReserva = new Reserva
                {
                    VehiculoID = (int)cmbVehiculo.SelectedValue,
                    FechaReserva = DateTime.Now,
                    FechaHoraCita = fechaHoraCita,
                    TipoServicio = (cmbTipoServicio.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content.ToString(),
                    Observaciones = txtObservaciones.Text,
                    Estado = "Pendiente",
                    Prioridad = (cmbPrioridad.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Tag.ToString() ?? "Normal"
                };

                if (!string.IsNullOrWhiteSpace(txtPrecioEstimado.Text))
                {
                    if (decimal.TryParse(txtPrecioEstimado.Text, out decimal precio))
                    {
                        nuevaReserva.PrecioEstimado = precio;
                    }
                }

                db.Reservas.Add(nuevaReserva);
                db.SaveChanges();

                // Obtener nombre del cliente para el mensaje
                var vehiculo = db.Vehiculos.Include(v => v.Cliente).FirstOrDefault(v => v.VehiculoID == nuevaReserva.VehiculoID);
                var nombreCliente = vehiculo?.Cliente != null
                    ? $"{vehiculo.Cliente.Nombre} {vehiculo.Cliente.Apellido}"
                    : "Cliente";

                MessageBox.Show(
                    $"✅ Reserva creada exitosamente\n\n" +
                    $"📅 Fecha y Hora: {fechaHoraCita:dd/MM/yyyy HH:mm}\n" +
                    $"👤 Cliente: {nombreCliente}\n" +
                    $"🛠️ Servicio: {nuevaReserva.TipoServicio}",
                    "Reserva Creada",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al guardar reserva:\n{ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancelar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}