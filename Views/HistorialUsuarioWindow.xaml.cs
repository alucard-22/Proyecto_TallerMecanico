using Proyecto_taller.Data;
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
using Microsoft.EntityFrameworkCore;

namespace Proyecto_taller.Views
{
    public partial class HistorialUsuarioWindow : Window
    {
        private readonly int _usuarioId;

        public HistorialUsuarioWindow(int usuarioId)
        {
            InitializeComponent();
            _usuarioId = usuarioId;
            Loaded += (_, __) => Cargar();
        }
        
        private void Cargar()
        {
            try
            {
                using var db = new TallerDbContext();
                var usuario = db.Usuarios.Find(_usuarioId);
                if (usuario == null) { Close(); return; }

                // ── Header ────────────────────────────────────────────────────
                txtTitulo.Text = $"👤  {usuario.NombreCompleto}";
                txtRol.Text = usuario.Rol;
                txtSubtitulo.Text = $"@{usuario.NombreUsuario}   ·   " +
                                    $"Creado el {usuario.FechaCreacion:dd/MM/yyyy}";

                // Color del badge según rol
                if (usuario.Rol == "Administrador")
                {
                    badgeRol.Background = new SolidColorBrush(
                        Color.FromRgb(49, 46, 107));
                    txtRol.Foreground = (Brush)FindResource("AccentLight");
                }
                else
                {
                    badgeRol.Background = new SolidColorBrush(
                        Color.FromRgb(24, 40, 32));
                    txtRol.Foreground = (Brush)FindResource("ColorSuccess");
                }

                // ── Ficha ─────────────────────────────────────────────────────
                txtNombreUsuario.Text = usuario.NombreUsuario;
                txtNombreCompleto.Text = usuario.NombreCompleto;

                txtEstado.Text = usuario.Activo ? "✓  Activo" : "✕  Inactivo";
                txtEstado.Foreground = usuario.Activo
                    ? (Brush)FindResource("ColorSuccess")
                    : (Brush)FindResource("ColorDanger");

                txtFechaCreacion.Text = usuario.FechaCreacion.ToString("dd/MM/yyyy HH:mm");

                txtUltimoAcceso.Text = usuario.UltimoAcceso.HasValue
                    ? usuario.UltimoAcceso.Value.ToString("dd/MM/yyyy HH:mm")
                    : "Nunca";

                txtUltimoCambioPass.Text = usuario.FechaUltimoCambioPassword.HasValue
                    ? usuario.FechaUltimoCambioPassword.Value.ToString("dd/MM/yyyy HH:mm")
                    : "Sin cambios registrados";

                // ── Estadísticas ──────────────────────────────────────────────
                // Días activo: diferencia entre creación y hoy (aproximado)
                int diasActivo = (DateTime.Now - usuario.FechaCreacion).Days;
                txtDiasActivo.Text = Math.Max(0, diasActivo).ToString();

                // Cambios de contraseña: dato del campo FechaUltimoCambioPassword
                // (indicamos 1 si hay fecha, 0 si no; en una versión futura
                // se puede agregar un contador real en la tabla)
                txtCambiosPass.Text = usuario.FechaUltimoCambioPassword.HasValue ? "≥1" : "0";

                txtRolCard.Text = usuario.Rol;
                txtRolCard.Foreground = usuario.Rol == "Administrador"
                    ? (Brush)FindResource("AccentLight")
                    : (Brush)FindResource("ColorSuccess");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar historial:\n{ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }
        private void Cerrar_Click(object sender, RoutedEventArgs e) => Close();
    }
}
