using Microsoft.Data.SqlClient;
using Proyecto_taller.Data;
using Proyecto_taller.Helpers;
using Proyecto_taller.Models;
using Proyecto_taller.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Proyecto_taller.ViewModels
{
    public class ConfiguracionViewModel : INotifyPropertyChanged
    {
        // ── Información del taller ────────────────────────────────────────────
        private string _nombreTaller = string.Empty;
        private string _direccionTaller = string.Empty;
        private string _telefonoTaller = string.Empty;
        private string _emailTaller = string.Empty;
        private string _nitTaller = string.Empty;

        // ── Facturación ───────────────────────────────────────────────────────
        private decimal _descuentoMaximo;
        private bool _solicitarNIT;

        // ── Base de datos ─────────────────────────────────────────────────────
        private string _connectionString = string.Empty;
        private DateTime _ultimoRespaldo;
        private int _totalRegistros;

        // ── Servicios ─────────────────────────────────────────────────────────
        private Servicio? _servicioSeleccionado;

        public ObservableCollection<Servicio> Servicios { get; set; } = new();

        // ── Propiedades ───────────────────────────────────────────────────────

        public string NombreTaller
        {
            get => _nombreTaller;
            set { _nombreTaller = value; OnPropertyChanged(); }
        }
        public string DireccionTaller
        {
            get => _direccionTaller;
            set { _direccionTaller = value; OnPropertyChanged(); }
        }
        public string TelefonoTaller
        {
            get => _telefonoTaller;
            set { _telefonoTaller = value; OnPropertyChanged(); }
        }
        public string EmailTaller
        {
            get => _emailTaller;
            set { _emailTaller = value; OnPropertyChanged(); }
        }
        public string NITTaller
        {
            get => _nitTaller;
            set { _nitTaller = value; OnPropertyChanged(); }
        }
        public decimal DescuentoMaximo
        {
            get => _descuentoMaximo;
            set { _descuentoMaximo = value; OnPropertyChanged(); }
        }
        public bool SolicitarNIT
        {
            get => _solicitarNIT;
            set { _solicitarNIT = value; OnPropertyChanged(); }
        }
        public string ConnectionString
        {
            get => _connectionString;
            set { _connectionString = value; OnPropertyChanged(); }
        }
        public DateTime UltimoRespaldo
        {
            get => _ultimoRespaldo;
            set { _ultimoRespaldo = value; OnPropertyChanged(); }
        }
        public int TotalRegistros
        {
            get => _totalRegistros;
            set { _totalRegistros = value; OnPropertyChanged(); }
        }
        public Servicio? ServicioSeleccionado
        {
            get => _servicioSeleccionado;
            set
            {
                _servicioSeleccionado = value;
                OnPropertyChanged();
                CommandManager.InvalidateRequerySuggested();
            }
        }

        // ── Comandos ──────────────────────────────────────────────────────────

        public ICommand GuardarInformacionCommand { get; }
        public ICommand GuardarFacturacionCommand { get; }
        public ICommand GuardarConexionCommand { get; }
        public ICommand ProbarConexionCommand { get; }
        public ICommand RespaldarBDCommand { get; }
        public ICommand AgregarServicioCommand { get; }
        public ICommand EditarServicioCommand { get; }
        public ICommand EliminarServicioCommand { get; }
        public ICommand LimpiarLogsCommand { get; }
        public ICommand VerEstadisticasCommand { get; }
        public ICommand ReiniciarBDCommand { get; }

        // ── Constructor ───────────────────────────────────────────────────────

        public ConfiguracionViewModel()
        {
            GuardarInformacionCommand = new RelayCommand(GuardarInformacion);
            GuardarFacturacionCommand = new RelayCommand(GuardarFacturacion);
            GuardarConexionCommand = new RelayCommand(GuardarConexion);
            ProbarConexionCommand = new RelayCommand(ProbarConexion);
            RespaldarBDCommand = new RelayCommand(RespaldarBD);
            AgregarServicioCommand = new RelayCommand(AgregarServicio);
            EditarServicioCommand = new RelayCommand(EditarServicio,
                                            () => ServicioSeleccionado != null);
            EliminarServicioCommand = new RelayCommand(EliminarServicio,
                                            () => ServicioSeleccionado != null);
            LimpiarLogsCommand = new RelayCommand(LimpiarLogs);
            VerEstadisticasCommand = new RelayCommand(VerEstadisticas);
            ReiniciarBDCommand = new RelayCommand(ReiniciarBD);

            CargarConfiguracion();
            CargarServicios();
            CargarEstadisticas();
        }

        // ── Carga inicial ─────────────────────────────────────────────────────

        private void CargarConfiguracion()
        {
            try
            {
                var config = ConfiguracionHelper.CargarConfiguracion();
                NombreTaller = config.NombreTaller;
                DireccionTaller = config.DireccionTaller;
                TelefonoTaller = config.TelefonoTaller;
                EmailTaller = config.EmailTaller;
                NITTaller = config.NITTaller;
                DescuentoMaximo = config.DescuentoMaximo;
                SolicitarNIT = config.SolicitarNIT;
                UltimoRespaldo = config.UltimoRespaldo;

                // FIX: leer la cadena de conexión desde appsettings.json real,
                // no del JSON de AppData (que era solo una copia desactualizada).
                var connReal = ConfiguracionHelper.LeerConnectionStringActual();
                ConnectionString = string.IsNullOrEmpty(connReal)
                    ? config.ConnectionString
                    : connReal;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error al cargar configuración:\n{ex.Message}\n\nSe usarán valores por defecto.",
                    "Advertencia", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void CargarServicios()
        {
            Servicios.Clear();
            using var db = new TallerDbContext();
            foreach (var s in db.Servicios.OrderBy(s => s.Nombre).ToList())
                Servicios.Add(s);
        }

        private void CargarEstadisticas()
        {
            try
            {
                using var db = new TallerDbContext();
                TotalRegistros = db.Clientes.Count()
                               + db.Vehiculos.Count()
                               + db.Trabajos.Count()
                               + db.Facturas.Count();
            }
            catch { TotalRegistros = 0; }
        }

        // ── Información del taller ────────────────────────────────────────────

        private void GuardarInformacion()
        {
            if (string.IsNullOrWhiteSpace(NombreTaller))
            {
                MessageBox.Show("El nombre del taller no puede estar vacío.",
                    "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var config = ConfiguracionHelper.CargarConfiguracion();
                config.NombreTaller = NombreTaller.Trim();
                config.DireccionTaller = DireccionTaller.Trim();
                config.TelefonoTaller = TelefonoTaller.Trim();
                config.EmailTaller = EmailTaller.Trim();
                config.NITTaller = NITTaller.Trim();
                ConfiguracionHelper.GuardarConfiguracion(config);

                MessageBox.Show(
                    $"✅  INFORMACIÓN DEL TALLER GUARDADA\n\n" +
                    $"Nombre:    {NombreTaller}\n" +
                    $"Dirección: {DireccionTaller}\n" +
                    $"Teléfono:  {TelefonoTaller}\n" +
                    $"Email:     {EmailTaller}\n" +
                    $"NIT:       {NITTaller}\n\n" +
                    $"Los PDFs de facturas generados a partir de ahora\n" +
                    $"usarán estos datos actualizados.",
                    "Guardado", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al guardar:\n{ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ── Facturación ───────────────────────────────────────────────────────

        private void GuardarFacturacion()
        {
            if (DescuentoMaximo < 0 || DescuentoMaximo > 100)
            {
                MessageBox.Show("El descuento máximo debe estar entre 0% y 100%.",
                    "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var config = ConfiguracionHelper.CargarConfiguracion();
                config.DescuentoMaximo = DescuentoMaximo;
                config.SolicitarNIT = SolicitarNIT;
                ConfiguracionHelper.GuardarConfiguracion(config);

                MessageBox.Show(
                    $"✅  CONFIGURACIÓN DE FACTURACIÓN GUARDADA\n\n" +
                    $"Descuento máximo: {DescuentoMaximo}%\n" +
                    $"Solicitar NIT:    {(SolicitarNIT ? "Sí" : "No")}",
                    "Guardado", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al guardar:\n{ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ── Cadena de conexión ────────────────────────────────────────────────
        // FIX: ahora "Guardar Conexión" escribe en appsettings.json directamente,
        // para que TallerDbContext la lea en el próximo arranque de la app.

        private void GuardarConexion()
        {
            if (string.IsNullOrWhiteSpace(ConnectionString))
            {
                MessageBox.Show("La cadena de conexión no puede estar vacía.",
                    "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Probar la conexión antes de guardar
            try
            {
                using var conn = new SqlConnection(ConnectionString);
                conn.Open();
                conn.Close();
            }
            catch (Exception ex)
            {
                var continuar = MessageBox.Show(
                    $"⚠️  La conexión falló con la cadena ingresada:\n\n{ex.Message}\n\n" +
                    $"¿Guardar igualmente? (La app no funcionará hasta que la cadena sea correcta)",
                    "Conexión fallida", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (continuar != MessageBoxResult.Yes) return;
            }

            try
            {
                // Guardar en appsettings.json (lo que lee TallerDbContext)
                ConfiguracionHelper.GuardarConnectionString(ConnectionString);

                // Guardar también en el JSON de preferencias como copia de referencia
                var config = ConfiguracionHelper.CargarConfiguracion();
                config.ConnectionString = ConnectionString;
                ConfiguracionHelper.GuardarConfiguracion(config);

                MessageBox.Show(
                    $"✅  CADENA DE CONEXIÓN GUARDADA\n\n" +
                    $"El cambio toma efecto al reiniciar la aplicación.\n\n" +
                    $"Archivo actualizado:\nappsettings.json",
                    "Guardado", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al guardar la cadena:\n{ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ── Probar conexión ───────────────────────────────────────────────────

        private void ProbarConexion()
        {
            try
            {
                using var conn = new SqlConnection(ConnectionString);
                conn.Open();

                MessageBox.Show(
                    $"✅  Conexión exitosa con la base de datos.\n\n" +
                    $"Servidor: {conn.DataSource}\n" +
                    $"BD:       {conn.Database}",
                    "Conexión OK", MessageBoxButton.OK, MessageBoxImage.Information);

                conn.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"❌  No se pudo conectar a la base de datos.\n\n" +
                    $"Cadena usada:\n{ConnectionString}\n\n" +
                    $"Error:\n{ex.Message}",
                    "Error de Conexión", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ── Respaldo ──────────────────────────────────────────────────────────

        private void RespaldarBD()
        {
            var r = MessageBox.Show(
                "¿Crear un respaldo completo de la base de datos?\n\n" +
                "El archivo .bak se guardará en:\n" +
                "Documentos\\TallerElChoco_Backups\\",
                "Confirmar Respaldo", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (r != MessageBoxResult.Yes) return;

            try
            {
                string carpeta = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "TallerElChoco_Backups");
                Directory.CreateDirectory(carpeta);

                string fechaStr = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string nombreArchivo = $"TallerMecanico_{fechaStr}.bak";
                string rutaBak = Path.Combine(carpeta, nombreArchivo);

                string sql =
                    $"BACKUP DATABASE [TallerMecanico] TO DISK = N'{rutaBak}' " +
                    $"WITH NOFORMAT, INIT, NAME = N'TallerMecanico-Backup', " +
                    $"SKIP, NOREWIND, NOUNLOAD, STATS = 10";

                using var conn = new SqlConnection(ConnectionString);
                using var command = new SqlCommand(sql, conn) { CommandTimeout = 120 };
                conn.Open();
                command.ExecuteNonQuery();
                conn.Close();

                UltimoRespaldo = DateTime.Now;
                var config = ConfiguracionHelper.CargarConfiguracion();
                config.UltimoRespaldo = UltimoRespaldo;
                ConfiguracionHelper.GuardarConfiguracion(config);

                var abrir = MessageBox.Show(
                    $"✅  RESPALDO CREADO EXITOSAMENTE\n\n📁  {rutaBak}\n\n¿Abrir la carpeta?",
                    "Respaldo OK", MessageBoxButton.YesNo, MessageBoxImage.Information);

                if (abrir == MessageBoxResult.Yes)
                    Process.Start(new ProcessStartInfo(carpeta) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"❌  Error al crear el respaldo:\n\n{ex.Message}\n\n" +
                    $"Verifica que SQL Server tenga permisos de escritura en la carpeta destino.",
                    "Error de Respaldo", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ── Servicios — CRUD ──────────────────────────────────────────────────

        private void AgregarServicio()
        {
            var win = new EditarServicioWindow();
            if (win.ShowDialog() == true)
                CargarServicios();
        }

        private void EditarServicio()
        {
            if (ServicioSeleccionado == null) return;
            var win = new EditarServicioWindow(ServicioSeleccionado);
            if (win.ShowDialog() == true)
                CargarServicios();
        }

        private void EliminarServicio()
        {
            if (ServicioSeleccionado == null) return;

            var r = MessageBox.Show(
                $"¿Eliminar el servicio '{ServicioSeleccionado.Nombre}'?\n\n" +
                $"⚠️ Si está asociado a trabajos existentes, la eliminación fallará.",
                "Confirmar Eliminación", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (r != MessageBoxResult.Yes) return;

            try
            {
                using var db = new TallerDbContext();

                bool tieneTrabajos = db.Trabajos_Servicios
                    .Any(ts => ts.ServicioID == ServicioSeleccionado.ServicioID);

                if (tieneTrabajos)
                {
                    MessageBox.Show(
                        $"No se puede eliminar '{ServicioSeleccionado.Nombre}' porque está\n" +
                        $"asociado a uno o más trabajos.\n\nPuedes cambiar su nombre o costo.",
                        "No se puede eliminar", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var svc = db.Servicios.Find(ServicioSeleccionado.ServicioID);
                if (svc != null) { db.Servicios.Remove(svc); db.SaveChanges(); }

                CargarServicios();
                MessageBox.Show("Servicio eliminado correctamente.",
                    "Eliminado", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al eliminar:\n{ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ── Limpiar logs ──────────────────────────────────────────────────────

        private void LimpiarLogs()
        {
            var r = MessageBox.Show(
                "¿Limpiar los archivos de log del sistema?\n\n" +
                "Se eliminarán los archivos .log de la carpeta de datos de la aplicación.",
                "Confirmar Limpieza", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (r != MessageBoxResult.Yes) return;

            try
            {
                string carpetaApp = Path.GetDirectoryName(
                    ConfiguracionHelper.ObtenerRutaConfiguracion())!;

                if (!Directory.Exists(carpetaApp))
                {
                    MessageBox.Show("No hay carpeta de logs.",
                        "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var archivosLog = Directory.GetFiles(carpetaApp, "*.log");
                int eliminados = 0;

                foreach (var archivo in archivosLog)
                {
                    try { File.Delete(archivo); eliminados++; }
                    catch { /* archivo en uso, se omite */ }
                }

                MessageBox.Show(
                    eliminados > 0
                        ? $"✅  Se eliminaron {eliminados} archivo(s) de log."
                        : "No se encontraron archivos de log para limpiar.",
                    "Limpieza Completada", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al limpiar logs:\n{ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ── Estadísticas ──────────────────────────────────────────────────────

        private void VerEstadisticas()
        {
            try
            {
                using var db = new TallerDbContext();

                decimal totalFacturado = db.Facturas
                    .Where(f => f.Estado == "Pagada")
                    .Sum(f => (decimal?)f.Total) ?? 0;

                string stats =
                    $"📊  ESTADÍSTICAS DEL SISTEMA\n" +
                    $"{"━",38}\n\n" +
                    $"👥  Clientes:           {db.Clientes.Count()}\n" +
                    $"🚗  Vehículos:          {db.Vehiculos.Count()}\n\n" +
                    $"🔧  Trabajos:           {db.Trabajos.Count()}\n" +
                    $"    ⏳ Pendientes:      {db.Trabajos.Count(t => t.Estado == "Pendiente")}\n" +
                    $"    🔄 En Progreso:     {db.Trabajos.Count(t => t.Estado == "En Progreso")}\n" +
                    $"    ✅ Finalizados:     {db.Trabajos.Count(t => t.Estado == "Finalizado")}\n\n" +
                    $"📅  Reservas:           {db.Reservas.Count()}\n" +
                    $"    ⏳ Pendientes:      {db.Reservas.Count(r => r.Estado == "Pendiente")}\n\n" +
                    $"📄  Facturas:           {db.Facturas.Count()}\n" +
                    $"    💰 Total facturado: Bs. {totalFacturado:N2}\n\n" +
                    $"⚙️   Servicios:          {db.Servicios.Count()}\n" +
                    $"📦  Repuestos:          {db.Repuestos.Count()}\n" +
                    $"    ⚠️ Stock bajo:      {db.Repuestos.Count(r => r.StockActual <= r.StockMinimo)}\n\n" +
                    $"👤  Usuarios:           {db.Usuarios.Count()}\n\n" +
                    $"{"━",38}\n" +
                    $"Total registros: {TotalRegistros}";

                MessageBox.Show(stats, "Estadísticas del Sistema",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar estadísticas:\n{ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ── Reiniciar BD ──────────────────────────────────────────────────────
        // FIX: antes no borraba usuarios, dejando el sistema en estado inconsistente.
        // Ahora pregunta explícitamente si también se quieren borrar los usuarios,
        // y siempre preserva al admin para no dejar el sistema sin acceso.

        private void ReiniciarBD()
        {
            var r1 = MessageBox.Show(
                "⚠️  ADVERTENCIA CRÍTICA ⚠️\n\n" +
                "Esta acción eliminará TODOS los datos del sistema.\n" +
                "Esta operación NO se puede deshacer.\n\n" +
                "¿Está completamente seguro de continuar?",
                "Confirmar Reinicio", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (r1 != MessageBoxResult.Yes) return;

            var r2 = MessageBox.Show(
                "Última confirmación.\n\n¿Eliminar TODOS los datos de la base de datos?",
                "Confirmación Final", MessageBoxButton.YesNo, MessageBoxImage.Stop);

            if (r2 != MessageBoxResult.Yes) return;

            // FIX: preguntar explícitamente qué hacer con los usuarios
            var r3 = MessageBox.Show(
                "¿También deseas reiniciar los USUARIOS del sistema?\n\n" +
                "• Sí → se borran todos los usuarios excepto 'admin'\n" +
                "• No → se conservan todos los usuarios actuales",
                "Reiniciar Usuarios", MessageBoxButton.YesNo, MessageBoxImage.Question);

            bool reiniciarUsuarios = (r3 == MessageBoxResult.Yes);

            try
            {
                using var db = new TallerDbContext();

                // Borrar en orden para respetar las FK
                db.Pagos.RemoveRange(db.Pagos);
                db.Trabajos_Repuestos.RemoveRange(db.Trabajos_Repuestos);
                db.Trabajos_Servicios.RemoveRange(db.Trabajos_Servicios);
                db.Facturas.RemoveRange(db.Facturas);
                db.Reservas.RemoveRange(db.Reservas);
                db.Trabajos.RemoveRange(db.Trabajos);
                db.Vehiculos.RemoveRange(db.Vehiculos);
                db.Clientes.RemoveRange(db.Clientes);

                if (reiniciarUsuarios)
                {
                    // Preservar al admin para no dejar el sistema sin acceso
                    var usuariosABorrar = db.Usuarios
                        .Where(u => u.NombreUsuario.ToLower() != "admin")
                        .ToList();
                    db.Usuarios.RemoveRange(usuariosABorrar);
                }

                db.SaveChanges();
                CargarEstadisticas();

                string mensajeUsuarios = reiniciarUsuarios
                    ? "Usuarios:   reiniciados (se conservó 'admin')"
                    : "Usuarios:   conservados sin cambios";

                MessageBox.Show(
                    $"✅  BASE DE DATOS REINICIADA\n\n" +
                    $"Clientes, vehículos, trabajos,\n" +
                    $"reservas y facturas eliminados.\n" +
                    $"{mensajeUsuarios}",
                    "Reinicio Completado", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al reiniciar:\n{ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ── INotifyPropertyChanged ────────────────────────────────────────────

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
