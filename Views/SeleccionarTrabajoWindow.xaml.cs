using Proyecto_taller.Data;
using Proyecto_taller.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.EntityFrameworkCore;

namespace Proyecto_taller.Views
{
    public partial class SeleccionarTrabajoWindow : Window
    {
        // El trabajo seleccionado que devolverá esta ventana
        public Trabajo TrabajoSeleccionado { get; private set; }

        private List<Trabajo> _todos = new();

        public SeleccionarTrabajoWindow()
        {
            InitializeComponent();
            Loaded += (_, __) => Cargar();
        }

        private void Cargar()
        {
            using var db = new TallerDbContext();

            _todos = db.Trabajos
                .Include(t => t.Vehiculo).ThenInclude(v => v.Cliente)
                .Where(t => t.Estado == "Finalizado"
                         && t.PrecioFinal != null
                         && !db.Facturas.Any(f => f.TrabajoID == t.TrabajoID))
                .OrderByDescending(t => t.FechaEntrega)
                .ToList();

            dgTrabajos.ItemsSource = _todos;
            ActualizarContador(_todos.Count);

            if (_todos.Count == 0)
            {
                MessageBox.Show(
                    "No hay trabajos finalizados pendientes de facturar.\n\n" +
                    "Asegúrate de que el trabajo esté en estado 'Finalizado' y tenga un precio final asignado.",
                    "Sin trabajos disponibles",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = false;
                Close();
            }
        }

        private void TxtBuscar_Changed(object sender, TextChangedEventArgs e)
        {
            var f = txtBuscar.Text.Trim().ToLower();
            var filtrados = string.IsNullOrEmpty(f)
                ? _todos
                : _todos.Where(t =>
                    (t.Vehiculo?.Cliente?.Nombre?.ToLower().Contains(f) ?? false) ||
                    (t.Vehiculo?.Cliente?.Apellido?.ToLower().Contains(f) ?? false) ||
                    (t.Vehiculo?.Cliente?.Telefono?.Contains(f) ?? false) ||
                    (t.Vehiculo?.Placa?.ToLower().Contains(f) ?? false) ||
                    (t.Vehiculo?.Marca?.ToLower().Contains(f) ?? false) ||
                    (t.TipoTrabajo?.ToLower().Contains(f) ?? false) ||
                    t.TrabajoID.ToString().Contains(f)).ToList();

            dgTrabajos.ItemsSource = filtrados;
            ActualizarContador(filtrados.Count);
        }

        private void ActualizarContador(int n)
            => txtContador.Text = $"{n} trabajo(s) disponible(s) para facturar";

        private void DgTrabajos_DoubleClick(object sender, MouseButtonEventArgs e)
            => ConfirmarSeleccion();

        private void Facturar_Click(object sender, RoutedEventArgs e)
            => ConfirmarSeleccion();

        private void ConfirmarSeleccion()
        {
            if (dgTrabajos.SelectedItem is not Trabajo t)
            {
                MessageBox.Show("Selecciona un trabajo de la lista.",
                    "Sin selección", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            TrabajoSeleccionado = t;
            DialogResult = true;
            Close();
        }

        private void Cancelar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}