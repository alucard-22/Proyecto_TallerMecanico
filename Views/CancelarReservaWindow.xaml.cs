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
    public partial class CancelarReservaWindow : Window
    {
        // Propiedades que lee Reservas.xaml.cs
        public string Motivo { get; private set; } = string.Empty;
        public bool EsNoShow { get; private set; } = false;

        public CancelarReservaWindow()
        {
            InitializeComponent();
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
            DialogResult = true; // ← Esto es lo que lee ventana.ShowDialog() == true
        }

        private void BtnVolver_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
