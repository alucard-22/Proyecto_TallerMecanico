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
    public enum TipoMovimiento { Entrada, Salida }

    public partial class EntradaSalidaStockWindow : Window
    {
        private readonly Repuesto _repuesto;
        private readonly TipoMovimiento _tipo;

        // Resultado que lee el ViewModel tras ShowDialog() == true
        public int CantidadMovida { get; private set; }
        public string Motivo { get; private set; } = string.Empty;

        public EntradaSalidaStockWindow(Repuesto repuesto, TipoMovimiento tipo)
        {
            InitializeComponent();
            _repuesto = repuesto;
            _tipo = tipo;
            ConfigurarUI();
            txtCantidad.Focus();
            txtCantidad.SelectAll();
        }

        // ─── Configuración visual según tipo ───────────────────────────────────

        private void ConfigurarUI()
        {
            if (_tipo == TipoMovimiento.Entrada)
            {
                // Verde para entradas
                rootBorder.BorderBrush = (Brush)FindResource("ColorSuccess");
                headerBorder.Background = new SolidColorBrush(Color.FromRgb(24, 40, 32));
                txtTitulo.Text = "📥  Entrada de Stock";
                txtTitulo.Foreground = (Brush)FindResource("ColorSuccess");
                infoPanel.BorderBrush = (Brush)FindResource("BorderColor");
                infoPanel.Background = new SolidColorBrush(Color.FromRgb(24, 40, 32));
                resultadoPanel.BorderBrush = (Brush)FindResource("ColorSuccess");
                resultadoPanel.Background = new SolidColorBrush(Color.FromRgb(24, 40, 32));
                btnConfirmar.Content = "✅  Confirmar Entrada";
                btnMas.Style = (Style)FindResource("BtnSuccess");
            }
            else
            {
                // Naranja/warning para salidas
                rootBorder.BorderBrush = (Brush)FindResource("ColorWarning");
                headerBorder.Background = new SolidColorBrush(Color.FromRgb(45, 32, 10));
                txtTitulo.Text = "📤  Salida de Stock";
                txtTitulo.Foreground = (Brush)FindResource("ColorWarning");
                infoPanel.BorderBrush = (Brush)FindResource("BorderColor");
                infoPanel.Background = new SolidColorBrush(Color.FromRgb(45, 32, 10));
                resultadoPanel.BorderBrush = (Brush)FindResource("ColorWarning");
                resultadoPanel.Background = new SolidColorBrush(Color.FromRgb(45, 32, 10));
                btnConfirmar.Content = "📤  Confirmar Salida";
                btnMas.Style = (Style)FindResource("BtnWarning");
            }

            // Datos del repuesto
            txtRepuestoNombre.Text = _repuesto.Nombre;
            txtStockActual.Text = _repuesto.StockActual.ToString();
            txtStockMinimo.Text = _repuesto.StockMinimo.ToString();
            txtPrecioUnitario.Text = $"Bs. {_repuesto.PrecioUnitario:N2}";

            ActualizarResultado();
        }

        // ─── Controles de cantidad ─────────────────────────────────────────────

        private void BtnMas_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(txtCantidad.Text, out int v))
                txtCantidad.Text = (v + 1).ToString();
        }

        private void BtnMenos_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(txtCantidad.Text, out int v) && v > 1)
                txtCantidad.Text = (v - 1).ToString();
        }

        private void TxtCantidad_Changed(object sender, System.Windows.Controls.TextChangedEventArgs e)
            => ActualizarResultado();

        // ─── Proyección de stock resultante ───────────────────────────────────

        private void ActualizarResultado()
        {
            if (txtStockResultante == null) return;

            if (!int.TryParse(txtCantidad.Text, out int cant) || cant <= 0)
            {
                txtStockResultante.Text = "—";
                txtStockResultante.Foreground = (Brush)FindResource("TextMuted");
                return;
            }

            int resultado = _tipo == TipoMovimiento.Entrada
                ? _repuesto.StockActual + cant
                : _repuesto.StockActual - cant;

            txtStockResultante.Text = resultado.ToString();

            // Colorear según si queda bien, bajo o negativo
            if (resultado < 0)
                txtStockResultante.Foreground = (Brush)FindResource("ColorDanger");
            else if (resultado <= _repuesto.StockMinimo)
                txtStockResultante.Foreground = (Brush)FindResource("ColorWarning");
            else
                txtStockResultante.Foreground = (Brush)FindResource("ColorSuccess");
        }

        // ─── Confirmar ────────────────────────────────────────────────────────

        private void Confirmar_Click(object sender, RoutedEventArgs e)
        {
            // Validar que sea un número entero positivo
            if (!int.TryParse(txtCantidad.Text.Trim(), out int cant) || cant <= 0)
            {
                MessageBox.Show("Ingresa una cantidad válida mayor a 0.",
                    "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtCantidad.Focus();
                return;
            }

            // Para salida: validar que no supere el stock disponible
            if (_tipo == TipoMovimiento.Salida && cant > _repuesto.StockActual)
            {
                MessageBox.Show(
                    $"No hay suficiente stock.\n\n" +
                    $"Stock disponible: {_repuesto.StockActual}\n" +
                    $"Cantidad solicitada: {cant}",
                    "Stock insuficiente",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtCantidad.Focus();
                return;
            }

            CantidadMovida = cant;
            Motivo = txtMotivo.Text.Trim();
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