using Proyecto_taller.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Proyecto_taller.Models;

namespace Proyecto_taller.ViewModels
{
    public class ClientesViewModel : INotifyPropertyChanged
    {
        private Cliente _clienteSeleccionado;

        public ObservableCollection<Cliente> Clientes { get; set; }
        public Cliente ClienteSeleccionado
        {
            get => _clienteSeleccionado;
            set { _clienteSeleccionado = value; OnPropertyChanged(); }
        }

        public ICommand AgregarCommand { get; }
        public ICommand EliminarCommand { get; }

        public ClientesViewModel()
        {
            Clientes = new ObservableCollection<Cliente>();
            CargarClientes();

            AgregarCommand = new RelayCommand(AgregarCliente);
            EliminarCommand = new RelayCommand(EliminarCliente, () => ClienteSeleccionado != null);
        }

        private void CargarClientes()
        {
            using var db = new TallerDbContext();
            foreach (var cliente in db.Clientes.ToList())
                Clientes.Add(cliente);
        }

        private void AgregarCliente()
        {
            using var db = new TallerDbContext();
            var nuevo = new Cliente { Nombre = "Nuevo", Apellido = "Cliente" };
            db.Clientes.Add(nuevo);
            db.SaveChanges();
            Clientes.Add(nuevo);
        }

        private void EliminarCliente()
        {
            if (ClienteSeleccionado == null) return;

            using var db = new TallerDbContext();
            db.Clientes.Remove(ClienteSeleccionado);
            db.SaveChanges();
            Clientes.Remove(ClienteSeleccionado);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
