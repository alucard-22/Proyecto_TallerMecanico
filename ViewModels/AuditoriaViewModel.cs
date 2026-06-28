using Proyecto_taller.Data;
using Proyecto_taller.Helpers;
using Proyecto_taller.Models;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;

namespace Proyecto_taller.ViewModels
{
    public class AuditoriaViewModel : INotifyPropertyChanged
    {
        private Usuario _usuarioFiltro;
        private DateTime _fechaInicio;
        private DateTime _fechaFin;
        private string _accionFiltro = "Todas";
        private int _totalRegistros;

        public ObservableCollection<RegistroAuditoria> Registros { get; set; } = new();
        public ObservableCollection<Usuario> Usuarios { get; set; } = new();

        // Lista fija de acciones conocidas, para no depender de strings libres
        // en el ComboBox de filtro. "Todas" siempre va primero.
        public ObservableCollection<string> AccionesDisponibles { get; set; } = new()
        {
            "Todas", "Login", "Crear", "Editar", "Eliminar", "Finalizar", "Reiniciar BD"
        };

        public Usuario UsuarioFiltro
        {
            get => _usuarioFiltro;
            set { _usuarioFiltro = value; OnPropertyChanged(); AplicarFiltro(); }
        }

        public DateTime FechaInicio
        {
            get => _fechaInicio;
            set { _fechaInicio = value; OnPropertyChanged(); AplicarFiltro(); }
        }

        public DateTime FechaFin
        {
            get => _fechaFin;
            set { _fechaFin = value; OnPropertyChanged(); AplicarFiltro(); }
        }

        public string AccionFiltro
        {
            get => _accionFiltro;
            set { _accionFiltro = value; OnPropertyChanged(); AplicarFiltro(); }
        }

        public int TotalRegistros
        {
            get => _totalRegistros;
            set { _totalRegistros = value; OnPropertyChanged(); }
        }

        public ICommand CargarCommand { get; }
        public ICommand LimpiarFiltrosCommand { get; }

        public AuditoriaViewModel()
        {
            // Período por defecto: últimos 30 días, suficiente para revisar
            // actividad reciente sin sobrecargar la vista al abrir.
            FechaFin = DateTime.Today.AddDays(1);
            FechaInicio = DateTime.Today.AddDays(-30);

            CargarCommand = new RelayCommand(Cargar);
            LimpiarFiltrosCommand = new RelayCommand(LimpiarFiltros);

            CargarUsuarios();
            Cargar();
        }

        private void CargarUsuarios()
        {
            using var db = new TallerDbContext();
            Usuarios.Clear();
            foreach (var u in db.Usuarios.OrderBy(u => u.NombreCompleto).ToList())
                Usuarios.Add(u);
        }

        private void Cargar() => AplicarFiltro();

        private void AplicarFiltro()
        {
            using var db = new TallerDbContext();

            var query = db.RegistrosAuditoria
                .Include(a => a.Usuario)
                .Where(a => a.Fecha >= FechaInicio && a.Fecha < FechaFin)
                .AsQueryable();

            if (UsuarioFiltro != null)
                query = query.Where(a => a.UsuarioID == UsuarioFiltro.UsuarioID);

            if (!string.IsNullOrEmpty(AccionFiltro) && AccionFiltro != "Todas")
                query = query.Where(a => a.Accion == AccionFiltro);

            var resultado = query
                .OrderByDescending(a => a.Fecha)
                .Take(500) // límite razonable para no cargar miles de filas en memoria
                .ToList();

            Registros.Clear();
            foreach (var r in resultado)
                Registros.Add(r);

            TotalRegistros = resultado.Count;
        }

        private void LimpiarFiltros()
        {
            UsuarioFiltro = null;
            AccionFiltro = "Todas";
            FechaFin = DateTime.Today.AddDays(1);
            FechaInicio = DateTime.Today.AddDays(-30);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
