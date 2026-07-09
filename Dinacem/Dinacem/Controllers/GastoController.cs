using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Dinacem.Models;

namespace Dinacem.Controllers
{
    public class GastoController : Controller
    {
        private readonly AplicacionDbContexto _context;

        public GastoController(AplicacionDbContexto context)
        {
            _context = context;
        }

        //=========================================
        // MOSTRAR RENDICIÓN Y GASTOS
        //=========================================

        public IActionResult Index(int idRendicion)
        {
            var rendicion = _context.Rendiciones
                .Include(r => r.Solicitud)
                .FirstOrDefault(r => r.IdRendicion == idRendicion);

            if (rendicion == null)
            {
                return RedirectToAction("Index", "Rendicion");
            }

            var gastos = _context.Gastos
                .Include(g => g.TipoGasto)
                .Include(g => g.TipoComprobante)
                .Where(g => g.IdRendicion == idRendicion)
                .OrderByDescending(g => g.Fecha)
                .ToList();

            ViewBag.Rendicion = rendicion;
            ViewBag.TiposGasto = _context.TipoGastos.ToList();
            ViewBag.TiposComprobante = _context.TipoComprobantes.ToList();

            return View(gastos);
        }

        //=========================================
        // REGISTRAR GASTO
        //=========================================

        [HttpPost]
        public IActionResult Create(Gasto gasto)
        {
            if (!ModelState.IsValid)
            {
                var errores = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .Select(x => $"{x.Key}: {string.Join(", ", x.Value.Errors.Select(e => e.ErrorMessage))}");

                TempData["error"] = string.Join("<br>", errores);

                return RedirectToAction(nameof(Index),
                    new { idRendicion = gasto.IdRendicion });
            }

            _context.Gastos.Add(gasto);
            _context.SaveChanges();

            //=================================
            // ACTUALIZAR TOTAL Y SALDO
            //=================================

            var rendicion = _context.Rendiciones
                .Include(r => r.Solicitud)
                .FirstOrDefault(r => r.IdRendicion == gasto.IdRendicion);

            if (rendicion != null)
            {
                decimal total = _context.Gastos
                    .Where(g => g.IdRendicion == gasto.IdRendicion)
                    .Sum(g => g.MontoTotal);

                rendicion.Total = total;
                rendicion.Saldo = rendicion.Solicitud.Monto - total;

                _context.Rendiciones.Update(rendicion);
                _context.SaveChanges();
            }

            TempData["mensaje"] = "Gasto registrado correctamente.";

            return RedirectToAction(nameof(Index),
                new { idRendicion = gasto.IdRendicion });
        }

        //=========================================
        // ELIMINAR GASTO
        //=========================================

        [HttpPost]
        public IActionResult Delete(int id, int idRendicion)
        {
            var gasto = _context.Gastos.Find(id);

            if (gasto != null)
            {
                _context.Gastos.Remove(gasto);
                _context.SaveChanges();
            }

            var rendicion = _context.Rendiciones
                .Include(r => r.Solicitud)
                .FirstOrDefault(r => r.IdRendicion == idRendicion);

            if (rendicion != null)
            {
                decimal total = _context.Gastos
                    .Where(g => g.IdRendicion == idRendicion)
                    .Sum(g => g.MontoTotal);

                rendicion.Total = total;
                rendicion.Saldo = rendicion.Solicitud.Monto - total;

                _context.SaveChanges();
            }

            TempData["mensaje"] = "Gasto eliminado correctamente.";

            return RedirectToAction(nameof(Index),
                new { idRendicion });
        }
        [HttpPost]
        public IActionResult EnviarRendicion(int idRendicion)
        {
            var rendicion = _context.Rendiciones
                .FirstOrDefault(r => r.IdRendicion == idRendicion);

            if (rendicion == null)
            {
                TempData["error"] = "No se encontró la rendición.";
                return RedirectToAction("Index", "Rendicion");
            }

            if (rendicion.Total <= 0)
            {
                TempData["error"] = "Debe registrar al menos un gasto antes de enviar la rendición.";
                return RedirectToAction("Index", new { idRendicion });
            }

            // 2 = Pendiente de Revisión
            rendicion.IdEstadoRendicion = 2;

            _context.SaveChanges();

            TempData["mensaje"] = "La rendición fue enviada para revisión.";

            return RedirectToAction("MisRendiciones", "Rendicion");
        }
    }
}