using Proyecto_taller.Data;
using Proyecto_taller.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Proyecto_taller.Views;

namespace Proyecto_taller.ViewModels
{
    public class InventarioViewModel : INotifyPropertyChanged
    {
        // ─── Estado ───────────────────────────────────────────────────────────

        private Repuesto? _repuestoSeleccionado;
        private bool _filtroTodos = true;
        private bool _filtroStockBajo;
        private bool _filtroSinStock;
        private int _repuestosStockBajo;
        private int _totalRepuestos;
        private decimal _valorTotalInventario;
        private string _textoBusqueda = string.Empty;

        // Colección maestra (sin filtrar) para búsqueda local
        private ObservableCollection<Repuesto> _todosMaestro = new();

        public ObservableCollection<Repuesto> Repuestos { get; set; } = new();

        // ─── Propiedades ──────────────────────────────────────────────────────

        public Repuesto? RepuestoSeleccionado
        {
            get => _repuestoSeleccionado;
            set
            {
                _repuestoSeleccionado = value;
                OnPropertyChanged();
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public int RepuestosStockBajo
        {
            get => _repuestosStockBajo;
            set { _repuestosStockBajo = value; OnPropertyChanged(); }
        }

        public int TotalRepuestos
        {
            get => _totalRepuestos;
            set { _totalRepuestos = value; OnPropertyChanged(); }
        }

        public decimal ValorTotalInventario
        {
            get => _valorTotalInventario;
            set { _valorTotalInventario = value; OnPropertyChanged(); }
        }

        public string TextoBusqueda
        {
            get => _textoBusqueda;
            set
            {
                _textoBusqueda = value;
                OnPropertyChanged();
                AplicarFiltroYBusqueda();
            }
        }

        // Filtros de radio (excluyentes)
        public bool FiltroTodos
        {
            get => _filtroTodos;
            set { _filtroTodos = value; OnPropertyChanged(); if (value) AplicarFiltroYBusqueda(); }
        }
        public bool FiltroStockBajo
        {
            get => _filtroStockBajo;
            set { _filtroStockBajo = value; OnPropertyChanged(); if (value) AplicarFiltroYBusqueda(); }
        }
        public bool FiltroSinStock
        {
            get => _filtroSinStock;
            set { _filtroSinStock = value; OnPropertyChanged(); if (value) AplicarFiltroYBusqueda(); }
        }

        // ─── Comandos ─────────────────────────────────────────────────────────

        public ICommand CargarRepuestosCommand { get; }
        public ICommand AgregarRepuestoCommand { get; }
        public ICommand EditarRepuestoCommand { get; }
        public ICommand EntradaStockCommand { get; }
        public ICommand SalidaStockCommand { get; }
        public ICommand EliminarRepuestoCommand { get; }
        public ICommand LimpiarBusquedaCommand { get; }

        // ─── Constructor ──────────────────────────────────────────────────────

        public InventarioViewModel()
        {
            CargarRepuestosCommand = new RelayCommand(CargarRepuestos);
            AgregarRepuestoCommand = new RelayCommand(AgregarRepuesto);
            EditarRepuestoCommand = new RelayCommand(EditarRepuesto, () => RepuestoSeleccionado != null);
            EntradaStockCommand = new RelayCommand(EntradaStock, () => RepuestoSeleccionado != null);
            SalidaStockCommand = new RelayCommand(SalidaStock, () => RepuestoSeleccionado != null && RepuestoSeleccionado.StockActual > 0);
            EliminarRepuestoCommand = new RelayCommand(EliminarRepuesto, () => RepuestoSeleccionado != null);
            LimpiarBusquedaCommand = new RelayCommand(() => TextoBusqueda = string.Empty);

            CargarRepuestos();
        }

        // ─── Carga y filtro ───────────────────────────────────────────────────

        public void CargarRepuestos()
        {
            var idAnterior = RepuestoSeleccionado?.RepuestoID;

            using var db = new TallerDbContext();
            var lista = db.Repuestos.OrderBy(r => r.Nombre).ToList();

            _todosMaestro.Clear();
            foreach (var r in lista)
                _todosMaestro.Add(r);

            AplicarFiltroYBusqueda();
            ActualizarEstadisticas();

            // Restaurar selección si sigue existiendo
            if (idAnterior.HasValue)
                RepuestoSeleccionado = Repuestos.FirstOrDefault(r => r.RepuestoID == idAnterior.Value);
        }

        private void AplicarFiltroYBusqueda()
        {
            var query = _todosMaestro.AsEnumerable();

            // Filtro de radio
            if (FiltroStockBajo)
                query = query.Where(r => r.StockActual > 0 && r.StockActual <= r.StockMinimo);
            else if (FiltroSinStock)
                query = query.Where(r => r.StockActual == 0);

            // Búsqueda por texto
            var texto = TextoBusqueda.Trim().ToLower();
            if (!string.IsNullOrEmpty(texto))
                query = query.Where(r =>
                    r.Nombre.ToLower().Contains(texto) ||
                    (r.Descripcion?.ToLower().Contains(texto) ?? false));

            Repuestos.Clear();
            foreach (var r in query)
                Repuestos.Add(r);
        }

        private void ActualizarEstadisticas()
        {
            TotalRepuestos = _todosMaestro.Count;
            RepuestosStockBajo = _todosMaestro.Count(r => r.StockActual <= r.StockMinimo);
            ValorTotalInventario = _todosMaestro.Sum(r => r.ValorTotal);
        }

        // ─── Agregar repuesto ─────────────────────────────────────────────────

        private void AgregarRepuesto()
        {
            var win = new EditarRepuestoWindow();   // modo Nuevo
            if (win.ShowDialog() == true)
                CargarRepuestos();
        }

        // ─── Editar repuesto ──────────────────────────────────────────────────

        private void EditarRepuesto()
        {
            if (RepuestoSeleccionado == null) return;

            // Recargar desde BD para obtener datos frescos
            using var db = new TallerDbContext();
            var repFresco = db.Repuestos.Find(RepuestoSeleccionado.RepuestoID);
            if (repFresco == null)
            {
                MessageBox.Show("El repuesto ya no existe en la base de datos.",
                    "No encontrado", MessageBoxButton.OK, MessageBoxImage.Warning);
                CargarRepuestos();
                return;
            }

            var win = new EditarRepuestoWindow(repFresco);   // modo Editar
            if (win.ShowDialog() == true)
                CargarRepuestos();
        }

        // ─── Entrada de stock ─────────────────────────────────────────────────

        private void EntradaStock()
        {
            if (RepuestoSeleccionado == null) return;

            var win = new EntradaSalidaStockWindow(RepuestoSeleccionado, TipoMovimiento.Entrada);

            if (win.ShowDialog() != true) return;

            try
            {
                using var db = new TallerDbContext();
                var rep = db.Repuestos.Find(RepuestoSeleccionado.RepuestoID);
                if (rep == null) return;

                int stockAnterior = rep.StockActual;
                rep.StockActual += win.CantidadMovida;
                db.SaveChanges();

                // Actualizar en memoria inmediatamente (sin recargar todo)
                RepuestoSeleccionado.StockActual = rep.StockActual;
                ActualizarEstadisticas();
                CommandManager.InvalidateRequerySuggested();

                MessageBox.Show(
                    $"✅  Entrada registrada correctamente.\n\n" +
                    $"Repuesto:         {rep.Nombre}\n" +
                    $"Cantidad entrada: +{win.CantidadMovida}\n" +
                    $"Stock anterior:   {stockAnterior}\n" +
                    $"Stock actual:     {rep.StockActual}" +
                    (string.IsNullOrEmpty(win.Motivo) ? "" : $"\nMotivo:           {win.Motivo}"),
                    "Entrada de Stock",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al registrar la entrada:\n{ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ─── Salida de stock ──────────────────────────────────────────────────

        private void SalidaStock()
        {
            if (RepuestoSeleccionado == null) return;

            if (RepuestoSeleccionado.StockActual <= 0)
            {
                MessageBox.Show(
                    $"'{RepuestoSeleccionado.Nombre}' no tiene stock disponible para retirar.",
                    "Sin stock", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var win = new EntradaSalidaStockWindow(RepuestoSeleccionado, TipoMovimiento.Salida);

            if (win.ShowDialog() != true) return;

            try
            {
                using var db = new TallerDbContext();

                // ⭐ Verificación optimista: recargar stock real desde BD
                //    por si otro usuario descontó stock mientras el diálogo estaba abierto
                var rep = db.Repuestos.Find(RepuestoSeleccionado.RepuestoID);
                if (rep == null) return;

                if (win.CantidadMovida > rep.StockActual)
                {
                    MessageBox.Show(
                        $"El stock del repuesto cambió mientras tenías el diálogo abierto.\n\n" +
                        $"Stock disponible actual: {rep.StockActual}\n" +
                        $"Cantidad solicitada:     {win.CantidadMovida}\n\n" +
                        $"Por favor, intenta de nuevo.",
                        "Stock insuficiente",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    CargarRepuestos();
                    return;
                }

                int stockAnterior = rep.StockActual;
                rep.StockActual -= win.CantidadMovida;
                db.SaveChanges();

                // Actualizar en memoria
                RepuestoSeleccionado.StockActual = rep.StockActual;
                ActualizarEstadisticas();
                CommandManager.InvalidateRequerySuggested();

                // Avisar si quedó en stock bajo o sin stock
                string aviso = string.Empty;
                if (rep.StockActual == 0)
                    aviso = "\n\n⚠️  El repuesto quedó SIN STOCK.";
                else if (rep.StockActual <= rep.StockMinimo)
                    aviso = $"\n\n⚠️  El stock quedó por debajo del mínimo ({rep.StockMinimo}).";

                MessageBox.Show(
                    $"✅  Salida registrada correctamente.\n\n" +
                    $"Repuesto:        {rep.Nombre}\n" +
                    $"Cantidad salida: -{win.CantidadMovida}\n" +
                    $"Stock anterior:  {stockAnterior}\n" +
                    $"Stock actual:    {rep.StockActual}" +
                    (string.IsNullOrEmpty(win.Motivo) ? "" : $"\nMotivo:          {win.Motivo}") +
                    aviso,
                    "Salida de Stock",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al registrar la salida:\n{ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ─── Eliminar repuesto ────────────────────────────────────────────────

        private void EliminarRepuesto()
        {
            if (RepuestoSeleccionado == null) return;

            var resultado = MessageBox.Show(
                $"¿Eliminar el repuesto '{RepuestoSeleccionado.Nombre}'?\n\n" +
                $"⚠️  Si este repuesto está asociado a trabajos existentes,\n" +
                $"la eliminación fallará por integridad referencial.",
                "Confirmar Eliminación",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (resultado != MessageBoxResult.Yes) return;

            try
            {
                using var db = new TallerDbContext();

                // Verificar si está asociado a trabajos
                bool tieneUsos = db.Trabajos_Repuestos
                    .Any(tr => tr.RepuestoID == RepuestoSeleccionado.RepuestoID);

                if (tieneUsos)
                {
                    MessageBox.Show(
                        $"No se puede eliminar '{RepuestoSeleccionado.Nombre}' porque\n" +
                        $"está asociado a uno o más trabajos.\n\n" +
                        $"Puedes editar su nombre o precio en su lugar.",
                        "No se puede eliminar",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var rep = db.Repuestos.Find(RepuestoSeleccionado.RepuestoID);
                if (rep != null)
                {
                    db.Repuestos.Remove(rep);
                    db.SaveChanges();
                }

                CargarRepuestos();

                MessageBox.Show("Repuesto eliminado correctamente.",
                    "Eliminado", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al eliminar:\n{ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ─── INotifyPropertyChanged ───────────────────────────────────────────

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
