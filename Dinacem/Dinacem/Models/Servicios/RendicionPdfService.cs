using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Dinacem.Models.Servicios
{
    public class RendicionPdfService
    {
        private readonly IWebHostEnvironment _environment;

        public RendicionPdfService(
            IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        public async Task<ResultadoPdfRendicion> GenerarAsync(
            Rendicion rendicion,
            List<Gasto> gastos,
            DevolucionSaldo? devolucion)
        {
            ArgumentNullException.ThrowIfNull(rendicion);

            var nombreEmpleado =
                $"{rendicion.Usuario?.Nombres} " +
                $"{rendicion.Usuario?.Apellidos}".Trim();

            if (string.IsNullOrWhiteSpace(nombreEmpleado))
            {
                nombreEmpleado =
                    $"Usuario {rendicion.IdUsuario}";
            }

            var totalBase =
                gastos.Sum(g => g.ValorVenta);

            var totalIgv =
                gastos.Sum(g => g.IGV);

            var totalRendido =
                gastos.Sum(g => g.MontoTotal);

            var nombreSeguroEmpleado =
                LimpiarNombreArchivo(nombreEmpleado);

            var nombreArchivo =
                $"Liquidacion-{rendicion.IdRendicion}-" +
                $"{nombreSeguroEmpleado}.pdf";

            var carpeta = Path.Combine(
                _environment.WebRootPath,
                "liquidaciones");

            Directory.CreateDirectory(carpeta);

            var rutaFisica = Path.Combine(
                carpeta,
                nombreArchivo);

            var rutaPublica =
                $"/liquidaciones/{nombreArchivo}";

            var documento = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(30);
                    page.DefaultTextStyle(text =>
                        text.FontSize(10));

                    page.Header()
                        .Column(header =>
                        {
                            header.Spacing(4);

                            header.Item()
                                .Text("LIQUIDACIÓN DE GASTOS DE VIÁTICOS")
                                .FontSize(18)
                                .Bold()
                                .FontColor(Colors.Blue.Darken2);

                            header.Item()
                                .Text(
                                    $"Fecha del reporte: " +
                                    $"{DateTime.Now:dd/MM/yyyy}");

                            header.Item()
                                .Text(
                                    $"Periodo: " +
                                    $"{rendicion.FechaInicio:dd/MM/yyyy} " +
                                    $"al {rendicion.FechaFin:dd/MM/yyyy}");

                            header.Item()
                                .PaddingTop(8)
                                .Text(text =>
                                {
                                    text.Span("Empleado rendidor: ")
                                        .Bold();

                                    text.Span(nombreEmpleado);
                                });

                            header.Item()
                                .Row(row =>
                                {
                                    row.RelativeItem()
                                        .Text(text =>
                                        {
                                            text.Span("Correo: ")
                                                .Bold();

                                            text.Span(
                                                rendicion.Usuario?.Correo
                                                ?? "-");
                                        });

                                    row.RelativeItem()
                                        .Text(text =>
                                        {
                                            text.Span("Celular: ")
                                                .Bold();

                                            text.Span(
                                                rendicion.Usuario?.Celular
                                                ?? "-");
                                        });
                                });
                        });

                    page.Content()
                        .PaddingVertical(15)
                        .Column(content =>
                        {
                            content.Spacing(15);

                            content.Item()
                                .Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.ConstantColumn(62);
                                        columns.RelativeColumn(1.2f);
                                        columns.RelativeColumn(1.4f);
                                        columns.RelativeColumn(1.6f);
                                        columns.ConstantColumn(65);
                                        columns.ConstantColumn(55);
                                        columns.ConstantColumn(65);
                                    });

                                    table.Header(header =>
                                    {
                                        CeldaCabecera(
                                            header.Cell(),
                                            "Fecha");

                                        CeldaCabecera(
                                            header.Cell(),
                                            "Tipo gasto");

                                        CeldaCabecera(
                                            header.Cell(),
                                            "Comprobante");

                                        CeldaCabecera(
                                            header.Cell(),
                                            "Detalle");

                                        CeldaCabecera(
                                            header.Cell(),
                                            "Base (S/)");

                                        CeldaCabecera(
                                            header.Cell(),
                                            "IGV (S/)");

                                        CeldaCabecera(
                                            header.Cell(),
                                            "Total (S/)");
                                    });

                                    foreach (var gasto in gastos)
                                    {
                                        CeldaDetalle(
                                            table.Cell(),
                                            gasto.Fecha
                                                .ToString("dd/MM/yyyy"));

                                        CeldaDetalle(
                                            table.Cell(),
                                            gasto.TipoGasto?.Nombre
                                            ?? "-");

                                        CeldaDetalle(
                                            table.Cell(),
                                            $"{gasto.TipoComprobante?.Nombre ?? "-"}\n" +
                                            $"{gasto.Serie}-{gasto.Numero}");

                                        CeldaDetalle(
                                            table.Cell(),
                                            gasto.Detalle ?? "-");

                                        CeldaNumero(
                                            table.Cell(),
                                            gasto.ValorVenta);

                                        CeldaNumero(
                                            table.Cell(),
                                            gasto.IGV);

                                        CeldaNumero(
                                            table.Cell(),
                                            gasto.MontoTotal);
                                    }
                                });

                            content.Item()
                                .AlignRight()
                                .Width(275)
                                .Border(1)
                                .BorderColor(Colors.Grey.Lighten1)
                                .Background(Colors.Grey.Lighten4)
                                .Padding(12)
                                .Column(resumen =>
                                {
                                    resumen.Spacing(7);

                                    FilaResumen(
                                        resumen,
                                        "Subtotal valor venta:",
                                        totalBase);

                                    FilaResumen(
                                        resumen,
                                        "IGV total (18%):",
                                        totalIgv);

                                    FilaResumen(
                                        resumen,
                                        "Monto total rendido:",
                                        totalRendido,
                                        esTotal: true);

                                    FilaResumen(
                                        resumen,
                                        "Monto aprobado:",
                                        rendicion.Solicitud?.Monto ?? 0);

                                    FilaResumen(
                                        resumen,
                                        "Saldo:",
                                        rendicion.Saldo);

                                    if (devolucion != null)
                                    {
                                        FilaResumen(
                                            resumen,
                                            "Monto devuelto:",
                                            devolucion.Monto,
                                            esTotal: true);
                                    }
                                });

                            if (devolucion != null)
                            {
                                content.Item()
                                    .Border(1)
                                    .BorderColor(Colors.Grey.Lighten1)
                                    .Padding(12)
                                    .Column(devolucionSeccion =>
                                    {
                                        devolucionSeccion.Item()
                                            .Text("DEVOLUCIÓN DE SALDO")
                                            .Bold()
                                            .FontColor(
                                                Colors.Blue.Darken2);

                                        devolucionSeccion.Item()
                                            .PaddingTop(6)
                                            .Text(
                                                $"Banco: " +
                                                $"{devolucion.Banco}");

                                        devolucionSeccion.Item()
                                            .Text(
                                                $"Número de operación: " +
                                                $"{devolucion.NumeroOperacion}");

                                        devolucionSeccion.Item()
                                            .Text(
                                                $"Fecha: " +
                                                $"{devolucion.Fecha:dd/MM/yyyy}");

                                        devolucionSeccion.Item()
                                            .Text(
                                                $"Monto: " +
                                                $"S/ {devolucion.Monto:N2}");
                                    });
                            }

                            content.Item()
                                .PaddingTop(70)
                                .Row(firmas =>
                                {
                                    firmas.RelativeItem()
                                        .AlignCenter()
                                        .Column(firma =>
                                        {
                                            firma.Item()
                                                .Width(210)
                                                .LineHorizontal(1);

                                            firma.Item()
                                                .AlignCenter()
                                                .Text("Firma del empleado");

                                            firma.Item()
                                                .AlignCenter()
                                                .Text(nombreEmpleado);
                                        });

                                    firmas.ConstantItem(60);

                                    firmas.RelativeItem()
                                        .AlignCenter()
                                        .Column(firma =>
                                        {
                                            firma.Item()
                                                .Width(210)
                                                .LineHorizontal(1);

                                            firma.Item()
                                                .AlignCenter()
                                                .Text("Firma de aprobación");
                                        });
                                });
                        });

                    page.Footer()
                        .AlignCenter()
                        .Text(text =>
                        {
                            text.Span(
                                "DINACEM - Sistema de Viáticos | Página ");

                            text.CurrentPageNumber();

                            text.Span(" de ");

                            text.TotalPages();
                        });
                });
            });

            await Task.Run(() =>
                documento.GeneratePdf(rutaFisica));

            return new ResultadoPdfRendicion
            {
                RutaFisica = rutaFisica,
                RutaPublica = rutaPublica,
                NombreArchivo = nombreArchivo
            };
        }

        private static void CeldaCabecera(
            IContainer container,
            string texto)
        {
            container
                .Background(Colors.Blue.Medium)
                .Border(0.5f)
                .BorderColor(Colors.Grey.Lighten2)
                .Padding(6)
                .Text(texto)
                .FontColor(Colors.White)
                .Bold()
                .FontSize(8);
        }

        private static void CeldaDetalle(
            IContainer container,
            string texto)
        {
            container
                .Border(0.5f)
                .BorderColor(Colors.Grey.Lighten2)
                .Padding(6)
                .Text(texto)
                .FontSize(8);
        }

        private static void CeldaNumero(
            IContainer container,
            decimal monto)
        {
            container
                .Border(0.5f)
                .BorderColor(Colors.Grey.Lighten2)
                .Padding(6)
                .AlignRight()
                .Text(monto.ToString("N2"))
                .FontSize(8);
        }

        private static void FilaResumen(
            ColumnDescriptor columna,
            string etiqueta,
            decimal monto,
            bool esTotal = false)
        {
            columna.Item()
                .Row(row =>
                {
                    var textoEtiqueta =
                        row.RelativeItem()
                            .Text(etiqueta);

                    var textoMonto =
                        row.ConstantItem(90)
                            .AlignRight()
                            .Text($"S/ {monto:N2}");

                    if (esTotal)
                    {
                        textoEtiqueta.Bold();
                        textoMonto.Bold();
                    }
                });
        }

        private static string LimpiarNombreArchivo(
            string nombre)
        {
            foreach (var caracter in
                     Path.GetInvalidFileNameChars())
            {
                nombre = nombre.Replace(
                    caracter,
                    '-');
            }

            return nombre.Replace(' ', '-');
        }
    }

    public class ResultadoPdfRendicion
    {
        public string RutaFisica { get; set; } =
            string.Empty;

        public string RutaPublica { get; set; } =
            string.Empty;

        public string NombreArchivo { get; set; } =
            string.Empty;
    }
}