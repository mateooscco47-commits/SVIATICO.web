using Dinacem.Models.Servicios;
using Dinacem.Services;
using Microsoft.AspNetCore.Mvc;

namespace Dinacen.Controllers
{
    public class ReporteController : Controller
    {

        private readonly ReporteService _reporteService;


        public ReporteController(
            ReporteService reporteService)
        {
            _reporteService = reporteService;
        }



        // =====================================================
        // DASHBOARD EJECUTIVO
        // =====================================================

        public async Task<IActionResult> Dashboard()
        {

            var reporte =
                await _reporteService
                .ObtenerDashboard();


            return View(reporte);

        }





        // =====================================================
        // REPORTE GENERAL
        // =====================================================

        public async Task<IActionResult> Index(
            DateTime? fechaInicio,
            DateTime? fechaFin)
        {


            var reporte =
                await _reporteService
                .ObtenerReporteGeneral(
                    fechaInicio,
                    fechaFin);



            reporte.Detalles =
                await _reporteService
                .ObtenerDetalleReporte(
                    fechaInicio,
                    fechaFin);



            return View(reporte);

        }





        // =====================================================
        // REPORTE DE RENDICIONES
        // =====================================================

        public async Task<IActionResult> Rendiciones()
        {

            var reporte =
                await _reporteService
                .ObtenerReporteRendiciones();


            return View(reporte);

        }





        // =====================================================
        // REPORTE DE GASTOS POR TIPO
        // =====================================================

        public async Task<IActionResult> Gastos()
        {

            var reporte =
                await _reporteService
                .ObtenerReporteGastos();


            return View(reporte);

        }





        // =====================================================
        // REPORTE POR USUARIO
        // =====================================================

        public async Task<IActionResult> Usuarios()
        {

            var reporte =
                await _reporteService
                .ObtenerReporteUsuarios();


            return View(reporte);

        }





        // =====================================================
        // SOLICITUDES PENDIENTES
        // =====================================================

        public async Task<IActionResult> Pendientes()
        {

            var reporte =
                await _reporteService
                .ObtenerSolicitudesPendientes();


            return View(reporte);

        }





        // =====================================================
        // REEMBOLSOS
        // =====================================================

        public async Task<IActionResult> Reembolsos()
        {

            var reporte =
                await _reporteService
                .ObtenerReporteReembolsos();


            return View(reporte);

        }

    }
}