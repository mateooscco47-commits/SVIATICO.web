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
        private readonly RendicionPdfService _rendicionPdfService;
        private readonly CorreoService _correoService;

        public GastoController(
    AplicacionDbContexto context,
    RucService rucService,
    RendicionPdfService rendicionPdfService,
    CorreoService correoService)
        {
            _context = context;
            _rucService = rucService;
            _rendicionPdfService = rendicionPdfService;
            _correoService = correoService;
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
            ViewBag.DevolucionSaldo = await _context.DevolucionesSaldo
    .FirstOrDefaultAsync(d => d.IdRendicion == idRendicion);
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

            // =========================================
            // VALIDAR ESTADO DE LA RENDICIÓN
            // =========================================

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
            // LIMPIAR CAMPOS RECIBIDOS
            // =========================================

            gasto.Ruc =
                gasto.Ruc?.Trim();

            gasto.RazonSocial =
                gasto.RazonSocial?.Trim();

            gasto.DomicilioFiscal =
                gasto.DomicilioFiscal?.Trim();

            gasto.Serie =
                gasto.Serie?.Trim();

            gasto.Numero =
                gasto.Numero?.Trim();

            gasto.Detalle =
                gasto.Detalle?.Trim();

            /*
             * Estos campos pueden ser completados por la API.
             * Se eliminan del ModelState para validarlos después
             * de consultar el RUC.
             */
            ModelState.Remove(
                nameof(gasto.RazonSocial));

            ModelState.Remove(
                nameof(gasto.DomicilioFiscal));

            ModelState.Remove(
                nameof(gasto.ValorVenta));

            ModelState.Remove(
                nameof(gasto.IGV));

            // =========================================
            // VALIDAR FECHA DEL GASTO
            // =========================================

            if (gasto.Fecha == default)
            {
                ModelState.AddModelError(
                    nameof(gasto.Fecha),
                    "Debe ingresar la fecha del gasto.");
            }
            else if (
                gasto.Fecha.Date <
                    rendicion.FechaInicio.Date ||
                gasto.Fecha.Date >
                    rendicion.FechaFin.Date)
            {
                ModelState.AddModelError(
                    nameof(gasto.Fecha),
                    $"La fecha del gasto debe estar entre " +
                    $"{rendicion.FechaInicio:dd/MM/yyyy} y " +
                    $"{rendicion.FechaFin:dd/MM/yyyy}. " +
                    "Puede registrar el gasto posteriormente, " +
                    "pero la fecha del comprobante debe pertenecer " +
                    "al periodo aprobado.");
            }

            // =========================================
            // VALIDAR MONTO TOTAL
            // =========================================

            if (gasto.MontoTotal <= 0)
            {
                ModelState.AddModelError(
                    nameof(gasto.MontoTotal),
                    "El monto total debe ser mayor que cero.");
            }

            // =========================================
            // CALCULAR VALOR DE VENTA E IGV
            // =========================================

            if (gasto.MontoTotal > 0)
            {
                if (gasto.ExoneracionIGV)
                {
                    gasto.ValorVenta = Math.Round(
                        gasto.MontoTotal,
                        2,
                        MidpointRounding.AwayFromZero);

                    gasto.IGV = 0;
                }
                else
                {
                    gasto.ValorVenta = Math.Round(
                        gasto.MontoTotal / 1.18m,
                        2,
                        MidpointRounding.AwayFromZero);

                    gasto.IGV = Math.Round(
                        gasto.MontoTotal -
                        gasto.ValorVenta,
                        2,
                        MidpointRounding.AwayFromZero);
                }
            }

            // =========================================
            // VALIDAR TIPO DE GASTO
            // Y LÍMITE DIARIO
            //
            // Hospedaje: S/ 50 por día
            // Alimentación: S/ 40 por día
            // =========================================

            var tipoGasto =
                await _context.TipoGastos
                    .FirstOrDefaultAsync(t =>
                        t.IdTipoGasto ==
                        gasto.IdTipoGasto);

            if (tipoGasto == null)
            {
                ModelState.AddModelError(
                    nameof(gasto.IdTipoGasto),
                    "El tipo de gasto seleccionado no existe.");
            }
            else if (gasto.Fecha != default)
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
                    DateTime inicioDia =
                        gasto.Fecha.Date;

                    DateTime finDia =
                        inicioDia.AddDays(1);

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

                    if (nuevoTotalDelDia >
                        limiteDiario)
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

            if (string.IsNullOrWhiteSpace(
                    gasto.Ruc) ||
                gasto.Ruc.Length != 11 ||
                !gasto.Ruc.All(char.IsDigit))
            {
                ModelState.AddModelError(
                    nameof(gasto.Ruc),
                    "El RUC debe contener exactamente 11 dígitos.");
            }

            // =========================================
            // CONSULTAR RUC
            // =========================================

            if (ModelState.IsValid)
            {
                var domicilioIngresado =
                    gasto.DomicilioFiscal;

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
                        string.IsNullOrWhiteSpace(
                            consultaRuc.Ruc)
                            ? gasto.Ruc
                            : consultaRuc.Ruc.Trim();

                    gasto.RazonSocial =
                        consultaRuc.RazonSocial?.Trim();

                    /*
                     * Si el empleado escribió o corrigió el domicilio,
                     * se conserva ese valor.
                     *
                     * Solo se utiliza el domicilio de la API cuando
                     * el campo enviado está vacío.
                     */
                    if (string.IsNullOrWhiteSpace(
                            domicilioIngresado))
                    {
                        gasto.DomicilioFiscal =
                            consultaRuc.DomicilioFiscal?
                                .Trim();
                    }
                    else
                    {
                        gasto.DomicilioFiscal =
                            domicilioIngresado.Trim();
                    }
                }
            }

            // =========================================
            // VALIDAR RAZÓN SOCIAL
            // =========================================

            if (string.IsNullOrWhiteSpace(
                    gasto.RazonSocial))
            {
                ModelState.AddModelError(
                    nameof(gasto.RazonSocial),
                    "No se encontró la razón social del RUC.");
            }
            else if (gasto.RazonSocial.Length > 250)
            {
                ModelState.AddModelError(
                    nameof(gasto.RazonSocial),
                    "La razón social no puede superar los 250 caracteres.");
            }

            // =========================================
            // VALIDAR DOMICILIO FISCAL
            // =========================================

            if (string.IsNullOrWhiteSpace(
                    gasto.DomicilioFiscal))
            {
                ModelState.AddModelError(
                    nameof(gasto.DomicilioFiscal),
                    "Debe ingresar el domicilio fiscal.");
            }
            else if (
                gasto.DomicilioFiscal.Length > 300)
            {
                ModelState.AddModelError(
                    nameof(gasto.DomicilioFiscal),
                    "El domicilio fiscal no puede superar los 300 caracteres.");
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
                        $"{x.Key}: {string.Join(
                            ", ",
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
                        idRendicion =
                            gasto.IdRendicion
                    });
            }

            // =========================================
            // VALIDAR Y GUARDAR COMPROBANTE
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
                    .GetExtension(
                        archivo.FileName)
                    .ToLowerInvariant();

                if (!extensionesPermitidas
                    .Contains(extension))
                {
                    TempData["error"] =
                        "El comprobante debe ser PDF, JPG, JPEG o PNG.";

                    return RedirectToAction(
                        nameof(Index),
                        new
                        {
                            idRendicion =
                                gasto.IdRendicion
                        });
                }

                const long tamanioMaximo =
                    5 * 1024 * 1024;

                if (archivo.Length >
                    tamanioMaximo)
                {
                    TempData["error"] =
                        "El comprobante no debe superar los 5 MB.";

                    return RedirectToAction(
                        nameof(Index),
                        new
                        {
                            idRendicion =
                                gasto.IdRendicion
                        });
                }

                var nombreArchivo =
                    $"{Guid.NewGuid()}{extension}";

                var carpeta = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot",
                    "comprobantes");

                Directory.CreateDirectory(
                    carpeta);

                var rutaCompleta =
                    Path.Combine(
                        carpeta,
                        nombreArchivo);

                await using var stream =
                    new FileStream(
                        rutaCompleta,
                        FileMode.Create);

                await archivo.CopyToAsync(
                    stream);

                gasto.Comprobante =
                    $"/comprobantes/{nombreArchivo}";
            }

            // =========================================
            // GUARDAR GASTO
            // =========================================

            _context.Gastos.Add(
                gasto);

            await _context.SaveChangesAsync();

            await ActualizarTotalesRendicion(
                gasto.IdRendicion);

            TempData["mensaje"] =
                gasto.ExoneracionIGV
                    ? $"Gasto registrado. Operación exonerada: " +
                      $"valor de venta S/ {gasto.ValorVenta:N2}, " +
                      "IGV S/ 0.00."
                    : $"Gasto registrado. Valor de venta: " +
                      $"S/ {gasto.ValorVenta:N2}, " +
                      $"IGV: S/ {gasto.IGV:N2}.";

            return RedirectToAction(
                nameof(Index),
                new
                {
                    idRendicion =
                        gasto.IdRendicion
                });
        }


        // =========================================
        // EDITAR GASTO - ADMINISTRADOR
        // =========================================
        [HttpGet]
        public async Task<IActionResult> EditAdmin(int id)
        {
            var idRol =
                HttpContext.Session.GetInt32("IdRol");

            if (idRol != 1)
            {
                TempData["error"] =
                    "No tiene permisos para editar gastos.";

                return RedirectToAction(
                    "Index",
                    "Home");
            }

            var gasto =
                await _context.Gastos
                    .Include(g => g.Rendicion)
                        .ThenInclude(r => r!.Solicitud)
                    .Include(g => g.TipoGasto)
                    .Include(g => g.TipoComprobante)
                    .FirstOrDefaultAsync(g =>
                        g.IdGasto == id);

            if (gasto == null)
            {
                TempData["error"] =
                    "No se encontró el gasto.";

                return RedirectToAction(
                    "IndexAdmin",
                    "Rendicion");
            }

            if (gasto.Rendicion == null)
            {
                TempData["error"] =
                    "No se encontró la rendición asociada al gasto.";

                return RedirectToAction(
                    "IndexAdmin",
                    "Rendicion");
            }

            // Solo se permite corregir mientras está pendiente de revisión.
            if (gasto.Rendicion.IdEstadoRendicion != 2)
            {
                TempData["error"] =
                    "Solo se pueden editar gastos de una rendición pendiente de revisión.";

                return RedirectToAction(
                    "DetalleAdmin",
                    "Rendicion",
                    new
                    {
                        id = gasto.IdRendicion
                    });
            }

            ViewBag.TiposGasto =
                await _context.TipoGastos
                    .OrderBy(t => t.Nombre)
                    .ToListAsync();

            ViewBag.TiposComprobante =
                await _context.TipoComprobantes
                    .OrderBy(t => t.Nombre)
                    .ToListAsync();

            ViewBag.Rendicion =
                gasto.Rendicion;

            return View(gasto);
        }


        // =========================================
        // GUARDAR EDICIÓN DE GASTO - ADMINISTRADOR
        // =========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditAdmin(
            Gasto modelo,
            IFormFile? archivo)
        {
            var idRol =
                HttpContext.Session.GetInt32("IdRol");

            if (idRol != 1)
            {
                TempData["error"] =
                    "No tiene permisos para editar gastos.";

                return RedirectToAction(
                    "Index",
                    "Home");
            }

            var gasto =
                await _context.Gastos
                    .Include(g => g.Rendicion)
                        .ThenInclude(r => r!.Solicitud)
                    .FirstOrDefaultAsync(g =>
                        g.IdGasto == modelo.IdGasto);

            if (gasto == null)
            {
                TempData["error"] =
                    "No se encontró el gasto.";

                return RedirectToAction(
                    "IndexAdmin",
                    "Rendicion");
            }

            if (gasto.Rendicion == null ||
                gasto.Rendicion.Solicitud == null)
            {
                TempData["error"] =
                    "No se encontró la información de la rendición.";

                return RedirectToAction(
                    "IndexAdmin",
                    "Rendicion");
            }

            var rendicion =
                gasto.Rendicion;

            // No modificar rendiciones ya aprobadas o finalizadas.
            if (rendicion.IdEstadoRendicion != 2)
            {
                TempData["error"] =
                    "Solo se pueden editar gastos de una rendición pendiente de revisión.";

                return RedirectToAction(
                    "DetalleAdmin",
                    "Rendicion",
                    new
                    {
                        id = gasto.IdRendicion
                    });
            }

            // =========================================
            // LIMPIAR CAMPOS
            // =========================================

            modelo.Ruc =
                modelo.Ruc?.Trim();

            modelo.RazonSocial =
                modelo.RazonSocial?.Trim();

            modelo.DomicilioFiscal =
                modelo.DomicilioFiscal?.Trim();

            modelo.Serie =
                modelo.Serie?.Trim();

            modelo.Numero =
                modelo.Numero?.Trim();

            modelo.Detalle =
                modelo.Detalle?.Trim();

            ModelState.Remove(
                nameof(modelo.RazonSocial));

            ModelState.Remove(
                nameof(modelo.DomicilioFiscal));

            ModelState.Remove(
                nameof(modelo.ValorVenta));

            ModelState.Remove(
                nameof(modelo.IGV));

            ModelState.Remove(
                nameof(modelo.Comprobante));

            ModelState.Remove(
                nameof(modelo.Rendicion));

            ModelState.Remove(
                nameof(modelo.TipoGasto));

            ModelState.Remove(
                nameof(modelo.TipoComprobante));

            // =========================================
            // VALIDAR FECHA
            // =========================================

            if (modelo.Fecha == default)
            {
                ModelState.AddModelError(
                    nameof(modelo.Fecha),
                    "Debe ingresar la fecha del gasto.");
            }
            else if (
                modelo.Fecha.Date <
                    rendicion.FechaInicio.Date ||
                modelo.Fecha.Date >
                    rendicion.FechaFin.Date)
            {
                ModelState.AddModelError(
                    nameof(modelo.Fecha),
                    $"La fecha del gasto debe estar entre " +
                    $"{rendicion.FechaInicio:dd/MM/yyyy} y " +
                    $"{rendicion.FechaFin:dd/MM/yyyy}.");
            }

            // =========================================
            // VALIDAR MONTO TOTAL
            // =========================================

            if (modelo.MontoTotal <= 0)
            {
                ModelState.AddModelError(
                    nameof(modelo.MontoTotal),
                    "El monto total debe ser mayor que cero.");
            }

            // =========================================
            // CALCULAR VALOR DE VENTA E IGV
            // =========================================

            if (modelo.MontoTotal > 0)
            {
                if (modelo.ExoneracionIGV)
                {
                    modelo.ValorVenta =
                        Math.Round(
                            modelo.MontoTotal,
                            2,
                            MidpointRounding.AwayFromZero);

                    modelo.IGV = 0;
                }
                else
                {
                    modelo.ValorVenta =
                        Math.Round(
                            modelo.MontoTotal / 1.18m,
                            2,
                            MidpointRounding.AwayFromZero);

                    modelo.IGV =
                        Math.Round(
                            modelo.MontoTotal -
                            modelo.ValorVenta,
                            2,
                            MidpointRounding.AwayFromZero);
                }
            }

            // =========================================
            // VALIDAR TIPO DE GASTO Y LÍMITE DIARIO
            // EXCLUYENDO EL GASTO QUE SE ESTÁ EDITANDO
            // =========================================

            var tipoGasto =
                await _context.TipoGastos
                    .FirstOrDefaultAsync(t =>
                        t.IdTipoGasto ==
                        modelo.IdTipoGasto);

            if (tipoGasto == null)
            {
                ModelState.AddModelError(
                    nameof(modelo.IdTipoGasto),
                    "El tipo de gasto seleccionado no existe.");
            }
            else if (modelo.Fecha != default)
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
                    DateTime inicioDia =
                        modelo.Fecha.Date;

                    DateTime finDia =
                        inicioDia.AddDays(1);

                    decimal montoRegistradoEseDia =
                        await _context.Gastos
                            .Where(g =>
                                g.IdRendicion ==
                                    gasto.IdRendicion &&
                                g.IdGasto !=
                                    gasto.IdGasto &&
                                g.IdTipoGasto ==
                                    modelo.IdTipoGasto &&
                                g.Fecha >= inicioDia &&
                                g.Fecha < finDia)
                            .SumAsync(g =>
                                (decimal?)g.MontoTotal) ?? 0;

                    decimal nuevoTotalDelDia =
                        montoRegistradoEseDia +
                        modelo.MontoTotal;

                    if (nuevoTotalDelDia >
                        limiteDiario)
                    {
                        decimal disponible =
                            limiteDiario -
                            montoRegistradoEseDia;

                        if (disponible < 0)
                        {
                            disponible = 0;
                        }

                        ModelState.AddModelError(
                            nameof(modelo.MontoTotal),
                            $"El límite diario para " +
                            $"{tipoGasto.Nombre} es " +
                            $"S/ {limiteDiario:N2}. " +
                            $"Solo puede dejar este gasto hasta " +
                            $"S/ {disponible:N2}.");
                    }
                }
            }

            // =========================================
            // VALIDAR TIPO DE COMPROBANTE
            // =========================================

            var tipoComprobanteExiste =
                await _context.TipoComprobantes
                    .AnyAsync(t =>
                        t.IdTipoComprobante ==
                        modelo.IdTipoComprobante);

            if (!tipoComprobanteExiste)
            {
                ModelState.AddModelError(
                    nameof(modelo.IdTipoComprobante),
                    "El tipo de comprobante seleccionado no existe.");
            }

            // =========================================
            // VALIDAR RUC
            // =========================================

            if (string.IsNullOrWhiteSpace(
                    modelo.Ruc) ||
                modelo.Ruc.Length != 11 ||
                !modelo.Ruc.All(char.IsDigit))
            {
                ModelState.AddModelError(
                    nameof(modelo.Ruc),
                    "El RUC debe contener exactamente 11 dígitos.");
            }

            // =========================================
            // CONSULTAR RUC
            // =========================================

            if (ModelState.IsValid)
            {
                var domicilioIngresado =
                    modelo.DomicilioFiscal;

                var consultaRuc =
                    await _rucService.ConsultarAsync(
                        modelo.Ruc!);

                if (!consultaRuc.Exito)
                {
                    ModelState.AddModelError(
                        nameof(modelo.Ruc),
                        consultaRuc.Mensaje ??
                        "No se pudo validar el RUC.");
                }
                else
                {
                    modelo.Ruc =
                        string.IsNullOrWhiteSpace(
                            consultaRuc.Ruc)
                            ? modelo.Ruc
                            : consultaRuc.Ruc.Trim();

                    modelo.RazonSocial =
                        consultaRuc.RazonSocial?.Trim();

                    if (string.IsNullOrWhiteSpace(
                            domicilioIngresado))
                    {
                        modelo.DomicilioFiscal =
                            consultaRuc.DomicilioFiscal?
                                .Trim();
                    }
                    else
                    {
                        modelo.DomicilioFiscal =
                            domicilioIngresado.Trim();
                    }
                }
            }

            // =========================================
            // VALIDAR RAZÓN SOCIAL
            // =========================================

            if (string.IsNullOrWhiteSpace(
                    modelo.RazonSocial))
            {
                ModelState.AddModelError(
                    nameof(modelo.RazonSocial),
                    "No se encontró la razón social del RUC.");
            }
            else if (modelo.RazonSocial.Length > 250)
            {
                ModelState.AddModelError(
                    nameof(modelo.RazonSocial),
                    "La razón social no puede superar los 250 caracteres.");
            }

            // =========================================
            // VALIDAR DOMICILIO FISCAL
            // =========================================

            if (string.IsNullOrWhiteSpace(
                    modelo.DomicilioFiscal))
            {
                ModelState.AddModelError(
                    nameof(modelo.DomicilioFiscal),
                    "Debe ingresar el domicilio fiscal.");
            }
            else if (
                modelo.DomicilioFiscal.Length > 300)
            {
                ModelState.AddModelError(
                    nameof(modelo.DomicilioFiscal),
                    "El domicilio fiscal no puede superar los 300 caracteres.");
            }

            // =========================================
            // MOSTRAR ERRORES
            // =========================================

            if (!ModelState.IsValid)
            {
                var errores =
                    ModelState
                        .Where(x =>
                            x.Value != null &&
                            x.Value.Errors.Count > 0)
                        .Select(x =>
                            $"{x.Key}: " +
                            $"{string.Join(
                                ", ",
                                x.Value!.Errors.Select(e =>
                                    string.IsNullOrWhiteSpace(
                                        e.ErrorMessage)
                                        ? "Valor no válido."
                                        : e.ErrorMessage))}");

                TempData["error"] =
                    string.Join(
                        "<br>",
                        errores);

                return RedirectToAction(
                    nameof(EditAdmin),
                    new
                    {
                        id = gasto.IdGasto
                    });
            }

            // =========================================
            // VALIDAR NUEVO COMPROBANTE
            // =========================================

            string? nuevaRutaComprobante = null;
            string? nuevaRutaFisica = null;

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

                var extension =
                    Path.GetExtension(
                        archivo.FileName)
                    .ToLowerInvariant();

                if (!extensionesPermitidas
                    .Contains(extension))
                {
                    TempData["error"] =
                        "El comprobante debe ser PDF, JPG, JPEG o PNG.";

                    return RedirectToAction(
                        nameof(EditAdmin),
                        new
                        {
                            id = gasto.IdGasto
                        });
                }

                const long tamanioMaximo =
                    5 * 1024 * 1024;

                if (archivo.Length >
                    tamanioMaximo)
                {
                    TempData["error"] =
                        "El comprobante no debe superar los 5 MB.";

                    return RedirectToAction(
                        nameof(EditAdmin),
                        new
                        {
                            id = gasto.IdGasto
                        });
                }

                var nombreArchivo =
                    $"{Guid.NewGuid()}{extension}";

                var carpeta =
                    Path.Combine(
                        Directory.GetCurrentDirectory(),
                        "wwwroot",
                        "comprobantes");

                Directory.CreateDirectory(
                    carpeta);

                nuevaRutaFisica =
                    Path.Combine(
                        carpeta,
                        nombreArchivo);

                await using var stream =
                    new FileStream(
                        nuevaRutaFisica,
                        FileMode.Create);

                await archivo.CopyToAsync(
                    stream);

                nuevaRutaComprobante =
                    $"/comprobantes/{nombreArchivo}";
            }

            // =========================================
            // ACTUALIZAR GASTO
            // =========================================

            var comprobanteAnterior =
                gasto.Comprobante;

            gasto.Fecha =
                modelo.Fecha;

            gasto.IdTipoGasto =
                modelo.IdTipoGasto;

            gasto.IdTipoComprobante =
                modelo.IdTipoComprobante;

            gasto.Ruc =
                modelo.Ruc;

            gasto.RazonSocial =
                modelo.RazonSocial;

            gasto.DomicilioFiscal =
                modelo.DomicilioFiscal;

            gasto.Serie =
                modelo.Serie;

            gasto.Numero =
                modelo.Numero;

            gasto.Detalle =
                modelo.Detalle;

            gasto.MontoTotal =
                modelo.MontoTotal;

            gasto.ValorVenta =
                modelo.ValorVenta;

            gasto.IGV =
                modelo.IGV;

            gasto.ExoneracionIGV =
                modelo.ExoneracionIGV;

            if (!string.IsNullOrWhiteSpace(
                    nuevaRutaComprobante))
            {
                gasto.Comprobante =
                    nuevaRutaComprobante;
            }

            try
            {
                // =========================================
                // 1. GUARDAR CAMBIOS DEL GASTO
                // =========================================

                await _context.SaveChangesAsync();


                // =========================================
                // 2. RECALCULAR TOTAL Y SALDO
                // =========================================

                await ActualizarTotalesRendicion(
                    gasto.IdRendicion);


                // =========================================
                // 3. RECARGAR RENDICIÓN ACTUALIZADA
                // =========================================

                var rendicionActualizada =
                    await _context.Rendiciones
                        .Include(r => r.Solicitud)
                        .Include(r => r.Usuario)
                        .FirstOrDefaultAsync(r =>
                            r.IdRendicion ==
                            gasto.IdRendicion);

                if (rendicionActualizada == null)
                {
                    TempData["error"] =
                        "El gasto fue actualizado, pero no se pudo cargar la rendición para regenerar el PDF.";

                    return RedirectToAction(
                        "DetalleAdmin",
                        "Rendicion",
                        new
                        {
                            id = gasto.IdRendicion
                        });
                }


                // =========================================
                // 4. RECARGAR TODOS LOS GASTOS
                // CON LOS DATOS YA ACTUALIZADOS
                // =========================================

                var gastosActualizados =
                    await _context.Gastos
                        .Include(g => g.TipoGasto)
                        .Include(g => g.TipoComprobante)
                        .Where(g =>
                            g.IdRendicion ==
                            gasto.IdRendicion)
                        .OrderBy(g => g.Fecha)
                        .ToListAsync();


                // =========================================
                // 5. OBTENER DEVOLUCIÓN
                // =========================================

                var devolucionActualizada =
                    await _context.DevolucionesSaldo
                        .FirstOrDefaultAsync(d =>
                            d.IdRendicion ==
                            gasto.IdRendicion);


                // =========================================
                // 6. REGENERAR PDF
                // =========================================

                var resultadoPdf =
                    await _rendicionPdfService
                        .GenerarAsync(
                            rendicionActualizada,
                            gastosActualizados,
                            devolucionActualizada);


                // =========================================
                // 7. ACTUALIZAR RUTA DEL PDF
                //
                // Se agrega una versión a la URL para evitar
                // que el navegador muestre una copia antigua
                // almacenada en caché.
                // =========================================

                rendicionActualizada.ArchivoPdf =
                    $"{resultadoPdf.RutaPublica}?v={DateTime.Now.Ticks}";


                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                if (!string.IsNullOrWhiteSpace(
                        nuevaRutaFisica) &&
                    System.IO.File.Exists(
                        nuevaRutaFisica))
                {
                    System.IO.File.Delete(
                        nuevaRutaFisica);
                }

                TempData["error"] =
                    "No se pudo actualizar completamente el gasto y regenerar el PDF. " +
                    ex.Message;

                return RedirectToAction(
                    nameof(EditAdmin),
                    new
                    {
                        id = gasto.IdGasto
                    });
            }

            // =========================================
            // ELIMINAR COMPROBANTE ANTERIOR
            // SOLO DESPUÉS DE GUARDAR CORRECTAMENTE
            // =========================================

            if (!string.IsNullOrWhiteSpace(
                    nuevaRutaComprobante) &&
                !string.IsNullOrWhiteSpace(
                    comprobanteAnterior))
            {
                var rutaRelativaAnterior =
                    comprobanteAnterior
                        .TrimStart('/')
                        .Replace(
                            '/',
                            Path.DirectorySeparatorChar);

                var rutaAnterior =
                    Path.Combine(
                        Directory.GetCurrentDirectory(),
                        "wwwroot",
                        rutaRelativaAnterior);

                if (System.IO.File.Exists(
                        rutaAnterior))
                {
                    try
                    {
                        System.IO.File.Delete(
                            rutaAnterior);
                    }
                    catch
                    {
                        // La edición ya fue guardada.
                        // Si no se puede borrar el archivo antiguo,
                        // no se interrumpe la operación.
                    }
                }
            }

            TempData["mensaje"] =
                "El gasto fue corregido correctamente por el administrador.";

            return RedirectToAction(
                "DetalleAdmin",
                "Rendicion",
                new
                {
                    id = gasto.IdRendicion
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
            var idUsuario =
                HttpContext.Session.GetInt32("IdUsuario");

            if (idUsuario == null)
            {
                TempData["error"] =
                    "La sesión ha expirado. Inicie sesión nuevamente.";

                return RedirectToAction(
                    "Index",
                    "Home");
            }

            var rendicion = await _context.Rendiciones
                .Include(r => r.Solicitud)
                .Include(r => r.Usuario)
                .FirstOrDefaultAsync(r =>
                    r.IdRendicion == idRendicion &&
                    r.IdUsuario == idUsuario.Value);

            if (rendicion == null)
            {
                TempData["error"] =
                    "No se encontró la rendición o no pertenece al usuario conectado.";

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

            var gastos = await _context.Gastos
                .Include(g => g.TipoGasto)
                .Include(g => g.TipoComprobante)
                .Where(g =>
                    g.IdRendicion == idRendicion)
                .OrderBy(g => g.Fecha)
                .ToListAsync();

            if (gastos.Count == 0 ||
                rendicion.Total <= 0)
            {
                TempData["error"] =
                    "Debe registrar al menos un gasto antes de enviar la rendición.";

                return RedirectToAction(
                    nameof(Index),
                    new { idRendicion });
            }

            var devolucion =
                await _context.DevolucionesSaldo
                    .FirstOrDefaultAsync(d =>
                        d.IdRendicion == idRendicion);

            if (rendicion.Saldo > 0 &&
                devolucion == null)
            {
                TempData["error"] =
                    $"Debe registrar la devolución de " +
                    $"S/ {rendicion.Saldo:N2} antes de enviar.";

                return RedirectToAction(
                    nameof(Index),
                    new { idRendicion });
            }

            if (rendicion.Saldo > 0 &&
                devolucion != null &&
                devolucion.Monto != rendicion.Saldo)
            {
                TempData["error"] =
                    $"El monto devuelto debe ser exactamente " +
                    $"S/ {rendicion.Saldo:N2}.";

                return RedirectToAction(
                    nameof(Index),
                    new { idRendicion });
            }

            if (rendicion.Saldo > 0 &&
                devolucion != null &&
                string.IsNullOrWhiteSpace(devolucion.Voucher))
            {
                TempData["error"] =
                    "La devolución debe tener un voucher adjunto.";

                return RedirectToAction(
                    nameof(Index),
                    new { idRendicion });
            }

            // =========================================
            // GENERAR REEMBOLSO SI EL EMPLEADO GASTÓ MÁS
            // =========================================

            if (rendicion.Saldo < 0)
            {
                decimal montoReembolso =
                    Math.Abs(rendicion.Saldo);

                var reembolsoExistente =
                    await _context.Reembolsos
                        .FirstOrDefaultAsync(r =>
                            r.IdRendicion == idRendicion);

                if (reembolsoExistente == null)
                {
                    var nuevoReembolso = new Reembolso
                    {
                        IdRendicion =
                            rendicion.IdRendicion,

                        IdUsuario =
                            rendicion.IdUsuario,

                        Monto =
                            montoReembolso,

                        FechaSolicitud =
                            DateTime.Now,

                        // 1 = Pendiente de aprobación
                        IdEstadoReembolso = 1
                    };

                    _context.Reembolsos.Add(
                        nuevoReembolso);
                }
                else
                {
                    reembolsoExistente.Monto =
                        montoReembolso;

                    reembolsoExistente.FechaSolicitud =
                        DateTime.Now;

                    reembolsoExistente.IdEstadoReembolso = 1;
                    reembolsoExistente.FechaAprobacion = null;
                    reembolsoExistente.FechaPago = null;
                    reembolsoExistente.Banco = null;
                    reembolsoExistente.NumeroOperacion = null;
                    reembolsoExistente.ComprobantePago = null;
                    reembolsoExistente.Observaciones = null;
                }
            }
            rendicion.IdEstadoRendicion = 2;
            rendicion.FechaEnvioRevision = DateTime.Now;

            await _context.SaveChangesAsync();

            ResultadoPdfRendicion resultadoPdf;

            try
            {
                resultadoPdf =
                    await _rendicionPdfService.GenerarAsync(
                        rendicion,
                        gastos,
                        devolucion);
            }
            catch (Exception)
            {
                TempData["error"] =
                    "No se pudo generar el PDF de la liquidación.";

                return RedirectToAction(
                    nameof(Index),
                    new { idRendicion });
            }

            rendicion.ArchivoPdf =
                resultadoPdf.RutaPublica;

            rendicion.FechaEnvioRevision =
                DateTime.Now;

            // 2 = Pendiente de revisión
            rendicion.IdEstadoRendicion = 2;

            await _context.SaveChangesAsync();

            var correosAdministradores =
                await _context.Usuarios
                    .Where(u =>
                        u.IdRol == 1 &&
                        u.Estado &&
                        !string.IsNullOrWhiteSpace(u.Correo))
                    .Select(u => u.Correo!)
                    .ToListAsync();

            var nombreEmpleado =
                $"{rendicion.Usuario?.Nombres} " +
                $"{rendicion.Usuario?.Apellidos}";

            var totalBase =
                gastos.Sum(g => g.ValorVenta);

            var totalIgv =
                gastos.Sum(g => g.IGV);

            var asunto =
                $"Liquidación de viáticos #{rendicion.IdRendicion} pendiente de revisión";

            var contenidoHtml = $"""
        <div style="font-family:Arial,sans-serif;max-width:700px">
            <h2 style="color:#0d6efd">
                Nueva liquidación pendiente de revisión
            </h2>

            <p>
                El empleado
                <strong>{nombreEmpleado}</strong>
                ha enviado una liquidación de gastos.
            </p>

            <table style="border-collapse:collapse;width:100%">
                <tr>
                    <td style="border:1px solid #ddd;padding:8px">
                        <strong>Liquidación</strong>
                    </td>
                    <td style="border:1px solid #ddd;padding:8px">
                        #{rendicion.IdRendicion}
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
                        {rendicion.Solicitud?.Destino}
                    </td>
                </tr>

                <tr>
                    <td style="border:1px solid #ddd;padding:8px">
                        <strong>Periodo</strong>
                    </td>
                    <td style="border:1px solid #ddd;padding:8px">
                        {rendicion.FechaInicio:dd/MM/yyyy}
                        al
                        {rendicion.FechaFin:dd/MM/yyyy}
                    </td>
                </tr>

                <tr>
                    <td style="border:1px solid #ddd;padding:8px">
                        <strong>Monto aprobado</strong>
                    </td>
                    <td style="border:1px solid #ddd;padding:8px">
                        S/ {rendicion.Solicitud?.Monto:N2}
                    </td>
                </tr>

                <tr>
                    <td style="border:1px solid #ddd;padding:8px">
                        <strong>Valor de venta</strong>
                    </td>
                    <td style="border:1px solid #ddd;padding:8px">
                        S/ {totalBase:N2}
                    </td>
                </tr>

                <tr>
                    <td style="border:1px solid #ddd;padding:8px">
                        <strong>IGV</strong>
                    </td>
                    <td style="border:1px solid #ddd;padding:8px">
                        S/ {totalIgv:N2}
                    </td>
                </tr>

                <tr>
                    <td style="border:1px solid #ddd;padding:8px">
                        <strong>Total rendido</strong>
                    </td>
                    <td style="border:1px solid #ddd;padding:8px">
                        S/ {rendicion.Total:N2}
                    </td>
                </tr>

                <tr>
                    <td style="border:1px solid #ddd;padding:8px">
                        <strong>Saldo</strong>
                    </td>
                    <td style="border:1px solid #ddd;padding:8px">
                        S/ {rendicion.Saldo:N2}
                    </td>
                </tr>
            </table>

            <p style="margin-top:20px">
                Se adjunta el PDF de la liquidación.
                Ingrese al sistema DINACEM para revisar los comprobantes,
                la devolución y aprobar o rechazar la rendición.
            </p>
        </div>
        """;

            var correoEnviado =
                await _correoService.EnviarAsync(
                    correosAdministradores,
                    asunto,
                    contenidoHtml,
                    resultadoPdf.RutaFisica,
                    resultadoPdf.NombreArchivo);

            TempData["mensaje"] =
                correoEnviado
                    ? "La rendición fue enviada para revisión y el PDF fue enviado a los administradores."
                    : "La rendición y el PDF fueron guardados, pero no fue posible enviar el correo.";

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