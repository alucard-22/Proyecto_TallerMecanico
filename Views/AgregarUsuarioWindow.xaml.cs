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

        private void GuardarButton_Click(object sender, RoutedEventArgs e)
        {
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
            if (string.IsNullOrWhiteSpace(txtPassword.Password))
            {
                MessageBox.Show("La contraseña es obligatoria.", "Validación",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtPassword.Focus();
                return;
            }
            if (txtPassword.Password.Length < 6)
            {
                MessageBox.Show("La contraseña debe tener al menos 6 caracteres.", "Validación",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtPassword.Focus();
                return;
            }
            if (txtPassword.Password != txtConfirmarPassword.Password)
            {
                MessageBox.Show("Las contraseñas no coinciden.", "Validación",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtConfirmarPassword.Focus();
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
                    PasswordHash = PasswordHelper.HashPassword(txtPassword.Password),
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