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
    public partial class DetallesReservaWindow : Window
    {
        private int _reservaId;
        private Reserva _reserva;

        public DetallesReservaWindow(int reservaId)
        {
            InitializeComponent();
            _reservaId = reservaId;
            CargarDetalles();
        }

        private void CargarDetalles()
        {
            try
            {
                using var db = new TallerDbContext();
                _reserva = db.Reservas
                    .Include(r => r.Vehiculo)
                        .ThenInclude(v => v.Cliente) // ⭐ Cliente a través del vehículo
                    .FirstOrDefault(r => r.ReservaID == _reservaId);

                if (_reserva == null)
                {
                    MessageBox.Show("No se encontró la reserva.", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    Close();
                    return;
                }

                // Header
                txtTitulo.Text = $"Reserva #{_reserva.ReservaID}";
                txtEstado.Text = _reserva.Estado;
                txtEstado.Foreground = _reserva.Estado switch
                {
                    "Pendiente" => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(245, 124, 0)),
                    "Confirmada" => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(56, 142, 60)),
                    "En Curso" => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(2, 136, 209)),
                    "Completada" => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(46, 125, 50)),
                    "Cancelada" => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(211, 47, 47)),
                    "No Asistió" => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(194, 24, 91)),
                    _ => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(108, 117, 125))
                };

                // Información de la Reserva
                txtFechaHoraCita.Text = _reserva.FechaHoraCita.ToString("dddd, dd/MM/yyyy HH:mm");
                txtTipoServicio.Text = _reserva.TipoServicio;
                txtPrioridad.Text = _reserva.Prioridad;
                txtObservaciones.Text = string.IsNullOrWhiteSpace(_reserva.Observaciones)
                    ? "Sin observaciones" : _reserva.Observaciones;
                txtPrecioEstimado.Text = _reserva.PrecioEstimado.HasValue
                    ? $"Bs. {_reserva.PrecioEstimado:N2}" : "No especificado";

                // Cliente (a través del vehículo)
                if (_reserva.Vehiculo?.Cliente != null)
                {
                    var cliente = _reserva.Vehiculo.Cliente;
                    txtNombreCliente.Text = $"{cliente.Nombre} {cliente.Apellido}";
                    txtTelefonoCliente.Text = cliente.Telefono ?? "N/A";
                    txtEmailCliente.Text = string.IsNullOrWhiteSpace(cliente.Correo)
                        ? "N/A" : cliente.Correo;
                }

                // Vehículo
                if (_reserva.Vehiculo != null)
                {
                    txtMarcaModelo.Text = $"{_reserva.Vehiculo.Marca} {_reserva.Vehiculo.Modelo}";
                    txtPlaca.Text = _reserva.Vehiculo.Placa;
                    txtAnio.Text = _reserva.Vehiculo.Anio.HasValue ? _reserva.Vehiculo.Anio.ToString() : "N/A";
                    panelVehiculo.Visibility = Visibility.Visible;
                }
                else
                {
                    panelVehiculo.Visibility = Visibility.Collapsed;
                }

                // Fechas
                txtFechaCreacion.Text = _reserva.FechaReserva.ToString("dd/MM/yyyy HH:mm");
                txtFechaConfirmacion.Text = _reserva.FechaConfirmacion.HasValue
                    ? _reserva.FechaConfirmacion.Value.ToString("dd/MM/yyyy HH:mm") : "No confirmada";
                txtFechaCompletado.Text = _reserva.FechaCompletado.HasValue
                    ? _reserva.FechaCompletado.Value.ToString("dd/MM/yyyy HH:mm") : "No completada";

                // Cancelación
                if (!string.IsNullOrWhiteSpace(_reserva.MotivoCancelacion))
                {
                    txtMotivoCancelacion.Text = _reserva.MotivoCancelacion;
                    txtFechaCancelacion.Text = _reserva.FechaCancelacion.HasValue
                        ? _reserva.FechaCancelacion.Value.ToString("dd/MM/yyyy HH:mm") : "";
                    panelCancelacion.Visibility = Visibility.Visible;
                }
                else
                {
                    panelCancelacion.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar detalles:\n{ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cerrar_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
