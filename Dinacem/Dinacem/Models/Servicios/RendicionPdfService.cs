using Dinacem.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

public class RendicionPdfService
{
    private readonly IWebHostEnvironment _environment;

    // =============================================================
    // COLORES INSTITUCIONALES
    // =============================================================

    private const string AzulDinacen = "#0C4A8A";
    private const string VerdeDinacen = "#6AA84F";

    private const string GrisTexto = "#333333";
    private const string GrisSecundario = "#666666";
    private const string GrisBorde = "#D9DEE5";
    private const string GrisSuave = "#F5F7F9";

    public RendicionPdfService(
        IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public async Task<ResultadoPdfRendicion> GenerarAsync(
        Rendicion rendicion,
        List<Gasto> gastos,
        DevolucionSaldo? devolucion,
        List<BitacoraVehiculo>? bitacorasVehiculo = null)
    {
        ArgumentNullException.ThrowIfNull(rendicion);

        gastos ??= new List<Gasto>();
        bitacorasVehiculo ??= new List<BitacoraVehiculo>();

        // =========================================================
        // DATOS GENERALES
        // =========================================================

        var nombreEmpleado =
            $"{rendicion.Usuario?.Nombres} {rendicion.Usuario?.Apellidos}"
            .Trim();

        if (string.IsNullOrWhiteSpace(nombreEmpleado))
        {
            nombreEmpleado =
                $"Usuario {rendicion.IdUsuario}";
        }

        var correoEmpleado =
            rendicion.Usuario?.Correo ?? "-";

        var celularEmpleado =
            rendicion.Usuario?.Celular ?? "-";

        var destino =
            rendicion.Solicitud?.Destino ?? "-";

        var totalBase =
            gastos.Sum(g => g.ValorVenta);

        var totalIgv =
            gastos.Sum(g => g.IGV);

        var totalGastos =
            gastos.Sum(g => g.MontoTotal);

        var totalVehiculo =
            bitacorasVehiculo.Sum(
                b => b.MontoAsignado);

        var totalKm =
            bitacorasVehiculo.Sum(
                b => b.DistanciaKm);

        var totalRendido =
            totalGastos + totalVehiculo;

        var montoAprobado =
            rendicion.Solicitud?.Monto ?? 0;

        var saldoCalculado =
            montoAprobado - totalRendido;

        var nombreSeguroEmpleado =
            LimpiarNombreArchivo(
                nombreEmpleado);

        // =========================================================
        // CARPETA
        // =========================================================

        var carpeta =
            Path.Combine(
                _environment.WebRootPath,
                "liquidaciones");

        Directory.CreateDirectory(carpeta);

        // =========================================================
        // NOMBRES DE ARCHIVOS
        //
        // Se mantienen los nombres internos "Vouchers"
        // para no romper el controlador existente.
        // =========================================================

        var nombreArchivoLiquidacion =
            $"Liquidacion-{rendicion.IdRendicion}-{nombreSeguroEmpleado}.pdf";

        var nombreArchivoVouchers =
            $"Comprobantes-{rendicion.IdRendicion}-{nombreSeguroEmpleado}.pdf";

        var rutaFisicaLiquidacion =
            Path.Combine(
                carpeta,
                nombreArchivoLiquidacion);

        var rutaFisicaVouchers =
            Path.Combine(
                carpeta,
                nombreArchivoVouchers);

        var rutaPublicaLiquidacion =
            $"/liquidaciones/{nombreArchivoLiquidacion}";

        var rutaPublicaVouchers =
            $"/liquidaciones/{nombreArchivoVouchers}";

        // =========================================================
        // LOGO DINACEN
        // =========================================================

        var rutaLogo =
            Path.Combine(
                _environment.WebRootPath,
                "images",
                "logo-dinacen.png");

        byte[]? logoBytes = null;

        if (File.Exists(rutaLogo))
        {
            logoBytes =
                await File.ReadAllBytesAsync(
                    rutaLogo);
        }

        // =========================================================
        // CARGAR IMÁGENES DE COMPROBANTES
        // =========================================================

        var imagenesGastos =
            new Dictionary<int, byte[]?>();

        foreach (var gasto in gastos)
        {
            imagenesGastos[gasto.IdGasto] =
                CargarArchivoImagen(
                    gasto.Comprobante);
        }

        // =========================================================
        // COMPROBANTE DE DEVOLUCIÓN
        // =========================================================

        byte[]? imagenVoucherDevolucion = null;

        if (devolucion != null)
        {
            imagenVoucherDevolucion =
                CargarArchivoImagen(
                    devolucion.Voucher);
        }

        // =========================================================
        // =========================================================
        // PDF 1
        // LIQUIDACIÓN DE GASTOS DE VIÁTICOS
        // =========================================================
        // =========================================================

        var documentoLiquidacion =
            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);

                    page.MarginTop(28);
                    page.MarginBottom(30);
                    page.MarginLeft(30);
                    page.MarginRight(30);

                    page.DefaultTextStyle(text =>
                        text
                            .FontFamily("Arial")
                            .FontSize(9)
                            .FontColor(GrisTexto));

                    // =====================================================
                    // ENCABEZADO
                    // =====================================================

                    page.Header()
                        .Column(header =>
                        {
                            header.Spacing(7);

                            // =================================================
                            // CABECERA PRINCIPAL
                            // =================================================

                            header.Item()
                                .Row(row =>
                                {
                                    // -------------------------------------------------
                                    // LOGO
                                    // -------------------------------------------------

                                    row.ConstantItem(105)
                                        .AlignLeft()
                                        .AlignMiddle()
                                        .Element(logo =>
                                        {
                                            if (logoBytes != null)
                                            {
                                                logo
                                                    .Width(92)
                                                    .Image(logoBytes)
                                                    .FitArea();
                                            }
                                            else
                                            {
                                                logo
                                                    .Text("DINACEN")
                                                    .FontSize(18)
                                                    .Bold()
                                                    .FontColor(
                                                        AzulDinacen);
                                            }
                                        });

                                    // -------------------------------------------------
                                    // TÍTULO
                                    // -------------------------------------------------

                                    row.RelativeItem()
                                        .AlignCenter()
                                        .AlignMiddle()
                                        .Column(titulo =>
                                        {
                                            titulo.Spacing(1);

                                            titulo.Item()
                                                .AlignCenter()
                                                .Text(
                                                    "LIQUIDACIÓN DE GASTOS")
                                                .FontSize(17)
                                                .Bold()
                                                .FontColor(
                                                    GrisTexto);

                                            titulo.Item()
                                                .AlignCenter()
                                                .Text(
                                                    "DE VIÁTICOS")
                                                .FontSize(17)
                                                .Bold()
                                                .FontColor(
                                                    AzulDinacen);

                                            titulo.Item()
                                                .PaddingTop(3)
                                                .AlignCenter()
                                                .Text(
                                                    "Sistema de Gestión de Viáticos")
                                                .FontSize(8)
                                                .FontColor(
                                                    GrisSecundario);
                                        });

                                    // -------------------------------------------------
                                    // NÚMERO DE LIQUIDACIÓN
                                    // -------------------------------------------------

                                    row.ConstantItem(105)
                                        .AlignRight()
                                        .AlignMiddle()
                                        .Column(info =>
                                        {
                                            info.Spacing(2);

                                            info.Item()
                                                .AlignRight()
                                                .Text(
                                                    "N.º LIQUIDACIÓN")
                                                .FontSize(7.5f)
                                                .Bold()
                                                .FontColor(
                                                    GrisSecundario);

                                            info.Item()
                                                .AlignRight()
                                                .Text(
                                                    $"#{rendicion.IdRendicion}")
                                                .FontSize(15)
                                                .Bold()
                                                .FontColor(
                                                    AzulDinacen);

                                            info.Item()
                                                .PaddingTop(3)
                                                .AlignRight()
                                                .Text(
                                                    $"Emitido: {DateTime.Now:dd/MM/yyyy}")
                                                .FontSize(7.5f)
                                                .FontColor(
                                                    GrisSecundario);
                                        });
                                });

                            // =================================================
                            // LÍNEA INSTITUCIONAL
                            // =================================================

                            header.Item()
                                .PaddingTop(4)
                                .Height(2)
                                .Background(
                                    AzulDinacen);

                            header.Item()
                                .Height(1)
                                .Background(
                                    VerdeDinacen);

                            // =================================================
                            // INFORMACIÓN DEL EMPLEADO
                            // =================================================

                            header.Item()
                                .PaddingTop(7)
                                .Border(1)
                                .BorderColor(
                                    GrisBorde)
                                .Background(
                                    Colors.White)
                                .Padding(9)
                                .Column(datos =>
                                {
                                    datos.Spacing(5);

                                    // FILA 1
                                    datos.Item()
                                        .Row(row =>
                                        {
                                            DatoCabecera(
                                                row.RelativeItem(),
                                                "EMPLEADO",
                                                nombreEmpleado);

                                            DatoCabecera(
                                                row.RelativeItem(),
                                                "PERIODO",
                                                $"{rendicion.FechaInicio:dd/MM/yyyy} al {rendicion.FechaFin:dd/MM/yyyy}");
                                        });

                                    // FILA 2
                                    datos.Item()
                                        .Row(row =>
                                        {
                                            DatoCabecera(
                                                row.RelativeItem(),
                                                "CORREO",
                                                correoEmpleado);

                                            DatoCabecera(
                                                row.RelativeItem(),
                                                "CELULAR",
                                                celularEmpleado);
                                        });

                                    // FILA 3
                                    datos.Item()
                                        .Row(row =>
                                        {
                                            DatoCabecera(
                                                row.RelativeItem(),
                                                "DESTINO",
                                                destino);

                                            DatoCabecera(
                                                row.RelativeItem(),
                                                "MONTO APROBADO",
                                                $"S/ {montoAprobado:N2}");
                                        });
                                });
                        });

                    // =====================================================
                    // CONTENIDO
                    // =====================================================

                    page.Content()
                        .PaddingTop(14)
                        .Column(content =>
                        {
                            content.Spacing(11);

                            // =================================================
                            // DETALLE DE GASTOS
                            // =================================================

                            content.Item()
                                .Element(seccion =>
                                {
                                    EncabezadoSeccion(
                                        seccion,
                                        "DETALLE DE GASTOS");
                                });

                            if (gastos.Any())
                            {
                                content.Item()
                                    .Table(table =>
                                    {
                                        table.ColumnsDefinition(
                                            columns =>
                                            {
                                                columns.ConstantColumn(53);
                                                columns.RelativeColumn(1.15f);
                                                columns.RelativeColumn(1.35f);
                                                columns.RelativeColumn(1.6f);
                                                columns.ConstantColumn(55);
                                                columns.ConstantColumn(48);
                                                columns.ConstantColumn(58);
                                            });

                                        table.Header(header =>
                                        {
                                            CeldaCabecera(
                                                header.Cell(),
                                                "Fecha");

                                            CeldaCabecera(
                                                header.Cell(),
                                                "Tipo de gasto");

                                            CeldaCabecera(
                                                header.Cell(),
                                                "Comprobante");

                                            CeldaCabecera(
                                                header.Cell(),
                                                "Detalle");

                                            CeldaCabecera(
                                                header.Cell(),
                                                "Base\nS/");

                                            CeldaCabecera(
                                                header.Cell(),
                                                "IGV\nS/");

                                            CeldaCabecera(
                                                header.Cell(),
                                                "Total\nS/");
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
                            }
                            else
                            {
                                content.Item()
                                    .Border(1)
                                    .BorderColor(
                                        GrisBorde)
                                    .Padding(14)
                                    .AlignCenter()
                                    .Text(
                                        "No se registraron gastos con comprobante.")
                                    .FontSize(9)
                                    .FontColor(
                                        GrisSecundario);
                            }

                            // =================================================
                            // BITÁCORA VEHICULAR
                            // =================================================

                            if (bitacorasVehiculo.Any())
                            {
                                content.Item()
                                    .PaddingTop(4)
                                    .Element(seccion =>
                                    {
                                        EncabezadoSeccion(
                                            seccion,
                                            "BITÁCORA DE VEHÍCULO");
                                    });

                                content.Item()
                                    .Table(table =>
                                    {
                                        table.ColumnsDefinition(
                                            columns =>
                                            {
                                                columns.ConstantColumn(52);
                                                columns.RelativeColumn(1.15f);
                                                columns.RelativeColumn(1.15f);
                                                columns.ConstantColumn(55);
                                                columns.ConstantColumn(55);
                                                columns.RelativeColumn(1.25f);
                                                columns.ConstantColumn(58);
                                            });

                                        table.Header(header =>
                                        {
                                            CeldaCabecera(
                                                header.Cell(),
                                                "Fecha");

                                            CeldaCabecera(
                                                header.Cell(),
                                                "Origen");

                                            CeldaCabecera(
                                                header.Cell(),
                                                "Destino");

                                            CeldaCabecera(
                                                header.Cell(),
                                                "Distancia");

                                            CeldaCabecera(
                                                header.Cell(),
                                                "Tarifa/km\nS/");

                                            CeldaCabecera(
                                                header.Cell(),
                                                "Observaciones");

                                            CeldaCabecera(
                                                header.Cell(),
                                                "Monto\nS/");
                                        });

                                        foreach (
                                            var bitacora
                                            in bitacorasVehiculo)
                                        {
                                            CeldaDetalle(
                                                table.Cell(),
                                                bitacora.Fecha
                                                    .ToString("dd/MM/yyyy"));

                                            CeldaDetalle(
                                                table.Cell(),
                                                bitacora.Origen);

                                            CeldaDetalle(
                                                table.Cell(),
                                                bitacora.Destino);

                                            CeldaDetalle(
                                                table.Cell(),
                                                $"{bitacora.DistanciaKm:N2} km");

                                            CeldaNumero(
                                                table.Cell(),
                                                bitacora.TarifaKilometro);

                                            CeldaDetalle(
                                                table.Cell(),
                                                string.IsNullOrWhiteSpace(
                                                    bitacora.Observaciones)
                                                    ? "-"
                                                    : bitacora.Observaciones);

                                            CeldaNumero(
                                                table.Cell(),
                                                bitacora.MontoAsignado);
                                        }
                                    });

                                // -------------------------------------------------
                                // RESUMEN VEHÍCULO
                                // -------------------------------------------------

                                content.Item()
                                    .AlignRight()
                                    .Width(270)
                                    .Border(1)
                                    .BorderColor(
                                        GrisBorde)
                                    .Background(
                                        Colors.White)
                                    .Padding(9)
                                    .Column(resumenVehiculo =>
                                    {
                                        resumenVehiculo.Spacing(4);

                                        FilaResumen(
                                            resumenVehiculo,
                                            "Distancia total:",
                                            $"{totalKm:N2} km");

                                        var tarifasUsadas =
                                            bitacorasVehiculo
                                                .Select(
                                                    b => b.TarifaKilometro)
                                                .Distinct()
                                                .ToList();

                                        if (tarifasUsadas.Count == 1)
                                        {
                                            FilaResumen(
                                                resumenVehiculo,
                                                "Tarifa aplicada:",
                                                $"S/ {tarifasUsadas[0]:N2} / km");
                                        }
                                        else if (
                                            tarifasUsadas.Count > 1)
                                        {
                                            FilaResumen(
                                                resumenVehiculo,
                                                "Tarifa aplicada:",
                                                "Varias tarifas");
                                        }

                                        resumenVehiculo.Item()
                                            .PaddingTop(2)
                                            .Height(1)
                                            .Background(
                                                GrisBorde);

                                        FilaResumen(
                                            resumenVehiculo,
                                            "Total vehículo:",
                                            $"S/ {totalVehiculo:N2}",
                                            true);
                                    });
                            }

                            // =================================================
                            // RESUMEN
                            // =================================================

                            content.Item()
                                .PaddingTop(3)
                                .Element(seccion =>
                                {
                                    EncabezadoSeccion(
                                        seccion,
                                        "RESUMEN DE LA LIQUIDACIÓN");
                                });

                            content.Item()
                                .Row(row =>
                                {
                                    // -------------------------------------------------
                                    // RESUMEN DETALLADO
                                    // -------------------------------------------------

                                    row.RelativeItem()
                                        .Border(1)
                                        .BorderColor(
                                            GrisBorde)
                                        .Background(
                                            Colors.White)
                                        .Padding(11)
                                        .Column(resumen =>
                                        {
                                            resumen.Spacing(6);

                                            FilaResumen(
                                                resumen,
                                                "Subtotal valor venta:",
                                                $"S/ {totalBase:N2}");

                                            FilaResumen(
                                                resumen,
                                                "IGV total:",
                                                $"S/ {totalIgv:N2}");

                                            FilaResumen(
                                                resumen,
                                                "Gastos con comprobante:",
                                                $"S/ {totalGastos:N2}");

                                            if (totalVehiculo > 0)
                                            {
                                                FilaResumen(
                                                    resumen,
                                                    "Vehículo por kilometraje:",
                                                    $"S/ {totalVehiculo:N2}");
                                            }

                                            resumen.Item()
                                                .PaddingTop(2)
                                                .Height(1)
                                                .Background(
                                                    GrisBorde);

                                            FilaResumen(
                                                resumen,
                                                "MONTO TOTAL RENDIDO:",
                                                $"S/ {totalRendido:N2}",
                                                true);

                                            FilaResumen(
                                                resumen,
                                                "MONTO APROBADO:",
                                                $"S/ {montoAprobado:N2}",
                                                true);

                                            FilaResumen(
                                                resumen,
                                                saldoCalculado >= 0
                                                    ? "SALDO A DEVOLVER:"
                                                    : "EXCESO RENDIDO:",
                                                $"S/ {Math.Abs(saldoCalculado):N2}",
                                                true);
                                        });

                                    row.ConstantItem(10);

                                    // -------------------------------------------------
                                    // TOTAL PRINCIPAL
                                    // -------------------------------------------------

                                    row.ConstantItem(175)
                                        .Border(1)
                                        .BorderColor(
                                            AzulDinacen)
                                        .Background(
                                            Colors.White)
                                        .Padding(12)
                                        .Column(resumenFinal =>
                                        {
                                            resumenFinal.Spacing(6);

                                            resumenFinal.Item()
                                                .AlignCenter()
                                                .Text(
                                                    "TOTAL RENDIDO")
                                                .FontSize(9)
                                                .Bold()
                                                .FontColor(
                                                    AzulDinacen);

                                            resumenFinal.Item()
                                                .PaddingTop(3)
                                                .AlignCenter()
                                                .Text(
                                                    $"S/ {totalRendido:N2}")
                                                .FontSize(19)
                                                .Bold()
                                                .FontColor(
                                                    GrisTexto);

                                            resumenFinal.Item()
                                                .PaddingTop(3)
                                                .Height(2)
                                                .Background(
                                                    VerdeDinacen);

                                            resumenFinal.Item()
                                                .PaddingTop(5)
                                                .AlignCenter()
                                                .Text(
                                                    saldoCalculado >= 0
                                                        ? "SALDO A DEVOLVER"
                                                        : "EXCESO RENDIDO")
                                                .FontSize(8)
                                                .Bold()
                                                .FontColor(
                                                    GrisSecundario);

                                            resumenFinal.Item()
                                                .AlignCenter()
                                                .Text(
                                                    $"S/ {Math.Abs(saldoCalculado):N2}")
                                                .FontSize(14)
                                                .Bold()
                                                .FontColor(
                                                    GrisTexto);
                                        });
                                });

                            // =================================================
                            // DEVOLUCIÓN
                            // =================================================

                            if (devolucion != null)
                            {
                                content.Item()
                                    .PaddingTop(3)
                                    .Element(seccion =>
                                    {
                                        EncabezadoSeccion(
                                            seccion,
                                            "DEVOLUCIÓN DE SALDO");
                                    });

                                content.Item()
                                    .Border(1)
                                    .BorderColor(
                                        GrisBorde)
                                    .Background(
                                        Colors.White)
                                    .Padding(11)
                                    .Row(row =>
                                    {
                                        row.RelativeItem()
                                            .Column(datos =>
                                            {
                                                datos.Spacing(4);

                                                DatoDevolucion(
                                                    datos,
                                                    "Banco",
                                                    devolucion.Banco);

                                                DatoDevolucion(
                                                    datos,
                                                    "N.º de operación",
                                                    devolucion.NumeroOperacion);

                                                DatoDevolucion(
                                                    datos,
                                                    "Fecha",
                                                    devolucion.Fecha
                                                        .ToString(
                                                            "dd/MM/yyyy"));

                                                if (!string.IsNullOrWhiteSpace(
                                                        devolucion.Observaciones))
                                                {
                                                    DatoDevolucion(
                                                        datos,
                                                        "Observaciones",
                                                        devolucion.Observaciones);
                                                }
                                            });

                                        row.ConstantItem(145)
                                            .AlignMiddle()
                                            .Column(monto =>
                                            {
                                                monto.Item()
                                                    .AlignCenter()
                                                    .Text(
                                                        "MONTO DEVUELTO")
                                                    .FontSize(8)
                                                    .Bold()
                                                    .FontColor(
                                                        AzulDinacen);

                                                monto.Item()
                                                    .PaddingTop(3)
                                                    .AlignCenter()
                                                    .Text(
                                                        $"S/ {devolucion.Monto:N2}")
                                                    .FontSize(16)
                                                    .Bold()
                                                    .FontColor(
                                                        GrisTexto);
                                            });
                                    });
                            }

                            // =================================================
                            // FIRMAS
                            // =================================================

                            content.Item()
                                .PaddingTop(38)
                                .Row(firmas =>
                                {
                                    firmas.RelativeItem()
                                        .AlignCenter()
                                        .Column(firma =>
                                        {
                                            firma.Item()
                                                .Width(190)
                                                .AlignCenter()
                                                .LineHorizontal(1);

                                            firma.Item()
                                                .PaddingTop(5)
                                                .AlignCenter()
                                                .Text(
                                                    "FIRMA DEL EMPLEADO")
                                                .Bold()
                                                .FontSize(8.5f);

                                            firma.Item()
                                                .PaddingTop(2)
                                                .AlignCenter()
                                                .Text(
                                                    nombreEmpleado)
                                                .FontSize(8);
                                        });

                                    firmas.ConstantItem(75);

                                    firmas.RelativeItem()
                                        .AlignCenter()
                                        .Column(firma =>
                                        {
                                            firma.Item()
                                                .Width(190)
                                                .AlignCenter()
                                                .LineHorizontal(1);

                                            firma.Item()
                                                .PaddingTop(5)
                                                .AlignCenter()
                                                .Text(
                                                    "FIRMA DE APROBACIÓN")
                                                .Bold()
                                                .FontSize(8.5f);

                                            firma.Item()
                                                .PaddingTop(2)
                                                .AlignCenter()
                                                .Text(
                                                    "Responsable de revisión")
                                                .FontSize(8);
                                        });
                                });
                        });

                    // =====================================================
                    // FOOTER
                    // =====================================================

                    page.Footer()
                        .PaddingTop(7)
                        .Column(footer =>
                        {
                            footer.Item()
                                .Height(1)
                                .Background(
                                    GrisBorde);

                            footer.Item()
                                .PaddingTop(5)
                                .Row(row =>
                                {
                                    row.RelativeItem()
                                        .Text(
                                            "DINACEN")
                                        .FontSize(7.5f)
                                        .Bold()
                                        .FontColor(
                                            AzulDinacen);

                                    row.RelativeItem()
                                        .AlignCenter()
                                        .Text(
                                            "Sistema de Gestión de Viáticos")
                                        .FontSize(7.5f)
                                        .FontColor(
                                            GrisSecundario);

                                    row.RelativeItem()
                                        .AlignRight()
                                        .Text(text =>
                                        {
                                            text.Span(
                                                "Página ")
                                                .FontSize(7.5f);

                                            text.CurrentPageNumber()
                                                .FontSize(7.5f);

                                            text.Span(
                                                " de ")
                                                .FontSize(7.5f);

                                            text.TotalPages()
                                                .FontSize(7.5f);
                                        });
                                });
                        });
                });
            });

        // =========================================================
        // GENERAR PDF DE LIQUIDACIÓN
        // =========================================================

        await Task.Run(() =>
        {
            documentoLiquidacion.GeneratePdf(
                rutaFisicaLiquidacion);
        });

        // =========================================================
        // =========================================================
        // PDF 2
        // COMPROBANTES DE LA RENDICIÓN
        // =========================================================
        // =========================================================

        var documentoComprobantes =
            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);

                    page.MarginTop(28);
                    page.MarginBottom(30);
                    page.MarginLeft(30);
                    page.MarginRight(30);

                    page.DefaultTextStyle(text =>
                        text
                            .FontFamily("Arial")
                            .FontSize(9)
                            .FontColor(
                                GrisTexto));

                    // =====================================================
                    // ENCABEZADO
                    // =====================================================

                    page.Header()
                        .Column(header =>
                        {
                            header.Spacing(7);

                            header.Item()
                                .Row(row =>
                                {
                                    // -------------------------------------------------
                                    // LOGO
                                    // -------------------------------------------------

                                    row.ConstantItem(105)
                                        .AlignLeft()
                                        .AlignMiddle()
                                        .Element(logo =>
                                        {
                                            if (logoBytes != null)
                                            {
                                                logo
                                                    .Width(92)
                                                    .Image(logoBytes)
                                                    .FitArea();
                                            }
                                            else
                                            {
                                                logo
                                                    .Text("DINACEN")
                                                    .FontSize(18)
                                                    .Bold()
                                                    .FontColor(
                                                        AzulDinacen);
                                            }
                                        });

                                    // -------------------------------------------------
                                    // TÍTULO
                                    // -------------------------------------------------

                                    row.RelativeItem()
                                        .AlignCenter()
                                        .AlignMiddle()
                                        .Column(titulo =>
                                        {
                                            titulo.Spacing(1);

                                            titulo.Item()
                                                .AlignCenter()
                                                .Text(
                                                    "COMPROBANTES")
                                                .FontSize(17)
                                                .Bold()
                                                .FontColor(
                                                    GrisTexto);

                                            titulo.Item()
                                                .AlignCenter()
                                                .Text(
                                                    "DE LA RENDICIÓN")
                                                .FontSize(17)
                                                .Bold()
                                                .FontColor(
                                                    AzulDinacen);

                                            titulo.Item()
                                                .PaddingTop(3)
                                                .AlignCenter()
                                                .Text(
                                                    $"Liquidación N.º {rendicion.IdRendicion}")
                                                .FontSize(8)
                                                .FontColor(
                                                    GrisSecundario);
                                        });

                                    // -------------------------------------------------
                                    // NÚMERO
                                    // -------------------------------------------------

                                    row.ConstantItem(105)
                                        .AlignRight()
                                        .AlignMiddle()
                                        .Column(info =>
                                        {
                                            info.Spacing(2);

                                            info.Item()
                                                .AlignRight()
                                                .Text(
                                                    "LIQUIDACIÓN")
                                                .FontSize(7.5f)
                                                .Bold()
                                                .FontColor(
                                                    GrisSecundario);

                                            info.Item()
                                                .AlignRight()
                                                .Text(
                                                    $"#{rendicion.IdRendicion}")
                                                .FontSize(15)
                                                .Bold()
                                                .FontColor(
                                                    AzulDinacen);

                                            info.Item()
                                                .PaddingTop(3)
                                                .AlignRight()
                                                .Text(
                                                    $"Emitido: {DateTime.Now:dd/MM/yyyy}")
                                                .FontSize(7.5f)
                                                .FontColor(
                                                    GrisSecundario);
                                        });
                                });

                            // =================================================
                            // LÍNEAS
                            // =================================================

                            header.Item()
                                .PaddingTop(4)
                                .Height(2)
                                .Background(
                                    AzulDinacen);

                            header.Item()
                                .Height(1)
                                .Background(
                                    VerdeDinacen);

                            // =================================================
                            // DATOS DE LA RENDICIÓN
                            // =================================================

                            header.Item()
                                .PaddingTop(7)
                                .Border(1)
                                .BorderColor(
                                    GrisBorde)
                                .Background(
                                    Colors.White)
                                .Padding(9)
                                .Column(datos =>
                                {
                                    datos.Spacing(4);

                                    DatoSimple(
                                        datos,
                                        "EMPLEADO",
                                        nombreEmpleado);

                                    DatoSimple(
                                        datos,
                                        "DESTINO",
                                        destino);

                                    DatoSimple(
                                        datos,
                                        "PERIODO",
                                        $"{rendicion.FechaInicio:dd/MM/yyyy} al {rendicion.FechaFin:dd/MM/yyyy}");
                                });
                        });

                    // =====================================================
                    // CONTENIDO
                    // =====================================================

                    page.Content()
                        .PaddingTop(14)
                        .Column(content =>
                        {
                            content.Spacing(12);

                            // =================================================
                            // TÍTULO DE GASTOS
                            // =================================================

                            content.Item()
                                .Element(seccion =>
                                {
                                    EncabezadoSeccion(
                                        seccion,
                                        "COMPROBANTES DE GASTOS");
                                });

                            // =================================================
                            // COMPROBANTES DE GASTOS
                            // =================================================

                            var numeroComprobante = 0;

                            foreach (var gasto in gastos)
                            {
                                byte[]? imagenComprobante = null;

                                if (imagenesGastos.TryGetValue(
                                        gasto.IdGasto,
                                        out var imagen))
                                {
                                    imagenComprobante =
                                        imagen;
                                }

                                if (imagenComprobante != null)
                                {
                                    numeroComprobante++;

                                    content.Item()
                                        .Border(1)
                                        .BorderColor(
                                            GrisBorde)
                                        .Background(
                                            Colors.White)
                                        .Padding(9)
                                        .Column(comprobante =>
                                        {
                                            comprobante.Spacing(7);

                                            // -----------------------------------------
                                            // CABECERA DEL COMPROBANTE
                                            // -----------------------------------------

                                            comprobante.Item()
                                                .Row(row =>
                                                {
                                                    row.RelativeItem()
                                                        .Column(datos =>
                                                        {
                                                            datos.Spacing(2);

                                                            datos.Item()
                                                                .Text(text =>
                                                                {
                                                                    text.Span(
                                                                        $"COMPROBANTE N.º {numeroComprobante}")
                                                                        .Bold()
                                                                        .FontSize(9)
                                                                        .FontColor(
                                                                            AzulDinacen);

                                                                    text.Span(
                                                                        $"   |   {gasto.Fecha:dd/MM/yyyy}")
                                                                        .FontSize(8)
                                                                        .FontColor(
                                                                            GrisSecundario);
                                                                });

                                                            datos.Item()
                                                                .Text(text =>
                                                                {
                                                                    text.Span(
                                                                        "Tipo: ")
                                                                        .Bold()
                                                                        .FontSize(8);

                                                                    text.Span(
                                                                        gasto.TipoGasto?.Nombre
                                                                        ?? "-")
                                                                        .FontSize(8);

                                                                    text.Span(
                                                                        "   |   Documento: ")
                                                                        .Bold()
                                                                        .FontSize(8);

                                                                    text.Span(
                                                                        $"{gasto.TipoComprobante?.Nombre ?? "-"} {gasto.Serie}-{gasto.Numero}")
                                                                        .FontSize(8);
                                                                });
                                                        });

                                                    row.ConstantItem(90)
                                                        .AlignRight()
                                                        .AlignMiddle()
                                                        .Text(
                                                            $"S/ {gasto.MontoTotal:N2}")
                                                        .FontSize(11)
                                                        .Bold()
                                                        .FontColor(
                                                            GrisTexto);
                                                });

                                            // -----------------------------------------
                                            // LÍNEA
                                            // -----------------------------------------

                                            comprobante.Item()
                                                .Height(1)
                                                .Background(
                                                    GrisBorde);

                                            // -----------------------------------------
                                            // DETALLE
                                            // -----------------------------------------

                                            if (!string.IsNullOrWhiteSpace(
                                                    gasto.Detalle))
                                            {
                                                comprobante.Item()
                                                    .Text(text =>
                                                    {
                                                        text.Span(
                                                            "Detalle: ")
                                                            .Bold()
                                                            .FontSize(8);

                                                        text.Span(
                                                            gasto.Detalle)
                                                            .FontSize(8);
                                                    });
                                            }

                                            // -----------------------------------------
                                            // IMAGEN
                                            // -----------------------------------------

                                            comprobante.Item()
                                                .AlignCenter()
                                                .MaxHeight(620)
                                                .Image(
                                                    imagenComprobante)
                                                .FitArea();
                                        });
                                }
                            }

                            // =================================================
                            // COMPROBANTE DE DEVOLUCIÓN
                            // =================================================

                            if (devolucion != null &&
                                imagenVoucherDevolucion != null)
                            {
                                content.Item()
                                    .PaddingTop(5)
                                    .Element(seccion =>
                                    {
                                        EncabezadoSeccion(
                                            seccion,
                                            "COMPROBANTE DE DEVOLUCIÓN DE SALDO");
                                    });

                                content.Item()
                                    .Border(1)
                                    .BorderColor(
                                        GrisBorde)
                                    .Background(
                                        Colors.White)
                                    .Padding(9)
                                    .Column(comprobante =>
                                    {
                                        comprobante.Spacing(7);

                                        comprobante.Item()
                                            .Row(row =>
                                            {
                                                row.RelativeItem()
                                                    .Column(datos =>
                                                    {
                                                        datos.Spacing(2);

                                                        datos.Item()
                                                            .Text(
                                                                "DEVOLUCIÓN DE SALDO")
                                                            .Bold()
                                                            .FontSize(9)
                                                            .FontColor(
                                                                AzulDinacen);

                                                        datos.Item()
                                                            .Text(text =>
                                                            {
                                                                text.Span(
                                                                    "Banco: ")
                                                                    .Bold()
                                                                    .FontSize(8);

                                                                text.Span(
                                                                    devolucion.Banco
                                                                    ?? "-")
                                                                    .FontSize(8);

                                                                text.Span(
                                                                    "   |   Operación: ")
                                                                    .Bold()
                                                                    .FontSize(8);

                                                                text.Span(
                                                                    devolucion.NumeroOperacion
                                                                    ?? "-")
                                                                    .FontSize(8);
                                                            });

                                                        datos.Item()
                                                            .Text(text =>
                                                            {
                                                                text.Span(
                                                                    "Fecha: ")
                                                                    .Bold()
                                                                    .FontSize(8);

                                                                text.Span(
                                                                    devolucion.Fecha
                                                                        .ToString(
                                                                            "dd/MM/yyyy"))
                                                                    .FontSize(8);
                                                            });
                                                    });

                                                row.ConstantItem(95)
                                                    .AlignRight()
                                                    .AlignMiddle()
                                                    .Text(
                                                        $"S/ {devolucion.Monto:N2}")
                                                    .FontSize(11)
                                                    .Bold()
                                                    .FontColor(
                                                        GrisTexto);
                                            });

                                        comprobante.Item()
                                            .Height(1)
                                            .Background(
                                                GrisBorde);

                                        if (!string.IsNullOrWhiteSpace(
                                                devolucion.Observaciones))
                                        {
                                            comprobante.Item()
                                                .Text(text =>
                                                {
                                                    text.Span(
                                                        "Observaciones: ")
                                                        .Bold()
                                                        .FontSize(8);

                                                    text.Span(
                                                        devolucion.Observaciones)
                                                        .FontSize(8);
                                                });
                                        }

                                        comprobante.Item()
                                            .AlignCenter()
                                            .MaxHeight(620)
                                            .Image(
                                                imagenVoucherDevolucion)
                                            .FitArea();
                                    });
                            }

                            // =================================================
                            // SI NO EXISTEN IMÁGENES
                            // =================================================

                            var cantidadImagenes =
                                imagenesGastos.Values
                                    .Count(x => x != null);

                            if (imagenVoucherDevolucion != null)
                            {
                                cantidadImagenes++;
                            }

                            if (cantidadImagenes == 0)
                            {
                                content.Item()
                                    .Border(1)
                                    .BorderColor(
                                        GrisBorde)
                                    .Background(
                                        Colors.White)
                                    .Padding(25)
                                    .AlignCenter()
                                    .Column(mensaje =>
                                    {
                                        mensaje.Spacing(7);

                                        mensaje.Item()
                                            .AlignCenter()
                                            .Text(
                                                "NO HAY COMPROBANTES ADJUNTOS")
                                            .FontSize(11)
                                            .Bold()
                                            .FontColor(
                                                AzulDinacen);

                                        mensaje.Item()
                                            .AlignCenter()
                                            .Text(
                                                "No se encontraron comprobantes de gastos o comprobantes de devolución registrados como imágenes.")
                                            .FontSize(8.5f)
                                            .FontColor(
                                                GrisSecundario);
                                    });
                            }
                        });

                    // =====================================================
                    // FOOTER
                    // =====================================================

                    page.Footer()
                        .PaddingTop(7)
                        .Column(footer =>
                        {
                            footer.Item()
                                .Height(1)
                                .Background(
                                    GrisBorde);

                            footer.Item()
                                .PaddingTop(5)
                                .Row(row =>
                                {
                                    row.RelativeItem()
                                        .Text(
                                            "DINACEN")
                                        .FontSize(7.5f)
                                        .Bold()
                                        .FontColor(
                                            AzulDinacen);

                                    row.RelativeItem()
                                        .AlignCenter()
                                        .Text(
                                            "Comprobantes de la rendición")
                                        .FontSize(7.5f)
                                        .FontColor(
                                            GrisSecundario);

                                    row.RelativeItem()
                                        .AlignRight()
                                        .Text(text =>
                                        {
                                            text.Span(
                                                "Página ")
                                                .FontSize(7.5f);

                                            text.CurrentPageNumber()
                                                .FontSize(7.5f);

                                            text.Span(
                                                " de ")
                                                .FontSize(7.5f);

                                            text.TotalPages()
                                                .FontSize(7.5f);
                                        });
                                });
                        });
                });
            });

        // =========================================================
        // GENERAR PDF DE COMPROBANTES
        // =========================================================

        await Task.Run(() =>
        {
            documentoComprobantes.GeneratePdf(
                rutaFisicaVouchers);
        });

        // =========================================================
        // RETORNAR LOS DOS PDF
        // =========================================================

        return new ResultadoPdfRendicion
        {
            // PDF PRINCIPAL
            RutaFisica =
                rutaFisicaLiquidacion,

            RutaPublica =
                rutaPublicaLiquidacion,

            NombreArchivo =
                nombreArchivoLiquidacion,

            // PDF DE COMPROBANTES
            //
            // Se mantienen los nombres de propiedades
            // "Vouchers" para compatibilidad con tu código actual.
            RutaFisicaVouchers =
                rutaFisicaVouchers,

            RutaPublicaVouchers =
                rutaPublicaVouchers,

            NombreArchivoVouchers =
                nombreArchivoVouchers
        };
    }

    // =============================================================
    // CARGAR IMAGEN DEL COMPROBANTE
    // =============================================================

    private byte[]? CargarArchivoImagen(
        string? rutaArchivo)
    {
        if (string.IsNullOrWhiteSpace(rutaArchivo))
        {
            return null;
        }

        try
        {
            var rutaRelativa =
                rutaArchivo
                    .TrimStart('/')
                    .Replace(
                        '/',
                        Path.DirectorySeparatorChar);

            var rutaFisica =
                Path.Combine(
                    _environment.WebRootPath,
                    rutaRelativa);

            if (!File.Exists(rutaFisica))
            {
                return null;
            }

            var extension =
                Path.GetExtension(rutaFisica)
                    .ToLowerInvariant();

            if (extension != ".jpg" &&
                extension != ".jpeg" &&
                extension != ".png" &&
                extension != ".webp")
            {
                return null;
            }

            return File.ReadAllBytes(
                rutaFisica);
        }
        catch
        {
            return null;
        }
    }

    // =============================================================
    // VERIFICAR SI EL ARCHIVO ES PDF
    // =============================================================

    private static bool EsArchivoPdf(
        string? rutaArchivo)
    {
        if (string.IsNullOrWhiteSpace(rutaArchivo))
        {
            return false;
        }

        return Path.GetExtension(
                rutaArchivo)
            .Equals(
                ".pdf",
                StringComparison.OrdinalIgnoreCase);
    }

    // =============================================================
    // ENCABEZADO DE SECCIÓN
    // =============================================================

    private static void EncabezadoSeccion(
        IContainer container,
        string texto)
    {
        container
            .BorderBottom(1.5f)
            .BorderColor(
                AzulDinacen)
            .PaddingBottom(5)
            .Text(texto)
            .FontSize(10.5f)
            .Bold()
            .FontColor(
                AzulDinacen);
    }

    // =============================================================
    // DATO DEL ENCABEZADO
    // =============================================================

    private static void DatoCabecera(
        IContainer container,
        string etiqueta,
        string valor)
    {
        container
            .Text(text =>
            {
                text.Span(
                        $"{etiqueta}: ")
                    .Bold()
                    .FontSize(8)
                    .FontColor(
                        GrisSecundario);

                text.Span(
                        valor)
                    .FontSize(8.5f)
                    .FontColor(
                        GrisTexto);
            });
    }

    // =============================================================
    // DATO SIMPLE
    // =============================================================

    private static void DatoSimple(
        ColumnDescriptor columna,
        string etiqueta,
        string valor)
    {
        columna.Item()
            .Text(text =>
            {
                text.Span(
                        $"{etiqueta}: ")
                    .Bold()
                    .FontSize(8)
                    .FontColor(
                        GrisSecundario);

                text.Span(
                        valor)
                    .FontSize(8.5f)
                    .FontColor(
                        GrisTexto);
            });
    }

    // =============================================================
    // CELDA CABECERA
    // =============================================================

    private static void CeldaCabecera(
        IContainer container,
        string texto)
    {
        container
            .Background(
                AzulDinacen)
            .Border(0.5f)
            .BorderColor(
                AzulDinacen)
            .Padding(5)
            .AlignMiddle()
            .Text(texto)
            .FontColor(
                Colors.White)
            .Bold()
            .FontSize(7.5f);
    }

    // =============================================================
    // CELDA DETALLE
    // =============================================================

    private static void CeldaDetalle(
        IContainer container,
        string texto)
    {
        container
            .Border(0.5f)
            .BorderColor(
                GrisBorde)
            .Padding(5)
            .Text(texto)
            .FontSize(7.8f)
            .FontColor(
                GrisTexto);
    }

    // =============================================================
    // CELDA NUMÉRICA
    // =============================================================

    private static void CeldaNumero(
        IContainer container,
        decimal monto)
    {
        container
            .Border(0.5f)
            .BorderColor(
                GrisBorde)
            .Padding(5)
            .AlignRight()
            .Text(
                monto.ToString("N2"))
            .FontSize(7.8f)
            .FontColor(
                GrisTexto);
    }

    // =============================================================
    // FILA RESUMEN
    // =============================================================

    private static void FilaResumen(
        ColumnDescriptor columna,
        string etiqueta,
        string valor,
        bool esTotal = false)
    {
        columna.Item()
            .Row(row =>
            {
                var etiquetaTexto =
                    row.RelativeItem()
                        .Text(etiqueta)
                        .FontSize(
                            esTotal
                                ? 9
                                : 8.5f)
                        .FontColor(
                            GrisTexto);

                var valorTexto =
                    row.ConstantItem(100)
                        .AlignRight()
                        .Text(valor)
                        .FontSize(
                            esTotal
                                ? 9
                                : 8.5f)
                        .FontColor(
                            GrisTexto);

                if (esTotal)
                {
                    etiquetaTexto.Bold();
                    valorTexto.Bold();
                }
            });
    }

    // =============================================================
    // DATO DEVOLUCIÓN
    // =============================================================

    private static void DatoDevolucion(
        ColumnDescriptor columna,
        string etiqueta,
        string? valor)
    {
        columna.Item()
            .Text(text =>
            {
                text.Span(
                        $"{etiqueta}: ")
                    .Bold()
                    .FontSize(8)
                    .FontColor(
                        GrisSecundario);

                text.Span(
                        valor ?? "-")
                    .FontSize(8.5f)
                    .FontColor(
                        GrisTexto);
            });
    }

    // =============================================================
    // LIMPIAR NOMBRE DE ARCHIVO
    // =============================================================

    private static string LimpiarNombreArchivo(
        string nombre)
    {
        foreach (
            var caracter
            in Path.GetInvalidFileNameChars())
        {
            nombre =
                nombre.Replace(
                    caracter,
                    '-');
        }

        return nombre
            .Replace(
                ' ',
                '-');
    }
}


// =============================================================
// RESULTADO DE LOS DOS PDF
// =============================================================

public class ResultadoPdfRendicion
{
    // =============================================================
    // PDF PRINCIPAL DE LIQUIDACIÓN
    // =============================================================

    public string RutaFisica { get; set; } =
        string.Empty;

    public string RutaPublica { get; set; } =
        string.Empty;

    public string NombreArchivo { get; set; } =
        string.Empty;

    // =============================================================
    // PDF DE COMPROBANTES
    //
    // Se conservan los nombres "Vouchers" para que
    // tu controlador actual siga funcionando.
    // =============================================================

    public string RutaFisicaVouchers { get; set; } =
        string.Empty;

    public string RutaPublicaVouchers { get; set; } =
        string.Empty;

    public string NombreArchivoVouchers { get; set; } =
        string.Empty;
}

