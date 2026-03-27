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
using System.Windows.Shapes;

namespace Proyecto_taller.Views
{
    public partial class Usuarios : Page
    {
        public Usuarios()
        {
            InitializeComponent();
            DataContext = new UsuariosViewModel();
        }

        // Doble clic en fila → abrir historial del usuario
        private void DgUsuarios_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            var vm = DataContext as UsuariosViewModel;
            if (vm?.UsuarioSeleccionado == null) return;

            var win = new HistorialUsuarioWindow(vm.UsuarioSeleccionado.UsuarioID);
            win.ShowDialog();
        }
    }
}
