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

        // =========================================================
        // CONSTANTES
        // =========================================================

        private const int ROL_ADMINISTRADOR = 1;

        private const int ESTADO_RENDICION_BORRADOR = 1;
        private const int ESTADO_RENDICION_PENDIENTE_REVISION = 2;

        private const int ESTADO_REEMBOLSO_PENDIENTE = 1;

        private const decimal LIMITE_ALIMENTACION_DIARIO = 40m;
        private const decimal LIMITE_HOSPEDAJE_DIARIO = 50m;

        private const decimal TASA_IGV = 0.18m;

        private const long TAMANIO_MAXIMO_COMPROBANTE =
            5 * 1024 * 1024;

        private static readonly string[] ExtensionesPermitidas =
        {
        ".pdf",
        ".jpg",
        ".jpeg",
        ".png"
    };

        // =========================================================
        // CONSTRUCTOR
        // =========================================================

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

        // =========================================================
        // MOSTRAR RENDICIÓN Y GASTOS
        // =========================================================

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

            ViewBag.TiposGasto =
                await _context.TipoGastos
                    .OrderBy(t => t.Nombre)
                    .ToListAsync();

            ViewBag.TiposComprobante =
                await _context.TipoComprobantes
                    .OrderBy(t => t.Nombre)
                    .ToListAsync();

            ViewBag.DevolucionSaldo =
                await _context.DevolucionesSaldo
                    .FirstOrDefaultAsync(d =>
                        d.IdRendicion == idRendicion);

            ViewBag.BitacorasVehiculo =
                await _context.BitacorasVehiculo
                    .Where(b =>
                        b.IdRendicion == idRendicion)
                    .OrderBy(b => b.Fecha)
                    .ToListAsync();

            return View(gastos);
        }

        // =========================================================
        // CONSULTAR RUC
        // =========================================================

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

            ruc = ruc.Trim();

            if (ruc.Length != 11 ||
                !ruc.All(char.IsDigit))
            {
                return BadRequest(new
                {
                    mensaje =
                        "El RUC debe contener exactamente 11 dígitos."
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

        // =========================================================
        // REGISTRAR GASTO
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            Gasto gasto,
            IFormFile? archivo)
        {
            var rendicion = await ObtenerRendicionAsync(
                gasto.IdRendicion);

            if (rendicion == null)
            {
                TempData["error"] =
                    "No se encontró la rendición.";

                return RedirectToAction(
                    "Index",
                    "Rendicion");
            }

            if (rendicion.IdEstadoRendicion !=
                ESTADO_RENDICION_BORRADOR)
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

            LimpiarCampos(gasto);

            EliminarValidacionesCalculadas(
                nameof(gasto.RazonSocial),
                nameof(gasto.DomicilioFiscal),
                nameof(gasto.ValorVenta),
                nameof(gasto.IGV));

            // =====================================================
            // OBTENER TIPO DE GASTO
            // =====================================================

            var tipoGasto =
                await ObtenerTipoGastoAsync(
                    gasto.IdTipoGasto);

            if (tipoGasto == null)
            {
                ModelState.AddModelError(
                    nameof(gasto.IdTipoGasto),
                    "El tipo de gasto seleccionado no existe.");

                return await ProcesarErroresCreate(
                    gasto.IdRendicion);
            }

            bool esMovilidad =
                EsMovilidad(tipoGasto);

            // =====================================================
            // VALIDAR FECHA
            // =====================================================

            ValidarFechaGasto(
                gasto.Fecha,
                rendicion,
                incluirMensajeDetallado: true);

            // =====================================================
            // VALIDAR MONTO
            // =====================================================

            ValidarMonto(gasto.MontoTotal);

            // =====================================================
            // CALCULAR IGV
            // =====================================================

            CalcularImpuestos(gasto);

            // =====================================================
            // CONFIGURAR MOVILIDAD
            // =====================================================

            if (esMovilidad)
            {
                LimpiarDatosComprobante(gasto);
            }

            // =====================================================
            // VALIDAR LÍMITE DIARIO
            // =====================================================

            if (gasto.Fecha != default)
            {
                await ValidarLimiteDiarioAsync(
                    gasto,
                    tipoGasto,
                    gasto.IdGasto);
            }

            // =====================================================
            // VALIDAR DATOS DEL COMPROBANTE
            // SOLO SI NO ES MOVILIDAD
            // =====================================================

            if (!esMovilidad)
            {
                ValidarDatosComprobante(
                    gasto);

                if (ModelState.IsValid)
                {
                    await ValidarRucAsync(gasto);
                }

                ValidarDatosProveedor(
                    gasto);
            }

            // =====================================================
            // MOSTRAR ERRORES
            // =====================================================

            if (!ModelState.IsValid)
            {
                return await ProcesarErroresCreate(
                    gasto.IdRendicion);
            }

            // =====================================================
            // GUARDAR COMPROBANTE
            // =====================================================

            if (!esMovilidad)
            {
                var resultadoArchivo =
                    await GuardarComprobanteAsync(
                        archivo);

                if (!resultadoArchivo.Exito)
                {
                    TempData["error"] =
                        resultadoArchivo.Mensaje;

                    return RedirectToAction(
                        nameof(Index),
                        new
                        {
                            idRendicion =
                                gasto.IdRendicion
                        });
                }

                gasto.Comprobante =
                    resultadoArchivo.RutaPublica;
            }
            else
            {
                gasto.Comprobante = null;
            }

            // =====================================================
            // GUARDAR GASTO
            // =====================================================

            _context.Gastos.Add(gasto);

            await _context.SaveChangesAsync();

            // =====================================================
            // ACTUALIZAR TOTALES
            // =====================================================

            await ActualizarTotalesRendicion(
                gasto.IdRendicion);

            // =====================================================
            // MENSAJE
            // =====================================================

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

        // =========================================================
        // EDITAR GASTO - ADMINISTRADOR
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> EditAdmin(int id)
        {
            if (!EsAdministrador())
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

            if (gasto.Rendicion.IdEstadoRendicion !=
                ESTADO_RENDICION_PENDIENTE_REVISION)
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

        // =========================================================
        // GUARDAR EDICIÓN DE GASTO - ADMINISTRADOR
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditAdmin(
            Gasto modelo,
            IFormFile? archivo)
        {
            if (!EsAdministrador())
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

            if (rendicion.IdEstadoRendicion !=
                ESTADO_RENDICION_PENDIENTE_REVISION)
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

            // =====================================================
            // LIMPIAR CAMPOS
            // =====================================================

            LimpiarCampos(modelo);

            EliminarValidacionesCalculadas(
                nameof(modelo.RazonSocial),
                nameof(modelo.DomicilioFiscal),
                nameof(modelo.ValorVenta),
                nameof(modelo.IGV),
                nameof(modelo.Comprobante),
                nameof(modelo.Rendicion),
                nameof(modelo.TipoGasto),
                nameof(modelo.TipoComprobante));

            // =====================================================
            // OBTENER TIPO DE GASTO
            // =====================================================

            var tipoGasto =
                await ObtenerTipoGastoAsync(
                    modelo.IdTipoGasto);

            if (tipoGasto == null)
            {
                ModelState.AddModelError(
                    nameof(modelo.IdTipoGasto),
                    "El tipo de gasto seleccionado no existe.");
            }

            bool esMovilidad =
                tipoGasto != null &&
                EsMovilidad(tipoGasto);

            // =====================================================
            // VALIDAR FECHA
            // =====================================================

            ValidarFechaGasto(
                modelo.Fecha,
                rendicion,
                incluirMensajeDetallado: false);

            // =====================================================
            // VALIDAR MONTO
            // =====================================================

            ValidarMonto(
                modelo.MontoTotal);

            // =====================================================
            // CALCULAR IGV
            // =====================================================

            CalcularImpuestos(modelo);

            // =====================================================
            // CONFIGURAR MOVILIDAD
            // =====================================================

            if (esMovilidad)
            {
                LimpiarDatosComprobante(modelo);
            }

            // =====================================================
            // VALIDAR LÍMITE DIARIO
            // =====================================================

            if (tipoGasto != null &&
                modelo.Fecha != default)
            {
                await ValidarLimiteDiarioAsync(
                    modelo,
                    tipoGasto,
                    gasto.IdGasto);
            }

            // =====================================================
            // VALIDAR COMPROBANTE
            // =====================================================

            if (!esMovilidad)
            {
                ValidarDatosComprobante(
                    modelo);

                if (ModelState.IsValid)
                {
                    await ValidarRucAsync(modelo);
                }

                ValidarDatosProveedor(
                    modelo);
            }

            // =====================================================
            // MOSTRAR ERRORES
            // =====================================================

            if (!ModelState.IsValid)
            {
                AgregarErroresTempData();

                return RedirectToAction(
                    nameof(EditAdmin),
                    new
                    {
                        id = gasto.IdGasto
                    });
            }

            // =====================================================
            // NUEVO COMPROBANTE
            // =====================================================

            string? nuevaRutaComprobante = null;
            string? nuevaRutaFisica = null;

            if (!esMovilidad &&
                archivo != null &&
                archivo.Length > 0)
            {
                var resultadoArchivo =
                    await GuardarComprobanteAsync(
                        archivo);

                if (!resultadoArchivo.Exito)
                {
                    TempData["error"] =
                        resultadoArchivo.Mensaje;

                    return RedirectToAction(
                        nameof(EditAdmin),
                        new
                        {
                            id = gasto.IdGasto
                        });
                }

                nuevaRutaComprobante =
                    resultadoArchivo.RutaPublica;

                nuevaRutaFisica =
                    resultadoArchivo.RutaFisica;
            }

            // =====================================================
            // GUARDAR REFERENCIA ANTERIOR
            // =====================================================

            var comprobanteAnterior =
                gasto.Comprobante;

            // =====================================================
            // ACTUALIZAR ENTIDAD
            // =====================================================

            gasto.Fecha =
                modelo.Fecha;

            gasto.IdTipoGasto =
                modelo.IdTipoGasto;

            gasto.IdTipoComprobante =
                esMovilidad
                    ? null
                    : modelo.IdTipoComprobante;

            gasto.Ruc =
                esMovilidad
                    ? null
                    : modelo.Ruc;

            gasto.RazonSocial =
                esMovilidad
                    ? null
                    : modelo.RazonSocial;

            gasto.DomicilioFiscal =
                esMovilidad
                    ? null
                    : modelo.DomicilioFiscal;

            gasto.Serie =
                esMovilidad
                    ? null
                    : modelo.Serie;

            gasto.Numero =
                esMovilidad
                    ? null
                    : modelo.Numero;

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

            // =====================================================
            // ACTUALIZAR COMPROBANTE
            // =====================================================

            if (esMovilidad)
            {
                gasto.Comprobante = null;
            }
            else if (!string.IsNullOrWhiteSpace(
                nuevaRutaComprobante))
            {
                gasto.Comprobante =
                    nuevaRutaComprobante;
            }

            // =====================================================
            // GUARDAR Y REGENERAR PDF
            // =====================================================

            try
            {
                await _context.SaveChangesAsync();

                await ActualizarTotalesRendicion(
                    gasto.IdRendicion);

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

                var gastosActualizados =
                    await _context.Gastos
                        .Include(g => g.TipoGasto)
                        .Include(g => g.TipoComprobante)
                        .Where(g =>
                            g.IdRendicion ==
                            gasto.IdRendicion)
                        .OrderBy(g => g.Fecha)
                        .ToListAsync();

                var devolucionActualizada =
                    await _context.DevolucionesSaldo
                        .FirstOrDefaultAsync(d =>
                            d.IdRendicion ==
                            gasto.IdRendicion);

                var bitacorasActualizadas =
                    await _context.BitacorasVehiculo
                        .Where(b =>
                            b.IdRendicion ==
                            gasto.IdRendicion)
                        .OrderBy(b => b.Fecha)
                        .ToListAsync();

                var resultadoPdf =
                    await _rendicionPdfService.GenerarAsync(
                        rendicionActualizada,
                        gastosActualizados,
                        devolucionActualizada,
                        bitacorasActualizadas);

                rendicionActualizada.ArchivoPdf =
                    $"{resultadoPdf.RutaPublica}?v={DateTime.Now.Ticks}";

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                EliminarArchivoFisico(
                    nuevaRutaFisica);

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

            // =====================================================
            // ELIMINAR COMPROBANTE ANTERIOR
            // =====================================================

            if (esMovilidad)
            {
                EliminarComprobante(
                    comprobanteAnterior);
            }
            else if (!string.IsNullOrWhiteSpace(
                nuevaRutaComprobante))
            {
                EliminarComprobante(
                    comprobanteAnterior);
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

        // =========================================================
        // ELIMINAR GASTO
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(
            int id,
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

            if (rendicion.IdEstadoRendicion !=
                ESTADO_RENDICION_BORRADOR)
            {
                TempData["error"] =
                    "No se pueden eliminar gastos de una rendición enviada.";

                return RedirectToAction(
                    nameof(Index),
                    new
                    {
                        idRendicion
                    });
            }

            var gasto =
                await _context.Gastos
                    .FirstOrDefaultAsync(g =>
                        g.IdGasto == id &&
                        g.IdRendicion == idRendicion);

            if (gasto == null)
            {
                TempData["error"] =
                    "No se encontró el gasto.";

                return RedirectToAction(
                    nameof(Index),
                    new
                    {
                        idRendicion
                    });
            }

            var comprobante =
                gasto.Comprobante;

            _context.Gastos.Remove(gasto);

            await _context.SaveChangesAsync();

            EliminarComprobante(
                comprobante);

            await ActualizarTotalesRendicion(
                idRendicion);

            TempData["mensaje"] =
                "Gasto eliminado correctamente.";

            return RedirectToAction(
                nameof(Index),
                new
                {
                    idRendicion
                });
        }

        // =========================================================
        // ENVIAR RENDICIÓN
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EnviarRendicion(
            int idRendicion)
        {
            var idUsuario =
                HttpContext.Session.GetInt32(
                    "IdUsuario");

            if (idUsuario == null)
            {
                TempData["error"] =
                    "La sesión ha expirado. Inicie sesión nuevamente.";

                return RedirectToAction(
                    "Index",
                    "Home");
            }

            var rendicion =
                await _context.Rendiciones
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

            if (rendicion.IdEstadoRendicion !=
                ESTADO_RENDICION_BORRADOR)
            {
                TempData["error"] =
                    "Esta rendición ya fue enviada o finalizada.";

                return RedirectToAction(
                    "MisRendiciones",
                    "Rendicion");
            }

            var gastos =
                await _context.Gastos
                    .Include(g => g.TipoGasto)
                    .Include(g => g.TipoComprobante)
                    .Where(g =>
                        g.IdRendicion ==
                        idRendicion)
                    .OrderBy(g => g.Fecha)
                    .ToListAsync();

            var bitacorasVehiculo =
                await _context.BitacorasVehiculo
                    .Where(b =>
                        b.IdRendicion ==
                        idRendicion)
                    .OrderBy(b => b.Fecha)
                    .ToListAsync();

            if (gastos.Count == 0 &&
                bitacorasVehiculo.Count == 0)
            {
                TempData["error"] =
                    "Debe registrar al menos un gasto o un recorrido de vehículo antes de enviar la rendición.";

                return RedirectToAction(
                    nameof(Index),
                    new
                    {
                        idRendicion
                    });
            }

            // =====================================================
            // DEVOLUCIÓN
            // =====================================================

            var devolucion =
                await _context.DevolucionesSaldo
                    .FirstOrDefaultAsync(d =>
                        d.IdRendicion ==
                        idRendicion);

            if (!ValidarDevolucion(
                rendicion,
                devolucion))
            {
                return RedirectToAction(
                    nameof(Index),
                    new
                    {
                        idRendicion
                    });
            }

            // =====================================================
            // REEMBOLSO
            // =====================================================

            await ProcesarReembolsoAsync(
                rendicion);

            // =====================================================
            // GENERAR PDF
            // =====================================================

            ResultadoPdfRendicion resultadoPdf;

            try
            {
                resultadoPdf =
                    await _rendicionPdfService.GenerarAsync(
                        rendicion,
                        gastos,
                        devolucion,
                        bitacorasVehiculo);
            }
            catch (Exception ex)
            {
                TempData["error"] =
                    "No se pudo generar el PDF de la liquidación. " +
                    ex.Message;

                return RedirectToAction(
                    nameof(Index),
                    new
                    {
                        idRendicion
                    });
            }

            // =====================================================
            // CAMBIAR ESTADO
            // =====================================================

            rendicion.ArchivoPdf =
                resultadoPdf.RutaPublica;

            rendicion.FechaEnvioRevision =
                DateTime.Now;

            rendicion.IdEstadoRendicion =
                ESTADO_RENDICION_PENDIENTE_REVISION;

            await _context.SaveChangesAsync();

            // =====================================================
            // CORREOS ADMINISTRADORES
            // =====================================================

            var correosAdministradores =
                await _context.Usuarios
                    .Where(u =>
                        u.IdRol == ROL_ADMINISTRADOR &&
                        u.Estado &&
                        !string.IsNullOrWhiteSpace(
                            u.Correo))
                    .Select(u => u.Correo!)
                    .ToListAsync();

            var nombreEmpleado =
                $"{rendicion.Usuario?.Nombres} " +
                $"{rendicion.Usuario?.Apellidos}";

            var totalBase =
                gastos.Sum(g =>
                    g.ValorVenta);

            var totalIgv =
                gastos.Sum(g =>
                    g.IGV);

            var totalGastosCorreo =
                gastos.Sum(g =>
                    g.MontoTotal);

            var totalVehiculoCorreo =
                bitacorasVehiculo.Sum(b =>
                    b.MontoAsignado);

            var totalRendidoCorreo =
                totalGastosCorreo +
                totalVehiculoCorreo;

            var saldoCorreo =
                (rendicion.Solicitud?.Monto ?? 0) -
                totalRendidoCorreo;

            var asunto =
                $"Liquidación de viáticos #{rendicion.IdRendicion} pendiente de revisión";

            var contenidoHtml =
                GenerarCorreoLiquidacion(
                    rendicion,
                    nombreEmpleado,
                    totalBase,
                    totalIgv,
                    saldoCorreo);

            // =====================================================
            // PREPARAR LOS DOS PDF PARA EL CORREO
            // =====================================================

            var adjuntosCorreo =
                new List<(string Ruta, string Nombre)>();

            // =====================================================
            // PDF PRINCIPAL DE LIQUIDACIÓN
            // =====================================================

            if (!string.IsNullOrWhiteSpace(
                    resultadoPdf.RutaFisica) &&
                System.IO.File.Exists(
                    resultadoPdf.RutaFisica))
            {
                adjuntosCorreo.Add(
                    (
                        resultadoPdf.RutaFisica,
                        resultadoPdf.NombreArchivo
                    ));
            }

            // =====================================================
            // PDF DE VOUCHERS
            // =====================================================

            if (!string.IsNullOrWhiteSpace(
                    resultadoPdf.RutaFisicaVouchers) &&
                System.IO.File.Exists(
                    resultadoPdf.RutaFisicaVouchers))
            {
                adjuntosCorreo.Add(
                    (
                        resultadoPdf.RutaFisicaVouchers,
                        resultadoPdf.NombreArchivoVouchers
                    ));
            }

            // =====================================================
            // ENVIAR CORREO CON LOS DOS PDF
            // =====================================================

            var correoEnviado =
                await _correoService.EnviarAsync(
                    correosAdministradores,
                    asunto,
                    contenidoHtml,
                    adjuntosCorreo);

            TempData["mensaje"] =
                correoEnviado
                    ? "La rendición fue enviada para revisión y los PDF de liquidación y vouchers fueron enviados a los administradores."
                    : "La rendición y los PDF fueron guardados, pero no fue posible enviar el correo.";

            return RedirectToAction(
                "MisRendiciones",
                "Rendicion");
        }

        // =========================================================
        // ACTUALIZAR TOTAL Y SALDO
        // =========================================================

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

            var totalGastos =
                await _context.Gastos
                    .Where(g =>
                        g.IdRendicion ==
                        idRendicion)
                    .SumAsync(g =>
                        (decimal?)g.MontoTotal) ?? 0m;

            var totalVehiculo =
                await _context.BitacorasVehiculo
                    .Where(b =>
                        b.IdRendicion ==
                        idRendicion)
                    .SumAsync(b =>
                        (decimal?)b.MontoAsignado) ?? 0m;

            rendicion.Total =
                totalGastos +
                totalVehiculo;

            rendicion.Saldo =
                rendicion.Solicitud.Monto -
                rendicion.Total;

            await _context.SaveChangesAsync();
        }

        // =========================================================
        // OBTENER RENDICIÓN
        // =========================================================

        private async Task<Rendicion?> ObtenerRendicionAsync(
            int idRendicion)
        {
            return await _context.Rendiciones
                .Include(r => r.Solicitud)
                .FirstOrDefaultAsync(r =>
                    r.IdRendicion ==
                    idRendicion);
        }

        // =========================================================
        // OBTENER TIPO DE GASTO
        // =========================================================

        private async Task<TipoGasto?> ObtenerTipoGastoAsync(
            int idTipoGasto)
        {
            return await _context.TipoGastos
                .FirstOrDefaultAsync(t =>
                    t.IdTipoGasto ==
                    idTipoGasto);
        }

        // =========================================================
        // DETERMINAR SI ES ADMINISTRADOR
        // =========================================================

        private bool EsAdministrador()
        {
            var idRol =
                HttpContext.Session.GetInt32(
                    "IdRol");

            return idRol == ROL_ADMINISTRADOR;
        }

        // =========================================================
        // DETERMINAR SI ES MOVILIDAD
        // =========================================================

        private static bool EsMovilidad(
            TipoGasto tipoGasto)
        {
            return tipoGasto.Nombre
                .Trim()
                .Equals(
                    "Movilidad",
                    StringComparison.OrdinalIgnoreCase);
        }

        // =========================================================
        // OBTENER LÍMITE DIARIO
        // =========================================================

        private static decimal ObtenerLimiteDiario(
            TipoGasto tipoGasto)
        {
            var nombre =
                tipoGasto.Nombre.Trim();

            if (nombre.Equals(
                "Alimentación",
                StringComparison.OrdinalIgnoreCase))
            {
                return LIMITE_ALIMENTACION_DIARIO;
            }

            if (nombre.Equals(
                "Hospedaje",
                StringComparison.OrdinalIgnoreCase))
            {
                return LIMITE_HOSPEDAJE_DIARIO;
            }

            return 0m;
        }

        // =========================================================
        // LIMPIAR CAMPOS
        // =========================================================

        private static void LimpiarCampos(
            Gasto gasto)
        {
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
        }

        // =========================================================
        // LIMPIAR DATOS DE COMPROBANTE
        // =========================================================

        private static void LimpiarDatosComprobante(
            Gasto gasto)
        {
            gasto.Ruc = null;
            gasto.RazonSocial = null;
            gasto.DomicilioFiscal = null;

            gasto.IdTipoComprobante = null;

            gasto.Serie = null;
            gasto.Numero = null;

            gasto.Comprobante = null;
        }

        // =========================================================
        // ELIMINAR VALIDACIONES CALCULADAS
        // =========================================================

        private void EliminarValidacionesCalculadas(
            params string[] propiedades)
        {
            foreach (var propiedad in propiedades)
            {
                ModelState.Remove(propiedad);
            }
        }

        // =========================================================
        // VALIDAR FECHA
        // =========================================================

        private void ValidarFechaGasto(
            DateTime fecha,
            Rendicion rendicion,
            bool incluirMensajeDetallado)
        {
            if (fecha == default)
            {
                ModelState.AddModelError(
                    nameof(Gasto.Fecha),
                    "Debe ingresar la fecha del gasto.");

                return;
            }

            if (fecha.Date <
                    rendicion.FechaInicio.Date ||
                fecha.Date >
                    rendicion.FechaFin.Date)
            {
                var mensaje =
                    $"La fecha del gasto debe estar entre " +
                    $"{rendicion.FechaInicio:dd/MM/yyyy} y " +
                    $"{rendicion.FechaFin:dd/MM/yyyy}.";

                if (incluirMensajeDetallado)
                {
                    mensaje +=
                        " Puede registrar el gasto posteriormente, " +
                        "pero la fecha del comprobante debe pertenecer " +
                        "al periodo aprobado.";
                }

                ModelState.AddModelError(
                    nameof(Gasto.Fecha),
                    mensaje);
            }
        }

        // =========================================================
        // VALIDAR MONTO
        // =========================================================

        private void ValidarMonto(
            decimal monto)
        {
            if (monto <= 0)
            {
                ModelState.AddModelError(
                    nameof(Gasto.MontoTotal),
                    "El monto total debe ser mayor que cero.");
            }
        }

        // =========================================================
        // CALCULAR IMPUESTOS
        // =========================================================

        private static void CalcularImpuestos(
            Gasto gasto)
        {
            if (gasto.MontoTotal <= 0)
            {
                gasto.ValorVenta = 0;
                gasto.IGV = 0;
                return;
            }

            if (gasto.ExoneracionIGV)
            {
                gasto.ValorVenta =
                    Math.Round(
                        gasto.MontoTotal,
                        2,
                        MidpointRounding.AwayFromZero);

                gasto.IGV = 0;

                return;
            }

            gasto.ValorVenta =
                Math.Round(
                    gasto.MontoTotal /
                    (1 + TASA_IGV),
                    2,
                    MidpointRounding.AwayFromZero);

            gasto.IGV =
                Math.Round(
                    gasto.MontoTotal -
                    gasto.ValorVenta,
                    2,
                    MidpointRounding.AwayFromZero);
        }

        // =========================================================
        // VALIDAR LÍMITE DIARIO
        // =========================================================

        private async Task ValidarLimiteDiarioAsync(
            Gasto gasto,
            TipoGasto tipoGasto,
            int idGastoExcluir)
        {
            var limiteDiario =
                ObtenerLimiteDiario(tipoGasto);

            if (limiteDiario <= 0)
            {
                return;
            }

            var inicioDia =
                gasto.Fecha.Date;

            var finDia =
                inicioDia.AddDays(1);

            var query =
                _context.Gastos
                    .Where(g =>
                        g.IdRendicion ==
                            gasto.IdRendicion &&
                        g.IdTipoGasto ==
                            gasto.IdTipoGasto &&
                        g.Fecha >= inicioDia &&
                        g.Fecha < finDia);

            if (idGastoExcluir > 0)
            {
                query =
                    query.Where(g =>
                        g.IdGasto !=
                        idGastoExcluir);
            }

            var montoRegistrado =
                await query
                    .SumAsync(g =>
                        (decimal?)g.MontoTotal) ?? 0m;

            var nuevoTotal =
                montoRegistrado +
                gasto.MontoTotal;

            if (nuevoTotal <= limiteDiario)
            {
                return;
            }

            var disponible =
                limiteDiario -
                montoRegistrado;

            if (disponible < 0)
            {
                disponible = 0;
            }

            ModelState.AddModelError(
                nameof(Gasto.MontoTotal),
                $"El límite diario para {tipoGasto.Nombre} es " +
                $"S/ {limiteDiario:N2}. " +
                $"El {gasto.Fecha:dd/MM/yyyy} ya tiene registrado " +
                $"S/ {montoRegistrado:N2}. " +
                $"Solo puede registrar hasta S/ {disponible:N2}.");
        }

        // =========================================================
        // VALIDAR DATOS DEL COMPROBANTE
        // =========================================================

        private void ValidarDatosComprobante(
            Gasto gasto)
        {
            if (string.IsNullOrWhiteSpace(
                gasto.Ruc))
            {
                ModelState.AddModelError(
                    nameof(Gasto.Ruc),
                    "Debe ingresar el RUC.");
            }
            else if (
                gasto.Ruc.Length != 11 ||
                !gasto.Ruc.All(char.IsDigit))
            {
                ModelState.AddModelError(
                    nameof(Gasto.Ruc),
                    "El RUC debe contener exactamente 11 dígitos.");
            }

            var tipoComprobanteExiste =
                _context.TipoComprobantes.Any(
                    t =>
                        t.IdTipoComprobante ==
                        gasto.IdTipoComprobante);

            if (!tipoComprobanteExiste)
            {
                ModelState.AddModelError(
                    nameof(Gasto.IdTipoComprobante),
                    "El tipo de comprobante seleccionado no existe.");
            }
        }

        // =========================================================
        // VALIDAR RUC
        // =========================================================

        private async Task ValidarRucAsync(
            Gasto gasto)
        {
            if (string.IsNullOrWhiteSpace(
                gasto.Ruc))
            {
                return;
            }

            var domicilioIngresado =
                gasto.DomicilioFiscal;

            var consulta =
                await _rucService.ConsultarAsync(
                    gasto.Ruc);

            if (!consulta.Exito)
            {
                ModelState.AddModelError(
                    nameof(Gasto.Ruc),
                    consulta.Mensaje ??
                    "No se pudo validar el RUC.");

                return;
            }

            gasto.Ruc =
                string.IsNullOrWhiteSpace(
                    consulta.Ruc)
                    ? gasto.Ruc
                    : consulta.Ruc.Trim();

            gasto.RazonSocial =
                consulta.RazonSocial?.Trim();

            if (string.IsNullOrWhiteSpace(
                domicilioIngresado))
            {
                gasto.DomicilioFiscal =
                    consulta.DomicilioFiscal?.Trim();
            }
            else
            {
                gasto.DomicilioFiscal =
                    domicilioIngresado.Trim();
            }
        }

        // =========================================================
        // VALIDAR PROVEEDOR
        // =========================================================

        private void ValidarDatosProveedor(
            Gasto gasto)
        {
            if (string.IsNullOrWhiteSpace(
                gasto.RazonSocial))
            {
                ModelState.AddModelError(
                    nameof(Gasto.RazonSocial),
                    "No se encontró la razón social del RUC.");
            }
            else if (
                gasto.RazonSocial.Length > 250)
            {
                ModelState.AddModelError(
                    nameof(Gasto.RazonSocial),
                    "La razón social no puede superar los 250 caracteres.");
            }

            if (string.IsNullOrWhiteSpace(
                gasto.DomicilioFiscal))
            {
                ModelState.AddModelError(
                    nameof(Gasto.DomicilioFiscal),
                    "Debe ingresar el domicilio fiscal.");
            }
            else if (
                gasto.DomicilioFiscal.Length > 300)
            {
                ModelState.AddModelError(
                    nameof(Gasto.DomicilioFiscal),
                    "El domicilio fiscal no puede superar los 300 caracteres.");
            }
        }

        // =========================================================
        // GUARDAR COMPROBANTE
        // =========================================================

        private async Task<ResultadoArchivo> GuardarComprobanteAsync(
            IFormFile? archivo)
        {
            if (archivo == null ||
                archivo.Length == 0)
            {
                return new ResultadoArchivo
                {
                    Exito = true,
                    RutaPublica = null,
                    RutaFisica = null
                };
            }

            var extension =
                Path.GetExtension(
                    archivo.FileName)
                .ToLowerInvariant();

            if (!ExtensionesPermitidas.Contains(
                extension))
            {
                return new ResultadoArchivo
                {
                    Exito = false,
                    Mensaje =
                        "El comprobante debe ser PDF, JPG, JPEG o PNG."
                };
            }

            if (archivo.Length >
                TAMANIO_MAXIMO_COMPROBANTE)
            {
                return new ResultadoArchivo
                {
                    Exito = false,
                    Mensaje =
                        "El comprobante no debe superar los 5 MB."
                };
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

            var rutaFisica =
                Path.Combine(
                    carpeta,
                    nombreArchivo);

            await using var stream =
                new FileStream(
                    rutaFisica,
                    FileMode.Create);

            await archivo.CopyToAsync(
                stream);

            return new ResultadoArchivo
            {
                Exito = true,

                RutaPublica =
                    $"/comprobantes/{nombreArchivo}",

                RutaFisica =
                    rutaFisica
            };
        }

        // =========================================================
        // ELIMINAR COMPROBANTE
        // =========================================================

        private void EliminarComprobante(
            string? rutaPublica)
        {
            if (string.IsNullOrWhiteSpace(
                rutaPublica))
            {
                return;
            }

            var rutaRelativa =
                rutaPublica
                    .TrimStart('/')
                    .Replace(
                        '/',
                        Path.DirectorySeparatorChar);

            var rutaFisica =
                Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot",
                    rutaRelativa);

            EliminarArchivoFisico(
                rutaFisica);
        }

        // =========================================================
        // ELIMINAR ARCHIVO FÍSICO
        // =========================================================

        private static void EliminarArchivoFisico(
            string? rutaFisica)
        {
            if (string.IsNullOrWhiteSpace(
                rutaFisica))
            {
                return;
            }

            if (!System.IO.File.Exists(
                rutaFisica))
            {
                return;
            }

            try
            {
                System.IO.File.Delete(
                    rutaFisica);
            }
            catch
            {
                // No detener la operación principal
                // si falla la eliminación física.
            }
        }

        // =========================================================
        // PROCESAR ERRORES DE CREATE
        // =========================================================

        private async Task<IActionResult> ProcesarErroresCreate(
            int idRendicion)
        {
            AgregarErroresTempData();

            return RedirectToAction(
                nameof(Index),
                new
                {
                    idRendicion
                });
        }

        // =========================================================
        // AGREGAR ERRORES A TEMPDATA
        // =========================================================

        private void AgregarErroresTempData()
        {
            var errores =
                ModelState
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
                string.Join(
                    "<br>",
                    errores);
        }

        // =========================================================
        // VALIDAR DEVOLUCIÓN
        // =========================================================

        private bool ValidarDevolucion(
            Rendicion rendicion,
            DevolucionSaldo? devolucion)
        {
            if (rendicion.Saldo <= 0)
            {
                return true;
            }

            if (devolucion == null)
            {
                TempData["error"] =
                    $"Debe registrar la devolución de " +
                    $"S/ {rendicion.Saldo:N2} antes de enviar.";

                return false;
            }

            if (devolucion.Monto !=
                rendicion.Saldo)
            {
                TempData["error"] =
                    $"El monto devuelto debe ser exactamente " +
                    $"S/ {rendicion.Saldo:N2}.";

                return false;
            }

            if (string.IsNullOrWhiteSpace(
                devolucion.Voucher))
            {
                TempData["error"] =
                    "La devolución debe tener un voucher adjunto.";

                return false;
            }

            return true;
        }

        // =========================================================
        // PROCESAR REEMBOLSO
        // =========================================================

        private async Task ProcesarReembolsoAsync(
            Rendicion rendicion)
        {
            if (rendicion.Saldo >= 0)
            {
                return;
            }

            var montoReembolso =
                Math.Abs(
                    rendicion.Saldo);

            var reembolsoExistente =
                await _context.Reembolsos
                    .FirstOrDefaultAsync(r =>
                        r.IdRendicion ==
                        rendicion.IdRendicion);

            if (reembolsoExistente == null)
            {
                var nuevoReembolso =
                    new Reembolso
                    {
                        IdRendicion =
                            rendicion.IdRendicion,

                        IdUsuario =
                            rendicion.IdUsuario,

                        Monto =
                            montoReembolso,

                        FechaSolicitud =
                            DateTime.Now,

                        IdEstadoReembolso =
                            ESTADO_REEMBOLSO_PENDIENTE
                    };

                _context.Reembolsos.Add(
                    nuevoReembolso);

                return;
            }

            reembolsoExistente.Monto =
                montoReembolso;

            reembolsoExistente.FechaSolicitud =
                DateTime.Now;

            reembolsoExistente.IdEstadoReembolso =
                ESTADO_REEMBOLSO_PENDIENTE;

            reembolsoExistente.FechaAprobacion =
                null;

            reembolsoExistente.FechaPago =
                null;

            reembolsoExistente.Banco =
                null;

            reembolsoExistente.NumeroOperacion =
                null;

            reembolsoExistente.ComprobantePago =
                null;

            reembolsoExistente.Observaciones =
                null;
        }

        // =========================================================
        // GENERAR CORREO DE LIQUIDACIÓN
        // =========================================================

        private static string GenerarCorreoLiquidacion(
            Rendicion rendicion,
            string nombreEmpleado,
            decimal totalBase,
            decimal totalIgv,
            decimal saldo)
        {
            return $"""
<!DOCTYPE html>
<html lang="es">
<head>
<meta charset="UTF-8">
<meta name="viewport"
      content="width=device-width,initial-scale=1.0">
</head>

<body style="
margin:0;
padding:0;
background-color:#f2f5f8;
font-family:Arial,Helvetica,sans-serif;
color:#111111;">

<table role="presentation"
       width="100%"
       cellspacing="0"
       cellpadding="0"
       border="0"
       style="
       background-color:#f2f5f8;
       padding:35px 15px;">

<tr>
<td align="center">

<table role="presentation"
       width="700"
       cellspacing="0"
       cellpadding="0"
       border="0"
       style="
       max-width:700px;
       width:100%;
       background:#ffffff;
       border-radius:12px;
       overflow:hidden;">

<!-- ENCABEZADO -->

<tr>
<td style="
background:#0C4A8A;
padding:25px 35px;
text-align:center;">

<div style="
background:#ffffff;
display:inline-block;
padding:10px 18px;
border-radius:8px;
margin-bottom:15px;">

<img src="cid:logoDinacen"
     alt="DINACEN"
     style="
     max-width:210px;
     height:auto;
     display:block;
     margin:0 auto;">

</div>

<div style="
height:2px;
background:#6AA84F;
width:100%;
margin-bottom:18px;">
</div>

<div style="
font-size:24px;
font-weight:bold;
color:#ffffff;">
LIQUIDACIÓN DE VIÁTICOS
</div>

<div style="
font-size:16px;
color:#ffffff;
margin-top:8px;">
Pendiente de revisión
</div>

</td>
</tr>

<!-- CONTENIDO -->

<tr>
<td style="
padding:35px 40px 20px 40px;">

<div style="
font-size:22px;
font-weight:bold;
color:#111111;
margin-bottom:18px;">

Nueva liquidación pendiente de revisión

</div>

<div style="
font-size:17px;
line-height:1.7;
color:#222222;
margin-bottom:25px;">

El empleado
<strong>{nombreEmpleado}</strong>
ha enviado una liquidación de gastos
que se encuentra pendiente de revisión.

</div>

<table role="presentation"
       width="100%"
       cellspacing="0"
       cellpadding="0"
       border="0"
       style="
       background:#f7f9fb;
       border:1px solid #d9e2ea;
       border-radius:8px;">

<tr>
<td colspan="2"
    style="
    padding:18px 20px;
    background:#eef4f9;
    border-bottom:1px solid #d9e2ea;">

<div style="
font-size:18px;
font-weight:bold;
color:#0C4A8A;">

Información de la liquidación

</div>

</td>
</tr>

<tr>
<td width="42%"
    style="
    padding:14px 20px;
    border-bottom:1px solid #e1e7ec;
    font-size:16px;
    font-weight:bold;">

Liquidación

</td>

<td style="
padding:14px 20px;
border-bottom:1px solid #e1e7ec;
font-size:16px;">

#{rendicion.IdRendicion}

</td>
</tr>

<tr>
<td style="
padding:14px 20px;
border-bottom:1px solid #e1e7ec;
font-size:16px;
font-weight:bold;">

Empleado

</td>

<td style="
padding:14px 20px;
border-bottom:1px solid #e1e7ec;
font-size:16px;">

{nombreEmpleado}

</td>
</tr>

<tr>
<td style="
padding:14px 20px;
border-bottom:1px solid #e1e7ec;
font-size:16px;
font-weight:bold;">

Destino

</td>

<td style="
padding:14px 20px;
border-bottom:1px solid #e1e7ec;
font-size:16px;">

{rendicion.Solicitud?.Destino}

</td>
</tr>

<tr>
<td style="
padding:14px 20px;
border-bottom:1px solid #e1e7ec;
font-size:16px;
font-weight:bold;">

Periodo

</td>

<td style="
padding:14px 20px;
border-bottom:1px solid #e1e7ec;
font-size:16px;">

{rendicion.FechaInicio:dd/MM/yyyy}
al
{rendicion.FechaFin:dd/MM/yyyy}

</td>
</tr>

<tr>
<td style="
padding:14px 20px;
border-bottom:1px solid #e1e7ec;
font-size:16px;
font-weight:bold;">

Monto aprobado

</td>

<td style="
padding:14px 20px;
border-bottom:1px solid #e1e7ec;
font-size:16px;
font-weight:bold;">

S/ {rendicion.Solicitud?.Monto:N2}

</td>
</tr>

<tr>
<td style="
padding:14px 20px;
border-bottom:1px solid #e1e7ec;
font-size:16px;
font-weight:bold;">

Valor de venta

</td>

<td style="
padding:14px 20px;
border-bottom:1px solid #e1e7ec;
font-size:16px;">

S/ {totalBase:N2}

</td>
</tr>

<tr>
<td style="
padding:14px 20px;
border-bottom:1px solid #e1e7ec;
font-size:16px;
font-weight:bold;">

IGV

</td>

<td style="
padding:14px 20px;
border-bottom:1px solid #e1e7ec;
font-size:16px;">

S/ {totalIgv:N2}

</td>
</tr>

<tr>
<td style="
padding:16px 20px;
border-bottom:1px solid #e1e7ec;
font-size:17px;
font-weight:bold;">

Total rendido

</td>

<td style="
padding:16px 20px;
border-bottom:1px solid #e1e7ec;
font-size:19px;
font-weight:bold;
color:#0C4A8A;">

S/ {rendicion.Total:N2}

</td>
</tr>

<tr>
<td style="
padding:16px 20px;
font-size:17px;
font-weight:bold;">

Saldo

</td>

<td style="
padding:16px 20px;
font-size:19px;
font-weight:bold;
color:#6AA84F;">

S/ {saldo:N2}

</td>
</tr>

</table>

<div style="
margin-top:28px;
padding:20px;
background:#f7f9fb;
border-left:5px solid #0C4A8A;
border-radius:5px;">

<div style="
font-size:16px;
line-height:1.7;
color:#222222;">

Se adjuntan los PDF correspondientes a la
<strong>liquidación</strong> y a los
<strong>vouchers</strong> registrados.

Ingrese al <strong>sistema de gestión de viáticos DINACEN</strong>
para revisar los comprobantes, verificar la devolución
y proceder con la aprobación o rechazo de la rendición.

</div>

</div>

</td>
</tr>

<!-- PIE -->

<tr>
<td style="
background:#0C4A8A;
padding:25px 35px;
text-align:center;">

<div style="
font-size:16px;
font-weight:bold;
color:#ffffff;">

DINACEN

</div>

<div style="
font-size:13px;
color:#dce8f2;
margin-top:7px;">

Sistema de Gestión de Viáticos

</div>

<div style="
font-size:12px;
color:#c5d5e2;
margin-top:12px;">

Este mensaje ha sido generado automáticamente.
Por favor, no responda a este correo.

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
        }

        // =========================================================
        // RESULTADO PARA ARCHIVOS
        // =========================================================

        private sealed class ResultadoArchivo
        {
            public bool Exito { get; set; }

            public string? Mensaje { get; set; }

            public string? RutaPublica { get; set; }

            public string? RutaFisica { get; set; }
        }
    }
}