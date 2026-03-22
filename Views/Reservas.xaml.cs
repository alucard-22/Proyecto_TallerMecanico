using Microsoft.EntityFrameworkCore;
using Proyecto_taller.Data;
using Proyecto_taller.Helpers;
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
    /// <summary>
    /// Módulo de Reservas — corregido:
    ///   • Ahora es Page (no UserControl), se navega correctamente
    ///   • Sin ReservasViewModel desconectado (todo en code-behind)
    ///   • Estado manejado con botones explícitos (sin ComboBox inline que causaba bugs)
    ///   • Validación de transiciones de estado
    ///   • WhatsApp integrado en Confirmar y Cancelar
    /// </summary>
    public partial class Reservas : Page
    {
        private List<Reserva> _todas = new();
        private bool _cargado = false;

        public Reservas()
        {
            InitializeComponent();
            Loaded += (_, __) => { _cargado = true; Cargar(); };
        }

        // ─────────────────────────────────────────────────────────
        //  CARGA Y FILTRO
        // ─────────────────────────────────────────────────────────

        private void Cargar()
        {
            try
            {
                using var db = new TallerDbContext();
                _todas = db.Reservas
                    .Include(r => r.Vehiculo).ThenInclude(v => v.Cliente)
                    .OrderByDescending(r => r.FechaHoraCita)
                    .ToList();

                AplicarFiltro();
                ActualizarEstadisticas();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar reservas:\n{ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AplicarFiltro()
        {
            if (!_cargado) return;

            IEnumerable<Reserva> lista = _todas;

            if (rbPendientes?.IsChecked == true) lista = lista.Where(r => r.Estado == "Pendiente");
            else if (rbConfirmadas?.IsChecked == true) lista = lista.Where(r => r.Estado == "Confirmada");
            else if (rbEnCurso?.IsChecked == true) lista = lista.Where(r => r.Estado == "En Curso");
            else if (rbCompletadas?.IsChecked == true) lista = lista.Where(r => r.Estado == "Completada");
            else if (rbCanceladas?.IsChecked == true)
                lista = lista.Where(r => r.Estado == "Cancelada" || r.Estado == "No Asistió");

            string busq = txtBuscar?.Text?.Trim().ToLower() ?? "";
            if (!string.IsNullOrEmpty(busq))
                lista = lista.Where(r =>
                    (r.Vehiculo?.Cliente?.Nombre?.ToLower().Contains(busq) ?? false) ||
                    (r.Vehiculo?.Cliente?.Apellido?.ToLower().Contains(busq) ?? false) ||
                    (r.Vehiculo?.Cliente?.Telefono?.Contains(busq) ?? false) ||
                    (r.Vehiculo?.Placa?.ToLower().Contains(busq) ?? false) ||
                    (r.Vehiculo?.Marca?.ToLower().Contains(busq) ?? false) ||
                    (r.TipoServicio?.ToLower().Contains(busq) ?? false));

            var result = lista.ToList();
            dgReservas.ItemsSource = result;
            txtTotalReservas.Text = _todas.Count.ToString();
        }

        private void ActualizarEstadisticas()
        {
            var hoy = DateTime.Today;
            var inicioM = new DateTime(hoy.Year, hoy.Month, 1);

            statHoy.Text = _todas.Count(r => r.FechaHoraCita.Date == hoy &&
                                                   r.Estado is "Pendiente" or "Confirmada" or "En Curso").ToString();
            statPendientes.Text = _todas.Count(r => r.Estado == "Pendiente").ToString();
            statConfirmadas.Text = _todas.Count(r => r.Estado == "Confirmada").ToString();
            statEnCurso.Text = _todas.Count(r => r.Estado == "En Curso").ToString();
            statMes.Text = _todas.Count(r => r.FechaHoraCita >= inicioM &&
                                                    r.FechaHoraCita < inicioM.AddMonths(1)).ToString();
        }

        // ─────────────────────────────────────────────────────────
        //  EVENTOS DE FILTRO / BÚSQUEDA
        // ─────────────────────────────────────────────────────────

        private void Filtro_Changed(object sender, RoutedEventArgs e)
            => AplicarFiltro();

        private void TxtBuscar_Changed(object sender, TextChangedEventArgs e)
            => AplicarFiltro();

        // ─────────────────────────────────────────────────────────
        //  RESERVA SELECCIONADA
        // ─────────────────────────────────────────────────────────

        private Reserva SeleccionActual => dgReservas.SelectedItem as Reserva;

        private bool ValidarSeleccion(string accion = "realizar esta acción")
        {
            if (SeleccionActual != null) return true;
            MessageBox.Show($"Selecciona una reserva para {accion}.",
                "Sin selección", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        // ─────────────────────────────────────────────────────────
        //  BOTONES DE ACCIÓN
        // ─────────────────────────────────────────────────────────

        private void Actualizar_Click(object sender, RoutedEventArgs e)
        {
            Cargar();
            MessageBox.Show("Reservas actualizadas.", "Actualizar",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void NuevaReserva_Click(object sender, RoutedEventArgs e)
        {
            var win = new NuevaReservaWindow();
            if (win.ShowDialog() == true) Cargar();
        }

        private void Confirmar_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidarSeleccion("confirmar")) return;
            var r = SeleccionActual;

            if (r.Estado != "Pendiente")
            {
                MessageBox.Show("Solo se pueden confirmar reservas en estado Pendiente.",
                    "Estado inválido", MessageBoxButton.OK, MessageBoxImage.Warning); return;
            }

            var resp = MessageBox.Show(
                $"¿Confirmar la reserva de {r.Vehiculo?.Cliente?.Nombre} {r.Vehiculo?.Cliente?.Apellido}?\n\n" +
                $"📅 {r.FechaHoraCita:dd/MM/yyyy HH:mm}\n🔧 {r.TipoServicio}\n\n" +
                $"Se enviará confirmación por WhatsApp si el cliente tiene número registrado.",
                "Confirmar Reserva", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (resp != MessageBoxResult.Yes) return;

            CambiarEstado(r.ReservaID, "Confirmada", fechaConfirmacion: DateTime.Now);

            // WhatsApp de confirmación
            var tel = r.Vehiculo?.Cliente?.Telefono;
            if (!string.IsNullOrWhiteSpace(tel))
                WhatsAppHelper.EnviarConfirmacion(
                    tel,
                    $"{r.Vehiculo?.Cliente?.Nombre} {r.Vehiculo?.Cliente?.Apellido}",
                    r.FechaHoraCita,
                    r.TipoServicio,
                    r.PrecioEstimado);
        }

        private void Iniciar_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidarSeleccion("iniciar")) return;
            var r = SeleccionActual;

            if (r.Estado is not ("Pendiente" or "Confirmada"))
            {
                MessageBox.Show("Solo se pueden iniciar reservas Pendientes o Confirmadas.",
                    "Estado inválido", MessageBoxButton.OK, MessageBoxImage.Warning); return;
            }

            var resp = MessageBox.Show(
                $"¿Iniciar el trabajo para {r.Vehiculo?.Cliente?.Nombre}?\n\nEsto marcará la reserva como 'En Curso'.",
                "Iniciar Trabajo", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (resp == MessageBoxResult.Yes) CambiarEstado(r.ReservaID, "En Curso");
        }

        private void Completar_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidarSeleccion("completar")) return;
            var r = SeleccionActual;

            if (r.Estado != "En Curso")
            {
                MessageBox.Show("Solo se pueden completar reservas que están En Curso.",
                    "Estado inválido", MessageBoxButton.OK, MessageBoxImage.Warning); return;
            }

            var resp = MessageBox.Show(
                $"¿Marcar como completada la reserva de {r.Vehiculo?.Cliente?.Nombre}?",
                "Completar Reserva", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (resp == MessageBoxResult.Yes)
                CambiarEstado(r.ReservaID, "Completada", fechaCompletado: DateTime.Now);
        }

        private void Cancelar_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidarSeleccion("cancelar")) return;
            var r = SeleccionActual;

            if (r.Estado == "Completada")
            {
                MessageBox.Show("No se pueden cancelar reservas ya completadas.",
                    "Operación no permitida", MessageBoxButton.OK, MessageBoxImage.Warning); return;
            }

            var win = new CancelarReservaWindow();
            if (win.ShowDialog() != true) return;

            string nuevoEstado = win.EsNoShow ? "No Asistió" : "Cancelada";
            CambiarEstado(r.ReservaID, nuevoEstado,
                fechaCancelacion: DateTime.Now, motivo: win.Motivo);

            // WhatsApp de cancelación
            var tel = r.Vehiculo?.Cliente?.Telefono;
            if (!string.IsNullOrWhiteSpace(tel))
                WhatsAppHelper.EnviarCancelacion(
                    tel,
                    $"{r.Vehiculo?.Cliente?.Nombre} {r.Vehiculo?.Cliente?.Apellido}",
                    r.FechaHoraCita,
                    r.TipoServicio,
                    win.Motivo);
        }

        private void WhatsApp_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidarSeleccion("enviar WhatsApp")) return;
            var r = SeleccionActual;

            var tel = r.Vehiculo?.Cliente?.Telefono;
            if (string.IsNullOrWhiteSpace(tel))
            {
                MessageBox.Show("Este cliente no tiene número de teléfono registrado.",
                    "Sin teléfono", MessageBoxButton.OK, MessageBoxImage.Warning); return;
            }

            // Mostrar opciones de mensaje
            var win = new WhatsAppManualWindow(r);
            win.ShowDialog();
        }

        private void Detalles_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidarSeleccion("ver detalles")) return;
            var win = new DetallesReservaWindow(SeleccionActual.ReservaID);
            win.ShowDialog();
        }

        private void Eliminar_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidarSeleccion("eliminar")) return;
            var r = SeleccionActual;

            var resp = MessageBox.Show(
                $"¿Eliminar la reserva #{r.ReservaID} de {r.Vehiculo?.Cliente?.Nombre}?\n\nEsta acción no se puede deshacer.",
                "Confirmar Eliminación", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (resp != MessageBoxResult.Yes) return;

            try
            {
                using var db = new TallerDbContext();
                var reservaDb = db.Reservas.Find(r.ReservaID);
                if (reservaDb != null) { db.Reservas.Remove(reservaDb); db.SaveChanges(); }
                Cargar();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al eliminar:\n{ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void dgReservas_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (SeleccionActual == null) return;
            var win = new DetallesReservaWindow(SeleccionActual.ReservaID);
            win.ShowDialog();
        }

        // ─────────────────────────────────────────────────────────
        //  CAMBIO DE ESTADO — función centralizada
        // ─────────────────────────────────────────────────────────

        private void CambiarEstado(
            int reservaId,
            string nuevoEstado,
            DateTime? fechaConfirmacion = null,
            DateTime? fechaCompletado = null,
            DateTime? fechaCancelacion = null,
            string motivo = "")
        {
            try
            {
                using var db = new TallerDbContext();
                var reserva = db.Reservas.Find(reservaId);
                if (reserva == null) return;

                reserva.Estado = nuevoEstado;

                if (fechaConfirmacion.HasValue) reserva.FechaConfirmacion = fechaConfirmacion;
                if (fechaCompletado.HasValue) reserva.FechaCompletado = fechaCompletado;
                if (fechaCancelacion.HasValue)
                {
                    reserva.FechaCancelacion = fechaCancelacion;
                    reserva.MotivoCancelacion = motivo;
                }

                db.SaveChanges();

                MessageBox.Show($"Estado actualizado: {nuevoEstado}",
                    "Actualizado", MessageBoxButton.OK, MessageBoxImage.Information);

                Cargar();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cambiar estado:\n{ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}