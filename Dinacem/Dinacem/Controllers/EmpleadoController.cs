using Dinacem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Dinacem.Controllers
{
    public class EmpleadoController : Controller
    {
        private readonly AplicacionDbContexto _context;

        public EmpleadoController(AplicacionDbContexto context)
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


            // ============================================
            // OBTENER USUARIO CON ROL Y ZONA
            // ============================================

            var usuario =
                await _context.Usuarios
                    .Include(u => u.Rol)
                    .Include(u => u.Zona)
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
            string celular)
        {
            // ============================================
            // VALIDAR SESIÓN
            // ============================================

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


            // ============================================
            // BUSCAR USUARIO
            // ============================================

            var usuario =
                await _context.Usuarios
                    .Include(u => u.Zona)
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


            // ============================================
            // LIMPIAR DATOS
            // ============================================

            nombres =
                nombres?.Trim() ?? string.Empty;

            apellidos =
                apellidos?.Trim() ?? string.Empty;

            correo =
                correo?.Trim() ?? string.Empty;

            celular =
                celular?.Trim() ?? string.Empty;


            // ============================================
            // VALIDAR NOMBRES
            // ============================================

            if (string.IsNullOrWhiteSpace(nombres))
            {
                TempData["error"] =
                    "Debe ingresar sus nombres.";

                return RedirectToAction(
                    nameof(Perfil));
            }


            // ============================================
            // VALIDAR APELLIDOS
            // ============================================

            if (string.IsNullOrWhiteSpace(apellidos))
            {
                TempData["error"] =
                    "Debe ingresar sus apellidos.";

                return RedirectToAction(
                    nameof(Perfil));
            }


            // ============================================
            // VALIDAR CORREO
            // ============================================

            if (string.IsNullOrWhiteSpace(correo))
            {
                TempData["error"] =
                    "Debe ingresar su correo.";

                return RedirectToAction(
                    nameof(Perfil));
            }


            // ============================================
            // VALIDAR CELULAR
            // ============================================

            if (!string.IsNullOrWhiteSpace(celular))
            {
                if (celular.Length != 9 ||
                    !celular.All(char.IsDigit))
                {
                    TempData["error"] =
                        "El celular debe contener exactamente 9 dígitos.";

                    return RedirectToAction(
                        nameof(Perfil));
                }
            }


            // ============================================
            // CORREO ÚNICO
            // ============================================

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


            // ============================================
            // ACTUALIZAR DATOS PERMITIDOS
            // ============================================

            usuario.Nombres =
                nombres;

            usuario.Apellidos =
                apellidos;

            usuario.Correo =
                correo;

            usuario.Celular =
                celular;


            // ============================================
            // NO MODIFICAR ZONA
            // ============================================
            //
            // La zona es administrada desde Gestión de
            // Usuarios y no puede modificarse desde el
            // perfil del empleado.
            //
            // NO HACER:
            //
            // usuario.Zona = zona;
            //
            // NI:
            //
            // usuario.IdZona = ...
            //
            // ============================================


            // ============================================
            // GUARDAR CAMBIOS
            // ============================================

            await _context.SaveChangesAsync();


            // ============================================
            // NOTIFICACIÓN
            // ============================================

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
            // ============================================
            // VALIDAR SESIÓN
            // ============================================

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


            // ============================================
            // BUSCAR USUARIO
            // ============================================

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


            // ============================================
            // LIMPIAR DATOS
            // ============================================

            contraseniaActual =
                contraseniaActual?.Trim() ?? string.Empty;

            nuevaContrasenia =
                nuevaContrasenia?.Trim() ?? string.Empty;

            confirmarContrasenia =
                confirmarContrasenia?.Trim() ?? string.Empty;


            // ============================================
            // VALIDAR CONTRASEÑA ACTUAL
            // ============================================

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


            // ============================================
            // VALIDAR NUEVA CONTRASEÑA
            // ============================================

            if (string.IsNullOrWhiteSpace(
                    nuevaContrasenia))
            {
                TempData["error"] =
                    "Debe ingresar una nueva contraseña.";

                return RedirectToAction(
                    nameof(Perfil));
            }


            // ============================================
            // LONGITUD MÍNIMA
            // ============================================

            if (nuevaContrasenia.Length < 8)
            {
                TempData["error"] =
                    "La nueva contraseña debe tener al menos 8 caracteres.";

                return RedirectToAction(
                    nameof(Perfil));
            }


            // ============================================
            // VALIDAR MAYÚSCULA
            // ============================================

            if (!nuevaContrasenia.Any(char.IsUpper))
            {
                TempData["error"] =
                    "La nueva contraseña debe contener al menos una letra mayúscula.";

                return RedirectToAction(
                    nameof(Perfil));
            }


            // ============================================
            // VALIDAR NÚMERO
            // ============================================

            if (!nuevaContrasenia.Any(char.IsDigit))
            {
                TempData["error"] =
                    "La nueva contraseña debe contener al menos un número.";

                return RedirectToAction(
                    nameof(Perfil));
            }


            // ============================================
            // VALIDAR CARÁCTER ESPECIAL
            // ============================================

            if (!nuevaContrasenia.Any(c =>
                    !char.IsLetterOrDigit(c)))
            {
                TempData["error"] =
                    "La nueva contraseña debe contener al menos un carácter especial.";

                return RedirectToAction(
                    nameof(Perfil));
            }


            // ============================================
            // CONFIRMAR CONTRASEÑA
            // ============================================

            if (nuevaContrasenia !=
                confirmarContrasenia)
            {
                TempData["error"] =
                    "Las nuevas contraseñas no coinciden.";

                return RedirectToAction(
                    nameof(Perfil));
            }


            // ============================================
            // NO REPETIR CONTRASEÑA ACTUAL
            // ============================================

            if (nuevaContrasenia ==
                contraseniaActual)
            {
                TempData["error"] =
                    "La nueva contraseña debe ser diferente a la actual.";

                return RedirectToAction(
                    nameof(Perfil));
            }


            // ============================================
            // ACTUALIZAR CONTRASEÑA
            // ============================================

            usuario.Contrasenia =
                nuevaContrasenia;


            // ============================================
            // GUARDAR
            // ============================================

            await _context.SaveChangesAsync();


            // ============================================
            // NOTIFICACIÓN
            // ============================================

            TempData["mensaje"] =
                "Su contraseña fue actualizada correctamente.";


            return RedirectToAction(
                nameof(Perfil));
        }
    }
}