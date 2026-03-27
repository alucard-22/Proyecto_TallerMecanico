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
using System.Windows.Navigation;
using System.Windows.Shapes;
using Proyecto_taller.ViewModels;
using Proyecto_taller.Models;
using Proyecto_taller.Data;
using Proyecto_taller.Views;
using Microsoft.EntityFrameworkCore;

namespace Proyecto_taller.Views
{
    public partial class EditarVehiculoWindow : Window
    {
        private readonly Vehiculo? _vehiculo;
        private readonly bool _esNuevo;
        private Cliente? _clienteSeleccionado;

        // ── Modo EDITAR ────────────────────────────────────────────────────────
        public EditarVehiculoWindow(Vehiculo vehiculo)
        {
            InitializeComponent();
            _vehiculo = vehiculo;
            _esNuevo = false;

            txtTitulo.Text = $"✏️  Editar Vehículo — {vehiculo.Placa}";
            txtMarca.Text = vehiculo.Marca;
            txtModelo.Text = vehiculo.Modelo;
            txtPlaca.Text = vehiculo.Placa;
            txtAnio.Text = vehiculo.Anio?.ToString() ?? string.Empty;

            // Precargar el cliente actual
            using var db = new TallerDbContext();
            var cliente = db.Clientes.Find(vehiculo.ClienteID);
            if (cliente != null) SeleccionarCliente(cliente);

            txtMarca.Focus();
        }

        // ── Modo NUEVO ─────────────────────────────────────────────────────────
        public EditarVehiculoWindow()
        {
            InitializeComponent();
            _vehiculo = null;
            _esNuevo = true;
            txtTitulo.Text = "🚗  Nuevo Vehículo";
            txtBuscarCliente.Focus();
        }

        // ─── Búsqueda de cliente ──────────────────────────────────────────────

        private void TxtBuscarCliente_Changed(object sender, TextChangedEventArgs e)
        {
            var texto = txtBuscarCliente.Text.Trim();
            if (texto.Length < 2)
            {
                panelResultados.Visibility = Visibility.Collapsed;
                return;
            }

            using var db = new TallerDbContext();
            var lower = texto.ToLower();

            var resultados = db.Clientes
                .Where(c => c.Nombre.ToLower().Contains(lower)
                         || c.Apellido.ToLower().Contains(lower)
                         || c.Telefono.Contains(texto))
                .OrderBy(c => c.Apellido)
                .Take(8)
                .ToList();

            if (resultados.Count == 0)
            {
                panelResultados.Visibility = Visibility.Collapsed;
                return;
            }

            lstClientes.ItemsSource = resultados;
            panelResultados.Visibility = Visibility.Visible;
        }

        private void LstClientes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lstClientes.SelectedItem is not Cliente cliente) return;
            SeleccionarCliente(cliente);
            panelResultados.Visibility = Visibility.Collapsed;
        }

        private void SeleccionarCliente(Cliente cliente)
        {
            _clienteSeleccionado = cliente;
            txtClienteSeleccionado.Text =
                $"{cliente.Nombre} {cliente.Apellido}   ·   {cliente.Telefono}";
            panelClienteSeleccionado.Visibility = Visibility.Visible;
            txtBuscarCliente.Text = string.Empty;
            btnQuitarCliente.Visibility = Visibility.Visible;
        }

        private void QuitarCliente_Click(object sender, RoutedEventArgs e)
        {
            _clienteSeleccionado = null;
            panelClienteSeleccionado.Visibility = Visibility.Collapsed;
            btnQuitarCliente.Visibility = Visibility.Collapsed;
            txtBuscarCliente.Clear();
            txtBuscarCliente.Focus();
        }

        // ─── Guardar ──────────────────────────────────────────────────────────

        private void Guardar_Click(object sender, RoutedEventArgs e)
        {
            // Validaciones
            if (_clienteSeleccionado == null)
            {
                Msg("Selecciona un cliente propietario.");
                txtBuscarCliente.Focus();
                return;
            }
            if (string.IsNullOrWhiteSpace(txtMarca.Text))
            {
                Msg("La marca es obligatoria.");
                txtMarca.Focus();
                return;
            }
            if (string.IsNullOrWhiteSpace(txtModelo.Text))
            {
                Msg("El modelo es obligatorio.");
                txtModelo.Focus();
                return;
            }
            if (string.IsNullOrWhiteSpace(txtPlaca.Text))
            {
                Msg("La placa es obligatoria.");
                txtPlaca.Focus();
                return;
            }

            int? anio = null;
            if (!string.IsNullOrWhiteSpace(txtAnio.Text))
            {
                if (!int.TryParse(txtAnio.Text.Trim(), out int a)
                    || a < 1900 || a > DateTime.Now.Year + 1)
                {
                    Msg($"El año debe ser un número entre 1900 y {DateTime.Now.Year + 1}.");
                    txtAnio.Focus();
                    return;
                }
                anio = a;
            }

            string placa = txtPlaca.Text.Trim().ToUpper();

            try
            {
                using var db = new TallerDbContext();

                // Verificar placa única (excluyendo el vehículo actual en modo edición)
                int? idActual = _esNuevo ? null : _vehiculo!.VehiculoID;
                bool placaDuplicada = db.Vehiculos.Any(v =>
                    v.Placa.ToUpper() == placa &&
                    (!idActual.HasValue || v.VehiculoID != idActual.Value));

                if (placaDuplicada)
                {
                    Msg($"La placa '{placa}' ya está registrada en otro vehículo.");
                    txtPlaca.Focus();
                    return;
                }

                if (_esNuevo)
                {
                    var nuevo = new Vehiculo
                    {
                        ClienteID = _clienteSeleccionado.ClienteID,
                        Marca = txtMarca.Text.Trim(),
                        Modelo = txtModelo.Text.Trim(),
                        Placa = placa,
                        Anio = anio
                    };
                    db.Vehiculos.Add(nuevo);
                    db.SaveChanges();

                    MessageBox.Show(
                        $"✅  Vehículo registrado correctamente.\n\n" +
                        $"Placa:      {nuevo.Placa}\n" +
                        $"Vehículo:   {nuevo.Marca} {nuevo.Modelo}\n" +
                        $"Propietario: {_clienteSeleccionado.Nombre} {_clienteSeleccionado.Apellido}",
                        "Vehículo Creado",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    var v = db.Vehiculos.Find(_vehiculo!.VehiculoID);
                    if (v == null) { Msg("No se encontró el vehículo."); return; }

                    v.ClienteID = _clienteSeleccionado.ClienteID;
                    v.Marca = txtMarca.Text.Trim();
                    v.Modelo = txtModelo.Text.Trim();
                    v.Placa = placa;
                    v.Anio = anio;

                    db.SaveChanges();

                    MessageBox.Show(
                        $"✅  Vehículo actualizado correctamente.\n\n" +
                        $"Placa:       {v.Placa}\n" +
                        $"Vehículo:    {v.Marca} {v.Modelo}\n" +
                        $"Propietario: {_clienteSeleccionado.Nombre} {_clienteSeleccionado.Apellido}",
                        "Vehículo Actualizado",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al guardar:\n{ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancelar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Msg(string m)
            => MessageBox.Show(m, "Validación",
                MessageBoxButton.OK, MessageBoxImage.Warning);
    }
}
