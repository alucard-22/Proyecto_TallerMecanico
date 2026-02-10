using Proyecto_taller.Data;
using Proyecto_taller.Helpers;
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
    /// <summary>
    /// Lógica de interacción para LoginWindow.xaml
    /// VERSIÓN SIMPLIFICADA - Sin dependencia de Microsoft.Data.SqlClient
    /// </summary>
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
            InicializarUsuariosPorDefecto();
            txtUsuario.Focus();
        }

        /// <summary>
        /// Crea usuarios por defecto si no existen
        /// </summary>
        private void InicializarUsuariosPorDefecto()
        {
            try
            {
                using var db = new TallerDbContext();

                // Verificar si ya existen usuarios
                if (db.Usuarios.Any())
                    return;

                // Crear usuario Administrador
                var admin = new Models.Usuario
                {
                    NombreUsuario = "admin",
                    PasswordHash = PasswordHelper.HashPassword("admin123"),
                    NombreCompleto = "Administrador del Sistema",
                    Rol = "Administrador",
                    Activo = true,
                    FechaCreacion = DateTime.Now
                };

                // Crear usuario Empleado
                var empleado = new Models.Usuario
                {
                    NombreUsuario = "empleado",
                    PasswordHash = PasswordHelper.HashPassword("empleado123"),
                    NombreCompleto = "Empleado del Taller",
                    Rol = "Empleado",
                    Activo = true,
                    FechaCreacion = DateTime.Now
                };

                db.Usuarios.AddRange(admin, empleado);
                db.SaveChanges();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error al inicializar usuarios:\n{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            IntentarIniciarSesion();
        }

        private void TxtUsuario_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                txtPassword.Focus();
        }

        private void TxtPassword_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                IntentarIniciarSesion();
        }

        private void IntentarIniciarSesion()
        {
            // Ocultar mensaje de error
            errorPanel.Visibility = Visibility.Collapsed;

            // Validar campos vacíos
            if (string.IsNullOrWhiteSpace(txtUsuario.Text))
            {
                MostrarError("⚠️ El campo de usuario está vacío.\n\n💡 Por favor, ingrese su nombre de usuario para continuar.");
                txtUsuario.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(txtPassword.Password))
            {
                MostrarError("⚠️ El campo de contraseña está vacío.\n\n💡 Por favor, ingrese su contraseña para continuar.");
                txtPassword.Focus();
                return;
            }

            try
            {
                using var db = new TallerDbContext();

                // Buscar usuario por nombre (sin filtrar por activo primero)
                var usuario = db.Usuarios.FirstOrDefault(u =>
                    u.NombreUsuario.ToLower() == txtUsuario.Text.Trim().ToLower());

                // Usuario no existe
                if (usuario == null)
                {
                    MostrarError(
                        $"❌ ERROR: El usuario '{txtUsuario.Text}' no existe en el sistema.\n\n" +
                        $"💡 Sugerencias:\n" +
                        $"   • Verifique que escribió correctamente el nombre de usuario\n" +
                        $"   • Recuerde que las mayúsculas y minúsculas no importan\n" +
                        $"   • Usuarios de prueba: 'admin' o 'empleado'"
                    );
                    txtUsuario.SelectAll();
                    txtUsuario.Focus();
                    return;
                }

                // Usuario existe pero está inactivo
                if (!usuario.Activo)
                {
                    MostrarError(
                        $"🔒 ERROR: El usuario '{usuario.NombreUsuario}' está INACTIVO.\n\n" +
                        $"📋 Detalles:\n" +
                        $"   • Nombre: {usuario.NombreCompleto}\n" +
                        $"   • Estado: Desactivado por el administrador\n\n" +
                        $"💡 Solución:\n" +
                        $"   • Contacte al administrador del sistema para reactivar su cuenta"
                    );
                    return;
                }

                // Verificar contraseña
                if (!PasswordHelper.VerifyPassword(txtPassword.Password, usuario.PasswordHash))
                {
                    MostrarError(
                        $"❌ ERROR: Contraseña INCORRECTA.\n\n" +
                        $"📋 Usuario identificado:\n" +
                        $"   • Usuario: {usuario.NombreUsuario}\n" +
                        $"   • Nombre: {usuario.NombreCompleto}\n\n" +
                        $"💡 Sugerencias:\n" +
                        $"   • Verifique que escribió correctamente la contraseña\n" +
                        $"   • Asegúrese de no tener activado el BLOQ MAYÚS\n" +
                        $"   • Recuerde que la contraseña SÍ distingue mayúsculas de minúsculas"
                    );
                    txtPassword.Clear();
                    txtPassword.Focus();
                    return;
                }

                // ✅ Login exitoso
                // Actualizar último acceso
                usuario.UltimoAcceso = DateTime.Now;
                db.SaveChanges();

                // Iniciar sesión
                SessionManager.IniciarSesion(usuario);

                // Mostrar mensaje de bienvenida detallado
                string mensajeBienvenida =
                    $"✅ INICIO DE SESIÓN EXITOSO\n\n" +
                    $"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n\n" +
                    $"👤 Usuario: {usuario.NombreUsuario}\n" +
                    $"📝 Nombre: {usuario.NombreCompleto}\n" +
                    $"🔑 Rol: {usuario.Rol}\n" +
                    $"🕐 Fecha: {DateTime.Now:dd/MM/yyyy HH:mm:ss}\n\n";

                if (usuario.UltimoAcceso.HasValue)
                {
                    mensajeBienvenida += $"📅 Último acceso anterior:\n   {usuario.UltimoAcceso.Value.AddSeconds(-1):dd/MM/yyyy HH:mm:ss}\n\n";
                }
                else
                {
                    mensajeBienvenida += $"🎉 ¡Este es su PRIMER INGRESO al sistema!\n\n";
                }

                mensajeBienvenida +=
                    $"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n\n" +
                    $"🚀 Sistema Taller El Choco v1.0";

                MessageBox.Show(
                    mensajeBienvenida,
                    "¡Bienvenido!",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                // Abrir ventana principal
                MainWindow mainWindow = new MainWindow();
                mainWindow.Show();

                // Cerrar ventana de login
                this.Close();
            }
            catch (DbUpdateException dbEx)
            {
                // Error específico de Entity Framework al actualizar la BD
                MostrarError(
                    $"❌ ERROR AL ACTUALIZAR LA BASE DE DATOS\n\n" +
                    $"📋 Detalles técnicos:\n" +
                    $"   • {dbEx.InnerException?.Message ?? dbEx.Message}\n\n" +
                    $"💡 Soluciones posibles:\n" +
                    $"   • Verifique que la base de datos esté accesible\n" +
                    $"   • Compruebe los permisos de escritura en la BD\n" +
                    $"   • Asegúrese de que SQL Server esté ejecutándose"
                );

                System.Diagnostics.Debug.WriteLine($"Error DbUpdate en login: {dbEx.ToString()}");
            }
            catch (InvalidOperationException invEx) when (invEx.Message.Contains("database") || invEx.Message.Contains("connection"))
            {
                // Error de conexión
                MostrarError(
                    $"❌ ERROR DE CONEXIÓN A LA BASE DE DATOS\n\n" +
                    $"📋 Detalles técnicos:\n" +
                    $"   • {invEx.Message}\n\n" +
                    $"💡 Soluciones posibles:\n" +
                    $"   • Verifique que SQL Server esté ejecutándose\n" +
                    $"   • Compruebe la cadena de conexión en appsettings.json\n" +
                    $"   • Asegúrese de tener acceso a la base de datos 'TallerMecanico'\n" +
                    $"   • Verifique que el servidor sea 'localhost' o el nombre correcto"
                );

                System.Diagnostics.Debug.WriteLine($"Error de conexión en login: {invEx.ToString()}");
            }
            catch (Exception ex)
            {
                // Detectar si es un error de SQL/Conexión por el mensaje
                string mensajeError;

                if (ex.Message.Contains("network") ||
                    ex.Message.Contains("server") ||
                    ex.Message.Contains("connection") ||
                    ex.Message.Contains("SQL") ||
                    ex.InnerException?.Message.Contains("network") == true ||
                    ex.InnerException?.Message.Contains("server") == true)
                {
                    mensajeError =
                        $"❌ ERROR DE CONEXIÓN A LA BASE DE DATOS\n\n" +
                        $"📋 Detalles técnicos:\n" +
                        $"   • {ex.InnerException?.Message ?? ex.Message}\n\n" +
                        $"💡 Soluciones posibles:\n" +
                        $"   • Verifique que SQL Server esté ejecutándose\n" +
                        $"   • Compruebe la cadena de conexión en appsettings.json\n" +
                        $"   • Asegúrese de tener acceso a la base de datos 'TallerMecanico'\n" +
                        $"   • Verifique el nombre del servidor (localhost)";
                }
                else
                {
                    mensajeError =
                        $"❌ ERROR INESPERADO\n\n" +
                        $"📋 Detalles:\n" +
                        $"   • {ex.Message}\n\n" +
                        $"💡 Recomendación:\n" +
                        $"   • Reinicie la aplicación\n" +
                        $"   • Si el problema persiste, contacte al soporte técnico";
                }

                MostrarError(mensajeError);
                System.Diagnostics.Debug.WriteLine($"Error general en login: {ex.ToString()}");
            }
        }

        private void MostrarError(string mensaje)
        {
            txtError.Text = mensaje;
            errorPanel.Visibility = Visibility.Visible;

            // Efecto visual: hacer que el panel de error "parpadee" y se agrande ligeramente
            var fadeAnimation = new System.Windows.Media.Animation.DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(300)
            };

            var scaleTransform = new ScaleTransform(1, 1);
            errorPanel.RenderTransform = scaleTransform;
            errorPanel.RenderTransformOrigin = new Point(0.5, 0.5);

            var scaleAnimation = new System.Windows.Media.Animation.DoubleAnimation
            {
                From = 0.95,
                To = 1.0,
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new System.Windows.Media.Animation.QuadraticEase()
            };

            errorPanel.BeginAnimation(OpacityProperty, fadeAnimation);
            scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnimation);
            scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnimation);

            // Reproducir sonido de error del sistema (opcional)
            System.Media.SystemSounds.Hand.Play();
        }

        /// <summary>
        /// Muestra un MessageBox de error más detallado (método auxiliar opcional)
        /// </summary>
        private void MostrarErrorDetallado(string titulo, string mensaje, string detalles = "")
        {
            string mensajeCompleto = mensaje;

            if (!string.IsNullOrEmpty(detalles))
            {
                mensajeCompleto += $"\n\n━━━━━━━━━━━━━━━━━━━━━\n\n{detalles}";
            }

            MessageBox.Show(
                mensajeCompleto,
                titulo,
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}