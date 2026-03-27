using Proyecto_taller.Data;
using Proyecto_taller.Helpers;
using Proyecto_taller.Models;
using Proyecto_taller.Views;
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
        // ── Estado ────────────────────────────────────────────────────────────
        private Usuario? _usuarioSeleccionado;
        private string _textoBusqueda = string.Empty;
        private bool _filtroTodos = true;
        private bool _filtroActivos;
        private bool _filtroAdmins;
        private bool _filtroEmpleados;

        private ObservableCollection<Usuario> _todosMaestro = new();
        public ObservableCollection<Usuario> Usuarios { get; set; } = new();

        // ── Propiedades ───────────────────────────────────────────────────────

        public Usuario? UsuarioSeleccionado
        {
            get => _usuarioSeleccionado;
            set
            {
                _usuarioSeleccionado = value;
                OnPropertyChanged();
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public string TextoBusqueda
        {
            get => _textoBusqueda;
            set { _textoBusqueda = value; OnPropertyChanged(); AplicarFiltro(); }
        }

        public bool FiltroTodos
        {
            get => _filtroTodos;
            set { _filtroTodos = value; OnPropertyChanged(); if (value) AplicarFiltro(); }
        }
        public bool FiltroActivos
        {
            get => _filtroActivos;
            set { _filtroActivos = value; OnPropertyChanged(); if (value) AplicarFiltro(); }
        }
        public bool FiltroAdmins
        {
            get => _filtroAdmins;
            set { _filtroAdmins = value; OnPropertyChanged(); if (value) AplicarFiltro(); }
        }
        public bool FiltroEmpleados
        {
            get => _filtroEmpleados;
            set { _filtroEmpleados = value; OnPropertyChanged(); if (value) AplicarFiltro(); }
        }

        // ── Comandos ──────────────────────────────────────────────────────────

        public ICommand CargarUsuariosCommand { get; }
        public ICommand AgregarUsuarioCommand { get; }
        public ICommand EditarUsuarioCommand { get; }
        public ICommand VerHistorialCommand { get; }
        public ICommand ActivarDesactivarCommand { get; }
        public ICommand CambiarPasswordCommand { get; }
        public ICommand EliminarUsuarioCommand { get; }
        public ICommand LimpiarBusquedaCommand { get; }

        // ── Constructor ───────────────────────────────────────────────────────

        public UsuariosViewModel()
        {
            CargarUsuariosCommand = new RelayCommand(CargarUsuarios);
            AgregarUsuarioCommand = new RelayCommand(AgregarUsuario);
            EditarUsuarioCommand = new RelayCommand(EditarUsuario,
                                        () => UsuarioSeleccionado != null);
            VerHistorialCommand = new RelayCommand(VerHistorial,
                                        () => UsuarioSeleccionado != null);
            ActivarDesactivarCommand = new RelayCommand(ActivarDesactivar,
                                        () => UsuarioSeleccionado != null);
            CambiarPasswordCommand = new RelayCommand(CambiarPassword,
                                        () => UsuarioSeleccionado != null);
            EliminarUsuarioCommand = new RelayCommand(EliminarUsuario,
                                        () => UsuarioSeleccionado != null);
            LimpiarBusquedaCommand = new RelayCommand(() => TextoBusqueda = string.Empty);

            CargarUsuarios();
        }

        // ── Carga y filtro ────────────────────────────────────────────────────

        public void CargarUsuarios()
        {
            var idAnterior = UsuarioSeleccionado?.UsuarioID;
            _todosMaestro.Clear();

            using var db = new TallerDbContext();
            foreach (var u in db.Usuarios.OrderBy(u => u.NombreUsuario).ToList())
                _todosMaestro.Add(u);

            AplicarFiltro();

            if (idAnterior.HasValue)
                UsuarioSeleccionado =
                    Usuarios.FirstOrDefault(u => u.UsuarioID == idAnterior.Value);
        }

        private void AplicarFiltro()
        {
            var query = _todosMaestro.AsEnumerable();

            // Filtro de radio
            if (FiltroActivos)
                query = query.Where(u => u.Activo);
            else if (FiltroAdmins)
                query = query.Where(u => u.Rol == "Administrador");
            else if (FiltroEmpleados)
                query = query.Where(u => u.Rol == "Empleado");

            // Búsqueda por texto
            var texto = TextoBusqueda.Trim().ToLower();
            if (!string.IsNullOrEmpty(texto))
                query = query.Where(u =>
                    u.NombreUsuario.ToLower().Contains(texto)
                    || u.NombreCompleto.ToLower().Contains(texto)
                    || u.Rol.ToLower().Contains(texto));

            Usuarios.Clear();
            foreach (var u in query) Usuarios.Add(u);
        }

        // ── Agregar ───────────────────────────────────────────────────────────

        private void AgregarUsuario()
        {
            var win = new AgregarUsuarioWindow();
            if (win.ShowDialog() == true)
                CargarUsuarios();
        }

        // ── Editar (nombre completo y rol) ────────────────────────────────────

        private void EditarUsuario()
        {
            if (UsuarioSeleccionado == null) return;

            using var db = new TallerDbContext();
            var usuarioFresco = db.Usuarios.Find(UsuarioSeleccionado.UsuarioID);
            if (usuarioFresco == null)
            {
                MessageBox.Show("El usuario ya no existe.",
                    "No encontrado", MessageBoxButton.OK, MessageBoxImage.Warning);
                CargarUsuarios();
                return;
            }

            var win = new EditarUsuarioWindow(usuarioFresco);
            if (win.ShowDialog() == true)
                CargarUsuarios();
        }

        // ── Ver historial ─────────────────────────────────────────────────────

        private void VerHistorial()
        {
            if (UsuarioSeleccionado == null) return;
            var win = new HistorialUsuarioWindow(UsuarioSeleccionado.UsuarioID);
            win.ShowDialog();
        }

        // ── Activar / Desactivar ──────────────────────────────────────────────

        private void ActivarDesactivar()
        {
            if (UsuarioSeleccionado == null) return;

            if (UsuarioSeleccionado.UsuarioID == SessionManager.UsuarioActual?.UsuarioID)
            {
                MessageBox.Show("No puedes desactivar tu propio usuario.",
                    "Advertencia", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var nuevoEstado = !UsuarioSeleccionado.Activo;
            var accion = nuevoEstado ? "activar" : "desactivar";

            var resultado = MessageBox.Show(
                $"¿Seguro que deseas {accion} a '{UsuarioSeleccionado.NombreUsuario}'?",
                "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (resultado != MessageBoxResult.Yes) return;

            using var db = new TallerDbContext();
            var usuario = db.Usuarios.Find(UsuarioSeleccionado.UsuarioID);
            if (usuario != null)
            {
                usuario.Activo = nuevoEstado;
                db.SaveChanges();
                CargarUsuarios();

                MessageBox.Show(
                    $"Usuario {(nuevoEstado ? "activado" : "desactivado")} correctamente.",
                    "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        // ── Cambiar contraseña ────────────────────────────────────────────────

        private void CambiarPassword()
        {
            if (UsuarioSeleccionado == null) return;
            var win = new CambiarPasswordWindow(UsuarioSeleccionado.UsuarioID);
            if (win.ShowDialog() == true)
                CargarUsuarios(); // refrescar para mostrar fecha de cambio actualizada
        }

        // ── Eliminar ──────────────────────────────────────────────────────────

        private void EliminarUsuario()
        {
            if (UsuarioSeleccionado == null) return;

            if (UsuarioSeleccionado.UsuarioID == SessionManager.UsuarioActual?.UsuarioID)
            {
                MessageBox.Show("No puedes eliminar tu propio usuario.",
                    "Advertencia", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var resultado = MessageBox.Show(
                $"⚠️  ADVERTENCIA\n\n" +
                $"¿Eliminar al usuario '{UsuarioSeleccionado.NombreUsuario}'?\n\n" +
                $"Esta acción NO se puede deshacer.",
                "Confirmar Eliminación",
                MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (resultado != MessageBoxResult.Yes) return;

            using var db = new TallerDbContext();
            var usuario = db.Usuarios.Find(UsuarioSeleccionado.UsuarioID);
            if (usuario != null)
            {
                db.Usuarios.Remove(usuario);
                db.SaveChanges();
                CargarUsuarios();

                MessageBox.Show("Usuario eliminado correctamente.",
                    "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        // ── INotifyPropertyChanged ────────────────────────────────────────────

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
