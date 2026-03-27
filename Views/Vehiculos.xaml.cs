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
using Proyecto_taller.Models;
using Proyecto_taller.Data;
using Proyecto_taller.Views;
using Microsoft.EntityFrameworkCore;

namespace Proyecto_taller.Views
{
    public partial class Vehiculos : Page
    {
        public Vehiculos()
        {
            InitializeComponent();
            DataContext = new VehiculosViewModel();
        }

        // Doble clic en una fila → abrir historial de trabajos del vehículo
        private void DgVehiculos_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            var vm = DataContext as VehiculosViewModel;
            if (vm?.VehiculoSeleccionado == null) return;

            var win = new HistorialVehiculoWindow(vm.VehiculoSeleccionado.VehiculoID);
            win.ShowDialog();
        }
    }
}