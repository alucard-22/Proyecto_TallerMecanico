using Proyecto_taller.Data;
using Proyecto_taller.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;

namespace Proyecto_taller.ViewModels
{
    public class ClientesViewModel : INotifyPropertyChanged
    {
        private Cliente _clienteSeleccionado;

        public ObservableCollection<Cliente> Clientes { get; set; }

        public Cliente ClienteSeleccionado
        {
            get => _clienteSeleccionado;
            set
            {
                _clienteSeleccionado = value;
                OnPropertyChanged();
            }
        }

        public ICommand CargarClientesCommand { get; }
        public ICommand AgregarClienteCommand { get; }
        public ICommand EditarClienteCommand { get; }
        public ICommand EliminarClienteCommand { get; }
        public ICommand GuardarCambiosCommand { get; } // ⭐ NUEVO

        public ClientesViewModel()
        {
            Clientes = new ObservableCollection<Cliente>();
            CargarClientes();

            CargarClientesCommand = new RelayCommand(CargarClientes);
            AgregarClienteCommand = new RelayCommand(AgregarCliente);
            EditarClienteCommand = new RelayCommand(EditarCliente, () => ClienteSeleccionado != null);
            EliminarClienteCommand = new RelayCommand(EliminarCliente, () => ClienteSeleccionado != null);
            GuardarCambiosCommand = new RelayCommand(GuardarCambios); // ⭐ NUEVO

            // ⭐ Suscribirse a cambios en cada cliente
            Clientes.CollectionChanged += (s, e) =>
            {
                if (e.NewItems != null)
                {
                    foreach (Cliente cliente in e.NewItems)
                    {
                        cliente.PropertyChanged += Cliente_PropertyChanged;
                    }
                }
            };
        }

        // ⭐ NUEVO: Auto-guardar cuando se edita una celda
        private void Cliente_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var cliente = sender as Cliente;
            if (cliente != null)
            {
                GuardarCambioCliente(cliente);
            }
        }

        private void GuardarCambioCliente(Cliente cliente)
        {
            try
            {
                using var db = new TallerDbContext();

                // Buscar el cliente en la BD
                var clienteDb = db.Clientes.Find(cliente.ClienteID);

                if (clienteDb != null)
                {
                    // Actualizar los datos
                    clienteDb.Nombre = cliente.Nombre;
                    clienteDb.Apellido = cliente.Apellido;
                    clienteDb.Telefono = cliente.Telefono;
                    clienteDb.Correo = cliente.Correo;
                    clienteDb.Direccion = cliente.Direccion;

                    db.SaveChanges();

                    System.Diagnostics.Debug.WriteLine($"✅ Cliente {cliente.ClienteID} actualizado");
                }
            }
            catch (System.Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Error al guardar cambios: {ex.Message}",
                    "Error",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
        }

        private void CargarClientes()
        {
            Clientes.Clear();
            using var db = new TallerDbContext();

            foreach (var cliente in db.Clientes.ToList())
            {
                // Suscribirse a cambios de propiedad
                cliente.PropertyChanged += Cliente_PropertyChanged;
                Clientes.Add(cliente);
            }
        }

        private void AgregarCliente()
        {
            using var db = new TallerDbContext();
            var nuevo = new Cliente
            {
                Nombre = "Nuevo",
                Apellido = "Cliente",
                Telefono = "00000000",
                Correo = "cliente@email.com",
                Direccion = "Dirección",
                FechaRegistro = System.DateTime.Now
            };

            db.Clientes.Add(nuevo);
            db.SaveChanges();

            // Suscribirse a cambios
            nuevo.PropertyChanged += Cliente_PropertyChanged;
            Clientes.Add(nuevo);
        }

        private void EditarCliente()
        {
            if (ClienteSeleccionado == null) return;

            System.Windows.MessageBox.Show(
                $"Editando cliente: {ClienteSeleccionado.Nombre} {ClienteSeleccionado.Apellido}\n\n" +
                $"💡 Tip: Haz doble clic en cualquier celda para editarla directamente",
                "Editar Cliente",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);
        }

        private void EliminarCliente()
        {
            if (ClienteSeleccionado == null) return;

            var resultado = System.Windows.MessageBox.Show(
                $"¿Está seguro de eliminar al cliente '{ClienteSeleccionado.Nombre} {ClienteSeleccionado.Apellido}'?",
                "Confirmar Eliminación",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning);

            if (resultado == System.Windows.MessageBoxResult.Yes)
            {
                using var db = new TallerDbContext();
                var cliente = db.Clientes.Find(ClienteSeleccionado.ClienteID);

                if (cliente != null)
                {
                    db.Clientes.Remove(cliente);
                    db.SaveChanges();
                    Clientes.Remove(ClienteSeleccionado);
                }
            }
        }

        // ⭐ NUEVO: Guardar todos los cambios pendientes
        private void GuardarCambios()
        {
            try
            {
                using var db = new TallerDbContext();

                foreach (var cliente in Clientes)
                {
                    var clienteDb = db.Clientes.Find(cliente.ClienteID);
                    if (clienteDb != null)
                    {
                        db.Entry(clienteDb).CurrentValues.SetValues(cliente);
                    }
                }

                db.SaveChanges();

                System.Windows.MessageBox.Show(
                    "✅ Todos los cambios guardados exitosamente",
                    "Guardado Exitoso",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);
            }
            catch (System.Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Error al guardar: {ex.Message}",
                    "Error",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}