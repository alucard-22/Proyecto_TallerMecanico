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
    public partial class EditarClienteWindow : Window
    {
        private readonly int _clienteId;

        public EditarClienteWindow(Cliente cliente)
        {
            InitializeComponent();
            _clienteId = cliente.ClienteID;

            txtTitulo.Text = $"✏️  Editar — {cliente.Nombre} {cliente.Apellido}";
            txtClienteId.Text = cliente.ClienteID.ToString();
            txtNombre.Text = cliente.Nombre;
            txtApellido.Text = cliente.Apellido;
            txtTelefono.Text = cliente.Telefono;
            txtCorreo.Text = cliente.Correo ?? string.Empty;
            txtDireccion.Text = cliente.Direccion ?? string.Empty;
            txtFechaRegistro.Text = cliente.FechaRegistro.ToString("dd/MM/yyyy HH:mm");

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

                // Verificar teléfono duplicado excluyendo el cliente actual
                bool telefonoDuplicado = db.Clientes.Any(c =>
                    c.Telefono == txtTelefono.Text.Trim() &&
                    c.ClienteID != _clienteId);

                if (telefonoDuplicado)
                {
                    Msg($"Ya existe otro cliente con el teléfono '{txtTelefono.Text.Trim()}'.");
                    txtTelefono.Focus();
                    return;
                }

                var cliente = db.Clientes.Find(_clienteId);

                if (cliente == null)
                {
                    MessageBox.Show("El cliente ya no existe en la base de datos.",
                        "No encontrado", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                cliente.Nombre = txtNombre.Text.Trim();
                cliente.Apellido = txtApellido.Text.Trim();
                cliente.Telefono = txtTelefono.Text.Trim();
                cliente.Correo = txtCorreo.Text.Trim();
                cliente.Direccion = string.IsNullOrWhiteSpace(txtDireccion.Text)
                                        ? "Sin dirección"
                                        : txtDireccion.Text.Trim();

                db.SaveChanges();

                MessageBox.Show(
                    $"✅  Cliente actualizado correctamente.\n\n" +
                    $"Nombre:   {cliente.Nombre} {cliente.Apellido}\n" +
                    $"Teléfono: {cliente.Telefono}",
                    "Cliente Actualizado",
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