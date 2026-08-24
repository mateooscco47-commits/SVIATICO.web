using Dinacem.Models.Reporte;
using Microsoft.EntityFrameworkCore;
using Dinacem.Models;

namespace Dinacem.Services
{

    public class ReporteService
    {
        private readonly AplicacionDbContexto _context;


        public ReporteService(
            AplicacionDbContexto context)
        {
            _context = context;
        }



        // =====================================================
        // REPORTE GENERAL
        // =====================================================

        public async Task<ReporteGeneral> ObtenerReporteGeneral(
            DateTime? fechaInicio,
            DateTime? fechaFin)
        {

            var query = _context.Solicitudes

                .Include(x => x.Usuario)

                .Include(x => x.EstadoSolicitud)

                .Include(x => x.Rendicion)
                    .ThenInclude(x => x.Gastos)

                .AsQueryable();



            // =================================================
            // FILTRO FECHA INICIO
            // =================================================

            if (fechaInicio.HasValue)
            {
                query = query.Where(x =>
                    x.FechaInicio >= fechaInicio.Value);
            }



            // =================================================
            // FILTRO FECHA FIN
            // =================================================

            if (fechaFin.HasValue)
            {
                query = query.Where(x =>
                    x.FechaFin <= fechaFin.Value);
            }



            var solicitudes =
                await query.ToListAsync();



            // =================================================
            // CREAR REPORTE
            // =================================================

            var reporte =
                new ReporteGeneral();



            // =================================================
            // TOTAL SOLICITUDES
            // =================================================

            reporte.TotalSolicitudes =
                solicitudes.Count;



            // =================================================
            // SOLICITUDES PENDIENTES
            // =================================================

            reporte.SolicitudesPendientes =
                solicitudes.Count(x =>
                    x.EstadoSolicitud!.Nombre == "Pendiente");



            // =================================================
            // SOLICITUDES APROBADAS
            // =================================================

            reporte.SolicitudesAprobadas =
                solicitudes.Count(x =>
                    x.EstadoSolicitud!.Nombre == "Aprobado");



            // =================================================
            // SOLICITUDES RECHAZADAS
            // =================================================

            reporte.SolicitudesRechazadas =
                solicitudes.Count(x =>
                    x.EstadoSolicitud!.Nombre == "Rechazado");



            // =================================================
            // SOLICITUDES FINALIZADAS
            // =================================================

            reporte.SolicitudesFinalizadas =
                solicitudes.Count(x =>
                    x.EstadoSolicitud!.Nombre == "Finalizado");



            // =================================================
            // TOTAL SOLICITADO
            // =================================================

            reporte.TotalSolicitado =
                solicitudes.Sum(x =>
                    x.Monto);



            // =================================================
            // TOTAL RENDIDO
            // =================================================

            reporte.TotalRendido =
                solicitudes

                .Where(x =>
                    x.Rendicion != null)

                .Sum(x =>
                    x.Rendicion!.Total);



            // =================================================
            // TOTAL GASTADO
            // =================================================

            reporte.TotalGastado =
                solicitudes

                .Where(x =>
                    x.Rendicion != null)

                .SelectMany(x =>
                    x.Rendicion!.Gastos)

                .Sum(x =>
                    x.MontoTotal);



            // =================================================
            // SALDO PENDIENTE
            // =================================================

            reporte.SaldoPendiente =
                reporte.TotalSolicitado -
                reporte.TotalRendido;



            // =================================================
            // RENDICIONES PENDIENTES
            // =================================================

            reporte.RendicionesPendientes =
                solicitudes.Count(x =>
                    x.Rendicion == null);



            // =================================================
            // FECHAS DEL REPORTE
            // =================================================

            reporte.FechaInicio =
                fechaInicio;

            reporte.FechaFin =
                fechaFin;



            return reporte;
        }





        // =====================================================
        // DETALLE REPORTE GENERAL
        // =====================================================

        public async Task<List<DetalleReporte>>
            ObtenerDetalleReporte(
                DateTime? fechaInicio,
                DateTime? fechaFin)
        {

            var query =
                _context.Solicitudes

                .Include(x =>
                    x.Usuario)

                .Include(x =>
                    x.EstadoSolicitud)

                .Include(x =>
                    x.Rendicion)

                .AsQueryable();



            // =================================================
            // FILTRO FECHA INICIO
            // =================================================

            if (fechaInicio.HasValue)
            {
                query =
                    query.Where(x =>
                        x.FechaInicio >= fechaInicio.Value);
            }



            // =================================================
            // FILTRO FECHA FIN
            // =================================================

            if (fechaFin.HasValue)
            {
                query =
                    query.Where(x =>
                        x.FechaFin <= fechaFin.Value);
            }



            // =================================================
            // DETALLE
            // =================================================

            return await query

                .Select(x =>
                    new DetalleReporte
                    {

                        IdSolicitud =
                            x.IdSolicitud,


                        Usuario =
                            x.Usuario!.Nombres
                            + " "
                            + x.Usuario.Apellidos,


                        Motivo =
                            x.Motivo,


                        Destino =
                            x.Destino,


                        FechaInicio =
                            x.FechaInicio,


                        FechaFin =
                            x.FechaFin,


                        MontoSolicitado =
                            x.Monto,


                        MontoRendido =
                            x.Rendicion != null
                            ? x.Rendicion.Total
                            : 0,


                        Saldo =
                            x.Rendicion != null
                            ? x.Rendicion.Saldo
                            : x.Monto,


                        Estado =
                            x.EstadoSolicitud!.Nombre

                    })

                .ToListAsync();
        }





        // =====================================================
        // DASHBOARD EJECUTIVO
        // =====================================================

        public async Task<ReporteGeneral>
            ObtenerDashboard()
        {

            // =================================================
            // OBTENER RESUMEN GENERAL
            // =================================================

            var reporte =
                await ObtenerReporteGeneral(
                    null,
                    null);



            // =================================================
            // TOTAL DE USUARIOS
            // =================================================

            reporte.TotalUsuarios =
                await _context.Usuarios
                    .CountAsync();



            // =================================================
            // VIÁTICOS POR MES
            // =================================================

            var mensual =
                await _context.Solicitudes

                .GroupBy(x =>
                    new
                    {
                        x.Fecha.Year,
                        x.Fecha.Month
                    })

                .Select(x =>
                    new
                    {
                        Mes =
                            x.Key.Month,

                        Total =
                            x.Sum(y =>
                                y.Monto)
                    })

                .OrderBy(x =>
                    x.Mes)

                .ToListAsync();



            // =================================================
            // PREPARAR DATOS DEL GRÁFICO
            // =================================================

            foreach (var item in mensual)
            {

                reporte.Meses.Add(

                    new DateTime(
                        DateTime.Now.Year,
                        item.Mes,
                        1)

                    .ToString("MMMM")
                );


                reporte.MontosMensuales.Add(
                    item.Total
                );

            }



            return reporte;
        }





        // =====================================================
        // REPORTE DE RENDICIONES
        // =====================================================

        public async Task<List<ReporteRendicion>>
            ObtenerReporteRendiciones()
        {

            return await _context.Rendiciones

                .Include(x =>
                    x.Usuario)

                .Include(x =>
                    x.Solicitud)

                .Include(x =>
                    x.Gastos)

                .Include(x =>
                    x.DevolucionSaldo)

                .Select(x =>
                    new ReporteRendicion
                    {

                        Usuario =
                            x.Usuario!.Nombres
                            + " "
                            + x.Usuario.Apellidos,


                        Solicitud =
                            "SOL-" +
                            x.IdSolicitud,


                        MontoEntregado =
                            x.Solicitud!.Monto,


                        Gastado =
                            x.Gastos.Sum(g =>
                                g.MontoTotal),


                        Devuelto =
                            x.DevolucionSaldo != null
                            ? x.DevolucionSaldo.Monto
                            : 0,


                        Diferencia =
                            x.Solicitud.Monto -
                            x.Gastos.Sum(g =>
                                g.MontoTotal),


                        Estado =
                            x.EstadoRendicion!.Nombre

                    })

                .ToListAsync();
        }





        // =====================================================
        // GASTOS POR TIPO
        // =====================================================

        public async Task<List<ReporteGasto>>
            ObtenerReporteGastos()
        {

            return await _context.Gastos

                .Include(x =>
                    x.TipoGasto)

                .GroupBy(x =>
                    x.TipoGasto!.Nombre)

                .Select(x =>
                    new ReporteGasto
                    {

                        TipoGasto =
                            x.Key,


                        Total =
                            x.Sum(g =>
                                g.MontoTotal)

                    })

                .OrderByDescending(x =>
                    x.Total)

                .ToListAsync();
        }





        // =====================================================
        // REPORTE POR USUARIO
        // =====================================================

        public async Task<List<ReporteUsuario>>
            ObtenerReporteUsuarios()
        {

            return await _context.Usuarios

                .Select(x =>
                    new ReporteUsuario
                    {

                        Usuario =
                            x.Nombres
                            + " "
                            + x.Apellidos,


                        Solicitudes =
                            _context.Solicitudes

                            .Count(s =>
                                s.IdUsuario ==
                                x.IdUsuario),


                        TotalViaticos =
                            _context.Solicitudes

                            .Where(s =>
                                s.IdUsuario ==
                                x.IdUsuario)

                            .Sum(s =>
                                s.Monto),


                        RendicionesPendientes =
                            _context.Rendiciones

                            .Count(r =>
                                r.IdUsuario ==
                                x.IdUsuario

                                &&

                                r.IdEstadoRendicion != 3)

                    })

                .ToListAsync();
        }





        // =====================================================
        // REPORTE DE REEMBOLSOS
        // =====================================================

        public async Task<List<ReporteReembolso>>
            ObtenerReporteReembolsos()
        {

            return await _context.Reembolsos

                .Include(x =>
                    x.Usuario)

                .Include(x =>
                    x.Rendicion)

                    .ThenInclude(x =>
                        x.Solicitud)

                .Include(x =>
                    x.EstadoReembolso)

                .Select(x =>
                    new ReporteReembolso
                    {

                        Usuario =
                            x.Usuario!.Nombres
                            + " "
                            + x.Usuario.Apellidos,


                        Motivo =
                            x.Rendicion!
                            .Solicitud!
                            .Motivo,


                        Monto =
                            x.Monto,


                        Estado =
                            x.EstadoReembolso!.Nombre,


                        Fecha =
                            x.FechaSolicitud

                    })

                .ToListAsync();
        }
    }
}