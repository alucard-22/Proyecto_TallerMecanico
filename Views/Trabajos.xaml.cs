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

        // Doble clic en una fila → abrir detalles directamente
        private void dgTrabajos_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var vm = DataContext as TrabajosViewModel;
            if (vm?.TrabajoSeleccionado == null) return;

            var win = new DetallesTrabajoWindow(vm.TrabajoSeleccionado.TrabajoID);
            win.ShowDialog();
            vm.CargarTrabajos();
        }
    }
}

