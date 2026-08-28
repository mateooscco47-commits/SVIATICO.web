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

        // =========================================================
        // VALIDAR BLOQUEOS (SOLICITUD ACTIVA O RENDICIÓN PENDIENTE)
        // =========================================================
        private async Task<(bool tieneBloqueo, string mensajeError)> ObtenerMotivoBloqueoAsync(int idUsuario)
        {
            // 1. Validar si tiene una RENDICIÓN pendiente de revisión (Ej: IdEstadoRendicion == 1)
            bool tieneRendicionPendiente = await _context.Rendiciones.AnyAsync(r =>
                r.IdUsuario == idUsuario && r.IdEstadoRendicion == 1
            );

            if (tieneRendicionPendiente)
            {
                return (true, "No puede registrar una nueva solicitud porque tiene una rendición pendiente de revisión.");
            }

            // 2. Validar si tiene una SOLICITUD pendiente de revisión o aprobada sin rendición
            var solicitudBloqueante = await _context.Solicitudes.FirstOrDefaultAsync(s =>
                s.IdUsuario == idUsuario &&
                (
                    s.IdEstadoSolicitud == 1 ||
                    (
                        s.IdEstadoSolicitud == 2 &&
                        !_context.Rendiciones.Any(r => r.IdSolicitud == s.IdSolicitud)
                    )
                )
            );

            if (solicitudBloqueante != null)
            {
                string mensaje = solicitudBloqueante.IdEstadoSolicitud == 1
                    ? "No puede registrar una nueva solicitud porque tiene una solicitud pendiente de revisión."
                    : "No puede registrar una nueva solicitud porque tiene una solicitud aprobada activa pendiente de rendición.";

                return (true, mensaje);
            }

            return (false, string.Empty);
        }

        // =========================================================
        // CREAR SOLICITUD - GET
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var idUsuario = HttpContext.Session.GetInt32("IdUsuario");

            if (idUsuario == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var (tieneBloqueo, mensajeError) = await ObtenerMotivoBloqueoAsync(idUsuario.Value);

            if (tieneBloqueo)
            {
                TempData["errorSolicitud"] = mensajeError;
                return RedirectToAction(nameof(MisSolicitudes));
            }

            return View();
        }

        // =========================================================
        // CREAR SOLICITUD - POST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Solicitud solicitud)
        {
            var idUsuario = HttpContext.Session.GetInt32("IdUsuario");

            if (idUsuario == null)
            {
                return RedirectToAction("Index", "Home");
            }

            // -----------------------------------------------------
            // VALIDAR BLOQUEOS (DOBLE VALIDACIÓN EN POST)
            // -----------------------------------------------------

            var (tieneBloqueo, mensajeError) = await ObtenerMotivoBloqueoAsync(idUsuario.Value);

            if (tieneBloqueo)
            {
                TempData["errorSolicitud"] = mensajeError;
                return RedirectToAction(nameof(MisSolicitudes));
            }

            // -----------------------------------------------------
            // VALIDAR FECHAS
            // -----------------------------------------------------

            if (solicitud.FechaInicio.Date > solicitud.FechaFin.Date)
            {
                ModelState.AddModelError(
                    nameof(solicitud.FechaFin),
                    "La fecha final no puede ser anterior a la fecha inicial.");
            }

            // -----------------------------------------------------
            // VALIDAR MONTO
            // -----------------------------------------------------

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

            // -----------------------------------------------------
            // CONFIGURAR SOLICITUD
            // -----------------------------------------------------

            solicitud.IdUsuario = idUsuario.Value;
            solicitud.Fecha = DateTime.Now;
            solicitud.IdEstadoSolicitud = 1; // 1 = Pendiente de revisión
            solicitud.Observaciones = string.Empty;

            _context.Solicitudes.Add(solicitud);

            await _context.SaveChangesAsync();

            // =====================================================
            // OBTENER DATOS DEL REPRESENTANTE
            // =====================================================

            var empleado = await _context.Usuarios
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.IdUsuario == solicitud.IdUsuario);

            var nombreEmpleado = empleado == null
                ? $"Usuario {solicitud.IdUsuario}"
                : $"{empleado.Nombres} {empleado.Apellidos}";

            // =====================================================
            // OBTENER ADMINISTRADORES
            // =====================================================

            var correosAdministradores =
                await _context.Usuarios
                    .AsNoTracking()
                    .Where(u =>
                        u.IdRol == 1 &&
                        u.Estado &&
                        !string.IsNullOrWhiteSpace(u.Correo))
                    .Select(u => u.Correo!)
                    .ToListAsync();

            // =====================================================
            // PREPARAR CORREO
            // =====================================================

            string asunto = $"Nueva solicitud de viáticos #{solicitud.IdSolicitud}";

            string urlSistema = $"https://TU-DOMINIO.com/Solicitud/Details/{solicitud.IdSolicitud}";

            string contenidoHtml = GenerarCorreoSolicitud(
                solicitud,
                nombreEmpleado,
                urlSistema);

            bool correoEnviado = false;

            if (correosAdministradores.Any())
            {
                correoEnviado = await _correoService.EnviarAsync(
                    correosAdministradores,
                    asunto,
                    contenidoHtml);
            }

            // =====================================================
            // NOTIFICACIÓN PROPIA DEL MÓDULO SOLICITUD
            // =====================================================

            TempData["mensajeSolicitud"] =
                correoEnviado
                    ? "Solicitud registrada correctamente y notificación enviada a los administradores."
                    : "Solicitud registrada correctamente, pero no fue posible enviar la notificación por correo.";

            return RedirectToAction(nameof(MisSolicitudes));
        }

        // =========================================================
        // MIS SOLICITUDES
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> MisSolicitudes()
        {
            var idUsuario = HttpContext.Session.GetInt32("IdUsuario");

            if (idUsuario == null)
            {
                TempData["errorSolicitud"] =
                    "La sesión ha expirado. Inicie sesión nuevamente.";

                return RedirectToAction("Index", "Home");
            }

            var lista = await _context.Solicitudes
                .AsNoTracking()
                .Include(x => x.EstadoSolicitud)
                .Where(x => x.IdUsuario == idUsuario.Value)
                .OrderByDescending(x => x.Fecha)
                .ToListAsync();

            return View(lista);
        }

        // =========================================================
        // ADMINISTRADOR - TODAS LAS SOLICITUDES
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var lista = await _context.Solicitudes
                .AsNoTracking()
                .Include(x => x.Usuario)
                .Include(x => x.EstadoSolicitud)
                .OrderByDescending(x => x.Fecha)
                .ToListAsync();

            return View(lista);
        }

        // =========================================================
        // APROBAR SOLICITUD
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Aprobar(
    int id,
    IFormFile comprobante,
    string observacionAprobacion)
        {
            var solicitud = await _context.Solicitudes
                .FirstOrDefaultAsync(x => x.IdSolicitud == id);

            if (solicitud == null)
            {
                TempData["errorSolicitud"] = "Solicitud no encontrada.";
                return RedirectToAction(nameof(Index));
            }

            // =========================================================
            // VALIDAR COMPROBANTE
            // =========================================================

            if (comprobante == null || comprobante.Length == 0)
            {
                TempData["errorSolicitud"] =
                    "Debe adjuntar el comprobante de depósito.";

                return RedirectToAction(nameof(Index));
            }

            // Máximo 5 MB
            if (comprobante.Length > 5 * 1024 * 1024)
            {
                TempData["errorSolicitud"] =
                    "El archivo del comprobante no debe superar los 5MB.";

                return RedirectToAction(nameof(Index));
            }

            // Extensiones permitidas
            var extensionesPermitidas = new[]
            {
        ".jpg",
        ".jpeg",
        ".png",
        ".pdf"
    };

            var extension = Path
                .GetExtension(comprobante.FileName)
                .ToLowerInvariant();

            if (!extensionesPermitidas.Contains(extension))
            {
                TempData["errorSolicitud"] =
                    "Formato de archivo no permitido. Solo se permiten JPG, PNG o PDF.";

                return RedirectToAction(nameof(Index));
            }

            // =========================================================
            // CREAR CARPETA
            // =========================================================

            string carpetaDestino = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot",
                "uploads",
                "comprobantes"
            );

            if (!Directory.Exists(carpetaDestino))
            {
                Directory.CreateDirectory(carpetaDestino);
            }

            // =========================================================
            // GENERAR NOMBRE DEL ARCHIVO
            // =========================================================

            string nombreArchivo =
                $"Comprobante_{solicitud.IdSolicitud}_{Guid.NewGuid()}{extension}";

            string rutaCompleta = Path.Combine(
                carpetaDestino,
                nombreArchivo
            );

            // =========================================================
            // GUARDAR ARCHIVO FÍSICAMENTE
            // =========================================================

            using (var stream = new FileStream(
                rutaCompleta,
                FileMode.Create))
            {
                await comprobante.CopyToAsync(stream);
            }

            // =========================================================
            // GUARDAR RUTA EN BASE DE DATOS
            // =========================================================

            solicitud.RutaComprobante =
                $"/uploads/comprobantes/{nombreArchivo}";

            // =========================================================
            // CAMBIAR ESTADO
            // =========================================================

            solicitud.IdEstadoSolicitud = 2;

            // =========================================================
            // OBSERVACIÓN
            // =========================================================

            solicitud.Observaciones =
                !string.IsNullOrWhiteSpace(observacionAprobacion)
                    ? observacionAprobacion.Trim()
                    : "Solicitud aprobada y comprobante registrado.";

            await _context.SaveChangesAsync();

            TempData["mensajeSolicitud"] =
                "Solicitud aprobada y comprobante registrado correctamente.";

            return RedirectToAction(nameof(Index));
        }
        // =========================================================
        // RECHAZAR SOLICITUD
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Rechazar(int id, string observaciones)
        {
            var solicitud = await _context.Solicitudes
                .FirstOrDefaultAsync(x => x.IdSolicitud == id);

            if (solicitud == null)
            {
                TempData["errorSolicitud"] = "Solicitud no encontrada.";
                return RedirectToAction(nameof(Index));
            }

            if (string.IsNullOrWhiteSpace(observaciones))
            {
                TempData["errorSolicitud"] = "Debe ingresar las observaciones del rechazo.";
                return RedirectToAction(nameof(Index));
            }

            // 3 = Rechazada
            solicitud.IdEstadoSolicitud = 3;
            solicitud.Observaciones = observaciones.Trim();

            await _context.SaveChangesAsync();

            TempData["mensajeSolicitud"] = "La solicitud fue rechazada correctamente.";

            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // VER DETALLE
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var solicitud = await _context.Solicitudes
                .AsNoTracking()
                .Include(x => x.Usuario)
                .Include(x => x.EstadoSolicitud)
                .FirstOrDefaultAsync(x => x.IdSolicitud == id);

            if (solicitud == null)
            {
                return NotFound();
            }

            return View(solicitud);
        }



        // =========================================================
        // GENERAR CORREO HTML
        // =========================================================

        private string GenerarCorreoSolicitud(
            Solicitud solicitud,
            string nombreEmpleado,
            string urlSistema)
        {
            return $"""
<!DOCTYPE html>
<html lang="es">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Nueva solicitud de viáticos</title>
</head>

<body style="
    margin:0;
    padding:0;
    background:#f1f4f7;
    font-family:Arial,Helvetica,sans-serif;
    color:#111111;
">

<table width="100%" cellpadding="0" cellspacing="0" border="0"
       style="background:#f1f4f7;padding:35px 15px;">

    <tr>
        <td align="center">

            <table width="650" cellpadding="0" cellspacing="0" border="0"
                   style="
                   width:100%;
                   max-width:650px;
                   background:#ffffff;
                   border-radius:10px;
                   overflow:hidden;
                   border:1px solid #dfe3e7;
                   ">

                <tr>
                    <td style="
                        padding:24px;
                        text-align:center;
                        border-bottom:4px solid #C9A227;
                        background:#ffffff;
                    ">
                        <img src="cid:logoDinacen"
                             alt="DINACEN"
                             width="170"
                             style="
                             width:170px;
                             display:block;
                             margin:0 auto;
                             ">
                    </td>
                </tr>

                <tr>
                    <td style="
                        background:#123B5D;
                        padding:20px 25px;
                        text-align:center;
                    ">
                        <div style="
                            color:#ffffff;
                            font-size:20px;
                            font-weight:bold;
                        ">
                            Nueva solicitud de viáticos
                        </div>
                    </td>
                </tr>

                <tr>
                    <td style="padding:30px 32px;">

                        <p style="
                            margin:0 0 15px;
                            font-size:16px;
                            font-weight:bold;
                            color:#111111;
                        ">
                            Estimado administrador:
                        </p>

                        <p style="
                            margin:0 0 22px;
                            font-size:15px;
                            line-height:1.6;
                            color:#222222;
                        ">
                            El representante
                            <strong style="color:#123B5D;">
                                {nombreEmpleado}
                            </strong>
                            ha registrado una nueva solicitud de viáticos
                            que requiere su revisión.
                        </p>

                        <table width="100%"
                               cellpadding="0"
                               cellspacing="0"
                               border="0"
                               style="margin-bottom:25px;">

                            <tr>
                                <td style="
                                    background:#fff8df;
                                    border-left:4px solid #C9A227;
                                    padding:14px 16px;
                                    color:#222222;
                                    font-size:14px;
                                ">
                                    <strong>Estado:</strong>
                                    Pendiente de revisión
                                </td>
                            </tr>

                        </table>

                        <div style="
                            font-size:18px;
                            font-weight:bold;
                            color:#111111;
                            margin-bottom:12px;
                        ">
                            Detalle de la solicitud
                        </div>

                        <table width="100%"
                               cellpadding="0"
                               cellspacing="0"
                               border="0"
                               style="
                               border-collapse:collapse;
                               border:1px solid #d9dee3;
                               ">

                            <tr>
                                <td style="
                                    width:40%;
                                    padding:13px;
                                    background:#f4f6f8;
                                    border-bottom:1px solid #d9dee3;
                                    font-size:14px;
                                    font-weight:bold;
                                    color:#111111;
                                ">
                                    N.º de solicitud
                                </td>

                                <td style="
                                    padding:13px;
                                    border-bottom:1px solid #d9dee3;
                                    font-size:14px;
                                    color:#111111;
                                ">
                                    #{solicitud.IdSolicitud}
                                </td>
                            </tr>

                            <tr>
                                <td style="
                                    padding:13px;
                                    background:#f4f6f8;
                                    border-bottom:1px solid #d9dee3;
                                    font-size:14px;
                                    font-weight:bold;
                                    color:#111111;
                                ">
                                    Representante
                                </td>

                                <td style="
                                    padding:13px;
                                    border-bottom:1px solid #d9dee3;
                                    font-size:14px;
                                    color:#111111;
                                ">
                                    {nombreEmpleado}
                                </td>
                            </tr>

                            <tr>
                                <td style="
                                    padding:13px;
                                    background:#f4f6f8;
                                    border-bottom:1px solid #d9dee3;
                                    font-size:14px;
                                    font-weight:bold;
                                    color:#111111;
                                ">
                                    Destino
                                </td>

                                <td style="
                                    padding:13px;
                                    border-bottom:1px solid #d9dee3;
                                    font-size:14px;
                                    color:#111111;
                                ">
                                    {solicitud.Destino}
                                </td>
                            </tr>

                            <tr>
                                <td style="
                                    padding:13px;
                                    background:#f4f6f8;
                                    border-bottom:1px solid #d9dee3;
                                    font-size:14px;
                                    font-weight:bold;
                                    color:#111111;
                                ">
                                    Motivo
                                </td>

                                <td style="
                                    padding:13px;
                                    border-bottom:1px solid #d9dee3;
                                    font-size:14px;
                                    line-height:1.5;
                                    color:#111111;
                                ">
                                    {solicitud.Motivo}
                                </td>
                            </tr>

                            <tr>
                                <td style="
                                    padding:13px;
                                    background:#f4f6f8;
                                    border-bottom:1px solid #d9dee3;
                                    font-size:14px;
                                    font-weight:bold;
                                    color:#111111;
                                ">
                                    Fecha de inicio
                                </td>

                                <td style="
                                    padding:13px;
                                    border-bottom:1px solid #d9dee3;
                                    font-size:14px;
                                    color:#111111;
                                ">
                                    {solicitud.FechaInicio:dd/MM/yyyy}
                                </td>
                            </tr>

                            <tr>
                                <td style="
                                    padding:13px;
                                    background:#f4f6f8;
                                    font-size:14px;
                                    font-weight:bold;
                                    color:#111111;
                                ">
                                    Fecha de fin
                                </td>

                                <td style="
                                    padding:13px;
                                    font-size:14px;
                                    color:#111111;
                                ">
                                    {solicitud.FechaFin:dd/MM/yyyy}
                                </td>
                            </tr>

                            <tr>
                                <td style="
                                    padding:14px;
                                    background:#f4f6f8;
                                    font-size:14px;
                                    font-weight:bold;
                                    color:#111111;
                                ">
                                    Monto solicitado
                                </td>

                                <td style="
                                    padding:14px;
                                    font-size:17px;
                                    font-weight:bold;
                                    color:#123B5D;
                                ">
                                    S/ {solicitud.Monto:N2}
                                </td>
                            </tr>

                        </table>

                        <div style="
                            text-align:center;
                            margin-top:28px;
                        ">
                            <a href="{urlSistema}"
                               target="_blank"
                               style="
                               display:inline-block;
                               background:#123B5D;
                               color:#ffffff;
                               text-decoration:none;
                               font-size:15px;
                               font-weight:bold;
                               padding:14px 30px;
                               border-radius:6px;
                               ">
                                Revisar solicitud
                            </a>
                        </div>

                    </td>
                </tr>

                <tr>
                    <td style="
                        background:#f4f6f8;
                        border-top:1px solid #dfe3e7;
                        padding:16px;
                        text-align:center;
                        color:#555555;
                        font-size:12px;
                    ">
                        Mensaje generado automáticamente por el
                        Sistema de Gestión de Viáticos DINACEN.
                    </td>
                </tr>

            </table>

        </td>
    </tr>

</table>

</body>
</html>
""";
        }
    }
}