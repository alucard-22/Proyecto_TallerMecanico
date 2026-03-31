using Proyecto_taller.Data;
using Proyecto_taller.Models;
using Proyecto_taller.Views;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
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
                // Refrescar CanExecute de comandos que dependen de la selección
                CommandManager.InvalidateRequerySuggested();
            }
        }

        // ── Comandos ──────────────────────────────────────────────

        public ICommand CargarClientesCommand { get; }
        public ICommand AgregarClienteCommand { get; }
        public ICommand EditarClienteCommand { get; }
        public ICommand EliminarClienteCommand { get; }
        public ICommand VerHistorialCommand { get; }

        // ── Constructor ───────────────────────────────────────────

        public ClientesViewModel()
        {
            Clientes = new ObservableCollection<Cliente>();

            CargarClientesCommand = new RelayCommand(CargarClientes);
            AgregarClienteCommand = new RelayCommand(AgregarCliente);
            EditarClienteCommand = new RelayCommand(EditarCliente,
                                          () => ClienteSeleccionado != null);
            EliminarClienteCommand = new RelayCommand(EliminarCliente,
                                          () => ClienteSeleccionado != null);
            VerHistorialCommand = new RelayCommand(VerHistorial,
                                          () => ClienteSeleccionado != null);

            CargarClientes();

            // Suscribirse a cambios en la colección para auto-guardar
            Clientes.CollectionChanged += (s, e) =>
            {
                if (e.NewItems != null)
                    foreach (Cliente cliente in e.NewItems)
                        cliente.PropertyChanged += Cliente_PropertyChanged;
            };
        }

        // ── Auto-guardar al editar celda ──────────────────────────

        private void Cliente_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var cliente = sender as Cliente;
            if (cliente != null && cliente.ClienteID > 0)
                GuardarCambiosCliente(cliente);
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
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error al guardar: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        // ── Cargar clientes ───────────────────────────────────────

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

        // ── Agregar cliente ───────────────────────────────────────

        private void AgregarCliente()
        {
            var win = new NuevoClienteWindow();
            if (win.ShowDialog() == true)
                CargarClientes();
        }

        // ── Editar cliente ────────────────────────────────────────

        private void EditarCliente()
        {
            if (ClienteSeleccionado == null) return;

            // Recargar desde BD para obtener datos frescos
            using var db = new TallerDbContext();
            var clienteFresco = db.Clientes.Find(ClienteSeleccionado.ClienteID);

            if (clienteFresco == null)
            {
                MessageBox.Show("El cliente ya no existe en la base de datos.",
                    "No encontrado", MessageBoxButton.OK, MessageBoxImage.Warning);
                CargarClientes();
                return;
            }

            var win = new EditarClienteWindow(clienteFresco);
            if (win.ShowDialog() == true)
                CargarClientes();
        }

        // ── Ver historial del cliente ─────────────────────────────
        // Abre la ventana con vehículos y trabajos asociados al cliente.

        private void VerHistorial()
        {
            if (ClienteSeleccionado == null) return;

            var win = new HistorialClienteWindow(ClienteSeleccionado.ClienteID);
            win.ShowDialog();
        }

        // ── Eliminar cliente ──────────────────────────────────────

        private void EliminarCliente()
        {
            if (ClienteSeleccionado == null) return;

            var resultado = MessageBox.Show(
                $"¿Eliminar a '{ClienteSeleccionado.Nombre} {ClienteSeleccionado.Apellido}'?\n\n" +
                $"⚠️ También se eliminarán todos sus vehículos, trabajos y reservas.",
                "Confirmar Eliminación",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (resultado == MessageBoxResult.Yes)
            {
                try
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
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Error al eliminar el cliente:\n{ex.Message}",
                        "Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
        }

        // ── INotifyPropertyChanged ────────────────────────────────

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}