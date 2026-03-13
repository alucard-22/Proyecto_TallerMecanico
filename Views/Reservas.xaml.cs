using Proyecto_taller.Data;
using Proyecto_taller.Models;
using Proyecto_taller.ViewModels;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Proyecto_taller.Views
{
    public partial class Reservas : UserControl
    {
        private List<Reserva> _todasLasReservas = new List<Reserva>();

        public Reservas()
        {
            InitializeComponent();
            CargarReservas();
            ActualizarEstadisticas();
        }

        /// <summary>
        /// Carga todas las reservas desde la base de datos
        /// </summary>
        private void CargarReservas()
        {
            try
            {
                using var db = new TallerDbContext();
                _todasLasReservas = db.Reservas
                    .Include(r => r.Vehiculo)
                        .ThenInclude(v => v.Cliente) // ⭐ Cliente se obtiene a través del vehículo
                    .OrderByDescending(r => r.FechaHoraCita)
                    .ToList();

                AplicarFiltros();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error al cargar reservas:\n{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Aplica los filtros seleccionados
        /// </summary>
        private void AplicarFiltros()
        {
            var reservasFiltradas = _todasLasReservas.AsEnumerable();

            // Filtro por estado
            if (rbPendientes.IsChecked == true)
                reservasFiltradas = reservasFiltradas.Where(r => r.Estado == "Pendiente");
            else if (rbConfirmadas.IsChecked == true)
                reservasFiltradas = reservasFiltradas.Where(r => r.Estado == "Confirmada");
            else if (rbEnCurso.IsChecked == true)
                reservasFiltradas = reservasFiltradas.Where(r => r.Estado == "En Curso");
            else if (rbCompletadas.IsChecked == true)
                reservasFiltradas = reservasFiltradas.Where(r => r.Estado == "Completada");
            else if (rbCanceladas.IsChecked == true)
                reservasFiltradas = reservasFiltradas.Where(r => r.Estado == "Cancelada" || r.Estado == "No Asistió");

            // Filtro por búsqueda
            if (!string.IsNullOrWhiteSpace(txtBuscar.Text))
            {
                var busqueda = txtBuscar.Text.ToLower();
                reservasFiltradas = reservasFiltradas.Where(r =>
                    (r.Vehiculo?.Cliente?.Nombre?.ToLower().Contains(busqueda) ?? false) ||
                    (r.Vehiculo?.Cliente?.Apellido?.ToLower().Contains(busqueda) ?? false) ||
                    (r.Vehiculo?.Cliente?.Telefono?.ToLower().Contains(busqueda) ?? false) ||
                    (r.Vehiculo?.Placa?.ToLower().Contains(busqueda) ?? false) ||
                    (r.Vehiculo?.Marca?.ToLower().Contains(busqueda) ?? false));
            }

            dgReservas.ItemsSource = reservasFiltradas.ToList();
        }

        /// <summary>
        /// Actualiza las estadísticas en el panel superior
        /// </summary>
        private void ActualizarEstadisticas()
        {
            var hoy = DateTime.Today;
            var inicioMes = new DateTime(hoy.Year, hoy.Month, 1);

            txtHoy.Text = _todasLasReservas.Count(r =>
                r.FechaHoraCita.Date == hoy &&
                (r.Estado == "Pendiente" || r.Estado == "Confirmada" || r.Estado == "En Curso")).ToString();

            txtPendientes.Text = _todasLasReservas.Count(r => r.Estado == "Pendiente").ToString();
            txtConfirmadas.Text = _todasLasReservas.Count(r => r.Estado == "Confirmada").ToString();
            txtEnCurso.Text = _todasLasReservas.Count(r => r.Estado == "En Curso").ToString();
            txtEsteMes.Text = _todasLasReservas.Count(r =>
                r.FechaHoraCita >= inicioMes &&
                r.FechaHoraCita < inicioMes.AddMonths(1)).ToString();
        }

        /// <summary>
        /// Maneja el cambio de estado desde el ComboBox
        /// </summary>
        private void EstadoComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboBox = sender as ComboBox;
            if (comboBox?.SelectedItem is ComboBoxItem item)
            {
                var nuevoEstado = item.Tag.ToString();
                var reserva = comboBox.DataContext as Reserva;

                if (reserva != null && reserva.Estado != nuevoEstado)
                {
                    ActualizarEstadoReserva(reserva, nuevoEstado);
                }
            }
        }

        /// <summary>
        /// Actualiza el estado de una reserva
        /// </summary>
        private void ActualizarEstadoReserva(Reserva reserva, string nuevoEstado)
        {
            try
            {
                using var db = new TallerDbContext();
                var reservaDb = db.Reservas.Find(reserva.ReservaID);

                if (reservaDb == null) return;

                // Actualizar estado y fechas correspondientes
                reservaDb.Estado = nuevoEstado;

                switch (nuevoEstado)
                {
                    case "Confirmada":
                        reservaDb.FechaConfirmacion = DateTime.Now;
                        break;
                    case "Completada":
                        reservaDb.FechaCompletado = DateTime.Now;
                        break;
                    case "Cancelada":
                    case "No Asistió":
                        reservaDb.FechaCancelacion = DateTime.Now;
                        break;
                }

                db.SaveChanges();

                // Actualizar objeto en memoria
                reserva.Estado = nuevoEstado;
                reserva.FechaConfirmacion = reservaDb.FechaConfirmacion;
                reserva.FechaCompletado = reservaDb.FechaCompletado;
                reserva.FechaCancelacion = reservaDb.FechaCancelacion;

                dgReservas.Items.Refresh();
                ActualizarEstadisticas();

                MessageBox.Show(
                    $"Estado actualizado a: {nuevoEstado}",
                    "Estado Actualizado",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error al actualizar estado:\n{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                CargarReservas();
            }
        }

        // ====== EVENTOS DE BOTONES ======

        private void Actualizar_Click(object sender, RoutedEventArgs e)
        {
            CargarReservas();
            ActualizarEstadisticas();
            MessageBox.Show("Reservas actualizadas.", "Actualizar",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void NuevaReserva_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var ventana = new NuevaReservaWindow();
                if (ventana.ShowDialog() == true)
                {
                    CargarReservas();
                    ActualizarEstadisticas();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al abrir ventana:\n{ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ConfirmarReserva_Click(object sender, RoutedEventArgs e)
        {
            var reserva = dgReservas.SelectedItem as Reserva;
            if (reserva == null)
            {
                MessageBox.Show("Seleccione una reserva.", "Advertencia",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (reserva.Estado != "Pendiente")
            {
                MessageBox.Show("Solo se pueden confirmar reservas pendientes.",
                    "Advertencia", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var resultado = MessageBox.Show(
                $"¿Confirmar la reserva de {reserva.Vehiculo?.Cliente?.Nombre}?\n\n" +
                $"📅 Fecha: {reserva.FechaHoraCita:dd/MM/yyyy HH:mm}\n" +
                $"🛠️ Servicio: {reserva.TipoServicio}",
                "Confirmar Reserva",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (resultado == MessageBoxResult.Yes)
            {
                ActualizarEstadoReserva(reserva, "Confirmada");
            }
        }

        private void IniciarTrabajo_Click(object sender, RoutedEventArgs e)
        {
            var reserva = dgReservas.SelectedItem as Reserva;
            if (reserva == null)
            {
                MessageBox.Show("Seleccione una reserva.", "Advertencia",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (reserva.Estado != "Confirmada" && reserva.Estado != "Pendiente")
            {
                MessageBox.Show("Solo se pueden iniciar reservas confirmadas o pendientes.",
                    "Advertencia", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var resultado = MessageBox.Show(
                $"¿Iniciar trabajo para {reserva.Vehiculo?.Cliente?.Nombre}?\n\n" +
                $"Esto marcará la reserva como 'En Curso'.",
                "Iniciar Trabajo",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (resultado == MessageBoxResult.Yes)
            {
                ActualizarEstadoReserva(reserva, "En Curso");
            }
        }

        private void CompletarReserva_Click(object sender, RoutedEventArgs e)
        {
            var reserva = dgReservas.SelectedItem as Reserva;
            if (reserva == null)
            {
                MessageBox.Show("Seleccione una reserva.", "Advertencia",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (reserva.Estado != "En Curso")
            {
                MessageBox.Show("Solo se pueden completar reservas en curso.",
                    "Advertencia", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var resultado = MessageBox.Show(
                $"¿Completar la reserva de {reserva.Vehiculo?.Cliente?.Nombre}?\n\n" +
                $"Esto marcará el servicio como finalizado.",
                "Completar Reserva",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (resultado == MessageBoxResult.Yes)
            {
                ActualizarEstadoReserva(reserva, "Completada");
            }
        }

        private void CancelarReserva_Click(object sender, RoutedEventArgs e)
        {
            var reserva = dgReservas.SelectedItem as Reserva;
            if (reserva == null)
            {
                MessageBox.Show("Seleccione una reserva.", "Advertencia",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (reserva.Estado == "Completada")
            {
                MessageBox.Show("No se pueden cancelar reservas completadas.",
                    "Advertencia", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var ventana = new CancelarReservaWindow();
            if (ventana.ShowDialog() == true)
            {
                try
                {
                    using var db = new TallerDbContext();
                    var reservaDb = db.Reservas.Find(reserva.ReservaID);
                    if (reservaDb != null)
                    {
                        reservaDb.Estado = ventana.EsNoShow ? "No Asistió" : "Cancelada";
                        reservaDb.FechaCancelacion = DateTime.Now;
                        reservaDb.MotivoCancelacion = ventana.Motivo;
                        db.SaveChanges();

                        CargarReservas();
                        ActualizarEstadisticas();

                        MessageBox.Show("Reserva cancelada exitosamente.", "Cancelar",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al cancelar:\n{ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void VerDetalles_Click(object sender, RoutedEventArgs e)
        {
            var reserva = dgReservas.SelectedItem as Reserva;
            if (reserva == null)
            {
                MessageBox.Show("Seleccione una reserva.", "Advertencia",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var ventana = new DetallesReservaWindow(reserva.ReservaID);
                ventana.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al abrir detalles:\n{ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EliminarReserva_Click(object sender, RoutedEventArgs e)
        {
            var reserva = dgReservas.SelectedItem as Reserva;
            if (reserva == null)
            {
                MessageBox.Show("Seleccione una reserva.", "Advertencia",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var resultado = MessageBox.Show(
                $"¿Está seguro de eliminar la reserva de {reserva.Vehiculo?.Cliente?.Nombre}?\n\n" +
                $"Esta acción no se puede deshacer.",
                "Eliminar Reserva",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (resultado == MessageBoxResult.Yes)
            {
                try
                {
                    using var db = new TallerDbContext();
                    var reservaDb = db.Reservas.Find(reserva.ReservaID);
                    if (reservaDb != null)
                    {
                        db.Reservas.Remove(reservaDb);
                        db.SaveChanges();

                        CargarReservas();
                        ActualizarEstadisticas();

                        MessageBox.Show("Reserva eliminada exitosamente.", "Eliminar",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al eliminar:\n{ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void FiltroEstado_Changed(object sender, RoutedEventArgs e)
        {
            AplicarFiltros();
        }

        private void TxtBuscar_TextChanged(object sender, TextChangedEventArgs e)
        {
            AplicarFiltros();
        }
    }
}