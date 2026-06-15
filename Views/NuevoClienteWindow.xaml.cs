using Proyecto_taller.Data;
using Proyecto_taller.Helpers;
using Proyecto_taller.Models;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

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

            // Aplicar capitalización automática al perder el foco
            txtNombre.LostFocus += (s, e) => AplicarTitleCase(txtNombre);
            txtApellido.LostFocus += (s, e) => AplicarTitleCase(txtApellido);
            txtDireccion.LostFocus += (s, e) => AplicarPrimeraLetra(txtDireccion);
        }

        // ── Helpers de capitalización ─────────────────────────────────────────

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

        // ── Guardar ───────────────────────────────────────────────────────────

        private void Guardar_Click(object sender, RoutedEventArgs e)
        {
            // Aplicar capitalización antes de validar
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

            // Validar correo solo si se ingresó algo
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
                    Direccion = txtDireccion.Text.Trim(),
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
