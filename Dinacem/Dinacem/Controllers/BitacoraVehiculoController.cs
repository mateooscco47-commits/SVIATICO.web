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


        // =========================================
        // EMPLEADO: LISTAR BITÁCORA
        // =========================================
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
                    .Where(b =>
                        b.IdRendicion == idRendicion)
                    .OrderBy(b => b.Fecha)
                    .ToListAsync();

            // =========================================
            // OBTENER TARIFA ACTUAL
            // =========================================
            var configuracion =
                await _context.ConfiguracionesSistema
                    .AsNoTracking()
                    .FirstOrDefaultAsync();

            ViewBag.Rendicion = rendicion;

            ViewBag.TarifaKilometro =
                configuracion?.TarifaKilometro ?? 0m;

            return View(bitacoras);
        }


        // =========================================
        // EMPLEADO: REGISTRAR RECORRIDO
        // =========================================
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

            // =========================================
            // VALIDAR RENDICIÓN
            // =========================================
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

            // Solo mientras esté en proceso
            if (rendicion.IdEstadoRendicion != 1)
            {
                TempData["error"] =
                    "La rendición ya fue enviada y no permite registrar recorridos.";

                return RedirectToAction(
                    nameof(Index),
                    new
                    {
                        idRendicion = modelo.IdRendicion
                    });
            }


            // =========================================
            // LIMPIAR DATOS
            // =========================================
            modelo.Origen =
                modelo.Origen?.Trim()
                ?? string.Empty;

            modelo.Destino =
                modelo.Destino?.Trim()
                ?? string.Empty;

            modelo.Observaciones =
                modelo.Observaciones?.Trim();


            // =========================================
            // VALIDAR FECHA
            // =========================================
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


            // =========================================
            // VALIDAR ORIGEN
            // =========================================
            if (string.IsNullOrWhiteSpace(
                modelo.Origen))
            {
                ModelState.AddModelError(
                    nameof(modelo.Origen),
                    "Debe ingresar el origen.");
            }


            // =========================================
            // VALIDAR DESTINO
            // =========================================
            if (string.IsNullOrWhiteSpace(
                modelo.Destino))
            {
                ModelState.AddModelError(
                    nameof(modelo.Destino),
                    "Debe ingresar el destino.");
            }


            // =========================================
            // VALIDAR DISTANCIA
            // =========================================
            if (modelo.DistanciaKm <= 0)
            {
                ModelState.AddModelError(
                    nameof(modelo.DistanciaKm),
                    "La distancia debe ser mayor que cero.");
            }


            // =========================================
            // ESTOS VALORES NO LOS ENVÍA EL EMPLEADO
            // =========================================
            ModelState.Remove(
                nameof(modelo.MontoAsignado));

            ModelState.Remove(
                nameof(modelo.TarifaKilometro));

            ModelState.Remove(
                nameof(modelo.Rendicion));


            // =========================================
            // VALIDAR MODELO
            // =========================================
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
                                : e.ErrorMessage);

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


            // =========================================
            // OBTENER TARIFA CONFIGURADA
            // =========================================
            var configuracion =
                await _context.ConfiguracionesSistema
                    .AsNoTracking()
                    .FirstOrDefaultAsync();

            if (configuracion == null)
            {
                TempData["error"] =
                    "No existe una tarifa por kilómetro configurada.";

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


            // =========================================
            // GUARDAR TARIFA UTILIZADA
            // =========================================
            modelo.TarifaKilometro =
                configuracion.TarifaKilometro;


            // =========================================
            // CALCULAR MONTO AUTOMÁTICAMENTE
            //
            // Ejemplo:
            // 120 km x S/ 0.40 = S/ 48.00
            // =========================================
            modelo.MontoAsignado =
                Math.Round(
                    modelo.DistanciaKm *
                    modelo.TarifaKilometro,
                    2,
                    MidpointRounding.AwayFromZero);


            // =========================================
            // REGISTRAR BITÁCORA
            // =========================================
            _context.BitacorasVehiculo.Add(
                modelo);

            await _context.SaveChangesAsync();


            // =========================================
            // ACTUALIZAR TOTAL DE LA RENDICIÓN
            // =========================================
            await ActualizarTotalesConBitacora(
                modelo.IdRendicion);


            TempData["mensaje"] =
                $"Recorrido registrado correctamente. " +
                $"Monto calculado: S/ {modelo.MontoAsignado:N2}";


            return RedirectToAction(
                nameof(Index),
                new
                {
                    idRendicion =
                        modelo.IdRendicion
                });
        }


        // =========================================
        // EMPLEADO: ELIMINAR RECORRIDO
        // =========================================
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
                return RedirectToAction(
                    "Index",
                    "Home");
            }


            // =========================================
            // VALIDAR RENDICIÓN
            // =========================================
            var rendicion =
                await _context.Rendiciones
                    .FirstOrDefaultAsync(r =>
                        r.IdRendicion ==
                            idRendicion &&
                        r.IdUsuario ==
                            idUsuario.Value);

            if (rendicion == null)
            {
                TempData["error"] =
                    "No se encontró la rendición.";

                return RedirectToAction(
                    "Index",
                    "Rendicion");
            }


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


            // =========================================
            // BUSCAR BITÁCORA
            // =========================================
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


            // =========================================
            // ELIMINAR
            // =========================================
            _context.BitacorasVehiculo.Remove(
                bitacora);

            await _context.SaveChangesAsync();


            // =========================================
            // RECALCULAR TOTAL
            // =========================================
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


        // =========================================
        // RECALCULAR TOTAL DE LA RENDICIÓN
        //
        // Total =
        // Gastos + Bitácora de vehículo
        // =========================================
        private async Task ActualizarTotalesConBitacora(
            int idRendicion)
        {
            var rendicion =
                await _context.Rendiciones
                    .Include(r => r.Solicitud)
                    .FirstOrDefaultAsync(r =>
                        r.IdRendicion ==
                            idRendicion);

            if (rendicion == null ||
                rendicion.Solicitud == null)
            {
                return;
            }


            // =========================================
            // TOTAL GASTOS
            // =========================================
            var totalGastos =
                await _context.Gastos
                    .Where(g =>
                        g.IdRendicion ==
                            idRendicion)
                    .SumAsync(g =>
                        (decimal?)g.MontoTotal)
                ?? 0;


            // =========================================
            // TOTAL VEHÍCULO
            // =========================================
            var totalVehiculo =
                await _context.BitacorasVehiculo
                    .Where(b =>
                        b.IdRendicion ==
                            idRendicion)
                    .SumAsync(b =>
                        (decimal?)b.MontoAsignado)
                ?? 0;


            // =========================================
            // TOTAL GENERAL
            // =========================================
            rendicion.Total =
                totalGastos +
                totalVehiculo;


            // =========================================
            // SALDO
            // =========================================
            rendicion.Saldo =
                rendicion.Solicitud.Monto -
                rendicion.Total;


            await _context.SaveChangesAsync();
        }


        // =========================================
        // REGENERAR PDF DE LA RENDICIÓN
        // =========================================
        private async Task RegenerarPdfRendicion(
            int idRendicion)
        {
            var rendicion =
                await _context.Rendiciones
                    .Include(r => r.Solicitud)
                    .Include(r => r.Usuario)
                    .FirstOrDefaultAsync(r =>
                        r.IdRendicion ==
                            idRendicion);

            if (rendicion == null)
            {
                return;
            }


            // =========================================
            // GASTOS
            // =========================================
            var gastos =
                await _context.Gastos
                    .Include(g => g.TipoGasto)
                    .Include(g => g.TipoComprobante)
                    .Where(g =>
                        g.IdRendicion ==
                            idRendicion)
                    .OrderBy(g => g.Fecha)
                    .ToListAsync();


            // =========================================
            // BITÁCORAS
            // =========================================
            var bitacoras =
                await _context.BitacorasVehiculo
                    .Where(b =>
                        b.IdRendicion ==
                            idRendicion)
                    .OrderBy(b => b.Fecha)
                    .ToListAsync();


            // =========================================
            // DEVOLUCIÓN
            // =========================================
            var devolucion =
                await _context.DevolucionesSaldo
                    .FirstOrDefaultAsync(d =>
                        d.IdRendicion ==
                            idRendicion);


            // =========================================
            // GENERAR PDF
            // =========================================
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