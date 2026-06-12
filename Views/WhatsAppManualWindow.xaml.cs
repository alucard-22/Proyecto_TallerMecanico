using Proyecto_taller.Helpers;
using Proyecto_taller.Models;
using System.Windows;
using System.Windows.Input;

namespace Proyecto_taller.Views
{
    public partial class WhatsAppManualWindow : Window
    {
        private readonly Reserva _reserva;

        public WhatsAppManualWindow(Reserva reserva)
        {
            InitializeComponent();
            _reserva = reserva;

            var cliente = reserva.Vehiculo?.Cliente;
            txtClienteInfo.Text =
                $"{cliente?.Nombre} {cliente?.Apellido}  ·  {cliente?.Telefono}  ·  {reserva.FechaHoraCita:dd/MM/yyyy HH:mm}";
        }

        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }

        private void EnviarConfirmacion_Click(object sender, RoutedEventArgs e)
        {
            var c = _reserva.Vehiculo?.Cliente;
            WhatsAppHelper.EnviarConfirmacion(
                c?.Telefono ?? "",
                $"{c?.Nombre} {c?.Apellido}",
                _reserva.FechaHoraCita,
                _reserva.TipoServicio,
                _reserva.PrecioEstimado);
            Close();
        }

        private void EnviarRecordatorio_Click(object sender, RoutedEventArgs e)
        {
            var c = _reserva.Vehiculo?.Cliente;
            WhatsAppHelper.EnviarRecordatorio(
                c?.Telefono ?? "",
                $"{c?.Nombre} {c?.Apellido}",
                _reserva.FechaHoraCita,
                _reserva.TipoServicio);
            Close();
        }

        private void EnviarCancelacion_Click(object sender, RoutedEventArgs e)
        {
            var c = _reserva.Vehiculo?.Cliente;
            WhatsAppHelper.EnviarCancelacion(
                c?.Telefono ?? "",
                $"{c?.Nombre} {c?.Apellido}",
                _reserva.FechaHoraCita,
                _reserva.TipoServicio);
            Close();
        }

        private void Cerrar_Click(object sender, RoutedEventArgs e) => Close();
    }
}
