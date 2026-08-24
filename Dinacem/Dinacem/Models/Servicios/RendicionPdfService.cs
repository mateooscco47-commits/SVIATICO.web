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

            gastos ??= new List<Gasto>();

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


            // ============================================
            // PREPARAR COMPROBANTES
            // ============================================

            var comprobantes =
                PrepararComprobantes(gastos);


            // ============================================================
            // PREPARAR VOUCHER DE DEVOLUCIÓN
            // ============================================================

            byte[]? voucherDevolucion = null;
            string? rutaVoucherDevolucion = null;

            if (!string.IsNullOrWhiteSpace(devolucion?.Voucher))
            {
                rutaVoucherDevolucion = Path.Combine(
                    _environment.WebRootPath,
                    devolucion.Voucher.TrimStart('/'));

                var extension = Path.GetExtension(rutaVoucherDevolucion)
                    .ToLowerInvariant();

                if (File.Exists(rutaVoucherDevolucion) &&
                    extension is ".jpg" or ".jpeg" or ".png")
                {
                    voucherDevolucion = File.ReadAllBytes(
                        rutaVoucherDevolucion);
                }
            }

            // ============================================================
            // CREAR PDF
            // ============================================================

            var documento = Document.Create(container =>
            {
                container.Page(page =>
                {
                    // ====================================================
                    // CONFIGURACIÓN GENERAL
                    // ====================================================

                    page.Size(PageSizes.A4);
                    page.MarginTop(30);
                    page.MarginBottom(35);
                    page.MarginLeft(32);
                    page.MarginRight(32);

                    // ====================================================
                    // COLORES
                    // ====================================================

                    var azulPrincipal = Colors.Blue.Darken3;
                    var azulSecundario = Colors.Blue.Darken2;
                    var azulClaro = Colors.Blue.Lighten5;
                    var verdePrincipal = Colors.Green.Darken2;
                    var verdeClaro = Colors.Green.Lighten5;
                    var negro = Colors.Black;
                    var blanco = Colors.White;

                    // ====================================================
                    // LOGO DINACEN
                    // ====================================================

                    byte[]? logoDinacen = null;

                    try
                    {
                        var rutaLogo = Path.Combine(
                            _environment.WebRootPath,
                            "images",
                            "logo-dinacen.png");

                        if (File.Exists(rutaLogo))
                        {
                            logoDinacen = File.ReadAllBytes(rutaLogo);
                        }
                    }
                    catch
                    {
                        logoDinacen = null;
                    }

                    // ====================================================
                    // CABECERA
                    // ====================================================

                    page.Header()
                        .Column(header =>
                        {
                            header.Spacing(0);

                            // =================================================
                            // CABECERA INSTITUCIONAL
                            // =================================================

                            header.Item()
                                .Row(row =>
                                {
                                    if (logoDinacen != null)
                                    {
                                        row.ConstantItem(95)
                                            .Height(65)
                                            .AlignMiddle()
                                            .Image(logoDinacen)
                                            .FitArea();
                                    }
                                    else
                                    {
                                        row.ConstantItem(95)
                                            .Height(65);
                                    }

                                    row.RelativeItem()
                                        .AlignMiddle()
                                        .AlignRight()
                                        .Column(col =>
                                        {
                                            col.Item()
                                                .Text("LIQUIDACIÓN")
                                                .FontSize(13)
                                                .Bold()
                                                .FontColor(azulPrincipal);

                                            col.Item()
                                                .Text("DE GASTOS DE VIÁTICOS")
                                                .FontSize(8)
                                                .Bold()
                                                .FontColor(negro);
                                        });
                                });

                            // =================================================
                            // LÍNEA INSTITUCIONAL
                            // =================================================

                            header.Item()
                                .PaddingTop(10)
                                .LineHorizontal(2)
                                .LineColor(azulPrincipal);

                            // =================================================
                            // INFORMACIÓN DEL REPORTE
                            // =================================================

                            header.Item()
                                .PaddingTop(10)
                                .Background(blanco)
                                .Border(0.7f)
                                .BorderColor(negro)
                                .Padding(10)
                                .Column(info =>
                                {
                                    info.Spacing(5);

                                    // EMPLEADO

                                    info.Item()
                                        .Row(row =>
                                        {
                                            row.ConstantItem(125)
                                                .Text("EMPLEADO RENDIDOR")
                                                .FontSize(7)
                                                .Bold()
                                                .FontColor(negro);

                                            row.RelativeItem()
                                                .Text(nombreEmpleado)
                                                .FontSize(10)
                                                .Bold()
                                                .FontColor(azulPrincipal);
                                        });

                                    // PERIODO

                                    info.Item()
                                        .Row(row =>
                                        {
                                            row.ConstantItem(125)
                                                .Text("PERIODO")
                                                .FontSize(7)
                                                .Bold()
                                                .FontColor(negro);

                                            row.RelativeItem()
                                                .Text(
                                                    $"{rendicion.FechaInicio:dd/MM/yyyy} al {rendicion.FechaFin:dd/MM/yyyy}")
                                                .FontSize(9)
                                                .FontColor(negro);

                                            row.ConstantItem(110)
                                                .AlignRight()
                                                .Text("FECHA DE REPORTE")
                                                .FontSize(7)
                                                .Bold()
                                                .FontColor(negro);

                                            row.ConstantItem(72)
                                                .AlignRight()
                                                .Text(
                                                    DateTime.Now.ToString(
                                                        "dd/MM/yyyy"))
                                                .FontSize(9)
                                                .FontColor(negro);
                                        });

                                    // CORREO / CELULAR

                                    info.Item()
                                        .Row(row =>
                                        {
                                            row.ConstantItem(125)
                                                .Text("CORREO")
                                                .FontSize(7)
                                                .Bold()
                                                .FontColor(negro);

                                            row.RelativeItem()
                                                .Text(
                                                    rendicion.Usuario?.Correo ?? "-")
                                                .FontSize(8.5f)
                                                .FontColor(negro);

                                            row.ConstantItem(110)
                                                .AlignRight()
                                                .Text("CELULAR")
                                                .FontSize(7)
                                                .Bold()
                                                .FontColor(negro);

                                            row.ConstantItem(72)
                                                .AlignRight()
                                                .Text(
                                                    rendicion.Usuario?.Celular ?? "-")
                                                .FontSize(8.5f)
                                                .FontColor(negro);
                                        });
                                });
                        });

                    // ====================================================
                    // CONTENIDO
                    // ====================================================

                    page.Content()
                        .PaddingTop(16)
                        .Column(content =>
                        {
                            content.Spacing(13);

                            // =================================================
                            // TÍTULO DETALLE DE GASTOS
                            // =================================================

                            content.Item()
                                .Column(col =>
                                {
                                    col.Item()
                                        .Text("DETALLE DE GASTOS")
                                        .FontSize(12)
                                        .Bold()
                                        .FontColor(azulPrincipal);

                                    col.Item()
                                        .PaddingTop(2)
                                        .Text(
                                            "Detalle de los gastos registrados en la rendición.")
                                        .FontSize(8)
                                        .FontColor(negro);
                                });

                            // =================================================
                            // TABLA DE GASTOS
                            // =================================================

                            content.Item()
                                .Border(0.7f)
                                .BorderColor(negro)
                                .Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.ConstantColumn(55);
                                        columns.RelativeColumn(1.15f);
                                        columns.RelativeColumn(1.35f);
                                        columns.RelativeColumn(1.60f);
                                        columns.ConstantColumn(53);
                                        columns.ConstantColumn(50);
                                        columns.ConstantColumn(58);
                                    });

                                    table.Header(header =>
                                    {
                                        CeldaCabecera(
                                            header.Cell(),
                                            "Fecha");

                                        CeldaCabecera(
                                            header.Cell(),
                                            "Tipo de\ngasto");

                                        CeldaCabecera(
                                            header.Cell(),
                                            "Comprobante");

                                        CeldaCabecera(
                                            header.Cell(),
                                            "Detalle");

                                        CeldaCabecera(
                                            header.Cell(),
                                            "Base\n(S/)");

                                        CeldaCabecera(
                                            header.Cell(),
                                            "IGV\n(S/)");

                                        CeldaCabecera(
                                            header.Cell(),
                                            "Total\n(S/)");
                                    });

                                    foreach (var gasto in gastos)
                                    {
                                        CeldaDetalle(
                                            table.Cell(),
                                            gasto.Fecha.ToString("dd/MM/yyyy"));

                                        CeldaDetalle(
                                            table.Cell(),
                                            gasto.TipoGasto?.Nombre ?? "-");

                                        CeldaDetalle(
                                            table.Cell(),
                                            $"{gasto.TipoComprobante?.Nombre ?? "-"}\n{gasto.Serie}-{gasto.Numero}");

                                        CeldaDetalle(
                                            table.Cell(),
                                            gasto.Detalle ?? "-");

                                        CeldaNumero(
                                            table.Cell(),
                                            gasto.ValorVenta);

                                        CeldaNumero(
                                            table.Cell(),
                                            gasto.IGV);

                                        CeldaNumeroDestacado(
                                            table.Cell(),
                                            gasto.MontoTotal);
                                    }
                                });

                            // =================================================
                            // RESUMEN FINANCIERO
                            // =================================================

                            content.Item()
                                .AlignRight()
                                .Width(290)
                                .Border(0.8f)
                                .BorderColor(negro)
                                .Column(resumen =>
                                {
                                    resumen.Item()
                                        .Background(azulPrincipal)
                                        .PaddingVertical(7)
                                        .PaddingHorizontal(10)
                                        .Text("RESUMEN DE LIQUIDACIÓN")
                                        .FontSize(8.5f)
                                        .Bold()
                                        .FontColor(blanco);

                                    resumen.Item()
                                        .Background(blanco)
                                        .Padding(10)
                                        .Column(datos =>
                                        {
                                            datos.Spacing(5);

                                            FilaResumen(
                                                datos,
                                                "Subtotal valor venta",
                                                totalBase);

                                            FilaResumen(
                                                datos,
                                                "IGV total (18%)",
                                                totalIgv);

                                            datos.Item()
                                                .PaddingVertical(4)
                                                .LineHorizontal(0.6f)
                                                .LineColor(negro);

                                            FilaResumenDestacada(
                                                datos,
                                                "MONTO TOTAL RENDIDO",
                                                totalRendido);

                                            FilaResumen(
                                                datos,
                                                "Monto aprobado",
                                                rendicion.Solicitud?.Monto ?? 0);

                                            FilaResumen(
                                                datos,
                                                "Saldo",
                                                rendicion.Saldo);

                                            if (devolucion != null)
                                            {
                                                datos.Item()
                                                    .PaddingVertical(4)
                                                    .LineHorizontal(0.6f)
                                                    .LineColor(negro);

                                                FilaResumenDestacada(
                                                    datos,
                                                    "MONTO DEVUELTO",
                                                    devolucion.Monto);
                                            }
                                        });
                                });

                            // =================================================
                            // DEVOLUCIÓN DE SALDO
                            // =================================================

                            if (devolucion != null)
                            {
                                content.Item()
                                    .Border(0.8f)
                                    .BorderColor(Colors.Green.Darken1)
                                    .Background(verdeClaro)
                                    .Padding(11)
                                    .Column(devolucionSeccion =>
                                    {
                                        devolucionSeccion.Spacing(5);

                                        devolucionSeccion.Item()
                                            .Text("DEVOLUCIÓN DE SALDO")
                                            .FontSize(10)
                                            .Bold()
                                            .FontColor(verdePrincipal);

                                        devolucionSeccion.Item()
                                            .PaddingBottom(4)
                                            .LineHorizontal(0.7f)
                                            .LineColor(
                                                Colors.Green.Darken1);

                                        devolucionSeccion.Item()
                                            .Row(row =>
                                            {
                                                row.RelativeItem()
                                                    .Text(text =>
                                                    {
                                                        text.Span("Banco: ")
                                                            .Bold()
                                                            .FontColor(negro);

                                                        text.Span(
                                                            devolucion.Banco ?? "-")
                                                            .FontColor(negro);
                                                    });

                                                row.RelativeItem()
                                                    .Text(text =>
                                                    {
                                                        text.Span("Operación: ")
                                                            .Bold()
                                                            .FontColor(negro);

                                                        text.Span(
                                                            devolucion.NumeroOperacion
                                                            ?? "-")
                                                            .FontColor(negro);
                                                    });
                                            });

                                        devolucionSeccion.Item()
                                            .Row(row =>
                                            {
                                                row.RelativeItem()
                                                    .Text(text =>
                                                    {
                                                        text.Span("Fecha: ")
                                                            .Bold()
                                                            .FontColor(negro);

                                                        text.Span(
                                                            devolucion.Fecha.ToString(
                                                                "dd/MM/yyyy"))
                                                            .FontColor(negro);
                                                    });

                                                row.RelativeItem()
                                                    .Text(text =>
                                                    {
                                                        text.Span("Monto: ")
                                                            .Bold()
                                                            .FontColor(negro);

                                                        text.Span(
                                                            $"S/ {devolucion.Monto:N2}")
                                                            .Bold()
                                                            .FontColor(
                                                                verdePrincipal);
                                                    });
                                            });

                                        // =============================================
                                        // VOUCHER DE DEVOLUCIÓN
                                        // =============================================

                                        if (voucherDevolucion != null &&
                                            voucherDevolucion.Length > 0)
                                        {
                                            devolucionSeccion.Item()
                                                .PaddingTop(7)
                                                .LineHorizontal(0.6f)
                                                .LineColor(
                                                    Colors.Green.Darken1);

                                            devolucionSeccion.Item()
                                                .PaddingTop(5)
                                                .AlignCenter()
                                                .Text("VOUCHER DE DEVOLUCIÓN")
                                                .FontSize(8.5f)
                                                .Bold()
                                                .FontColor(
                                                    verdePrincipal);

                                            devolucionSeccion.Item()
                                                .PaddingTop(7)
                                                .AlignCenter()
                                                .Background(blanco)
                                                .Border(0.6f)
                                                .BorderColor(negro)
                                                .Padding(8)
                                                .Column(voucher =>
                                                {
                                                    voucher.Spacing(5);

                                                    voucher.Item()
                                                        .AlignCenter()
                                                        .MaxWidth(470)
                                                        .MaxHeight(350)
                                                        .Image(voucherDevolucion)
                                                        .FitArea();

                                                    voucher.Item()
                                                        .AlignCenter()
                                                        .Text(
                                                            Path.GetFileName(
                                                                rutaVoucherDevolucion
                                                                ?? "Voucher"))
                                                        .FontSize(7)
                                                        .FontColor(negro);
                                                });
                                        }
                                        else if (
                                            !string.IsNullOrWhiteSpace(
                                                devolucion.Voucher))
                                        {
                                            var extension =
                                                Path.GetExtension(
                                                    devolucion.Voucher)
                                                .ToLowerInvariant();

                                            if (extension == ".pdf")
                                            {
                                                devolucionSeccion.Item()
                                                    .PaddingTop(7)
                                                    .Background(blanco)
                                                    .Border(0.6f)
                                                    .BorderColor(negro)
                                                    .Padding(12)
                                                    .AlignCenter()
                                                    .Column(pdf =>
                                                    {
                                                        pdf.Spacing(4);

                                                        pdf.Item()
                                                            .Text(
                                                                "VOUCHER DE DEVOLUCIÓN")
                                                            .FontSize(8.5f)
                                                            .Bold()
                                                            .FontColor(
                                                                verdePrincipal);

                                                        pdf.Item()
                                                            .Text(
                                                                Path.GetFileName(
                                                                    devolucion.Voucher))
                                                            .FontSize(7.5f)
                                                            .FontColor(negro);

                                                        pdf.Item()
                                                            .Text(
                                                                "Voucher adjunto en formato PDF.")
                                                            .FontSize(7)
                                                            .FontColor(negro);
                                                    });
                                            }
                                            else
                                            {
                                                devolucionSeccion.Item()
                                                    .PaddingTop(7)
                                                    .AlignCenter()
                                                    .Text(
                                                        "No fue posible mostrar el voucher.")
                                                    .FontSize(8)
                                                    .FontColor(negro);
                                            }
                                        }
                                    });
                            }

                            // =================================================
                            // COMPROBANTES
                            // =================================================

                            if (gastos.Any())
                            {
                                content.Item()
                                    .PageBreak();

                                // =================================================
                                // TÍTULO
                                // =================================================

                                content.Item()
                                    .Column(col =>
                                    {
                                        col.Item()
                                            .Text(
                                                "COMPROBANTES SUSTENTATORIOS")
                                            .FontSize(13)
                                            .Bold()
                                            .FontColor(azulPrincipal);

                                        col.Item()
                                            .PaddingTop(2)
                                            .Text(
                                                "Documentos presentados como sustento de los gastos registrados.")
                                            .FontSize(8)
                                            .FontColor(negro);
                                    });

                                content.Item()
                                    .PaddingTop(5)
                                    .LineHorizontal(1)
                                    .LineColor(azulPrincipal);

                                // =================================================
                                // CADA COMPROBANTE
                                // =================================================

                                foreach (var gasto in gastos)
                                {
                                    var comprobante =
                                        comprobantes.FirstOrDefault(
                                            x => x.IdGasto == gasto.IdGasto);

                                    content.Item()
                                        .PaddingTop(12)
                                        .Border(0.8f)
                                        .BorderColor(negro)
                                        .Column(seccion =>
                                        {
                                            // =========================================
                                            // ENCABEZADO
                                            // =========================================

                                            seccion.Item()
                                                .Background(azulClaro)
                                                .Padding(9)
                                                .Row(row =>
                                                {
                                                    row.RelativeItem()
                                                        .Column(col =>
                                                        {
                                                            col.Item()
                                                                .Text(
                                                                    $"GASTO N.º {gasto.IdGasto}")
                                                                .FontSize(10)
                                                                .Bold()
                                                                .FontColor(
                                                                    azulPrincipal);

                                                            col.Item()
                                                                .PaddingTop(2)
                                                                .Text(
                                                                    gasto.Fecha.ToString(
                                                                        "dd/MM/yyyy"))
                                                                .FontSize(8)
                                                                .FontColor(negro);
                                                        });

                                                    row.ConstantItem(110)
                                                        .AlignRight()
                                                        .AlignMiddle()
                                                        .Text(
                                                            $"S/ {gasto.MontoTotal:N2}")
                                                        .FontSize(12)
                                                        .Bold()
                                                        .FontColor(
                                                            azulPrincipal);
                                                });

                                            // =========================================
                                            // INFORMACIÓN
                                            // =========================================

                                            seccion.Item()
                                                .Padding(10)
                                                .Column(datos =>
                                                {
                                                    datos.Spacing(4);

                                                    datos.Item()
                                                        .Row(row =>
                                                        {
                                                            row.ConstantItem(100)
                                                                .Text(
                                                                    "Tipo de gasto")
                                                                .FontSize(7.5f)
                                                                .Bold()
                                                                .FontColor(negro);

                                                            row.RelativeItem()
                                                                .Text(
                                                                    gasto.TipoGasto?
                                                                        .Nombre ?? "-")
                                                                .FontSize(8.5f)
                                                                .FontColor(negro);
                                                        });

                                                    datos.Item()
                                                        .Row(row =>
                                                        {
                                                            row.ConstantItem(100)
                                                                .Text("Comprobante")
                                                                .FontSize(7.5f)
                                                                .Bold()
                                                                .FontColor(negro);

                                                            row.RelativeItem()
                                                                .Text(text =>
                                                                {
                                                                    text.Span(
                                                                        gasto
                                                                            .TipoComprobante?
                                                                            .Nombre ?? "-")
                                                                        .FontSize(8.5f)
                                                                        .FontColor(negro);

                                                                    if (
                                                                        !string.IsNullOrWhiteSpace(
                                                                            gasto.Serie) ||
                                                                        !string.IsNullOrWhiteSpace(
                                                                            gasto.Numero))
                                                                    {
                                                                        text.Span(
                                                                            $"  |  {gasto.Serie}-{gasto.Numero}")
                                                                            .FontSize(8.5f)
                                                                            .Bold()
                                                                            .FontColor(
                                                                                negro);
                                                                    }
                                                                });
                                                        });

                                                    datos.Item()
                                                        .Row(row =>
                                                        {
                                                            row.ConstantItem(100)
                                                                .Text("Detalle")
                                                                .FontSize(7.5f)
                                                                .Bold()
                                                                .FontColor(negro);

                                                            row.RelativeItem()
                                                                .Text(
                                                                    gasto.Detalle ?? "-")
                                                                .FontSize(8.5f)
                                                                .FontColor(negro);
                                                        });

                                                    datos.Item()
                                                        .PaddingTop(4)
                                                        .LineHorizontal(0.5f)
                                                        .LineColor(negro);
                                                });

                                            // =========================================
                                            // SIN COMPROBANTE
                                            // =========================================

                                            if (comprobante == null)
                                            {
                                                seccion.Item()
                                                    .Padding(12)
                                                    .Background(blanco)
                                                    .AlignCenter()
                                                    .Text(
                                                        "No se adjuntó comprobante para este gasto.")
                                                    .FontSize(8)
                                                    .FontColor(negro);
                                            }

                                            // =========================================
                                            // IMAGEN
                                            // =========================================

                                            else if (
                                                comprobante.EsImagen &&
                                                comprobante.Datos != null &&
                                                comprobante.Datos.Length > 0)
                                            {
                                                seccion.Item()
                                                    .Padding(10)
                                                    .AlignCenter()
                                                    .Background(blanco)
                                                    .BorderTop(0.5f)
                                                    .BorderColor(negro)
                                                    .Column(imagen =>
                                                    {
                                                        imagen.Spacing(6);

                                                        imagen.Item()
                                                            .AlignCenter()
                                                            .Text(
                                                                "IMAGEN DEL COMPROBANTE")
                                                            .FontSize(7.5f)
                                                            .Bold()
                                                            .FontColor(negro);

                                                        imagen.Item()
                                                            .AlignCenter()
                                                            .MaxHeight(430)
                                                            .MaxWidth(470)
                                                            .Image(comprobante.Datos)
                                                            .FitArea();

                                                        imagen.Item()
                                                            .AlignCenter()
                                                            .Text(
                                                                comprobante.NombreArchivo)
                                                            .FontSize(7)
                                                            .FontColor(negro);
                                                    });
                                            }

                                            // =========================================
                                            // PDF
                                            // =========================================

                                            else if (comprobante.EsPdf)
                                            {
                                                seccion.Item()
                                                    .Padding(15)
                                                    .Background(blanco)
                                                    .AlignCenter()
                                                    .Column(pdf =>
                                                    {
                                                        pdf.Spacing(5);

                                                        pdf.Item()
                                                            .Text(
                                                                "DOCUMENTO PDF ADJUNTO")
                                                            .FontSize(9)
                                                            .Bold()
                                                            .FontColor(
                                                                azulPrincipal);

                                                        pdf.Item()
                                                            .Text(
                                                                comprobante.NombreArchivo)
                                                            .FontSize(8)
                                                            .FontColor(negro);

                                                        pdf.Item()
                                                            .Text(
                                                                "El comprobante fue cargado en formato PDF.")
                                                            .FontSize(7.5f)
                                                            .FontColor(negro);
                                                    });
                                            }

                                            // =========================================
                                            // ARCHIVO NO VÁLIDO
                                            // =========================================

                                            else
                                            {
                                                seccion.Item()
                                                    .Padding(15)
                                                    .Background(blanco)
                                                    .AlignCenter()
                                                    .Text(
                                                        "El comprobante no pudo mostrarse dentro del PDF.")
                                                    .FontSize(8)
                                                    .FontColor(negro);
                                            }
                                        });
                                }
                            }

                            // =================================================
                            // FIRMAS
                            // =================================================

                            content.Item()
                                .PaddingTop(60)
                                .Row(firmas =>
                                {
                                    // FIRMA EMPLEADO

                                    firmas.RelativeItem()
                                        .AlignCenter()
                                        .Column(firma =>
                                        {
                                            firma.Item()
                                                .Width(200)
                                                .LineHorizontal(0.8f)
                                                .LineColor(negro);

                                            firma.Item()
                                                .PaddingTop(5)
                                                .AlignCenter()
                                                .Text("FIRMA DEL EMPLEADO")
                                                .FontSize(8)
                                                .Bold()
                                                .FontColor(negro);

                                            firma.Item()
                                                .PaddingTop(2)
                                                .AlignCenter()
                                                .Text(nombreEmpleado)
                                                .FontSize(8)
                                                .FontColor(negro);
                                        });

                                    firmas.ConstantItem(70);

                                    // FIRMA APROBACIÓN

                                    firmas.RelativeItem()
                                        .AlignCenter()
                                        .Column(firma =>
                                        {
                                            firma.Item()
                                                .Width(200)
                                                .LineHorizontal(0.8f)
                                                .LineColor(negro);

                                            firma.Item()
                                                .PaddingTop(5)
                                                .AlignCenter()
                                                .Text("FIRMA DE APROBACIÓN")
                                                .FontSize(8)
                                                .Bold()
                                                .FontColor(negro);

                                            firma.Item()
                                                .PaddingTop(2)
                                                .AlignCenter()
                                                .Text(
                                                    "Responsable de aprobación")
                                                .FontSize(8)
                                                .FontColor(negro);
                                        });
                                });
                        });

                    // ====================================================
                    // PIE DE PÁGINA
                    // ====================================================

                    page.Footer()
                        .PaddingTop(8)
                        .BorderTop(0.7f)
                        .BorderColor(negro)
                        .Row(footer =>
                        {
                            footer.RelativeItem()
                                .Text(
                                    "DINACEN • Sistema de Gestión de Viáticos")
                                .FontSize(7)
                                .FontColor(negro);

                            footer.RelativeItem()
                                .AlignRight()
                                .Text(text =>
                                {
                                    text.Span("Página ")
                                        .FontSize(7)
                                        .FontColor(negro);

                                    text.CurrentPageNumber()
                                        .FontSize(7)
                                        .Bold()
                                        .FontColor(negro);

                                    text.Span(" de ")
                                        .FontSize(7)
                                        .FontColor(negro);

                                    text.TotalPages()
                                        .FontSize(7)
                                        .Bold()
                                        .FontColor(negro);
                                });
                        });
                });
            });       

            // ============================================
            // GENERAR ARCHIVO
            // ============================================

            await Task.Run(() =>
                documento.GeneratePdf(rutaFisica));


            return new ResultadoPdfRendicion
            {
                RutaFisica = rutaFisica,
                RutaPublica = rutaPublica,
                NombreArchivo = nombreArchivo
            };
        }


        // ================================================
        // PREPARAR COMPROBANTES
        // ================================================

        private List<ComprobantePdf> PrepararComprobantes(
            List<Gasto> gastos)
        {
            var resultado =
                new List<ComprobantePdf>();

            foreach (var gasto in gastos)
            {
                if (string.IsNullOrWhiteSpace(
                    gasto.Comprobante))
                {
                    continue;
                }

                var rutaFisica =
                    ObtenerRutaFisica(
                        gasto.Comprobante);

                if (string.IsNullOrWhiteSpace(
                        rutaFisica))
                {
                    continue;
                }

                if (!File.Exists(rutaFisica))
                {
                    continue;
                }

                var extension =
                    Path.GetExtension(rutaFisica)
                        .ToLowerInvariant();

                var comprobante =
                    new ComprobantePdf
                    {
                        IdGasto =
                            gasto.IdGasto,

                        NombreArchivo =
                            Path.GetFileName(
                                rutaFisica),

                        RutaFisica =
                            rutaFisica
                    };


                // ========================================
                // IMÁGENES
                // ========================================

                if (extension == ".jpg" ||
                    extension == ".jpeg" ||
                    extension == ".png")
                {
                    try
                    {
                        comprobante.Datos =
                            File.ReadAllBytes(
                                rutaFisica);

                        comprobante.EsImagen =
                            true;
                    }
                    catch
                    {
                        comprobante.EsImagen =
                            false;
                    }
                }


                // ========================================
                // PDF
                // ========================================

                else if (extension == ".pdf")
                {
                    comprobante.EsPdf =
                        true;
                }


                resultado.Add(
                    comprobante);
            }

            return resultado;
        }


        // ================================================
        // OBTENER RUTA FÍSICA
        // ================================================

        private string? ObtenerRutaFisica(
            string rutaGuardada)
        {
            if (string.IsNullOrWhiteSpace(
                rutaGuardada))
            {
                return null;
            }


            // Si por algún motivo ya está guardada
            // como ruta física
            if (Path.IsPathRooted(
                rutaGuardada) &&
                File.Exists(rutaGuardada))
            {
                return rutaGuardada;
            }


            // Ejemplo:
            //
            // /comprobantes/factura.jpg
            //
            // se convierte en:
            //
            // wwwroot/comprobantes/factura.jpg

            var rutaRelativa =
                rutaGuardada
                    .Replace("\\", "/")
                    .TrimStart('/');


            var rutaFisica =
                Path.Combine(
                    _environment.WebRootPath,
                    rutaRelativa.Replace(
                        "/",
                        Path.DirectorySeparatorChar
                            .ToString()));


            return rutaFisica;
        }


        // ============================================================
        // CELDA CABECERA
        // ============================================================

        private static void CeldaCabecera(
            IContainer container,
            string texto)
        {
            container
                .Background(
                    Colors.Blue.Darken3)
                .Border(0.5f)
                .BorderColor(
                    Colors.Blue.Darken4)
                .PaddingVertical(7)
                .PaddingHorizontal(5)
                .AlignMiddle()
                .Text(texto)
                .FontColor(
                    Colors.White)
                .Bold()
                .FontSize(7.5f);
        }


        // ============================================================
        // CELDA DETALLE
        // ============================================================

        private static void CeldaDetalle(
            IContainer container,
            string texto)
        {
            container
                .Border(0.5f)
                .BorderColor(
                    Colors.Grey.Lighten2)
                .PaddingVertical(6)
                .PaddingHorizontal(5)
                .AlignMiddle()
                .Text(texto)
                .FontColor(
                    Colors.Grey.Darken2)
                .FontSize(7.5f);
        }


        // ============================================================
        // CELDA NUMÉRICA
        // ============================================================

        private static void CeldaNumero(
            IContainer container,
            decimal monto)
        {
            container
                .Border(0.5f)
                .BorderColor(
                    Colors.Grey.Lighten2)
                .PaddingVertical(6)
                .PaddingHorizontal(5)
                .AlignMiddle()
                .AlignRight()
                .Text(
                    $"S/ {monto:N2}")
                .FontSize(7.5f)
                .FontColor(
                    Colors.Grey.Darken2);
        }


        // ============================================================
        // CELDA NUMÉRICA DESTACADA
        // ============================================================

        private static void CeldaNumeroDestacado(
            IContainer container,
            decimal monto)
        {
            container
                .Border(0.5f)
                .BorderColor(
                    Colors.Grey.Lighten2)
                .PaddingVertical(6)
                .PaddingHorizontal(5)
                .AlignMiddle()
                .AlignRight()
                .Background(
                    Colors.Blue.Lighten5)
                .Text(
                    $"S/ {monto:N2}")
                .FontSize(7.5f)
                .Bold()
                .FontColor(
                    Colors.Blue.Darken3);
        }


        // ============================================================
        // FILA DEL RESUMEN
        // ============================================================

        private static void FilaResumen(
            ColumnDescriptor columna,
            string etiqueta,
            decimal monto)
        {
            columna.Item()
                .Row(row =>
                {
                    row.RelativeItem()
                        .Text(etiqueta)
                        .FontSize(8)
                        .FontColor(
                            Colors.Grey.Darken2);


                    row.ConstantItem(90)
                        .AlignRight()
                        .Text(
                            $"S/ {monto:N2}")
                        .FontSize(8)
                        .FontColor(
                            Colors.Grey.Darken2);
                });
        }


        // ============================================================
        // FILA DESTACADA DEL RESUMEN
        // ============================================================

        private static void FilaResumenDestacada(
            ColumnDescriptor columna,
            string etiqueta,
            decimal monto)
        {
            columna.Item()
                .Background(
                    Colors.Blue.Lighten5)
                .PaddingVertical(6)
                .PaddingHorizontal(7)
                .Row(row =>
                {
                    row.RelativeItem()
                        .Text(etiqueta)
                        .FontSize(8)
                        .Bold()
                        .FontColor(
                            Colors.Blue.Darken3);


                    row.ConstantItem(95)
                        .AlignRight()
                        .Text(
                            $"S/ {monto:N2}")
                        .FontSize(9)
                        .Bold()
                        .FontColor(
                            Colors.Blue.Darken3);
                });
        }

        // ================================================
        // LIMPIAR NOMBRE DEL ARCHIVO
        // ================================================

        private static string LimpiarNombreArchivo(
            string nombre)
        {
            foreach (
                var caracter in
                Path.GetInvalidFileNameChars())
            {
                nombre =
                    nombre.Replace(
                        caracter,
                        '-');
            }

            return nombre.Replace(
                ' ',
                '-');
        }
    }


    // ================================================
    // RESULTADO PDF
    // ================================================

    public class ResultadoPdfRendicion
    {
        public string RutaFisica { get; set; } =
            string.Empty;

        public string RutaPublica { get; set; } =
            string.Empty;

        public string NombreArchivo { get; set; } =
            string.Empty;
    }


    // ================================================
    // COMPROBANTE PARA PDF
    // ================================================

    public class ComprobantePdf
    {
        public int IdGasto { get; set; }

        public string NombreArchivo { get; set; } =
            string.Empty;

        public string RutaFisica { get; set; } =
            string.Empty;

        public byte[]? Datos { get; set; }

        public bool EsImagen { get; set; }

        public bool EsPdf { get; set; }
    }
}