using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TelegramCatalogo.Data;
using TelegramCatalogo.Models;
using TelegramCatalogo.Services;

namespace TelegramCatalogo.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class AdminCategoriasController : Controller
    {
        private readonly AplicacionDbContexto _context;
        private readonly SlugService _slugService;

        public AdminCategoriasController(
    AplicacionDbContexto context,
    SlugService slugService)
        {
            _context = context;
            _slugService = slugService;
        }

        // ==========================================
        // LISTADO
        // ==========================================

        [HttpGet("/admin/categorias")]
        public async Task<IActionResult> Index()
        {
            var categorias = await _context.Categorias
                .Include(c => c.Canales)
                .OrderBy(c => c.Nombre)
                .ToListAsync();

            return View(categorias);
        }


        // ==========================================
        // CREAR - GET
        // ==========================================

        [HttpGet("/admin/categorias/nueva")]
        public IActionResult Crear()
        {
            return View(new Categoria
            {
                Estado = true,
                Icono = "bi-grid"
            });
        }


        // ==========================================
        // CREAR - POST
        // ==========================================

        [HttpPost("/admin/categorias/nueva")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(
            Categoria categoria)
        {
            if (string.IsNullOrWhiteSpace(categoria.Nombre))
            {
                ModelState.AddModelError(
                    nameof(categoria.Nombre),
                    "El nombre es obligatorio."
                );
            }

            if (!ModelState.IsValid)
            {
                return View(categoria);
            }

            categoria.Nombre =
                categoria.Nombre.Trim();

            categoria.Descripcion =
                categoria.Descripcion?.Trim();

            categoria.Icono =
                string.IsNullOrWhiteSpace(categoria.Icono)
                    ? "bi-grid"
                    : categoria.Icono.Trim();

            categoria.Slug =
    await _slugService
        .GenerarSlugCategoriaAsync(
            categoria.Nombre);

            _context.Categorias.Add(categoria);

            await _context.SaveChangesAsync();

            TempData["Exito"] =
                "La categoría fue creada correctamente.";

            return RedirectToAction(nameof(Index));
        }


        // ==========================================
        // EDITAR - GET
        // ==========================================

        [HttpGet("/admin/categorias/editar/{id:int}")]
        public async Task<IActionResult> Editar(int id)
        {
            var categoria =
                await _context.Categorias
                    .FirstOrDefaultAsync(c =>
                        c.IdCategoria == id);

            if (categoria == null)
            {
                return NotFound();
            }

            return View(categoria);
        }


        // ==========================================
        // EDITAR - POST
        // ==========================================

        [HttpPost("/admin/categorias/editar/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(
            int id,
            Categoria formulario)
        {
            if (id != formulario.IdCategoria)
            {
                return BadRequest();
            }

            var categoria =
                await _context.Categorias
                    .FirstOrDefaultAsync(c =>
                        c.IdCategoria == id);

            if (categoria == null)
            {
                return NotFound();
            }

            if (string.IsNullOrWhiteSpace(formulario.Nombre))
            {
                ModelState.AddModelError(
                    nameof(formulario.Nombre),
                    "El nombre es obligatorio."
                );
            }

            if (!ModelState.IsValid)
            {
                return View(formulario);
            }

            var nombreNuevo =
                formulario.Nombre.Trim();

            

            categoria.Nombre = nombreNuevo;

            categoria.Descripcion =
                formulario.Descripcion?.Trim();

            categoria.Icono =
                string.IsNullOrWhiteSpace(formulario.Icono)
                    ? "bi-grid"
                    : formulario.Icono.Trim();

            categoria.Estado =
                formulario.Estado;

            await _context.SaveChangesAsync();

            TempData["Exito"] =
                "La categoría fue actualizada correctamente.";

            return RedirectToAction(nameof(Index));
        }


        // ==========================================
        // ACTIVAR / DESACTIVAR
        // ==========================================

        [HttpPost("/admin/categorias/estado/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CambiarEstado(int id)
        {
            var categoria =
                await _context.Categorias
                    .FirstOrDefaultAsync(c =>
                        c.IdCategoria == id);

            if (categoria == null)
            {
                return NotFound();
            }

            categoria.Estado = !categoria.Estado;

            await _context.SaveChangesAsync();

            TempData["Exito"] =
                categoria.Estado
                    ? "La categoría fue activada."
                    : "La categoría fue desactivada.";

            return RedirectToAction(nameof(Index));
        }


        


        
    }
}