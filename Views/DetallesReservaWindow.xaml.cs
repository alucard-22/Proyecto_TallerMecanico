using Proyecto_taller.Data;
using Proyecto_taller.Models;
using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
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

        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }

        private void CargarDetalles()
        {
            try
            {
                using var db = new TallerDbContext();
                _reserva = db.Reservas
                    .Include(r => r.Vehiculo)
                        .ThenInclude(v => v.Cliente)
                    .FirstOrDefault(r => r.ReservaID == _reservaId);

                if (_reserva == null)
                {
                    MessageBox.Show("No se encontró la reserva.", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    Close();
                    return;
                }

                txtTitulo.Text = $"Reserva #{_reserva.ReservaID}";
                txtEstado.Text = _reserva.Estado;
                txtEstado.Foreground = _reserva.Estado switch
                {
                    "Pendiente" => new SolidColorBrush(Color.FromRgb(245, 124, 0)),
                    "Confirmada" => new SolidColorBrush(Color.FromRgb(56, 142, 60)),
                    "En Curso" => new SolidColorBrush(Color.FromRgb(2, 136, 209)),
                    "Completada" => new SolidColorBrush(Color.FromRgb(46, 125, 50)),
                    "Cancelada" => new SolidColorBrush(Color.FromRgb(211, 47, 47)),
                    "No Asistió" => new SolidColorBrush(Color.FromRgb(194, 24, 91)),
                    _ => new SolidColorBrush(Color.FromRgb(108, 117, 125))
                };

                txtFechaHoraCita.Text = _reserva.FechaHoraCita.ToString("dddd, dd/MM/yyyy HH:mm");
                txtTipoServicio.Text = _reserva.TipoServicio;
                txtPrioridad.Text = _reserva.Prioridad;
                txtObservaciones.Text = string.IsNullOrWhiteSpace(_reserva.Observaciones)
                    ? "Sin observaciones" : _reserva.Observaciones;
                txtPrecioEstimado.Text = _reserva.PrecioEstimado.HasValue
                    ? $"Bs. {_reserva.PrecioEstimado:N2}" : "No especificado";

                if (_reserva.Vehiculo?.Cliente != null)
                {
                    var cliente = _reserva.Vehiculo.Cliente;
                    txtNombreCliente.Text = $"{cliente.Nombre} {cliente.Apellido}";
                    txtTelefonoCliente.Text = cliente.Telefono ?? "N/A";
                    txtEmailCliente.Text = string.IsNullOrWhiteSpace(cliente.Correo)
                        ? "N/A" : cliente.Correo;
                }

                if (_reserva.Vehiculo != null)
                {
                    txtMarcaModelo.Text = $"{_reserva.Vehiculo.Marca} {_reserva.Vehiculo.Modelo}";
                    txtPlaca.Text = _reserva.Vehiculo.Placa;
                    txtAnio.Text = _reserva.Vehiculo.Anio.HasValue
                        ? _reserva.Vehiculo.Anio.ToString() : "N/A";
                    panelVehiculo.Visibility = Visibility.Visible;
                }
                else
                {
                    panelVehiculo.Visibility = Visibility.Collapsed;
                }

                txtFechaCreacion.Text = _reserva.FechaReserva.ToString("dd/MM/yyyy HH:mm");
                txtFechaConfirmacion.Text = _reserva.FechaConfirmacion.HasValue
                    ? _reserva.FechaConfirmacion.Value.ToString("dd/MM/yyyy HH:mm") : "No confirmada";
                txtFechaCompletado.Text = _reserva.FechaCompletado.HasValue
                    ? _reserva.FechaCompletado.Value.ToString("dd/MM/yyyy HH:mm") : "No completada";

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

        private void Cerrar_Click(object sender, RoutedEventArgs e) => Close();
    }
}
