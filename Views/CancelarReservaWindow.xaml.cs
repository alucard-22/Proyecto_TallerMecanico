using System.Windows;
using System.Windows.Input;

namespace Proyecto_taller.Views
{
    public partial class CancelarReservaWindow : Window
    {
        public string Motivo { get; private set; } = string.Empty;
        public bool EsNoShow { get; private set; } = false;

        public CancelarReservaWindow()
        {
            InitializeComponent();
        }

        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }

        private void BtnConfirmar_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtMotivo.Text) && chkNoShow.IsChecked == false)
            {
                MessageBox.Show("Ingrese un motivo o marque 'No Show'.",
                    "Advertencia", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Motivo = txtMotivo.Text.Trim();
            EsNoShow = chkNoShow.IsChecked == true;
            DialogResult = true;
        }

        private void BtnVolver_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
