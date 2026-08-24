using Dinacem.Models;
using Dinacem.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Dinacem.Controllers
{
    public class PrincipalController : Controller
    {
        private readonly ReporteService _reporteService;


        public PrincipalController(
            ReporteService reporteService)
        {
            _reporteService = reporteService;
        }


        // =====================================================
        // INICIO / DASHBOARD EJECUTIVO
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            // ================================================
            // VERIFICAR ROL
            // ================================================

            var idRol =
                HttpContext.Session.GetInt32("IdRol");


            if (idRol != 1)
            {
                TempData["error"] =
                    "No tiene permiso para ingresar al panel administrativo.";

                return RedirectToAction(
                    "Index",
                    "Home");
            }


            // ================================================
            // OBTENER DASHBOARD
            // ================================================

            var reporte =
                await _reporteService
                    .ObtenerDashboard();


            return View(reporte);
        }
    }
}