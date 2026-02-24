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
using Microsoft.EntityFrameworkCore;

namespace Proyecto_taller.Views
{
    public partial class Trabajos : Page
    {
        public Trabajos()
        {
            InitializeComponent();
            DataContext = new TrabajosViewModel();
        }

        /// <summary>
        /// Maneja el cambio de estado desde el ComboBox
        /// </summary>
        private void EstadoComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboBox = sender as ComboBox;
            if (comboBox == null) return;

            var trabajo = comboBox.DataContext as Trabajo;
            if (trabajo == null || trabajo.TrabajoID == 0) return;

            // Obtener el nuevo estado seleccionado
            var itemSeleccionado = comboBox.SelectedItem as ComboBoxItem;
            if (itemSeleccionado == null) return;

            string nuevoEstado = itemSeleccionado.Tag?.ToString() ?? itemSeleccionado.Content.ToString();

            // Limpiar el emoji del contenido si es necesario
            nuevoEstado = nuevoEstado.Replace("⏳", "").Replace("🔄", "").Replace("✅", "").Trim();

            // Evitar actualizar si no ha cambiado realmente
            if (trabajo.Estado == nuevoEstado) return;

            // Si cambia a "Finalizado", mostrar mensaje de confirmación
            if (nuevoEstado == "Finalizado")
            {
                var resultado = MessageBox.Show(
                    $"¿Está seguro de marcar este trabajo como FINALIZADO?\n\n" +
                    $"📋 Trabajo #{trabajo.TrabajoID}\n" +
                    $"🚗 {trabajo.Vehiculo?.Marca} {trabajo.Vehiculo?.Modelo}\n" +
                    $"👤 {trabajo.Vehiculo?.Cliente?.Nombre} {trabajo.Vehiculo?.Cliente?.Apellido}\n\n" +
                    $"⚠️ IMPORTANTE:\n" +
                    $"• Asegúrese de haber agregado todos los servicios y repuestos\n" +
                    $"• El precio final se calculará automáticamente\n" +
                    $"• Podrá generar la factura después de finalizar",
                    "Confirmar Finalización",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (resultado == MessageBoxResult.No)
                {
                    // Revertir el cambio en el ComboBox
                    comboBox.SelectedValue = trabajo.Estado;
                    return;
                }

                // Calcular y asignar precio final
                CalcularYFinalizarTrabajo(trabajo);
            }
            else
            {
                // Para cambios a "Pendiente" o "En Progreso", solo actualizar el estado
                ActualizarEstadoTrabajo(trabajo, nuevoEstado);
            }
        }

        /// <summary>
        /// Calcula el precio final y finaliza el trabajo
        /// </summary>
        private void CalcularYFinalizarTrabajo(Trabajo trabajo)
        {
            try
            {
                using var db = new TallerDbContext();
                var trabajoDb = db.Trabajos
                    .Include(t => t.Servicios)
                    .Include(t => t.Repuestos)
                    .FirstOrDefault(t => t.TrabajoID == trabajo.TrabajoID);

                if (trabajoDb == null) return;

                // ⭐ CALCULAR EL PRECIO FINAL
                decimal precioFinalCalculado = 0;

                // 1. Sumar servicios
                if (trabajoDb.Servicios != null && trabajoDb.Servicios.Any())
                {
                    precioFinalCalculado += trabajoDb.Servicios.Sum(s => s.Subtotal);
                }

                // 2. Sumar repuestos
                if (trabajoDb.Repuestos != null && trabajoDb.Repuestos.Any())
                {
                    precioFinalCalculado += trabajoDb.Repuestos.Sum(r => r.Subtotal);
                }

                // ⭐ Si no hay servicios ni repuestos, usar el precio estimado
                if (precioFinalCalculado == 0 && trabajoDb.PrecioEstimado.HasValue)
                {
                    precioFinalCalculado = trabajoDb.PrecioEstimado.Value;
                }

                // Actualizar trabajo
                trabajoDb.Estado = "Finalizado";
                trabajoDb.FechaEntrega = System.DateTime.Now;
                trabajoDb.PrecioFinal = precioFinalCalculado;

                db.SaveChanges();

                // Actualizar el objeto en memoria
                trabajo.Estado = "Finalizado";
                trabajo.FechaEntrega = trabajoDb.FechaEntrega;
                trabajo.PrecioFinal = trabajoDb.PrecioFinal;

                // Refrescar el DataGrid
                dgTrabajos.Items.Refresh();

                // Mensaje de éxito
                MessageBox.Show(
                    $"✅ TRABAJO FINALIZADO EXITOSAMENTE\n\n" +
                    $"📋 Trabajo #{trabajo.TrabajoID}\n" +
                    $"💰 Precio Final: Bs. {precioFinalCalculado:N2}\n" +
                    $"📅 Fecha Finalización: {trabajoDb.FechaEntrega:dd/MM/yyyy HH:mm}\n\n" +
                    $"💡 Puede generar la factura desde el módulo de Facturación.",
                    "Trabajo Finalizado",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(
                    $"❌ Error al finalizar el trabajo:\n{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                // Revertir el cambio
                trabajo.Estado = trabajo.Estado == "Finalizado" ? "En Progreso" : trabajo.Estado;
                dgTrabajos.Items.Refresh();
            }
        }

        /// <summary>
        /// Actualiza solo el estado del trabajo (sin finalizar)
        /// </summary>
        private void ActualizarEstadoTrabajo(Trabajo trabajo, string nuevoEstado)
        {
            try
            {
                using var db = new TallerDbContext();
                var trabajoDb = db.Trabajos.Find(trabajo.TrabajoID);

                if (trabajoDb != null)
                {
                    trabajoDb.Estado = nuevoEstado;
                    db.SaveChanges();

                    // Actualizar el objeto en memoria
                    trabajo.Estado = nuevoEstado;

                    // Refrescar el DataGrid para actualizar colores
                    dgTrabajos.Items.Refresh();

                    // Notificación breve
                    MessageBox.Show(
                        $"✅ Estado actualizado a: {nuevoEstado}\n\n" +
                        $"Trabajo #{trabajo.TrabajoID}",
                        "Estado Actualizado",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(
                    $"❌ Error al actualizar el estado:\n{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}
