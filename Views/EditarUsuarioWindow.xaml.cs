using Proyecto_taller.Data;
using Proyecto_taller.Helpers;
using Proyecto_taller.Models;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Proyecto_taller.Views
{
    public partial class EditarUsuarioWindow : Window
    {
        private readonly Usuario _usuario;

        public EditarUsuarioWindow(Usuario usuario)
        {
            InitializeComponent();

            _usuario = usuario;

            Loaded += EditarUsuarioWindow_Loaded;
        }

        private void EditarUsuarioWindow_Loaded(object sender, RoutedEventArgs e)
        {
            txtTitulo.Text = $"✏️ Editar — {_usuario.NombreUsuario}";
            txtSubtitulo.Text =
                "Puedes cambiar el nombre completo, rol y permisos.";

            txtNombreUsuario.Text =
                _usuario.NombreUsuario;

            txtNombreCompleto.Text =
                _usuario.NombreCompleto;

            // Seleccionar rol
            foreach (ComboBoxItem item in cmbRol.Items)
            {
                if ((item.Tag?.ToString() ?? "")
                    == _usuario.Rol)
                {
                    cmbRol.SelectedItem = item;
                    break;
                }
            }

            // Rol por defecto
            if (cmbRol.SelectedItem == null)
            {
                foreach (ComboBoxItem item in cmbRol.Items)
                {
                    if ((item.Tag?.ToString() ?? "")
                        == "Empleado")
                    {
                        cmbRol.SelectedItem = item;
                        break;
                    }
                }
            }

            CargarPermisosActuales(
                _usuario.Permisos
            );

            ActualizarVisibilidadPermisos();

            txtNombreCompleto.Focus();
        }

        private void CargarPermisosActuales(
            List<string> permisos)
        {
            permisos ??= new();

            chkClientes.IsChecked =
                permisos.Contains("Clientes");

            chkVehiculos.IsChecked =
                permisos.Contains("Vehiculos");

            chkTrabajos.IsChecked =
                permisos.Contains("Trabajos");

            chkReservas.IsChecked =
                permisos.Contains("Reservas");

            chkInventario.IsChecked =
                permisos.Contains("Inventario");

            chkRecibos.IsChecked =
                permisos.Contains("Recibos");

            chkReportes.IsChecked =
                permisos.Contains("Reportes");
        }

        private void Border_MouseDown(
            object sender,
            MouseButtonEventArgs e)
        {
            if (e.LeftButton ==
                MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void CmbRol_SelectionChanged(
            object sender,
            SelectionChangedEventArgs e)
        {
            ActualizarVisibilidadPermisos();
        }

        private void ActualizarVisibilidadPermisos()
        {
            if (panelPermisos == null)
                return;

            string rol =
                (cmbRol.SelectedItem
                    as ComboBoxItem)?
                .Tag?.ToString()
                ?? "Empleado";

            // Administrador → acceso total
            // Empleado → permisos por módulo

            panelPermisos.Visibility =
                rol == "Empleado"
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private void SeleccionarTodos_Click(
            object sender,
            RoutedEventArgs e)
        {
            chkClientes.IsChecked = true;
            chkVehiculos.IsChecked = true;
            chkTrabajos.IsChecked = true;
            chkReservas.IsChecked = true;
            chkInventario.IsChecked = true;
            chkRecibos.IsChecked = true;
            chkReportes.IsChecked = true;
        }

        private void SeleccionarNinguno_Click(
            object sender,
            RoutedEventArgs e)
        {
            chkClientes.IsChecked = false;
            chkVehiculos.IsChecked = false;
            chkTrabajos.IsChecked = false;
            chkReservas.IsChecked = false;
            chkInventario.IsChecked = false;
            chkRecibos.IsChecked = false;
            chkReportes.IsChecked = false;
        }

        private List<string>
            ObtenerPermisosSeleccionados()
        {
            var permisos =
                new List<string>
                {
                    "Inicio"
                };

            if (chkClientes.IsChecked == true)
                permisos.Add("Clientes");

            if (chkVehiculos.IsChecked == true)
                permisos.Add("Vehiculos");

            if (chkTrabajos.IsChecked == true)
                permisos.Add("Trabajos");

            if (chkReservas.IsChecked == true)
                permisos.Add("Reservas");

            if (chkInventario.IsChecked == true)
                permisos.Add("Inventario");

            if (chkRecibos.IsChecked == true)
                permisos.Add("Recibos");

            if (chkReportes.IsChecked == true)
                permisos.Add("Reportes");

            return permisos;
        }

        private void Guardar_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(
                txtNombreCompleto.Text))
            {
                MessageBox.Show(
                    "El nombre completo es obligatorio.",
                    "Validación",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                txtNombreCompleto.Focus();

                return;
            }

            try
            {
                using var db =
                    new TallerDbContext();

                var usuario =
                    db.Usuarios.Find(
                        _usuario.UsuarioID);

                if (usuario == null)
                {
                    MessageBox.Show(
                        "Usuario no encontrado.",
                        "Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    return;
                }

                string rol =
                    (cmbRol.SelectedItem
                        as ComboBoxItem)?
                    .Tag?.ToString()
                    ?? "Empleado";

                usuario.NombreCompleto =
                    txtNombreCompleto.Text.Trim();

                usuario.Rol = rol;

                if (rol == "Administrador")
                {
                    usuario.Permisos =
                        SessionManager
                        .ModulosDisponibles;
                }
                else
                {
                    usuario.Permisos =
                        ObtenerPermisosSeleccionados();
                }

                db.SaveChanges();

                MessageBox.Show(
                    "Usuario actualizado correctamente.",
                    "Guardado",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                DialogResult = true;

                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error al guardar:\n\n{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void Cancelar_Click(
            object sender,
            RoutedEventArgs e)
        {
            DialogResult = false;

            Close();
        }
    }
}