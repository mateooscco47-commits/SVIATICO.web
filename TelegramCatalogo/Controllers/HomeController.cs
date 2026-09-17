using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TelegramCatalogo.Data;

namespace TelegramCatalogo.Controllers
{
    public class HomeController : Controller
    {
        private readonly AplicacionDbContexto _context;

        public HomeController(AplicacionDbContexto context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var canales = _context.Canales
                .Include(c => c.Categoria)
                .Where(c => c.Estado == "Activo")
                .OrderByDescending(c => c.FechaRegistro)
                .Take(8)
                .ToList();

            ViewBag.Categorias = _context.Categorias
                .Where(c => c.Estado)
                .OrderBy(c => c.Nombre)
                .ToList();

            return View(canales);
        }
    }
}