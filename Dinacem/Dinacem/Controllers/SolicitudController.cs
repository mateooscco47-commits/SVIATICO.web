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
            // OBTENER MOTIVO DE BLOQUEO
            // =========================================================
            //
            // REGLAS:
            //
            // SOLICITUD
            // 1 = Pendiente de revisión
            // 2 = Aprobada
            // 3 = Rechazada
            //
            // RENDICIÓN
            // 1 = Borrador
            // 2 = Pendiente de revisión
            // 3 = Aprobada
            //
            // El representante NO puede crear una nueva solicitud
            // mientras tenga un proceso anterior pendiente.
            //
            // =========================================================

            private async Task<(bool tieneBloqueo, string mensajeError)>
                ObtenerMotivoBloqueoAsync(int idUsuario)
            {
                // =====================================================
                // ESTADOS DE SOLICITUD
                // =====================================================

                const int ESTADO_SOLICITUD_PENDIENTE = 1;
                const int ESTADO_SOLICITUD_APROBADA = 2;


                // =====================================================
                // ESTADOS DE RENDICIÓN
                // =====================================================

                const int ESTADO_RENDICION_BORRADOR = 1;
                const int ESTADO_RENDICION_PENDIENTE_REVISION = 2;


                // =====================================================
                // 1. SOLICITUD PENDIENTE DE REVISIÓN
                // =====================================================

                bool tieneSolicitudPendiente =
                    await _context.Solicitudes
                        .AsNoTracking()
                        .AnyAsync(s =>
                            s.IdUsuario == idUsuario &&
                            s.IdEstadoSolicitud ==
                                ESTADO_SOLICITUD_PENDIENTE);

                if (tieneSolicitudPendiente)
                {
                    return (
                        true,
                        "No puede registrar una nueva solicitud porque tiene una solicitud pendiente de revisión."
                    );
                }


                // =====================================================
                // 2. SOLICITUD APROBADA SIN RENDICIÓN
                // =====================================================
                //
                // Si la solicitud fue aprobada pero todavía no tiene
                // una rendición registrada, también bloqueamos.
                //
                // =====================================================

                bool tieneSolicitudSinRendicion =
                    await _context.Solicitudes
                        .AsNoTracking()
                        .AnyAsync(s =>
                            s.IdUsuario == idUsuario &&
                            s.IdEstadoSolicitud ==
                                ESTADO_SOLICITUD_APROBADA &&
                            !_context.Rendiciones
                                .Any(r =>
                                    r.IdSolicitud ==
                                    s.IdSolicitud));

                if (tieneSolicitudSinRendicion)
                {
                    return (
                        true,
                        "No puede registrar una nueva solicitud porque tiene una solicitud aprobada pendiente de rendición."
                    );
                }


                // =====================================================
                // 3. RENDICIÓN EN BORRADOR
                // =====================================================

                bool tieneRendicionBorrador =
                    await _context.Rendiciones
                        .AsNoTracking()
                        .AnyAsync(r =>
                            r.IdUsuario == idUsuario &&
                            r.IdEstadoRendicion ==
                                ESTADO_RENDICION_BORRADOR);

                if (tieneRendicionBorrador)
                {
                    return (
                        true,
                        "No puede registrar una nueva solicitud porque tiene una rendición pendiente de completar."
                    );
                }


                // =====================================================
                // 4. RENDICIÓN PENDIENTE DE REVISIÓN
                // =====================================================
                //
                // ESTA ES LA CORRECCIÓN PRINCIPAL.
                //
                // Antes estabas utilizando:
                //
                // IdEstadoRendicion == 1
                //
                // pero 1 corresponde a Borrador.
                //
                // Pendiente de revisión = 2.
                //
                // =====================================================

                bool tieneRendicionPendienteRevision =
                    await _context.Rendiciones
                        .AsNoTracking()
                        .AnyAsync(r =>
                            r.IdUsuario == idUsuario &&
                            r.IdEstadoRendicion ==
                                ESTADO_RENDICION_PENDIENTE_REVISION);

                if (tieneRendicionPendienteRevision)
                {
                    return (
                        true,
                        "No puede registrar una nueva solicitud porque tiene una rendición pendiente de revisión. Debe esperar a que la rendición sea aprobada."
                    );
                }


                // =====================================================
                // 5. NO EXISTE BLOQUEO
                // =====================================================

                return (
                    false,
                    string.Empty
                );
            }


            // =========================================================
            // CREAR SOLICITUD - GET
            // =========================================================

            [HttpGet]
            public async Task<IActionResult> Create()
            {
                var idUsuario =
                    HttpContext.Session.GetInt32("IdUsuario");


                // =====================================================
                // VALIDAR SESIÓN
                // =====================================================

                if (idUsuario == null)
                {
                    TempData["errorSolicitud"] =
                        "La sesión ha expirado. Inicie sesión nuevamente.";

                    return RedirectToAction(
                        "Index",
                        "Home");
                }


                // =====================================================
                // VALIDAR BLOQUEOS
                // =====================================================

                var (
                    tieneBloqueo,
                    mensajeError
                ) =
                    await ObtenerMotivoBloqueoAsync(
                        idUsuario.Value);


                if (tieneBloqueo)
                {
                    TempData["errorSolicitud"] =
                        mensajeError;

                    return RedirectToAction(
                        nameof(MisSolicitudes));
                }


                // =====================================================
                // MOSTRAR FORMULARIO
                // =====================================================

                return View();
            }


            // =========================================================
            // CREAR SOLICITUD - POST
            // =========================================================

            [HttpPost]
            [ValidateAntiForgeryToken]
            public async Task<IActionResult> Create(
                Solicitud solicitud)
            {
                var idUsuario =
                    HttpContext.Session.GetInt32("IdUsuario");


                // =====================================================
                // VALIDAR SESIÓN
                // =====================================================

                if (idUsuario == null)
                {
                    TempData["errorSolicitud"] =
                        "La sesión ha expirado. Inicie sesión nuevamente.";

                    return RedirectToAction(
                        "Index",
                        "Home");
                }


                // =====================================================
                // VALIDAR BLOQUEOS
                // =====================================================
                //
                // IMPORTANTE:
                // Esta validación también está en POST para impedir
                // que alguien pueda saltarse el bloqueo enviando
                // directamente el formulario.
                //
                // =====================================================

                var (
                    tieneBloqueo,
                    mensajeError
                ) =
                    await ObtenerMotivoBloqueoAsync(
                        idUsuario.Value);


                if (tieneBloqueo)
                {
                    TempData["errorSolicitud"] =
                        mensajeError;

                    return RedirectToAction(
                        nameof(MisSolicitudes));
                }


                // =====================================================
                // VALIDAR FECHAS
                // =====================================================

                if (solicitud.FechaInicio.Date >
                    solicitud.FechaFin.Date)
                {
                    ModelState.AddModelError(
                        nameof(solicitud.FechaFin),
                        "La fecha final no puede ser anterior a la fecha inicial."
                    );
                }


                // =====================================================
                // VALIDAR MONTO
                // =====================================================

                if (solicitud.Monto <= 0)
                {
                    ModelState.AddModelError(
                        nameof(solicitud.Monto),
                        "El monto debe ser mayor que cero."
                    );
                }


                // =====================================================
                // VALIDAR MODELO
                // =====================================================

                if (!ModelState.IsValid)
                {
                    return View(solicitud);
                }


                // =====================================================
                // ASIGNAR DATOS DEL USUARIO
                // =====================================================

                solicitud.IdUsuario =
                    idUsuario.Value;

                solicitud.Fecha =
                    DateTime.Now;

                solicitud.IdEstadoSolicitud =
                    1;

                solicitud.Observaciones =
                    string.Empty;


                // =====================================================
                // GUARDAR SOLICITUD
                // =====================================================

                _context.Solicitudes.Add(
                    solicitud);

                await _context.SaveChangesAsync();


                // =====================================================
                // OBTENER DATOS DEL REPRESENTANTE Y ZONA
                // =====================================================

                var empleado =
                    await _context.Usuarios
                        .AsNoTracking()
                        .Include(u => u.Zona)
                        .FirstOrDefaultAsync(u =>
                            u.IdUsuario ==
                            solicitud.IdUsuario);


                string nombreEmpleado;
                string zonaEmpleado;


                if (empleado == null)
                {
                    nombreEmpleado =
                        $"Usuario {solicitud.IdUsuario}";

                    zonaEmpleado =
                        "Zona no registrada";
                }
                else
                {
                    nombreEmpleado =
                        $"{empleado.Nombres} {empleado.Apellidos}"
                        .Trim();

                    zonaEmpleado =
                        empleado.Zona?.CodigoZona ??
                        "Zona no registrada";
                }


                // =====================================================
                // OBTENER ADMINISTRADORES
                // =====================================================

                var correosAdministradores =
                    await _context.Usuarios
                        .AsNoTracking()
                        .Include(u => u.Rol)
                        .Where(u =>
                            u.Rol != null &&
                            u.Rol.Nombre == "Administrador" &&
                            u.Estado &&
                            u.IdUsuario != solicitud.IdUsuario &&
                            !string.IsNullOrWhiteSpace(u.Correo))
                        .Select(u =>
                            u.Correo!.Trim())
                        .Distinct()
                        .ToListAsync();


                // =====================================================
                // PREPARAR ASUNTO
                // =====================================================

                string asunto =
                    $"Nueva solicitud de viáticos #{solicitud.IdSolicitud}";


            // =================================================
            // URL REAL DEL SISTEMA
            // =================================================

            string urlSistema = "https://viaticos.dinacen.online/";


            // =====================================================
            // GENERAR HTML
            // =====================================================

            string contenidoHtml =
                    GenerarCorreoSolicitud(
                        solicitud,
                        nombreEmpleado,
                        zonaEmpleado,
                        urlSistema);


                // =====================================================
                // ENVIAR CORREO
                // =====================================================

                bool correoEnviado =
                    false;


                if (correosAdministradores.Any())
                {
                    try
                    {
                        correoEnviado =
                            await _correoService.EnviarAsync(
                                correosAdministradores,
                                asunto,
                                contenidoHtml);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(
                            $"Error enviando correo: {ex.Message}");

                        correoEnviado =
                            false;
                    }
                }


                // =====================================================
                // MENSAJE FINAL
                // =====================================================

                TempData["mensajeSolicitud"] =
                    correoEnviado

                    ? "Solicitud registrada correctamente y notificación enviada a los administradores."

                    : "Solicitud registrada correctamente, pero no fue posible enviar la notificación por correo.";


                return RedirectToAction(
                    nameof(MisSolicitudes));
            }


            // =========================================================
            // MIS SOLICITUDES
            // =========================================================

            [HttpGet]
            public async Task<IActionResult> MisSolicitudes()
            {
                var idUsuario =
                    HttpContext.Session.GetInt32("IdUsuario");


                // =====================================================
                // VALIDAR SESIÓN
                // =====================================================

                if (idUsuario == null)
                {
                    TempData["errorSolicitud"] =
                        "La sesión ha expirado. Inicie sesión nuevamente.";

                    return RedirectToAction(
                        "Index",
                        "Home");
                }


                // =====================================================
                // OBTENER SOLICITUDES
                // =====================================================

                var lista =
                    await _context.Solicitudes
                        .AsNoTracking()
                        .Include(x =>
                            x.EstadoSolicitud)
                        .Where(x =>
                            x.IdUsuario ==
                            idUsuario.Value)
                        .OrderByDescending(x =>
                            x.Fecha)
                        .ToListAsync();


                return View(lista);
            }


            // =========================================================
            // LISTADO DE SOLICITUDES
            // =========================================================

            [HttpGet]
            public async Task<IActionResult> Index()
            {
                var lista =
                    await _context.Solicitudes
                        .AsNoTracking()
                        .Include(x =>
                            x.Usuario)
                        .Include(x =>
                            x.EstadoSolicitud)
                        .OrderByDescending(x =>
                            x.Fecha)
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
                IFormFile? comprobante,
                string? observacionAprobacion)
            {
                // =====================================================
                // BUSCAR SOLICITUD
                // =====================================================

                var solicitud =
                    await _context.Solicitudes
                        .FirstOrDefaultAsync(x =>
                            x.IdSolicitud == id);


                if (solicitud == null)
                {
                    TempData["errorSolicitud"] =
                        "Solicitud no encontrada.";

                    return RedirectToAction(
                        nameof(Index));
                }


                // =====================================================
                // VALIDAR QUE NO ESTÉ APROBADA
                // =====================================================

                if (solicitud.IdEstadoSolicitud == 2)
                {
                    TempData["errorSolicitud"] =
                        "La solicitud ya se encuentra aprobada.";

                    return RedirectToAction(
                        nameof(Index));
                }


                // =====================================================
                // VALIDAR ARCHIVO
                // =====================================================

                if (comprobante != null &&
                    comprobante.Length > 0)
                {
                    // =================================================
                    // TAMAÑO MÁXIMO 5 MB
                    // =================================================

                    if (comprobante.Length >
                        5 * 1024 * 1024)
                    {
                        TempData["errorSolicitud"] =
                            "El archivo del comprobante no debe superar los 5MB.";

                        return RedirectToAction(
                            nameof(Index));
                    }


                    // =================================================
                    // EXTENSIONES PERMITIDAS
                    // =================================================

                    var extensionesPermitidas =
                        new[]
                        {
                        ".jpg",
                        ".jpeg",
                        ".png",
                        ".pdf"
                        };


                    var extension =
                        Path
                            .GetExtension(
                                comprobante.FileName)
                            .ToLowerInvariant();


                    if (!extensionesPermitidas
                        .Contains(extension))
                    {
                        TempData["errorSolicitud"] =
                            "Formato de archivo no permitido. Solo se permiten JPG, PNG o PDF.";

                        return RedirectToAction(
                            nameof(Index));
                    }


                    // =================================================
                    // CARPETA DE DESTINO
                    // =================================================

                    string carpetaDestino =
                        Path.Combine(
                            Directory.GetCurrentDirectory(),
                            "wwwroot",
                            "uploads",
                            "comprobantes");


                    if (!Directory.Exists(
                            carpetaDestino))
                    {
                        Directory.CreateDirectory(
                            carpetaDestino);
                    }


                    // =================================================
                    // GENERAR NOMBRE ÚNICO
                    // =================================================

                    string nombreArchivo =
                        $"Comprobante_{solicitud.IdSolicitud}_{Guid.NewGuid()}{extension}";


                    string rutaCompleta =
                        Path.Combine(
                            carpetaDestino,
                            nombreArchivo);


                    // =================================================
                    // GUARDAR ARCHIVO
                    // =================================================

                    using (var stream =
                        new FileStream(
                            rutaCompleta,
                            FileMode.Create))
                    {
                        await comprobante
                            .CopyToAsync(stream);
                    }


                    // =================================================
                    // GUARDAR RUTA
                    // =================================================

                    solicitud.RutaComprobante =
                        $"/uploads/comprobantes/{nombreArchivo}";
                }


                // =====================================================
                // CAMBIAR ESTADO
                // =====================================================

                solicitud.IdEstadoSolicitud =
                    2;


                // =====================================================
                // GUARDAR OBSERVACIÓN
                // =====================================================

                if (!string.IsNullOrWhiteSpace(
                    observacionAprobacion))
                {
                    solicitud.Observaciones =
                        observacionAprobacion.Trim();
                }
                else
                {
                    solicitud.Observaciones =
                        "Solicitud aprobada.";
                }


                // =====================================================
                // GUARDAR CAMBIOS
                // =====================================================

                await _context.SaveChangesAsync();


                // =====================================================
                // BUSCAR SOLICITANTE
                // =====================================================

                var solicitante =
                    await _context.Usuarios
                        .AsNoTracking()
                        .Include(u => u.Zona)
                        .FirstOrDefaultAsync(u =>
                            u.IdUsuario ==
                            solicitud.IdUsuario);


                bool correoEnviado =
                    false;


                // =====================================================
                // ENVIAR CORREO AL SOLICITANTE
                // =====================================================

                if (solicitante != null &&
                    !string.IsNullOrWhiteSpace(
                        solicitante.Correo))
                {
                    string nombreSolicitante =
                        $"{solicitante.Nombres} {solicitante.Apellidos}"
                        .Trim();


                    string correoSolicitante =
                        solicitante.Correo.Trim();


                // =================================================
                // URL REAL DEL SISTEMA
                // =================================================

                string urlSistema = "https://viaticos.dinacen.online/";


                // =================================================
                // ASUNTO
                // =================================================

                string asunto =
                        $"Solicitud de viáticos #{solicitud.IdSolicitud} aprobada";


                    // =================================================
                    // GENERAR CORREO
                    // =================================================

                    string contenidoHtml =
                        GenerarCorreoSolicitudAprobada(
                            solicitud,
                            nombreSolicitante,
                            urlSistema);


                    // =================================================
                    // ENVIAR
                    // =================================================

                    try
                    {
                        correoEnviado =
                            await _correoService.EnviarAsync(
                                new[]
                                {
                                correoSolicitante
                                },
                                asunto,
                                contenidoHtml);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(
                            $"Error enviando correo: {ex.Message}");

                        correoEnviado =
                            false;
                    }
                }


                // =====================================================
                // MENSAJE FINAL
                // =====================================================

                if (comprobante != null &&
                    comprobante.Length > 0)
                {
                    if (correoEnviado)
                    {
                        TempData["mensajeSolicitud"] =
                            "Solicitud aprobada, comprobante registrado y notificación enviada al solicitante.";
                    }
                    else
                    {
                        TempData["mensajeSolicitud"] =
                            "Solicitud aprobada y comprobante registrado correctamente, pero no fue posible enviar la notificación al solicitante.";
                    }
                }
                else
                {
                    if (correoEnviado)
                    {
                        TempData["mensajeSolicitud"] =
                            "Solicitud aprobada correctamente y notificación enviada al solicitante.";
                    }
                    else
                    {
                        TempData["mensajeSolicitud"] =
                            "Solicitud aprobada correctamente, pero no fue posible enviar la notificación al solicitante.";
                    }
                }


                return RedirectToAction(
                    nameof(Index));
            }


            // =========================================================
            // RECHAZAR SOLICITUD
            // =========================================================

            [HttpPost]
            [ValidateAntiForgeryToken]
            public async Task<IActionResult> Rechazar(
                int id,
                string observaciones)
            {
                var solicitud =
                    await _context.Solicitudes
                        .FirstOrDefaultAsync(x =>
                            x.IdSolicitud == id);


                if (solicitud == null)
                {
                    TempData["errorSolicitud"] =
                        "Solicitud no encontrada.";

                    return RedirectToAction(
                        nameof(Index));
                }


                if (string.IsNullOrWhiteSpace(
                    observaciones))
                {
                    TempData["errorSolicitud"] =
                        "Debe ingresar las observaciones del rechazo.";

                    return RedirectToAction(
                        nameof(Index));
                }


                // =====================================================
                // CAMBIAR ESTADO A RECHAZADA
                // =====================================================

                solicitud.IdEstadoSolicitud =
                    3;


                solicitud.Observaciones =
                    observaciones.Trim();


                await _context.SaveChangesAsync();


                TempData["mensajeSolicitud"] =
                    "La solicitud fue rechazada correctamente.";


                return RedirectToAction(
                    nameof(Index));
            }


            // =========================================================
            // DETALLES DE SOLICITUD
            // =========================================================

            [HttpGet]
            public async Task<IActionResult> Details(
                int id)
            {
                var solicitud =
                    await _context.Solicitudes
                        .AsNoTracking()
                        .Include(x =>
                            x.Usuario)
                            .ThenInclude(u =>
                                u.Zona)
                        .Include(x =>
                            x.EstadoSolicitud)
                        .FirstOrDefaultAsync(x =>
                            x.IdSolicitud == id);


                if (solicitud == null)
                {
                    return NotFound();
                }


                return View(solicitud);
            }


            // =========================================================
            // CORREO DE SOLICITUD APROBADA
            // =========================================================

            private string GenerarCorreoSolicitudAprobada(
                Solicitud solicitud,
                string nombreSolicitante,
                string urlSistema)
            {
                return $"""
<!DOCTYPE html>
<html lang="es">

<head>

    <meta charset="UTF-8">

    <meta name="viewport"
          content="width=device-width, initial-scale=1.0">

    <title>Solicitud de viáticos aprobada</title>

</head>

<body style="
    margin:0;
    padding:0;
    background:#f1f4f7;
    font-family:Arial,Helvetica,sans-serif;
    color:#111111;
">

<table width="100%"
       cellpadding="0"
       cellspacing="0"
       border="0"
       style="
           background:#f1f4f7;
           padding:35px 15px;
       ">

<tr>

<td align="center">

<table width="650"
       cellpadding="0"
       cellspacing="0"
       border="0"
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

Solicitud de viáticos aprobada

</div>

</td>

</tr>

<tr>

<td style="
    padding:30px 32px;
">

<p style="
    margin:0 0 15px;
    font-size:16px;
    font-weight:bold;
    color:#111111;
">

Estimado(a) {nombreSolicitante}:

</p>

<p style="
    margin:0 0 22px;
    font-size:15px;
    line-height:1.6;
    color:#222222;
">

Le informamos que su solicitud de viáticos

<strong style="color:#123B5D;">
#{solicitud.IdSolicitud}
</strong>

ha sido

<strong style="color:#6AA84F;">
APROBADA
</strong>

correctamente.

</p>

<table width="100%"
       cellpadding="0"
       cellspacing="0"
       border="0"
       style="
           margin-bottom:25px;
       ">

<tr>

<td style="
    background:#eef7e9;
    border-left:4px solid #6AA84F;
    padding:14px 16px;
    color:#222222;
    font-size:14px;
">

<strong>Estado:</strong>

Solicitud aprobada

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
    background:#ffffff;
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
    border-bottom:1px solid #d9dee3;
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

Monto aprobado

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

<tr>

<td style="
    padding:14px;
    background:#f4f6f8;
    font-size:14px;
    font-weight:bold;
    color:#111111;
">

Observación

</td>

<td style="
    padding:14px;
    font-size:14px;
    line-height:1.5;
    color:#111111;
">

{solicitud.Observaciones}

</td>

</tr>

</table>

<div style="
    text-align:center;
    margin-top:28px;
">

<a href="{urlSistema}"
   style="display:inline-block;
          padding:12px 24px;
          background:#0C4A8A;
          color:#ffffff;
          text-decoration:none;
          border-radius:6px;
          font-weight:bold;">
    Ingresar al sistema
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

Mensaje generado automáticamente por el Sistema de Gestión de Viáticos DINACEN.

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


            // =========================================================
            // CORREO DE NUEVA SOLICITUD
            // =========================================================

            private string GenerarCorreoSolicitud(
                Solicitud solicitud,
                string nombreEmpleado,
                string zonaEmpleado,
                string urlSistema)
            {
                return $"""
<!DOCTYPE html>
<html lang="es">

<head>

    <meta charset="UTF-8">

    <meta name="viewport"
          content="width=device-width, initial-scale=1.0">

    <title>Nueva solicitud de viáticos</title>

</head>

<body style="
    margin:0;
    padding:0;
    background:#f1f4f7;
    font-family:Arial,Helvetica,sans-serif;
    color:#111111;
">

<table width="100%"
       cellpadding="0"
       cellspacing="0"
       border="0"
       style="
           background:#f1f4f7;
           padding:35px 15px;
       ">

<tr>

<td align="center">

<table width="650"
       cellpadding="0"
       cellspacing="0"
       border="0"
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

<td style="
    padding:30px 32px;
">

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

<span style="color:#666666;">

— {zonaEmpleado}

</span>

ha registrado una nueva solicitud de viáticos que requiere su revisión.

</p>

<table width="100%"
       cellpadding="0"
       cellspacing="0"
       border="0"
       style="
           margin-bottom:25px;
       ">

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
">

N.º de solicitud

</td>

<td style="
    padding:13px;
    border-bottom:1px solid #d9dee3;
    font-size:14px;
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
">

Representante

</td>

<td style="
    padding:13px;
    border-bottom:1px solid #d9dee3;
    font-size:14px;
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
">

Zona

</td>

<td style="
    padding:13px;
    border-bottom:1px solid #d9dee3;
    font-size:14px;
">

{zonaEmpleado}

</td>

</tr>

<tr>

<td style="
    padding:13px;
    background:#f4f6f8;
    border-bottom:1px solid #d9dee3;
    font-size:14px;
    font-weight:bold;
">

Destino

</td>

<td style="
    padding:13px;
    border-bottom:1px solid #d9dee3;
    font-size:14px;
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
">

Motivo

</td>

<td style="
    padding:13px;
    border-bottom:1px solid #d9dee3;
    font-size:14px;
    line-height:1.5;
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
">

Fecha de inicio

</td>

<td style="
    padding:13px;
    border-bottom:1px solid #d9dee3;
    font-size:14px;
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
">

Fecha de fin

</td>

<td style="
    padding:13px;
    font-size:14px;
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
   style="display:inline-block;
          padding:12px 24px;
          background:#0C4A8A;
          color:#ffffff;
          text-decoration:none;
          border-radius:6px;
          font-weight:bold;">
    Ingresar al sistema
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

Mensaje generado automáticamente por el Sistema de Gestión de Viáticos DINACEN.

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
