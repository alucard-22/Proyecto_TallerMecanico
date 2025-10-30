using Proyecto_taller.Data;
using Proyecto_taller.Models;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using System.Windows;
using System.IO;
using iTextSharp.text;
using iTextSharp.text.pdf;
using System.Diagnostics;

namespace Proyecto_taller.ViewModels
{
    public class FacturacionViewModel : INotifyPropertyChanged
    {
        private Factura _facturaSeleccionada;
        private bool _filtroTodas = true;
        private bool _filtroPagadas;
        private bool _filtroPendientes;
        private bool _filtroEsteMes;
        private decimal _totalFacturado;
        private int _totalFacturas;

        public ObservableCollection<Factura> Facturas { get; set; }

        public Factura FacturaSeleccionada
        {
            get => _facturaSeleccionada;
            set
            {
                _facturaSeleccionada = value;
                OnPropertyChanged();
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public decimal TotalFacturado
        {
            get => _totalFacturado;
            set
            {
                _totalFacturado = value;
                OnPropertyChanged();
            }
        }

        public int TotalFacturas
        {
            get => _totalFacturas;
            set
            {
                _totalFacturas = value;
                OnPropertyChanged();
            }
        }

        // Propiedades para los filtros
        public bool FiltroTodas
        {
            get => _filtroTodas;
            set
            {
                _filtroTodas = value;
                OnPropertyChanged();
                if (value) AplicarFiltro("Todas");
            }
        }

        public bool FiltroPagadas
        {
            get => _filtroPagadas;
            set
            {
                _filtroPagadas = value;
                OnPropertyChanged();
                if (value) AplicarFiltro("Pagada");
            }
        }

        public bool FiltroPendientes
        {
            get => _filtroPendientes;
            set
            {
                _filtroPendientes = value;
                OnPropertyChanged();
                if (value) AplicarFiltro("Pendiente");
            }
        }

        public bool FiltroEsteMes
        {
            get => _filtroEsteMes;
            set
            {
                _filtroEsteMes = value;
                OnPropertyChanged();
                if (value) AplicarFiltro("EsteMes");
            }
        }

        public ICommand CargarFacturasCommand { get; }
        public ICommand NuevaFacturaCommand { get; }
        public ICommand VerDetalleCommand { get; }
        public ICommand ImprimirFacturaCommand { get; }
        public ICommand AnularFacturaCommand { get; }
        public ICommand EliminarFacturaCommand { get; }

        public FacturacionViewModel()
        {
            Facturas = new ObservableCollection<Factura>();

            CargarFacturasCommand = new RelayCommand(CargarFacturas);
            NuevaFacturaCommand = new RelayCommand(NuevaFactura);
            VerDetalleCommand = new RelayCommand(VerDetalle, () => FacturaSeleccionada != null);
            ImprimirFacturaCommand = new RelayCommand(ImprimirFactura, () => FacturaSeleccionada != null);
            AnularFacturaCommand = new RelayCommand(AnularFactura, () => FacturaSeleccionada != null && FacturaSeleccionada.Estado != "Anulada");
            EliminarFacturaCommand = new RelayCommand(EliminarFactura, () => FacturaSeleccionada != null);

            CargarFacturas();
        }

        private void CargarFacturas()
        {
            Facturas.Clear();
            using var db = new TallerDbContext();

            var facturas = db.Facturas
                .Include(f => f.Trabajo)
                    .ThenInclude(t => t.Vehiculo)
                        .ThenInclude(v => v.Cliente)
                .OrderByDescending(f => f.FechaEmision)
                .ToList();

            foreach (var factura in facturas)
            {
                Facturas.Add(factura);
            }

            ActualizarEstadisticas();
        }

        private void AplicarFiltro(string filtro)
        {
            Facturas.Clear();
            using var db = new TallerDbContext();

            var query = db.Facturas
                .Include(f => f.Trabajo)
                    .ThenInclude(t => t.Vehiculo)
                        .ThenInclude(v => v.Cliente)
                .AsQueryable();

            if (filtro == "Pagada")
            {
                query = query.Where(f => f.Estado == "Pagada");
            }
            else if (filtro == "Pendiente")
            {
                query = query.Where(f => f.Estado == "Pendiente");
            }
            else if (filtro == "EsteMes")
            {
                var primerDiaMes = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                var ultimoDiaMes = primerDiaMes.AddMonths(1).AddDays(-1);
                query = query.Where(f => f.FechaEmision >= primerDiaMes && f.FechaEmision <= ultimoDiaMes);
            }

            var facturas = query.OrderByDescending(f => f.FechaEmision).ToList();

            foreach (var factura in facturas)
            {
                Facturas.Add(factura);
            }

            ActualizarEstadisticas();
        }

        private void ActualizarEstadisticas()
        {
            using var db = new TallerDbContext();
            TotalFacturas = db.Facturas.Count(f => f.Estado != "Anulada");
            TotalFacturado = db.Facturas.Where(f => f.Estado == "Pagada").Sum(f => f.Total);
        }

        private void NuevaFactura()
        {
            using var db = new TallerDbContext();

            // Buscar un trabajo finalizado sin factura
            var trabajoSinFactura = db.Trabajos
                .Include(t => t.Vehiculo)
                    .ThenInclude(v => v.Cliente)
                .Where(t => t.Estado == "Finalizado" && t.PrecioFinal != null)
                .FirstOrDefault(t => !db.Facturas.Any(f => f.TrabajoID == t.TrabajoID));

            if (trabajoSinFactura == null)
            {
                MessageBox.Show(
                    "No hay trabajos finalizados sin factura.",
                    "Sin Trabajos",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            // Generar número de factura
            var ultimaFactura = db.Facturas.OrderByDescending(f => f.FacturaID).FirstOrDefault();
            int numeroConsecutivo = ultimaFactura != null ? ultimaFactura.FacturaID + 1 : 1;
            string numeroFactura = $"FACT-{DateTime.Now.Year}-{numeroConsecutivo:D3}";

            // Calcular montos
            decimal subtotal = trabajoSinFactura.PrecioFinal ?? 0;
            decimal iva = subtotal * 0.13m; // IVA 13%
            decimal total = subtotal + iva;

            var nueva = new Factura
            {
                TrabajoID = trabajoSinFactura.TrabajoID,
                NumeroFactura = numeroFactura,
                FechaEmision = DateTime.Now,
                Subtotal = subtotal,
                Descuento = 0,
                Total = total,
                Estado = "Pagada"
            };

            db.Facturas.Add(nueva);
            db.SaveChanges();

            var facturaConRelaciones = db.Facturas
                .Include(f => f.Trabajo)
                    .ThenInclude(t => t.Vehiculo)
                        .ThenInclude(v => v.Cliente)
                .FirstOrDefault(f => f.FacturaID == nueva.FacturaID);

            if (facturaConRelaciones != null)
            {
                Facturas.Insert(0, facturaConRelaciones);
            }

            ActualizarEstadisticas();

            MessageBox.Show(
                $"Factura {numeroFactura} creada exitosamente.\nTotal: Bs. {total:N2}",
                "Factura Creada",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void VerDetalle()
        {
            if (FacturaSeleccionada == null) return;

            var mensaje = $"FACTURA: {FacturaSeleccionada.NumeroFactura}\n" +
                         $"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n\n" +
                         $"Fecha: {FacturaSeleccionada.FechaEmision:dd/MM/yyyy HH:mm}\n" +
                         $"Cliente: {FacturaSeleccionada.Trabajo?.Vehiculo?.Cliente?.Nombre} {FacturaSeleccionada.Trabajo?.Vehiculo?.Cliente?.Apellido}\n" +
                         $"Vehículo: {FacturaSeleccionada.Trabajo?.Vehiculo?.Marca} {FacturaSeleccionada.Trabajo?.Vehiculo?.Modelo}\n" +
                         $"Placa: {FacturaSeleccionada.Trabajo?.Vehiculo?.Placa}\n\n";

            if (!string.IsNullOrEmpty(FacturaSeleccionada.NIT))
            {
                mensaje += $"NIT: {FacturaSeleccionada.NIT}\n" +
                          $"Razón Social: {FacturaSeleccionada.RazonSocial}\n\n";
            }

            mensaje += $"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n" +
                      $"Subtotal:     Bs. {FacturaSeleccionada.Subtotal:N2}\n" +
                      $"Descuento:    Bs. {FacturaSeleccionada.Descuento:N2}\n" +
                      $"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n" +
                      $"TOTAL:        Bs. {FacturaSeleccionada.Total:N2}\n\n" +
                      $"Estado: {FacturaSeleccionada.Estado}";

            MessageBox.Show(
                mensaje,
                "Detalle de Factura",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void ImprimirFactura()
        {
            if (FacturaSeleccionada == null) return;

            try
            {
                // Obtener el nombre del cliente
                string nombreCliente = $"{FacturaSeleccionada.Trabajo?.Vehiculo?.Cliente?.Nombre}_{FacturaSeleccionada.Trabajo?.Vehiculo?.Cliente?.Apellido}";

                // Limpiar caracteres no válidos para nombres de carpeta
                char[] caracteresInvalidos = Path.GetInvalidFileNameChars().Concat(Path.GetInvalidPathChars()).Distinct().ToArray();
                foreach (char c in caracteresInvalidos)
                {
                    nombreCliente = nombreCliente.Replace(c, '_');
                }

                // Crear estructura de carpetas en Documentos
                string carpetaDocumentos = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                string carpetaBase = Path.Combine(carpetaDocumentos, "TallerElChoco_Facturas");
                string carpetaCliente = Path.Combine(carpetaBase, nombreCliente);

                // Crear las carpetas si no existen
                if (!Directory.Exists(carpetaBase))
                {
                    Directory.CreateDirectory(carpetaBase);
                }

                if (!Directory.Exists(carpetaCliente))
                {
                    Directory.CreateDirectory(carpetaCliente);
                }

                // Nombre del archivo
                string nombreArchivo = $"Factura_{FacturaSeleccionada.NumeroFactura.Replace("/", "-")}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
                string rutaCompleta = Path.Combine(carpetaCliente, nombreArchivo);

                // Crear el documento PDF
                Document documento = new Document(PageSize.Letter);
                PdfWriter writer = PdfWriter.GetInstance(documento, new FileStream(rutaCompleta, FileMode.Create));

                documento.Open();

                // Fuentes
                var fuenteTitulo = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 18, BaseColor.Black);
                var fuenteSubtitulo = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 14, BaseColor.Black);
                var fuenteNormal = FontFactory.GetFont(FontFactory.HELVETICA, 10, BaseColor.Black);
                var fuenteNegrita = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10, BaseColor.Black);
                var fuenteTotal = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12, BaseColor.Black);

                // ===== ENCABEZADO =====
                Paragraph encabezado = new Paragraph("TALLER MECÁNICO EL CHOCO", fuenteTitulo);
                encabezado.Alignment = Element.ALIGN_CENTER;
                documento.Add(encabezado);

                Paragraph direccion = new Paragraph("Av. América #1234, Cochabamba\nTel: 4-4567890 | Email: contacto@tallerelchoco.com", fuenteNormal);
                direccion.Alignment = Element.ALIGN_CENTER;
                direccion.SpacingAfter = 20f;
                documento.Add(direccion);

                // Línea separadora
                documento.Add(new Paragraph(new Chunk(new iTextSharp.text.pdf.draw.LineSeparator(1f, 100f, BaseColor.Black, Element.ALIGN_CENTER, -1))));
                documento.Add(new Paragraph(" "));

                // ===== INFORMACIÓN DE LA FACTURA =====
                PdfPTable tablaInfo = new PdfPTable(2);
                tablaInfo.WidthPercentage = 100;
                tablaInfo.SetWidths(new float[] { 50f, 50f });
                tablaInfo.SpacingAfter = 15f;

                // Columna izquierda
                PdfPCell celdaIzq = new PdfPCell();
                celdaIzq.Border = Rectangle.NO_BORDER;
                celdaIzq.AddElement(new Phrase($"Factura: {FacturaSeleccionada.NumeroFactura}\n", fuenteNegrita));
                celdaIzq.AddElement(new Phrase($"Fecha: {FacturaSeleccionada.FechaEmision:dd/MM/yyyy HH:mm}\n", fuenteNormal));
                celdaIzq.AddElement(new Phrase($"Estado: {FacturaSeleccionada.Estado}\n", fuenteNormal));
                tablaInfo.AddCell(celdaIzq);

                // Columna derecha
                PdfPCell celdaDer = new PdfPCell();
                celdaDer.Border = Rectangle.NO_BORDER;
                celdaDer.HorizontalAlignment = Element.ALIGN_RIGHT;
                celdaDer.AddElement(new Phrase($"Trabajo ID: {FacturaSeleccionada.TrabajoID}\n", fuenteNormal));
                celdaDer.AddElement(new Phrase($"NIT: {FacturaSeleccionada.NIT ?? "N/A"}\n", fuenteNormal));
                if (!string.IsNullOrEmpty(FacturaSeleccionada.RazonSocial))
                {
                    celdaDer.AddElement(new Phrase($"Razón Social: {FacturaSeleccionada.RazonSocial}\n", fuenteNormal));
                }
                tablaInfo.AddCell(celdaDer);

                documento.Add(tablaInfo);

                // ===== DATOS DEL CLIENTE =====
                Paragraph tituloCliente = new Paragraph("DATOS DEL CLIENTE", fuenteSubtitulo);
                tituloCliente.SpacingBefore = 10f;
                tituloCliente.SpacingAfter = 10f;
                documento.Add(tituloCliente);

                PdfPTable tablaCliente = new PdfPTable(2);
                tablaCliente.WidthPercentage = 100;
                tablaCliente.SetWidths(new float[] { 30f, 70f });
                tablaCliente.SpacingAfter = 15f;

                AgregarFilaTabla(tablaCliente, "Cliente:",
                    $"{FacturaSeleccionada.Trabajo?.Vehiculo?.Cliente?.Nombre} {FacturaSeleccionada.Trabajo?.Vehiculo?.Cliente?.Apellido}",
                    fuenteNegrita, fuenteNormal);
                AgregarFilaTabla(tablaCliente, "Teléfono:",
                    FacturaSeleccionada.Trabajo?.Vehiculo?.Cliente?.Telefono ?? "N/A",
                    fuenteNegrita, fuenteNormal);
                AgregarFilaTabla(tablaCliente, "Email:",
                    FacturaSeleccionada.Trabajo?.Vehiculo?.Cliente?.Correo ?? "N/A",
                    fuenteNegrita, fuenteNormal);

                documento.Add(tablaCliente);

                // ===== DATOS DEL VEHÍCULO =====
                Paragraph tituloVehiculo = new Paragraph("DATOS DEL VEHÍCULO", fuenteSubtitulo);
                tituloVehiculo.SpacingBefore = 10f;
                tituloVehiculo.SpacingAfter = 10f;
                documento.Add(tituloVehiculo);

                PdfPTable tablaVehiculo = new PdfPTable(2);
                tablaVehiculo.WidthPercentage = 100;
                tablaVehiculo.SetWidths(new float[] { 30f, 70f });
                tablaVehiculo.SpacingAfter = 15f;

                AgregarFilaTabla(tablaVehiculo, "Marca:",
                    FacturaSeleccionada.Trabajo?.Vehiculo?.Marca ?? "N/A",
                    fuenteNegrita, fuenteNormal);
                AgregarFilaTabla(tablaVehiculo, "Modelo:",
                    FacturaSeleccionada.Trabajo?.Vehiculo?.Modelo ?? "N/A",
                    fuenteNegrita, fuenteNormal);
                AgregarFilaTabla(tablaVehiculo, "Placa:",
                    FacturaSeleccionada.Trabajo?.Vehiculo?.Placa ?? "N/A",
                    fuenteNegrita, fuenteNormal);
                AgregarFilaTabla(tablaVehiculo, "Año:",
                    FacturaSeleccionada.Trabajo?.Vehiculo?.Anio?.ToString() ?? "N/A",
                    fuenteNegrita, fuenteNormal);

                documento.Add(tablaVehiculo);

                // ===== DESCRIPCIÓN DEL TRABAJO =====
                if (!string.IsNullOrEmpty(FacturaSeleccionada.Trabajo?.Descripcion))
                {
                    Paragraph tituloTrabajo = new Paragraph("DESCRIPCIÓN DEL TRABAJO", fuenteSubtitulo);
                    tituloTrabajo.SpacingBefore = 10f;
                    tituloTrabajo.SpacingAfter = 10f;
                    documento.Add(tituloTrabajo);

                    Paragraph descripcion = new Paragraph(FacturaSeleccionada.Trabajo.Descripcion, fuenteNormal);
                    descripcion.SpacingAfter = 20f;
                    documento.Add(descripcion);
                }

                // ===== DETALLE DE COSTOS =====
                Paragraph tituloCostos = new Paragraph("DETALLE DE COSTOS", fuenteSubtitulo);
                tituloCostos.SpacingBefore = 20f;
                tituloCostos.SpacingAfter = 10f;
                documento.Add(tituloCostos);

                PdfPTable tablaCostos = new PdfPTable(2);
                tablaCostos.WidthPercentage = 60;
                tablaCostos.HorizontalAlignment = Element.ALIGN_RIGHT;
                tablaCostos.SpacingAfter = 20f;

                AgregarFilaTabla(tablaCostos, "Subtotal:",
                    $"Bs. {FacturaSeleccionada.Subtotal:N2}",
                    fuenteNegrita, fuenteNormal);
                AgregarFilaTabla(tablaCostos, "Descuento:",
                    $"Bs. {FacturaSeleccionada.Descuento:N2}",
                    fuenteNegrita, fuenteNormal);
               
                // Línea separadora
                PdfPCell celdaLinea = new PdfPCell(new Phrase(" "));
                celdaLinea.Colspan = 2;
                celdaLinea.Border = Rectangle.TOP_BORDER;
                celdaLinea.BorderWidthTop = 2f;
                tablaCostos.AddCell(celdaLinea);

                // Total
                PdfPCell celdaTotalLabel = new PdfPCell(new Phrase("TOTAL:", fuenteTotal));
                celdaTotalLabel.Border = Rectangle.NO_BORDER;
                celdaTotalLabel.HorizontalAlignment = Element.ALIGN_LEFT;
                celdaTotalLabel.PaddingTop = 5f;
                tablaCostos.AddCell(celdaTotalLabel);

                PdfPCell celdaTotalValor = new PdfPCell(new Phrase($"Bs. {FacturaSeleccionada.Total:N2}", fuenteTotal));
                celdaTotalValor.Border = Rectangle.NO_BORDER;
                celdaTotalValor.HorizontalAlignment = Element.ALIGN_RIGHT;
                celdaTotalValor.PaddingTop = 5f;
                tablaCostos.AddCell(celdaTotalValor);

                documento.Add(tablaCostos);

                // ===== NOTA AL PIE =====
                documento.Add(new Paragraph(" "));
                documento.Add(new Paragraph(new Chunk(new iTextSharp.text.pdf.draw.LineSeparator(0.5f, 100f, BaseColor.Gray, Element.ALIGN_CENTER, -1))));

                Paragraph notaPie = new Paragraph(
                    $"Documento generado el {DateTime.Now:dd/MM/yyyy HH:mm}\n" +
                    "Gracias por su confianza - Taller El Choco",
                    FontFactory.GetFont(FontFactory.HELVETICA, 8, BaseColor.Gray));
                notaPie.Alignment = Element.ALIGN_CENTER;
                notaPie.SpacingBefore = 10f;
                documento.Add(notaPie);

                // Cerrar documento
                documento.Close();
                writer.Close();

                // Mostrar mensaje de éxito con información de la ubicación
                var resultado = MessageBox.Show(
                    $"✅ PDF generado exitosamente\n\n" +
                    $"📁 Carpeta: {Path.GetFileName(carpetaCliente)}\n" +
                    $"📄 Archivo: {nombreArchivo}\n\n" +
                    $"Ruta completa:\n{rutaCompleta}\n\n" +
                    $"¿Desea abrir el archivo?",
                    "PDF Generado",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Information);

                if (resultado == MessageBoxResult.Yes)
                {
                    Process.Start(new ProcessStartInfo(rutaCompleta) { UseShellExecute = true });
                }
                else
                {
                    // Preguntar si desea abrir la carpeta del cliente
                    var abrirCarpeta = MessageBox.Show(
                        "¿Desea abrir la carpeta del cliente?",
                        "Abrir Carpeta",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (abrirCarpeta == MessageBoxResult.Yes)
                    {
                        Process.Start(new ProcessStartInfo(carpetaCliente) { UseShellExecute = true });
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error al generar el PDF:\n{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void AgregarFilaTabla(PdfPTable tabla, string etiqueta, string valor, Font fuenteEtiqueta, Font fuenteValor)
        {
            PdfPCell celdaEtiqueta = new PdfPCell(new Phrase(etiqueta, fuenteEtiqueta));
            celdaEtiqueta.Border = Rectangle.NO_BORDER;
            celdaEtiqueta.PaddingBottom = 5f;
            tabla.AddCell(celdaEtiqueta);

            PdfPCell celdaValor = new PdfPCell(new Phrase(valor, fuenteValor));
            celdaValor.Border = Rectangle.NO_BORDER;
            celdaValor.PaddingBottom = 5f;
            tabla.AddCell(celdaValor);
        }

        private void AnularFactura()
        {
            if (FacturaSeleccionada == null || FacturaSeleccionada.Estado == "Anulada")
                return;

            var resultado = MessageBox.Show(
                $"¿Está seguro de anular la factura {FacturaSeleccionada.NumeroFactura}?\n\n" +
                $"Esta acción no se puede deshacer.",
                "Anular Factura",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (resultado == MessageBoxResult.Yes)
            {
                using var db = new TallerDbContext();
                var factura = db.Facturas.Find(FacturaSeleccionada.FacturaID);

                if (factura != null)
                {
                    factura.Estado = "Anulada";
                    db.SaveChanges();

                    FacturaSeleccionada.Estado = "Anulada";
                    OnPropertyChanged(nameof(Facturas));
                    ActualizarEstadisticas();
                    CommandManager.InvalidateRequerySuggested();

                    MessageBox.Show(
                        "Factura anulada exitosamente.",
                        "Factura Anulada",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
        }

        private void EliminarFactura()
        {
            if (FacturaSeleccionada == null) return;

            var resultado = MessageBox.Show(
                $"¿Está seguro de eliminar la factura {FacturaSeleccionada.NumeroFactura}?\n\n" +
                $"ADVERTENCIA: Esta acción eliminará permanentemente el registro.",
                "Confirmar Eliminación",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (resultado == MessageBoxResult.Yes)
            {
                using var db = new TallerDbContext();
                var factura = db.Facturas.Find(FacturaSeleccionada.FacturaID);

                if (factura != null)
                {
                    db.Facturas.Remove(factura);
                    db.SaveChanges();
                    Facturas.Remove(FacturaSeleccionada);
                    ActualizarEstadisticas();
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}