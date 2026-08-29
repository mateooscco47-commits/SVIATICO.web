using Dinacem.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

public class RendicionPdfService
{
    private readonly IWebHostEnvironment _environment;

    public RendicionPdfService(IWebHostEnvironment environment)
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

        var nombreEmpleado =
            $"{rendicion.Usuario?.Nombres} {rendicion.Usuario?.Apellidos}".Trim();

        if (string.IsNullOrWhiteSpace(nombreEmpleado))
        {
            nombreEmpleado = $"Usuario {rendicion.IdUsuario}";
        }

        var totalBase =
            gastos.Sum(g => g.ValorVenta);

        var totalIgv =
            gastos.Sum(g => g.IGV);

        var totalGastos =
            gastos.Sum(g => g.MontoTotal);

        var totalVehiculo =
            bitacorasVehiculo.Sum(b => b.MontoAsignado);

        var totalKm =
            bitacorasVehiculo.Sum(b => b.DistanciaKm);

        var totalRendido =
            totalGastos + totalVehiculo;

        var montoAprobado =
            rendicion.Solicitud?.Monto ?? 0;

        var saldoCalculado =
            montoAprobado - totalRendido;

        var nombreSeguroEmpleado =
            LimpiarNombreArchivo(nombreEmpleado);

        var nombreArchivo =
            $"Liquidacion-{rendicion.IdRendicion}-{nombreSeguroEmpleado}.pdf";

        var carpeta =
            Path.Combine(
                _environment.WebRootPath,
                "liquidaciones");

        Directory.CreateDirectory(carpeta);

        var rutaFisica =
            Path.Combine(
                carpeta,
                nombreArchivo);

        var rutaPublica =
            $"/liquidaciones/{nombreArchivo}";

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
                await File.ReadAllBytesAsync(rutaLogo);
        }

        // =========================================================
        // CARGAR VOUCHERS ANTES DE CREAR EL DOCUMENTO
        // =========================================================

        var imagenesGastos =
            new Dictionary<int, byte[]?>();

        foreach (var gasto in gastos)
        {
            imagenesGastos[gasto.IdGasto] =
                CargarArchivoImagen(
                    gasto.Comprobante);
        }

        byte[]? imagenVoucherDevolucion = null;

        if (devolucion != null)
        {
            imagenVoucherDevolucion =
                CargarArchivoImagen(
                    devolucion.Voucher);
        }

        // =========================================================
        // GENERAR DOCUMENTO
        // =========================================================

        var documento =
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
                            .FontSize(10)
                            .FontColor(Colors.Black));

                    // =================================================
                    // ENCABEZADO
                    // =================================================

                    page.Header()
                        .Column(header =>
                        {
                            header.Spacing(8);

                            // =================================================
                            // LOGO + TÍTULO + INFORMACIÓN
                            // =================================================

                            header.Item()
                                .Row(row =>
                                {
                                    // -------------------------------------------------
                                    // LOGO IZQUIERDO
                                    // -------------------------------------------------

                                    row.ConstantItem(105)
                                        .AlignLeft()
                                        .AlignMiddle()
                                        .Element(logo =>
                                        {
                                            if (logoBytes != null)
                                            {
                                                logo
                                                    .Width(95)
                                                    .Image(logoBytes)
                                                    .FitArea();
                                            }
                                            else
                                            {
                                                logo
                                                    .Text("DINACEN")
                                                    .FontSize(18)
                                                    .Bold()
                                                    .FontColor("#0C4A8A");
                                            }
                                        });

                                    // -------------------------------------------------
                                    // TÍTULO CENTRADO
                                    // -------------------------------------------------

                                    row.RelativeItem()
                                        .AlignCenter()
                                        .AlignMiddle()
                                        .Column(titulo =>
                                        {
                                            titulo.Item()
                                                .AlignCenter()
                                                .Text("LIQUIDACIÓN DE GASTOS")
                                                .FontSize(18)
                                                .Bold()
                                                .FontColor(Colors.Black);

                                            titulo.Item()
                                                .AlignCenter()
                                                .Text("DE VIÁTICOS")
                                                .FontSize(18)
                                                .Bold()
                                                .FontColor("#0C4A8A");

                                            titulo.Item()
                                                .PaddingTop(3)
                                                .AlignCenter()
                                                .Text("Sistema de Gestión de Viáticos")
                                                .FontSize(9)
                                                .FontColor(Colors.Grey.Darken2);
                                        });

                                    // -------------------------------------------------
                                    // INFORMACIÓN DERECHA
                                    // -------------------------------------------------

                                    row.ConstantItem(105)
                                        .AlignRight()
                                        .AlignMiddle()
                                        .Column(info =>
                                        {
                                            info.Item()
                                                .AlignRight()
                                                .Text("N.º LIQUIDACIÓN")
                                                .FontSize(8)
                                                .Bold()
                                                .FontColor(Colors.Grey.Darken2);

                                            info.Item()
                                                .AlignRight()
                                                .Text($"#{rendicion.IdRendicion}")
                                                .FontSize(16)
                                                .Bold()
                                                .FontColor("#0C4A8A");

                                            info.Item()
                                                .PaddingTop(4)
                                                .AlignRight()
                                                .Text(
                                                    $"Emitido: {DateTime.Now:dd/MM/yyyy}")
                                                .FontSize(8)
                                                .FontColor(Colors.Grey.Darken2);
                                        });
                                });

                            // =================================================
                            // LÍNEAS INSTITUCIONALES
                            // =================================================

                            header.Item()
                                .Height(3)
                                .Background("#0C4A8A");

                            header.Item()
                                .Height(2)
                                .Background("#6AA84F");

                            // =================================================
                            // DATOS DEL EMPLEADO
                            // =================================================

                            header.Item()
                                .PaddingTop(8)
                                .Border(1)
                                .BorderColor("#D8E0E7")
                                .Background("#F7F9FB")
                                .Padding(10)
                                .Column(datos =>
                                {
                                    datos.Spacing(6);

                                    // -------------------------------------------------
                                    // FILA 1
                                    // -------------------------------------------------

                                    datos.Item()
                                        .Row(row =>
                                        {
                                            row.RelativeItem()
                                                .Text(text =>
                                                {
                                                    text.Span("EMPLEADO: ")
                                                        .Bold();

                                                    text.Span(nombreEmpleado);
                                                });

                                            row.RelativeItem()
                                                .Text(text =>
                                                {
                                                    text.Span("PERIODO: ")
                                                        .Bold();

                                                    text.Span(
                                                        $"{rendicion.FechaInicio:dd/MM/yyyy} " +
                                                        $"al {rendicion.FechaFin:dd/MM/yyyy}");
                                                });
                                        });

                                    // -------------------------------------------------
                                    // FILA 2
                                    // -------------------------------------------------

                                    datos.Item()
                                        .Row(row =>
                                        {
                                            row.RelativeItem()
                                                .Text(text =>
                                                {
                                                    text.Span("CORREO: ")
                                                        .Bold();

                                                    text.Span(
                                                        rendicion.Usuario?.Correo ?? "-");
                                                });

                                            row.RelativeItem()
                                                .Text(text =>
                                                {
                                                    text.Span("CELULAR: ")
                                                        .Bold();

                                                    text.Span(
                                                        rendicion.Usuario?.Celular ?? "-");
                                                });
                                        });

                                    // -------------------------------------------------
                                    // FILA 3
                                    // -------------------------------------------------

                                    datos.Item()
                                        .Row(row =>
                                        {
                                            row.RelativeItem()
                                                .Text(text =>
                                                {
                                                    text.Span("DESTINO: ")
                                                        .Bold();

                                                    text.Span(
                                                        rendicion.Solicitud?.Destino ?? "-");
                                                });

                                            row.RelativeItem()
                                                .Text(text =>
                                                {
                                                    text.Span("MONTO APROBADO: ")
                                                        .Bold();

                                                    text.Span(
                                                        $"S/ {montoAprobado:N2}");
                                                });
                                        });
                                });
                        });

                    // =========================================================
                    // CONTENIDO
                    // =========================================================

                    page.Content()
                        .PaddingTop(15)
                        .Column(content =>
                        {
                            content.Spacing(12);

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
                                        table.ColumnsDefinition(columns =>
                                        {
                                            columns.ConstantColumn(55);
                                            columns.RelativeColumn(1.15f);
                                            columns.RelativeColumn(1.35f);
                                            columns.RelativeColumn(1.6f);
                                            columns.ConstantColumn(62);
                                            columns.ConstantColumn(52);
                                            columns.ConstantColumn(62);
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
                                                gasto.Fecha.ToString("dd/MM/yyyy"));

                                            CeldaDetalle(
                                                table.Cell(),
                                                gasto.TipoGasto?.Nombre ?? "-");

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
                                    .BorderColor("#D8E0E7")
                                    .Padding(10)
                                    .AlignCenter()
                                    .Text(
                                        "No se registraron gastos con comprobante.")
                                    .FontSize(10);
                            }

                            // =================================================
                            // VOUCHERS DE GASTOS
                            // =================================================

                            if (gastos.Any())
                            {
                                content.Item()
                                    .PaddingTop(8)
                                    .Element(seccion =>
                                    {
                                        EncabezadoSeccion(
                                            seccion,
                                            "VOUCHERS DE GASTOS");
                                    });

                                foreach (var gasto in gastos)
                                {
                                    byte[]? imagenVoucher = null;

                                    if (imagenesGastos.TryGetValue(
                                            gasto.IdGasto,
                                            out var imagen))
                                    {
                                        imagenVoucher = imagen;
                                    }

                                    content.Item()
                                        .Element(contenedor =>
                                        {
                                            contenedor
                                                .Border(1)
                                                .BorderColor("#D8E0E7")
                                                .Background("#F7F9FB")
                                                .Padding(10)
                                                .Column(voucher =>
                                                {
                                                    voucher.Spacing(7);

                                                    voucher.Item()
                                                        .Row(row =>
                                                        {
                                                            row.RelativeItem()
                                                                .Text(text =>
                                                                {
                                                                    text.Span(
                                                                        $"Gasto del {gasto.Fecha:dd/MM/yyyy}")
                                                                        .Bold()
                                                                        .FontSize(9);

                                                                    text.Span(
                                                                        $"  |  {gasto.TipoGasto?.Nombre ?? "-"}")
                                                                        .FontSize(9);

                                                                    text.Span(
                                                                        $"  |  S/ {gasto.MontoTotal:N2}")
                                                                        .Bold()
                                                                        .FontSize(9);
                                                                });

                                                            row.ConstantItem(90)
                                                                .AlignRight()
                                                                .Text(
                                                                    gasto.TipoComprobante?.Nombre ?? "-")
                                                                .FontSize(8)
                                                                .FontColor(
                                                                    Colors.Grey.Darken2);
                                                        });

                                                    voucher.Item()
                                                        .Height(1)
                                                        .Background("#D8E0E7");

                                                    // -------------------------------------------------
                                                    // IMAGEN
                                                    // -------------------------------------------------

                                                    if (imagenVoucher != null)
                                                    {
                                                        voucher.Item()
                                                            .AlignCenter()
                                                            .MaxHeight(480)
                                                            .Image(imagenVoucher)
                                                            .FitArea();
                                                    }
                                                    else if (
                                                        EsArchivoPdf(
                                                            gasto.Comprobante))
                                                    {
                                                        voucher.Item()
                                                            .AlignCenter()
                                                            .Padding(20)
                                                            .Text(
                                                                "El comprobante fue adjuntado como archivo PDF.")
                                                            .FontSize(10)
                                                            .Bold()
                                                            .FontColor("#0C4A8A");
                                                    }
                                                    else if (
                                                        !string.IsNullOrWhiteSpace(
                                                            gasto.Comprobante))
                                                    {
                                                        voucher.Item()
                                                            .AlignCenter()
                                                            .Padding(20)
                                                            .Text(
                                                                "No se pudo cargar la imagen del comprobante.")
                                                            .FontSize(9)
                                                            .FontColor(
                                                                Colors.Grey.Darken2);
                                                    }
                                                    else
                                                    {
                                                        voucher.Item()
                                                            .AlignCenter()
                                                            .Padding(20)
                                                            .Text(
                                                                "Este gasto no tiene un comprobante adjunto.")
                                                            .FontSize(9)
                                                            .FontColor(
                                                                Colors.Grey.Darken2);
                                                    }
                                                });
                                        });
                                }
                            }

                            // =================================================
                            // BITÁCORA DE VEHÍCULO
                            // =================================================

                            if (bitacorasVehiculo.Any())
                            {
                                content.Item()
                                    .PaddingTop(5)
                                    .Element(seccion =>
                                    {
                                        EncabezadoSeccion(
                                            seccion,
                                            "BITÁCORA DE VEHÍCULO");
                                    });

                                content.Item()
                                    .Table(table =>
                                    {
                                        table.ColumnsDefinition(columns =>
                                        {
                                            columns.ConstantColumn(52);
                                            columns.RelativeColumn(1.15f);
                                            columns.RelativeColumn(1.15f);
                                            columns.ConstantColumn(58);
                                            columns.ConstantColumn(58);
                                            columns.RelativeColumn(1.25f);
                                            columns.ConstantColumn(62);
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

                                        foreach (var bitacora in bitacorasVehiculo)
                                        {
                                            CeldaDetalle(
                                                table.Cell(),
                                                bitacora.Fecha.ToString("dd/MM/yyyy"));

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

                                content.Item()
                                    .AlignRight()
                                    .Width(280)
                                    .Border(1)
                                    .BorderColor("#D8E0E7")
                                    .Background("#F7F9FB")
                                    .Padding(10)
                                    .Column(resumenVehiculo =>
                                    {
                                        resumenVehiculo.Spacing(5);

                                        FilaResumen(
                                            resumenVehiculo,
                                            "Distancia total:",
                                            $"{totalKm:N2} km");

                                        var tarifasUsadas =
                                            bitacorasVehiculo
                                                .Select(b => b.TarifaKilometro)
                                                .Distinct()
                                                .ToList();

                                        if (tarifasUsadas.Count == 1)
                                        {
                                            FilaResumen(
                                                resumenVehiculo,
                                                "Tarifa aplicada:",
                                                $"S/ {tarifasUsadas[0]:N2} / km");
                                        }
                                        else if (tarifasUsadas.Count > 1)
                                        {
                                            FilaResumen(
                                                resumenVehiculo,
                                                "Tarifa aplicada:",
                                                "Varias tarifas");
                                        }

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
                                .PaddingTop(4)
                                .Element(seccion =>
                                {
                                    EncabezadoSeccion(
                                        seccion,
                                        "RESUMEN DE LA LIQUIDACIÓN");
                                });

                            content.Item()
                                .Row(row =>
                                {
                                    row.RelativeItem()
                                        .Border(1)
                                        .BorderColor("#D8E0E7")
                                        .Padding(12)
                                        .Column(resumen =>
                                        {
                                            resumen.Spacing(7);

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
                                                .PaddingTop(4)
                                                .Height(1)
                                                .Background("#D8E0E7");

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
                                                "SALDO:",
                                                $"S/ {saldoCalculado:N2}",
                                                true);
                                        });

                                    row.ConstantItem(12);

                                    row.ConstantItem(180)
                                        .Border(1)
                                        .BorderColor("#0C4A8A")
                                        .Background("#F1F6FA")
                                        .Padding(12)
                                        .Column(resumenFinal =>
                                        {
                                            resumenFinal.Spacing(7);

                                            resumenFinal.Item()
                                                .AlignCenter()
                                                .Text("TOTAL RENDIDO")
                                                .FontSize(10)
                                                .Bold()
                                                .FontColor("#0C4A8A");

                                            resumenFinal.Item()
                                                .PaddingTop(5)
                                                .AlignCenter()
                                                .Text(
                                                    $"S/ {totalRendido:N2}")
                                                .FontSize(20)
                                                .Bold()
                                                .FontColor(Colors.Black);

                                            resumenFinal.Item()
                                                .Height(2)
                                                .Background("#6AA84F");

                                            resumenFinal.Item()
                                                .PaddingTop(5)
                                                .AlignCenter()
                                                .Text(
                                                    saldoCalculado >= 0
                                                        ? "SALDO A DEVOLVER"
                                                        : "EXCESO RENDIDO")
                                                .FontSize(9)
                                                .Bold()
                                                .FontColor(
                                                    Colors.Grey.Darken2);

                                            resumenFinal.Item()
                                                .AlignCenter()
                                                .Text(
                                                    $"S/ {Math.Abs(saldoCalculado):N2}")
                                                .FontSize(15)
                                                .Bold()
                                                .FontColor(Colors.Black);
                                        });
                                });

                            // =================================================
                            // DEVOLUCIÓN DE SALDO
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
                                    .BorderColor("#D8E0E7")
                                    .Background("#F7F9FB")
                                    .Padding(12)
                                    .Row(row =>
                                    {
                                        row.RelativeItem()
                                            .Column(datos =>
                                            {
                                                datos.Spacing(5);

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
                                                        .ToString("dd/MM/yyyy"));

                                                if (!string.IsNullOrWhiteSpace(
                                                        devolucion.Observaciones))
                                                {
                                                    DatoDevolucion(
                                                        datos,
                                                        "Observaciones",
                                                        devolucion.Observaciones);
                                                }
                                            });

                                        row.ConstantItem(150)
                                            .AlignMiddle()
                                            .Column(monto =>
                                            {
                                                monto.Item()
                                                    .AlignCenter()
                                                    .Text("MONTO DEVUELTO")
                                                    .FontSize(9)
                                                    .Bold()
                                                    .FontColor("#0C4A8A");

                                                monto.Item()
                                                    .PaddingTop(4)
                                                    .AlignCenter()
                                                    .Text(
                                                        $"S/ {devolucion.Monto:N2}")
                                                    .FontSize(17)
                                                    .Bold()
                                                    .FontColor(Colors.Black);
                                            });
                                    });

                                // =================================================
                                // VOUCHER DEVOLUCIÓN
                                // =================================================

                                content.Item()
                                    .PaddingTop(8)
                                    .Element(seccion =>
                                    {
                                        EncabezadoSeccion(
                                            seccion,
                                            "VOUCHER DE DEVOLUCIÓN");
                                    });

                                content.Item()
                                    .Border(1)
                                    .BorderColor("#D8E0E7")
                                    .Background("#F7F9FB")
                                    .Padding(10)
                                    .Column(voucher =>
                                    {
                                        voucher.Spacing(7);

                                        voucher.Item()
                                            .Text(
                                                $"Banco: {devolucion.Banco}  |  " +
                                                $"Operación: {devolucion.NumeroOperacion}  |  " +
                                                $"Monto: S/ {devolucion.Monto:N2}")
                                            .FontSize(9)
                                            .Bold();

                                        voucher.Item()
                                            .Height(1)
                                            .Background("#D8E0E7");

                                        if (imagenVoucherDevolucion != null)
                                        {
                                            voucher.Item()
                                                .AlignCenter()
                                                .MaxHeight(500)
                                                .Image(
                                                    imagenVoucherDevolucion)
                                                .FitArea();
                                        }
                                        else if (
                                            EsArchivoPdf(
                                                devolucion.Voucher))
                                        {
                                            voucher.Item()
                                                .AlignCenter()
                                                .Padding(20)
                                                .Text(
                                                    "El voucher de devolución fue adjuntado como archivo PDF.")
                                                .FontSize(10)
                                                .Bold()
                                                .FontColor("#0C4A8A");
                                        }
                                        else if (
                                            !string.IsNullOrWhiteSpace(
                                                devolucion.Voucher))
                                        {
                                            voucher.Item()
                                                .AlignCenter()
                                                .Padding(20)
                                                .Text(
                                                    "No se pudo cargar la imagen del voucher de devolución.")
                                                .FontSize(9)
                                                .FontColor(
                                                    Colors.Grey.Darken2);
                                        }
                                        else
                                        {
                                            voucher.Item()
                                                .AlignCenter()
                                                .Padding(20)
                                                .Text(
                                                    "No se adjuntó voucher de devolución.")
                                                .FontSize(9)
                                                .FontColor(
                                                    Colors.Grey.Darken2);
                                        }
                                    });
                            }

                            // =================================================
                            // FIRMAS
                            // =================================================

                            content.Item()
                                .PaddingTop(45)
                                .Row(firmas =>
                                {
                                    firmas.RelativeItem()
                                        .AlignCenter()
                                        .Column(firma =>
                                        {
                                            firma.Item()
                                                .Width(200)
                                                .LineHorizontal(1);

                                            firma.Item()
                                                .PaddingTop(5)
                                                .AlignCenter()
                                                .Text("FIRMA DEL EMPLEADO")
                                                .Bold()
                                                .FontSize(9);

                                            firma.Item()
                                                .PaddingTop(3)
                                                .AlignCenter()
                                                .Text(nombreEmpleado)
                                                .FontSize(9);
                                        });

                                    firmas.ConstantItem(80);

                                    firmas.RelativeItem()
                                        .AlignCenter()
                                        .Column(firma =>
                                        {
                                            firma.Item()
                                                .Width(200)
                                                .LineHorizontal(1);

                                            firma.Item()
                                                .PaddingTop(5)
                                                .AlignCenter()
                                                .Text("FIRMA DE APROBACIÓN")
                                                .Bold()
                                                .FontSize(9);

                                            firma.Item()
                                                .PaddingTop(3)
                                                .AlignCenter()
                                                .Text("Responsable de revisión")
                                                .FontSize(9);
                                        });
                                });
                        });

                    // =========================================================
                    // FOOTER
                    // =========================================================

                    page.Footer()
                        .PaddingTop(8)
                        .Column(footer =>
                        {
                            footer.Item()
                                .Height(1)
                                .Background("#D8E0E7");

                            footer.Item()
                                .PaddingTop(6)
                                .Row(row =>
                                {
                                    row.RelativeItem()
                                        .Text("DINACEN")
                                        .FontSize(8)
                                        .Bold()
                                        .FontColor("#0C4A8A");

                                    row.RelativeItem()
                                        .AlignCenter()
                                        .Text("Sistema de Gestión de Viáticos")
                                        .FontSize(8)
                                        .FontColor(
                                            Colors.Grey.Darken2);

                                    row.RelativeItem()
                                        .AlignRight()
                                        .Text(text =>
                                        {
                                            text.Span("Página ")
                                                .FontSize(8);

                                            text.CurrentPageNumber()
                                                .FontSize(8);

                                            text.Span(" de ")
                                                .FontSize(8);

                                            text.TotalPages()
                                                .FontSize(8);
                                        });
                                });
                        });
                });
            });

        // =========================================================
        // GENERAR PDF
        // =========================================================

        await Task.Run(() =>
        {
            documento.GeneratePdf(rutaFisica);
        });

        return new ResultadoPdfRendicion
        {
            RutaFisica = rutaFisica,
            RutaPublica = rutaPublica,
            NombreArchivo = nombreArchivo
        };
    }

    // =========================================================
    // CARGAR IMAGEN DEL COMPROBANTE
    // =========================================================

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

            return File.ReadAllBytes(rutaFisica);
        }
        catch
        {
            return null;
        }
    }

    // =========================================================
    // VERIFICAR SI EL ARCHIVO ES PDF
    // =========================================================

    private static bool EsArchivoPdf(
        string? rutaArchivo)
    {
        if (string.IsNullOrWhiteSpace(rutaArchivo))
        {
            return false;
        }

        return Path.GetExtension(rutaArchivo)
            .Equals(
                ".pdf",
                StringComparison.OrdinalIgnoreCase);
    }

    // =========================================================
    // ENCABEZADO DE SECCIÓN
    // =========================================================

    private static void EncabezadoSeccion(
        IContainer container,
        string texto)
    {
        container
            .BorderBottom(2)
            .BorderColor("#0C4A8A")
            .PaddingBottom(5)
            .Text(texto)
            .FontSize(11)
            .Bold()
            .FontColor("#0C4A8A");
    }

    // =========================================================
    // CELDA CABECERA
    // =========================================================

    private static void CeldaCabecera(
        IContainer container,
        string texto)
    {
        container
            .Background("#0C4A8A")
            .Border(0.5f)
            .BorderColor("#D8E0E7")
            .Padding(6)
            .AlignMiddle()
            .Text(texto)
            .FontColor(Colors.White)
            .Bold()
            .FontSize(8);
    }

    // =========================================================
    // CELDA DETALLE
    // =========================================================

    private static void CeldaDetalle(
        IContainer container,
        string texto)
    {
        container
            .Border(0.5f)
            .BorderColor("#D8E0E7")
            .Padding(6)
            .Text(texto)
            .FontSize(8.5f)
            .FontColor(Colors.Black);
    }

    // =========================================================
    // CELDA NUMÉRICA
    // =========================================================

    private static void CeldaNumero(
        IContainer container,
        decimal monto)
    {
        container
            .Border(0.5f)
            .BorderColor("#D8E0E7")
            .Padding(6)
            .AlignRight()
            .Text(monto.ToString("N2"))
            .FontSize(8.5f)
            .FontColor(Colors.Black);
    }

    // =========================================================
    // FILA RESUMEN
    // =========================================================

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
                        .FontSize(esTotal ? 10 : 9)
                        .FontColor(Colors.Black);

                var valorTexto =
                    row.ConstantItem(100)
                        .AlignRight()
                        .Text(valor)
                        .FontSize(esTotal ? 10 : 9)
                        .FontColor(Colors.Black);

                if (esTotal)
                {
                    etiquetaTexto.Bold();
                    valorTexto.Bold();
                }
            });
    }

    // =========================================================
    // DATO DEVOLUCIÓN
    // =========================================================

    private static void DatoDevolucion(
        ColumnDescriptor columna,
        string etiqueta,
        string? valor)
    {
        columna.Item()
            .Text(text =>
            {
                text.Span($"{etiqueta}: ")
                    .Bold()
                    .FontSize(9);

                text.Span(valor ?? "-")
                    .FontSize(9);
            });
    }

    // =========================================================
    // LIMPIAR NOMBRE DE ARCHIVO
    // =========================================================

    private static string LimpiarNombreArchivo(
        string nombre)
    {
        foreach (var caracter in
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

// =========================================================
// RESULTADO PDF
// =========================================================

public class ResultadoPdfRendicion
{
    public string RutaFisica { get; set; } =
        string.Empty;

    public string RutaPublica { get; set; } =
        string.Empty;

    public string NombreArchivo { get; set; } =
        string.Empty;
}
