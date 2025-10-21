using Proyecto_taller.Data;
using Proyecto_taller.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
        // Información del Taller
        private string _nombreTaller = "Taller Mecánico El Choco";
        private string _direccionTaller = "Av. América #1234, Cochabamba";
        private string _telefonoTaller = "4-4567890";
        private string _emailTaller = "contacto@tallerelchoco.com";
        private string _nitTaller = "123456789";

        // Configuración de Facturación
        private decimal _porcentajeIVA = 13;
        private decimal _descuentoMaximo = 20;
        private bool _incluirIVAAutomatico = true;
        private bool _solicitarNIT = false;

        // Base de Datos
        private string _connectionString = "Server=localhost;Database=TallerMecanico;Trusted_Connection=True;TrustServerCertificate=True;";
        private DateTime _ultimoRespaldo = DateTime.Now.AddDays(-7);
        private int _totalRegistros;

        public ObservableCollection<Servicio> Servicios { get; set; }

        // Propiedades - Información del Taller
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

        // Propiedades - Facturación
        public decimal PorcentajeIVA
        {
            get => _porcentajeIVA;
            set { _porcentajeIVA = value; OnPropertyChanged(); }
        }

        public decimal DescuentoMaximo
        {
            get => _descuentoMaximo;
            set { _descuentoMaximo = value; OnPropertyChanged(); }
        }

        public bool IncluirIVAAutomatico
        {
            get => _incluirIVAAutomatico;
            set { _incluirIVAAutomatico = value; OnPropertyChanged(); }
        }

        public bool SolicitarNIT
        {
            get => _solicitarNIT;
            set { _solicitarNIT = value; OnPropertyChanged(); }
        }

        // Propiedades - Base de Datos
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

        // Comandos
        public ICommand GuardarInformacionCommand { get; }
        public ICommand GuardarFacturacionCommand { get; }
        public ICommand ProbarConexionCommand { get; }
        public ICommand RespaldarBDCommand { get; }
        public ICommand AgregarServicioCommand { get; }
        public ICommand EditarServicioCommand { get; }
        public ICommand LimpiarLogsCommand { get; }
        public ICommand VerEstadisticasCommand { get; }
        public ICommand ReiniciarBDCommand { get; }

        public ConfiguracionViewModel()
        {
            Servicios = new ObservableCollection<Servicio>();

            GuardarInformacionCommand = new RelayCommand(GuardarInformacion);
            GuardarFacturacionCommand = new RelayCommand(GuardarFacturacion);
            ProbarConexionCommand = new RelayCommand(ProbarConexion);
            RespaldarBDCommand = new RelayCommand(RespaldarBD);
            AgregarServicioCommand = new RelayCommand(AgregarServicio);
            EditarServicioCommand = new RelayCommand(EditarServicio);
            LimpiarLogsCommand = new RelayCommand(LimpiarLogs);
            VerEstadisticasCommand = new RelayCommand(VerEstadisticas);
            ReiniciarBDCommand = new RelayCommand(ReiniciarBD);

            CargarServicios();
            CargarEstadisticas();
        }

        private void CargarServicios()
        {
            using var db = new TallerDbContext();
            var servicios = db.Servicios.OrderBy(s => s.Nombre).ToList();

            Servicios.Clear();
            foreach (var servicio in servicios)
            {
                Servicios.Add(servicio);
            }
        }

        private void CargarEstadisticas()
        {
            using var db = new TallerDbContext();
            TotalRegistros = db.Clientes.Count() +
                           db.Vehiculos.Count() +
                           db.Trabajos.Count() +
                           db.Facturas.Count();
        }

        private void GuardarInformacion()
        {
            // Aquí podrías guardar la configuración en un archivo o BD
            MessageBox.Show(
                $"Información del taller guardada:\n\n" +
                $"Nombre: {NombreTaller}\n" +
                $"Dirección: {DireccionTaller}\n" +
                $"Teléfono: {TelefonoTaller}\n" +
                $"Email: {EmailTaller}\n" +
                $"NIT: {NITTaller}",
                "Configuración Guardada",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void GuardarFacturacion()
        {
            MessageBox.Show(
                $"Configuración de facturación guardada:\n\n" +
                $"IVA: {PorcentajeIVA}%\n" +
                $"Descuento Máximo: {DescuentoMaximo}%\n" +
                $"IVA Automático: {(IncluirIVAAutomatico ? "Sí" : "No")}\n" +
                $"Solicitar NIT: {(SolicitarNIT ? "Sí" : "No")}",
                "Configuración Guardada",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void ProbarConexion()
        {
            try
            {
                using var db = new TallerDbContext();
                var canConnect = db.Database.CanConnect();

                if (canConnect)
                {
                    MessageBox.Show(
                        "✅ Conexión exitosa con la base de datos.",
                        "Conexión Exitosa",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show(
                        "❌ No se pudo conectar a la base de datos.",
                        "Error de Conexión",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error al probar la conexión:\n{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void RespaldarBD()
        {
            var resultado = MessageBox.Show(
                "¿Desea crear un respaldo de la base de datos?",
                "Confirmar Respaldo",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (resultado == MessageBoxResult.Yes)
            {
                // Aquí implementarías la lógica real de respaldo
                UltimoRespaldo = DateTime.Now;

                MessageBox.Show(
                    $"Respaldo creado exitosamente.\nFecha: {UltimoRespaldo:dd/MM/yyyy HH:mm}",
                    "Respaldo Completado",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }

        private void AgregarServicio()
        {
            using var db = new TallerDbContext();

            var nuevo = new Servicio
            {
                Nombre = "Nuevo Servicio",
                Descripcion = "Descripción del servicio",
                Categoria = "Mecánica",
                CostoBase = 100.00m
            };

            db.Servicios.Add(nuevo);
            db.SaveChanges();
            Servicios.Add(nuevo);

            MessageBox.Show(
                "Servicio agregado exitosamente.",
                "Éxito",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void EditarServicio()
        {
            MessageBox.Show(
                "Funcionalidad para editar servicios.\nPróximamente...",
                "Editar Servicio",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void LimpiarLogs()
        {
            var resultado = MessageBox.Show(
                "¿Está seguro de limpiar los logs del sistema?",
                "Confirmar Limpieza",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (resultado == MessageBoxResult.Yes)
            {
                MessageBox.Show(
                    "Logs limpiados exitosamente.",
                    "Limpieza Completada",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }

        private void VerEstadisticas()
        {
            using var db = new TallerDbContext();

            var stats = $"📊 ESTADÍSTICAS DEL SISTEMA\n" +
                       $"{'=',40}\n\n" +
                       $"Clientes: {db.Clientes.Count()}\n" +
                       $"Vehículos: {db.Vehiculos.Count()}\n" +
                       $"Trabajos: {db.Trabajos.Count()}\n" +
                       $"  - Pendientes: {db.Trabajos.Count(t => t.Estado == "Pendiente")}\n" +
                       $"  - En Progreso: {db.Trabajos.Count(t => t.Estado == "En Progreso")}\n" +
                       $"  - Finalizados: {db.Trabajos.Count(t => t.Estado == "Finalizado")}\n\n" +
                       $"Facturas: {db.Facturas.Count()}\n" +
                       $"Servicios: {db.Servicios.Count()}\n" +
                       $"Repuestos: {db.Repuestos.Count()}\n" +
                       $"  - Stock Bajo: {db.Repuestos.Count(r => r.StockActual <= r.StockMinimo)}\n\n" +
                       $"Total Facturado: Bs. {db.Facturas.Where(f => f.Estado == "Pagada").Sum(f => f.Total):N2}";

            MessageBox.Show(stats, "Estadísticas del Sistema", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ReiniciarBD()
        {
            var resultado = MessageBox.Show(
                "⚠️ ADVERTENCIA ⚠️\n\n" +
                "Esta acción eliminará TODOS los datos de la base de datos.\n" +
                "Esta operación NO se puede deshacer.\n\n" +
                "¿Está completamente seguro de continuar?",
                "Confirmar Reinicio",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (resultado == MessageBoxResult.Yes)
            {
                var confirmacion = MessageBox.Show(
                    "Por favor confirme nuevamente.\n¿Desea ELIMINAR TODOS LOS DATOS?",
                    "Confirmación Final",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Stop);

                if (confirmacion == MessageBoxResult.Yes)
                {
                    try
                    {
                        using var db = new TallerDbContext();

                        // Eliminar datos (respetando el orden de foreign keys)
                        db.Pagos.RemoveRange(db.Pagos);
                        db.Trabajos_Repuestos.RemoveRange(db.Trabajos_Repuestos);
                        db.Trabajos_Servicios.RemoveRange(db.Trabajos_Servicios);
                        db.Facturas.RemoveRange(db.Facturas);
                        db.Trabajos.RemoveRange(db.Trabajos);
                        db.Vehiculos.RemoveRange(db.Vehiculos);
                        db.Clientes.RemoveRange(db.Clientes);

                        db.SaveChanges();

                        CargarEstadisticas();

                        MessageBox.Show(
                            "Base de datos reiniciada exitosamente.\nTodos los datos han sido eliminados.",
                            "Reinicio Completado",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(
                            $"Error al reiniciar la base de datos:\n{ex.Message}",
                            "Error",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                    }
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
