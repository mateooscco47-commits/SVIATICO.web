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
        public IActionResult Index()
        {
            int idUsuario = 1; // Temporal

            var solicitudes = _context.Solicitudes
                .Include(s => s.EstadoSolicitud)
                .Where(s => s.IdUsuario == idUsuario && s.IdEstadoSolicitud == 2)
                .OrderByDescending(s => s.Fecha)
                .ToList();

            var rendiciones = _context.Rendiciones
                .Where(r => r.IdUsuario == idUsuario)
                .ToList();

            ViewBag.Rendiciones = rendiciones;

            return View(solicitudes);
        }

        // ================================
        // EMPLEADO: CREAR RENDICIÓN
        // ================================
        [HttpGet]
        public IActionResult Create(int idSolicitud)
        {
            bool yaExisteRendicion = _context.Rendiciones
                .Any(r => r.IdSolicitud == idSolicitud);

            if (yaExisteRendicion)
            {
                TempData["error"] = "Esta solicitud ya tiene una rendición registrada.";
                return RedirectToAction(nameof(Index));
            }

            var solicitud = _context.Solicitudes
                .FirstOrDefault(s => s.IdSolicitud == idSolicitud);

            if (solicitud == null)
            {
                return RedirectToAction(nameof(Index));
            }

            var rendicion = new Rendicion
            {
                IdSolicitud = solicitud.IdSolicitud,
                IdUsuario = solicitud.IdUsuario,
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
        public IActionResult Create(Rendicion rendicion)
        {
            bool yaExisteRendicion = _context.Rendiciones
                .Any(r => r.IdSolicitud == rendicion.IdSolicitud);

            if (yaExisteRendicion)
            {
                TempData["error"] = "Esta solicitud ya tiene una rendición registrada.";
                return RedirectToAction(nameof(Index));
            }

            if (!ModelState.IsValid)
            {
                return View(rendicion);
            }

            _context.Rendiciones.Add(rendicion);
            _context.SaveChanges();

            TempData["mensaje"] = "Rendición creada correctamente.";

            return RedirectToAction("Index", "Gasto",
                new { idRendicion = rendicion.IdRendicion });
        }

        // ================================
        // EMPLEADO: MIS RENDICIONES
        // ================================
        public IActionResult MisRendiciones()
        {
            int idUsuario = 1; // Temporal

            var lista = _context.Rendiciones
                .Include(r => r.Solicitud)
                .Include(r => r.EstadoRendicion)
                .Where(r => r.IdUsuario == idUsuario)
                .OrderByDescending(r => r.Fecha)
                .ToList();

            return View(lista);
        }

        // ================================
        // ADMIN: LISTAR RENDICIONES
        // ================================
        public IActionResult IndexAdmin()
        {
            var lista = _context.Rendiciones
                .Include(r => r.Usuario)
                .Include(r => r.Solicitud)
                .Include(r => r.EstadoRendicion)
                .Where(r => r.IdEstadoRendicion == 2)
                .OrderByDescending(r => r.Fecha)
                .ToList();

            return View(lista);
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