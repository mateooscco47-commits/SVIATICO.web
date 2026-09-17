using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TelegramCatalogo.Data;

namespace TelegramCatalogo.Controllers
{
    public class AdminController : Controller
    {
        private readonly AplicacionDbContexto _context;

        public AdminController(AplicacionDbContexto context)
        {
            _context = context;
        }

        // ========================================
        // LOGIN GET
        // ========================================

        [AllowAnonymous]
        [HttpGet("/admin/login")]
        public IActionResult Login()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction(nameof(Index));
            }

            return View();
        }


        // ========================================
        // LOGIN POST
        // ========================================

        [AllowAnonymous]
        [HttpPost("/admin/login")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(
            string correo,
            string contrasenia)
        {
            if (string.IsNullOrWhiteSpace(correo) ||
                string.IsNullOrWhiteSpace(contrasenia))
            {
                ViewBag.Error =
                    "Ingresa tu correo y contraseña.";

                return View();
            }

            correo = correo.Trim().ToLower();

            var usuario = await _context.Usuarios
                .AsNoTracking()
                .FirstOrDefaultAsync(u =>
                    u.Correo.ToLower() == correo &&
                    u.Estado &&
                    u.Rol == "Administrador");

            if (usuario == null)
            {
                ViewBag.Error =
                    "Correo o contraseña incorrectos.";

                return View();
            }

            bool passwordCorrecto;

            try
            {
                passwordCorrecto =
                    BCrypt.Net.BCrypt.Verify(
                        contrasenia,
                        usuario.Contrasenia
                    );
            }
            catch
            {
                passwordCorrecto = false;
            }

            if (!passwordCorrecto)
            {
                ViewBag.Error =
                    "Correo o contraseña incorrectos.";

                return View();
            }

            var claims = new List<Claim>
            {
                new Claim(
                    ClaimTypes.NameIdentifier,
                    usuario.IdUsuario.ToString()
                ),

                new Claim(
                    ClaimTypes.Name,
                    usuario.Nombres
                ),

                new Claim(
                    ClaimTypes.Email,
                    usuario.Correo
                ),

                new Claim(
                    ClaimTypes.Role,
                    usuario.Rol
                )
            };

            var identity = new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme
            );

            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal
            );

            return RedirectToAction(nameof(Index));
        }


        // ========================================
        // DASHBOARD
        // ========================================

        [Authorize(Roles = "Administrador")]
        [HttpGet("/admin")]
        public async Task<IActionResult> Index()
        {
            ViewBag.TotalCanales =
                await _context.Canales.CountAsync();

            ViewBag.CanalesActivos =
                await _context.Canales.CountAsync(
                    c => c.Estado == "Activo"
                );

            ViewBag.Destacados =
                await _context.Canales.CountAsync(
                    c =>
                        c.Estado == "Activo" &&
                        c.EsDestacado
                );

            ViewBag.TotalCategorias =
                await _context.Categorias.CountAsync(
                    c => c.Estado
                );

            ViewBag.TotalVisitas =
                await _context.Canales.SumAsync(
                    c => (long)c.Visitas
                );

            ViewBag.TotalClicks =
                await _context.Canales.SumAsync(
                    c => (long)c.Clicks
                );
            ViewBag.SolicitudesPendientes =
    await _context.SolicitudesCanal
        .CountAsync(s =>
            s.Estado == "Pendiente"
        );

            return View();
        }


        // ========================================
        // LOGOUT
        // ========================================

        [Authorize]
        [HttpPost("/admin/logout")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme
            );

            return RedirectToAction(nameof(Login));
        }
    }
}