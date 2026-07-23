using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Dinacem.Models;
using Dinacem.Models.Servicios;

namespace Dinacem.Controllers
{
    public class SolicitudController : Controller
    {
        private readonly AplicacionDbContexto _context;
        private readonly CorreoService _correoService;

        public SolicitudController(
    AplicacionDbContexto context,
    CorreoService correoService)
        {
            _context = context;
            _correoService = correoService;
        }

        // ==========================================
        // EMPLEADO
        // ==========================================

        // Mostrar formulario
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var idUsuario = HttpContext.Session.GetInt32("IdUsuario");

            if (idUsuario == null)
            {
                TempData["error"] =
                    "La sesión ha expirado. Inicie sesión nuevamente.";

                return RedirectToAction(
                    "Index",
                    "Home");
            }

            bool tieneSolicitudEnProceso =
                await _context.Solicitudes.AnyAsync(s =>
                    s.IdUsuario == idUsuario.Value &&
                    s.IdEstadoSolicitud == 1);

            if (tieneSolicitudEnProceso)
            {
                TempData["error"] =
                    "No puede registrar una nueva solicitud porque tiene una solicitud pendiente de aprobación.";

                return RedirectToAction(nameof(MisSolicitudes));
            }

            return View();
        }

        // Guardar solicitud
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Solicitud solicitud)
        {
            var idUsuario = HttpContext.Session.GetInt32("IdUsuario");

            if (idUsuario == null)
            {
                TempData["error"] =
                    "Su sesión ha expirado. Inicie sesión nuevamente.";

                return RedirectToAction(
                    "Index",
                    "Home");
            }

            bool tieneSolicitudPendiente =
                await _context.Solicitudes.AnyAsync(s =>
                    s.IdUsuario == idUsuario.Value &&
                    s.IdEstadoSolicitud == 1);

            if (tieneSolicitudPendiente)
            {
                TempData["error"] =
                    "No puede registrar una nueva solicitud porque ya tiene una solicitud pendiente.";

                return RedirectToAction(nameof(MisSolicitudes));
            }

            if (solicitud.FechaInicio.Date >
                solicitud.FechaFin.Date)
            {
                ModelState.AddModelError(
                    nameof(solicitud.FechaFin),
                    "La fecha final no puede ser anterior a la fecha inicial.");
            }

            if (solicitud.Monto <= 0)
            {
                ModelState.AddModelError(
                    nameof(solicitud.Monto),
                    "El monto debe ser mayor que cero.");
            }

            if (!ModelState.IsValid)
            {
                return View(solicitud);
            }

            solicitud.IdUsuario = idUsuario.Value;
            solicitud.Fecha = DateTime.Now;
            solicitud.IdEstadoSolicitud = 1;
            solicitud.Observaciones = string.Empty;

            _context.Solicitudes.Add(solicitud);

            await _context.SaveChangesAsync();

            var empleado = await _context.Usuarios
                .FirstOrDefaultAsync(u =>
                    u.IdUsuario == solicitud.IdUsuario);

            var correosAdministradores =
                await _context.Usuarios
                    .Where(u =>
                        u.IdRol == 1 &&
                        u.Estado &&
                        !string.IsNullOrWhiteSpace(u.Correo))
                    .Select(u => u.Correo)
                    .ToListAsync();

            var nombreEmpleado = empleado == null
                ? $"Usuario {solicitud.IdUsuario}"
                : $"{empleado.Nombres} {empleado.Apellidos}";

            string asunto =
                $"Nueva solicitud de viáticos #{solicitud.IdSolicitud}";

            string contenidoHtml = $"""
        <div style="font-family:Arial,sans-serif;max-width:650px">
            <h2 style="color:#0d6efd">
                Nueva solicitud de viáticos
            </h2>

            <p>
                El empleado <strong>{nombreEmpleado}</strong>
                ha registrado una nueva solicitud.
            </p>

            <table style="border-collapse:collapse;width:100%">
                <tr>
                    <td style="border:1px solid #ddd;padding:8px">
                        <strong>Número de solicitud</strong>
                    </td>
                    <td style="border:1px solid #ddd;padding:8px">
                        {solicitud.IdSolicitud}
                    </td>
                </tr>

                <tr>
                    <td style="border:1px solid #ddd;padding:8px">
                        <strong>Empleado</strong>
                    </td>
                    <td style="border:1px solid #ddd;padding:8px">
                        {nombreEmpleado}
                    </td>
                </tr>

                <tr>
                    <td style="border:1px solid #ddd;padding:8px">
                        <strong>Destino</strong>
                    </td>
                    <td style="border:1px solid #ddd;padding:8px">
                        {solicitud.Destino}
                    </td>
                </tr>

                <tr>
                    <td style="border:1px solid #ddd;padding:8px">
                        <strong>Motivo</strong>
                    </td>
                    <td style="border:1px solid #ddd;padding:8px">
                        {solicitud.Motivo}
                    </td>
                </tr>

                <tr>
                    <td style="border:1px solid #ddd;padding:8px">
                        <strong>Fecha de inicio</strong>
                    </td>
                    <td style="border:1px solid #ddd;padding:8px">
                        {solicitud.FechaInicio:dd/MM/yyyy}
                    </td>
                </tr>

                <tr>
                    <td style="border:1px solid #ddd;padding:8px">
                        <strong>Fecha de fin</strong>
                    </td>
                    <td style="border:1px solid #ddd;padding:8px">
                        {solicitud.FechaFin:dd/MM/yyyy}
                    </td>
                </tr>

                <tr>
                    <td style="border:1px solid #ddd;padding:8px">
                        <strong>Monto solicitado</strong>
                    </td>
                    <td style="border:1px solid #ddd;padding:8px">
                        S/ {solicitud.Monto:N2}
                    </td>
                </tr>

                <tr>
                    <td style="border:1px solid #ddd;padding:8px">
                        <strong>Estado</strong>
                    </td>
                    <td style="border:1px solid #ddd;padding:8px">
                        Pendiente
                    </td>
                </tr>
            </table>

            <p style="margin-top:20px">
                Ingrese al sistema DINACEM para revisar,
                aprobar o rechazar la solicitud.
            </p>
        </div>
        """;

            bool correoEnviado = await _correoService.EnviarAsync(
                correosAdministradores,
                asunto,
                contenidoHtml);

            TempData["mensaje"] =
                correoEnviado
                    ? "Solicitud registrada y notificación enviada a los administradores."
                    : "Solicitud registrada correctamente, pero no fue posible enviar la notificación por correo.";

            return RedirectToAction(nameof(MisSolicitudes));
        }

        // Mis solicitudes
        [HttpGet]
        public IActionResult MisSolicitudes()
        {
            var idUsuario = HttpContext.Session.GetInt32("IdUsuario");

            if (idUsuario == null)
            {
                TempData["error"] = "La sesión ha expirado.";

                return RedirectToAction(
                    "Index",
                    "Home");
            }

            var lista = _context.Solicitudes
                .Include(x => x.EstadoSolicitud)
                .Where(x => x.IdUsuario == idUsuario.Value)
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
        [ValidateAntiForgeryToken]
        public IActionResult Rechazar(int id, string observaciones)
        {
            var solicitud = _context.Solicitudes
                .FirstOrDefault(x => x.IdSolicitud == id);

            if (solicitud == null)
            {
                TempData["error"] = "Solicitud no encontrada.";
                return RedirectToAction(nameof(Index));
            }

            if (string.IsNullOrWhiteSpace(observaciones))
            {
                TempData["error"] =
                    "Debe ingresar las observaciones del rechazo.";

                return RedirectToAction(nameof(Index));
            }

            // Estado 3 = Rechazada
            solicitud.IdEstadoSolicitud = 3;
            solicitud.Observaciones = observaciones.Trim();

            _context.SaveChanges();

            TempData["mensaje"] =
                "La solicitud fue rechazada correctamente.";

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