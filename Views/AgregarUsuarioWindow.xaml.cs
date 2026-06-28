using Proyecto_taller.Data;
using Proyecto_taller.Helpers;
using Proyecto_taller.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Proyecto_taller.Views
{
    public partial class AgregarUsuarioWindow : Window
    {
        // Indican si cada campo de contraseña se está mostrando en texto plano
        private bool _passwordVisible = false;
        private bool _confirmarPasswordVisible = false;

        public AgregarUsuarioWindow()
        {
            InitializeComponent();
            txtNombreUsuario.Focus();
            ActualizarVisibilidadPermisos();
        }

        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }

        private void CmbRol_SelectionChanged(object sender, SelectionChangedEventArgs e)
            => ActualizarVisibilidadPermisos();

        private void ActualizarVisibilidadPermisos()
        {
            if (panelPermisos == null || cmbRol == null) return;
            bool esEmpleado = (cmbRol.SelectedIndex == 1);
            panelPermisos.Visibility = esEmpleado
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        // ── NUEVO: mostrar/ocultar contraseña ─────────────────────────────────

        private void ToggleMostrarPassword_Click(object sender, RoutedEventArgs e)
        {
            _passwordVisible = !_passwordVisible;

            if (_passwordVisible)
            {
                txtPasswordVisible.Text = txtPassword.Password;
                txtPassword.Visibility = Visibility.Collapsed;
                txtPasswordVisible.Visibility = Visibility.Visible;
                txtPasswordVisible.Focus();
                txtPasswordVisible.CaretIndex = txtPasswordVisible.Text.Length;
                btnTogglePassword.Content = "🙈";
            }
            else
            {
                txtPassword.Password = txtPasswordVisible.Text;
                txtPasswordVisible.Visibility = Visibility.Collapsed;
                txtPassword.Visibility = Visibility.Visible;
                txtPassword.Focus();
                btnTogglePassword.Content = "👁️";
            }
        }

        private void ToggleMostrarConfirmarPassword_Click(object sender, RoutedEventArgs e)
        {
            _confirmarPasswordVisible = !_confirmarPasswordVisible;

            if (_confirmarPasswordVisible)
            {
                txtConfirmarPasswordVisible.Text = txtConfirmarPassword.Password;
                txtConfirmarPassword.Visibility = Visibility.Collapsed;
                txtConfirmarPasswordVisible.Visibility = Visibility.Visible;
                txtConfirmarPasswordVisible.Focus();
                txtConfirmarPasswordVisible.CaretIndex = txtConfirmarPasswordVisible.Text.Length;
                btnToggleConfirmarPassword.Content = "🙈";
            }
            else
            {
                txtConfirmarPassword.Password = txtConfirmarPasswordVisible.Text;
                txtConfirmarPasswordVisible.Visibility = Visibility.Collapsed;
                txtConfirmarPassword.Visibility = Visibility.Visible;
                txtConfirmarPassword.Focus();
                btnToggleConfirmarPassword.Content = "👁️";
            }
        }

        private string ObtenerPassword()
            => _passwordVisible ? txtPasswordVisible.Text : txtPassword.Password;

        private string ObtenerConfirmarPassword()
            => _confirmarPasswordVisible ? txtConfirmarPasswordVisible.Text : txtConfirmarPassword.Password;

        // ── Permisos ──────────────────────────────────────────────────────────

        private void SeleccionarTodos_Click(object sender, RoutedEventArgs e)
        {
            chkClientes.IsChecked = true;
            chkVehiculos.IsChecked = true;
            chkTrabajos.IsChecked = true;
            chkReservas.IsChecked = true;
            chkInventario.IsChecked = true;
            chkRecibos.IsChecked = true;
            chkReportes.IsChecked = true;
        }

        private void SeleccionarNinguno_Click(object sender, RoutedEventArgs e)
        {
            chkClientes.IsChecked = false;
            chkVehiculos.IsChecked = false;
            chkTrabajos.IsChecked = false;
            chkReservas.IsChecked = false;
            chkInventario.IsChecked = false;
            chkRecibos.IsChecked = false;
            chkReportes.IsChecked = false;
        }

        private List<string> ObtenerPermisosSeleccionados()
        {
            var permisos = new List<string> { "Inicio" }; // Siempre incluido

            if (chkClientes.IsChecked == true) permisos.Add("Clientes");
            if (chkVehiculos.IsChecked == true) permisos.Add("Vehiculos");
            if (chkTrabajos.IsChecked == true) permisos.Add("Trabajos");
            if (chkReservas.IsChecked == true) permisos.Add("Reservas");
            if (chkInventario.IsChecked == true) permisos.Add("Inventario");
            if (chkRecibos.IsChecked == true) permisos.Add("Recibos");
            if (chkReportes.IsChecked == true) permisos.Add("Reportes");

            return permisos;
        }

        // ── Guardar ───────────────────────────────────────────────────────────

        private void GuardarButton_Click(object sender, RoutedEventArgs e)
        {
            string password = ObtenerPassword();
            string confirmarPassword = ObtenerConfirmarPassword();

            // Validaciones
            if (string.IsNullOrWhiteSpace(txtNombreUsuario.Text))
            {
                MessageBox.Show("El nombre de usuario es obligatorio.", "Validación",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtNombreUsuario.Focus();
                return;
            }
            if (string.IsNullOrWhiteSpace(txtNombreCompleto.Text))
            {
                MessageBox.Show("El nombre completo es obligatorio.", "Validación",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtNombreCompleto.Focus();
                return;
            }
            if (string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("La contraseña es obligatoria.", "Validación",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (password.Length < 6)
            {
                MessageBox.Show("La contraseña debe tener al menos 6 caracteres.", "Validación",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (password != confirmarPassword)
            {
                MessageBox.Show("Las contraseñas no coinciden.", "Validación",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string rol = ((ComboBoxItem)cmbRol.SelectedItem).Content.ToString();

            // Validar que empleado tenga al menos un permiso
            if (rol == "Empleado")
            {
                var permisos = ObtenerPermisosSeleccionados();
                if (permisos.Count <= 1) // Solo tiene "Inicio"
                {
                    var continuar = MessageBox.Show(
                        "Este empleado no tiene ningún módulo asignado (solo Inicio).\n\n" +
                        "¿Deseas continuar de todas formas?",
                        "Sin permisos",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning);
                    if (continuar != MessageBoxResult.Yes) return;
                }
            }

            try
            {
                using var db = new TallerDbContext();

                var usuarioExiste = db.Usuarios.Any(u =>
                    u.NombreUsuario.ToLower() == txtNombreUsuario.Text.ToLower());

                if (usuarioExiste)
                {
                    MessageBox.Show("Ya existe un usuario con ese nombre de usuario.",
                        "Usuario Duplicado", MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtNombreUsuario.Focus();
                    return;
                }

                var nuevoUsuario = new Usuario
                {
                    NombreUsuario = txtNombreUsuario.Text.Trim(),
                    NombreCompleto = txtNombreCompleto.Text.Trim(),
                    PasswordHash = PasswordHelper.HashPassword(password),
                    Rol = rol,
                    Activo = chkActivo.IsChecked == true,
                    FechaCreacion = DateTime.Now
                };

                // Asignar permisos
                if (rol == "Administrador")
                    nuevoUsuario.Permisos = SessionManager.ModulosDisponibles;
                else
                    nuevoUsuario.Permisos = ObtenerPermisosSeleccionados();

                db.Usuarios.Add(nuevoUsuario);
                db.SaveChanges();

                string permisosStr = rol == "Administrador"
                    ? "Acceso completo (Administrador)"
                    : string.Join(", ", nuevoUsuario.Permisos);

                MessageBox.Show(
                    $"✅ Usuario '{nuevoUsuario.NombreUsuario}' creado exitosamente.\n\n" +
                    $"Rol:      {nuevoUsuario.Rol}\n" +
                    $"Estado:   {(nuevoUsuario.Activo ? "Activo" : "Inactivo")}\n" +
                    $"Módulos:  {permisosStr}",
                    "Usuario Creado",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al crear el usuario:\n{ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
