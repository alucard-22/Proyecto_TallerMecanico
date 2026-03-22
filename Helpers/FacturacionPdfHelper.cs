using iTextSharp.text;
using iTextSharp.text.pdf;
using Proyecto_taller.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Proyecto_taller.Helpers
{
    /// <summary>
    /// Genera PDF de factura usando iTextSharp.
    /// Extraído de FacturacionViewModel para poder usarlo también desde DetallesFacturaWindow.
    /// </summary>
    public static class FacturacionPdfHelper
    {
        public static void GenerarPdf(Factura factura)
        {
            if (factura == null) return;

            try
            {
                // ── Preparar ruta ────────────────────────────────────────────────
                var cliente = factura.Trabajo?.Vehiculo?.Cliente;
                string nombreCliente = $"{cliente?.Nombre}_{cliente?.Apellido}";

                // Limpiar caracteres inválidos
                foreach (char c in Path.GetInvalidFileNameChars().Concat(Path.GetInvalidPathChars()).Distinct())
                    nombreCliente = nombreCliente.Replace(c, '_');

                string carpetaBase = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "TallerElChoco_Facturas");
                string carpetaCliente = Path.Combine(carpetaBase, nombreCliente);

                Directory.CreateDirectory(carpetaCliente);

                string nombreArchivo = $"Factura_{factura.NumeroFactura.Replace("/", "-")}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
                string rutaCompleta = Path.Combine(carpetaCliente, nombreArchivo);

                // ── Fuentes ──────────────────────────────────────────────────────
                var fTitulo = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 18, BaseColor.Black);
                var fSubtitulo = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 13, BaseColor.Black);
                var fNormal = FontFactory.GetFont(FontFactory.HELVETICA, 10, BaseColor.Black);
                var fNegrita = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10, BaseColor.Black);
                var fTotal = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 13, BaseColor.Black);

                // ── Documento ────────────────────────────────────────────────────
                var doc = new Document(PageSize.Letter);
                var writer = PdfWriter.GetInstance(doc, new FileStream(rutaCompleta, FileMode.Create));
                doc.Open();

                // Encabezado taller
                var encabezado = new Paragraph("TALLER MECÁNICO EL CHOCO", fTitulo)
                { Alignment = Element.ALIGN_CENTER };
                doc.Add(encabezado);

                var direccion = new Paragraph("Av. América #1234, Cochabamba\nTel: 4-4567890 | contacto@tallerelchoco.com", fNormal)
                { Alignment = Element.ALIGN_CENTER, SpacingAfter = 12f };
                doc.Add(direccion);

                doc.Add(new Paragraph(new Chunk(new iTextSharp.text.pdf.draw.LineSeparator(1f, 100f, BaseColor.Black, Element.ALIGN_CENTER, -1))));
                doc.Add(new Paragraph(" "));

                // Info factura
                var tablaInfo = new PdfPTable(2) { WidthPercentage = 100, SpacingAfter = 14f };
                tablaInfo.SetWidths(new float[] { 50f, 50f });

                var celdaIzq = new PdfPCell { Border = Rectangle.NO_BORDER };
                celdaIzq.AddElement(new Phrase($"Factura: {factura.NumeroFactura}\n", fNegrita));
                celdaIzq.AddElement(new Phrase($"Fecha: {factura.FechaEmision:dd/MM/yyyy HH:mm}\n", fNormal));
                celdaIzq.AddElement(new Phrase($"Estado: {factura.Estado}\n", fNormal));
                tablaInfo.AddCell(celdaIzq);

                var celdaDer = new PdfPCell { Border = Rectangle.NO_BORDER, HorizontalAlignment = Element.ALIGN_RIGHT };
                celdaDer.AddElement(new Phrase($"Trabajo ID: {factura.TrabajoID}\n", fNormal));
                if (!string.IsNullOrEmpty(factura.RazonSocial))
                    celdaDer.AddElement(new Phrase($"Razón Social: {factura.RazonSocial}\n", fNormal));
                tablaInfo.AddCell(celdaDer);
                doc.Add(tablaInfo);

                // Cliente
                AgregarSeccion(doc, "DATOS DEL CLIENTE", fSubtitulo);
                var tablaCliente = new PdfPTable(2) { WidthPercentage = 100, SpacingAfter = 14f };
                tablaCliente.SetWidths(new float[] { 30f, 70f });
                AgregarFila(tablaCliente, "Cliente:", $"{cliente?.Nombre} {cliente?.Apellido}", fNegrita, fNormal);
                AgregarFila(tablaCliente, "Teléfono:", cliente?.Telefono ?? "N/A", fNegrita, fNormal);
                AgregarFila(tablaCliente, "Email:", cliente?.Correo ?? "N/A", fNegrita, fNormal);
                doc.Add(tablaCliente);

                // Vehículo
                AgregarSeccion(doc, "DATOS DEL VEHÍCULO", fSubtitulo);
                var tablaVehiculo = new PdfPTable(2) { WidthPercentage = 100, SpacingAfter = 14f };
                tablaVehiculo.SetWidths(new float[] { 30f, 70f });
                var v = factura.Trabajo?.Vehiculo;
                AgregarFila(tablaVehiculo, "Marca:", v?.Marca ?? "N/A", fNegrita, fNormal);
                AgregarFila(tablaVehiculo, "Modelo:", v?.Modelo ?? "N/A", fNegrita, fNormal);
                AgregarFila(tablaVehiculo, "Placa:", v?.Placa ?? "N/A", fNegrita, fNormal);
                AgregarFila(tablaVehiculo, "Año:", v?.Anio?.ToString() ?? "N/A", fNegrita, fNormal);
                doc.Add(tablaVehiculo);

                // Descripción trabajo
                if (!string.IsNullOrWhiteSpace(factura.Trabajo?.Descripcion))
                {
                    AgregarSeccion(doc, "DESCRIPCIÓN DEL TRABAJO", fSubtitulo);
                    doc.Add(new Paragraph(factura.Trabajo.Descripcion, fNormal) { SpacingAfter = 16f });
                }

                // Costos
                AgregarSeccion(doc, "DETALLE DE COSTOS", fSubtitulo);
                var tablaCostos = new PdfPTable(2)
                {
                    WidthPercentage = 55,
                    HorizontalAlignment = Element.ALIGN_RIGHT,
                    SpacingAfter = 16f
                };

                AgregarFila(tablaCostos, "Subtotal:", $"Bs. {factura.Subtotal:N2}", fNegrita, fNormal);
                AgregarFila(tablaCostos, "Descuento:", $"Bs. {factura.Descuento:N2}", fNegrita, fNormal);

                // Separador
                var linea = new PdfPCell(new Phrase(" ")) { Colspan = 2, Border = Rectangle.TOP_BORDER, BorderWidthTop = 1.5f };
                tablaCostos.AddCell(linea);

                // Total
                var cTotal1 = new PdfPCell(new Phrase("TOTAL:", fTotal)) { Border = Rectangle.NO_BORDER, PaddingTop = 4f };
                var cTotal2 = new PdfPCell(new Phrase($"Bs. {factura.Total:N2}", fTotal))
                {
                    Border = Rectangle.NO_BORDER,
                    HorizontalAlignment = Element.ALIGN_RIGHT,
                    PaddingTop = 4f
                };
                tablaCostos.AddCell(cTotal1);
                tablaCostos.AddCell(cTotal2);
                doc.Add(tablaCostos);

                // Pie
                doc.Add(new Paragraph(" "));
                doc.Add(new Paragraph(new Chunk(new iTextSharp.text.pdf.draw.LineSeparator(0.5f, 100f, BaseColor.Gray, Element.ALIGN_CENTER, -1))));
                doc.Add(new Paragraph($"Documento generado el {DateTime.Now:dd/MM/yyyy HH:mm} — Taller Mecánico El Choco",
                    FontFactory.GetFont(FontFactory.HELVETICA, 8, BaseColor.Gray))
                {
                    Alignment = Element.ALIGN_CENTER,
                    SpacingBefore = 8f
                });

                doc.Close();
                writer.Close();

                // Preguntar al usuario
                var r = MessageBox.Show(
                    $"✅ PDF generado exitosamente\n\n📄 {nombreArchivo}\n📁 {carpetaCliente}\n\n¿Abrir el archivo?",
                    "PDF Generado", MessageBoxButton.YesNo, MessageBoxImage.Information);

                if (r == MessageBoxResult.Yes)
                    Process.Start(new ProcessStartInfo(rutaCompleta) { UseShellExecute = true });
                else
                {
                    var abrirCarpeta = MessageBox.Show("¿Abrir la carpeta?", "Abrir Carpeta",
                        MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (abrirCarpeta == MessageBoxResult.Yes)
                        Process.Start(new ProcessStartInfo(carpetaCliente) { UseShellExecute = true });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al generar PDF:\n{ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ── Helpers ──────────────────────────────────────────────────────────────

        private static void AgregarSeccion(Document doc, string titulo, Font fuente)
        {
            var p = new Paragraph(titulo, fuente) { SpacingBefore = 8f, SpacingAfter = 8f };
            doc.Add(p);
        }

        private static void AgregarFila(PdfPTable tabla, string etiqueta, string valor, Font fEtiqueta, Font fValor)
        {
            var c1 = new PdfPCell(new Phrase(etiqueta, fEtiqueta)) { Border = Rectangle.NO_BORDER, PaddingBottom = 4f };
            var c2 = new PdfPCell(new Phrase(valor, fValor)) { Border = Rectangle.NO_BORDER, PaddingBottom = 4f };
            tabla.AddCell(c1);
            tabla.AddCell(c2);
        }
    }
}
