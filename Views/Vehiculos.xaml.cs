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
using Proyecto_taller.ViewModels;

namespace Proyecto_taller.Views
{
    public partial class Vehiculos : Page
    {
        public Vehiculos()
        {
            InitializeComponent();
            DataContext = new VehiculosViewModel();
        }

        private void TxtBuscar_TextChanged(object sender, TextChangedEventArgs e)
        {
            var viewModel = DataContext as VehiculosViewModel;
            if (viewModel == null) return;

            string filtro = txtBuscar.Text.ToLower();

            if (string.IsNullOrWhiteSpace(filtro))
            {
                dgVehiculos.ItemsSource = viewModel.Vehiculos;
            }
            else
            {
                var vehiculosFiltrados = viewModel.Vehiculos.Where(v =>
                    v.Placa.ToLower().Contains(filtro) ||
                    v.Marca.ToLower().Contains(filtro) ||
                    v.Modelo.ToLower().Contains(filtro) ||
                    (v.Anio.HasValue && v.Anio.ToString().Contains(filtro)) ||
                    (v.Cliente?.Nombre != null && v.Cliente.Nombre.ToLower().Contains(filtro)) ||
                    (v.Cliente?.Apellido != null && v.Cliente.Apellido.ToLower().Contains(filtro))
                ).ToList();

                dgVehiculos.ItemsSource = vehiculosFiltrados;
            }
        }
    }
}