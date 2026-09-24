using Dinacem.Models;
using Dinacem.Models.Servicios;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

    namespace Dinacem.Controllers
    {
        public class BitacoraVehiculoController : Controller
        {
            private readonly AplicacionDbContexto _context;
            private readonly RendicionPdfService _rendicionPdfService;

            public BitacoraVehiculoController(
                AplicacionDbContexto context,
                RendicionPdfService rendicionPdfService)
            {
                _context = context;
                _rendicionPdfService = rendicionPdfService;
            }

            // =========================================================
            // EMPLEADO: LISTAR BITÁCORA
            // =========================================================
            [HttpGet]
            public async Task<IActionResult> Index(int idRendicion)
            {
                var idUsuario =
                    HttpContext.Session.GetInt32("IdUsuario");

                if (idUsuario == null)
                {
                    TempData["error"] =
                        "La sesión ha expirado.";

                    return RedirectToAction(
                        "Index",
                        "Home");
                }

                var rendicion =
                    await _context.Rendiciones
                        .Include(r => r.Solicitud)
                        .FirstOrDefaultAsync(r =>
                            r.IdRendicion == idRendicion &&
                            r.IdUsuario == idUsuario.Value);

                if (rendicion == null)
                {
                    TempData["error"] =
                        "No se encontró la rendición.";

                    return RedirectToAction(
                        "Index",
                        "Rendicion");
                }

                var bitacoras =
                    await _context.BitacorasVehiculo
                        .AsNoTracking()
                        .Where(b =>
                            b.IdRendicion == idRendicion)
                        .OrderBy(b => b.Fecha)
                        .ThenBy(b => b.IdBitacoraVehiculo)
                        .ToListAsync();

                var configuracion =
                    await _context.ConfiguracionesSistema
                        .AsNoTracking()
                        .FirstOrDefaultAsync();

                var puntosOrigen =
                    await _context.Rutas
                        .AsNoTracking()
                        .Where(r =>
                            r.Estado)
                        .Select(r =>
                            r.Origen)
                        .Distinct()
                        .ToListAsync();

                var puntosDestino =
                    await _context.Rutas
                        .AsNoTracking()
                        .Where(r =>
                            r.Estado)
                        .Select(r =>
                            r.Destino)
                        .Distinct()
                        .ToListAsync();

                var puntos =
                    puntosOrigen
                        .Concat(puntosDestino)
                        .Where(p =>
                            !string.IsNullOrWhiteSpace(p))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .OrderBy(p => p)
                        .ToList();

                ViewBag.Rendicion =
                    rendicion;

                ViewBag.TarifaKilometro =
                    configuracion?.TarifaKilometro ?? 0m;

                ViewBag.Puntos =
                    puntos;

                return View(bitacoras);
            }

            // =========================================================
            // OBTENER RUTA DESDE LA BD
            // =========================================================
            [HttpGet]
            public async Task<IActionResult> ObtenerRuta(
                string origen,
                string destino)
            {
                if (string.IsNullOrWhiteSpace(origen) ||
                    string.IsNullOrWhiteSpace(destino))
                {
                    return Json(new
                    {
                        success = false,
                        mensaje =
                            "Debe seleccionar el origen y el destino."
                    });
                }

                origen =
                    origen.Trim();

                destino =
                    destino.Trim();

                if (string.Equals(
                        origen,
                        destino,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return Json(new
                    {
                        success = false,
                        mensaje =
                            "El origen y el destino no pueden ser iguales."
                    });
                }

                var ruta =
                    await _context.Rutas
                        .AsNoTracking()
                        .FirstOrDefaultAsync(r =>
                            r.Estado &&
                            (
                                (
                                    r.Origen == origen &&
                                    r.Destino == destino
                                )
                                ||
                                (
                                    r.Origen == destino &&
                                    r.Destino == origen
                                )
                            ));

                if (ruta == null)
                {
                    return Json(new
                    {
                        success = false,
                        mensaje =
                            $"No existe una ruta registrada entre " +
                            $"{origen} y {destino}."
                    });
                }

                if (ruta.Kilometros <= 0)
                {
                    return Json(new
                    {
                        success = false,
                        mensaje =
                            "La distancia registrada para esta ruta no es válida."
                    });
                }

                var configuracion =
                    await _context.ConfiguracionesSistema
                        .AsNoTracking()
                        .FirstOrDefaultAsync();

                if (configuracion == null)
                {
                    return Json(new
                    {
                        success = false,
                        mensaje =
                            "No existe una configuración del sistema."
                    });
                }

                if (configuracion.TarifaKilometro <= 0)
                {
                    return Json(new
                    {
                        success = false,
                        mensaje =
                            "La tarifa por kilómetro configurada no es válida."
                    });
                }

                decimal distancia =
                    ruta.Kilometros;

                decimal tarifa =
                    configuracion.TarifaKilometro;

                decimal monto =
                    Math.Round(
                        distancia * tarifa,
                        2,
                        MidpointRounding.AwayFromZero);

                return Json(new
                {
                    success = true,
                    origen = origen,
                    destino = destino,
                    distanciaKm = distancia,
                    tarifaKilometro = tarifa,
                    montoAsignado = monto
                });
            }

            // =========================================================
            // EMPLEADO: REGISTRAR RECORRIDO
            // =========================================================
            [HttpPost]
            [ValidateAntiForgeryToken]
            public async Task<IActionResult> Create(
                BitacoraVehiculo modelo)
            {
                var idUsuario =
                    HttpContext.Session.GetInt32("IdUsuario");

                if (idUsuario == null)
                {
                    TempData["error"] =
                        "La sesión ha expirado.";

                    return RedirectToAction(
                        "Index",
                        "Home");
                }

                var rendicion =
                    await _context.Rendiciones
                        .FirstOrDefaultAsync(r =>
                            r.IdRendicion == modelo.IdRendicion &&
                            r.IdUsuario == idUsuario.Value);

                if (rendicion == null)
                {
                    TempData["error"] =
                        "No se encontró la rendición.";

                    return RedirectToAction(
                        "Index",
                        "Rendicion");
                }

                // =====================================================
                // PERMITIR ESTADO 1 Y ESTADO 4
                // =====================================================
                if (rendicion.IdEstadoRendicion != 1 &&
                    rendicion.IdEstadoRendicion != 4)
                {
                    TempData["error"] =
                        "La rendición no permite registrar recorridos en este momento.";

                    return RedirectToAction(
                        nameof(Index),
                        new
                        {
                            idRendicion =
                                modelo.IdRendicion
                        });
                }

                // =====================================================
                // LIMPIAR DATOS
                // =====================================================
                modelo.Origen =
                    modelo.Origen?.Trim()
                    ?? string.Empty;

                modelo.Destino =
                    modelo.Destino?.Trim()
                    ?? string.Empty;

                modelo.Observaciones =
                    modelo.Observaciones?.Trim();

                // =====================================================
                // VALIDAR FECHA
                // =====================================================
                if (modelo.Fecha == default)
                {
                    ModelState.AddModelError(
                        nameof(modelo.Fecha),
                        "Debe ingresar la fecha.");
                }
                else if (
                    modelo.Fecha.Date <
                        rendicion.FechaInicio.Date ||
                    modelo.Fecha.Date >
                        rendicion.FechaFin.Date)
                {
                    ModelState.AddModelError(
                        nameof(modelo.Fecha),
                        $"La fecha debe estar entre " +
                        $"{rendicion.FechaInicio:dd/MM/yyyy} y " +
                        $"{rendicion.FechaFin:dd/MM/yyyy}.");
                }

                // =====================================================
                // VALIDAR ORIGEN
                // =====================================================
                if (string.IsNullOrWhiteSpace(
                    modelo.Origen))
                {
                    ModelState.AddModelError(
                        nameof(modelo.Origen),
                        "Debe seleccionar el origen.");
                }

                // =====================================================
                // VALIDAR DESTINO
                // =====================================================
                if (string.IsNullOrWhiteSpace(
                    modelo.Destino))
                {
                    ModelState.AddModelError(
                        nameof(modelo.Destino),
                        "Debe seleccionar el destino.");
                }

                // =====================================================
                // NO VALIDAR CAMPOS CALCULADOS
                // =====================================================
                ModelState.Remove(
                    nameof(modelo.DistanciaKm));

                ModelState.Remove(
                    nameof(modelo.TarifaKilometro));

                ModelState.Remove(
                    nameof(modelo.MontoAsignado));

                ModelState.Remove(
                    nameof(modelo.Rendicion));

                // =====================================================
                // VALIDAR MODELO
                // =====================================================
                if (!ModelState.IsValid)
                {
                    var errores =
                        ModelState
                            .Where(x =>
                                x.Value != null &&
                                x.Value.Errors.Count > 0)
                            .SelectMany(x =>
                                x.Value!.Errors)
                            .Select(e =>
                                string.IsNullOrWhiteSpace(
                                    e.ErrorMessage)
                                    ? "Valor no válido."
                                    : e.ErrorMessage)
                            .Distinct()
                            .ToList();

                    TempData["error"] =
                        string.Join(
                            "<br>",
                            errores);

                    return RedirectToAction(
                        nameof(Index),
                        new
                        {
                            idRendicion =
                                modelo.IdRendicion
                        });
                }

                // =====================================================
                // VALIDAR ORIGEN Y DESTINO
                // =====================================================
                if (string.Equals(
                        modelo.Origen,
                        modelo.Destino,
                        StringComparison.OrdinalIgnoreCase))
                {
                    TempData["error"] =
                        "El origen y el destino no pueden ser iguales.";

                    return RedirectToAction(
                        nameof(Index),
                        new
                        {
                            idRendicion =
                                modelo.IdRendicion
                        });
                }

                // =====================================================
                // BUSCAR RUTA REAL
                // =====================================================
                var ruta =
                    await _context.Rutas
                        .AsNoTracking()
                        .FirstOrDefaultAsync(r =>
                            r.Estado &&
                            (
                                (
                                    r.Origen == modelo.Origen &&
                                    r.Destino == modelo.Destino
                                )
                                ||
                                (
                                    r.Origen == modelo.Destino &&
                                    r.Destino == modelo.Origen
                                )
                            ));

                if (ruta == null)
                {
                    TempData["error"] =
                        $"No existe una ruta registrada entre " +
                        $"{modelo.Origen} y {modelo.Destino}.";

                    return RedirectToAction(
                        nameof(Index),
                        new
                        {
                            idRendicion =
                                modelo.IdRendicion
                        });
                }

                // =====================================================
                // VALIDAR DISTANCIA
                // =====================================================
                if (ruta.Kilometros <= 0)
                {
                    TempData["error"] =
                        "La distancia configurada para la ruta no es válida.";

                    return RedirectToAction(
                        nameof(Index),
                        new
                        {
                            idRendicion =
                                modelo.IdRendicion
                        });
                }

                // =====================================================
                // OBTENER CONFIGURACIÓN
                // =====================================================
                var configuracion =
                    await _context.ConfiguracionesSistema
                        .AsNoTracking()
                        .FirstOrDefaultAsync();

                if (configuracion == null)
                {
                    TempData["error"] =
                        "No existe una configuración del sistema.";

                    return RedirectToAction(
                        nameof(Index),
                        new
                        {
                            idRendicion =
                                modelo.IdRendicion
                        });
                }

                if (configuracion.TarifaKilometro <= 0)
                {
                    TempData["error"] =
                        "La tarifa por kilómetro configurada no es válida.";

                    return RedirectToAction(
                        nameof(Index),
                        new
                        {
                            idRendicion =
                                modelo.IdRendicion
                        });
                }

                // =====================================================
                // CALCULAR DATOS DESDE BD
                // =====================================================
                modelo.DistanciaKm =
                    ruta.Kilometros;

                modelo.TarifaKilometro =
                    configuracion.TarifaKilometro;

                modelo.MontoAsignado =
                    Math.Round(
                        modelo.DistanciaKm *
                        modelo.TarifaKilometro,
                        2,
                        MidpointRounding.AwayFromZero);

                modelo.IdRendicion =
                    rendicion.IdRendicion;

                // =====================================================
                // GUARDAR
                // =====================================================
                _context.BitacorasVehiculo.Add(
                    modelo);

                await _context.SaveChangesAsync();

                // =====================================================
                // ACTUALIZAR TOTALES
                // =====================================================
                await ActualizarTotalesConBitacora(
                    modelo.IdRendicion);

                TempData["mensaje"] =
                    $"Recorrido registrado correctamente. " +
                    $"Distancia: {modelo.DistanciaKm:N2} km. " +
                    $"Monto calculado: S/ {modelo.MontoAsignado:N2}";

                return RedirectToAction(
                    nameof(Index),
                    new
                    {
                        idRendicion =
                            modelo.IdRendicion
                    });
            }

            // =========================================================
            // EMPLEADO: EDITAR RECORRIDO - MOSTRAR FORMULARIO
            // =========================================================
            [HttpGet]
            public async Task<IActionResult> Edit(
                int id,
                int idRendicion)
            {
                var idUsuario =
                    HttpContext.Session.GetInt32("IdUsuario");

                if (idUsuario == null)
                {
                    TempData["error"] =
                        "La sesión ha expirado.";

                    return RedirectToAction(
                        "Index",
                        "Home");
                }

                var rendicion =
                    await _context.Rendiciones
                        .Include(r => r.Solicitud)
                        .FirstOrDefaultAsync(r =>
                            r.IdRendicion == idRendicion &&
                            r.IdUsuario == idUsuario.Value);

                if (rendicion == null)
                {
                    TempData["error"] =
                        "No se encontró la rendición.";

                    return RedirectToAction(
                        "Index",
                        "Rendicion");
                }

                if (rendicion.IdEstadoRendicion != 1 &&
                    rendicion.IdEstadoRendicion != 4)
                {
                    TempData["error"] =
                        "La rendición no permite editar recorridos en este momento.";

                    return RedirectToAction(
                        nameof(Index),
                        new
                        {
                            idRendicion
                        });
                }

                var bitacora =
                    await _context.BitacorasVehiculo
                        .FirstOrDefaultAsync(b =>
                            b.IdBitacoraVehiculo == id &&
                            b.IdRendicion == idRendicion);

                if (bitacora == null)
                {
                    TempData["error"] =
                        "No se encontró el recorrido.";

                    return RedirectToAction(
                        nameof(Index),
                        new
                        {
                            idRendicion
                        });
                }

                var configuracion =
                    await _context.ConfiguracionesSistema
                        .AsNoTracking()
                        .FirstOrDefaultAsync();

                var puntosOrigen =
                    await _context.Rutas
                        .AsNoTracking()
                        .Where(r =>
                            r.Estado)
                        .Select(r =>
                            r.Origen)
                        .Distinct()
                        .ToListAsync();

                var puntosDestino =
                    await _context.Rutas
                        .AsNoTracking()
                        .Where(r =>
                            r.Estado)
                        .Select(r =>
                            r.Destino)
                        .Distinct()
                        .ToListAsync();

                var puntos =
                    puntosOrigen
                        .Concat(puntosDestino)
                        .Where(p =>
                            !string.IsNullOrWhiteSpace(p))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .OrderBy(p => p)
                        .ToList();

                ViewBag.Rendicion =
                    rendicion;

                ViewBag.TarifaKilometro =
                    configuracion?.TarifaKilometro ?? 0m;

                ViewBag.Puntos =
                    puntos;

                return View(bitacora);
            }

            // =========================================================
            // EMPLEADO: EDITAR RECORRIDO - GUARDAR
            // =========================================================
            [HttpPost]
            [ValidateAntiForgeryToken]
            public async Task<IActionResult> Edit(
                int id,
                BitacoraVehiculo modelo)
            {
                var idUsuario =
                    HttpContext.Session.GetInt32("IdUsuario");

                if (idUsuario == null)
                {
                    TempData["error"] =
                        "La sesión ha expirado.";

                    return RedirectToAction(
                        "Index",
                        "Home");
                }

                var rendicion =
                    await _context.Rendiciones
                        .FirstOrDefaultAsync(r =>
                            r.IdRendicion == modelo.IdRendicion &&
                            r.IdUsuario == idUsuario.Value);

                if (rendicion == null)
                {
                    TempData["error"] =
                        "No se encontró la rendición.";

                    return RedirectToAction(
                        "Index",
                        "Rendicion");
                }

                if (rendicion.IdEstadoRendicion != 1 &&
                    rendicion.IdEstadoRendicion != 4)
                {
                    TempData["error"] =
                        "La rendición no permite editar recorridos en este momento.";

                    return RedirectToAction(
                        nameof(Index),
                        new
                        {
                            idRendicion =
                                modelo.IdRendicion
                        });
                }

                var bitacora =
                    await _context.BitacorasVehiculo
                        .FirstOrDefaultAsync(b =>
                            b.IdBitacoraVehiculo == id &&
                            b.IdRendicion == modelo.IdRendicion);

                if (bitacora == null)
                {
                    TempData["error"] =
                        "No se encontró el recorrido.";

                    return RedirectToAction(
                        nameof(Index),
                        new
                        {
                            idRendicion =
                                modelo.IdRendicion
                        });
                }

                modelo.Origen =
                    modelo.Origen?.Trim()
                    ?? string.Empty;

                modelo.Destino =
                    modelo.Destino?.Trim()
                    ?? string.Empty;

                modelo.Observaciones =
                    modelo.Observaciones?.Trim();

                if (modelo.Fecha == default)
                {
                    ModelState.AddModelError(
                        nameof(modelo.Fecha),
                        "Debe ingresar la fecha.");
                }
                else if (
                    modelo.Fecha.Date <
                        rendicion.FechaInicio.Date ||
                    modelo.Fecha.Date >
                        rendicion.FechaFin.Date)
                {
                    ModelState.AddModelError(
                        nameof(modelo.Fecha),
                        $"La fecha debe estar entre " +
                        $"{rendicion.FechaInicio:dd/MM/yyyy} y " +
                        $"{rendicion.FechaFin:dd/MM/yyyy}.");
                }

                if (string.IsNullOrWhiteSpace(
                    modelo.Origen))
                {
                    ModelState.AddModelError(
                        nameof(modelo.Origen),
                        "Debe seleccionar el origen.");
                }

                if (string.IsNullOrWhiteSpace(
                    modelo.Destino))
                {
                    ModelState.AddModelError(
                        nameof(modelo.Destino),
                        "Debe seleccionar el destino.");
                }

                ModelState.Remove(
                    nameof(modelo.DistanciaKm));

                ModelState.Remove(
                    nameof(modelo.TarifaKilometro));

                ModelState.Remove(
                    nameof(modelo.MontoAsignado));

                ModelState.Remove(
                    nameof(modelo.Rendicion));

                if (!ModelState.IsValid)
                {
                    var errores =
                        ModelState
                            .Where(x =>
                                x.Value != null &&
                                x.Value.Errors.Count > 0)
                            .SelectMany(x =>
                                x.Value!.Errors)
                            .Select(e =>
                                string.IsNullOrWhiteSpace(
                                    e.ErrorMessage)
                                    ? "Valor no válido."
                                    : e.ErrorMessage)
                            .Distinct()
                            .ToList();

                    TempData["error"] =
                        string.Join(
                            "<br>",
                            errores);

                    return RedirectToAction(
                        nameof(Edit),
                        new
                        {
                            id,
                            idRendicion =
                                modelo.IdRendicion
                        });
                }

                if (string.Equals(
                        modelo.Origen,
                        modelo.Destino,
                        StringComparison.OrdinalIgnoreCase))
                {
                    TempData["error"] =
                        "El origen y el destino no pueden ser iguales.";

                    return RedirectToAction(
                        nameof(Edit),
                        new
                        {
                            id,
                            idRendicion =
                                modelo.IdRendicion
                        });
                }

                var ruta =
                    await _context.Rutas
                        .AsNoTracking()
                        .FirstOrDefaultAsync(r =>
                            r.Estado &&
                            (
                                (
                                    r.Origen == modelo.Origen &&
                                    r.Destino == modelo.Destino
                                )
                                ||
                                (
                                    r.Origen == modelo.Destino &&
                                    r.Destino == modelo.Origen
                                )
                            ));

                if (ruta == null)
                {
                    TempData["error"] =
                        $"No existe una ruta registrada entre " +
                        $"{modelo.Origen} y {modelo.Destino}.";

                    return RedirectToAction(
                        nameof(Edit),
                        new
                        {
                            id,
                            idRendicion =
                                modelo.IdRendicion
                        });
                }

                if (ruta.Kilometros <= 0)
                {
                    TempData["error"] =
                        "La distancia configurada para la ruta no es válida.";

                    return RedirectToAction(
                        nameof(Edit),
                        new
                        {
                            id,
                            idRendicion =
                                modelo.IdRendicion
                        });
                }

                var configuracion =
                    await _context.ConfiguracionesSistema
                        .AsNoTracking()
                        .FirstOrDefaultAsync();

                if (configuracion == null)
                {
                    TempData["error"] =
                        "No existe una configuración del sistema.";

                    return RedirectToAction(
                        nameof(Edit),
                        new
                        {
                            id,
                            idRendicion =
                                modelo.IdRendicion
                        });
                }

                if (configuracion.TarifaKilometro <= 0)
                {
                    TempData["error"] =
                        "La tarifa por kilómetro configurada no es válida.";

                    return RedirectToAction(
                        nameof(Edit),
                        new
                        {
                            id,
                            idRendicion =
                                modelo.IdRendicion
                        });
                }

                bitacora.Fecha =
                    modelo.Fecha;

                bitacora.Origen =
                    modelo.Origen;

                bitacora.Destino =
                    modelo.Destino;

                bitacora.Observaciones =
                    modelo.Observaciones;

                bitacora.DistanciaKm =
                    ruta.Kilometros;

                bitacora.TarifaKilometro =
                    configuracion.TarifaKilometro;

                bitacora.MontoAsignado =
                    Math.Round(
                        bitacora.DistanciaKm *
                        bitacora.TarifaKilometro,
                        2,
                        MidpointRounding.AwayFromZero);

                await _context.SaveChangesAsync();

                await ActualizarTotalesConBitacora(
                    modelo.IdRendicion);

                TempData["mensaje"] =
                    "El recorrido fue actualizado correctamente.";

                return RedirectToAction(
                    nameof(Index),
                    new
                    {
                        idRendicion =
                            modelo.IdRendicion
                    });
            }

            // =========================================================
            // ADMINISTRADOR: EDITAR RECORRIDO - MOSTRAR FORMULARIO
            // =========================================================
            [HttpGet]
            public async Task<IActionResult> EditAdmin(int id)
            {
                var bitacora =
                    await _context.BitacorasVehiculo
                        .Include(b => b.Rendicion)
                            .ThenInclude(r => r!.Solicitud)
                        .FirstOrDefaultAsync(b =>
                            b.IdBitacoraVehiculo == id);

                if (bitacora == null)
                {
                    TempData["error"] =
                        "No se encontró el recorrido.";

                    return RedirectToAction(
                        "IndexAdmin",
                        "Rendicion");
                }

                if (bitacora.Rendicion == null)
                {
                    TempData["error"] =
                        "La bitácora no tiene una rendición asociada.";

                    return RedirectToAction(
                        "IndexAdmin",
                        "Rendicion");
                }

                // =====================================================
                // SOLO ESTADO 2 Y 4
                // 2 = Pendiente de revisión
                // 4 = Rechazada
                // =====================================================
                if (bitacora.Rendicion.IdEstadoRendicion != 2 &&
                    bitacora.Rendicion.IdEstadoRendicion != 4)
                {
                    TempData["error"] =
                        "Esta bitácora no puede ser editada en el estado actual de la rendición.";

                    return RedirectToAction(
                        "DetalleAdmin",
                        "Rendicion",
                        new
                        {
                            id =
                                bitacora.IdRendicion
                        });
                }

                // =====================================================
                // OBTENER PUNTOS DE RUTA
                // =====================================================
                var puntosOrigen =
                    await _context.Rutas
                        .AsNoTracking()
                        .Where(r =>
                            r.Estado)
                        .Select(r =>
                            r.Origen)
                        .Distinct()
                        .ToListAsync();

                var puntosDestino =
                    await _context.Rutas
                        .AsNoTracking()
                        .Where(r =>
                            r.Estado)
                        .Select(r =>
                            r.Destino)
                        .Distinct()
                        .ToListAsync();

                var puntos =
                    puntosOrigen
                        .Concat(puntosDestino)
                        .Where(p =>
                            !string.IsNullOrWhiteSpace(p))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .OrderBy(p => p)
                        .ToList();

                ViewBag.Puntos =
                    puntos;

                ViewBag.Rendicion =
                    bitacora.Rendicion;

                return View(bitacora);
            }

            // =========================================================
            // ADMINISTRADOR: EDITAR RECORRIDO - GUARDAR
            // =========================================================
            [HttpPost]
            [ValidateAntiForgeryToken]
            public async Task<IActionResult> EditAdmin(
                BitacoraVehiculo modelo)
            {
                var bitacora =
                    await _context.BitacorasVehiculo
                        .Include(b => b.Rendicion)
                        .FirstOrDefaultAsync(b =>
                            b.IdBitacoraVehiculo ==
                            modelo.IdBitacoraVehiculo);

                if (bitacora == null)
                {
                    TempData["error"] =
                        "No se encontró el recorrido.";

                    return RedirectToAction(
                        "IndexAdmin",
                        "Rendicion");
                }

                if (bitacora.Rendicion == null)
                {
                    TempData["error"] =
                        "La bitácora no tiene una rendición asociada.";

                    return RedirectToAction(
                        "IndexAdmin",
                        "Rendicion");
                }

                // =====================================================
                // VALIDAR ESTADO
                // =====================================================
                if (bitacora.Rendicion.IdEstadoRendicion != 2 &&
                    bitacora.Rendicion.IdEstadoRendicion != 4)
                {
                    TempData["error"] =
                        "Esta bitácora no puede ser editada en el estado actual de la rendición.";

                    return RedirectToAction(
                        "DetalleAdmin",
                        "Rendicion",
                        new
                        {
                            id =
                                bitacora.IdRendicion
                        });
                }

                // =====================================================
                // LIMPIAR DATOS
                // =====================================================
                modelo.Origen =
                    modelo.Origen?.Trim()
                    ?? string.Empty;

                modelo.Destino =
                    modelo.Destino?.Trim()
                    ?? string.Empty;

                modelo.Observaciones =
                    modelo.Observaciones?.Trim();

                // =====================================================
                // VALIDAR FECHA
                // =====================================================
                if (modelo.Fecha == default)
                {
                    ModelState.AddModelError(
                        nameof(modelo.Fecha),
                        "Debe ingresar la fecha.");
                }
                else if (
                    modelo.Fecha.Date <
                        bitacora.Rendicion.FechaInicio.Date ||
                    modelo.Fecha.Date >
                        bitacora.Rendicion.FechaFin.Date)
                {
                    ModelState.AddModelError(
                        nameof(modelo.Fecha),
                        $"La fecha debe estar entre " +
                        $"{bitacora.Rendicion.FechaInicio:dd/MM/yyyy} y " +
                        $"{bitacora.Rendicion.FechaFin:dd/MM/yyyy}.");
                }

                // =====================================================
                // VALIDAR ORIGEN
                // =====================================================
                if (string.IsNullOrWhiteSpace(
                    modelo.Origen))
                {
                    ModelState.AddModelError(
                        nameof(modelo.Origen),
                        "Debe seleccionar el origen.");
                }

                // =====================================================
                // VALIDAR DESTINO
                // =====================================================
                if (string.IsNullOrWhiteSpace(
                    modelo.Destino))
                {
                    ModelState.AddModelError(
                        nameof(modelo.Destino),
                        "Debe seleccionar el destino.");
                }

                // =====================================================
                // NO VALIDAR CAMPOS CALCULADOS
                // =====================================================
                ModelState.Remove(
                    nameof(modelo.DistanciaKm));

                ModelState.Remove(
                    nameof(modelo.TarifaKilometro));

                ModelState.Remove(
                    nameof(modelo.MontoAsignado));

                ModelState.Remove(
                    nameof(modelo.Rendicion));

                // =====================================================
                // VALIDAR MODELO
                // =====================================================
                if (!ModelState.IsValid)
                {
                    var errores =
                        ModelState
                            .Where(x =>
                                x.Value != null &&
                                x.Value.Errors.Count > 0)
                            .SelectMany(x =>
                                x.Value!.Errors)
                            .Select(e =>
                                string.IsNullOrWhiteSpace(
                                    e.ErrorMessage)
                                    ? "Valor no válido."
                                    : e.ErrorMessage)
                            .Distinct()
                            .ToList();

                    TempData["error"] =
                        string.Join(
                            "<br>",
                            errores);

                    return RedirectToAction(
                        nameof(EditAdmin),
                        new
                        {
                            id =
                                modelo.IdBitacoraVehiculo
                        });
                }

                // =====================================================
                // VALIDAR ORIGEN Y DESTINO
                // =====================================================
                if (string.Equals(
                        modelo.Origen,
                        modelo.Destino,
                        StringComparison.OrdinalIgnoreCase))
                {
                    TempData["error"] =
                        "El origen y el destino no pueden ser iguales.";

                    return RedirectToAction(
                        nameof(EditAdmin),
                        new
                        {
                            id =
                                modelo.IdBitacoraVehiculo
                        });
                }

                // =====================================================
                // BUSCAR RUTA REAL
                // =====================================================
                var ruta =
                    await _context.Rutas
                        .AsNoTracking()
                        .FirstOrDefaultAsync(r =>
                            r.Estado &&
                            (
                                (
                                    r.Origen == modelo.Origen &&
                                    r.Destino == modelo.Destino
                                )
                                ||
                                (
                                    r.Origen == modelo.Destino &&
                                    r.Destino == modelo.Origen
                                )
                            ));

                if (ruta == null)
                {
                    TempData["error"] =
                        $"No existe una ruta registrada entre " +
                        $"{modelo.Origen} y {modelo.Destino}.";

                    return RedirectToAction(
                        nameof(EditAdmin),
                        new
                        {
                            id =
                                modelo.IdBitacoraVehiculo
                        });
                }

                // =====================================================
                // VALIDAR DISTANCIA
                // =====================================================
                if (ruta.Kilometros <= 0)
                {
                    TempData["error"] =
                        "La distancia configurada para la ruta no es válida.";

                    return RedirectToAction(
                        nameof(EditAdmin),
                        new
                        {
                            id =
                                modelo.IdBitacoraVehiculo
                        });
                }

                // =====================================================
                // OBTENER CONFIGURACIÓN
                // =====================================================
                var configuracion =
                    await _context.ConfiguracionesSistema
                        .AsNoTracking()
                        .FirstOrDefaultAsync();

                if (configuracion == null)
                {
                    TempData["error"] =
                        "No existe una configuración del sistema.";

                    return RedirectToAction(
                        nameof(EditAdmin),
                        new
                        {
                            id =
                                modelo.IdBitacoraVehiculo
                        });
                }

                if (configuracion.TarifaKilometro <= 0)
                {
                    TempData["error"] =
                        "La tarifa por kilómetro configurada no es válida.";

                    return RedirectToAction(
                        nameof(EditAdmin),
                        new
                        {
                            id =
                                modelo.IdBitacoraVehiculo
                        });
                }

                // =====================================================
                // ACTUALIZAR LA MISMA BITÁCORA
                // =====================================================
                bitacora.Fecha =
                    modelo.Fecha;

                bitacora.Origen =
                    modelo.Origen;

                bitacora.Destino =
                    modelo.Destino;

                bitacora.Observaciones =
                    modelo.Observaciones;

                bitacora.DistanciaKm =
                    ruta.Kilometros;

                bitacora.TarifaKilometro =
                    configuracion.TarifaKilometro;

                bitacora.MontoAsignado =
                    Math.Round(
                        bitacora.DistanciaKm *
                        bitacora.TarifaKilometro,
                        2,
                        MidpointRounding.AwayFromZero);

                // =====================================================
                // GUARDAR CAMBIOS
                // =====================================================
                await _context.SaveChangesAsync();

                // =====================================================
                // ACTUALIZAR TOTALES
                // =====================================================
                await ActualizarTotalesConBitacora(
                    bitacora.IdRendicion);

                // =====================================================
                // REGENERAR PDF
                // =====================================================
                await RegenerarPdfRendicion(
                    bitacora.IdRendicion);

                TempData["mensaje"] =
                    "El recorrido fue actualizado correctamente.";

                // =====================================================
                // VOLVER AL DETALLE ADMINISTRATIVO
                // =====================================================
                return RedirectToAction(
                    "DetalleAdmin",
                    "Rendicion",
                    new
                    {
                        id =
                            bitacora.IdRendicion
                    });
            }

            // =========================================================
            // EMPLEADO: ELIMINAR RECORRIDO
            // =========================================================
            [HttpPost]
            [ValidateAntiForgeryToken]
            public async Task<IActionResult> Delete(
                int id,
                int idRendicion)
            {
                var idUsuario =
                    HttpContext.Session.GetInt32("IdUsuario");

                if (idUsuario == null)
                {
                    TempData["error"] =
                        "La sesión ha expirado.";

                    return RedirectToAction(
                        "Index",
                        "Home");
                }

                var rendicion =
                    await _context.Rendiciones
                        .FirstOrDefaultAsync(r =>
                            r.IdRendicion == idRendicion &&
                            r.IdUsuario == idUsuario.Value);

                if (rendicion == null)
                {
                    TempData["error"] =
                        "No se encontró la rendición.";

                    return RedirectToAction(
                        "Index",
                        "Rendicion");
                }

                if (rendicion.IdEstadoRendicion != 1 &&
                    rendicion.IdEstadoRendicion != 4)
                {
                    TempData["error"] =
                        "La rendición no permite eliminar recorridos en este momento.";

                    return RedirectToAction(
                        nameof(Index),
                        new
                        {
                            idRendicion
                        });
                }

                var bitacora =
                    await _context.BitacorasVehiculo
                        .FirstOrDefaultAsync(b =>
                            b.IdBitacoraVehiculo == id &&
                            b.IdRendicion == idRendicion);

                if (bitacora == null)
                {
                    TempData["error"] =
                        "No se encontró el recorrido.";

                    return RedirectToAction(
                        nameof(Index),
                        new
                        {
                            idRendicion
                        });
                }

                _context.BitacorasVehiculo.Remove(
                    bitacora);

                await _context.SaveChangesAsync();

                await ActualizarTotalesConBitacora(
                    idRendicion);

                TempData["mensaje"] =
                    "Recorrido eliminado correctamente.";

                return RedirectToAction(
                    nameof(Index),
                    new
                    {
                        idRendicion
                    });
            }

            // =========================================================
            // RECALCULAR TOTAL DE LA RENDICIÓN
            // =========================================================
            private async Task ActualizarTotalesConBitacora(
                int idRendicion)
            {
                var rendicion =
                    await _context.Rendiciones
                        .Include(r => r.Solicitud)
                        .FirstOrDefaultAsync(r =>
                            r.IdRendicion == idRendicion);

                if (rendicion == null ||
                    rendicion.Solicitud == null)
                {
                    return;
                }

                var totalGastos =
                    await _context.Gastos
                        .Where(g =>
                            g.IdRendicion == idRendicion)
                        .SumAsync(g =>
                            (decimal?)g.MontoTotal)
                    ?? 0m;

                var totalVehiculo =
                    await _context.BitacorasVehiculo
                        .Where(b =>
                            b.IdRendicion == idRendicion)
                        .SumAsync(b =>
                            (decimal?)b.MontoAsignado)
                    ?? 0m;

                rendicion.Total =
                    totalGastos +
                    totalVehiculo;

                rendicion.Saldo =
                    rendicion.Solicitud.Monto -
                    rendicion.Total;

                await _context.SaveChangesAsync();
            }

            // =========================================================
            // REGENERAR PDF DE LA RENDICIÓN
            // =========================================================
            private async Task RegenerarPdfRendicion(
                int idRendicion)
            {
                var rendicion =
                    await _context.Rendiciones
                        .Include(r => r.Solicitud)
                        .Include(r => r.Usuario)
                        .FirstOrDefaultAsync(r =>
                            r.IdRendicion == idRendicion);

                if (rendicion == null)
                {
                    return;
                }

                var gastos =
                    await _context.Gastos
                        .Include(g => g.TipoGasto)
                        .Include(g => g.TipoComprobante)
                        .Where(g =>
                            g.IdRendicion == idRendicion)
                        .OrderBy(g => g.Fecha)
                        .ToListAsync();

                var bitacoras =
                    await _context.BitacorasVehiculo
                        .Where(b =>
                            b.IdRendicion == idRendicion)
                        .OrderBy(b => b.Fecha)
                        .ToListAsync();

                var devolucion =
                    await _context.DevolucionesSaldo
                        .FirstOrDefaultAsync(d =>
                            d.IdRendicion == idRendicion);

                var resultadoPdf =
                    await _rendicionPdfService
                        .GenerarAsync(
                            rendicion,
                            gastos,
                            devolucion,
                            bitacoras);

                rendicion.ArchivoPdf =
                    $"{resultadoPdf.RutaPublica}" +
                    $"?v={DateTime.Now.Ticks}";

                await _context.SaveChangesAsync();
            }
        }
    }
