using System.Diagnostics;
using Dinacem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
            // Si había sesión anterior, no la borramos aquí.
            return View();
        }


        // =========================================
        // PROCESAR LOGIN
        // =========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(
            string usuario,
            string password)
        {
            usuario =
                usuario?.Trim() ?? string.Empty;

            password =
                password?.Trim() ?? string.Empty;


            // =========================================
            // VALIDAR CAMPOS
            // =========================================

            if (string.IsNullOrWhiteSpace(usuario) ||
                string.IsNullOrWhiteSpace(password))
            {
                return Json(new
                {
                    success = false,
                    message =
                        "Ingrese usuario y contraseña."
                });
            }


            // =========================================
            // BUSCAR USUARIO
            // =========================================

            var user =
                await _context.Usuarios
                    .Include(u => u.Rol)
                    .FirstOrDefaultAsync(u =>
                        u.UsuarioAcceso == usuario);

            if (user == null)
            {
                return Json(new
                {
                    success = false,
                    message =
                        "Usuario o contraseña incorrectos."
                });
            }


            // =========================================
            // VALIDAR ESTADO
            // =========================================

            if (!user.Estado)
            {
                return Json(new
                {
                    success = false,
                    message =
                        "El usuario se encuentra desactivado. " +
                        "Comuníquese con el administrador."
                });
            }


            // =========================================
            // VALIDAR CONTRASEÑA
            // SIN CIFRADO
            // =========================================

            if (user.Contrasenia != password)
            {
                return Json(new
                {
                    success = false,
                    message =
                        "Usuario o contraseña incorrectos."
                });
            }


            // =========================================
            // VALIDAR ROL
            // =========================================

            if (user.Rol == null)
            {
                return Json(new
                {
                    success = false,
                    message =
                        "El usuario no tiene un rol asignado."
                });
            }


            // =========================================
            // LIMPIAR SESIÓN ANTERIOR
            // =========================================

            HttpContext.Session.Clear();


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
                user.Rol.Nombre);

            HttpContext.Session.SetString(
                "UsuarioAcceso",
                user.UsuarioAcceso ?? string.Empty);


            // =========================================
            // REDIRECCIÓN SEGÚN ROL
            //
            // 1 = Administrador
            // 2 = Empleado
            // =========================================

            string? redirectUrl =
                user.IdRol switch
                {
                    1 => Url.Action(
                        "Index",
                        "Principal"),

                    2 => Url.Action(
                        "Index",
                        "Empleado"),

                    _ => null
                };


            // =========================================
            // ROL NO RECONOCIDO
            // =========================================

            if (string.IsNullOrWhiteSpace(
                    redirectUrl))
            {
                HttpContext.Session.Clear();

                return Json(new
                {
                    success = false,
                    message =
                        "El usuario no tiene un rol válido."
                });
            }


            // =========================================
            // LOGIN CORRECTO
            // =========================================

            return Json(new
            {
                success = true,
                nombre =
                    $"{user.Nombres} {user.Apellidos}",
                rol =
                    user.Rol.Nombre,
                redirectUrl
            });
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

            return RedirectToAction(
                nameof(Index));
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
            return View(
                new ErrorViewModel
                {
                    RequestId =
                        Activity.Current?.Id ??
                        HttpContext.TraceIdentifier
                });
        }
    }
}