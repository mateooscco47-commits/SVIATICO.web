using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Dinacem.Models;

namespace Dinacem.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly AplicacionDbContexto _context;

        public HomeController(
            ILogger<HomeController> logger,
            AplicacionDbContexto context)
        {
            _logger = logger;
            _context = context;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Login(string usuario, string password)
        {
            var user = _context.Usuarios
                .FirstOrDefault(x =>
                    x.UsuarioAcceso == usuario &&
                    x.Contrasenia == password);

            if (user == null)
            {
                TempData["error"] = "Usuario o contraseña incorrectos.";
                return RedirectToAction("Index");
            }

            if (user.IdRol == 1)
            {
                return RedirectToAction("Index", "Principal");
            }

            if (user.IdRol == 2)
            {
                return RedirectToAction("Index", "Empleado");
            }

            TempData["error"] = "El usuario no tiene un rol válido.";
            return RedirectToAction("Index");
        }

        public IActionResult Logout()
        {
            return RedirectToAction("Index");
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }
    }
}