using Proyecto_taller.Data;
using Proyecto_taller.Models;
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
    public partial class NuevoClienteWindow : Window
    {
        // Expone el cliente creado para que quien abre la ventana pueda usarlo
        public Cliente ClienteCreado { get; private set; }

        public NuevoClienteWindow()
        {
            InitializeComponent();
            txtNombre.Focus();
        }

        private void Guardar_Click(object sender, RoutedEventArgs e)
        {
            // Validaciones
            if (string.IsNullOrWhiteSpace(txtNombre.Text))
            {
                Msg("El nombre es obligatorio.");
                txtNombre.Focus();
                return;
            }
            if (string.IsNullOrWhiteSpace(txtApellido.Text))
            {
                Msg("El apellido es obligatorio.");
                txtApellido.Focus();
                return;
            }
            if (string.IsNullOrWhiteSpace(txtTelefono.Text))
            {
                Msg("El teléfono es obligatorio.");
                txtTelefono.Focus();
                return;
            }

            try
            {
                using var db = new TallerDbContext();

                // Verificar teléfono duplicado
                if (db.Clientes.Any(c => c.Telefono == txtTelefono.Text.Trim()))
                {
                    Msg($"Ya existe un cliente con el teléfono '{txtTelefono.Text.Trim()}'.\nVerifique los datos e intente con otro número.");
                    txtTelefono.Focus();
                    return;
                }

                var nuevo = new Cliente
                {
                    Nombre = txtNombre.Text.Trim(),
                    Apellido = txtApellido.Text.Trim(),
                    Telefono = txtTelefono.Text.Trim(),
                    Correo = txtCorreo.Text.Trim(),
                    Direccion = string.IsNullOrWhiteSpace(txtDireccion.Text)
                                        ? "Sin dirección"
                                        : txtDireccion.Text.Trim(),
                    FechaRegistro = DateTime.Now
                };

                db.Clientes.Add(nuevo);
                db.SaveChanges();

                ClienteCreado = nuevo;

                MessageBox.Show(
                    $"✅  Cliente registrado correctamente.\n\n" +
                    $"Nombre:   {nuevo.Nombre} {nuevo.Apellido}\n" +
                    $"Teléfono: {nuevo.Telefono}",
                    "Cliente Creado",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al guardar:\n{ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancelar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }

        private void Msg(string m)
            => MessageBox.Show(m, "Validación",
                MessageBoxButton.OK, MessageBoxImage.Warning);
    }
}
