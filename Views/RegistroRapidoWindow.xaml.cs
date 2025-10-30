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
            dpFechaEntrega.SelectedDate = DateTime.Now.AddDays(3);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void RegistrarButton_Click(object sender, RoutedEventArgs e)
        {
            //  VALIDACIONES 
            if (string.IsNullOrWhiteSpace(txtNombre.Text))
            {
                MessageBox.Show("❌ El nombre del cliente es obligatorio.",
                    "Campo Requerido", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtNombre.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(txtApellido.Text))
            {
                MessageBox.Show("❌ El apellido del cliente es obligatorio.",
                    "Campo Requerido", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtApellido.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(txtTelefono.Text))
            {
                MessageBox.Show("❌ El teléfono del cliente es obligatorio.",
                    "Campo Requerido", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtTelefono.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(txtMarca.Text))
            {
                MessageBox.Show("❌ La marca del vehículo es obligatoria.",
                    "Campo Requerido", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtMarca.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(txtModelo.Text))
            {
                MessageBox.Show("❌ El modelo del vehículo es obligatorio.",
                    "Campo Requerido", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtModelo.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(txtPlaca.Text))
            {
                MessageBox.Show("❌ La placa del vehículo es obligatoria.",
                    "Campo Requerido", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtPlaca.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(txtDescripcion.Text))
            {
                MessageBox.Show("❌ La descripción del trabajo es obligatoria.",
                    "Campo Requerido", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtDescripcion.Focus();
                return;
            }

            try
            {
                using var db = new TallerDbContext();

                // ========== 1. CREAR O BUSCAR CLIENTE ==========
                var cliente = db.Clientes.FirstOrDefault(c =>
                    c.Nombre.ToLower() == txtNombre.Text.Trim().ToLower() &&
                    c.Apellido.ToLower() == txtApellido.Text.Trim().ToLower() &&
                    c.Telefono == txtTelefono.Text.Trim());

                bool clienteNuevo = false;
                if (cliente == null)
                {
                    clienteNuevo = true;
                    cliente = new Cliente
                    {
                        Nombre = txtNombre.Text.Trim(),
                        Apellido = txtApellido.Text.Trim(),
                        Telefono = txtTelefono.Text.Trim(),
                        Correo = txtEmail.Text.Trim(),
                        Direccion = string.IsNullOrWhiteSpace(txtDireccion.Text)
                            ? "Sin dirección"
                            : txtDireccion.Text.Trim(),
                        FechaRegistro = DateTime.Now
                    };
                    db.Clientes.Add(cliente);
                    db.SaveChanges();
                }

                // ========== 2. CREAR O BUSCAR VEHÍCULO ==========
                var vehiculo = db.Vehiculos.FirstOrDefault(v =>
                    v.Placa.ToLower() == txtPlaca.Text.Trim().ToLower());

                bool vehiculoNuevo = false;
                if (vehiculo == null)
                {
                    vehiculoNuevo = true;
                    int.TryParse(txtAnio.Text, out int anio);

                    vehiculo = new Vehiculo
                    {
                        ClienteID = cliente.ClienteID,
                        Marca = txtMarca.Text.Trim(),
                        Modelo = txtModelo.Text.Trim(),
                        Placa = txtPlaca.Text.Trim().ToUpper(),
                        Anio = anio > 1900 && anio <= DateTime.Now.Year + 1 ? anio : null
                    };
                    db.Vehiculos.Add(vehiculo);
                    db.SaveChanges();
                }
                else
                {
                    // Si el vehículo existe pero pertenece a otro cliente
                    if (vehiculo.ClienteID != cliente.ClienteID)
                    {
                        var resultado = MessageBox.Show(
                            $"⚠️ Este vehículo (Placa: {vehiculo.Placa}) ya está registrado " +
                            $"a nombre de otro cliente.\n\n" +
                            $"¿Desea actualizar el propietario a {cliente.Nombre} {cliente.Apellido}?",
                            "Vehículo Existente",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Question);

                        if (resultado == MessageBoxResult.Yes)
                        {
                            vehiculo.ClienteID = cliente.ClienteID;
                            db.SaveChanges();
                        }
                    }
                }

                // ========== 3. CREAR TRABAJO ==========
                decimal.TryParse(txtPrecio.Text, out decimal precio);

                // ⭐ CORRECCIÓN: Solo asignar PrecioEstimado, NO PrecioFinal
                // El PrecioFinal se debe calcular cuando se agreguen servicios/repuestos
                // o cuando se finalice el trabajo
                var trabajo = new Trabajo
                {
                    VehiculoID = vehiculo.VehiculoID,
                    FechaIngreso = DateTime.Now,
                    FechaEntrega = dpFechaEntrega.SelectedDate,
                    Descripcion = txtDescripcion.Text.Trim(),
                    Estado = ((ComboBoxItem)cmbEstado.SelectedItem).Content.ToString(),
                    TipoTrabajo = ((ComboBoxItem)cmbTipoTrabajo.SelectedItem).Content.ToString(),
                    PrecioEstimado = precio > 0 ? precio : null,
                    PrecioFinal = null // ⭐ IMPORTANTE: Dejar en NULL hasta finalizar el trabajo
                };

                db.Trabajos.Add(trabajo);
                db.SaveChanges();

                // ========== MENSAJE DE ÉXITO ==========
                string mensajeDetalle = $"✅ TRABAJO REGISTRADO EXITOSAMENTE\n\n";
                mensajeDetalle += $"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n\n";
                mensajeDetalle += $"📋 INFORMACIÓN DEL TRABAJO\n";
                mensajeDetalle += $"   • ID: #{trabajo.TrabajoID}\n";
                mensajeDetalle += $"   • Tipo: {trabajo.TipoTrabajo}\n";
                mensajeDetalle += $"   • Estado: {trabajo.Estado}\n";
                mensajeDetalle += $"   • Fecha Ingreso: {trabajo.FechaIngreso:dd/MM/yyyy HH:mm}\n";

                if (trabajo.FechaEntrega.HasValue)
                    mensajeDetalle += $"   • Entrega Estimada: {trabajo.FechaEntrega.Value:dd/MM/yyyy}\n";

                if (trabajo.PrecioEstimado.HasValue)
                    mensajeDetalle += $"   • Precio Estimado: Bs. {trabajo.PrecioEstimado:N2}\n";

                mensajeDetalle += $"\n👤 CLIENTE\n";
                mensajeDetalle += $"   • Nombre: {cliente.Nombre} {cliente.Apellido}\n";
                mensajeDetalle += $"   • Teléfono: {cliente.Telefono}\n";

                if (clienteNuevo)
                    mensajeDetalle += $"   • ✨ Cliente NUEVO registrado\n";

                mensajeDetalle += $"\n🚗 VEHÍCULO\n";
                mensajeDetalle += $"   • {vehiculo.Marca} {vehiculo.Modelo}";

                if (vehiculo.Anio.HasValue)
                    mensajeDetalle += $" ({vehiculo.Anio})";

                mensajeDetalle += $"\n   • Placa: {vehiculo.Placa}\n";

                if (vehiculoNuevo)
                    mensajeDetalle += $"   • ✨ Vehículo NUEVO registrado\n";

                mensajeDetalle += $"\n━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n";
                mensajeDetalle += $"\n💡 Próximos pasos:\n";
                mensajeDetalle += $"   1. Agregar servicios desde el módulo Trabajos\n";
                mensajeDetalle += $"   2. Agregar repuestos necesarios\n";
                mensajeDetalle += $"   3. El precio final se calculará automáticamente\n";
                mensajeDetalle += $"   4. Finalizar el trabajo cuando esté completado";

                MessageBox.Show(
                    mensajeDetalle,
                    "✅ Registro Exitoso",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"❌ ERROR AL REGISTRAR EL TRABAJO\n\n" +
                    $"Detalles técnicos:\n{ex.Message}\n\n" +
                    $"Por favor, verifique:\n" +
                    $"• Conexión a la base de datos\n" +
                    $"• Formato de los datos ingresados\n" +
                    $"• Permisos del sistema",
                    "Error de Sistema",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}
