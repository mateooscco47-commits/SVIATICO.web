using Dinacem.Models;
using Dinacem.Models.Servicios;
using ImageMagick;
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
        private readonly ILogger<GastoController> _logger;

        private const int ROL_ADMINISTRADOR = 1;

        private const int ESTADO_RENDICION_BORRADOR = 1;
        private const int ESTADO_RENDICION_PENDIENTE_REVISION = 2;
        private const int ESTADO_RENDICION_RECHAZADA = 4;

        private const int ESTADO_REEMBOLSO_PENDIENTE = 1;

        private const decimal LIMITE_ALIMENTACION_DIARIO = 40m;
        private const decimal LIMITE_MOVILIDAD_INTERNA_DIARIO = 10m;
        private const decimal LIMITE_HOSPEDAJE_POR_DIA = 50m;

        private const decimal TASA_IGV = 0.18m;

        private const long TAMANIO_MAXIMO_COMPROBANTE = 5 * 1024 * 1024;

        private static readonly string[] ExtensionesPermitidas =
        {
            ".pdf",
            ".jpg",
            ".jpeg",
            ".png"
        };

        public GastoController(
            AplicacionDbContexto context,
            RucService rucService,
            RendicionPdfService rendicionPdfService,
            CorreoService correoService,
            ILogger<GastoController> logger)
        {
            _context = context;
            _rucService = rucService;
            _rendicionPdfService = rendicionPdfService;
            _correoService = correoService;
            _logger = logger;
        }

        // =========================================================
        // MOSTRAR RENDICIÓN Y GASTOS
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Index(int idRendicion)
        {
            var idUsuario = HttpContext.Session.GetInt32("IdUsuario");

            if (!idUsuario.HasValue)
            {
                TempData["error"] =
                    "La sesión ha expirado. Inicie sesión nuevamente.";

                return RedirectToAction("Login", "Cuenta");
            }

            var esAdministrador = EsAdministrador();

            var consulta = _context.Rendiciones
                .Include(r => r.Solicitud)
                .Include(r => r.EstadoRendicion)
                .Include(r => r.Usuario)
                .Where(r => r.IdRendicion == idRendicion);

            if (!esAdministrador)
            {
                consulta = consulta.Where(r =>
                    r.IdUsuario == idUsuario.Value);
            }

            var rendicion = await consulta.FirstOrDefaultAsync();

            if (rendicion == null)
            {
                TempData["error"] =
                    "No se encontró la rendición o no tiene permisos para acceder a ella.";

                return RedirectToAction("Index", "Rendicion");
            }

            var gastos = await _context.Gastos
                .Include(g => g.TipoGasto)
                .Include(g => g.TipoComprobante)
                .Where(g => g.IdRendicion == idRendicion)
                .OrderByDescending(g => g.Fecha)
                .ToListAsync();

            ViewBag.Rendicion = rendicion;

            ViewBag.TiposGasto = await _context.TipoGastos
                .OrderBy(t => t.Nombre)
                .ToListAsync();

            ViewBag.TiposComprobante = await _context.TipoComprobantes
                .OrderBy(t => t.Nombre)
                .ToListAsync();

            ViewBag.DevolucionSaldo =
                await _context.DevolucionesSaldo
                    .FirstOrDefaultAsync(d =>
                        d.IdRendicion == idRendicion);

            ViewBag.BitacorasVehiculo =
                await _context.BitacorasVehiculo
                    .Where(b => b.IdRendicion == idRendicion)
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

            if (ruc.Length != 11 || !ruc.All(char.IsDigit))
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
            var idUsuario =
                HttpContext.Session.GetInt32("IdUsuario");

            if (!idUsuario.HasValue)
            {
                TempData["error"] =
                    "La sesión ha expirado. Inicie sesión nuevamente.";

                return RedirectToAction("Login", "Cuenta");
            }

            var rendicion =
                await ObtenerRendicionAsync(
                    gasto.IdRendicion,
                    idUsuario.Value);

            if (rendicion == null)
            {
                TempData["error"] =
                    "No se encontró la rendición o no pertenece al usuario conectado.";

                return RedirectToAction(
                    "MisRendiciones",
                    "Rendicion");
            }

            if (rendicion.IdEstadoRendicion != ESTADO_RENDICION_BORRADOR &&
                rendicion.IdEstadoRendicion != ESTADO_RENDICION_RECHAZADA)
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

            bool esMovilidadInterna =
                EsMovilidadInterna(tipoGasto);

            bool esHospedaje =
                EsHospedaje(tipoGasto);

            bool esOtros =
                EsOtros(tipoGasto);

            ValidarFechaGasto(
                gasto.Fecha,
                rendicion,
                true);

            ValidarMonto(
                gasto.MontoTotal);

            CalcularImpuestos(gasto);

            if (esMovilidadInterna)
            {
                LimpiarDatosComprobante(gasto);
                LimpiarDatosHospedaje(gasto);
            }
            else if (esMovilidad)
            {
                LimpiarDatosHospedaje(gasto);
            }
            else if (!esHospedaje)
            {
                LimpiarDatosHospedaje(gasto);
            }

            if (esHospedaje)
            {
                await ValidarHospedajeAsync(
                    gasto,
                    rendicion,
                    gasto.IdGasto,
                    true);
            }
            else if (gasto.Fecha != default)
            {
                await ValidarLimiteDiarioAsync(
                    gasto,
                    tipoGasto,
                    gasto.IdGasto);
            }

            // =========================================================
            // VALIDACIÓN DE COMPROBANTE
            // =========================================================

            if (esMovilidadInterna)
            {
                LimpiarDatosComprobante(gasto);
            }
            else if (esOtros)
            {
                // En "Otros" el comprobante es opcional.
                // Si se ingresa RUC, se valida normalmente.
                if (ModelState.IsValid &&
                    !string.IsNullOrWhiteSpace(gasto.Ruc))
                {
                    await ValidarRucAsync(gasto);
                }

                ValidarDatosProveedor(gasto);
            }
            else
            {
                // Para los demás tipos el comprobante sigue siendo obligatorio.
                ValidarDatosComprobante(gasto);

                if (ModelState.IsValid &&
                    !string.IsNullOrWhiteSpace(gasto.Ruc))
                {
                    await ValidarRucAsync(gasto);
                }

                ValidarDatosProveedor(gasto);

                if (archivo == null || archivo.Length == 0)
                {
                    ModelState.AddModelError(
                        "archivo",
                        "Debe adjuntar el comprobante.");
                }
            }

            if (!ModelState.IsValid)
            {
                return await ProcesarErroresCreate(
                    gasto.IdRendicion);
            }

            // =========================================================
            // GUARDAR COMPROBANTE
            // =========================================================

            if (!esMovilidadInterna &&
                archivo != null &&
                archivo.Length > 0)
            {
                var resultadoArchivo =
                    await GuardarComprobanteAsync(archivo);

                if (!resultadoArchivo.Exito)
                {
                    TempData["error"] =
                        resultadoArchivo.Mensaje;

                    return RedirectToAction(
                        nameof(Index),
                        new
                        {
                            idRendicion = gasto.IdRendicion
                        });
                }

                gasto.Comprobante =
                    resultadoArchivo.RutaPublica;
            }
            else
            {
                gasto.Comprobante = null;
            }

            _context.Gastos.Add(gasto);

            await _context.SaveChangesAsync();

            await ActualizarTotalesRendicion(
                gasto.IdRendicion);

            TempData["mensaje"] =
                gasto.ExoneracionIGV
                    ? $"Gasto registrado. Operación exonerada: valor de venta S/ {gasto.ValorVenta:N2}, IGV S/ 0.00."
                    : $"Gasto registrado. Valor de venta: S/ {gasto.ValorVenta:N2}, IGV: S/ {gasto.IGV:N2}.";

            return RedirectToAction(
                nameof(Index),
                new
                {
                    idRendicion = gasto.IdRendicion
                });
        }

        // =========================================================
        // EDITAR GASTO - EMPLEADO
        // =========================================================
        // Este método NO muestra Edit.cshtml.
        // Redirige al mismo panel de Registro de Gastos,
        // indicando qué gasto debe cargarse para edición.
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var idUsuario =
                HttpContext.Session.GetInt32("IdUsuario");

            if (!idUsuario.HasValue)
            {
                return RedirectToAction(
                    "Login",
                    "Cuenta");
            }

            var gasto =
                await _context.Gastos
                    .Include(g => g.Rendicion)
                    .FirstOrDefaultAsync(g =>
                        g.IdGasto == id);

            if (gasto == null)
            {
                TempData["error"] =
                    "El gasto no existe.";

                return RedirectToAction(
                    nameof(Index));
            }

            if (gasto.Rendicion == null)
            {
                TempData["error"] =
                    "La rendición asociada al gasto no existe.";

                return RedirectToAction(
                    nameof(Index));
            }

            // =====================================================
            // VERIFICAR PROPIETARIO
            // =====================================================

            if (gasto.Rendicion.IdUsuario != idUsuario.Value)
            {
                TempData["error"] =
                    "No tiene permiso para editar este gasto.";

                return RedirectToAction(
                    nameof(Index),
                    new
                    {
                        idRendicion = gasto.IdRendicion
                    });
            }

            // =====================================================
            // VERIFICAR ESTADO DE RENDICIÓN
            // =====================================================

            if (gasto.Rendicion.IdEstadoRendicion !=
                    ESTADO_RENDICION_BORRADOR &&
                gasto.Rendicion.IdEstadoRendicion !=
                    ESTADO_RENDICION_RECHAZADA)
            {
                TempData["error"] =
                    "Este gasto no puede editarse porque la rendición ya fue enviada o finalizada.";

                return RedirectToAction(
                    nameof(Index),
                    new
                    {
                        idRendicion = gasto.IdRendicion
                    });
            }

            // =====================================================
            // IR AL MISMO PANEL DE REGISTRO
            // =====================================================
            //
            // Index deberá recibir:
            //
            // idRendicion = rendición actual
            // idGasto     = gasto que se quiere editar
            //
            // De esta manera NO se abre Edit.cshtml.
            // El mismo formulario de Registrar Gasto deberá
            // cargarse con los datos de este IdGasto.
            // =====================================================

            return RedirectToAction(
                nameof(Index),
                new
                {
                    idRendicion = gasto.IdRendicion,
                    idGasto = gasto.IdGasto
                });
        }


        // =========================================================
        // GUARDAR EDICIÓN - EMPLEADO
        // =========================================================
        // IMPORTANTE:
        // Este método ACTUALIZA el mismo gasto.
        // NO crea un nuevo registro.
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            Gasto modelo,
            IFormFile? archivo)
        {
            var idUsuario =
                HttpContext.Session.GetInt32("IdUsuario");

            if (!idUsuario.HasValue)
            {
                return RedirectToAction(
                    "Login",
                    "Cuenta");
            }

            // =====================================================
            // VALIDAR ID DEL GASTO
            // =====================================================

            if (modelo.IdGasto <= 0)
            {
                TempData["error"] =
                    "No se indicó el gasto que se desea editar.";

                return RedirectToAction(
                    nameof(Index),
                    new
                    {
                        idRendicion = modelo.IdRendicion
                    });
            }

            // =====================================================
            // BUSCAR GASTO EXISTENTE
            // =====================================================

            var gasto =
                await _context.Gastos
                    .Include(g => g.Rendicion)
                        .ThenInclude(r => r.Solicitud)
                    .FirstOrDefaultAsync(g =>
                        g.IdGasto == modelo.IdGasto);

            if (gasto == null)
            {
                TempData["error"] =
                    "No se encontró el gasto que desea editar.";

                return RedirectToAction(
                    nameof(Index),
                    new
                    {
                        idRendicion = modelo.IdRendicion
                    });
            }

            if (gasto.Rendicion == null)
            {
                TempData["error"] =
                    "La rendición asociada al gasto no existe.";

                return RedirectToAction(
                    nameof(Index));
            }

            // =====================================================
            // VERIFICAR PROPIETARIO
            // =====================================================

            if (gasto.Rendicion.IdUsuario != idUsuario.Value)
            {
                TempData["error"] =
                    "No tiene permiso para editar este gasto.";

                return RedirectToAction(
                    nameof(Index),
                    new
                    {
                        idRendicion = gasto.IdRendicion
                    });
            }

            // =====================================================
            // VERIFICAR ESTADO DE RENDICIÓN
            // =====================================================

            if (gasto.Rendicion.IdEstadoRendicion !=
                    ESTADO_RENDICION_BORRADOR &&
                gasto.Rendicion.IdEstadoRendicion !=
                    ESTADO_RENDICION_RECHAZADA)
            {
                TempData["error"] =
                    "La rendición ya fue enviada o finalizada y no permite modificar gastos.";

                return RedirectToAction(
                    nameof(Index),
                    new
                    {
                        idRendicion = gasto.IdRendicion
                    });
            }

            // =====================================================
            // LIMPIAR VALIDACIONES DE CAMPOS CALCULADOS
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

            var rendicion =
                gasto.Rendicion;

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

            bool esMovilidadInterna =
                tipoGasto != null &&
                EsMovilidadInterna(tipoGasto);

            bool esHospedaje =
                tipoGasto != null &&
                EsHospedaje(tipoGasto);

            // =====================================================
            // VALIDAR FECHA
            // =====================================================

            ValidarFechaGasto(
                modelo.Fecha,
                rendicion,
                true);

            // =====================================================
            // VALIDAR MONTO
            // =====================================================

            ValidarMonto(
                modelo.MontoTotal);

            // =====================================================
            // CALCULAR IGV / VALOR DE VENTA
            // =====================================================

            CalcularImpuestos(modelo);

            // =====================================================
            // LIMPIAR CAMPOS SEGÚN TIPO DE GASTO
            // =====================================================

            if (esMovilidadInterna)
            {
                LimpiarDatosComprobante(modelo);
                LimpiarDatosHospedaje(modelo);
            }
            else if (esMovilidad)
            {
                LimpiarDatosHospedaje(modelo);
            }
            else if (!esHospedaje)
            {
                LimpiarDatosHospedaje(modelo);
            }

            // =====================================================
            // VALIDACIONES ESPECÍFICAS DEL GASTO
            // =====================================================

            if (tipoGasto != null)
            {
                if (esHospedaje)
                {
                    await ValidarHospedajeAsync(
                        modelo,
                        rendicion,
                        gasto.IdGasto,
                        true);
                }
                else if (modelo.Fecha != default)
                {
                    await ValidarLimiteDiarioAsync(
                        modelo,
                        tipoGasto,
                        gasto.IdGasto);
                }
            }

            // =====================================================
            // MOVILIDAD INTERNA
            // =====================================================

            if (esMovilidadInterna)
            {
                LimpiarDatosComprobante(modelo);
            }
            else
            {
                // =================================================
                // COMPROBANTE
                // =================================================

                ValidarDatosComprobante(modelo);

                // =================================================
                // RUC
                // =================================================

                if (ModelState.IsValid &&
                    !string.IsNullOrWhiteSpace(modelo.Ruc))
                {
                    await ValidarRucAsync(modelo);
                }

                // =================================================
                // PROVEEDOR
                // =================================================

                ValidarDatosProveedor(modelo);

                // =================================================
                // COMPROBANTE EXISTENTE
                // =================================================

                bool existeComprobanteAnterior =
                    !string.IsNullOrWhiteSpace(
                        gasto.Comprobante);

                bool seSubioNuevoComprobante =
                    archivo != null &&
                    archivo.Length > 0;

                // Si ya existe comprobante, no es obligatorio
                // volver a subirlo.
                if (!existeComprobanteAnterior &&
                    !seSubioNuevoComprobante)
                {
                    ModelState.AddModelError(
                        "archivo",
                        "Debe adjuntar el comprobante.");
                }
            }

            // =====================================================
            // VALIDAR MODELO
            // =====================================================

            if (!ModelState.IsValid)
            {
                AgregarErroresTempData();

                // Volvemos al MISMO panel.
                // El gasto queda seleccionado para edición.
                return RedirectToAction(
                    nameof(Index),
                    new
                    {
                        idRendicion = gasto.IdRendicion,
                        idGasto = gasto.IdGasto
                    });
            }

            // =====================================================
            // GUARDAR NUEVO COMPROBANTE
            // =====================================================

            string? nuevaRutaComprobante = null;
            string? nuevaRutaFisica = null;

            if (!esMovilidadInterna &&
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
                        nameof(Index),
                        new
                        {
                            idRendicion = gasto.IdRendicion,
                            idGasto = gasto.IdGasto
                        });
                }

                nuevaRutaComprobante =
                    resultadoArchivo.RutaPublica;

                nuevaRutaFisica =
                    resultadoArchivo.RutaFisica;
            }

            // =====================================================
            // GUARDAR REFERENCIA DEL COMPROBANTE ANTERIOR
            // =====================================================

            var comprobanteAnterior =
                gasto.Comprobante;

            // =====================================================
            // ACTUALIZAR EL MISMO GASTO
            // =====================================================

            gasto.Fecha =
                modelo.Fecha;

            gasto.IdTipoGasto =
                modelo.IdTipoGasto;

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
            // DATOS DE COMPROBANTE
            // =====================================================

            if (esMovilidadInterna)
            {
                gasto.Ruc = null;

                gasto.RazonSocial = null;

                gasto.DomicilioFiscal = null;

                gasto.IdTipoComprobante = null;

                gasto.Serie = null;

                gasto.Numero = null;

                gasto.Comprobante = null;
            }
            else
            {
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

                // Solo reemplazar comprobante si se subió
                // uno nuevo.
                if (!string.IsNullOrWhiteSpace(
                    nuevaRutaComprobante))
                {
                    gasto.Comprobante =
                        nuevaRutaComprobante;
                }
            }

            // =====================================================
            // DATOS DE HOSPEDAJE
            // =====================================================

            if (esHospedaje)
            {
                gasto.FechaInicioHospedaje =
                    modelo.FechaInicioHospedaje;

                gasto.FechaFinHospedaje =
                    modelo.FechaFinHospedaje;

                gasto.DiasHospedaje =
                    modelo.DiasHospedaje;
            }
            else
            {
                gasto.FechaInicioHospedaje = null;

                gasto.FechaFinHospedaje = null;

                gasto.DiasHospedaje = 0;
            }

            // =====================================================
            // GUARDAR CAMBIOS
            // =====================================================

            try
            {
                await _context.SaveChangesAsync();

                await ActualizarTotalesRendicion(
                    gasto.IdRendicion);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error actualizando gasto {IdGasto}.",
                    gasto.IdGasto);

                // Si se subió un archivo nuevo pero la BD falló,
                // eliminamos el archivo nuevo para no dejar
                // archivos huérfanos.
                EliminarArchivoFisico(
                    nuevaRutaFisica);

                TempData["error"] =
                    "No se pudo actualizar el gasto.";

                return RedirectToAction(
                    nameof(Index),
                    new
                    {
                        idRendicion = gasto.IdRendicion,
                        idGasto = gasto.IdGasto
                    });
            }

            // =====================================================
            // ELIMINAR COMPROBANTE ANTERIOR
            // =====================================================

            if (esMovilidadInterna ||
                !string.IsNullOrWhiteSpace(
                    nuevaRutaComprobante))
            {
                EliminarComprobante(
                    comprobanteAnterior);
            }

            // =====================================================
            // MENSAJE
            // =====================================================

            TempData["mensaje"] =
                "El gasto fue actualizado correctamente.";

            // =====================================================
            // VOLVER AL PANEL PRINCIPAL
            //
            // No enviamos idGasto para que el formulario
            // aparezca nuevamente limpio y listo para registrar
            // otro gasto.
            // =====================================================

            return RedirectToAction(
                nameof(Index),
                new
                {
                    idRendicion = gasto.IdRendicion
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
        // GUARDAR EDICIÓN - ADMINISTRADOR
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

            if (gasto.Rendicion == null)
            {
                TempData["error"] =
                    "No se encontró la rendición asociada al gasto.";

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

            LimpiarCampos(modelo);

            ModelState.Clear();

            var tipoGasto =
                await ObtenerTipoGastoAsync(
                    modelo.IdTipoGasto);

            if (tipoGasto == null)
            {
                TempData["error"] =
                    "El tipo de gasto seleccionado no existe.";

                return RedirectToAction(
                    nameof(EditAdmin),
                    new
                    {
                        id = gasto.IdGasto
                    });
            }

            bool esMovilidadInterna =
                EsMovilidadInterna(tipoGasto);

            bool esHospedaje =
                EsHospedaje(tipoGasto);

            // =========================================================
            // VALIDAR Y CALCULAR HOSPEDAJE
            // =========================================================

            if (esHospedaje)
            {
                if (!modelo.FechaInicioHospedaje.HasValue ||
                    !modelo.FechaFinHospedaje.HasValue)
                {
                    TempData["error"] =
                        "Debe indicar la fecha de inicio y la fecha de fin del hospedaje.";

                    return RedirectToAction(
                        nameof(EditAdmin),
                        new
                        {
                            id = gasto.IdGasto
                        });
                }

                if (modelo.FechaFinHospedaje.Value <
                    modelo.FechaInicioHospedaje.Value)
                {
                    TempData["error"] =
                        "La fecha de fin del hospedaje no puede ser anterior a la fecha de inicio.";

                    return RedirectToAction(
                        nameof(EditAdmin),
                        new
                        {
                            id = gasto.IdGasto
                        });
                }

                modelo.DiasHospedaje =
                    (modelo.FechaFinHospedaje.Value.Date -
                     modelo.FechaInicioHospedaje.Value.Date)
                    .Days + 1;
            }
            else
            {
                modelo.FechaInicioHospedaje = null;
                modelo.FechaFinHospedaje = null;
                modelo.DiasHospedaje = 0;
            }

            // =========================================================
            // CALCULAR IMPUESTOS
            // =========================================================

            CalcularImpuestos(modelo);

            string? nuevaRutaComprobante = null;
            string? nuevaRutaFisica = null;

            // =========================================================
            // GUARDAR NUEVO COMPROBANTE
            // =========================================================

            if (!esMovilidadInterna &&
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

            var comprobanteAnterior =
                gasto.Comprobante;

            // =========================================================
            // ACTUALIZAR DATOS GENERALES
            // =========================================================

            gasto.Fecha =
                modelo.Fecha;

            gasto.IdTipoGasto =
                modelo.IdTipoGasto;

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

            // =========================================================
            // DATOS DEL COMPROBANTE
            // =========================================================

            if (esMovilidadInterna)
            {
                gasto.Ruc = null;
                gasto.RazonSocial = null;
                gasto.DomicilioFiscal = null;
                gasto.IdTipoComprobante = null;
                gasto.Serie = null;
                gasto.Numero = null;
                gasto.Comprobante = null;
            }
            else
            {
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

                // Si se cargó un nuevo comprobante,
                // reemplazar el anterior.
                if (!string.IsNullOrWhiteSpace(
                    nuevaRutaComprobante))
                {
                    gasto.Comprobante =
                        nuevaRutaComprobante;
                }
            }

            // =========================================================
            // DATOS DEL PERÍODO DE HOSPEDAJE
            // =========================================================

            if (esHospedaje)
            {
                gasto.FechaInicioHospedaje =
                    modelo.FechaInicioHospedaje;

                gasto.FechaFinHospedaje =
                    modelo.FechaFinHospedaje;

                gasto.DiasHospedaje =
                    modelo.DiasHospedaje;
            }
            else
            {
                gasto.FechaInicioHospedaje = null;
                gasto.FechaFinHospedaje = null;
                gasto.DiasHospedaje = 0;
            }

            try
            {
                // =====================================================
                // GUARDAR CAMBIOS
                // =====================================================

                await _context.SaveChangesAsync();

                // =====================================================
                // ACTUALIZAR TOTALES DE LA RENDICIÓN
                // =====================================================

                await ActualizarTotalesRendicion(
                    gasto.IdRendicion);

                // =====================================================
                // RECARGAR RENDICIÓN
                // =====================================================

                var rendicionActualizada =
                    await _context.Rendiciones
                        .Include(r => r.Solicitud)
                        .Include(r => r.Usuario)
                        .FirstOrDefaultAsync(r =>
                            r.IdRendicion ==
                            gasto.IdRendicion);

                if (rendicionActualizada == null)
                {
                    EliminarArchivoFisico(
                        nuevaRutaFisica);

                    TempData["error"] =
                        "El gasto fue actualizado, pero no se pudo cargar la rendición.";

                    return RedirectToAction(
                        "DetalleAdmin",
                        "Rendicion",
                        new
                        {
                            id = gasto.IdRendicion
                        });
                }

                // =====================================================
                // RECARGAR GASTOS
                // =====================================================

                var gastosActualizados =
                    await _context.Gastos
                        .Include(g => g.TipoGasto)
                        .Include(g => g.TipoComprobante)
                        .Where(g =>
                            g.IdRendicion ==
                            gasto.IdRendicion)
                        .OrderBy(g => g.Fecha)
                        .ToListAsync();

                // =====================================================
                // RECARGAR DEVOLUCIÓN
                // =====================================================

                var devolucionActualizada =
                    await _context.DevolucionesSaldo
                        .FirstOrDefaultAsync(d =>
                            d.IdRendicion ==
                            gasto.IdRendicion);

                // =====================================================
                // RECARGAR BITÁCORAS
                // =====================================================

                var bitacorasActualizadas =
                    await _context.BitacorasVehiculo
                        .Where(b =>
                            b.IdRendicion ==
                            gasto.IdRendicion)
                        .OrderBy(b => b.Fecha)
                        .ToListAsync();

                // =====================================================
                // REGENERAR PDF
                // =====================================================

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
                _logger.LogError(
                    ex,
                    "Error actualizando gasto {IdGasto}.",
                    gasto.IdGasto);

                EliminarArchivoFisico(
                    nuevaRutaFisica);

                TempData["error"] =
                    "No se pudo actualizar completamente el gasto.";

                return RedirectToAction(
                    nameof(EditAdmin),
                    new
                    {
                        id = gasto.IdGasto
                    });
            }

            // =========================================================
            // ELIMINAR COMPROBANTE ANTERIOR
            // =========================================================

            if (esMovilidadInterna ||
                !string.IsNullOrWhiteSpace(
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
            var idUsuario =
                HttpContext.Session.GetInt32("IdUsuario");

            if (!idUsuario.HasValue)
            {
                TempData["error"] =
                    "La sesión ha expirado.";

                return RedirectToAction(
                    "Login",
                    "Cuenta");
            }

            var rendicion =
                await ObtenerRendicionAsync(
                    idRendicion,
                    idUsuario.Value);

            if (rendicion == null)
            {
                TempData["error"] =
                    "No se encontró la rendición o no pertenece al usuario conectado.";

                return RedirectToAction(
                    "MisRendiciones",
                    "Rendicion");
            }

            if (rendicion.IdEstadoRendicion !=
                    ESTADO_RENDICION_BORRADOR &&
                rendicion.IdEstadoRendicion !=
                    ESTADO_RENDICION_RECHAZADA)
            {
                TempData["error"] =
                    "No se pueden eliminar gastos de una rendición enviada o finalizada.";

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
            var inicio = DateTime.Now;

            var idUsuario =
                HttpContext.Session.GetInt32("IdUsuario");

            if (!idUsuario.HasValue)
            {
                TempData["error"] =
                    "La sesión ha expirado. Inicie sesión nuevamente.";

                return RedirectToAction(
                    "Login",
                    "Cuenta");
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
                    "MisRendiciones",
                    "Rendicion");
            }

            if (rendicion.IdEstadoRendicion !=
                    ESTADO_RENDICION_BORRADOR &&
                rendicion.IdEstadoRendicion !=
                    ESTADO_RENDICION_RECHAZADA)
            {
                TempData["error"] =
                    "La rendición ya fue enviada o finalizada y no permite registrar más gastos.";

                return RedirectToAction(
                    nameof(Index),
                    new
                    {
                        idRendicion
                    });
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

            var totalGastos =
                gastos.Sum(g =>
                    g.MontoTotal);

            var totalVehiculo =
                bitacorasVehiculo.Sum(b =>
                    b.MontoAsignado);

            var totalRendido =
                totalGastos +
                totalVehiculo;

            var montoAprobado =
                rendicion.Solicitud?.Monto ??
                0m;

            var saldo =
                montoAprobado -
                totalRendido;

            rendicion.Total =
                totalRendido;

            rendicion.Saldo =
                saldo;

            _logger.LogInformation(
                "Rendición {IdRendicion}: Gastos={Gastos}, Vehículo={Vehiculo}, Total={Total}, Aprobado={Aprobado}, Saldo={Saldo}",
                idRendicion,
                totalGastos,
                totalVehiculo,
                totalRendido,
                montoAprobado,
                saldo);

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

            await ProcesarReembolsoAsync(
                rendicion);

            ResultadoPdfRendicion resultadoPdf;

            try
            {
                var inicioPdf =
                    DateTime.Now;

                resultadoPdf =
                    await _rendicionPdfService.GenerarAsync(
                        rendicion,
                        gastos,
                        devolucion,
                        bitacorasVehiculo);

                _logger.LogInformation(
                    "PDF de rendición {IdRendicion} generado en {Tiempo} ms.",
                    idRendicion,
                    (DateTime.Now - inicioPdf).TotalMilliseconds);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error generando PDF de rendición {IdRendicion}.",
                    idRendicion);

                TempData["error"] =
                    "No se pudo generar el PDF de la liquidación.";

                return RedirectToAction(
                    nameof(Index),
                    new
                    {
                        idRendicion
                    });
            }

            rendicion.ArchivoPdf =
                resultadoPdf.RutaPublica;

            rendicion.FechaEnvioRevision =
                DateTime.Now;

            rendicion.IdEstadoRendicion =
                ESTADO_RENDICION_PENDIENTE_REVISION;

            try
            {
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Rendición {IdRendicion} guardada como pendiente de revisión.",
                    idRendicion);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error guardando la rendición {IdRendicion}.",
                    idRendicion);

                TempData["error"] =
                    "No se pudo guardar la rendición.";

                return RedirectToAction(
                    nameof(Index),
                    new
                    {
                        idRendicion
                    });
            }

            List<string> correosAdministradores;

            try
            {
                correosAdministradores =
                    await _context.Usuarios
                        .AsNoTracking()
                        .Where(u =>
                            u.IdRol == ROL_ADMINISTRADOR &&
                            u.Estado &&
                            u.Correo != null)
                        .Select(u =>
                            u.Correo!.Trim())
                        .Where(c =>
                            c != "")
                        .Distinct()
                        .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error consultando administradores.");

                correosAdministradores =
                    new List<string>();
            }

            var nombreEmpleado =
                $"{rendicion.Usuario?.Nombres} {rendicion.Usuario?.Apellidos}"
                    .Trim();

            if (string.IsNullOrWhiteSpace(nombreEmpleado))
            {
                nombreEmpleado =
                    $"Usuario {rendicion.IdUsuario}";
            }

            var totalBase =
                gastos.Sum(g =>
                    g.ValorVenta);

            var totalIgv =
                gastos.Sum(g =>
                    g.IGV);

            var saldoCorreo =
                rendicion.Saldo;

            var asunto =
                $"Liquidación de viáticos #{rendicion.IdRendicion} pendiente de revisión";

            var contenidoHtml =
                GenerarCorreoLiquidacion(
                    rendicion,
                    nombreEmpleado,
                    totalBase,
                    totalIgv,
                    saldoCorreo);

            var adjuntosCorreo =
                new List<(string Ruta, string Nombre)>();

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

            bool correoEnviado = false;

            if (correosAdministradores.Count > 0)
            {
                try
                {
                    correoEnviado =
                        await _correoService.EnviarAsync(
                            correosAdministradores,
                            asunto,
                            contenidoHtml,
                            adjuntosCorreo);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Error enviando correo de rendición {IdRendicion}.",
                        idRendicion);
                }
            }

            if (correoEnviado)
            {
                TempData["Success"] =
                    "La rendición fue enviada correctamente para revisión y se notificó al administrador por correo.";
            }
            else if (correosAdministradores.Count == 0)
            {
                TempData["Success"] =
                    "La rendición fue enviada correctamente para revisión. No se encontró ningún administrador activo con correo configurado.";
            }
            else
            {
                TempData["Success"] =
                    "La rendición fue enviada correctamente para revisión, pero no se pudo enviar el correo de notificación.";
            }

            _logger.LogInformation(
                "Proceso completo de rendición {IdRendicion} terminado en {Tiempo} ms. CorreoEnviado={CorreoEnviado}",
                idRendicion,
                (DateTime.Now - inicio).TotalMilliseconds,
                correoEnviado);

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
                        (decimal?)g.MontoTotal) ?? 0m;

            var totalVehiculo =
                await _context.BitacorasVehiculo
                    .Where(b =>
                        b.IdRendicion == idRendicion)
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
        // OBTENER RENDICIÓN DEL USUARIO
        // =========================================================

        private async Task<Rendicion?> ObtenerRendicionAsync(
            int idRendicion,
            int idUsuario)
        {
            return await _context.Rendiciones
                .Include(r => r.Solicitud)
                .Include(r => r.Usuario)
                .FirstOrDefaultAsync(r =>
                    r.IdRendicion == idRendicion &&
                    r.IdUsuario == idUsuario);
        }

        // =========================================================
        // OBTENER TIPO DE GASTO
        // =========================================================

        private async Task<TipoGasto?> ObtenerTipoGastoAsync(
            int idTipoGasto)
        {
            return await _context.TipoGastos
                .FirstOrDefaultAsync(t =>
                    t.IdTipoGasto == idTipoGasto);
        }

        // =========================================================
        // ADMINISTRADOR
        // =========================================================

        private bool EsAdministrador()
        {
            var idRol =
                HttpContext.Session.GetInt32("IdRol");

            return idRol == ROL_ADMINISTRADOR;
        }

        // =========================================================
        // MOVILIDAD
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
        // MOVILIDAD INTERNA
        // =========================================================

        private static bool EsMovilidadInterna(
            TipoGasto tipoGasto)
        {
            return tipoGasto.Nombre
                .Trim()
                .Equals(
                    "Movilidad interna",
                    StringComparison.OrdinalIgnoreCase);
        }

        // =========================================================
        // HOSPEDAJE
        // =========================================================

        private static bool EsHospedaje(
            TipoGasto tipoGasto)
        {
            return tipoGasto.Nombre
                .Trim()
                .Equals(
                    "Hospedaje",
                    StringComparison.OrdinalIgnoreCase);
        }
        private static bool EsOtros(TipoGasto tipoGasto)
        {
            return tipoGasto.Nombre.Trim().Equals(
                "Otros",
                StringComparison.OrdinalIgnoreCase);
        }

        // =========================================================
        // ALIMENTACIÓN
        // =========================================================

        private static bool EsAlimentacion(
            TipoGasto tipoGasto)
        {
            return tipoGasto.Nombre
                .Trim()
                .Equals(
                    "Alimentación",
                    StringComparison.OrdinalIgnoreCase);
        }

        // =========================================================
        // LÍMITE DIARIO
        // =========================================================

        private static decimal ObtenerLimiteDiario(
            TipoGasto tipoGasto)
        {
            if (EsAlimentacion(tipoGasto))
            {
                return LIMITE_ALIMENTACION_DIARIO;
            }

            if (EsMovilidadInterna(tipoGasto))
            {
                return LIMITE_MOVILIDAD_INTERNA_DIARIO;
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
        // LIMPIAR COMPROBANTE
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
        // LIMPIAR HOSPEDAJE
        // =========================================================

        private static void LimpiarDatosHospedaje(
            Gasto gasto)
        {
            gasto.FechaInicioHospedaje = null;
            gasto.FechaFinHospedaje = null;
            gasto.DiasHospedaje = 0;
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

            if (fecha.Date < rendicion.FechaInicio.Date ||
                fecha.Date > rendicion.FechaFin.Date)
            {
                var mensaje =
                    $"La fecha del gasto debe estar entre {rendicion.FechaInicio:dd/MM/yyyy} y {rendicion.FechaFin:dd/MM/yyyy}.";

                if (incluirMensajeDetallado)
                {
                    mensaje +=
                        " La fecha del comprobante debe pertenecer al periodo aprobado.";
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
        // CALCULAR IGV
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
                    gasto.MontoTotal / (1 + TASA_IGV),
                    2,
                    MidpointRounding.AwayFromZero);

            gasto.IGV =
                Math.Round(
                    gasto.MontoTotal - gasto.ValorVenta,
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
            if (!EsAlimentacion(tipoGasto) &&
                !EsMovilidadInterna(tipoGasto))
            {
                return;
            }

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
                        g.IdRendicion == gasto.IdRendicion &&
                        g.IdTipoGasto == gasto.IdTipoGasto &&
                        g.Fecha >= inicioDia &&
                        g.Fecha < finDia);

            if (idGastoExcluir > 0)
            {
                query =
                    query.Where(g =>
                        g.IdGasto != idGastoExcluir);
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
                $"El límite diario para {tipoGasto.Nombre} es S/ {limiteDiario:N2}. El {gasto.Fecha:dd/MM/yyyy} ya tiene registrado S/ {montoRegistrado:N2}. Solo puede registrar hasta S/ {disponible:N2}.");
        }

        // =========================================================
        // VALIDAR HOSPEDAJE
        // =========================================================

        private async Task ValidarHospedajeAsync(
            Gasto gasto,
            Rendicion rendicion,
            int idGastoExcluir,
            bool validarPeriodoRendicion)
        {
            if (gasto.FechaInicioHospedaje == null)
            {
                ModelState.AddModelError(
                    nameof(Gasto.FechaInicioHospedaje),
                    "Debe ingresar la fecha de inicio del hospedaje.");

                return;
            }

            if (gasto.FechaFinHospedaje == null)
            {
                ModelState.AddModelError(
                    nameof(Gasto.FechaFinHospedaje),
                    "Debe ingresar la fecha de fin del hospedaje.");

                return;
            }

            var fechaInicio =
                gasto.FechaInicioHospedaje.Value.Date;

            var fechaFin =
                gasto.FechaFinHospedaje.Value.Date;

            if (fechaFin < fechaInicio)
            {
                ModelState.AddModelError(
                    nameof(Gasto.FechaFinHospedaje),
                    "La fecha de fin del hospedaje no puede ser anterior a la fecha de inicio.");

                return;
            }

            if (gasto.DiasHospedaje < 1)
            {
                ModelState.AddModelError(
                    nameof(Gasto.DiasHospedaje),
                    "La cantidad de días de hospedaje debe ser como mínimo 1.");

                return;
            }

            if (validarPeriodoRendicion)
            {
                if (fechaInicio < rendicion.FechaInicio.Date ||
                    fechaInicio > rendicion.FechaFin.Date)
                {
                    ModelState.AddModelError(
                        nameof(Gasto.FechaInicioHospedaje),
                        $"La fecha de inicio del hospedaje debe estar entre {rendicion.FechaInicio:dd/MM/yyyy} y {rendicion.FechaFin:dd/MM/yyyy}.");

                    return;
                }

                if (fechaFin < rendicion.FechaInicio.Date ||
                    fechaFin > rendicion.FechaFin.Date)
                {
                    ModelState.AddModelError(
                        nameof(Gasto.FechaFinHospedaje),
                        $"La fecha de fin del hospedaje debe estar entre {rendicion.FechaInicio:dd/MM/yyyy} y {rendicion.FechaFin:dd/MM/yyyy}.");

                    return;
                }
            }

            var hospedajes =
                _context.Gastos
                    .Where(g =>
                        g.IdRendicion == gasto.IdRendicion &&
                        g.IdTipoGasto == gasto.IdTipoGasto &&
                        g.FechaInicioHospedaje != null &&
                        g.FechaFinHospedaje != null);

            if (idGastoExcluir > 0)
            {
                hospedajes =
                    hospedajes.Where(g =>
                        g.IdGasto != idGastoExcluir);
            }

            var existeCruce =
                await hospedajes.AnyAsync(g =>
                    fechaInicio <=
                        g.FechaFinHospedaje!.Value.Date &&
                    fechaFin >=
                        g.FechaInicioHospedaje!.Value.Date);

            if (existeCruce)
            {
                ModelState.AddModelError(
                    nameof(Gasto.FechaInicioHospedaje),
                    "El periodo de hospedaje se cruza con otro hospedaje ya registrado en esta rendición.");

                return;
            }

            var limiteHospedaje =
                gasto.DiasHospedaje *
                LIMITE_HOSPEDAJE_POR_DIA;

            if (gasto.MontoTotal >
                limiteHospedaje)
            {
                ModelState.AddModelError(
                    nameof(Gasto.MontoTotal),
                    $"El monto máximo permitido para hospedaje es S/ {limiteHospedaje:N2}, considerando {gasto.DiasHospedaje} día(s) × S/ {LIMITE_HOSPEDAJE_POR_DIA:N2}.");
            }
        }

        // =========================================================
        // VALIDAR DATOS DEL COMPROBANTE
        // =========================================================

        private void ValidarDatosComprobante(
            Gasto gasto)
        {
            if (!gasto.IdTipoComprobante.HasValue ||
                gasto.IdTipoComprobante.Value <= 0)
            {
                ModelState.AddModelError(
                    nameof(Gasto.IdTipoComprobante),
                    "Debe seleccionar el tipo de comprobante.");

                return;
            }

            var tipoComprobante =
                _context.TipoComprobantes
                    .FirstOrDefault(t =>
                        t.IdTipoComprobante ==
                        gasto.IdTipoComprobante.Value);

            if (tipoComprobante == null)
            {
                ModelState.AddModelError(
                    nameof(Gasto.IdTipoComprobante),
                    "El tipo de comprobante seleccionado no existe.");

                return;
            }

            var nombreComprobante =
                tipoComprobante.Nombre?
                    .Trim()
                    .ToLowerInvariant() ?? "";

            bool esFactura =
                nombreComprobante == "factura";

            if (!string.IsNullOrWhiteSpace(gasto.Ruc))
            {
                gasto.Ruc =
                    gasto.Ruc.Trim();

                if (gasto.Ruc.Length != 11 ||
                    !gasto.Ruc.All(char.IsDigit))
                {
                    ModelState.AddModelError(
                        nameof(Gasto.Ruc),
                        "El RUC debe contener exactamente 11 dígitos.");
                }
            }
            else if (esFactura)
            {
                ModelState.AddModelError(
                    nameof(Gasto.Ruc),
                    "Para una factura debe ingresar el RUC.");
            }

            if (esFactura)
            {
                if (string.IsNullOrWhiteSpace(
                    gasto.RazonSocial))
                {
                    ModelState.AddModelError(
                        nameof(Gasto.RazonSocial),
                        "Para una factura debe ingresar la razón social.");
                }

                if (string.IsNullOrWhiteSpace(
                    gasto.DomicilioFiscal))
                {
                    ModelState.AddModelError(
                        nameof(Gasto.DomicilioFiscal),
                        "Para una factura debe ingresar el domicilio fiscal.");
                }
            }
        }

        // =========================================================
        // VALIDAR RUC
        // =========================================================

        private async Task ValidarRucAsync(
            Gasto gasto)
        {
            if (string.IsNullOrWhiteSpace(gasto.Ruc))
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
                string.IsNullOrWhiteSpace(consulta.Ruc)
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
            if (!gasto.IdTipoComprobante.HasValue)
            {
                return;
            }

            var tipoComprobante =
                _context.TipoComprobantes
                    .FirstOrDefault(t =>
                        t.IdTipoComprobante ==
                        gasto.IdTipoComprobante.Value);

            if (tipoComprobante == null)
            {
                return;
            }

            var nombreComprobante =
                tipoComprobante.Nombre?
                    .Trim()
                    .ToLowerInvariant() ?? "";

            if (nombreComprobante != "factura")
            {
                if (!string.IsNullOrWhiteSpace(
                    gasto.RazonSocial) &&
                    gasto.RazonSocial.Length > 250)
                {
                    ModelState.AddModelError(
                        nameof(Gasto.RazonSocial),
                        "La razón social no puede superar los 250 caracteres.");
                }

                if (!string.IsNullOrWhiteSpace(
                    gasto.DomicilioFiscal) &&
                    gasto.DomicilioFiscal.Length > 300)
                {
                    ModelState.AddModelError(
                        nameof(Gasto.DomicilioFiscal),
                        "El domicilio fiscal no puede superar los 300 caracteres.");
                }

                return;
            }

            if (string.IsNullOrWhiteSpace(
                gasto.RazonSocial))
            {
                ModelState.AddModelError(
                    nameof(Gasto.RazonSocial),
                    "No se encontró la razón social del RUC.");
            }
            else if (gasto.RazonSocial.Length > 250)
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
            else if (gasto.DomicilioFiscal.Length > 300)
            {
                ModelState.AddModelError(
                    nameof(Gasto.DomicilioFiscal),
                    "El domicilio fiscal no puede superar los 300 caracteres.");
            }
        }

        // =========================================================
        // GUARDAR COMPROBANTE
        // =========================================================

        private async Task<ResultadoArchivo>
            GuardarComprobanteAsync(
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
                Path.GetExtension(archivo.FileName)
                    .ToLowerInvariant();

            if (!ExtensionesPermitidas.Contains(extension))
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

            var carpeta =
                Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot",
                    "comprobantes");

            Directory.CreateDirectory(carpeta);

            if (extension == ".pdf")
            {
                var nombrePdf =
                    $"{Guid.NewGuid()}.pdf";

                var rutaPdf =
                    Path.Combine(
                        carpeta,
                        nombrePdf);

                await using var streamPdf =
                    new FileStream(
                        rutaPdf,
                        FileMode.Create);

                await archivo.CopyToAsync(
                    streamPdf);

                return new ResultadoArchivo
                {
                    Exito = true,
                    RutaPublica =
                        $"/comprobantes/{nombrePdf}",
                    RutaFisica =
                        rutaPdf
                };
            }

            try
            {
                await using var memoria =
                    new MemoryStream();

                await archivo.CopyToAsync(
                    memoria);

                memoria.Position = 0;

                using var imagen =
                    new MagickImage(memoria);

                imagen.AutoOrient();
                imagen.BackgroundColor =
                    MagickColors.White;

                imagen.Alpha(
                    AlphaOption.Remove);

                imagen.Strip();

                imagen.Format =
                    MagickFormat.Jpeg;

                imagen.Quality = 90;

                var nombreImagen =
                    $"{Guid.NewGuid()}.jpg";

                var rutaImagen =
                    Path.Combine(
                        carpeta,
                        nombreImagen);

                await imagen.WriteAsync(
                    rutaImagen);

                return new ResultadoArchivo
                {
                    Exito = true,
                    RutaPublica =
                        $"/comprobantes/{nombreImagen}",
                    RutaFisica =
                        rutaImagen
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "No se pudo procesar el comprobante {Archivo} como imagen.",
                    archivo.FileName);

                return new ResultadoArchivo
                {
                    Exito = false,
                    Mensaje =
                        "No se pudo procesar la imagen del comprobante. Seleccione una imagen JPG, JPEG o PNG válida."
                };
            }
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
            }
        }

        // =========================================================
        // PROCESAR ERRORES CREATE
        // =========================================================

        private async Task<IActionResult>
            ProcesarErroresCreate(
                int idRendicion)
        {
            AgregarErroresTempData();

            await Task.CompletedTask;

            return RedirectToAction(
                nameof(Index),
                new
                {
                    idRendicion
                });
        }

        // =========================================================
        // AGREGAR ERRORES
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
                    $"Debe registrar la devolución de S/ {rendicion.Saldo:N2} antes de enviar.";

                return false;
            }

            if (devolucion.Monto !=
                rendicion.Saldo)
            {
                TempData["error"] =
                    $"El monto devuelto debe ser exactamente S/ {rendicion.Saldo:N2}.";

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
                Math.Abs(rendicion.Saldo);

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

            reembolsoExistente.FechaAprobacion = null;
            reembolsoExistente.FechaPago = null;
            reembolsoExistente.Banco = null;
            reembolsoExistente.NumeroOperacion = null;
            reembolsoExistente.ComprobantePago = null;
            reembolsoExistente.Observaciones = null;
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
            return $$"""
<!DOCTYPE html>
<html lang="es">
<head>
<meta charset="UTF-8">
<meta name="viewport" content="width=device-width,initial-scale=1.0">
<title>Liquidación de Viáticos - DINACEN</title>
</head>
<body style="margin:0;padding:0;background-color:#f2f5f8;font-family:Arial,Helvetica,sans-serif;color:#111111;">
<table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0" style="background-color:#f2f5f8;padding:35px 15px;">
<tr>
<td align="center">
<table role="presentation" width="700" cellspacing="0" cellpadding="0" border="0" style="max-width:700px;width:100%;background:#ffffff;border-radius:12px;overflow:hidden;box-shadow:0 4px 15px rgba(0,0,0,0.08);">
<tr>
<td style="background:#ffffff;padding:30px 35px 20px;text-align:center;">
<img src="cid:logoDinacen" alt="DINACEN" style="width:210px;max-width:80%;height:auto;display:block;margin:0 auto;">
</td>
</tr>
<tr>
<td style="background:#ffffff;padding:0 35px 22px;text-align:center;">
<div style="height:4px;background:#6AA84F;width:100%;border-radius:3px;"></div>
</td>
</tr>
<tr>
<td style="background:#0C4A8A;padding:24px 35px;text-align:center;">
<div style="font-size:25px;font-weight:bold;color:#ffffff;line-height:1.3;">LIQUIDACIÓN DE VIÁTICOS</div>
<div style="font-size:15px;color:#ffffff;margin-top:8px;opacity:0.95;">Pendiente de revisión</div>
</td>
</tr>
<tr>
<td style="padding:35px 40px 25px;">
<div style="font-size:22px;font-weight:bold;color:#111111;margin-bottom:18px;">Nueva liquidación pendiente de revisión</div>
<div style="font-size:16px;line-height:1.7;color:#222222;margin-bottom:25px;">
El empleado <strong>{{nombreEmpleado}}</strong> ha enviado una liquidación de gastos que se encuentra pendiente de revisión.
</div>
<table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0" style="background:#ffffff;border:1px solid #d9e2ea;border-radius:8px;overflow:hidden;">
<tr>
<td colspan="2" style="padding:18px 20px;background:#eef4f9;border-bottom:1px solid #d9e2ea;">
<div style="font-size:18px;font-weight:bold;color:#0C4A8A;">Información de la liquidación</div>
</td>
</tr>
<tr>
<td width="42%" style="padding:14px 20px;border-bottom:1px solid #e1e7ec;font-size:15px;font-weight:bold;color:#333333;">Liquidación</td>
<td style="padding:14px 20px;border-bottom:1px solid #e1e7ec;font-size:15px;color:#333333;">#{{rendicion.IdRendicion}}</td>
</tr>
<tr>
<td style="padding:14px 20px;border-bottom:1px solid #e1e7ec;font-size:15px;font-weight:bold;color:#333333;">Empleado</td>
<td style="padding:14px 20px;border-bottom:1px solid #e1e7ec;font-size:15px;color:#333333;">{{nombreEmpleado}}</td>
</tr>
<tr>
<td style="padding:14px 20px;border-bottom:1px solid #e1e7ec;font-size:15px;font-weight:bold;color:#333333;">Destino</td>
<td style="padding:14px 20px;border-bottom:1px solid #e1e7ec;font-size:15px;color:#333333;">{{rendicion.Solicitud?.Destino}}</td>
</tr>
<tr>
<td style="padding:14px 20px;border-bottom:1px solid #e1e7ec;font-size:15px;font-weight:bold;color:#333333;">Periodo</td>
<td style="padding:14px 20px;border-bottom:1px solid #e1e7ec;font-size:15px;color:#333333;">{{rendicion.FechaInicio:dd/MM/yyyy}} al {{rendicion.FechaFin:dd/MM/yyyy}}</td>
</tr>
<tr>
<td style="padding:14px 20px;border-bottom:1px solid #e1e7ec;font-size:15px;font-weight:bold;color:#333333;">Monto aprobado</td>
<td style="padding:14px 20px;border-bottom:1px solid #e1e7ec;font-size:15px;font-weight:bold;color:#333333;">S/ {{rendicion.Solicitud?.Monto:N2}}</td>
</tr>
<tr>
<td style="padding:14px 20px;border-bottom:1px solid #e1e7ec;font-size:15px;font-weight:bold;color:#333333;">Valor de venta</td>
<td style="padding:14px 20px;border-bottom:1px solid #e1e7ec;font-size:15px;color:#333333;">S/ {{totalBase:N2}}</td>
</tr>
<tr>
<td style="padding:14px 20px;border-bottom:1px solid #e1e7ec;font-size:15px;font-weight:bold;color:#333333;">IGV</td>
<td style="padding:14px 20px;border-bottom:1px solid #e1e7ec;font-size:15px;color:#333333;">S/ {{totalIgv:N2}}</td>
</tr>
<tr>
<td style="padding:16px 20px;border-bottom:1px solid #e1e7ec;font-size:16px;font-weight:bold;color:#333333;">Total rendido</td>
<td style="padding:16px 20px;border-bottom:1px solid #e1e7ec;font-size:19px;font-weight:bold;color:#0C4A8A;">S/ {{rendicion.Total:N2}}</td>
</tr>
<tr>
<td style="padding:16px 20px;font-size:16px;font-weight:bold;color:#333333;">Saldo</td>
<td style="padding:16px 20px;font-size:19px;font-weight:bold;color:#6AA84F;">S/ {{saldo:N2}}</td>
</tr>
</table>
<div style="margin-top:28px;padding:20px;background:#f7f9fb;border-left:5px solid #0C4A8A;border-radius:5px;">
<div style="font-size:15px;line-height:1.7;color:#222222;">
Se adjuntan los PDF correspondientes a la <strong>liquidación</strong> y a los <strong>vouchers</strong> registrados.
<br><br>
Ingrese al <strong>sistema de gestión de viáticos DINACEN</strong> para revisar los comprobantes, verificar la devolución y proceder con la aprobación o rechazo de la rendición.
</div>
</div>
</td>
</tr>
<tr>
<td style="background:#0C4A8A;padding:25px 35px;text-align:center;">
<div style="font-size:17px;font-weight:bold;color:#ffffff;">DINACEN</div>
<div style="font-size:13px;color:#dce8f2;margin-top:7px;">Sistema de Gestión de Viáticos</div>
<div style="font-size:12px;color:#c5d5e2;margin-top:12px;line-height:1.5;">Este mensaje ha sido generado automáticamente.<br>Por favor, no responda a este correo.</div>
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