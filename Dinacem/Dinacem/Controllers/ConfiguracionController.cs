using Dinacem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Dinacem.Controllers
{
    public class ConfiguracionController : Controller
    {
        private readonly AplicacionDbContexto _context;

        public ConfiguracionController(
            AplicacionDbContexto context)
        {
            _context = context;
        }

        // =========================================
        // ADMINISTRADOR: VER CONFIGURACIÓN
        // =========================================
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var idRol =
                HttpContext.Session.GetInt32("IdRol");

            if (idRol != 1)
            {
                return RedirectToAction(
                    "Index",
                    "Home");
            }

            var configuracion =
                await _context.ConfiguracionesSistema
                    .FirstOrDefaultAsync();

            if (configuracion == null)
            {
                configuracion =
                    new ConfiguracionSistema
                    {
                        TarifaKilometro = 0.40m
                    };

                _context.ConfiguracionesSistema.Add(
                    configuracion);

                await _context.SaveChangesAsync();
            }

            return View(configuracion);
        }


        // =========================================
        // ADMINISTRADOR: ACTUALIZAR TARIFA
        // =========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Guardar(
            decimal tarifaKilometro)
        {
            var idRol =
                HttpContext.Session.GetInt32("IdRol");

            if (idRol != 1)
            {
                return RedirectToAction(
                    "Index",
                    "Home");
            }

            if (tarifaKilometro <= 0)
            {
                TempData["error"] =
                    "La tarifa por kilómetro debe ser mayor que cero.";

                return RedirectToAction(
                    nameof(Index));
            }

            var configuracion =
                await _context.ConfiguracionesSistema
                    .FirstOrDefaultAsync();

            if (configuracion == null)
            {
                configuracion =
                    new ConfiguracionSistema();

                _context.ConfiguracionesSistema.Add(
                    configuracion);
            }

            configuracion.TarifaKilometro =
                Math.Round(
                    tarifaKilometro,
                    2,
                    MidpointRounding.AwayFromZero);

            await _context.SaveChangesAsync();

            TempData["mensaje"] =
                "Tarifa por kilómetro actualizada correctamente.";

            return RedirectToAction(
                nameof(Index));
        }
    }
}