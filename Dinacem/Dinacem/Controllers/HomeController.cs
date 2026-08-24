using System.Diagnostics;
using Dinacem.Models;
using Microsoft.AspNetCore.Mvc;

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

        // =========================================
        // MOSTRAR LOGIN
        // =========================================
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        // =========================================
        // PROCESAR LOGIN
        // =========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(string usuario, string password)
        {
            usuario = usuario?.Trim() ?? string.Empty;
            password = password?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(usuario) ||
                string.IsNullOrWhiteSpace(password))
            {
                TempData["error"] = "Ingrese usuario y contraseña.";
                return RedirectToAction(nameof(Index));
            }

            var user = _context.Usuarios
                .FirstOrDefault(x =>
                    x.UsuarioAcceso == usuario);

            // Usuario no existe
            if (user == null)
            {
                TempData["error"] = "Usuario o contraseña incorrectos.";
                return RedirectToAction(nameof(Index));
            }

            // Usuario desactivado
            if (!user.Estado)
            {
                TempData["error"] =
                    "El usuario se encuentra desactivado. Comuníquese con el administrador.";

                return RedirectToAction(nameof(Index));
            }

            // Contraseña incorrecta
            if (user.Contrasenia != password)
            {
                TempData["error"] = "Usuario o contraseña incorrectos.";
                return RedirectToAction(nameof(Index));
            }

            // =========================================
            // OBTENER ROL
            // =========================================

            var rol = _context.Roles
                .FirstOrDefault(r => r.IdRol == user.IdRol);

            if (rol == null)
            {
                TempData["error"] = "El usuario no tiene un rol asignado.";
                return RedirectToAction(nameof(Index));
            }

            // =========================================
            // GUARDAR SESIÓN
            // =========================================

            HttpContext.Session.SetInt32(
                "IdUsuario",
                user.IdUsuario);

            HttpContext.Session.SetInt32(
                "IdRol",
                user.IdRol);

            HttpContext.Session.SetString(
                "NombreUsuario",
                $"{user.Nombres} {user.Apellidos}");

            HttpContext.Session.SetString(
                "RolUsuario",
                rol.Nombre);

            // =========================================
            // REDIRECCIÓN SEGÚN ROL
            // =========================================

            // 1 = Administrador
            if (user.IdRol == 1)
            {
                return RedirectToAction(
                    "Index",
                    "Principal");
            }

            // 2 = Supervisor
            if (user.IdRol == 2)
            {
                return RedirectToAction(
                    "Index",
                    "Supervisor");
            }

            // 3 = Representante
            if (user.IdRol == 3)
            {
                return RedirectToAction(
                    "Index",
                    "Empleado");
            }

            // =========================================
            // ROL NO RECONOCIDO
            // =========================================

            HttpContext.Session.Clear();

            TempData["error"] =
                "El usuario no tiene un rol válido.";

            return RedirectToAction(nameof(Index));
        }

        // =========================================
        // CERRAR SESIÓN
        // =========================================
        [HttpGet]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();

            TempData["mensaje"] =
                "La sesión se cerró correctamente.";

            return RedirectToAction(nameof(Index));
        }

        // =========================================
        // PRIVACY
        // =========================================
        public IActionResult Privacy()
        {
            return View();
        }

        // =========================================
        // ERROR
        // =========================================
        [ResponseCache(
            Duration = 0,
            Location = ResponseCacheLocation.None,
            NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId =
                    Activity.Current?.Id ??
                    HttpContext.TraceIdentifier
            });
        }
    }
}