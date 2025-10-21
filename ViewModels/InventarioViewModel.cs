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
using System.Windows.Input;

namespace Proyecto_taller.ViewModels
{
    public class InventarioViewModel : INotifyPropertyChanged
    {
        private Repuesto _repuestoSeleccionado;
        private bool _filtroTodos = true;
        private bool _filtroStockBajo;
        private bool _filtroSinStock;
        private int _repuestosStockBajo;
        private int _totalRepuestos;

        public ObservableCollection<Repuesto> Repuestos { get; set; }

        public Repuesto RepuestoSeleccionado
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
            set
            {
                _repuestosStockBajo = value;
                OnPropertyChanged();
            }
        }

        public int TotalRepuestos
        {
            get => _totalRepuestos;
            set
            {
                _totalRepuestos = value;
                OnPropertyChanged();
            }
        }

        // Propiedades para los filtros
        public bool FiltroTodos
        {
            get => _filtroTodos;
            set
            {
                _filtroTodos = value;
                OnPropertyChanged();
                if (value) AplicarFiltro("Todos");
            }
        }

        public bool FiltroStockBajo
        {
            get => _filtroStockBajo;
            set
            {
                _filtroStockBajo = value;
                OnPropertyChanged();
                if (value) AplicarFiltro("StockBajo");
            }
        }

        public bool FiltroSinStock
        {
            get => _filtroSinStock;
            set
            {
                _filtroSinStock = value;
                OnPropertyChanged();
                if (value) AplicarFiltro("SinStock");
            }
        }

        public ICommand CargarRepuestosCommand { get; }
        public ICommand AgregarRepuestoCommand { get; }
        public ICommand EditarRepuestoCommand { get; }
        public ICommand EntradaStockCommand { get; }
        public ICommand SalidaStockCommand { get; }
        public ICommand EliminarRepuestoCommand { get; }

        public InventarioViewModel()
        {
            Repuestos = new ObservableCollection<Repuesto>();

            CargarRepuestosCommand = new RelayCommand(CargarRepuestos);
            AgregarRepuestoCommand = new RelayCommand(AgregarRepuesto);
            EditarRepuestoCommand = new RelayCommand(EditarRepuesto, () => RepuestoSeleccionado != null);
            EntradaStockCommand = new RelayCommand(EntradaStock, () => RepuestoSeleccionado != null);
            SalidaStockCommand = new RelayCommand(SalidaStock, () => RepuestoSeleccionado != null);
            EliminarRepuestoCommand = new RelayCommand(EliminarRepuesto, () => RepuestoSeleccionado != null);

            CargarRepuestos();
        }

        private void CargarRepuestos()
        {
            Repuestos.Clear();
            using var db = new TallerDbContext();

            var repuestos = db.Repuestos.OrderBy(r => r.Nombre).ToList();

            foreach (var repuesto in repuestos)
            {
                Repuestos.Add(repuesto);
            }

            ActualizarEstadisticas();
        }

        private void AplicarFiltro(string filtro)
        {
            Repuestos.Clear();
            using var db = new TallerDbContext();

            var query = db.Repuestos.AsQueryable();

            if (filtro == "StockBajo")
            {
                query = query.Where(r => r.StockActual <= r.StockMinimo && r.StockActual > 0);
            }
            else if (filtro == "SinStock")
            {
                query = query.Where(r => r.StockActual == 0);
            }

            var repuestos = query.OrderBy(r => r.Nombre).ToList();

            foreach (var repuesto in repuestos)
            {
                Repuestos.Add(repuesto);
            }

            ActualizarEstadisticas();
        }

        private void ActualizarEstadisticas()
        {
            using var db = new TallerDbContext();
            TotalRepuestos = db.Repuestos.Count();
            RepuestosStockBajo = db.Repuestos.Count(r => r.StockActual <= r.StockMinimo);
        }

        private void AgregarRepuesto()
        {
            using var db = new TallerDbContext();

            var nuevo = new Repuesto
            {
                Nombre = "Nuevo Repuesto",
                Descripcion = "Descripción del repuesto",
                PrecioUnitario = 50.00m,
                StockActual = 0,
                StockMinimo = 5
            };

            db.Repuestos.Add(nuevo);
            db.SaveChanges();
            Repuestos.Add(nuevo);

            ActualizarEstadisticas();

            System.Windows.MessageBox.Show(
                "Repuesto agregado exitosamente. Recuerde editarlo con los datos correctos.",
                "Éxito",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);
        }

        private void EditarRepuesto()
        {
            if (RepuestoSeleccionado == null) return;

            System.Windows.MessageBox.Show(
                $"Editando repuesto: {RepuestoSeleccionado.Nombre}\n" +
                $"Stock actual: {RepuestoSeleccionado.StockActual}\n" +
                $"Precio: Bs. {RepuestoSeleccionado.PrecioUnitario:N2}",
                "Editar Repuesto");
        }

        private void EntradaStock()
        {
            if (RepuestoSeleccionado == null) return;

            // Aquí podrías abrir un diálogo para ingresar la cantidad
            var inputDialog = new InputDialog("Entrada de Stock", "Ingrese la cantidad a agregar:");

            if (inputDialog.ShowDialog() == true && int.TryParse(inputDialog.ResponseText, out int cantidad) && cantidad > 0)
            {
                using var db = new TallerDbContext();
                var repuesto = db.Repuestos.Find(RepuestoSeleccionado.RepuestoID);

                if (repuesto != null)
                {
                    repuesto.StockActual += cantidad;
                    db.SaveChanges();

                    RepuestoSeleccionado.StockActual = repuesto.StockActual;
                    OnPropertyChanged(nameof(Repuestos));
                    ActualizarEstadisticas();

                    System.Windows.MessageBox.Show(
                        $"Se agregaron {cantidad} unidades al stock.\nNuevo stock: {repuesto.StockActual}",
                        "Entrada de Stock",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Information);
                }
            }
        }

        private void SalidaStock()
        {
            if (RepuestoSeleccionado == null) return;

            var inputDialog = new InputDialog("Salida de Stock", "Ingrese la cantidad a retirar:");

            if (inputDialog.ShowDialog() == true && int.TryParse(inputDialog.ResponseText, out int cantidad) && cantidad > 0)
            {
                if (cantidad > RepuestoSeleccionado.StockActual)
                {
                    System.Windows.MessageBox.Show(
                        $"No hay suficiente stock. Stock disponible: {RepuestoSeleccionado.StockActual}",
                        "Error",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Error);
                    return;
                }

                using var db = new TallerDbContext();
                var repuesto = db.Repuestos.Find(RepuestoSeleccionado.RepuestoID);

                if (repuesto != null)
                {
                    repuesto.StockActual -= cantidad;
                    db.SaveChanges();

                    RepuestoSeleccionado.StockActual = repuesto.StockActual;
                    OnPropertyChanged(nameof(Repuestos));
                    ActualizarEstadisticas();

                    System.Windows.MessageBox.Show(
                        $"Se retiraron {cantidad} unidades del stock.\nStock restante: {repuesto.StockActual}",
                        "Salida de Stock",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Information);
                }
            }
        }

        private void EliminarRepuesto()
        {
            if (RepuestoSeleccionado == null) return;

            var resultado = System.Windows.MessageBox.Show(
                $"¿Está seguro de eliminar el repuesto '{RepuestoSeleccionado.Nombre}'?",
                "Confirmar Eliminación",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning);

            if (resultado == System.Windows.MessageBoxResult.Yes)
            {
                using var db = new TallerDbContext();
                var repuesto = db.Repuestos.Find(RepuestoSeleccionado.RepuestoID);

                if (repuesto != null)
                {
                    db.Repuestos.Remove(repuesto);
                    db.SaveChanges();
                    Repuestos.Remove(RepuestoSeleccionado);
                    ActualizarEstadisticas();
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    // Clase auxiliar para entrada de datos
    public class InputDialog : System.Windows.Window
    {
        public string ResponseText { get; private set; }
        private System.Windows.Controls.TextBox txtInput;

        public InputDialog(string title, string question)
        {
            Title = title;
            Width = 400;
            Height = 180;
            WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;
            ResizeMode = System.Windows.ResizeMode.NoResize;
            Background = System.Windows.Media.Brushes.White;

            var grid = new System.Windows.Controls.Grid();
            grid.Margin = new System.Windows.Thickness(20);

            var stackPanel = new System.Windows.Controls.StackPanel();

            var lblQuestion = new System.Windows.Controls.TextBlock
            {
                Text = question,
                FontSize = 14,
                Margin = new System.Windows.Thickness(0, 0, 0, 15)
            };

            txtInput = new System.Windows.Controls.TextBox
            {
                FontSize = 14,
                Padding = new System.Windows.Thickness(8),
                Margin = new System.Windows.Thickness(0, 0, 0, 20)
            };

            var buttonPanel = new System.Windows.Controls.StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Right
            };

            var btnOk = new System.Windows.Controls.Button
            {
                Content = "Aceptar",
                Width = 100,
                Height = 35,
                Margin = new System.Windows.Thickness(0, 0, 10, 0),
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(74, 144, 226)),
                Foreground = System.Windows.Media.Brushes.White,
                BorderThickness = new System.Windows.Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand
            };

            btnOk.Click += (s, e) => { ResponseText = txtInput.Text; DialogResult = true; };

            var btnCancel = new System.Windows.Controls.Button
            {
                Content = "Cancelar",
                Width = 100,
                Height = 35,
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(231, 76, 60)),
                Foreground = System.Windows.Media.Brushes.White,
                BorderThickness = new System.Windows.Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand
            };

            btnCancel.Click += (s, e) => { DialogResult = false; };

            buttonPanel.Children.Add(btnOk);
            buttonPanel.Children.Add(btnCancel);

            stackPanel.Children.Add(lblQuestion);
            stackPanel.Children.Add(txtInput);
            stackPanel.Children.Add(buttonPanel);

            grid.Children.Add(stackPanel);
            Content = grid;

            txtInput.Focus();
        }
    }
}
