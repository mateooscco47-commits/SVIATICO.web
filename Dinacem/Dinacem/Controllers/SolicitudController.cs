using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Dinacem.Models;

namespace Dinacem.Controllers
{
    public class SolicitudController : Controller
    {
        private readonly AplicacionDbContexto _context;

        public SolicitudController(AplicacionDbContexto context)
        {
            _context = context;
        }

        // ==========================================
        // EMPLEADO
        // ==========================================

        // Mostrar formulario
        [HttpGet]
        public IActionResult Create()
        {
            int idUsuario = 1; // Temporal

            bool tieneSolicitudEnProceso = _context.Solicitudes
                .Any(s => s.IdUsuario == idUsuario && s.IdEstadoSolicitud == 1);

            if (tieneSolicitudEnProceso)
            {
                TempData["error"] = "No puede registrar una nueva solicitud porque tiene una solicitud pendiente de aprobación.";
                return RedirectToAction(nameof(MisSolicitudes));
            }

            return View();
        }

        // Guardar solicitud
        [HttpPost]
        public IActionResult Create(Solicitud solicitud)
        {
            int idUsuario = 1; // Temporal

            bool tieneSolicitudEnProceso = _context.Solicitudes
                .Any(s => s.IdUsuario == idUsuario && s.IdEstadoSolicitud == 1);

            if (tieneSolicitudEnProceso)
            {
                TempData["error"] = "No puede registrar una nueva solicitud porque tiene una solicitud pendiente de aprobación.";
                return RedirectToAction(nameof(MisSolicitudes));
            }

            if (!ModelState.IsValid)
            {
                return View(solicitud);
            }

            solicitud.IdUsuario = idUsuario;
            solicitud.Fecha = DateTime.Now;
            solicitud.IdEstadoSolicitud = 1;
            solicitud.Observaciones = "";

            _context.Solicitudes.Add(solicitud);
            _context.SaveChanges();

            TempData["mensaje"] = "Solicitud registrada correctamente.";
            return RedirectToAction(nameof(MisSolicitudes));
        }

        // Mis solicitudes
        public IActionResult MisSolicitudes()
        {
            // Temporal
            int idUsuario = 1;

            var lista = _context.Solicitudes
                .Include(x => x.EstadoSolicitud)
                .Where(x => x.IdUsuario == idUsuario)
                .OrderByDescending(x => x.Fecha)
                .ToList();

            return View(lista);
        }

        // ==========================================
        // ADMINISTRADOR
        // ==========================================

        // Todas las solicitudes
        public IActionResult Index()
        {
            var lista = _context.Solicitudes
                .Include(x => x.Usuario)
                .Include(x => x.EstadoSolicitud)
                .OrderByDescending(x => x.Fecha)
                .ToList();

            return View(lista);
        }

        // Aprobar solicitud
        [HttpPost]
        public IActionResult Aprobar(int id)
        {
            var solicitud = _context.Solicitudes
                .FirstOrDefault(x => x.IdSolicitud == id);

            if (solicitud == null)
            {
                TempData["error"] = "Solicitud no encontrada.";
                return RedirectToAction(nameof(Index));
            }

            // 2 = Aprobado
            solicitud.IdEstadoSolicitud = 2;
            solicitud.Observaciones = "Solicitud aprobada.";

            _context.SaveChanges();

            TempData["mensaje"] = "Solicitud aprobada correctamente.";

            return RedirectToAction(nameof(Index));
        }

        // Rechazar solicitud
        [HttpPost]
        public IActionResult Rechazar(int id, string observaciones)
        {
            var solicitud = _context.Solicitudes
                .FirstOrDefault(x => x.IdSolicitud == id);

            if (solicitud == null)
            {
                TempData["error"] = "Solicitud no encontrada.";
                return RedirectToAction(nameof(Index));
            }

            // 3 = Rechazado
            solicitud.IdEstadoSolicitud = 3;
            solicitud.Observaciones = observaciones;

            _context.SaveChanges();

            TempData["mensaje"] = "Solicitud rechazada correctamente.";

            return RedirectToAction(nameof(Index));
        }

        // Ver detalle
        public IActionResult Details(int id)
        {
            var solicitud = _context.Solicitudes
                .Include(x => x.Usuario)
                .Include(x => x.EstadoSolicitud)
                .FirstOrDefault(x => x.IdSolicitud == id);

            if (solicitud == null)
            {
                return NotFound();
            }

            return View(solicitud);
        }
    }
}