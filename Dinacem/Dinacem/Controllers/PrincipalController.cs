using Dinacem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Dinacem.Controllers
{
    public class PrincipalController : Controller
    {
        private readonly AplicacionDbContexto _context;

        public PrincipalController(
            AplicacionDbContexto context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
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

            ViewBag.TotalUsuarios =
                await _context.Usuarios.CountAsync();

            ViewBag.SolicitudesPendientes =
                await _context.Solicitudes.CountAsync(s =>
                    s.IdEstadoSolicitud == 1);

            ViewBag.RendicionesPendientes =
                await _context.Rendiciones.CountAsync(r =>
                    r.IdEstadoRendicion == 2);

            ViewBag.SolicitudesAprobadas =
                await _context.Solicitudes.CountAsync(s =>
                    s.IdEstadoSolicitud == 2);

            return View();
        }
    }
}