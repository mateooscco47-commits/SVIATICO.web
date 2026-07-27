using Dinacem.Models.Reporte;
using Microsoft.EntityFrameworkCore;
using Dinacem.Models;

namespace Dinacem.Services
{

    public class ReporteService
    {
        private readonly AplicacionDbContexto _context;


        public ReporteService(AplicacionDbContexto context)
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



            if (fechaInicio.HasValue)
            {
                query = query.Where(x =>
                    x.FechaInicio >= fechaInicio.Value);
            }



            if (fechaFin.HasValue)
            {
                query = query.Where(x =>
                    x.FechaFin <= fechaFin.Value);
            }



            var solicitudes = await query.ToListAsync();



            var reporte = new ReporteGeneral();



            reporte.TotalSolicitudes =
                solicitudes.Count;



            reporte.SolicitudesPendientes =
                solicitudes.Count(x =>
                    x.EstadoSolicitud!.Nombre == "Pendiente");



            reporte.SolicitudesAprobadas =
                solicitudes.Count(x =>
                    x.EstadoSolicitud!.Nombre == "Aprobado");



            reporte.SolicitudesRechazadas =
                solicitudes.Count(x =>
                    x.EstadoSolicitud!.Nombre == "Rechazado");



            reporte.SolicitudesFinalizadas =
                solicitudes.Count(x =>
                    x.EstadoSolicitud!.Nombre == "Finalizado");



            reporte.TotalSolicitado =
                solicitudes.Sum(x => x.Monto);



            reporte.TotalRendido =
                solicitudes
                .Where(x => x.Rendicion != null)
                .Sum(x => x.Rendicion!.Total);



            reporte.TotalGastado =
                solicitudes
                .Where(x => x.Rendicion != null)
                .SelectMany(x => x.Rendicion!.Gastos)
                .Sum(x => x.MontoTotal);



            reporte.SaldoPendiente =
                reporte.TotalSolicitado -
                reporte.TotalRendido;



            reporte.RendicionesPendientes =
                solicitudes.Count(x =>
                    x.Rendicion == null);



            reporte.FechaInicio = fechaInicio;
            reporte.FechaFin = fechaFin;



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


            var query = _context.Solicitudes

                .Include(x => x.Usuario)

                .Include(x => x.EstadoSolicitud)

                .Include(x => x.Rendicion)

                .AsQueryable();



            if (fechaInicio.HasValue)
            {
                query = query.Where(x =>
                    x.FechaInicio >= fechaInicio);
            }



            if (fechaFin.HasValue)
            {
                query = query.Where(x =>
                    x.FechaFin <= fechaFin);
            }



            return await query
                .Select(x => new DetalleReporte
                {

                    IdSolicitud = x.IdSolicitud,


                    Usuario =
                        x.Usuario!.Nombres
                        + " "
                        + x.Usuario.Apellidos,


                    Motivo = x.Motivo,


                    Destino = x.Destino,


                    FechaInicio = x.FechaInicio,


                    FechaFin = x.FechaFin,


                    MontoSolicitado = x.Monto,


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


            var reporte =
                await ObtenerReporteGeneral(null, null);



            var mensual =
                await _context.Solicitudes

                .GroupBy(x => new
                {
                    x.Fecha.Year,
                    x.Fecha.Month
                })

                .Select(x => new
                {
                    Mes = x.Key.Month,
                    Total = x.Sum(y => y.Monto)
                })

                .OrderBy(x => x.Mes)

                .ToListAsync();



            foreach (var item in mensual)
            {

                reporte.Meses.Add(
                    new DateTime(
                        DateTime.Now.Year,
                        item.Mes,
                        1)
                    .ToString("MMMM")
                );


                reporte.MontosMensuales
                    .Add(item.Total);

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

                .Include(x => x.Usuario)

                .Include(x => x.Solicitud)

                .Include(x => x.Gastos)

                .Include(x => x.DevolucionSaldo)

                .Select(x => new ReporteRendicion
                {

                    Usuario =
                    x.Usuario!.Nombres
                    + " "
                    + x.Usuario.Apellidos,


                    Solicitud =
                    "SOL-" + x.IdSolicitud,


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

                .Include(x => x.TipoGasto)

                .GroupBy(x => x.TipoGasto!.Nombre)

                .Select(x => new ReporteGasto
                {

                    TipoGasto = x.Key,


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

                .Select(x => new ReporteUsuario
                {

                    Usuario =
                    x.Nombres
                    + " "
                    + x.Apellidos,


                    Solicitudes =
                    _context.Solicitudes
                    .Count(s =>
                    s.IdUsuario == x.IdUsuario),


                    TotalViaticos =
                    _context.Solicitudes
                    .Where(s =>
                    s.IdUsuario == x.IdUsuario)
                    .Sum(s =>
                    s.Monto),


                    RendicionesPendientes =
                    _context.Rendiciones
                    .Count(r =>
                    r.IdUsuario == x.IdUsuario
                    &&
                    r.IdEstadoRendicion != 3)


                })

                .ToListAsync();

        }





        // =====================================================
        // SOLICITUDES PENDIENTES
        // =====================================================

        public async Task<List<ReportePendiente>>
            ObtenerSolicitudesPendientes()
        {


            return await _context.Solicitudes

                .Include(x => x.Usuario)

                .Include(x => x.EstadoSolicitud)

                .Where(x =>
                x.EstadoSolicitud!.Nombre == "Pendiente")

                .Select(x => new ReportePendiente
                {

                    IdSolicitud =
                    x.IdSolicitud,


                    Codigo =
                    "SOL-" + x.IdSolicitud,


                    Solicitante =
                    x.Usuario!.Nombres
                    + " "
                    + x.Usuario.Apellidos,


                    Destino =
                    x.Destino,


                    Monto =
                    x.Monto,


                    Fecha =
                    x.Fecha

                })

                .ToListAsync();

        }





        // =====================================================
        // REPORTE REEMBOLSOS
        // =====================================================

        public async Task<List<ReporteReembolso>>
            ObtenerReporteReembolsos()
        {


            return await _context.Reembolsos

                .Include(x => x.Usuario)

                .Include(x => x.Rendicion)

                    .ThenInclude(x =>
                    x.Solicitud)

                .Include(x =>
                x.EstadoReembolso)

                .Select(x => new ReporteReembolso
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