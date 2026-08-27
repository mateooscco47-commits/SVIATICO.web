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
        public async Task<IActionResult> Index(
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

            var bitacoras =
                await _context.BitacorasVehiculo
                    .Where(b =>
                        b.IdRendicion == idRendicion)
                    .OrderBy(b => b.Fecha)
                    .ToListAsync();

            ViewBag.Rendicion =
                rendicion;

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

            var rendicion =
                await _context.Rendiciones
                    .FirstOrDefaultAsync(r =>
                        r.IdRendicion ==
                            modelo.IdRendicion &&
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
                    "La rendición ya fue enviada y no permite registrar recorridos.";

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
                    "Debe ingresar el origen.");
            }

            if (string.IsNullOrWhiteSpace(
                    modelo.Destino))
            {
                ModelState.AddModelError(
                    nameof(modelo.Destino),
                    "Debe ingresar el destino.");
            }

            if (modelo.DistanciaKm <= 0)
            {
                ModelState.AddModelError(
                    nameof(modelo.DistanciaKm),
                    "La distancia debe ser mayor que cero.");
            }

            // El empleado NO asigna monto
            modelo.MontoAsignado = 0;

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

            _context.BitacorasVehiculo.Add(
                modelo);

            await _context.SaveChangesAsync();

            TempData["mensaje"] =
                "Recorrido registrado correctamente.";

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
        // ADMINISTRADOR: ASIGNAR MONTO
        // =========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AsignarMonto(
            int id,
            decimal monto)
        {
            var idRol =
                HttpContext.Session.GetInt32("IdRol");

            if (idRol != 1)
            {
                TempData["error"] =
                    "No tiene permisos para realizar esta operación.";

                return RedirectToAction(
                    "Index",
                    "Home");
            }

            var bitacora =
                await _context.BitacorasVehiculo
                    .Include(b => b.Rendicion)
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
                    "No se encontró la rendición asociada.";

                return RedirectToAction(
                    "IndexAdmin",
                    "Rendicion");
            }

            // Solo mientras esté pendiente de revisión
            if (bitacora.Rendicion.IdEstadoRendicion != 2)
            {
                TempData["error"] =
                    "Solo se puede asignar monto mientras la rendición está pendiente de revisión.";

                return RedirectToAction(
                    "DetalleAdmin",
                    "Rendicion",
                    new
                    {
                        id = bitacora.IdRendicion
                    });
            }

            if (monto <= 0)
            {
                TempData["error"] =
                    "El monto asignado debe ser mayor que cero.";

                return RedirectToAction(
                    "DetalleAdmin",
                    "Rendicion",
                    new
                    {
                        id = bitacora.IdRendicion
                    });
            }

            bitacora.MontoAsignado =
                Math.Round(
                    monto,
                    2,
                    MidpointRounding.AwayFromZero);

            await _context.SaveChangesAsync();

            // Recalcular total y saldo
            await ActualizarTotalesConBitacora(
                bitacora.IdRendicion);

            // Regenerar PDF con la bitácora actualizada
            await RegenerarPdfRendicion(
                bitacora.IdRendicion);

            TempData["mensaje"] =
                "Monto de vehículo asignado correctamente.";

            return RedirectToAction(
                "DetalleAdmin",
                "Rendicion",
                new
                {
                    id = bitacora.IdRendicion
                });
        }


        // =========================================
        // ADMINISTRADOR: QUITAR MONTO ASIGNADO
        // =========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> QuitarMonto(
            int id)
        {
            var idRol =
                HttpContext.Session.GetInt32("IdRol");

            if (idRol != 1)
            {
                TempData["error"] =
                    "No tiene permisos para realizar esta operación.";

                return RedirectToAction(
                    "Index",
                    "Home");
            }

            var bitacora =
                await _context.BitacorasVehiculo
                    .Include(b => b.Rendicion)
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

            if (bitacora.Rendicion == null ||
                bitacora.Rendicion.IdEstadoRendicion != 2)
            {
                TempData["error"] =
                    "El monto ya no puede modificarse porque la rendición fue procesada.";

                return RedirectToAction(
                    "DetalleAdmin",
                    "Rendicion",
                    new
                    {
                        id = bitacora.IdRendicion
                    });
            }

            bitacora.MontoAsignado = 0;

            await _context.SaveChangesAsync();

            await ActualizarTotalesConBitacora(
                bitacora.IdRendicion);

            await RegenerarPdfRendicion(
                bitacora.IdRendicion);

            TempData["mensaje"] =
                "El monto asignado fue retirado.";

            return RedirectToAction(
                "DetalleAdmin",
                "Rendicion",
                new
                {
                    id = bitacora.IdRendicion
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

            var totalGastos =
                await _context.Gastos
                    .Where(g =>
                        g.IdRendicion ==
                            idRendicion)
                    .SumAsync(g =>
                        (decimal?)g.MontoTotal)
                    ?? 0;

            var totalVehiculo =
                await _context.BitacorasVehiculo
                    .Where(b =>
                        b.IdRendicion ==
                            idRendicion)
                    .SumAsync(b =>
                        (decimal?)b.MontoAsignado)
                    ?? 0;

            rendicion.Total =
                totalGastos +
                totalVehiculo;

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
                $"{resultadoPdf.RutaPublica}?v={DateTime.Now.Ticks}";

            await _context.SaveChangesAsync();
        }

    }
}