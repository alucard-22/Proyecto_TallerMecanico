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
using Microsoft.EntityFrameworkCore;

namespace Proyecto_taller.ViewModels
{
    public class InicioViewModel : INotifyPropertyChanged
    {
        // Estadísticas
        private int _trabajosActivos;
        private int _totalClientes;
        private int _totalVehiculos;
        private decimal _ventasMes;

        // Formulario de Trabajo Rápido - Cliente
        private string _nuevoClienteNombre = "";
        private string _nuevoClienteApellido = "";
        private string _nuevoClienteTelefono = "";
        private string _nuevoClienteEmail = "";

        // Formulario de Trabajo Rápido - Vehículo
        private string _nuevoVehiculoMarca = "";
        private string _nuevoVehiculoModelo = "";
        private string _nuevoVehiculoPlaca = "";
        private string _nuevoVehiculoAnio = "";

        // Formulario de Trabajo Rápido - Trabajo
        private string _nuevoTrabajoTipo = "Mecánica";
        private string _nuevoTrabajoDescripcion = "";
        private string _nuevoTrabajoPrecio = "0";

        public ObservableCollection<string> ActividadReciente { get; set; }
        public ObservableCollection<Trabajo> TrabajosPendientes { get; set; }
        public ObservableCollection<Repuesto> RepuestosStockBajo { get; set; }

        // Propiedades de Estadísticas
        public int TrabajosActivos
        {
            get => _trabajosActivos;
            set { _trabajosActivos = value; OnPropertyChanged(); }
        }

        public int TotalClientes
        {
            get => _totalClientes;
            set { _totalClientes = value; OnPropertyChanged(); }
        }

        public int TotalVehiculos
        {
            get => _totalVehiculos;
            set { _totalVehiculos = value; OnPropertyChanged(); }
        }

        public decimal VentasMes
        {
            get => _ventasMes;
            set { _ventasMes = value; OnPropertyChanged(); }
        }

        // Propiedades del Formulario - Cliente
        public string NuevoClienteNombre
        {
            get => _nuevoClienteNombre;
            set { _nuevoClienteNombre = value; OnPropertyChanged(); }
        }

        public string NuevoClienteApellido
        {
            get => _nuevoClienteApellido;
            set { _nuevoClienteApellido = value; OnPropertyChanged(); }
        }

        public string NuevoClienteTelefono
        {
            get => _nuevoClienteTelefono;
            set { _nuevoClienteTelefono = value; OnPropertyChanged(); }
        }

        public string NuevoClienteEmail
        {
            get => _nuevoClienteEmail;
            set { _nuevoClienteEmail = value; OnPropertyChanged(); }
        }

        // Propiedades del Formulario - Vehículo
        public string NuevoVehiculoMarca
        {
            get => _nuevoVehiculoMarca;
            set { _nuevoVehiculoMarca = value; OnPropertyChanged(); }
        }

        public string NuevoVehiculoModelo
        {
            get => _nuevoVehiculoModelo;
            set { _nuevoVehiculoModelo = value; OnPropertyChanged(); }
        }

        public string NuevoVehiculoPlaca
        {
            get => _nuevoVehiculoPlaca;
            set { _nuevoVehiculoPlaca = value; OnPropertyChanged(); }
        }

        public string NuevoVehiculoAnio
        {
            get => _nuevoVehiculoAnio;
            set { _nuevoVehiculoAnio = value; OnPropertyChanged(); }
        }

        // Propiedades del Formulario - Trabajo
        public string NuevoTrabajoTipo
        {
            get => _nuevoTrabajoTipo;
            set { _nuevoTrabajoTipo = value; OnPropertyChanged(); }
        }

        public string NuevoTrabajoDescripcion
        {
            get => _nuevoTrabajoDescripcion;
            set { _nuevoTrabajoDescripcion = value; OnPropertyChanged(); }
        }

        public string NuevoTrabajoPrecio
        {
            get => _nuevoTrabajoPrecio;
            set { _nuevoTrabajoPrecio = value; OnPropertyChanged(); }
        }

        public ICommand RegistrarTrabajoRapidoCommand { get; }

        public InicioViewModel()
        {
            ActividadReciente = new ObservableCollection<string>();
            TrabajosPendientes = new ObservableCollection<Trabajo>();
            RepuestosStockBajo = new ObservableCollection<Repuesto>();

            RegistrarTrabajoRapidoCommand = new RelayCommand(RegistrarTrabajoRapido);

            CargarEstadisticas();
            CargarActividadReciente();
            CargarTrabajosPendientes();
            CargarRepuestosStockBajo();
        }

        private void CargarEstadisticas()
        {
            using var db = new TallerDbContext();

            TrabajosActivos = db.Trabajos.Count(t => t.Estado == "Pendiente" || t.Estado == "En Progreso");
            TotalClientes = db.Clientes.Count();
            TotalVehiculos = db.Vehiculos.Count();

            var primerDiaMes = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            VentasMes = db.Facturas
                .Where(f => f.Estado == "Pagada" && f.FechaEmision >= primerDiaMes)
                .Sum(f => (decimal?)f.Total) ?? 0;
        }

        private void CargarActividadReciente()
        {
            using var db = new TallerDbContext();

            ActividadReciente.Clear();

            var ultimosTrabajos = db.Trabajos
                .Include(t => t.Vehiculo)
                    .ThenInclude(v => v.Cliente)
                .OrderByDescending(t => t.FechaIngreso)
                .Take(5)
                .ToList();

            foreach (var trabajo in ultimosTrabajos)
            {
                ActividadReciente.Add(
                    $"• Trabajo #{trabajo.TrabajoID} - {trabajo.Vehiculo?.Cliente?.Nombre} - " +
                    $"{trabajo.Vehiculo?.Marca} {trabajo.Vehiculo?.Modelo} ({trabajo.Estado})");
            }

            if (ActividadReciente.Count == 0)
            {
                ActividadReciente.Add("• No hay actividad reciente");
            }
        }

        private void CargarTrabajosPendientes()
        {
            using var db = new TallerDbContext();

            TrabajosPendientes.Clear();

            var pendientes = db.Trabajos
                .Include(t => t.Vehiculo)
                    .ThenInclude(v => v.Cliente)
                .Where(t => t.Estado == "Pendiente" || t.Estado == "En Progreso")
                .OrderBy(t => t.FechaIngreso)
                .Take(5)
                .ToList();

            foreach (var trabajo in pendientes)
            {
                TrabajosPendientes.Add(trabajo);
            }
        }

        private void CargarRepuestosStockBajo()
        {
            using var db = new TallerDbContext();

            RepuestosStockBajo.Clear();

            var stockBajo = db.Repuestos
                .Where(r => r.StockActual <= r.StockMinimo)
                .OrderBy(r => r.StockActual)
                .Take(5)
                .ToList();

            foreach (var repuesto in stockBajo)
            {
                RepuestosStockBajo.Add(repuesto);
            }

            if (RepuestosStockBajo.Count == 0)
            {
                var sinProblemas = new Repuesto { Nombre = "✅ Todos los repuestos tienen stock adecuado", StockActual = 0 };
                RepuestosStockBajo.Add(sinProblemas);
            }
        }

        private void RegistrarTrabajoRapido()
        {
            // Validaciones
            if (string.IsNullOrWhiteSpace(NuevoClienteNombre) ||
                string.IsNullOrWhiteSpace(NuevoClienteApellido))
            {
                MessageBox.Show(
                    "Por favor complete el nombre y apellido del cliente.",
                    "Datos Incompletos",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(NuevoVehiculoMarca) ||
                string.IsNullOrWhiteSpace(NuevoVehiculoPlaca))
            {
                MessageBox.Show(
                    "Por favor complete la marca y placa del vehículo.",
                    "Datos Incompletos",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(NuevoTrabajoDescripcion))
            {
                MessageBox.Show(
                    "Por favor describa el problema o trabajo a realizar.",
                    "Datos Incompletos",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            try
            {
                using var db = new TallerDbContext();

                // 1. Crear o buscar el cliente
                var clienteExistente = db.Clientes
                    .FirstOrDefault(c =>
                        c.Nombre.ToLower() == NuevoClienteNombre.ToLower() &&
                        c.Apellido.ToLower() == NuevoClienteApellido.ToLower());

                Cliente cliente;
                if (clienteExistente != null)
                {
                    cliente = clienteExistente;
                }
                else
                {
                    cliente = new Cliente
                    {
                        Nombre = NuevoClienteNombre,
                        Apellido = NuevoClienteApellido,
                        Telefono = NuevoClienteTelefono,
                        Correo = NuevoClienteEmail,
                        Direccion = "Sin dirección",
                        FechaRegistro = DateTime.Now
                    };
                    db.Clientes.Add(cliente);
                    db.SaveChanges();
                }

                // 2. Crear o buscar el vehículo
                var vehiculoExistente = db.Vehiculos
                    .FirstOrDefault(v => v.Placa.ToLower() == NuevoVehiculoPlaca.ToLower());

                Vehiculo vehiculo;
                if (vehiculoExistente != null)
                {
                    vehiculo = vehiculoExistente;
                }
                else
                {
                    int anio = 0;
                    int.TryParse(NuevoVehiculoAnio, out anio);

                    vehiculo = new Vehiculo
                    {
                        ClienteID = cliente.ClienteID,
                        Marca = NuevoVehiculoMarca,
                        Modelo = NuevoVehiculoModelo,
                        Placa = NuevoVehiculoPlaca,
                        Anio = anio > 0 ? anio : null
                    };
                    db.Vehiculos.Add(vehiculo);
                    db.SaveChanges();
                }

                // 3. Crear el trabajo
                decimal precioEstimado = 0;
                decimal.TryParse(NuevoTrabajoPrecio, out precioEstimado);

                var trabajo = new Trabajo
                {
                    VehiculoID = vehiculo.VehiculoID,
                    FechaIngreso = DateTime.Now,
                    Descripcion = NuevoTrabajoDescripcion,
                    Estado = "Pendiente",
                    TipoTrabajo = NuevoTrabajoTipo,
                    PrecioEstimado = precioEstimado > 0 ? precioEstimado : null
                };

                db.Trabajos.Add(trabajo);
                db.SaveChanges();

                MessageBox.Show(
                    $"✅ Trabajo registrado exitosamente!\n\n" +
                    $"Trabajo ID: {trabajo.TrabajoID}\n" +
                    $"Cliente: {cliente.Nombre} {cliente.Apellido}\n" +
                    $"Vehículo: {vehiculo.Marca} {vehiculo.Modelo} - {vehiculo.Placa}\n" +
                    $"Tipo: {trabajo.TipoTrabajo}\n" +
                    $"Estado: {trabajo.Estado}",
                    "Registro Exitoso",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                // Limpiar formulario
                LimpiarFormulario();

                // Actualizar estadísticas
                CargarEstadisticas();
                CargarActividadReciente();
                CargarTrabajosPendientes();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error al registrar el trabajo:\n{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void LimpiarFormulario()
        {
            // Limpiar datos del cliente
            NuevoClienteNombre = "";
            NuevoClienteApellido = "";
            NuevoClienteTelefono = "";
            NuevoClienteEmail = "";

            // Limpiar datos del vehículo
            NuevoVehiculoMarca = "";
            NuevoVehiculoModelo = "";
            NuevoVehiculoPlaca = "";
            NuevoVehiculoAnio = "";

            // Limpiar datos del trabajo
            NuevoTrabajoTipo = "Mecánica";
            NuevoTrabajoDescripcion = "";
            NuevoTrabajoPrecio = "0";
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
