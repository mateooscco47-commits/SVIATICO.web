using Dinacem.Models;
using Dinacem.Models.Reporte;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Globalization;

namespace Dinacem.Services
{
    public class ReportePdfService
    {
        // ============================================================
        // COLORES
        // ============================================================

        private static readonly string AzulDinacen = "#0C4A8A";
        private static readonly string AzulOscuro = "#083763";

        private static readonly string Negro = "#111111";
        private static readonly string GrisTexto = "#222222";
        private static readonly string GrisSecundario = "#555555";
        private static readonly string GrisBorde = "#1F1F1F";
        private static readonly string GrisFondo = "#F5F7FA";

        private static readonly string Verde = "#15803D";
        private static readonly string Amarillo = "#B45309";
        private static readonly string Rojo = "#B91C1C";


        // ============================================================
        // LOGO
        // ============================================================

        private readonly IWebHostEnvironment _environment;


        public ReportePdfService(
            IWebHostEnvironment environment)
        {
            _environment = environment;
        }


        // ============================================================
        // REPORTE GENERAL
        // ============================================================

        public byte[] GenerarReporteGeneral(
            ReporteGeneral reporte)
        {
            return CrearDocumento(
                "REPORTE GENERAL DE VIÁTICOS",
                documento =>
                {
                    documento.Page(page =>
                    {
                        ConfigurarPagina(page);

                        page.Content()
                            .Column(columna =>
                            {
                                // ENCABEZADO
                                columna.Item()
                                    .Element(c =>
                                        EncabezadoReporte(
                                            c,
                                            "REPORTE GENERAL DE VIÁTICOS"));

                                // INFORMACIÓN DEL PERIODO
                                columna.Item()
                                    .PaddingTop(12)
                                    .Element(c =>
                                        InformacionPeriodo(
                                            c,
                                            reporte.FechaInicio,
                                            reporte.FechaFin));

                                // RESUMEN
                                columna.Item()
                                    .PaddingTop(14)
                                    .Element(c =>
                                        ResumenGeneral(
                                            c,
                                            reporte));

                                // TABLA
                                columna.Item()
                                    .PaddingTop(16)
                                    .Element(c =>
                                        TablaDetalleGeneral(
                                            c,
                                            reporte.Detalles));
                            });

                        PiePagina(page);
                    });
                });
        }


        // ============================================================
        // REPORTE DE RENDICIONES
        // ============================================================

        public byte[] GenerarReporteRendiciones(
            List<ReporteRendicion> reporte)
        {
            return CrearDocumento(
                "REPORTE DE RENDICIONES",
                documento =>
                {
                    documento.Page(page =>
                    {
                        ConfigurarPagina(page);

                        page.Content()
                            .Column(columna =>
                            {
                                columna.Item()
                                    .Element(c =>
                                        EncabezadoReporte(
                                            c,
                                            "REPORTE DE RENDICIONES"));

                                columna.Item()
                                    .PaddingTop(16)
                                    .Element(c =>
                                        TablaRendiciones(
                                            c,
                                            reporte));
                            });

                        PiePagina(page);
                    });
                });
        }


        // ============================================================
        // REPORTE DE GASTOS
        // ============================================================

        public byte[] GenerarReporteGastos(
            List<ReporteGasto> reporte)
        {
            return CrearDocumento(
                "REPORTE DE GASTOS POR TIPO",
                documento =>
                {
                    documento.Page(page =>
                    {
                        ConfigurarPagina(page);

                        page.Content()
                            .Column(columna =>
                            {
                                columna.Item()
                                    .Element(c =>
                                        EncabezadoReporte(
                                            c,
                                            "REPORTE DE GASTOS POR TIPO"));

                                columna.Item()
                                    .PaddingTop(16)
                                    .Element(c =>
                                        TablaGastos(
                                            c,
                                            reporte));
                            });

                        PiePagina(page);
                    });
                });
        }


        // ============================================================
        // REPORTE POR USUARIO
        // ============================================================

        public byte[] GenerarReporteUsuarios(
            List<ReporteUsuario> reporte)
        {
            return CrearDocumento(
                "REPORTE POR USUARIO",
                documento =>
                {
                    documento.Page(page =>
                    {
                        ConfigurarPagina(page);

                        page.Content()
                            .Column(columna =>
                            {
                                columna.Item()
                                    .Element(c =>
                                        EncabezadoReporte(
                                            c,
                                            "REPORTE POR USUARIO"));

                                columna.Item()
                                    .PaddingTop(16)
                                    .Element(c =>
                                        TablaUsuarios(
                                            c,
                                            reporte));
                            });

                        PiePagina(page);
                    });
                });
        }


        // ============================================================
        // REPORTE DE REEMBOLSOS
        // ============================================================

        public byte[] GenerarReporteReembolsos(
            List<ReporteReembolso> reporte)
        {
            return CrearDocumento(
                "REPORTE DE REEMBOLSOS",
                documento =>
                {
                    documento.Page(page =>
                    {
                        ConfigurarPagina(page);

                        page.Content()
                            .Column(columna =>
                            {
                                columna.Item()
                                    .Element(c =>
                                        EncabezadoReporte(
                                            c,
                                            "REPORTE DE REEMBOLSOS"));

                                columna.Item()
                                    .PaddingTop(16)
                                    .Element(c =>
                                        TablaReembolsos(
                                            c,
                                            reporte));
                            });

                        PiePagina(page);
                    });
                });
        }


        // ============================================================
        // CREAR DOCUMENTO
        // ============================================================

        private byte[] CrearDocumento(
            string titulo,
            Action<IDocumentContainer> contenido)
        {
            var documento =
                Document.Create(contenido);

            return documento.GeneratePdf();
        }


        // ============================================================
        // CONFIGURACIÓN DE PÁGINA
        // ============================================================

        private static void ConfigurarPagina(
            PageDescriptor page)
        {
            page.Size(PageSizes.A4);

            page.MarginTop(28);
            page.MarginBottom(30);
            page.MarginLeft(28);
            page.MarginRight(28);

            page.DefaultTextStyle(
                TextStyle.Default
                    .FontFamily("Arial")
                    .FontSize(9)
                    .FontColor(Negro));
        }


        // ============================================================
        // ENCABEZADO PRINCIPAL
        // ============================================================

        private void EncabezadoReporte(
            IContainer container,
            string titulo)
        {
            container
                .PaddingBottom(10)
                .BorderBottom(1.2f)
                .BorderColor(AzulDinacen)
                .Row(fila =>
                {
                    // ------------------------------------------------
                    // LOGO IZQUIERDA
                    // ------------------------------------------------

                    fila.ConstantItem(90)
                        .Height(55)
                        .AlignLeft()
                        .AlignMiddle()
                        .Element(c =>
                            LogoDinacen(c));


                    // ------------------------------------------------
                    // TÍTULO CENTRADO
                    // ------------------------------------------------

                    fila.RelativeItem()
                        .AlignCenter()
                        .AlignMiddle()
                        .Text(titulo)
                        .FontSize(15)
                        .Bold()
                        .FontColor(Negro)
                        .AlignCenter();


                    // ------------------------------------------------
                    // FECHA DERECHA
                    // ------------------------------------------------

                    fila.ConstantItem(90)
                        .AlignRight()
                        .AlignMiddle()
                        .Column(datos =>
                        {
                            datos.Item()
                                .AlignRight()
                                .Text("Fecha")
                                .FontSize(7)
                                .FontColor(GrisSecundario);

                            datos.Item()
                                .PaddingTop(2)
                                .AlignRight()
                                .Text(
                                    DateTime.Now.ToString(
                                        "dd/MM/yyyy"))
                                .FontSize(8)
                                .Bold()
                                .FontColor(Negro);

                            datos.Item()
                                .PaddingTop(1)
                                .AlignRight()
                                .Text(
                                    DateTime.Now.ToString(
                                        "HH:mm"))
                                .FontSize(7)
                                .FontColor(GrisSecundario);
                        });
                });
        }


        // ============================================================
        // LOGO DINACEN
        // ============================================================

        private void LogoDinacen(
            IContainer container)
        {
            var rutaLogo =
                Path.Combine(
                    _environment.WebRootPath,
                    "images",
                    "logo-dinacen.png");


            if (File.Exists(rutaLogo))
            {
                container
                    .Image(rutaLogo)
                    .FitArea();
            }
            else
            {
                container
                    .AlignLeft()
                    .AlignMiddle()
                    .Text("DINACEN")
                    .FontSize(14)
                    .Bold()
                    .FontColor(AzulDinacen);
            }
        }


        // ============================================================
        // INFORMACIÓN DEL PERIODO
        // ============================================================

        private static void InformacionPeriodo(
            IContainer container,
            DateTime? fechaInicio,
            DateTime? fechaFin)
        {
            container
                .Background(GrisFondo)
                .Border(0.7f)
                .BorderColor(GrisBorde)
                .PaddingVertical(7)
                .PaddingHorizontal(10)
                .AlignCenter()
                .Text(ObtenerRangoFechas(
                    fechaInicio,
                    fechaFin))
                .FontSize(8)
                .FontColor(Negro)
                .Bold();
        }


        // ============================================================
        // RESUMEN GENERAL
        // ============================================================

        private static void ResumenGeneral(
            IContainer container,
            ReporteGeneral reporte)
        {
            container.Column(columna =>
            {
                // ----------------------------------------------------
                // PRIMERA FILA
                // ----------------------------------------------------

                columna.Item()
                    .Row(fila =>
                    {
                        fila.RelativeItem()
                            .Element(c =>
                                TarjetaResumen(
                                    c,
                                    "SOLICITUDES",
                                    reporte.TotalSolicitudes
                                        .ToString(),
                                    AzulDinacen));

                        fila.ConstantItem(8);

                        fila.RelativeItem()
                            .Element(c =>
                                TarjetaResumen(
                                    c,
                                    "APROBADAS",
                                    reporte.SolicitudesAprobadas
                                        .ToString(),
                                    Verde));

                        fila.ConstantItem(8);

                        fila.RelativeItem()
                            .Element(c =>
                                TarjetaResumen(
                                    c,
                                    "TOTAL SOLICITADO",
                                    FormatearMoneda(
                                        reporte.TotalSolicitado),
                                    AzulDinacen));

                        fila.ConstantItem(8);

                        fila.RelativeItem()
                            .Element(c =>
                                TarjetaResumen(
                                    c,
                                    "SALDO PENDIENTE",
                                    FormatearMoneda(
                                        reporte.SaldoPendiente),
                                    Rojo));
                    });


                // ----------------------------------------------------
                // SEGUNDA FILA
                // ----------------------------------------------------

                columna.Item()
                    .PaddingTop(8)
                    .Row(fila =>
                    {
                        fila.RelativeItem()
                            .Element(c =>
                                TarjetaResumenPequena(
                                    c,
                                    "Pendientes",
                                    reporte.SolicitudesPendientes,
                                    Amarillo));

                        fila.ConstantItem(8);

                        fila.RelativeItem()
                            .Element(c =>
                                TarjetaResumenPequena(
                                    c,
                                    "Rechazadas",
                                    reporte.SolicitudesRechazadas,
                                    Rojo));

                        fila.ConstantItem(8);

                        fila.RelativeItem()
                            .Element(c =>
                                TarjetaResumenPequena(
                                    c,
                                    "Finalizadas",
                                    reporte.SolicitudesFinalizadas,
                                    Verde));

                        fila.ConstantItem(8);

                        fila.RelativeItem()
                            .Element(c =>
                                TarjetaResumenPequena(
                                    c,
                                    "Rendiciones pendientes",
                                    reporte.RendicionesPendientes,
                                    Amarillo));
                    });
            });
        }


        // ============================================================
        // TARJETA PRINCIPAL
        // ============================================================

        private static void TarjetaResumen(
            IContainer container,
            string titulo,
            string valor,
            string color)
        {
            container
                .Background(Colors.White)
                .Border(0.8f)
                .BorderColor(GrisBorde)
                .PaddingVertical(9)
                .PaddingHorizontal(8)
                .Column(columna =>
                {
                    columna.Item()
                        .AlignCenter()
                        .Text(titulo)
                        .FontSize(7)
                        .Bold()
                        .FontColor(GrisSecundario);

                    columna.Item()
                        .PaddingTop(4)
                        .AlignCenter()
                        .Text(valor)
                        .FontSize(12)
                        .Bold()
                        .FontColor(color);
                });
        }


        // ============================================================
        // TARJETA PEQUEÑA
        // ============================================================

        private static void TarjetaResumenPequena(
            IContainer container,
            string titulo,
            int valor,
            string color)
        {
            container
                .Background(GrisFondo)
                .Border(0.7f)
                .BorderColor(GrisBorde)
                .PaddingVertical(6)
                .PaddingHorizontal(8)
                .Row(fila =>
                {
                    fila.RelativeItem()
                        .AlignMiddle()
                        .Text(titulo)
                        .FontSize(7.5f)
                        .FontColor(Negro);

                    fila.ConstantItem(28)
                        .AlignRight()
                        .Text(valor.ToString())
                        .FontSize(10)
                        .Bold()
                        .FontColor(color);
                });
        }


        // ============================================================
        // TABLA DETALLE GENERAL
        // ============================================================

        private static void TablaDetalleGeneral(
            IContainer container,
            List<DetalleReporte> detalles)
        {
            container.Table(tabla =>
            {
                tabla.ColumnsDefinition(columnas =>
                {
                    columnas.ConstantColumn(48);
                    columnas.RelativeColumn(1.5f);
                    columnas.RelativeColumn(1.4f);
                    columnas.RelativeColumn(1.3f);
                    columnas.ConstantColumn(58);
                    columnas.ConstantColumn(58);
                    columnas.ConstantColumn(65);
                    columnas.ConstantColumn(55);
                });


                tabla.Header(cabecera =>
                {
                    CeldaCabecera(cabecera.Cell(), "Código");
                    CeldaCabecera(cabecera.Cell(), "Usuario");
                    CeldaCabecera(cabecera.Cell(), "Motivo");
                    CeldaCabecera(cabecera.Cell(), "Destino");
                    CeldaCabecera(cabecera.Cell(), "Salida");
                    CeldaCabecera(cabecera.Cell(), "Retorno");
                    CeldaCabecera(cabecera.Cell(), "Monto");
                    CeldaCabecera(cabecera.Cell(), "Estado");
                });


                if (detalles == null ||
                    detalles.Count == 0)
                {
                    tabla.Cell()
                        .ColumnSpan(8)
                        .Padding(15)
                        .Border(0.7f)
                        .BorderColor(GrisBorde)
                        .AlignCenter()
                        .Text("No existen registros.")
                        .FontColor(GrisSecundario);
                }
                else
                {
                    foreach (var item in detalles)
                    {
                        CeldaTabla(
                            tabla.Cell(),
                            $"SOL-{item.IdSolicitud:000}");

                        CeldaTabla(
                            tabla.Cell(),
                            item.Usuario);

                        CeldaTabla(
                            tabla.Cell(),
                            item.Motivo);

                        CeldaTabla(
                            tabla.Cell(),
                            item.Destino);

                        CeldaTabla(
                            tabla.Cell(),
                            item.FechaInicio
                                .ToString("dd/MM/yyyy"));

                        CeldaTabla(
                            tabla.Cell(),
                            item.FechaFin
                                .ToString("dd/MM/yyyy"));

                        CeldaTabla(
                            tabla.Cell(),
                            FormatearMoneda(
                                item.MontoSolicitado));

                        CeldaEstado(
                            tabla.Cell(),
                            item.Estado);
                    }
                }
            });
        }


        // ============================================================
        // TABLA RENDICIONES
        // ============================================================

        private static void TablaRendiciones(
            IContainer container,
            List<ReporteRendicion> reporte)
        {
            container.Table(tabla =>
            {
                tabla.ColumnsDefinition(columnas =>
                {
                    columnas.RelativeColumn(1.5f);
                    columnas.ConstantColumn(60);
                    columnas.ConstantColumn(75);
                    columnas.ConstantColumn(70);
                    columnas.ConstantColumn(65);
                    columnas.ConstantColumn(70);
                    columnas.ConstantColumn(70);
                });


                tabla.Header(cabecera =>
                {
                    CeldaCabecera(cabecera.Cell(), "Usuario");
                    CeldaCabecera(cabecera.Cell(), "Solicitud");
                    CeldaCabecera(cabecera.Cell(), "Entregado");
                    CeldaCabecera(cabecera.Cell(), "Gastado");
                    CeldaCabecera(cabecera.Cell(), "Devuelto");
                    CeldaCabecera(cabecera.Cell(), "Diferencia");
                    CeldaCabecera(cabecera.Cell(), "Estado");
                });


                if (reporte == null ||
                    reporte.Count == 0)
                {
                    tabla.Cell()
                        .ColumnSpan(7)
                        .Padding(15)
                        .Border(0.7f)
                        .BorderColor(GrisBorde)
                        .AlignCenter()
                        .Text("No existen registros.");
                }
                else
                {
                    foreach (var item in reporte)
                    {
                        CeldaTabla(
                            tabla.Cell(),
                            item.Usuario);

                        CeldaTabla(
                            tabla.Cell(),
                            item.Solicitud);

                        CeldaTabla(
                            tabla.Cell(),
                            FormatearMoneda(
                                item.MontoEntregado));

                        CeldaTabla(
                            tabla.Cell(),
                            FormatearMoneda(
                                item.Gastado));

                        CeldaTabla(
                            tabla.Cell(),
                            FormatearMoneda(
                                item.Devuelto));

                        CeldaTabla(
                            tabla.Cell(),
                            FormatearMoneda(
                                item.Diferencia));

                        CeldaEstado(
                            tabla.Cell(),
                            item.Estado);
                    }
                }
            });
        }


        // ============================================================
        // TABLA GASTOS
        // ============================================================

        private static void TablaGastos(
            IContainer container,
            List<ReporteGasto> reporte)
        {
            container.Table(tabla =>
            {
                tabla.ColumnsDefinition(columnas =>
                {
                    columnas.ConstantColumn(45);
                    columnas.RelativeColumn();
                    columnas.ConstantColumn(100);
                });


                tabla.Header(cabecera =>
                {
                    CeldaCabecera(cabecera.Cell(), "#");
                    CeldaCabecera(cabecera.Cell(), "Tipo de gasto");
                    CeldaCabecera(cabecera.Cell(), "Total");
                });


                if (reporte == null ||
                    reporte.Count == 0)
                {
                    tabla.Cell()
                        .ColumnSpan(3)
                        .Padding(15)
                        .Border(0.7f)
                        .BorderColor(GrisBorde)
                        .AlignCenter()
                        .Text("No existen registros.");
                }
                else
                {
                    int numero = 1;

                    foreach (var item in reporte)
                    {
                        CeldaTabla(
                            tabla.Cell(),
                            numero.ToString());

                        CeldaTabla(
                            tabla.Cell(),
                            item.TipoGasto);

                        CeldaTabla(
                            tabla.Cell(),
                            FormatearMoneda(
                                item.Total));

                        numero++;
                    }


                    var total =
                        reporte.Sum(x => x.Total);


                    tabla.Cell()
                        .ColumnSpan(2)
                        .Background(AzulDinacen)
                        .Border(0.7f)
                        .BorderColor(GrisBorde)
                        .Padding(6)
                        .AlignRight()
                        .Text("TOTAL")
                        .Bold()
                        .FontColor(Colors.White);


                    tabla.Cell()
                        .Background("#EAF3FB")
                        .Border(0.7f)
                        .BorderColor(GrisBorde)
                        .Padding(6)
                        .Text(
                            FormatearMoneda(total))
                        .Bold()
                        .FontColor(Negro);
                }
            });
        }


        // ============================================================
        // TABLA USUARIOS
        // ============================================================

        private static void TablaUsuarios(
            IContainer container,
            List<ReporteUsuario> reporte)
        {
            container.Table(tabla =>
            {
                tabla.ColumnsDefinition(columnas =>
                {
                    columnas.RelativeColumn(1.7f);
                    columnas.ConstantColumn(75);
                    columnas.ConstantColumn(100);
                    columnas.ConstantColumn(105);
                });


                tabla.Header(cabecera =>
                {
                    CeldaCabecera(cabecera.Cell(), "Usuario");
                    CeldaCabecera(cabecera.Cell(), "Solicitudes");
                    CeldaCabecera(cabecera.Cell(), "Total viáticos");
                    CeldaCabecera(
                        cabecera.Cell(),
                        "Rendiciones pendientes");
                });


                if (reporte == null ||
                    reporte.Count == 0)
                {
                    tabla.Cell()
                        .ColumnSpan(4)
                        .Padding(15)
                        .Border(0.7f)
                        .BorderColor(GrisBorde)
                        .AlignCenter()
                        .Text("No existen registros.");
                }
                else
                {
                    foreach (var item in reporte)
                    {
                        CeldaTabla(
                            tabla.Cell(),
                            item.Usuario);

                        CeldaTabla(
                            tabla.Cell(),
                            item.Solicitudes.ToString());

                        CeldaTabla(
                            tabla.Cell(),
                            FormatearMoneda(
                                item.TotalViaticos));

                        CeldaTabla(
                            tabla.Cell(),
                            item.RendicionesPendientes
                                .ToString());
                    }
                }
            });
        }


        // ============================================================
        // TABLA REEMBOLSOS
        // ============================================================

        private static void TablaReembolsos(
            IContainer container,
            List<ReporteReembolso> reporte)
        {
            container.Table(tabla =>
            {
                tabla.ColumnsDefinition(columnas =>
                {
                    columnas.RelativeColumn(1.5f);
                    columnas.RelativeColumn(2.2f);
                    columnas.ConstantColumn(85);
                    columnas.ConstantColumn(85);
                    columnas.ConstantColumn(70);
                });


                tabla.Header(cabecera =>
                {
                    CeldaCabecera(cabecera.Cell(), "Usuario");
                    CeldaCabecera(cabecera.Cell(), "Motivo");
                    CeldaCabecera(cabecera.Cell(), "Monto");
                    CeldaCabecera(cabecera.Cell(), "Estado");
                    CeldaCabecera(cabecera.Cell(), "Fecha");
                });


                if (reporte == null ||
                    reporte.Count == 0)
                {
                    tabla.Cell()
                        .ColumnSpan(5)
                        .Padding(15)
                        .Border(0.7f)
                        .BorderColor(GrisBorde)
                        .AlignCenter()
                        .Text("No existen registros.");
                }
                else
                {
                    foreach (var item in reporte)
                    {
                        CeldaTabla(
                            tabla.Cell(),
                            item.Usuario);

                        CeldaTabla(
                            tabla.Cell(),
                            item.Motivo);

                        CeldaTabla(
                            tabla.Cell(),
                            FormatearMoneda(
                                item.Monto));

                        CeldaEstado(
                            tabla.Cell(),
                            item.Estado);

                        CeldaTabla(
                            tabla.Cell(),
                            item.Fecha
                                .ToString("dd/MM/yyyy"));
                    }
                }
            });
        }


        // ============================================================
        // CELDA CABECERA
        // ============================================================

        private static void CeldaCabecera(
            IContainer container,
            string texto)
        {
            container
                .Background(AzulDinacen)
                .Border(0.8f)
                .BorderColor(GrisBorde)
                .PaddingVertical(7)
                .PaddingHorizontal(5)
                .AlignMiddle()
                .AlignCenter()
                .Text(texto)
                .FontSize(7.5f)
                .Bold()
                .FontColor(Colors.White);
        }


        // ============================================================
        // CELDA TABLA
        // ============================================================

        private static void CeldaTabla(
            IContainer container,
            string? texto)
        {
            container
                .Border(0.6f)
                .BorderColor(GrisBorde)
                .Background(Colors.White)
                .PaddingVertical(6)
                .PaddingHorizontal(5)
                .AlignMiddle()
                .Text(texto ?? "-")
                .FontSize(7.5f)
                .FontColor(Negro);
        }


        // ============================================================
        // CELDA ESTADO
        // ============================================================

        private static void CeldaEstado(
            IContainer container,
            string? estado)
        {
            string colorFondo;
            string colorTexto;


            switch (estado?.ToLower())
            {
                case "aprobado":
                case "aprobada":
                case "finalizado":
                case "finalizada":
                case "pagado":

                    colorFondo = "#DCFCE7";
                    colorTexto = Verde;

                    break;


                case "pendiente":

                    colorFondo = "#FEF3C7";
                    colorTexto = Amarillo;

                    break;


                case "rechazado":
                case "rechazada":

                    colorFondo = "#FEE2E2";
                    colorTexto = Rojo;

                    break;


                default:

                    colorFondo = "#E5E7EB";
                    colorTexto = Negro;

                    break;
            }


            container
                .Background(colorFondo)
                .Border(0.6f)
                .BorderColor(GrisBorde)
                .PaddingVertical(5)
                .PaddingHorizontal(5)
                .AlignCenter()
                .Text(estado ?? "-")
                .FontSize(7)
                .Bold()
                .FontColor(colorTexto);
        }


        // ============================================================
        // PIE DE PÁGINA
        // ============================================================

        private static void PiePagina(
            PageDescriptor page)
        {
            page.Footer()
                .PaddingTop(7)
                .BorderTop(0.7f)
                .BorderColor(GrisBorde)
                .Row(fila =>
                {
                    fila.RelativeItem()
                        .Text(
                            "DINACEN | Sistema de Gestión de Viáticos")
                        .FontSize(7)
                        .FontColor(GrisSecundario);


                    fila.RelativeItem()
                        .AlignRight()
                        .Text(texto =>
                        {
                            texto.Span("Página ")
                                .FontSize(7)
                                .FontColor(GrisSecundario);

                            texto.CurrentPageNumber()
                                .FontSize(7)
                                .Bold()
                                .FontColor(Negro);

                            texto.Span(" de ")
                                .FontSize(7)
                                .FontColor(GrisSecundario);

                            texto.TotalPages()
                                .FontSize(7)
                                .Bold()
                                .FontColor(Negro);
                        });
                });
        }


        // ============================================================
        // RANGO DE FECHAS
        // ============================================================

        private static string ObtenerRangoFechas(
            DateTime? fechaInicio,
            DateTime? fechaFin)
        {
            if (!fechaInicio.HasValue &&
                !fechaFin.HasValue)
            {
                return "Periodo: Todos los registros";
            }


            if (fechaInicio.HasValue &&
                fechaFin.HasValue)
            {
                return
                    $"Periodo: {fechaInicio.Value:dd/MM/yyyy} - {fechaFin.Value:dd/MM/yyyy}";
            }


            if (fechaInicio.HasValue)
            {
                return
                    $"Periodo: Desde {fechaInicio.Value:dd/MM/yyyy}";
            }


            return
                $"Periodo: Hasta {fechaFin!.Value:dd/MM/yyyy}";
        }


        // ============================================================
        // FORMATO MONEDA
        // ============================================================

        private static string FormatearMoneda(
            decimal monto)
        {
            return
                $"S/ {monto.ToString(
                    "N2",
                    CultureInfo.GetCultureInfo("es-PE"))}";
        }
    }
}
