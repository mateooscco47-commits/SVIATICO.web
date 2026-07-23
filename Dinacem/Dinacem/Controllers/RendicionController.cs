using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Dinacem.Models;

namespace Dinacem.Controllers
{
    public class RendicionController : Controller
    {
        private readonly AplicacionDbContexto _context;

        public RendicionController(AplicacionDbContexto context)
        {
            _context = context;
        }

        // ================================
        // EMPLEADO: SOLICITUDES APROBADAS
        // ================================
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var idUsuario = HttpContext.Session.GetInt32("IdUsuario");

            if (idUsuario == null)
            {
                TempData["error"] = "La sesión ha expirado.";

                return RedirectToAction(
                    "Index",
                    "Home");
            }

            var solicitudes = await _context.Solicitudes
                .Include(s => s.EstadoSolicitud)
                .Where(s =>
                    s.IdUsuario == idUsuario.Value &&
                    s.IdEstadoSolicitud == 2)
                .OrderByDescending(s => s.Fecha)
                .ToListAsync();

            var idsSolicitudes = solicitudes
                .Select(s => s.IdSolicitud)
                .ToList();

            var rendiciones = await _context.Rendiciones
                .Include(r => r.EstadoRendicion)
                .Where(r =>
                    r.IdUsuario == idUsuario.Value &&
                    idsSolicitudes.Contains(r.IdSolicitud))
                .ToListAsync();

            ViewBag.Rendiciones = rendiciones;

            return View(solicitudes);
        }

        // ================================
        // EMPLEADO: CREAR RENDICIÓN
        // ================================
        [HttpGet]
        public async Task<IActionResult> Create(int idSolicitud)
        {
            var idUsuario = HttpContext.Session.GetInt32("IdUsuario");

            if (idUsuario == null)
            {
                TempData["error"] = "La sesión ha expirado.";

                return RedirectToAction(
                    "Index",
                    "Home");
            }

            var solicitud = await _context.Solicitudes
                .FirstOrDefaultAsync(s =>
                    s.IdSolicitud == idSolicitud &&
                    s.IdUsuario == idUsuario.Value &&
                    s.IdEstadoSolicitud == 2);

            if (solicitud == null)
            {
                TempData["error"] =
                    "La solicitud no existe, no está aprobada o no pertenece al usuario conectado.";

                return RedirectToAction(nameof(Index));
            }

            var existeRendicion = await _context.Rendiciones
                .AnyAsync(r => r.IdSolicitud == idSolicitud);

            if (existeRendicion)
            {
                TempData["error"] =
                    "Esta solicitud ya tiene una rendición registrada.";

                return RedirectToAction(nameof(Index));
            }

            var rendicion = new Rendicion
            {
                IdSolicitud = solicitud.IdSolicitud,
                IdUsuario = idUsuario.Value,
                Fecha = DateTime.Now,
                FechaInicio = solicitud.FechaInicio,
                FechaFin = solicitud.FechaFin,
                Total = 0,
                Saldo = solicitud.Monto,
                IdEstadoRendicion = 1
            };

            return View(rendicion);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Rendicion rendicion)
        {
            var idUsuario = HttpContext.Session.GetInt32("IdUsuario");

            if (idUsuario == null)
            {
                TempData["error"] = "La sesión ha expirado.";

                return RedirectToAction(
                    "Index",
                    "Home");
            }

            var solicitud = await _context.Solicitudes
                .FirstOrDefaultAsync(s =>
                    s.IdSolicitud == rendicion.IdSolicitud &&
                    s.IdUsuario == idUsuario.Value &&
                    s.IdEstadoSolicitud == 2);

            if (solicitud == null)
            {
                TempData["error"] =
                    "La solicitud no pertenece al usuario conectado.";

                return RedirectToAction(nameof(Index));
            }

            var existeRendicion = await _context.Rendiciones
                .AnyAsync(r => r.IdSolicitud == rendicion.IdSolicitud);

            if (existeRendicion)
            {
                TempData["error"] =
                    "Esta solicitud ya tiene una rendición.";

                return RedirectToAction(nameof(Index));
            }

            rendicion.IdUsuario = idUsuario.Value;
            rendicion.Fecha = DateTime.Now;
            rendicion.FechaInicio = solicitud.FechaInicio;
            rendicion.FechaFin = solicitud.FechaFin;
            rendicion.Total = 0;
            rendicion.Saldo = solicitud.Monto;
            rendicion.IdEstadoRendicion = 1;

            _context.Rendiciones.Add(rendicion);
            await _context.SaveChangesAsync();

            return RedirectToAction(
                "Index",
                "Gasto",
                new { idRendicion = rendicion.IdRendicion });
        }

        // ================================
        // EMPLEADO: MIS RENDICIONES
        // ================================
        [HttpGet]
        public async Task<IActionResult> MisRendiciones()
        {
            var idUsuario = HttpContext.Session.GetInt32("IdUsuario");

            if (idUsuario == null)
            {
                TempData["error"] = "La sesión ha expirado.";

                return RedirectToAction(
                    "Index",
                    "Home");
            }

            var lista = await _context.Rendiciones
                .Include(r => r.Solicitud)
                .Include(r => r.EstadoRendicion)
                .Where(r => r.IdUsuario == idUsuario.Value)
                .OrderByDescending(r => r.Fecha)
                .ToListAsync();

            return View(lista);
        }

        // ================================
        // ADMIN: LISTAR RENDICIONES
        // ================================
        [HttpGet]
        public async Task<IActionResult> IndexAdmin()
        {
            var rendiciones = await _context.Rendiciones
                .Include(r => r.Usuario)
                .Include(r => r.Solicitud)
                .Include(r => r.EstadoRendicion)
                .OrderByDescending(r => r.Fecha)
                .ToListAsync();

            return View(rendiciones);
        }

        // ================================
        // ADMIN/EMPLEADO: DETALLE
        // ================================
        public IActionResult Detalle(int id)
        {
            var rendicion = _context.Rendiciones
                .Include(r => r.Usuario)
                .Include(r => r.Solicitud)
                .Include(r => r.EstadoRendicion)
                .FirstOrDefault(r => r.IdRendicion == id);

            if (rendicion == null)
            {
                return NotFound();
            }

            var gastos = _context.Gastos
                .Include(g => g.TipoGasto)
                .Include(g => g.TipoComprobante)
                .Where(g => g.IdRendicion == id)
                .OrderByDescending(g => g.Fecha)
                .ToList();

            ViewBag.Rendicion = rendicion;

            return View(gastos);
        }

        // ================================
        // ADMIN: APROBAR RENDICIÓN
        // ================================
        [HttpPost]
        public IActionResult Aprobar(int id)
        {
            var rendicion = _context.Rendiciones
                .FirstOrDefault(r => r.IdRendicion == id);

            if (rendicion == null)
            {
                TempData["error"] = "Rendición no encontrada.";
                return RedirectToAction(nameof(IndexAdmin));
            }

            rendicion.IdEstadoRendicion = 3; // Aprobada

            _context.SaveChanges();

            TempData["mensaje"] = "Rendición aprobada correctamente.";
            return RedirectToAction(nameof(IndexAdmin));
        }

        // ================================
        // ADMIN: RECHAZAR RENDICIÓN
        // ================================
        [HttpPost]
        public IActionResult Rechazar(int id)
        {
            var rendicion = _context.Rendiciones
                .FirstOrDefault(r => r.IdRendicion == id);

            if (rendicion == null)
            {
                TempData["error"] = "Rendición no encontrada.";
                return RedirectToAction(nameof(IndexAdmin));
            }

            rendicion.IdEstadoRendicion = 4; // Rechazada

            _context.SaveChanges();

            TempData["mensaje"] = "Rendición rechazada correctamente.";
            return RedirectToAction(nameof(IndexAdmin));
        }
        // =====================================
        // EMPLEADO
        // VER DETALLE DE SU RENDICIÓN
        // =====================================
        public IActionResult DetalleEmpleado(int id)
        {
            var rendicion = _context.Rendiciones
                .Include(r => r.Usuario)
                .Include(r => r.Solicitud)
                .Include(r => r.EstadoRendicion)
                .FirstOrDefault(r => r.IdRendicion == id);

            if (rendicion == null)
            {
                return NotFound();
            }

            var gastos = _context.Gastos
                .Include(g => g.TipoGasto)
                .Include(g => g.TipoComprobante)
                .Where(g => g.IdRendicion == id)
                .OrderBy(g => g.Fecha)
                .ToList();

            ViewBag.Rendicion = rendicion;

            return View(gastos);
        }
        // =====================================
        // ADMINISTRADOR
        // VER DETALLE DE LA RENDICIÓN
        // =====================================
        public IActionResult DetalleAdmin(int id)
        {
            var rendicion = _context.Rendiciones
                .Include(r => r.Usuario)
                .Include(r => r.Solicitud)
                .Include(r => r.EstadoRendicion)
                .FirstOrDefault(r => r.IdRendicion == id);

            if (rendicion == null)
            {
                return NotFound();
            }

            var gastos = _context.Gastos
                .Include(g => g.TipoGasto)
                .Include(g => g.TipoComprobante)
                .Where(g => g.IdRendicion == id)
                .OrderBy(g => g.Fecha)
                .ToList();

            ViewBag.Rendicion = rendicion;

            return View(gastos);
        }
    }

}