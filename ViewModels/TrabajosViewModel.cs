using Microsoft.EntityFrameworkCore;
using Proyecto_taller.Data;
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
using System.Windows.Input;

namespace Proyecto_taller.ViewModels
{
    public class TrabajosViewModel : INotifyPropertyChanged
    {
        private Trabajo _trabajoSeleccionado;
        private bool _filtroTodos = true;
        private bool _filtroPendientes;
        private bool _filtroEnProgreso;
        private bool _filtroFinalizados;

        public ObservableCollection<Trabajo> Trabajos { get; set; }

        public Trabajo TrabajoSeleccionado
        {
            get => _trabajoSeleccionado;
            set
            {
                _trabajoSeleccionado = value;
                OnPropertyChanged();
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public bool FiltroTodos
        {
            get => _filtroTodos;
            set { _filtroTodos = value; OnPropertyChanged(); if (value) AplicarFiltro(null); }
        }
        public bool FiltroPendientes
        {
            get => _filtroPendientes;
            set { _filtroPendientes = value; OnPropertyChanged(); if (value) AplicarFiltro("Pendiente"); }
        }
        public bool FiltroEnProgreso
        {
            get => _filtroEnProgreso;
            set { _filtroEnProgreso = value; OnPropertyChanged(); if (value) AplicarFiltro("En Progreso"); }
        }
        public bool FiltroFinalizados
        {
            get => _filtroFinalizados;
            set { _filtroFinalizados = value; OnPropertyChanged(); if (value) AplicarFiltro("Finalizado"); }
        }

        // ── Comandos ──────────────────────────────────────────────
        public ICommand CargarTrabajosCommand { get; }
        public ICommand NuevoTrabajoCommand { get; }
        public ICommand VerDetallesCommand { get; }
        public ICommand GestionarCommand { get; }
        public ICommand FinalizarCommand { get; }
        public ICommand EliminarCommand { get; }

        public TrabajosViewModel()
        {
            Trabajos = new ObservableCollection<Trabajo>();

            CargarTrabajosCommand = new RelayCommand(CargarTrabajos);
            NuevoTrabajoCommand = new RelayCommand(NuevoTrabajo);
            VerDetallesCommand = new RelayCommand(VerDetalles, () => TrabajoSeleccionado != null);
            GestionarCommand = new RelayCommand(Gestionar, () => TrabajoSeleccionado != null && TrabajoSeleccionado.Estado != "Finalizado");
            FinalizarCommand = new RelayCommand(Finalizar, () => TrabajoSeleccionado != null && TrabajoSeleccionado.Estado != "Finalizado");
            EliminarCommand = new RelayCommand(Eliminar, () => TrabajoSeleccionado != null);

            CargarTrabajos();
        }

        // ── Carga / Filtro ─────────────────────────────────────────

        public void CargarTrabajos()
        {
            var idAnterior = TrabajoSeleccionado?.TrabajoID;
            Trabajos.Clear();

            using var db = new TallerDbContext();
            var lista = db.Trabajos
                .Include(t => t.Vehiculo).ThenInclude(v => v.Cliente)
                .OrderByDescending(t => t.FechaIngreso)
                .ToList();

            foreach (var t in lista) Trabajos.Add(t);

            if (idAnterior.HasValue)
                TrabajoSeleccionado = Trabajos.FirstOrDefault(t => t.TrabajoID == idAnterior);
        }

        private void AplicarFiltro(string estado)
        {
            Trabajos.Clear();
            using var db = new TallerDbContext();

            var q = db.Trabajos
                .Include(t => t.Vehiculo).ThenInclude(v => v.Cliente)
                .AsQueryable();

            if (estado != null) q = q.Where(t => t.Estado == estado);

            foreach (var t in q.OrderByDescending(t => t.FechaIngreso).ToList())
                Trabajos.Add(t);
        }

        // ── Acciones ───────────────────────────────────────────────

        private void NuevoTrabajo()
        {
            var win = new RegistroRapidoWindow();
            if (win.ShowDialog() == true) CargarTrabajos();
        }

        private void VerDetalles()
        {
            if (TrabajoSeleccionado == null) return;
            var win = new DetallesTrabajoWindow(TrabajoSeleccionado.TrabajoID);
            win.ShowDialog();
            CargarTrabajos();
        }

        private void Gestionar()
        {
            if (TrabajoSeleccionado == null) return;

            if (TrabajoSeleccionado.Estado == "Finalizado")
            {
                System.Windows.MessageBox.Show(
                    "No se puede modificar un trabajo ya finalizado.\nPuede ver sus detalles con el botón 'Ver Detalles'.",
                    "Trabajo Finalizado",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
                return;
            }

            var win = new GestionarTrabajoWindow(TrabajoSeleccionado.TrabajoID);
            if (win.ShowDialog() == true) CargarTrabajos();
        }

        private void Finalizar()
        {
            if (TrabajoSeleccionado == null) return;

            if (TrabajoSeleccionado.Estado == "Finalizado")
            {
                System.Windows.MessageBox.Show("Este trabajo ya está finalizado.",
                    "Información", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                return;
            }

            var win = new FinalizarTrabajoWindow(TrabajoSeleccionado.TrabajoID);
            if (win.ShowDialog() == true) CargarTrabajos();
        }

        private void Eliminar()
        {
            if (TrabajoSeleccionado == null) return;

            var r = System.Windows.MessageBox.Show(
                $"¿Eliminar el trabajo #{TrabajoSeleccionado.TrabajoID}?\nEsta acción no se puede deshacer.",
                "Confirmar Eliminación",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning);

            if (r != System.Windows.MessageBoxResult.Yes) return;

            using var db = new TallerDbContext();
            var t = db.Trabajos.Find(TrabajoSeleccionado.TrabajoID);
            if (t != null) { db.Trabajos.Remove(t); db.SaveChanges(); }
            Trabajos.Remove(TrabajoSeleccionado);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string n = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }
}
