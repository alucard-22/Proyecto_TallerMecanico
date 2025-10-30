﻿using Microsoft.EntityFrameworkCore;
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

        // Propiedades para los filtros
        public bool FiltroTodos
        {
            get => _filtroTodos;
            set
            {
                _filtroTodos = value;
                OnPropertyChanged();
                if (value) AplicarFiltro(null);
            }
        }

        public bool FiltroPendientes
        {
            get => _filtroPendientes;
            set
            {
                _filtroPendientes = value;
                OnPropertyChanged();
                if (value) AplicarFiltro("Pendiente");
            }
        }

        public bool FiltroEnProgreso
        {
            get => _filtroEnProgreso;
            set
            {
                _filtroEnProgreso = value;
                OnPropertyChanged();
                if (value) AplicarFiltro("En Progreso");
            }
        }

        public bool FiltroFinalizados
        {
            get => _filtroFinalizados;
            set
            {
                _filtroFinalizados = value;
                OnPropertyChanged();
                if (value) AplicarFiltro("Finalizado");
            }
        }

        public ICommand CargarTrabajosCommand { get; }
        public ICommand AgregarTrabajoCommand { get; }
        public ICommand EditarTrabajoCommand { get; }
        public ICommand GestionarServiciosRepuestosCommand { get; } // ⭐ NUEVO
        public ICommand FinalizarTrabajoCommand { get; }
        public ICommand EliminarTrabajoCommand { get; }

        public TrabajosViewModel()
        {
            Trabajos = new ObservableCollection<Trabajo>();

            CargarTrabajosCommand = new RelayCommand(CargarTrabajos);
            AgregarTrabajoCommand = new RelayCommand(AgregarTrabajo);
            EditarTrabajoCommand = new RelayCommand(EditarTrabajo, () => TrabajoSeleccionado != null);
            GestionarServiciosRepuestosCommand = new RelayCommand(GestionarServiciosRepuestos, () => TrabajoSeleccionado != null); // ⭐ NUEVO
            FinalizarTrabajoCommand = new RelayCommand(FinalizarTrabajo, () => TrabajoSeleccionado != null && TrabajoSeleccionado.Estado != "Finalizado");
            EliminarTrabajoCommand = new RelayCommand(EliminarTrabajo, () => TrabajoSeleccionado != null);

            CargarTrabajos();
        }

        private void CargarTrabajos()
        {
            Trabajos.Clear();
            using var db = new TallerDbContext();

            var trabajos = db.Trabajos
                .Include(t => t.Vehiculo)
                    .ThenInclude(v => v.Cliente)
                .OrderByDescending(t => t.FechaIngreso)
                .ToList();

            foreach (var trabajo in trabajos)
            {
                Trabajos.Add(trabajo);
            }
        }

        private void AplicarFiltro(string estado)
        {
            Trabajos.Clear();
            using var db = new TallerDbContext();

            var query = db.Trabajos
                .Include(t => t.Vehiculo)
                    .ThenInclude(v => v.Cliente)
                .AsQueryable();

            if (estado != null)
            {
                query = query.Where(t => t.Estado == estado);
            }

            var trabajos = query.OrderByDescending(t => t.FechaIngreso).ToList();

            foreach (var trabajo in trabajos)
            {
                Trabajos.Add(trabajo);
            }
        }

        private void AgregarTrabajo()
        {
            using var db = new TallerDbContext();

            var primerVehiculo = db.Vehiculos
                .Include(v => v.Cliente)
                .FirstOrDefault();

            if (primerVehiculo == null)
            {
                System.Windows.MessageBox.Show(
                    "Debe registrar al menos un vehículo antes de crear trabajos.",
                    "Sin Vehículos",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
                return;
            }

            var nuevo = new Trabajo
            {
                VehiculoID = primerVehiculo.VehiculoID,
                Descripcion = "Mantenimiento general",
                Estado = "Pendiente",
                TipoTrabajo = "Mecánica",
                FechaIngreso = DateTime.Now,
                PrecioEstimado = 500.00m
            };

            db.Trabajos.Add(nuevo);
            db.SaveChanges();

            var trabajoConRelaciones = db.Trabajos
                .Include(t => t.Vehiculo)
                    .ThenInclude(v => v.Cliente)
                .FirstOrDefault(t => t.TrabajoID == nuevo.TrabajoID);

            if (trabajoConRelaciones != null)
            {
                Trabajos.Insert(0, trabajoConRelaciones);
            }
        }

        private void EditarTrabajo()
        {
            if (TrabajoSeleccionado == null) return;

            System.Windows.MessageBox.Show(
                $"Editando trabajo #{TrabajoSeleccionado.TrabajoID}\n" +
                $"Vehículo: {TrabajoSeleccionado.Vehiculo?.Marca} {TrabajoSeleccionado.Vehiculo?.Modelo}\n" +
                $"Estado: {TrabajoSeleccionado.Estado}",
                "Editar Trabajo");
        }

        // ⭐ NUEVO MÉTODO
        private void GestionarServiciosRepuestos()
        {
            if (TrabajoSeleccionado == null) return;

            var ventana = new GestionarTrabajoWindow(TrabajoSeleccionado.TrabajoID);
            if (ventana.ShowDialog() == true)
            {
                // Recargar el trabajo para actualizar los datos
                CargarTrabajos();

                System.Windows.MessageBox.Show(
                    "✅ Servicios y repuestos actualizados correctamente.",
                    "Éxito",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);
            }
        }

        private void FinalizarTrabajo()
        {
            if (TrabajoSeleccionado == null || TrabajoSeleccionado.Estado == "Finalizado")
                return;

            var resultado = System.Windows.MessageBox.Show(
                $"¿Marcar como finalizado el trabajo #{TrabajoSeleccionado.TrabajoID}?",
                "Finalizar Trabajo",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Question);

            if (resultado == System.Windows.MessageBoxResult.Yes)
            {
                using var db = new TallerDbContext();
                var trabajo = db.Trabajos.Find(TrabajoSeleccionado.TrabajoID);

                if (trabajo != null)
                {
                    trabajo.Estado = "Finalizado";
                    trabajo.FechaEntrega = DateTime.Now;

                    if (trabajo.PrecioFinal == null)
                    {
                        trabajo.PrecioFinal = trabajo.PrecioEstimado;
                    }

                    db.SaveChanges();
                    TrabajoSeleccionado.Estado = "Finalizado";
                    TrabajoSeleccionado.FechaEntrega = trabajo.FechaEntrega;
                    TrabajoSeleccionado.PrecioFinal = trabajo.PrecioFinal;
                    OnPropertyChanged(nameof(Trabajos));
                }
            }
        }

        private void EliminarTrabajo()
        {
            if (TrabajoSeleccionado == null) return;

            var resultado = System.Windows.MessageBox.Show(
                $"¿Está seguro de eliminar el trabajo #{TrabajoSeleccionado.TrabajoID}?",
                "Confirmar Eliminación",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning);

            if (resultado == System.Windows.MessageBoxResult.Yes)
            {
                using var db = new TallerDbContext();
                var trabajo = db.Trabajos.Find(TrabajoSeleccionado.TrabajoID);

                if (trabajo != null)
                {
                    db.Trabajos.Remove(trabajo);
                    db.SaveChanges();
                    Trabajos.Remove(TrabajoSeleccionado);
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
