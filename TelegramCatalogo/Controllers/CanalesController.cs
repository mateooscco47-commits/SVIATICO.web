using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TelegramCatalogo.Data;

namespace TelegramCatalogo.Controllers
{
    public class CanalesController : Controller
    {
        private readonly AplicacionDbContexto _context;

        public CanalesController(AplicacionDbContexto context)
        {
            _context = context;
        }

        public IActionResult Index(string? buscar, int? categoria)
        {
            var consulta = _context.Canales
                .Include(c => c.Categoria)
                .Where(c => c.Estado == "Activo")
                .AsQueryable();

            // Buscar por nombre, username o descripción
            if (!string.IsNullOrWhiteSpace(buscar))
            {
                consulta = consulta.Where(c =>
                    c.Nombre.Contains(buscar) ||
                    (c.UsernameTelegram != null &&
                     c.UsernameTelegram.Contains(buscar)) ||
                    (c.Descripcion != null &&
                     c.Descripcion.Contains(buscar))
                );
            }

            // Filtrar por categoría
            if (categoria.HasValue)
            {
                consulta = consulta.Where(c =>
                    c.IdCategoria == categoria.Value);
            }

            ViewBag.Buscar = buscar;
            ViewBag.CategoriaSeleccionada = categoria;

            ViewBag.Categorias = _context.Categorias
                .Where(c => c.Estado)
                .OrderBy(c => c.Nombre)
                .ToList();

            var canales = consulta
                .OrderByDescending(c => c.EsDestacado)
                .ThenByDescending(c => c.FechaRegistro)
                .ToList();

            return View(canales);
        }

        public IActionResult Detalle(int id)
        {
            var canal = _context.Canales
                .Include(c => c.Categoria)
                .FirstOrDefault(c =>
                    c.IdCanal == id &&
                    c.Estado == "Activo");

            if (canal == null)
            {
                return NotFound();
            }

            // Registrar una visita
            canal.Visitas++;

            _context.SaveChanges();

            return View(canal);
        }
        public IActionResult IrTelegram(int id)
        {
            var canal = _context.Canales
                .FirstOrDefault(c =>
                    c.IdCanal == id &&
                    c.Estado == "Activo");

            if (canal == null)
            {
                return NotFound();
            }

            canal.Clicks++;

            _context.SaveChanges();

            return Redirect(canal.EnlaceTelegram);
        }

        [HttpGet("/canal/{slug}")]
        public IActionResult DetalleSlug(string slug)
        {
            var canal = _context.Canales
                .Include(c => c.Categoria)
                .FirstOrDefault(c =>
                    c.Slug == slug &&
                    c.Estado == "Activo");

            if (canal == null)
            {
                return NotFound();
            }

            canal.Visitas++;

            _context.SaveChanges();

            return View("Detalle", canal);
        }
        [HttpGet("/canales/{slug}")]
        public IActionResult Categoria(string slug)
        {
            var categoria = _context.Categorias
                .FirstOrDefault(c =>
                    c.Slug == slug &&
                    c.Estado);

            if (categoria == null)
            {
                return NotFound();
            }

            var canales = _context.Canales
                .Include(c => c.Categoria)
                .Where(c =>
                    c.IdCategoria == categoria.IdCategoria &&
                    c.Estado == "Activo")
                .OrderByDescending(c => c.EsDestacado)
                .ThenByDescending(c => c.FechaRegistro)
                .ToList();

            ViewBag.Categorias = _context.Categorias
                .Where(c => c.Estado)
                .OrderBy(c => c.Nombre)
                .ToList();

            ViewBag.CategoriaActual = categoria;

            return View("Index", canales);
        }
    }
}