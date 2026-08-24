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

            // ============================================================
            // VALIDAR SOLICITUD SIN RENDIR
            // ============================================================

            bool tieneSolicitudSinRendir =
                await _context.Solicitudes.AnyAsync(s =>
                    s.IdUsuario == idUsuario.Value &&
                    (
                        // 1 = Pendiente de aprobación
                        s.IdEstadoSolicitud == 1

                        ||

                        // 2 = Aprobada pero todavía sin rendición
                        (
                            s.IdEstadoSolicitud == 2 &&
                            !_context.Rendiciones.Any(r =>
                                r.IdSolicitud == s.IdSolicitud)
                        )
                    )
                );

            if (tieneSolicitudSinRendir)
            {
                TempData["error"] =
                    "No puede registrar una nueva solicitud porque tiene una solicitud pendiente de rendición.";

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

            // ============================================================
            // VALIDAR SOLICITUD SIN RENDIR
            // ============================================================

            bool tieneSolicitudSinRendir =
                await _context.Solicitudes.AnyAsync(s =>
                    s.IdUsuario == idUsuario.Value &&
                    (
                        // 1 = Pendiente de aprobación
                        s.IdEstadoSolicitud == 1

                        ||

                        // 2 = Aprobada pero todavía sin rendición
                        (
                            s.IdEstadoSolicitud == 2 &&
                            !_context.Rendiciones.Any(r =>
                                r.IdSolicitud == s.IdSolicitud)
                        )
                    )
                );

            if (tieneSolicitudSinRendir)
            {
                TempData["error"] =
                    
                    "No puede registrar una nueva solicitud porque tiene una solicitud pendiente de aprobación o rendición.";

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

            string urlSistema =
                $"https://TU-DOMINIO.com/Solicitud/Details/{solicitud.IdSolicitud}";

            string contenidoHtml = $"""
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
    background-color:#eef2f5;
    font-family:Arial, Helvetica, sans-serif;
">

<table width="100%"
       cellpadding="0"
       cellspacing="0"
       border="0"
       style="
       background-color:#eef2f5;
       padding:35px 15px;
       ">

    <tr>
        <td align="center">

            <!-- CONTENEDOR PRINCIPAL -->
            <table width="650"
                   cellpadding="0"
                   cellspacing="0"
                   border="0"
                   style="
                   width:100%;
                   max-width:650px;
                   background-color:#ffffff;
                   border-radius:12px;
                   overflow:hidden;
                   box-shadow:0 4px 15px rgba(0,0,0,0.08);
                   ">

                <!-- ================================= -->
                <!-- ENCABEZADO                         -->
                <!-- ================================= -->

                <tr>
                    <td style="
                        background-color:#ffffff;
                        padding:22px 30px;
                        text-align:center;
                        border-bottom:4px solid #C9A227;
                    ">

                        <img src="cid:logoDinacen"
                             alt="Logo"
                             width="165"
                             style="
                             width:165px;
                             max-width:165px;
                             height:auto;
                             display:block;
                             margin:0 auto;
                             border:0;
                             ">

                    </td>
                </tr>


                <!-- ================================= -->
                <!-- FRANJA SUPERIOR                    -->
                <!-- ================================= -->

                <tr>
                    <td style="
                        background-color:#123B5D;
                        padding:18px 30px;
                        text-align:center;
                    ">

                        <div style="
                            color:#ffffff;
                            font-size:19px;
                            font-weight:bold;
                            letter-spacing:0.3px;
                        ">

                            Nueva solicitud de viáticos

                        </div>

                    </td>
                </tr>


                <!-- ================================= -->
                <!-- CONTENIDO                          -->
                <!-- ================================= -->

                <tr>
                    <td style="
                        padding:30px 32px 35px 32px;
                    ">

                        <!-- SALUDO -->

                        <p style="
                            margin:0 0 12px 0;
                            color:#263238;
                            font-size:16px;
                            font-weight:bold;
                        ">

                            Estimado administrador:

                        </p>


                        <!-- MENSAJE -->

                        <p style="
                            margin:0 0 22px 0;
                            color:#555555;
                            font-size:15px;
                            line-height:1.6;
                        ">

                            El representante

                            <strong style="
                                color:#123B5D;
                            ">

                                {nombreEmpleado}

                            </strong>

                            ha registrado una nueva solicitud que requiere
                            su revisión.

                        </p>


                        <!-- ================================= -->
                        <!-- ESTADO                              -->
                        <!-- ================================= -->

                        <table width="100%"
                               cellpadding="0"
                               cellspacing="0"
                               border="0"
                               style="
                               margin-bottom:25px;
                               ">

                            <tr>

                                <td style="
                                    background-color:#fff9e6;
                                    border-left:4px solid #C9A227;
                                    padding:13px 16px;
                                    color:#6b5710;
                                    font-size:14px;
                                ">

                                    <strong>Estado:</strong>

                                    <span style="
                                        font-weight:bold;
                                        margin-left:5px;
                                    ">

                                        Pendiente de revisión

                                    </span>

                                </td>

                            </tr>

                        </table>


                        <!-- ================================= -->
                        <!-- DETALLE                             -->
                        <!-- ================================= -->

                        <div style="
                            color:#123B5D;
                            font-size:17px;
                            font-weight:bold;
                            margin-bottom:12px;
                        ">

                            Detalle de la solicitud

                        </div>


                        <!-- ================================= -->
                        <!-- TABLA DE INFORMACIÓN                -->
                        <!-- ================================= -->

                        <table width="100%"
                               cellpadding="0"
                               cellspacing="0"
                               border="0"
                               style="
                               border-collapse:collapse;
                               border:1px solid #dfe5ea;
                               ">

                            <!-- SOLICITUD -->

                            <tr>

                                <td width="40%"
                                    style="
                                    padding:12px 14px;
                                    background-color:#f5f7f9;
                                    border-bottom:1px solid #dfe5ea;
                                    color:#52606d;
                                    font-size:13px;
                                    ">

                                    <strong>N.º de solicitud</strong>

                                </td>

                                <td style="
                                    padding:12px 14px;
                                    border-bottom:1px solid #dfe5ea;
                                    color:#263238;
                                    font-size:14px;
                                ">

                                    #{solicitud.IdSolicitud}

                                </td>

                            </tr>


                            <!-- REPRESENTANTE -->

                            <tr>

                                <td style="
                                    padding:12px 14px;
                                    background-color:#f5f7f9;
                                    border-bottom:1px solid #dfe5ea;
                                    color:#52606d;
                                    font-size:13px;
                                ">

                                    <strong>Representante</strong>

                                </td>

                                <td style="
                                    padding:12px 14px;
                                    border-bottom:1px solid #dfe5ea;
                                    color:#263238;
                                    font-size:14px;
                                ">

                                    {nombreEmpleado}

                                </td>

                            </tr>


                            <!-- DESTINO -->

                            <tr>

                                <td style="
                                    padding:12px 14px;
                                    background-color:#f5f7f9;
                                    border-bottom:1px solid #dfe5ea;
                                    color:#52606d;
                                    font-size:13px;
                                ">

                                    <strong>Destino</strong>

                                </td>

                                <td style="
                                    padding:12px 14px;
                                    border-bottom:1px solid #dfe5ea;
                                    color:#263238;
                                    font-size:14px;
                                ">

                                    {solicitud.Destino}

                                </td>

                            </tr>


                            <!-- MOTIVO -->

                            <tr>

                                <td style="
                                    padding:12px 14px;
                                    background-color:#f5f7f9;
                                    border-bottom:1px solid #dfe5ea;
                                    color:#52606d;
                                    font-size:13px;
                                ">

                                    <strong>Motivo</strong>

                                </td>

                                <td style="
                                    padding:12px 14px;
                                    border-bottom:1px solid #dfe5ea;
                                    color:#263238;
                                    font-size:14px;
                                    line-height:1.5;
                                ">

                                    {solicitud.Motivo}

                                </td>

                            </tr>


                            <!-- FECHA INICIO -->

                            <tr>

                                <td style="
                                    padding:12px 14px;
                                    background-color:#f5f7f9;
                                    border-bottom:1px solid #dfe5ea;
                                    color:#52606d;
                                    font-size:13px;
                                ">

                                    <strong>Fecha de inicio</strong>

                                </td>

                                <td style="
                                    padding:12px 14px;
                                    border-bottom:1px solid #dfe5ea;
                                    color:#263238;
                                    font-size:14px;
                                ">

                                    {solicitud.FechaInicio:dd/MM/yyyy}

                                </td>

                            </tr>


                            <!-- FECHA FIN -->

                            <tr>

                                <td style="
                                    padding:12px 14px;
                                    background-color:#f5f7f9;
                                    border-bottom:1px solid #dfe5ea;
                                    color:#52606d;
                                    font-size:13px;
                                ">

                                    <strong>Fecha de fin</strong>

                                </td>

                                <td style="
                                    padding:12px 14px;
                                    border-bottom:1px solid #dfe5ea;
                                    color:#263238;
                                    font-size:14px;
                                ">

                                    {solicitud.FechaFin:dd/MM/yyyy}

                                </td>

                            </tr>


                            <!-- MONTO -->

                            <tr>

                                <td style="
                                    padding:13px 14px;
                                    background-color:#f5f7f9;
                                    color:#52606d;
                                    font-size:13px;
                                ">

                                    <strong>Monto solicitado</strong>

                                </td>

                                <td style="
                                    padding:13px 14px;
                                    color:#123B5D;
                                    font-size:17px;
                                    font-weight:bold;
                                ">

                                    S/ {solicitud.Monto:N2}

                                </td>

                            </tr>

                        </table>


                        <!-- ================================= -->
                        <!-- BOTÓN                              -->
                        <!-- ================================= -->

                        <div style="
                            text-align:center;
                            margin-top:28px;
                        ">

                            <a href="{urlSistema}"
                               target="_blank"
                               style="
                               display:inline-block;
                               background-color:#123B5D;
                               color:#ffffff;
                               text-decoration:none;
                               font-size:14px;
                               font-weight:bold;
                               padding:13px 30px;
                               border-radius:6px;
                               border-bottom:3px solid #C9A227;
                               ">

                                Revisar solicitud

                            </a>

                        </div>


                        <!-- ================================= -->
                        <!-- MENSAJE FINAL                      -->
                        <!-- ================================= -->

                        <p style="
                            margin:20px 0 0 0;
                            color:#7a858f;
                            font-size:12px;
                            line-height:1.5;
                            text-align:center;
                        ">

                            Revise la solicitud en el sistema para
                            continuar con el proceso correspondiente.

                        </p>

                    </td>
                </tr>


                <!-- ================================= -->
                <!-- PIE                                -->
                <!-- ================================= -->

                <tr>

                    <td style="
                        background-color:#f5f7f9;
                        border-top:1px solid #e1e6ea;
                        padding:15px 25px;
                        text-align:center;
                    ">

                        <div style="
                            color:#7a858f;
                            font-size:11px;
                            line-height:1.5;
                        ">

                            Mensaje generado automáticamente.
                            No responda a este correo.

                        </div>

                    </td>

                </tr>

            </table>

        </td>
    </tr>

</table>

</body>
</html>
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