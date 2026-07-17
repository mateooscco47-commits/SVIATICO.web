using Dinacem.Models;
using Dinacem.Models.Servicios;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Dinacem.Controllers
{
    public class GastoController : Controller
    {
        private readonly AplicacionDbContexto _context;
        private readonly RucService _rucService;

        public GastoController(
            AplicacionDbContexto context,
            RucService rucService)
        {
            _context = context;
            _rucService = rucService;
        }

        // =========================================
        // MOSTRAR RENDICIÓN Y GASTOS
        // =========================================
        [HttpGet]
        public async Task<IActionResult> Index(int idRendicion)
        {
            var rendicion = await _context.Rendiciones
                .Include(r => r.Solicitud)
                .Include(r => r.EstadoRendicion)
                .FirstOrDefaultAsync(r =>
                    r.IdRendicion == idRendicion);

            if (rendicion == null)
            {
                TempData["error"] =
                    "No se encontró la rendición.";

                return RedirectToAction(
                    "Index",
                    "Rendicion");
            }

            var gastos = await _context.Gastos
                .Include(g => g.TipoGasto)
                .Include(g => g.TipoComprobante)
                .Where(g =>
                    g.IdRendicion == idRendicion)
                .OrderByDescending(g => g.Fecha)
                .ToListAsync();

            ViewBag.Rendicion = rendicion;

            ViewBag.TiposGasto = await _context.TipoGastos
                .OrderBy(t => t.Nombre)
                .ToListAsync();

            ViewBag.TiposComprobante =
                await _context.TipoComprobantes
                    .OrderBy(t => t.Nombre)
                    .ToListAsync();

            return View(gastos);
        }

        // =========================================
        // CONSULTAR RUC
        // =========================================
        [HttpGet]
        public async Task<IActionResult> ConsultarRuc(string ruc)
        {
            if (string.IsNullOrWhiteSpace(ruc))
            {
                return BadRequest(new
                {
                    mensaje = "Ingrese un número de RUC."
                });
            }

            var resultado =
                await _rucService.ConsultarAsync(ruc);

            if (!resultado.Exito)
            {
                return BadRequest(new
                {
                    mensaje = resultado.Mensaje
                });
            }

            return Json(new
            {
                ruc = resultado.Ruc,
                razonSocial = resultado.RazonSocial,
                domicilioFiscal = resultado.DomicilioFiscal,
                estado = resultado.Estado,
                condicion = resultado.Condicion
            });
        }

        // =========================================
        // REGISTRAR GASTO
        // =========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            Gasto gasto,
            IFormFile? archivo)
        {
            var rendicion = await _context.Rendiciones
                .Include(r => r.Solicitud)
                .FirstOrDefaultAsync(r =>
                    r.IdRendicion == gasto.IdRendicion);

            if (rendicion == null)
            {
                TempData["error"] =
                    "No se encontró la rendición.";

                return RedirectToAction(
                    "Index",
                    "Rendicion");
            }

            // Solo se agregan gastos mientras la rendición está en proceso.
            if (rendicion.IdEstadoRendicion != 1)
            {
                TempData["error"] =
                    "La rendición ya fue enviada y no permite registrar más gastos.";

                return RedirectToAction(
                    nameof(Index),
                    new
                    {
                        idRendicion = gasto.IdRendicion
                    });
            }

            // =========================================
            // VALIDAR FECHA DEL GASTO
            // =========================================
            if (gasto.Fecha.Date < rendicion.FechaInicio.Date ||
                gasto.Fecha.Date > rendicion.FechaFin.Date)
            {
                ModelState.AddModelError(
                    nameof(gasto.Fecha),
                    $"La fecha del gasto debe estar entre " +
                    $"{rendicion.FechaInicio:dd/MM/yyyy} y " +
                    $"{rendicion.FechaFin:dd/MM/yyyy}.");
            }

            // =========================================
            // VALIDAR TIPO Y LÍMITE DIARIO
            // Hospedaje: S/ 50 por día
            // Alimentación: S/ 40 por día
            // =========================================
            var tipoGasto = await _context.TipoGastos
                .FirstOrDefaultAsync(t =>
                    t.IdTipoGasto == gasto.IdTipoGasto);

            if (tipoGasto == null)
            {
                ModelState.AddModelError(
                    nameof(gasto.IdTipoGasto),
                    "El tipo de gasto seleccionado no existe.");
            }
            else
            {
                decimal limiteDiario = 0;

                if (tipoGasto.Nombre.Equals(
                    "Hospedaje",
                    StringComparison.OrdinalIgnoreCase))
                {
                    limiteDiario = 50;
                }
                else if (tipoGasto.Nombre.Equals(
                    "Alimentación",
                    StringComparison.OrdinalIgnoreCase))
                {
                    limiteDiario = 40;
                }

                if (limiteDiario > 0)
                {
                    DateTime inicioDia = gasto.Fecha.Date;
                    DateTime finDia = inicioDia.AddDays(1);

                    decimal montoRegistradoEseDia =
                        await _context.Gastos
                            .Where(g =>
                                g.IdRendicion ==
                                    gasto.IdRendicion &&
                                g.IdTipoGasto ==
                                    gasto.IdTipoGasto &&
                                g.Fecha >= inicioDia &&
                                g.Fecha < finDia)
                            .SumAsync(g =>
                                (decimal?)g.MontoTotal) ?? 0;

                    decimal nuevoTotalDelDia =
                        montoRegistradoEseDia +
                        gasto.MontoTotal;

                    if (nuevoTotalDelDia > limiteDiario)
                    {
                        decimal disponible =
                            limiteDiario -
                            montoRegistradoEseDia;

                        if (disponible < 0)
                        {
                            disponible = 0;
                        }

                        ModelState.AddModelError(
                            nameof(gasto.MontoTotal),
                            $"El límite diario para " +
                            $"{tipoGasto.Nombre} es " +
                            $"S/ {limiteDiario:N2}. " +
                            $"El {gasto.Fecha:dd/MM/yyyy} " +
                            $"ya tiene registrado " +
                            $"S/ {montoRegistradoEseDia:N2}. " +
                            $"Solo puede agregar hasta " +
                            $"S/ {disponible:N2}.");
                    }
                }
            }

            // =========================================
            // VALIDAR RUC
            // =========================================
            if (string.IsNullOrWhiteSpace(gasto.Ruc) ||
                gasto.Ruc.Length != 11 ||
                !gasto.Ruc.All(char.IsDigit))
            {
                ModelState.AddModelError(
                    nameof(gasto.Ruc),
                    "El RUC debe contener exactamente 11 dígitos.");
            }

            // Consultar el RUC y completar datos del proveedor.
            if (ModelState.IsValid)
            {
                var consultaRuc =
                    await _rucService.ConsultarAsync(
                        gasto.Ruc!);

                if (!consultaRuc.Exito)
                {
                    ModelState.AddModelError(
                        nameof(gasto.Ruc),
                        consultaRuc.Mensaje ??
                        "No se pudo validar el RUC.");
                }
                else
                {
                    gasto.Ruc =
                        consultaRuc.Ruc;

                    gasto.RazonSocial =
                        consultaRuc.RazonSocial;

                    gasto.DomicilioFiscal =
                        consultaRuc.DomicilioFiscal;
                }
            }

            // =========================================
            // MOSTRAR ERRORES
            // =========================================
            if (!ModelState.IsValid)
            {
                var errores = ModelState
                    .Where(x =>
                        x.Value != null &&
                        x.Value.Errors.Count > 0)
                    .Select(x =>
                        $"{x.Key}: {string.Join(", ",
                            x.Value!.Errors.Select(e =>
                                string.IsNullOrWhiteSpace(
                                    e.ErrorMessage)
                                    ? "Valor no válido."
                                    : e.ErrorMessage))}");

                TempData["error"] =
                    string.Join("<br>", errores);

                return RedirectToAction(
                    nameof(Index),
                    new
                    {
                        idRendicion = gasto.IdRendicion
                    });
            }

            // =========================================
            // GUARDAR COMPROBANTE
            // =========================================
            if (archivo != null &&
                archivo.Length > 0)
            {
                string[] extensionesPermitidas =
                {
                    ".pdf",
                    ".jpg",
                    ".jpeg",
                    ".png"
                };

                var extension = Path
                    .GetExtension(archivo.FileName)
                    .ToLowerInvariant();

                if (!extensionesPermitidas.Contains(extension))
                {
                    TempData["error"] =
                        "El comprobante debe ser PDF, JPG, JPEG o PNG.";

                    return RedirectToAction(
                        nameof(Index),
                        new
                        {
                            idRendicion = gasto.IdRendicion
                        });
                }

                const long tamanioMaximo =
                    5 * 1024 * 1024;

                if (archivo.Length > tamanioMaximo)
                {
                    TempData["error"] =
                        "El comprobante no debe superar los 5 MB.";

                    return RedirectToAction(
                        nameof(Index),
                        new
                        {
                            idRendicion = gasto.IdRendicion
                        });
                }

                var nombreArchivo =
                    $"{Guid.NewGuid()}{extension}";

                var carpeta = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot",
                    "comprobantes");

                Directory.CreateDirectory(carpeta);

                var rutaCompleta = Path.Combine(
                    carpeta,
                    nombreArchivo);

                await using var stream =
                    new FileStream(
                        rutaCompleta,
                        FileMode.Create);

                await archivo.CopyToAsync(stream);

                gasto.Comprobante =
                    $"/comprobantes/{nombreArchivo}";
            }

            // =========================================
            // GUARDAR GASTO
            // =========================================
            _context.Gastos.Add(gasto);
            await _context.SaveChangesAsync();

            await ActualizarTotalesRendicion(
                gasto.IdRendicion);

            TempData["mensaje"] =
                "Gasto registrado correctamente.";

            return RedirectToAction(
                nameof(Index),
                new
                {
                    idRendicion = gasto.IdRendicion
                });
        }

        // =========================================
        // ELIMINAR GASTO
        // =========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(
            int id,
            int idRendicion)
        {
            var rendicion = await _context.Rendiciones
                .FirstOrDefaultAsync(r =>
                    r.IdRendicion == idRendicion);

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
                    "No se pueden eliminar gastos de una rendición enviada.";

                return RedirectToAction(
                    nameof(Index),
                    new { idRendicion });
            }

            var gasto = await _context.Gastos
                .FirstOrDefaultAsync(g =>
                    g.IdGasto == id &&
                    g.IdRendicion == idRendicion);

            if (gasto == null)
            {
                TempData["error"] =
                    "No se encontró el gasto.";

                return RedirectToAction(
                    nameof(Index),
                    new { idRendicion });
            }

            if (!string.IsNullOrWhiteSpace(
                gasto.Comprobante))
            {
                var rutaRelativa =
                    gasto.Comprobante
                        .TrimStart('/')
                        .Replace(
                            '/',
                            Path.DirectorySeparatorChar);

                var rutaCompleta = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot",
                    rutaRelativa);

                if (System.IO.File.Exists(
                    rutaCompleta))
                {
                    System.IO.File.Delete(
                        rutaCompleta);
                }
            }

            _context.Gastos.Remove(gasto);
            await _context.SaveChangesAsync();

            await ActualizarTotalesRendicion(
                idRendicion);

            TempData["mensaje"] =
                "Gasto eliminado correctamente.";

            return RedirectToAction(
                nameof(Index),
                new { idRendicion });
        }

        // =========================================
        // ENVIAR RENDICIÓN
        // =========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EnviarRendicion(
            int idRendicion)
        {
            var rendicion =
                await _context.Rendiciones
                    .FirstOrDefaultAsync(r =>
                        r.IdRendicion ==
                        idRendicion);

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
                    "Esta rendición ya fue enviada o finalizada.";

                return RedirectToAction(
                    "MisRendiciones",
                    "Rendicion");
            }

            var tieneGastos =
                await _context.Gastos
                    .AnyAsync(g =>
                        g.IdRendicion ==
                        idRendicion);

            if (!tieneGastos ||
                rendicion.Total <= 0)
            {
                TempData["error"] =
                    "Debe registrar al menos un gasto antes de enviar la rendición.";

                return RedirectToAction(
                    nameof(Index),
                    new { idRendicion });
            }

            rendicion.IdEstadoRendicion = 2;

            await _context.SaveChangesAsync();

            TempData["mensaje"] =
                "La rendición fue enviada para revisión.";

            return RedirectToAction(
                "MisRendiciones",
                "Rendicion");
        }

        // =========================================
        // ACTUALIZAR TOTAL Y SALDO
        // =========================================
        private async Task ActualizarTotalesRendicion(
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

            var total =
                await _context.Gastos
                    .Where(g =>
                        g.IdRendicion ==
                        idRendicion)
                    .SumAsync(g =>
                        (decimal?)g.MontoTotal) ?? 0;

            rendicion.Total = total;

            rendicion.Saldo =
                rendicion.Solicitud.Monto -
                total;

            await _context.SaveChangesAsync();
        }
    }
}