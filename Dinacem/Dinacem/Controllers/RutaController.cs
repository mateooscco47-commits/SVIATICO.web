using Dinacem.Models;
using Dinacem.Models.Entidades;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Dinacem.Controllers
{
    public class RutaController : Controller
    {
        private readonly AplicacionDbContexto _context;
        private readonly ILogger<RutaController> _logger;

        public RutaController(
            AplicacionDbContexto context,
            ILogger<RutaController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // =========================================================
        // INDEX - LISTAR RUTAS
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var idUsuario = HttpContext.Session.GetInt32("IdUsuario");

            if (idUsuario == null)
            {
                return RedirectToAction("Login", "Cuenta");
            }

            var idRol = HttpContext.Session.GetInt32("IdRol");

            if (idRol != 1)
            {
                TempData["Error"] =
                    "No tienes permisos para administrar las rutas.";

                return RedirectToAction("Index", "Principal");
            }

            var rutas = await _context.Rutas
                .OrderBy(r => r.Origen)
                .ThenBy(r => r.Destino)
                .ToListAsync();

            return View(rutas);
        }

        // =========================================================
        // CREATE - MOSTRAR FORMULARIO
        // =========================================================

        [HttpGet]
        public IActionResult Create()
        {
            var idUsuario = HttpContext.Session.GetInt32("IdUsuario");

            if (idUsuario == null)
            {
                return RedirectToAction("Login", "Cuenta");
            }

            var idRol = HttpContext.Session.GetInt32("IdRol");

            if (idRol != 1)
            {
                TempData["Error"] =
                    "No tienes permisos para registrar rutas.";

                return RedirectToAction("Index", "Principal");
            }

            var ruta = new Ruta
            {
                Estado = true
            };

            return View(ruta);
        }

        // =========================================================
        // CREATE - GUARDAR RUTA
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Ruta ruta)
        {
            var idUsuario = HttpContext.Session.GetInt32("IdUsuario");

            if (idUsuario == null)
            {
                return RedirectToAction("Login", "Cuenta");
            }

            var idRol = HttpContext.Session.GetInt32("IdRol");

            if (idRol != 1)
            {
                TempData["Error"] =
                    "No tienes permisos para registrar rutas.";

                return RedirectToAction("Index", "Principal");
            }

            // =====================================================
            // LIMPIAR DATOS
            // =====================================================

            ruta.Origen = ruta.Origen?.Trim() ?? string.Empty;
            ruta.Destino = ruta.Destino?.Trim() ?? string.Empty;

            // =====================================================
            // VALIDAR ORIGEN
            // =====================================================

            if (string.IsNullOrWhiteSpace(ruta.Origen))
            {
                ModelState.AddModelError(
                    nameof(ruta.Origen),
                    "El origen es obligatorio.");
            }

            // =====================================================
            // VALIDAR DESTINO
            // =====================================================

            if (string.IsNullOrWhiteSpace(ruta.Destino))
            {
                ModelState.AddModelError(
                    nameof(ruta.Destino),
                    "El destino es obligatorio.");
            }

            // =====================================================
            // VALIDAR ORIGEN Y DESTINO DIFERENTES
            // =====================================================

            if (!string.IsNullOrWhiteSpace(ruta.Origen) &&
                !string.IsNullOrWhiteSpace(ruta.Destino) &&
                string.Equals(
                    ruta.Origen,
                    ruta.Destino,
                    StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(
                    nameof(ruta.Destino),
                    "El origen y el destino no pueden ser iguales.");
            }

            // =====================================================
            // VALIDAR KILÓMETROS
            // =====================================================

            if (ruta.Kilometros <= 0)
            {
                ModelState.AddModelError(
                    nameof(ruta.Kilometros),
                    "Los kilómetros deben ser mayores que 0.");
            }

            // =====================================================
            // VALIDAR RUTA DUPLICADA
            // =====================================================

            if (!string.IsNullOrWhiteSpace(ruta.Origen) &&
                !string.IsNullOrWhiteSpace(ruta.Destino))
            {
                var origen = ruta.Origen;
                var destino = ruta.Destino;

                var rutaExiste = await _context.Rutas
                    .AnyAsync(r =>
                        r.Origen == origen &&
                        r.Destino == destino);

                if (rutaExiste)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        "Ya existe una ruta registrada con ese origen y destino.");
                }
            }

            // =====================================================
            // SI HAY ERRORES
            // =====================================================

            if (!ModelState.IsValid)
            {
                return View(ruta);
            }

            // =====================================================
            // GUARDAR
            // =====================================================

            try
            {
                // Toda nueva ruta se registra activa
                ruta.Estado = true;

                _context.Rutas.Add(ruta);

                await _context.SaveChangesAsync();

                TempData["Success"] =
                    "La ruta se registró correctamente.";

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al registrar la ruta.");

                ModelState.AddModelError(
                    string.Empty,
                    "Ocurrió un error al registrar la ruta.");

                return View(ruta);
            }
        }

        // =========================================================
        // EDIT - MOSTRAR FORMULARIO
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            var idUsuario = HttpContext.Session.GetInt32("IdUsuario");

            if (idUsuario == null)
            {
                return RedirectToAction("Login", "Cuenta");
            }

            var idRol = HttpContext.Session.GetInt32("IdRol");

            if (idRol != 1)
            {
                TempData["Error"] =
                    "No tienes permisos para editar rutas.";

                return RedirectToAction("Index", "Principal");
            }

            if (id == null)
            {
                return NotFound();
            }

            var ruta = await _context.Rutas
                .FirstOrDefaultAsync(r => r.IdRuta == id.Value);

            if (ruta == null)
            {
                return NotFound();
            }

            return View(ruta);
        }

        // =========================================================
        // EDIT - GUARDAR CAMBIOS
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Ruta ruta)
        {
            var idUsuario = HttpContext.Session.GetInt32("IdUsuario");

            if (idUsuario == null)
            {
                return RedirectToAction("Login", "Cuenta");
            }

            var idRol = HttpContext.Session.GetInt32("IdRol");

            if (idRol != 1)
            {
                TempData["Error"] =
                    "No tienes permisos para editar rutas.";

                return RedirectToAction("Index", "Principal");
            }

            if (id != ruta.IdRuta)
            {
                return NotFound();
            }

            // =====================================================
            // LIMPIAR DATOS
            // =====================================================

            ruta.Origen = ruta.Origen?.Trim() ?? string.Empty;
            ruta.Destino = ruta.Destino?.Trim() ?? string.Empty;

            // =====================================================
            // VALIDACIONES
            // =====================================================

            if (string.IsNullOrWhiteSpace(ruta.Origen))
            {
                ModelState.AddModelError(
                    nameof(ruta.Origen),
                    "El origen es obligatorio.");
            }

            if (string.IsNullOrWhiteSpace(ruta.Destino))
            {
                ModelState.AddModelError(
                    nameof(ruta.Destino),
                    "El destino es obligatorio.");
            }

            if (!string.IsNullOrWhiteSpace(ruta.Origen) &&
                !string.IsNullOrWhiteSpace(ruta.Destino) &&
                string.Equals(
                    ruta.Origen,
                    ruta.Destino,
                    StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(
                    nameof(ruta.Destino),
                    "El origen y el destino no pueden ser iguales.");
            }

            if (ruta.Kilometros <= 0)
            {
                ModelState.AddModelError(
                    nameof(ruta.Kilometros),
                    "Los kilómetros deben ser mayores que 0.");
            }

            // =====================================================
            // VALIDAR DUPLICADO
            // =====================================================

            if (!string.IsNullOrWhiteSpace(ruta.Origen) &&
                !string.IsNullOrWhiteSpace(ruta.Destino))
            {
                var origen = ruta.Origen;
                var destino = ruta.Destino;

                var rutaExiste = await _context.Rutas
                    .AnyAsync(r =>
                        r.IdRuta != ruta.IdRuta &&
                        r.Origen == origen &&
                        r.Destino == destino);

                if (rutaExiste)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        "Ya existe otra ruta registrada con ese origen y destino.");
                }
            }

            // =====================================================
            // SI HAY ERRORES
            // =====================================================

            if (!ModelState.IsValid)
            {
                return View(ruta);
            }

            // =====================================================
            // ACTUALIZAR
            // =====================================================

            try
            {
                var rutaBD = await _context.Rutas
                    .FirstOrDefaultAsync(r => r.IdRuta == id);

                if (rutaBD == null)
                {
                    return NotFound();
                }

                rutaBD.Origen = ruta.Origen;
                rutaBD.Destino = ruta.Destino;
                rutaBD.Kilometros = ruta.Kilometros;
                rutaBD.Estado = ruta.Estado;

                await _context.SaveChangesAsync();

                TempData["Success"] =
                    "La ruta se actualizó correctamente.";

                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(
                    ex,
                    "Error de concurrencia al editar la ruta {IdRuta}.",
                    id);

                var existe = await _context.Rutas
                    .AnyAsync(r => r.IdRuta == id);

                if (!existe)
                {
                    return NotFound();
                }

                ModelState.AddModelError(
                    string.Empty,
                    "La ruta fue modificada por otro usuario. Intenta nuevamente.");

                return View(ruta);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al editar la ruta {IdRuta}.",
                    id);

                ModelState.AddModelError(
                    string.Empty,
                    "Ocurrió un error al actualizar la ruta.");

                return View(ruta);
            }
        }

        // =========================================================
        // CAMBIAR ESTADO
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CambiarEstado(int id)
        {
            var idUsuario = HttpContext.Session.GetInt32("IdUsuario");

            if (idUsuario == null)
            {
                return RedirectToAction("Login", "Cuenta");
            }

            var idRol = HttpContext.Session.GetInt32("IdRol");

            if (idRol != 1)
            {
                TempData["Error"] =
                    "No tienes permisos para cambiar el estado de las rutas.";

                return RedirectToAction("Index", "Principal");
            }

            try
            {
                var ruta = await _context.Rutas
                    .FirstOrDefaultAsync(r => r.IdRuta == id);

                if (ruta == null)
                {
                    TempData["Error"] =
                        "La ruta no existe.";

                    return RedirectToAction(nameof(Index));
                }

                ruta.Estado = !ruta.Estado;

                await _context.SaveChangesAsync();

                TempData["Success"] = ruta.Estado
                    ? "La ruta fue activada correctamente."
                    : "La ruta fue desactivada correctamente.";

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al cambiar el estado de la ruta {IdRuta}.",
                    id);

                TempData["Error"] =
                    "Ocurrió un error al cambiar el estado de la ruta.";

                return RedirectToAction(nameof(Index));
            }
        }
    }
}