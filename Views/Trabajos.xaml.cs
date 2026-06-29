using Proyecto_taller.ViewModels;
using System.Windows.Controls;
using System.Windows.Input;

namespace Proyecto_taller.Views
{
    public partial class Trabajos : Page
    {
        public Trabajos()
        {
            InitializeComponent();
            DataContext = new TrabajosViewModel();
        }

        // Doble clic en una fila → abrir detalles del trabajo seleccionado
        // (misma acción que el botón "Ver Detalles").
        private void dgTrabajos_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is TrabajosViewModel vm && vm.TrabajoSeleccionado != null)
            {
                var win = new DetallesTrabajoWindow(vm.TrabajoSeleccionado.TrabajoID);
                win.ShowDialog();
                vm.CargarTrabajos();
            }
        }
    }
}