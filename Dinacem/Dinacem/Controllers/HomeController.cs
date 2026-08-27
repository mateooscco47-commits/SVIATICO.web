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
        // PROCESAR LOGIN (Respuesta AJAX en JSON)
        // =========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(string usuario, string password)
        {
            usuario = usuario?.Trim() ?? string.Empty;
            password = password?.Trim() ?? string.Empty;

            // 1. Validar campos vacíos
            if (string.IsNullOrWhiteSpace(usuario) || string.IsNullOrWhiteSpace(password))
            {
                return Json(new { success = false, message = "Ingrese usuario y contraseña." });
            }

            // 2. Buscar usuario en base de datos
            var user = _context.Usuarios
                .FirstOrDefault(x => x.UsuarioAcceso == usuario);

            if (user == null)
            {
                return Json(new { success = false, message = "Usuario o contraseña incorrectos." });
            }

            // 3. Validar estado del usuario
            if (!user.Estado)
            {
                return Json(new { success = false, message = "El usuario se encuentra desactivado. Comuníquese con el administrador." });
            }

            // 4. Validar contraseña
            if (user.Contrasenia != password)
            {
                return Json(new { success = false, message = "Usuario o contraseña incorrectos." });
            }

            // 5. Obtener Rol
            var rol = _context.Roles
                .FirstOrDefault(r => r.IdRol == user.IdRol);

            if (rol == null)
            {
                return Json(new { success = false, message = "El usuario no tiene un rol asignado." });
            }

            // =========================================
            // GUARDAR SESIÓN
            // =========================================
            HttpContext.Session.SetInt32("IdUsuario", user.IdUsuario);
            HttpContext.Session.SetInt32("IdRol", user.IdRol);
            HttpContext.Session.SetString("NombreUsuario", $"{user.Nombres} {user.Apellidos}");
            HttpContext.Session.SetString("RolUsuario", rol.Nombre);

            // =========================================
            // REDIRECCIÓN SEGÚN ROL
            // =========================================
            string redirectUrl = user.IdRol switch
            {
                1 => Url.Action("Index", "Principal"),
                2 => Url.Action("Index", "Supervisor"),
                3 => Url.Action("Index", "Empleado"),
                _ => null
            };

            // Rol no reconocido
            if (string.IsNullOrEmpty(redirectUrl))
            {
                HttpContext.Session.Clear();
                return Json(new { success = false, message = "El usuario no tiene un rol válido." });
            }

            // Retorno exitoso
            return Json(new { success = true, nombre = user.Nombres, redirectUrl });
        }

        // =========================================
        // CERRAR SESIÓN
        // =========================================
        [HttpGet]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();

            TempData["mensaje"] = "La sesión se cerró correctamente.";

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
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }
    }
}