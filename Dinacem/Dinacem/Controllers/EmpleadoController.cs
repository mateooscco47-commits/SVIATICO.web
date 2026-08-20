using Dinacem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Dinacem.Controllers
{
    public class EmpleadoController : Controller
    {
        private readonly AplicacionDbContexto _context;

        public EmpleadoController(
            AplicacionDbContexto context)
        {
            _context = context;
        }

        // ============================================
        // PANEL PRINCIPAL DEL EMPLEADO
        // ============================================
        [HttpGet]
        public IActionResult Index()
        {
            var idUsuario =
                HttpContext.Session.GetInt32("IdUsuario");

            if (idUsuario == null)
            {
                TempData["error"] =
                    "Su sesión ha expirado.";

                return RedirectToAction(
                    "Index",
                    "Home");
            }

            return View();
        }

        // ============================================
        // MI PERFIL
        // ============================================
        [HttpGet]
        public async Task<IActionResult> Perfil()
        {
            var idUsuario =
                HttpContext.Session.GetInt32("IdUsuario");

            if (idUsuario == null)
            {
                TempData["error"] =
                    "Su sesión ha expirado.";

                return RedirectToAction(
                    "Index",
                    "Home");
            }

            var usuario =
                await _context.Usuarios
                    .Include(u => u.Rol)
                    .FirstOrDefaultAsync(u =>
                        u.IdUsuario ==
                        idUsuario.Value);

            if (usuario == null)
            {
                TempData["error"] =
                    "No se encontró el usuario.";

                return RedirectToAction(
                    nameof(Index));
            }

            return View(usuario);
        }

        // ============================================
        // ACTUALIZAR DATOS DEL PERFIL
        // ============================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ActualizarPerfil(
            string nombres,
            string apellidos,
            string correo,
            string celular,
            string zona)
        {
            var idUsuario =
                HttpContext.Session.GetInt32("IdUsuario");

            if (idUsuario == null)
            {
                TempData["error"] =
                    "Su sesión ha expirado.";

                return RedirectToAction(
                    "Index",
                    "Home");
            }

            var usuario =
                await _context.Usuarios
                    .FirstOrDefaultAsync(u =>
                        u.IdUsuario ==
                        idUsuario.Value);

            if (usuario == null)
            {
                TempData["error"] =
                    "No se encontró el usuario.";

                return RedirectToAction(
                    nameof(Index));
            }

            nombres =
                nombres?.Trim() ?? string.Empty;

            apellidos =
                apellidos?.Trim() ?? string.Empty;

            correo =
                correo?.Trim() ?? string.Empty;

            celular =
                celular?.Trim() ?? string.Empty;

            zona =
                zona?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(nombres))
            {
                TempData["error"] =
                    "Debe ingresar sus nombres.";

                return RedirectToAction(
                    nameof(Perfil));
            }

            if (string.IsNullOrWhiteSpace(apellidos))
            {
                TempData["error"] =
                    "Debe ingresar sus apellidos.";

                return RedirectToAction(
                    nameof(Perfil));
            }

            if (string.IsNullOrWhiteSpace(correo))
            {
                TempData["error"] =
                    "Debe ingresar su correo.";

                return RedirectToAction(
                    nameof(Perfil));
            }

            var correoExiste =
                await _context.Usuarios
                    .AnyAsync(u =>
                        u.Correo == correo &&
                        u.IdUsuario != idUsuario.Value);

            if (correoExiste)
            {
                TempData["error"] =
                    "El correo ya está registrado por otro usuario.";

                return RedirectToAction(
                    nameof(Perfil));
            }

            usuario.Nombres =
                nombres;

            usuario.Apellidos =
                apellidos;

            usuario.Correo =
                correo;

            usuario.Celular =
                celular;

            usuario.Zona =
                zona;

            await _context.SaveChangesAsync();

            TempData["mensaje"] =
                "Sus datos fueron actualizados correctamente.";

            return RedirectToAction(
                nameof(Perfil));
        }

        // ============================================
        // CAMBIAR CONTRASEÑA
        // SIN CIFRADO, POR AHORA
        // ============================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CambiarContrasenia(
            string contraseniaActual,
            string nuevaContrasenia,
            string confirmarContrasenia)
        {
            var idUsuario =
                HttpContext.Session.GetInt32("IdUsuario");

            if (idUsuario == null)
            {
                TempData["error"] =
                    "Su sesión ha expirado.";

                return RedirectToAction(
                    "Index",
                    "Home");
            }

            var usuario =
                await _context.Usuarios
                    .FirstOrDefaultAsync(u =>
                        u.IdUsuario ==
                        idUsuario.Value);

            if (usuario == null)
            {
                TempData["error"] =
                    "No se encontró el usuario.";

                return RedirectToAction(
                    nameof(Index));
            }

            if (string.IsNullOrWhiteSpace(
                    contraseniaActual))
            {
                TempData["error"] =
                    "Debe ingresar su contraseña actual.";

                return RedirectToAction(
                    nameof(Perfil));
            }

            if (usuario.Contrasenia !=
                contraseniaActual)
            {
                TempData["error"] =
                    "La contraseña actual es incorrecta.";

                return RedirectToAction(
                    nameof(Perfil));
            }

            if (string.IsNullOrWhiteSpace(
                    nuevaContrasenia))
            {
                TempData["error"] =
                    "Debe ingresar una nueva contraseña.";

                return RedirectToAction(
                    nameof(Perfil));
            }

            if (nuevaContrasenia.Length < 6)
            {
                TempData["error"] =
                    "La nueva contraseña debe tener al menos 6 caracteres.";

                return RedirectToAction(
                    nameof(Perfil));
            }

            if (nuevaContrasenia !=
                confirmarContrasenia)
            {
                TempData["error"] =
                    "Las nuevas contraseñas no coinciden.";

                return RedirectToAction(
                    nameof(Perfil));
            }

            if (nuevaContrasenia ==
                contraseniaActual)
            {
                TempData["error"] =
                    "La nueva contraseña debe ser diferente a la actual.";

                return RedirectToAction(
                    nameof(Perfil));
            }

            usuario.Contrasenia =
                nuevaContrasenia;

            await _context.SaveChangesAsync();

            TempData["mensaje"] =
                "Su contraseña fue actualizada correctamente.";

            return RedirectToAction(
                nameof(Perfil));
        }
    }
}