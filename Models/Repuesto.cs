using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Proyecto_taller.Models
{
    public class Repuesto : INotifyPropertyChanged
    {
        private int _stockActual;

        public int RepuestoID { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public decimal PrecioUnitario { get; set; }

        public int StockActual
        {
            get => _stockActual;
            set
            {
                _stockActual = value;
                OnPropertyChanged(nameof(StockActual));
                OnPropertyChanged(nameof(EsStockBajo));
                OnPropertyChanged(nameof(ValorTotal));
            }
        }

        public int StockMinimo { get; set; }
        public DateTime FechaRegistro { get; set; } = DateTime.Now;

        // Propiedades calculadas
        public bool EsStockBajo => StockActual <= StockMinimo && StockActual > 0;
        public decimal ValorTotal => StockActual * PrecioUnitario;

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
