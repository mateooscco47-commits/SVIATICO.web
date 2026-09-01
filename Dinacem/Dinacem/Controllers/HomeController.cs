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
            // Si había una sesión anterior,
            // no la eliminamos aquí.
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
            // VALIDAR ESTADO DEL USUARIO
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
            // VALIDAR ESTADO DEL ROL
            // =========================================

            if (!user.Rol.Estado)
            {
                return Json(new
                {
                    success = false,
                    message =
                        "El rol asignado al usuario se encuentra " +
                        "desactivado."
                });
            }


            // =========================================
            // VALIDAR ROLES PERMITIDOS
            // =========================================

            var rolesPermitidos = new[]
            {
            1, // Administrador
            2, // Supervisor
            3, // Auditor
            4, // Representante DINACEN
            5  // Representante Laboratorio
        };

            if (!rolesPermitidos.Contains(user.IdRol))
            {
                return Json(new
                {
                    success = false,
                    message =
                        "El usuario no tiene un rol válido " +
                        "para ingresar al sistema."
                });
            }


            // =========================================
            // LIMPIAR SESIÓN ANTERIOR
            // =========================================

            HttpContext.Session.Clear();


            // =========================================
            // GUARDAR DATOS DE SESIÓN
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
            // 2 = Supervisor
            // 3 = Auditor
            // 4 = Representante DINACEN
            // 5 = Representante Laboratorio
            //
            // ADMINISTRADOR → Principal
            // TODOS LOS DEMÁS → Empleado
            // =========================================

            string? redirectUrl =
                user.IdRol switch
                {
                    // Administrador
                    1 => Url.Action(
                        "Index",
                        "Principal"),

                    // Supervisor
                    2 => Url.Action(
                        "Index",
                        "Empleado"),

                    // Auditor
                    3 => Url.Action(
                        "Index",
                        "Empleado"),

                    // Representante DINACEN
                    4 => Url.Action(
                        "Index",
                        "Empleado"),

                    // Representante Laboratorio
                    5 => Url.Action(
                        "Index",
                        "Empleado"),

                    // Rol no reconocido
                    _ => null
                };


            // =========================================
            // VALIDAR REDIRECCIÓN
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
            // Limpiar todos los datos de la sesión
            HttpContext.Session.Clear();

            // Mensaje que será enviado únicamente al Login
            TempData["LogoutSuccess"] =
                "La sesión se cerró correctamente.";

            // Redirigir al Login
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