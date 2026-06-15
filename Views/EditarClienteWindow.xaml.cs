using Proyecto_taller.Data;
using Proyecto_taller.Helpers;
using Proyecto_taller.Models;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

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

            // Capitalización automática al perder el foco
            txtNombre.LostFocus += (s, e) => AplicarTitleCase(txtNombre);
            txtApellido.LostFocus += (s, e) => AplicarTitleCase(txtApellido);
            txtDireccion.LostFocus += (s, e) => AplicarPrimeraLetra(txtDireccion);

            txtNombre.Focus();
        }

        private static void AplicarTitleCase(TextBox tb)
        {
            if (!string.IsNullOrWhiteSpace(tb.Text))
                tb.Text = ValidationHelper.AplicarTitleCase(tb.Text);
        }

        private static void AplicarPrimeraLetra(TextBox tb)
        {
            if (!string.IsNullOrWhiteSpace(tb.Text))
                tb.Text = ValidationHelper.AplicarPrimeraLetraMayuscula(tb.Text);
        }

        private void Guardar_Click(object sender, RoutedEventArgs e)
        {
            // Aplicar capitalización
            txtNombre.Text = ValidationHelper.AplicarTitleCase(txtNombre.Text);
            txtApellido.Text = ValidationHelper.AplicarTitleCase(txtApellido.Text);
            txtDireccion.Text = string.IsNullOrWhiteSpace(txtDireccion.Text)
                ? "Sin dirección"
                : ValidationHelper.AplicarPrimeraLetraMayuscula(txtDireccion.Text);

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
            if (!ValidationHelper.EsTelefonoValido(txtTelefono.Text))
            {
                Msg(ValidationHelper.MsgTelefonoInvalido);
                txtTelefono.Focus();
                return;
            }
            if (!string.IsNullOrWhiteSpace(txtCorreo.Text) &&
                !ValidationHelper.EsCorreoValido(txtCorreo.Text))
            {
                Msg(ValidationHelper.MsgCorreoInvalido);
                txtCorreo.Focus();
                return;
            }

            try
            {
                using var db = new TallerDbContext();

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
                cliente.Direccion = txtDireccion.Text.Trim();

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
