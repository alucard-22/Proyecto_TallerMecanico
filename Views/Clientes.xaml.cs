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
    /// <summary>
    /// Lógica de interacción para Clientes.xaml
    /// </summary>
    public partial class Clientes : Page
    {
        public Clientes()
        {
            InitializeComponent();
            DataContext = new ClientesViewModel();
        }
        // Agregar este método para la funcionalidad de búsqueda
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
    }

}
