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
        // ── Información del taller ────────────────────────────────
        private string _nombreTaller;
        private string _direccionTaller;
        private string _telefonoTaller;
        private string _emailTaller;
        private string _nitTaller;

        // ── Facturación ───────────────────────────────────────────
        private decimal _descuentoMaximo;
        private bool _solicitarNIT;

        // ── Base de datos ─────────────────────────────────────────
        private string _connectionString;
        private DateTime _ultimoRespaldo;
        private int _totalRegistros;

        // ── Servicios ─────────────────────────────────────────────
        private Servicio _servicioSeleccionado;

        public ObservableCollection<Servicio> Servicios { get; set; } = new();

        // ─────────────────────────────────────────────────────────
        //  PROPIEDADES
        // ─────────────────────────────────────────────────────────

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
        public Servicio ServicioSeleccionado
        {
            get => _servicioSeleccionado;
            set
            {
                _servicioSeleccionado = value;
                OnPropertyChanged();
                CommandManager.InvalidateRequerySuggested();
            }
        }

        // ─────────────────────────────────────────────────────────
        //  COMANDOS
        // ─────────────────────────────────────────────────────────

        public ICommand GuardarInformacionCommand { get; }
        public ICommand GuardarFacturacionCommand { get; }
        public ICommand ProbarConexionCommand { get; }
        public ICommand RespaldarBDCommand { get; }
        public ICommand AgregarServicioCommand { get; }
        public ICommand EditarServicioCommand { get; }
        public ICommand EliminarServicioCommand { get; }
        public ICommand LimpiarLogsCommand { get; }
        public ICommand VerEstadisticasCommand { get; }
        public ICommand ReiniciarBDCommand { get; }

        public ConfiguracionViewModel()
        {
            GuardarInformacionCommand = new RelayCommand(GuardarInformacion);
            GuardarFacturacionCommand = new RelayCommand(GuardarFacturacion);
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

        // ─────────────────────────────────────────────────────────
        //  CARGA INICIAL
        // ─────────────────────────────────────────────────────────

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
                ConnectionString = config.ConnectionString;
                UltimoRespaldo = config.UltimoRespaldo;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar configuración:\n{ex.Message}\n\nSe usarán valores por defecto.",
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
                TotalRegistros = db.Clientes.Count() +
                                 db.Vehiculos.Count() +
                                 db.Trabajos.Count() +
                                 db.Facturas.Count();
            }
            catch { TotalRegistros = 0; }
        }

        // ─────────────────────────────────────────────────────────
        //  INFORMACIÓN DEL TALLER
        // ─────────────────────────────────────────────────────────

        private void GuardarInformacion()
        {
            try
            {
                var config = ConfiguracionHelper.CargarConfiguracion();
                config.NombreTaller = NombreTaller;
                config.DireccionTaller = DireccionTaller;
                config.TelefonoTaller = TelefonoTaller;
                config.EmailTaller = EmailTaller;
                config.NITTaller = NITTaller;
                ConfiguracionHelper.GuardarConfiguracion(config);

                MessageBox.Show(
                    $"✅  INFORMACIÓN DEL TALLER GUARDADA\n\n" +
                    $"Nombre:    {NombreTaller}\n" +
                    $"Dirección: {DireccionTaller}\n" +
                    $"Teléfono:  {TelefonoTaller}\n" +
                    $"Email:     {EmailTaller}\n" +
                    $"NIT:       {NITTaller}",
                    "Guardado", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al guardar:\n{ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ─────────────────────────────────────────────────────────
        //  FACTURACIÓN
        // ─────────────────────────────────────────────────────────

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
                MessageBox.Show($"Error al guardar:\n{ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ─────────────────────────────────────────────────────────
        //  CONEXIÓN A BD
        //  Probar con la connection string que el usuario escribió,
        //  no la hardcodeada del contexto.
        // ─────────────────────────────────────────────────────────

        private void ProbarConexion()
        {
            try
            {
                // Probamos abriendo una conexión ADO.NET directa con la cadena
                // que el usuario ve en pantalla, no la del contexto.
                using var conn = new SqlConnection(ConnectionString);
                conn.Open();
                conn.Close();

                MessageBox.Show(
                    "✅  Conexión exitosa con la base de datos.\n\n" +
                    $"Servidor: {conn.DataSource}\n" +
                    $"BD:       {conn.Database}",
                    "Conexión OK", MessageBoxButton.OK, MessageBoxImage.Information);
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

        // ─────────────────────────────────────────────────────────
        //  RESPALDO REAL
        //  Ejecuta BACKUP DATABASE de SQL Server y guarda el .bak
        //  en Documentos/TallerElChoco_Backups/
        // ─────────────────────────────────────────────────────────

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
                // Carpeta destino
                string carpeta = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "TallerElChoco_Backups");
                Directory.CreateDirectory(carpeta);

                string fechaStr = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string nombreArchivo = $"TallerMecanico_{fechaStr}.bak";
                string rutaBak = Path.Combine(carpeta, nombreArchivo);

                // SQL Server necesita la ruta del servidor (que en localhost == ruta local)
                string sql = $"BACKUP DATABASE [TallerMecanico] TO DISK = N'{rutaBak}' " +
                             $"WITH NOFORMAT, INIT, NAME = N'TallerMecanico-Backup', " +
                             $"SKIP, NOREWIND, NOUNLOAD, STATS = 10";

                using var conn = new SqlConnection(ConnectionString);
                using var command = new SqlCommand(sql, conn);
                command.CommandTimeout = 120; // 2 minutos para BD grandes

                conn.Open();
                command.ExecuteNonQuery();
                conn.Close();

                // Actualizar fecha en config
                UltimoRespaldo = DateTime.Now;
                var config = ConfiguracionHelper.CargarConfiguracion();
                config.UltimoRespaldo = UltimoRespaldo;
                ConfiguracionHelper.GuardarConfiguracion(config);

                var abrir = MessageBox.Show(
                    $"✅  RESPALDO CREADO EXITOSAMENTE\n\n" +
                    $"📁  {rutaBak}\n\n" +
                    $"¿Abrir la carpeta de respaldos?",
                    "Respaldo OK", MessageBoxButton.YesNo, MessageBoxImage.Information);

                if (abrir == MessageBoxResult.Yes)
                    Process.Start(new ProcessStartInfo(carpeta) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"❌  Error al crear el respaldo:\n\n{ex.Message}\n\n" +
                    $"Verifica que:\n" +
                    $"• SQL Server tiene permisos de escritura en la carpeta destino\n" +
                    $"• La conexión a la base de datos es correcta\n" +
                    $"• El servicio SQL Server está ejecutándose",
                    "Error de Respaldo", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ─────────────────────────────────────────────────────────
        //  SERVICIOS — CRUD completo
        // ─────────────────────────────────────────────────────────

        private void AgregarServicio()
        {
            var win = new EditarServicioWindow(); // constructor sin parámetros = nuevo
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
                $"⚠️ Si este servicio está asociado a trabajos existentes, la eliminación fallará.",
                "Confirmar Eliminación", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (r != MessageBoxResult.Yes) return;

            try
            {
                using var db = new TallerDbContext();

                // Verificar si tiene trabajos asociados
                bool tieneTrabajos = db.Trabajos_Servicios
                    .Any(ts => ts.ServicioID == ServicioSeleccionado.ServicioID);

                if (tieneTrabajos)
                {
                    MessageBox.Show(
                        $"No se puede eliminar '{ServicioSeleccionado.Nombre}' porque está " +
                        $"asociado a uno o más trabajos.\n\nPuedes cambiar su nombre o costo en su lugar.",
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
                MessageBox.Show($"Error al eliminar:\n{ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ─────────────────────────────────────────────────────────
        //  LIMPIAR LOGS — limpia los archivos .log reales
        //  (si no usas archivos de log, limpia la carpeta temp de la app)
        // ─────────────────────────────────────────────────────────

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
                    ConfiguracionHelper.ObtenerRutaConfiguracion());

                if (!Directory.Exists(carpetaApp))
                { MessageBox.Show("No hay carpeta de logs.", "Info", MessageBoxButton.OK, MessageBoxImage.Information); return; }

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
                MessageBox.Show($"Error al limpiar logs:\n{ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ─────────────────────────────────────────────────────────
        //  ESTADÍSTICAS
        // ─────────────────────────────────────────────────────────

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
                    $"{'━',38}\n\n" +
                    $"👥  Clientes:          {db.Clientes.Count()}\n" +
                    $"🚗  Vehículos:         {db.Vehiculos.Count()}\n\n" +
                    $"🔧  Trabajos:          {db.Trabajos.Count()}\n" +
                    $"    ⏳ Pendientes:     {db.Trabajos.Count(t => t.Estado == "Pendiente")}\n" +
                    $"    🔄 En Progreso:    {db.Trabajos.Count(t => t.Estado == "En Progreso")}\n" +
                    $"    ✅ Finalizados:    {db.Trabajos.Count(t => t.Estado == "Finalizado")}\n\n" +
                    $"📅  Reservas:          {db.Reservas.Count()}\n" +
                    $"    ⏳ Pendientes:     {db.Reservas.Count(r => r.Estado == "Pendiente")}\n\n" +
                    $"📄  Facturas:          {db.Facturas.Count()}\n" +
                    $"    💰 Total facturado: Bs. {totalFacturado:N2}\n\n" +
                    $"⚙️   Servicios:         {db.Servicios.Count()}\n" +
                    $"📦  Repuestos:         {db.Repuestos.Count()}\n" +
                    $"    ⚠️ Stock bajo:     {db.Repuestos.Count(r => r.StockActual <= r.StockMinimo)}\n\n" +
                    $"{'━',38}\n" +
                    $"Total registros: {TotalRegistros}";

                MessageBox.Show(stats, "Estadísticas del Sistema",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar estadísticas:\n{ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ─────────────────────────────────────────────────────────
        //  REINICIAR BD
        // ─────────────────────────────────────────────────────────

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

            try
            {
                using var db = new TallerDbContext();

                db.Pagos.RemoveRange(db.Pagos);
                db.Trabajos_Repuestos.RemoveRange(db.Trabajos_Repuestos);
                db.Trabajos_Servicios.RemoveRange(db.Trabajos_Servicios);
                db.Facturas.RemoveRange(db.Facturas);
                db.Reservas.RemoveRange(db.Reservas);
                db.Trabajos.RemoveRange(db.Trabajos);
                db.Vehiculos.RemoveRange(db.Vehiculos);
                db.Clientes.RemoveRange(db.Clientes);
                db.SaveChanges();

                CargarEstadisticas();

                MessageBox.Show("Base de datos reiniciada. Todos los datos han sido eliminados.",
                    "Reinicio Completado", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al reiniciar:\n{ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ─────────────────────────────────────────────────────────
        //  INotifyPropertyChanged
        // ─────────────────────────────────────────────────────────

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
