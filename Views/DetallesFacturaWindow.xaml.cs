using Microsoft.EntityFrameworkCore;
using Proyecto_taller.Data;
using Proyecto_taller.Helpers;
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
    public partial class DetallesFacturaWindow : Window
    {
        private readonly int _facturaId;
        private Factura _factura;

        public DetallesFacturaWindow(int facturaId)
        {
            InitializeComponent();
            _facturaId = facturaId;
            Loaded += (_, __) => Cargar();
        }

        private void Cargar()
        {
            try
            {
                using var db = new TallerDbContext();
                _factura = db.Facturas
                    .Include(f => f.Trabajo)
                        .ThenInclude(t => t.Vehiculo)
                            .ThenInclude(v => v.Cliente)
                    .FirstOrDefault(f => f.FacturaID == _facturaId);

                if (_factura == null) { Close(); return; }

                var c = _factura.Trabajo?.Vehiculo?.Cliente;
                var v = _factura.Trabajo?.Vehiculo;
                var t = _factura.Trabajo;

                // Header
                txtNumeroFactura.Text = $"Factura  {_factura.NumeroFactura}";
                txtEstado.Text = _factura.Estado;
                txtFechaEmision.Text = _factura.FechaEmision.ToString("dd/MM/yyyy  HH:mm");

                // Color del header según estado
                headerBorder.Background = _factura.Estado switch
                {
                    "Pagada" => new SolidColorBrush(Color.FromRgb(5, 88, 57)),
                    "Anulada" => new SolidColorBrush(Color.FromRgb(100, 20, 20)),
                    "Pendiente" => new SolidColorBrush(Color.FromRgb(90, 60, 0)),
                    _ => new SolidColorBrush(Color.FromRgb(50, 50, 80))
                };

                // Cliente
                txtCliente.Text = $"{c?.Nombre} {c?.Apellido}";
                txtTelefono.Text = c?.Telefono ?? "—";
                txtCorreo.Text = string.IsNullOrWhiteSpace(c?.Correo) ? "—" : c.Correo;

                // Vehículo
                txtVehiculo.Text = $"{v?.Marca} {v?.Modelo}";
                txtPlaca.Text = v?.Placa ?? "—";
                txtTrabajoId.Text = $"#{t?.TrabajoID}";

                // Descripción
                txtDescripcion.Text = string.IsNullOrWhiteSpace(t?.Descripcion)
                    ? "Sin descripción" : t.Descripcion;

                // Razón social
                if (!string.IsNullOrWhiteSpace(_factura.RazonSocial))
                {
                    txtRazonSocial.Text = _factura.RazonSocial;
                    panelRazonSocial.Visibility = Visibility.Visible;
                }

                // Financiero
                txtSubtotal.Text = $"Bs. {_factura.Subtotal:N2}";
                txtDescuento.Text = _factura.Descuento > 0
                    ? $"- Bs. {_factura.Descuento:N2}" : "—";
                txtTotal.Text = $"Bs. {_factura.Total:N2}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar factura:\n{ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void GenerarPdf_Click(object sender, RoutedEventArgs e)
        {
            if (_factura == null) return;
            FacturacionPdfHelper.GenerarPdf(_factura);
        }

        private void Cerrar_Click(object sender, RoutedEventArgs e) => Close();
    }
}
