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

namespace Proyecto_taller.Views
{
    public partial class NuevoVehiculoDialog : Window
    {
        private readonly Cliente _cliente;

        public NuevoVehiculoDialog(Cliente cliente)
        {
            InitializeComponent();
            _cliente = cliente;
            txtClienteInfo.Text = $"Para: {cliente?.Nombre} {cliente?.Apellido}";
            txtMarca.Focus();
        }

        private void Guardar_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtMarca.Text))
            { Msg("La marca es obligatoria."); return; }
            if (string.IsNullOrWhiteSpace(txtModelo.Text))
            { Msg("El modelo es obligatorio."); return; }
            if (string.IsNullOrWhiteSpace(txtPlaca.Text))
            { Msg("La placa es obligatoria."); return; }

            try
            {
                using var db = new TallerDbContext();

                var placaNorm = txtPlaca.Text.Trim().ToUpper();
                if (db.Vehiculos.Any(v => v.Placa.ToUpper() == placaNorm))
                {
                    Msg($"La placa '{placaNorm}' ya está registrada en el sistema.");
                    return;
                }

                int.TryParse(txtAnio.Text.Trim(), out int anio);
                var v = new Vehiculo
                {
                    ClienteID = _cliente.ClienteID,
                    Marca = txtMarca.Text.Trim(),
                    Modelo = txtModelo.Text.Trim(),
                    Placa = placaNorm,
                    Anio = anio > 1900 && anio <= DateTime.Now.Year + 1 ? anio : null
                };

                db.Vehiculos.Add(v);
                db.SaveChanges();

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al guardar:\n{ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancelar_Click(object sender, RoutedEventArgs e)
        { DialogResult = false; Close(); }

        private void Msg(string m)
            => MessageBox.Show(m, "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
    }
}
