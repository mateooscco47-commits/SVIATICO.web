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
                // =====================================================
                // VALIDAR SESIÓN
                // =====================================================
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


                // =====================================================
                // VALIDAR RENDICIÓN
                // =====================================================
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


                // =====================================================
                // OBTENER BITÁCORAS
                // =====================================================
                var bitacoras =
                    await _context.BitacorasVehiculo
                        .AsNoTracking()
                        .Where(b =>
                            b.IdRendicion == idRendicion)
                        .OrderBy(b => b.Fecha)
                        .ThenBy(b => b.IdBitacoraVehiculo)
                        .ToListAsync();


                // =====================================================
                // OBTENER CONFIGURACIÓN DEL SISTEMA
                // =====================================================
                var configuracion =
                    await _context.ConfiguracionesSistema
                        .AsNoTracking()
                        .FirstOrDefaultAsync();


                // =====================================================
                // OBTENER PUNTOS DESDE LA BD
                //
                // Ya NO existen rutas escritas en el controlador.
                // Los puntos salen de la tabla Rutas.
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


                // =====================================================
                // UNIR ORÍGENES Y DESTINOS
                // =====================================================
                var puntos =
                    puntosOrigen
                        .Concat(puntosDestino)
                        .Where(p =>
                            !string.IsNullOrWhiteSpace(p))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .OrderBy(p => p)
                        .ToList();


                // =====================================================
                // ENVIAR DATOS A LA VISTA
                // =====================================================
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
            //
            // Este método es llamado por JavaScript cuando el usuario
            // selecciona origen y destino.
            //
            // EJEMPLO:
            //
            // Huancayo + Tarma
            //          ↓
            //      Tabla Rutas
            //          ↓
            //      108 kilómetros
            //
            // =========================================================
            [HttpGet]
            public async Task<IActionResult> ObtenerRuta(
                string origen,
                string destino)
            {
                // =====================================================
                // VALIDAR DATOS
                // =====================================================
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


                // =====================================================
                // VALIDAR ORIGEN Y DESTINO IGUALES
                // =====================================================
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


                // =====================================================
                // BUSCAR RUTA EN LA BD
                // =====================================================
                //
                // Se permite:
                //
                // Huancayo → Tarma
                //
                // y también:
                //
                // Tarma → Huancayo
                //
                // usando la misma distancia.
                //
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


                // =====================================================
                // RUTA NO ENCONTRADA
                // =====================================================
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


                // =====================================================
                // VALIDAR DISTANCIA
                // =====================================================
                if (ruta.Kilometros <= 0)
                {
                    return Json(new
                    {
                        success = false,
                        mensaje =
                            "La distancia registrada para esta ruta no es válida."
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
                    return Json(new
                    {
                        success = false,
                        mensaje =
                            "No existe una configuración del sistema."
                    });
                }


                // =====================================================
                // VALIDAR TARIFA
                // =====================================================
                if (configuracion.TarifaKilometro <= 0)
                {
                    return Json(new
                    {
                        success = false,
                        mensaje =
                            "La tarifa por kilómetro configurada no es válida."
                    });
                }


                // =====================================================
                // CALCULAR MONTO
                // =====================================================
                decimal distancia =
                    ruta.Kilometros;

                decimal tarifa =
                    configuracion.TarifaKilometro;

                decimal monto =
                    Math.Round(
                        distancia * tarifa,
                        2,
                        MidpointRounding.AwayFromZero);


                // =====================================================
                // DEVOLVER INFORMACIÓN
                // =====================================================
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
                // =====================================================
                // VALIDAR SESIÓN
                // =====================================================
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


                // =====================================================
                // VALIDAR RENDICIÓN
                // =====================================================
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
                // SOLO PERMITIR RENDICIÓN EN PROCESO
                // =====================================================
                if (rendicion.IdEstadoRendicion != 1)
                {
                    TempData["error"] =
                        "La rendición ya fue enviada y no permite registrar recorridos.";

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
                // NO VALIDAR LOS CAMPOS CALCULADOS DEL FORMULARIO
                // =====================================================
                //
                // Estos valores serán obtenidos nuevamente desde
                // la BD. Nunca debemos confiar en los valores
                // enviados por JavaScript.
                //
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
                // VALIDAR QUE ORIGEN Y DESTINO NO SEAN IGUALES
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
                // BUSCAR RUTA REAL EN LA BD
                // =====================================================
                //
                // IMPORTANTE:
                //
                // No utilizamos:
                //
                // modelo.DistanciaKm
                //
                // La distancia se obtiene directamente de Rutas.
                //
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


                // =====================================================
                // RUTA NO EXISTE
                // =====================================================
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
                // VALIDAR DISTANCIA DE LA RUTA
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
                // OBTENER CONFIGURACIÓN DEL SISTEMA
                // =====================================================
                var configuracion =
                    await _context.ConfiguracionesSistema
                        .AsNoTracking()
                        .FirstOrDefaultAsync();


                // =====================================================
                // VALIDAR CONFIGURACIÓN
                // =====================================================
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
                // ASIGNAR DISTANCIA DESDE LA BD
                // =====================================================
                modelo.DistanciaKm =
                    ruta.Kilometros;


                // =====================================================
                // GUARDAR TARIFA UTILIZADA
                // =====================================================
                modelo.TarifaKilometro =
                    configuracion.TarifaKilometro;


                // =====================================================
                // CALCULAR MONTO
                // =====================================================
                modelo.MontoAsignado =
                    Math.Round(
                        modelo.DistanciaKm *
                        modelo.TarifaKilometro,
                        2,
                        MidpointRounding.AwayFromZero);


                // =====================================================
                // ASEGURAR RENDICIÓN
                // =====================================================
                modelo.IdRendicion =
                    rendicion.IdRendicion;


                // =====================================================
                // REGISTRAR BITÁCORA
                // =====================================================
                _context.BitacorasVehiculo.Add(
                    modelo);

                await _context.SaveChangesAsync();


                // =====================================================
                // ACTUALIZAR TOTALES
                // =====================================================
                await ActualizarTotalesConBitacora(
                    modelo.IdRendicion);


                // =====================================================
                // MENSAJE
                // =====================================================
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
            // EMPLEADO: ELIMINAR RECORRIDO
            // =========================================================
            [HttpPost]
            [ValidateAntiForgeryToken]
            public async Task<IActionResult> Delete(
                int id,
                int idRendicion)
            {
                // =====================================================
                // VALIDAR SESIÓN
                // =====================================================
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


                // =====================================================
                // VALIDAR RENDICIÓN
                // =====================================================
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


                // =====================================================
                // VALIDAR ESTADO
                // =====================================================
                if (rendicion.IdEstadoRendicion != 1)
                {
                    TempData["error"] =
                        "No se pueden eliminar recorridos de una rendición enviada.";

                    return RedirectToAction(
                        nameof(Index),
                        new
                        {
                            idRendicion
                        });
                }


                // =====================================================
                // BUSCAR BITÁCORA
                // =====================================================
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


                // =====================================================
                // ELIMINAR
                // =====================================================
                _context.BitacorasVehiculo.Remove(
                    bitacora);

                await _context.SaveChangesAsync();


                // =====================================================
                // RECALCULAR TOTALES
                // =====================================================
                await ActualizarTotalesConBitacora(
                    idRendicion);


                // =====================================================
                // MENSAJE
                // =====================================================
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
                // =====================================================
                // OBTENER RENDICIÓN
                // =====================================================
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


                // =====================================================
                // TOTAL DE GASTOS
                // =====================================================
                var totalGastos =
                    await _context.Gastos
                        .Where(g =>
                            g.IdRendicion == idRendicion)
                        .SumAsync(g =>
                            (decimal?)g.MontoTotal)
                    ?? 0m;


                // =====================================================
                // TOTAL DE VEHÍCULO
                // =====================================================
                var totalVehiculo =
                    await _context.BitacorasVehiculo
                        .Where(b =>
                            b.IdRendicion == idRendicion)
                        .SumAsync(b =>
                            (decimal?)b.MontoAsignado)
                    ?? 0m;


                // =====================================================
                // TOTAL GENERAL
                // =====================================================
                rendicion.Total =
                    totalGastos +
                    totalVehiculo;


                // =====================================================
                // SALDO
                // =====================================================
                rendicion.Saldo =
                    rendicion.Solicitud.Monto -
                    rendicion.Total;


                // =====================================================
                // GUARDAR
                // =====================================================
                await _context.SaveChangesAsync();
            }


            // =========================================================
            // REGENERAR PDF DE LA RENDICIÓN
            // =========================================================
            private async Task RegenerarPdfRendicion(
                int idRendicion)
            {
                // =====================================================
                // OBTENER RENDICIÓN
                // =====================================================
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


                // =====================================================
                // OBTENER GASTOS
                // =====================================================
                var gastos =
                    await _context.Gastos
                        .Include(g => g.TipoGasto)
                        .Include(g => g.TipoComprobante)
                        .Where(g =>
                            g.IdRendicion == idRendicion)
                        .OrderBy(g => g.Fecha)
                        .ToListAsync();


                // =====================================================
                // OBTENER BITÁCORAS
                // =====================================================
                var bitacoras =
                    await _context.BitacorasVehiculo
                        .Where(b =>
                            b.IdRendicion == idRendicion)
                        .OrderBy(b => b.Fecha)
                        .ToListAsync();


                // =====================================================
                // OBTENER DEVOLUCIÓN
                // =====================================================
                var devolucion =
                    await _context.DevolucionesSaldo
                        .FirstOrDefaultAsync(d =>
                            d.IdRendicion == idRendicion);


                // =====================================================
                // GENERAR PDF
                // =====================================================
                var resultadoPdf =
                    await _rendicionPdfService
                        .GenerarAsync(
                            rendicion,
                            gastos,
                            devolucion,
                            bitacoras);


                // =====================================================
                // ACTUALIZAR RUTA DEL PDF
                // =====================================================
                rendicion.ArchivoPdf =
                    $"{resultadoPdf.RutaPublica}" +
                    $"?v={DateTime.Now.Ticks}";


                await _context.SaveChangesAsync();
            }
        }
    }