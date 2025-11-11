using Proyecto_taller.Data;
using Proyecto_taller.Helpers;
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
    public class UsuariosViewModel : INotifyPropertyChanged
    {
        private Usuario _usuarioSeleccionado;

        public ObservableCollection<Usuario> Usuarios { get; set; }

        public Usuario UsuarioSeleccionado
        {
            get => _usuarioSeleccionado;
            set
            {
                _usuarioSeleccionado = value;
                OnPropertyChanged();
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public ICommand CargarUsuariosCommand { get; }
        public ICommand AgregarUsuarioCommand { get; }
        public ICommand ActivarDesactivarCommand { get; }
        public ICommand CambiarPasswordCommand { get; }
        public ICommand EliminarUsuarioCommand { get; }

        public UsuariosViewModel()
        {
            Usuarios = new ObservableCollection<Usuario>();

            CargarUsuariosCommand = new RelayCommand(CargarUsuarios);
            AgregarUsuarioCommand = new RelayCommand(AgregarUsuario);
            ActivarDesactivarCommand = new RelayCommand(ActivarDesactivar, () => UsuarioSeleccionado != null);
            CambiarPasswordCommand = new RelayCommand(CambiarPassword, () => UsuarioSeleccionado != null);
            EliminarUsuarioCommand = new RelayCommand(EliminarUsuario, () => UsuarioSeleccionado != null);

            CargarUsuarios();
        }

        private void CargarUsuarios()
        {
            Usuarios.Clear();
            using var db = new TallerDbContext();

            var usuarios = db.Usuarios.OrderBy(u => u.NombreUsuario).ToList();

            foreach (var usuario in usuarios)
            {
                Usuarios.Add(usuario);
            }
        }

        private void AgregarUsuario()
        {
            var ventana = new Views.AgregarUsuarioWindow();
            if (ventana.ShowDialog() == true)
            {
                CargarUsuarios();
            }
        }

        private void ActivarDesactivar()
        {
            if (UsuarioSeleccionado == null) return;

            // No permitir desactivar al usuario actual
            if (UsuarioSeleccionado.UsuarioID == SessionManager.UsuarioActual.UsuarioID)
            {
                MessageBox.Show(
                    "No puedes desactivar tu propio usuario.",
                    "Advertencia",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            var nuevoEstado = !UsuarioSeleccionado.Activo;
            var mensaje = nuevoEstado ? "activar" : "desactivar";

            var resultado = MessageBox.Show(
                $"¿Está seguro de {mensaje} al usuario '{UsuarioSeleccionado.NombreUsuario}'?",
                "Confirmar Acción",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (resultado == MessageBoxResult.Yes)
            {
                using var db = new TallerDbContext();
                var usuario = db.Usuarios.Find(UsuarioSeleccionado.UsuarioID);

                if (usuario != null)
                {
                    usuario.Activo = nuevoEstado;
                    db.SaveChanges();

                    UsuarioSeleccionado.Activo = nuevoEstado;
                    OnPropertyChanged(nameof(Usuarios));

                    MessageBox.Show(
                        $"Usuario {(nuevoEstado ? "activado" : "desactivado")} exitosamente.",
                        "Éxito",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
        }

        private void CambiarPassword()
        {
            if (UsuarioSeleccionado == null) return;

            var ventana = new Views.CambiarPasswordWindow(UsuarioSeleccionado.UsuarioID);
            ventana.ShowDialog();
        }

        private void EliminarUsuario()
        {
            if (UsuarioSeleccionado == null) return;

            // No permitir eliminar al usuario actual
            if (UsuarioSeleccionado.UsuarioID == SessionManager.UsuarioActual.UsuarioID)
            {
                MessageBox.Show(
                    "No puedes eliminar tu propio usuario.",
                    "Advertencia",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            var resultado = MessageBox.Show(
                $"⚠️ ADVERTENCIA ⚠️\n\n" +
                $"¿Está seguro de eliminar al usuario '{UsuarioSeleccionado.NombreUsuario}'?\n\n" +
                $"Esta acción NO se puede deshacer.",
                "Confirmar Eliminación",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (resultado == MessageBoxResult.Yes)
            {
                using var db = new TallerDbContext();
                var usuario = db.Usuarios.Find(UsuarioSeleccionado.UsuarioID);

                if (usuario != null)
                {
                    db.Usuarios.Remove(usuario);
                    db.SaveChanges();
                    Usuarios.Remove(UsuarioSeleccionado);

                    MessageBox.Show(
                        "Usuario eliminado exitosamente.",
                        "Éxito",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
