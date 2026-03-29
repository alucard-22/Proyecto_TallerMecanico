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
    public partial class Clientes : Page
    {
        public Clientes()
        {
            InitializeComponent();
            DataContext = new ClientesViewModel();
        }

        // ── Búsqueda en tiempo real ───────────────────────────────
        private void TxtBuscar_TextChanged(object sender, TextChangedEventArgs e)
        {
            var viewModel = DataContext as ClientesViewModel;
            if (viewModel == null) return;

            string filtro = txtBuscar.Text.ToLower();

            if (string.IsNullOrWhiteSpace(filtro))
            {
                dgClientes.ItemsSource = viewModel.Clientes;
            }
            else
            {
                var clientesFiltrados = viewModel.Clientes.Where(c =>
                    c.Nombre.ToLower().Contains(filtro) ||
                    c.Apellido.ToLower().Contains(filtro) ||
                    c.Telefono.Contains(filtro) ||
                    (c.Correo != null && c.Correo.ToLower().Contains(filtro)) ||
                    (c.Direccion != null && c.Direccion.ToLower().Contains(filtro))
                ).ToList();

                dgClientes.ItemsSource = clientesFiltrados;
            }
        }

        // ── Limpiar búsqueda ──────────────────────────────────────
        private void LimpiarBusqueda_Click(object sender, RoutedEventArgs e)
        {
            txtBuscar.Clear();
            txtBuscar.Focus();
        }

        // ── Doble clic → abrir historial del cliente ──────────────
        private void DgClientes_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var vm = DataContext as ClientesViewModel;
            if (vm?.ClienteSeleccionado == null) return;

            var win = new HistorialClienteWindow(vm.ClienteSeleccionado.ClienteID);
            win.ShowDialog();
        }
    }
}
