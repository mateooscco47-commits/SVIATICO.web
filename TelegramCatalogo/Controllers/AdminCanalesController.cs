using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TelegramCatalogo.Data;
using TelegramCatalogo.Models;
using TelegramCatalogo.Services;

namespace TelegramCatalogo.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class AdminCanalesController : Controller
    {
        private readonly AplicacionDbContexto _context;
        private readonly SlugService _slugService;
        private readonly ImagenService _imagenService;


        public AdminCanalesController(
            AplicacionDbContexto context,
            SlugService slugService,
            ImagenService imagenService)
        {
            _context = context;
            _slugService = slugService;
            _imagenService = imagenService;
        }


        // ==========================================
        // LISTADO
        // ==========================================

        [HttpGet("/admin/canales")]
        public async Task<IActionResult> Index(
            string? buscar)
        {
            var consulta =
                _context.Canales
                    .Include(c => c.Categoria)
                    .AsQueryable();


            if (!string.IsNullOrWhiteSpace(buscar))
            {
                buscar = buscar.Trim();

                consulta =
                    consulta.Where(c =>
                        c.Nombre.Contains(buscar) ||
                        (c.UsernameTelegram != null &&
                         c.UsernameTelegram.Contains(buscar))
                    );
            }


            ViewBag.Buscar = buscar;


            var canales =
                await consulta
                    .OrderByDescending(
                        c => c.FechaRegistro)
                    .ToListAsync();


            return View(canales);
        }


        // ==========================================
        // CREAR - GET
        // ==========================================

        [HttpGet("/admin/canales/nuevo")]
        public async Task<IActionResult> Crear()
        {
            await CargarCategorias();

            return View(new Canal());
        }


        // ==========================================
        // CREAR - POST
        // ==========================================

        [HttpPost("/admin/canales/nuevo")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(
            Canal canal,
            IFormFile? imagenArchivo)
        {
            if (!ModelState.IsValid)
            {
                await CargarCategorias(
                    canal.IdCategoria);

                return View(canal);
            }


            canal.Nombre =
                canal.Nombre.Trim();

            canal.EnlaceTelegram =
                canal.EnlaceTelegram.Trim();

            canal.UsernameTelegram =
                canal.UsernameTelegram?.Trim()
                    .TrimStart('@');

            canal.Descripcion =
                canal.Descripcion?.Trim();

            canal.Idioma =
                canal.Idioma?.Trim();

            canal.Pais =
                canal.Pais?.Trim();


            // ======================================
            // COMPROBAR ENLACE DUPLICADO
            // ======================================

            var existeEnlace =
                await _context.Canales
                    .AnyAsync(c =>
                        c.EnlaceTelegram ==
                        canal.EnlaceTelegram);

            if (existeEnlace)
            {
                ModelState.AddModelError(
                    nameof(canal.EnlaceTelegram),
                    "Ya existe un canal con este enlace de Telegram."
                );

                await CargarCategorias(
                    canal.IdCategoria);

                return View(canal);
            }


            // ======================================
            // SLUG
            // ======================================

            canal.Slug =
                await _slugService
                    .GenerarSlugCanalAsync(
                        canal.Nombre);


            // ======================================
            // DATOS INICIALES
            // ======================================

            canal.FechaRegistro =
                DateTime.Now;

            canal.FechaActualizacion =
                DateTime.Now;

            canal.Visitas = 0;
            canal.Clicks = 0;


            // ======================================
            // IMAGEN
            // ======================================

            if (imagenArchivo != null &&
                imagenArchivo.Length > 0)
            {
                try
                {
                    canal.Imagen =
                        await _imagenService
                            .GuardarCanalAsync(
                                imagenArchivo);
                }
                catch (InvalidOperationException ex)
                {
                    ModelState.AddModelError(
                        "imagenArchivo",
                        ex.Message
                    );

                    await CargarCategorias(
                        canal.IdCategoria);

                    return View(canal);
                }
            }


            try
            {
                _context.Canales.Add(canal);

                await _context.SaveChangesAsync();
            }
            catch
            {
                // Si SQL falla después de guardar
                // la imagen, eliminamos el archivo.
                if (!string.IsNullOrWhiteSpace(
                    canal.Imagen))
                {
                    _imagenService.Eliminar(
                        canal.Imagen);
                }

                ModelState.AddModelError(
                    "",
                    "No se pudo guardar el canal."
                );

                await CargarCategorias(
                    canal.IdCategoria);

                return View(canal);
            }


            TempData["Exito"] =
                "Canal creado correctamente.";


            return RedirectToAction(
                nameof(Index));
        }


        // ==========================================
        // EDITAR - GET
        // ==========================================

        [HttpGet("/admin/canales/editar/{id:int}")]
        public async Task<IActionResult> Editar(int id)
        {
            var canal =
                await _context.Canales
                    .FirstOrDefaultAsync(
                        c => c.IdCanal == id);

            if (canal == null)
            {
                return NotFound();
            }


            await CargarCategorias(
                canal.IdCategoria);


            return View(canal);
        }


        // ==========================================
        // EDITAR - POST
        // ==========================================

        [HttpPost("/admin/canales/editar/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(
            int id,
            Canal formulario,
            IFormFile? imagenArchivo,
            bool eliminarImagen = false)
        {
            if (id != formulario.IdCanal)
            {
                return BadRequest();
            }


            var canal =
                await _context.Canales
                    .FirstOrDefaultAsync(
                        c => c.IdCanal == id);

            if (canal == null)
            {
                return NotFound();
            }


            if (!ModelState.IsValid)
            {
                formulario.Imagen =
                    canal.Imagen;

                await CargarCategorias(
                    formulario.IdCategoria);

                return View(formulario);
            }


            var enlaceNuevo =
                formulario.EnlaceTelegram.Trim();


            // ======================================
            // COMPROBAR DUPLICADO
            // ======================================

            var existeEnlace =
                await _context.Canales
                    .AnyAsync(c =>
                        c.IdCanal != id &&
                        c.EnlaceTelegram ==
                            enlaceNuevo);

            if (existeEnlace)
            {
                ModelState.AddModelError(
                    nameof(formulario.EnlaceTelegram),
                    "Ya existe otro canal con este enlace de Telegram."
                );

                formulario.Imagen =
                    canal.Imagen;

                await CargarCategorias(
                    formulario.IdCategoria);

                return View(formulario);
            }


            // ======================================
            // CONSERVAR IMAGEN ANTERIOR
            // ======================================

            var imagenAnterior =
                canal.Imagen;

            string? imagenNueva = null;


            // ======================================
            // GUARDAR NUEVA IMAGEN
            // ======================================

            if (imagenArchivo != null &&
                imagenArchivo.Length > 0)
            {
                try
                {
                    imagenNueva =
                        await _imagenService
                            .GuardarCanalAsync(
                                imagenArchivo);
                }
                catch (InvalidOperationException ex)
                {
                    ModelState.AddModelError(
                        "imagenArchivo",
                        ex.Message
                    );

                    formulario.Imagen =
                        imagenAnterior;

                    await CargarCategorias(
                        formulario.IdCategoria);

                    return View(formulario);
                }
            }


            // ======================================
            // ACTUALIZAR DATOS
            // ======================================

            canal.Nombre =
                formulario.Nombre.Trim();

            canal.IdCategoria =
                formulario.IdCategoria;

            canal.UsernameTelegram =
                formulario.UsernameTelegram?
                    .Trim()
                    .TrimStart('@');

            canal.EnlaceTelegram =
                enlaceNuevo;

            canal.Descripcion =
                formulario.Descripcion?.Trim();

            canal.CantidadMiembros =
                formulario.CantidadMiembros;

            canal.Idioma =
                formulario.Idioma?.Trim();

            canal.Pais =
                formulario.Pais?.Trim();

            canal.EsDestacado =
                formulario.EsDestacado;

            canal.Estado =
                formulario.Estado;

            canal.FechaActualizacion =
                DateTime.Now;


            // IMPORTANTE:
            // NO modificamos:
            //
            // canal.Slug
            // canal.Visitas
            // canal.Clicks
            // canal.FechaRegistro


            // ======================================
            // DECIDIR IMAGEN
            // ======================================

            if (imagenNueva != null)
            {
                canal.Imagen =
                    imagenNueva;
            }
            else if (eliminarImagen)
            {
                canal.Imagen =
                    null;
            }
            else
            {
                canal.Imagen =
                    imagenAnterior;
            }


            // ======================================
            // GUARDAR SQL
            // ======================================

            try
            {
                await _context.SaveChangesAsync();
            }
            catch
            {
                // Si se había creado una nueva imagen
                // pero SQL falló, la quitamos.
                if (imagenNueva != null)
                {
                    _imagenService.Eliminar(
                        imagenNueva);
                }


                formulario.Imagen =
                    imagenAnterior;


                ModelState.AddModelError(
                    "",
                    "No se pudieron guardar los cambios."
                );


                await CargarCategorias(
                    formulario.IdCategoria);


                return View(formulario);
            }


            // ======================================
            // BORRAR IMAGEN ANTERIOR
            // SOLO DESPUÉS DE GUARDAR SQL
            // ======================================

            if (imagenNueva != null &&
                !string.IsNullOrWhiteSpace(
                    imagenAnterior))
            {
                _imagenService.Eliminar(
                    imagenAnterior);
            }
            else if (eliminarImagen &&
                     !string.IsNullOrWhiteSpace(
                         imagenAnterior))
            {
                _imagenService.Eliminar(
                    imagenAnterior);
            }


            TempData["Exito"] =
                "Canal actualizado correctamente.";


            return RedirectToAction(
                nameof(Index));
        }


        // ==========================================
        // ACTIVAR / DESACTIVAR
        // ==========================================

        [HttpPost("/admin/canales/estado/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Estado(int id)
        {
            var canal =
                await _context.Canales
                    .FirstOrDefaultAsync(
                        c => c.IdCanal == id);

            if (canal == null)
            {
                return NotFound();
            }


            canal.Estado =
                canal.Estado == "Activo"
                    ? "Inactivo"
                    : "Activo";


            canal.FechaActualizacion =
                DateTime.Now;


            await _context.SaveChangesAsync();


            TempData["Exito"] =
                canal.Estado == "Activo"
                    ? "Canal activado."
                    : "Canal desactivado.";


            return RedirectToAction(
                nameof(Index));
        }


        // ==========================================
        // DESTACAR / QUITAR DESTACADO
        // ==========================================

        [HttpPost("/admin/canales/destacado/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Destacado(
            int id)
        {
            var canal =
                await _context.Canales
                    .FirstOrDefaultAsync(
                        c => c.IdCanal == id);

            if (canal == null)
            {
                return NotFound();
            }


            canal.EsDestacado =
                !canal.EsDestacado;


            canal.FechaActualizacion =
                DateTime.Now;


            await _context.SaveChangesAsync();


            TempData["Exito"] =
                canal.EsDestacado
                    ? "Canal marcado como destacado."
                    : "Canal removido de destacados.";


            return RedirectToAction(
                nameof(Index));
        }


        // ==========================================
        // CARGAR CATEGORÍAS
        // ==========================================

        private async Task CargarCategorias(
            int? seleccionada = null)
        {
            var categorias =
                await _context.Categorias
                    .Where(c => c.Estado)
                    .OrderBy(c => c.Nombre)
                    .ToListAsync();


            ViewBag.Categorias =
                new SelectList(
                    categorias,
                    "IdCategoria",
                    "Nombre",
                    seleccionada
                );
        }
    }
}