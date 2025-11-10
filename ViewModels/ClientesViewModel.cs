using Proyecto_taller.Data;
using Proyecto_taller.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;

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

        public ClientesViewModel()
        {
            Clientes = new ObservableCollection<Cliente>();

            CargarClientesCommand = new RelayCommand(CargarClientes);
            AgregarClienteCommand = new RelayCommand(AgregarCliente);
            EditarClienteCommand = new RelayCommand(EditarCliente, () => ClienteSeleccionado != null);
            EliminarClienteCommand = new RelayCommand(EliminarCliente, () => ClienteSeleccionado != null);

            CargarClientes();

            // ⭐ Suscribirse a cambios en cada cliente para auto-guardar
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

        // ⭐ Auto-guardar cuando se edita una celda
        private void Cliente_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var cliente = sender as Cliente;
            if (cliente != null && cliente.ClienteID > 0)
            {
                GuardarCambiosCliente(cliente);
            }
        }

        private void GuardarCambiosCliente(Cliente cliente)
        {
            try
            {
                using var db = new TallerDbContext();
                var clienteDb = db.Clientes.Find(cliente.ClienteID);

                if (clienteDb != null)
                {
                    clienteDb.Nombre = cliente.Nombre;
                    clienteDb.Apellido = cliente.Apellido;
                    clienteDb.Telefono = cliente.Telefono;
                    clienteDb.Correo = cliente.Correo;
                    clienteDb.Direccion = cliente.Direccion;

                    db.SaveChanges();
                }
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

        private void CargarClientes()
        {
            Clientes.Clear();
            using var db = new TallerDbContext();

            foreach (var cliente in db.Clientes.OrderBy(c => c.ClienteID).ToList())
            {
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
                Telefono = "0000000",
                Correo = "correo@ejemplo.com",
                Direccion = "Dirección",
                FechaRegistro = System.DateTime.Now
            };

            db.Clientes.Add(nuevo);
            db.SaveChanges();

            nuevo.PropertyChanged += Cliente_PropertyChanged;
            Clientes.Add(nuevo);

            System.Windows.MessageBox.Show(
                "✅ Cliente agregado.\n\n💡 Haz doble clic en cualquier celda para editarla.",
                "Cliente Agregado",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);
        }

        private void EditarCliente()
        {
            if (ClienteSeleccionado == null) return;

            System.Windows.MessageBox.Show(
                $"📝 Para editar al cliente:\n\n" +
                $"1. Haz DOBLE CLIC en la celda que quieres editar\n" +
                $"2. Escribe el nuevo valor\n" +
                $"3. Presiona ENTER o TAB para guardar\n\n" +
                $"Los cambios se guardan automáticamente.",
                "Cómo Editar",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);
        }

        private void EliminarCliente()
        {
            if (ClienteSeleccionado == null) return;

            var resultado = System.Windows.MessageBox.Show(
                $"¿Eliminar a '{ClienteSeleccionado.Nombre} {ClienteSeleccionado.Apellido}'?",
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

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}