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
    /// <summary>
    /// Lógica de interacción para RegistroRapidoWindow.xaml
    /// </summary>
    public partial class RegistroRapidoWindow : Window
    {
        public RegistroRapidoWindow()
        {
            InitializeComponent();
        }
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void RegistrarButton_Click(object sender, RoutedEventArgs e)
        {
            // Validaciones
            if (string.IsNullOrWhiteSpace(txtNombre.Text) || string.IsNullOrWhiteSpace(txtApellido.Text))
            {
                MessageBox.Show("Por favor complete el nombre y apellido del cliente.", "Datos Incompletos", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(txtMarca.Text) || string.IsNullOrWhiteSpace(txtPlaca.Text))
            {
                MessageBox.Show("Por favor complete la marca y placa del vehículo.", "Datos Incompletos", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(txtDescripcion.Text))
            {
                MessageBox.Show("Por favor describa el problema o trabajo a realizar.", "Datos Incompletos", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using var db = new TallerDbContext();

                // 1. Crear o buscar cliente
                var cliente = db.Clientes.FirstOrDefault(c =>
                    c.Nombre.ToLower() == txtNombre.Text.ToLower() &&
                    c.Apellido.ToLower() == txtApellido.Text.ToLower());

                if (cliente == null)
                {
                    cliente = new Cliente
                    {
                        Nombre = txtNombre.Text,
                        Apellido = txtApellido.Text,
                        Telefono = txtTelefono.Text,
                        Correo = txtEmail.Text,
                        Direccion = "Sin dirección",
                        FechaRegistro = DateTime.Now
                    };
                    db.Clientes.Add(cliente);
                    db.SaveChanges();
                }

                // 2. Crear o buscar vehículo
                var vehiculo = db.Vehiculos.FirstOrDefault(v => v.Placa.ToLower() == txtPlaca.Text.ToLower());

                if (vehiculo == null)
                {
                    int.TryParse(txtAnio.Text, out int anio);
                    vehiculo = new Vehiculo
                    {
                        ClienteID = cliente.ClienteID,
                        Marca = txtMarca.Text,
                        Modelo = txtModelo.Text,
                        Placa = txtPlaca.Text,
                        Anio = anio > 0 ? anio : null
                    };
                    db.Vehiculos.Add(vehiculo);
                    db.SaveChanges();
                }

                // 3. Crear trabajo
                decimal.TryParse(txtPrecio.Text, out decimal precio);
                var trabajo = new Trabajo
                {
                    VehiculoID = vehiculo.VehiculoID,
                    FechaIngreso = DateTime.Now,
                    Descripcion = txtDescripcion.Text,
                    Estado = "Pendiente",
                    TipoTrabajo = ((ComboBoxItem)cmbTipoTrabajo.SelectedItem).Content.ToString(),
                    PrecioEstimado = precio > 0 ? precio : null
                };
                db.Trabajos.Add(trabajo);
                db.SaveChanges();

                MessageBox.Show(
                        $"✅ Trabajo registrado exitosamente!\n\n" +
                        $"Trabajo ID: {trabajo.TrabajoID}\n" +
                        $"Cliente: {cliente.Nombre} {cliente.Apellido}\n" +
                        $"Vehículo: {vehiculo.Marca} {vehiculo.Modelo} - {vehiculo.Placa}\n" +
                        $"Tipo: {trabajo.TipoTrabajo}\n" +
                        $"Estado: {trabajo.Estado}",
                        "Registro Exitoso",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error al registrar el trabajo:\n{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}
