using Dinacem.Models.Servicios;
using Dinacem.Services;
using Microsoft.AspNetCore.Mvc;

namespace Dinacen.Controllers
{
    public class ReporteController : Controller
    {
        private readonly ReporteService _reporteService;
        private readonly ReportePdfService _reportePdfService;


        public ReporteController(
            ReporteService reporteService,
            ReportePdfService reportePdfService)
        {
            _reporteService = reporteService;
            _reportePdfService = reportePdfService;
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
        // PDF REPORTE GENERAL
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> ExportarGeneralPdf(
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


            var pdf =
                _reportePdfService
                    .GenerarReporteGeneral(reporte);


            return File(
                pdf,
                "application/pdf",
                $"Reporte-General-Viaticos-{DateTime.Now:yyyyMMddHHmmss}.pdf");
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
        // PDF RENDICIONES
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> ExportarRendicionesPdf()
        {
            var reporte =
                await _reporteService
                    .ObtenerReporteRendiciones();


            var pdf =
                _reportePdfService
                    .GenerarReporteRendiciones(reporte);


            return File(
                pdf,
                "application/pdf",
                $"Reporte-Rendiciones-{DateTime.Now:yyyyMMddHHmmss}.pdf");
        }


        // =====================================================
        // REPORTE DE GASTOS
        // =====================================================

        public async Task<IActionResult> Gastos()
        {
            var reporte =
                await _reporteService
                    .ObtenerReporteGastos();


            return View(reporte);
        }


        // =====================================================
        // PDF GASTOS
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> ExportarGastosPdf()
        {
            var reporte =
                await _reporteService
                    .ObtenerReporteGastos();


            var pdf =
                _reportePdfService
                    .GenerarReporteGastos(reporte);


            return File(
                pdf,
                "application/pdf",
                $"Reporte-Gastos-{DateTime.Now:yyyyMMddHHmmss}.pdf");
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
        // PDF USUARIOS
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> ExportarUsuariosPdf()
        {
            var reporte =
                await _reporteService
                    .ObtenerReporteUsuarios();


            var pdf =
                _reportePdfService
                    .GenerarReporteUsuarios(reporte);


            return File(
                pdf,
                "application/pdf",
                $"Reporte-Usuarios-{DateTime.Now:yyyyMMddHHmmss}.pdf");
        }


        // =====================================================
        // REPORTE DE REEMBOLSOS
        // =====================================================

        public async Task<IActionResult> Reembolsos()
        {
            var reporte =
                await _reporteService
                    .ObtenerReporteReembolsos();


            return View(reporte);
        }


        // =====================================================
        // PDF REEMBOLSOS
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> ExportarReembolsosPdf()
        {
            var reporte =
                await _reporteService
                    .ObtenerReporteReembolsos();


            var pdf =
                _reportePdfService
                    .GenerarReporteReembolsos(reporte);


            return File(
                pdf,
                "application/pdf",
                $"Reporte-Reembolsos-{DateTime.Now:yyyyMMddHHmmss}.pdf");
        }
    }
}