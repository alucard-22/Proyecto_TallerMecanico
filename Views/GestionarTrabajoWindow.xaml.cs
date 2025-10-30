using Proyecto_taller.Data;
using Proyecto_taller.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;

namespace Proyecto_taller.Views
{
    public partial class GestionarTrabajoWindow : Window
    {
        private int _trabajoId;
        private Trabajo _trabajo;
        private List<Servicio> _todosLosServicios;
        private List<Repuesto> _todosLosRepuestos;
        private List<ServicioTrabajoItem> _serviciosTrabajo;
        private List<RepuestoTrabajoItem> _repuestosTrabajo;

        public GestionarTrabajoWindow(int trabajoId)
        {
            InitializeComponent();
            _trabajoId = trabajoId;
            _serviciosTrabajo = new List<ServicioTrabajoItem>();
            _repuestosTrabajo = new List<RepuestoTrabajoItem>();

            CargarDatos();
        }

        private void CargarDatos()
        {
            try
            {
                using var db = new TallerDbContext();

                // Cargar trabajo con sus relaciones
                _trabajo = db.Trabajos
                    .Include(t => t.Vehiculo)
                        .ThenInclude(v => v.Cliente)
                    .Include(t => t.Servicios)
                        .ThenInclude(ts => ts.Servicio)
                    .Include(t => t.Repuestos)
                        .ThenInclude(tr => tr.Repuesto)
                    .FirstOrDefault(t => t.TrabajoID == _trabajoId);

                if (_trabajo == null)
                {
                    MessageBox.Show("No se encontró el trabajo.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    Close();
                    return;
                }

                // Actualizar información del header
                txtTituloTrabajo.Text = $"🔧 Gestionar Trabajo #{_trabajo.TrabajoID}";
                txtInfoTrabajo.Text = $"{_trabajo.Vehiculo?.Marca} {_trabajo.Vehiculo?.Modelo} - {_trabajo.Vehiculo?.Cliente?.Nombre} {_trabajo.Vehiculo?.Cliente?.Apellido}";

                // Cargar todos los servicios y repuestos disponibles
                _todosLosServicios = db.Servicios.OrderBy(s => s.Nombre).ToList();
                _todosLosRepuestos = db.Repuestos.Where(r => r.StockActual > 0).OrderBy(r => r.Nombre).ToList();

                dgServiciosDisponibles.ItemsSource = _todosLosServicios;
                dgRepuestosDisponibles.ItemsSource = _todosLosRepuestos;

                // Cargar servicios ya asignados al trabajo
                if (_trabajo.Servicios != null)
                {
                    foreach (var ts in _trabajo.Servicios)
                    {
                        _serviciosTrabajo.Add(new ServicioTrabajoItem
                        {
                            ServicioID = ts.ServicioID,
                            NombreServicio = ts.Servicio?.Nombre ?? "Servicio",
                            Cantidad = ts.Cantidad,
                            CostoUnitario = ts.Servicio?.CostoBase ?? 0,
                            Subtotal = ts.Subtotal
                        });
                    }
                }

                // Cargar repuestos ya asignados al trabajo
                if (_trabajo.Repuestos != null)
                {
                    foreach (var tr in _trabajo.Repuestos)
                    {
                        _repuestosTrabajo.Add(new RepuestoTrabajoItem
                        {
                            RepuestoID = tr.RepuestoID,
                            NombreRepuesto = tr.Repuesto?.Nombre ?? "Repuesto",
                            Cantidad = tr.Cantidad,
                            PrecioUnitario = tr.PrecioUnitario,
                            Subtotal = tr.Subtotal
                        });
                    }
                }

                ActualizarGrids();
                CalcularTotales();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar datos: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AgregarServicio_Click(object sender, RoutedEventArgs e)
        {
            var servicioSeleccionado = dgServiciosDisponibles.SelectedItem as Servicio;
            if (servicioSeleccionado == null)
            {
                MessageBox.Show("Seleccione un servicio de la lista.", "Advertencia", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(txtCantidadServicio.Text, out int cantidad) || cantidad <= 0)
            {
                MessageBox.Show("Ingrese una cantidad válida.", "Advertencia", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Verificar si el servicio ya está agregado
            var servicioExistente = _serviciosTrabajo.FirstOrDefault(s => s.ServicioID == servicioSeleccionado.ServicioID);
            if (servicioExistente != null)
            {
                // Si ya existe, sumar la cantidad
                servicioExistente.Cantidad += cantidad;
                servicioExistente.Subtotal = servicioExistente.Cantidad * servicioExistente.CostoUnitario;
            }
            else
            {
                // Si no existe, agregarlo
                _serviciosTrabajo.Add(new ServicioTrabajoItem
                {
                    ServicioID = servicioSeleccionado.ServicioID,
                    NombreServicio = servicioSeleccionado.Nombre,
                    Cantidad = cantidad,
                    CostoUnitario = servicioSeleccionado.CostoBase,
                    Subtotal = cantidad * servicioSeleccionado.CostoBase
                });
            }

            txtCantidadServicio.Text = "1";
            ActualizarGrids();
            CalcularTotales();
        }

        private void AgregarRepuesto_Click(object sender, RoutedEventArgs e)
        {
            var repuestoSeleccionado = dgRepuestosDisponibles.SelectedItem as Repuesto;
            if (repuestoSeleccionado == null)
            {
                MessageBox.Show("Seleccione un repuesto de la lista.", "Advertencia", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(txtCantidadRepuesto.Text, out int cantidad) || cantidad <= 0)
            {
                MessageBox.Show("Ingrese una cantidad válida.", "Advertencia", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Verificar stock disponible
            var cantidadYaAsignada = _repuestosTrabajo
                .Where(r => r.RepuestoID == repuestoSeleccionado.RepuestoID)
                .Sum(r => r.Cantidad);

            if (cantidadYaAsignada + cantidad > repuestoSeleccionado.StockActual)
            {
                MessageBox.Show(
                    $"Stock insuficiente.\n\nDisponible: {repuestoSeleccionado.StockActual}\nYa asignado: {cantidadYaAsignada}\nIntentando agregar: {cantidad}",
                    "Stock Insuficiente",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            // Verificar si el repuesto ya está agregado
            var repuestoExistente = _repuestosTrabajo.FirstOrDefault(r => r.RepuestoID == repuestoSeleccionado.RepuestoID);
            if (repuestoExistente != null)
            {
                // Si ya existe, sumar la cantidad
                repuestoExistente.Cantidad += cantidad;
                repuestoExistente.Subtotal = repuestoExistente.Cantidad * repuestoExistente.PrecioUnitario;
            }
            else
            {
                // Si no existe, agregarlo
                _repuestosTrabajo.Add(new RepuestoTrabajoItem
                {
                    RepuestoID = repuestoSeleccionado.RepuestoID,
                    NombreRepuesto = repuestoSeleccionado.Nombre,
                    Cantidad = cantidad,
                    PrecioUnitario = repuestoSeleccionado.PrecioUnitario,
                    Subtotal = cantidad * repuestoSeleccionado.PrecioUnitario
                });
            }

            txtCantidadRepuesto.Text = "1";
            ActualizarGrids();
            CalcularTotales();
        }

        private void EliminarServicio_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var servicio = button?.DataContext as ServicioTrabajoItem;
            if (servicio != null)
            {
                _serviciosTrabajo.Remove(servicio);
                ActualizarGrids();
                CalcularTotales();
            }
        }

        private void EliminarRepuesto_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var repuesto = button?.DataContext as RepuestoTrabajoItem;
            if (repuesto != null)
            {
                _repuestosTrabajo.Remove(repuesto);
                ActualizarGrids();
                CalcularTotales();
            }
        }

        private void ActualizarGrids()
        {
            dgServiciosTrabajo.ItemsSource = null;
            dgServiciosTrabajo.ItemsSource = _serviciosTrabajo;

            dgRepuestosTrabajo.ItemsSource = null;
            dgRepuestosTrabajo.ItemsSource = _repuestosTrabajo;
        }

        private void CalcularTotales()
        {
            decimal totalServicios = _serviciosTrabajo.Sum(s => s.Subtotal);
            decimal totalRepuestos = _repuestosTrabajo.Sum(r => r.Subtotal);
            decimal totalGeneral = totalServicios + totalRepuestos;

            txtTotalServicios.Text = $"Bs. {totalServicios:N2}";
            txtTotalRepuestos.Text = $"Bs. {totalRepuestos:N2}";
            txtTotalGeneral.Text = $"Bs. {totalGeneral:N2}";
        }

        private void GuardarCambios_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using var db = new TallerDbContext();

                // Cargar el trabajo con sus relaciones
                var trabajo = db.Trabajos
                    .Include(t => t.Servicios)
                    .Include(t => t.Repuestos)
                    .FirstOrDefault(t => t.TrabajoID == _trabajoId);

                if (trabajo == null)
                {
                    MessageBox.Show("No se encontró el trabajo.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Eliminar servicios y repuestos existentes
                db.Trabajos_Servicios.RemoveRange(trabajo.Servicios);
                db.Trabajos_Repuestos.RemoveRange(trabajo.Repuestos);
                db.SaveChanges();

                // Agregar nuevos servicios
                foreach (var servicio in _serviciosTrabajo)
                {
                    db.Trabajos_Servicios.Add(new Trabajos_Servicios
                    {
                        TrabajoID = _trabajoId,
                        ServicioID = servicio.ServicioID,
                        Cantidad = servicio.Cantidad,
                        Subtotal = servicio.Subtotal
                    });
                }

                // Agregar nuevos repuestos
                foreach (var repuesto in _repuestosTrabajo)
                {
                    db.Trabajos_Repuestos.Add(new Trabajos_Repuestos
                    {
                        TrabajoID = _trabajoId,
                        RepuestoID = repuesto.RepuestoID,
                        Cantidad = repuesto.Cantidad,
                        PrecioUnitario = repuesto.PrecioUnitario,
                        Subtotal = repuesto.Subtotal
                    });

                    // Actualizar stock del repuesto
                    var repuestoDb = db.Repuestos.Find(repuesto.RepuestoID);
                    if (repuestoDb != null)
                    {
                        repuestoDb.StockActual -= repuesto.Cantidad;
                    }
                }

                // Actualizar precio final del trabajo
                decimal total = _serviciosTrabajo.Sum(s => s.Subtotal) + _repuestosTrabajo.Sum(r => r.Subtotal);
                trabajo.PrecioFinal = total;

                db.SaveChanges();

                MessageBox.Show(
                    $"✅ CAMBIOS GUARDADOS EXITOSAMENTE\n\n" +
                    $"Servicios: {_serviciosTrabajo.Count}\n" +
                    $"Repuestos: {_repuestosTrabajo.Count}\n\n" +
                    $"Total del Trabajo: Bs. {total:N2}",
                    "Éxito",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al guardar cambios: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TxtBuscarServicio_TextChanged(object sender, TextChangedEventArgs e)
        {
            var filtro = txtBuscarServicio.Text.ToLower();
            if (string.IsNullOrWhiteSpace(filtro))
            {
                dgServiciosDisponibles.ItemsSource = _todosLosServicios;
            }
            else
            {
                dgServiciosDisponibles.ItemsSource = _todosLosServicios
                    .Where(s => s.Nombre.ToLower().Contains(filtro) ||
                               (s.Descripcion != null && s.Descripcion.ToLower().Contains(filtro)))
                    .ToList();
            }
        }

        private void TxtBuscarRepuesto_TextChanged(object sender, TextChangedEventArgs e)
        {
            var filtro = txtBuscarRepuesto.Text.ToLower();
            if (string.IsNullOrWhiteSpace(filtro))
            {
                dgRepuestosDisponibles.ItemsSource = _todosLosRepuestos;
            }
            else
            {
                dgRepuestosDisponibles.ItemsSource = _todosLosRepuestos
                    .Where(r => r.Nombre.ToLower().Contains(filtro) ||
                               (r.Descripcion != null && r.Descripcion.ToLower().Contains(filtro)))
                    .ToList();
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            var resultado = MessageBox.Show(
                "¿Está seguro de cerrar sin guardar?\nSe perderán los cambios no guardados.",
                "Confirmar",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (resultado == MessageBoxResult.Yes)
            {
                DialogResult = false;
                Close();
            }
        }
    }

    // Clases auxiliares para el binding
    public class ServicioTrabajoItem
    {
        public int ServicioID { get; set; }
        public string NombreServicio { get; set; }
        public int Cantidad { get; set; }
        public decimal CostoUnitario { get; set; }
        public decimal Subtotal { get; set; }
    }

    public class RepuestoTrabajoItem
    {
        public int RepuestoID { get; set; }
        public string NombreRepuesto { get; set; }
        public int Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal Subtotal { get; set; }
    }
}