using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Dinacem.Models;

namespace Dinacem.Controllers
{
    public class UsuarioController : Controller
    {
        private readonly AplicacionDbContexto _context;

        public UsuarioController(
            AplicacionDbContexto context)
        {
            _context = context;
        }

        // =====================================
        // LISTAR USUARIOS
        // =====================================
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var usuarios = await _context.Usuarios
                .Include(u => u.Rol)
                .OrderBy(u => u.Nombres)
                .ThenBy(u => u.Apellidos)
                .ToListAsync();

            return View(usuarios);
        }

        // =====================================
        // CREAR USUARIO
        // =====================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            Usuario usuario)
        {
            // Validar rol
            if (usuario.IdRol != 1 &&
                usuario.IdRol != 2)
            {
                TempData["error"] =
                    $"El rol seleccionado no es válido. " +
                    $"IdRol recibido: {usuario.IdRol}";

                return RedirectToAction(
                    nameof(Index));
            }

            // Verificar que el rol realmente exista
            var existeRol =
                await _context.Roles
                    .AnyAsync(r =>
                        r.IdRol == usuario.IdRol &&
                        r.Estado);

            if (!existeRol)
            {
                TempData["error"] =
                    "El rol seleccionado no existe o está desactivado.";

                return RedirectToAction(
                    nameof(Index));
            }

            // Limpiar textos
            usuario.UsuarioAcceso =
                usuario.UsuarioAcceso?.Trim();

            usuario.Nombres =
                usuario.Nombres?.Trim();

            usuario.Apellidos =
                usuario.Apellidos?.Trim();

            usuario.Correo =
                usuario.Correo?.Trim();

            usuario.Celular =
                usuario.Celular?.Trim();

            usuario.Zona =
                usuario.Zona?.Trim();

            // Validaciones básicas
            if (string.IsNullOrWhiteSpace(
                    usuario.UsuarioAcceso))
            {
                TempData["error"] =
                    "Debe ingresar el usuario de acceso.";

                return RedirectToAction(
                    nameof(Index));
            }

            if (string.IsNullOrWhiteSpace(
                    usuario.Nombres))
            {
                TempData["error"] =
                    "Debe ingresar los nombres.";

                return RedirectToAction(
                    nameof(Index));
            }

            if (string.IsNullOrWhiteSpace(
                    usuario.Apellidos))
            {
                TempData["error"] =
                    "Debe ingresar los apellidos.";

                return RedirectToAction(
                    nameof(Index));
            }

            if (string.IsNullOrWhiteSpace(
                    usuario.Correo))
            {
                TempData["error"] =
                    "Debe ingresar el correo.";

                return RedirectToAction(
                    nameof(Index));
            }

            if (string.IsNullOrWhiteSpace(
                    usuario.Contrasenia))
            {
                TempData["error"] =
                    "Debe ingresar una contraseña.";

                return RedirectToAction(
                    nameof(Index));
            }

            // Usuario de acceso único
            var usuarioAccesoExiste =
                await _context.Usuarios
                    .AnyAsync(u =>
                        u.UsuarioAcceso ==
                        usuario.UsuarioAcceso);

            if (usuarioAccesoExiste)
            {
                TempData["error"] =
                    "El usuario de acceso ya está registrado.";

                return RedirectToAction(
                    nameof(Index));
            }

            // Correo único
            var correoExiste =
                await _context.Usuarios
                    .AnyAsync(u =>
                        u.Correo == usuario.Correo);

            if (correoExiste)
            {
                TempData["error"] =
                    "El correo ya está registrado.";

                return RedirectToAction(
                    nameof(Index));
            }

            // Usuario nuevo siempre activo
            usuario.Estado = true;

            // Evitar que EF intente insertar una navegación Rol
            usuario.Rol = null;

            _context.Usuarios.Add(
                usuario);

            await _context.SaveChangesAsync();

            TempData["mensaje"] =
                "Usuario registrado correctamente.";

            return RedirectToAction(
                nameof(Index));
        }

        // =====================================
        // EDITAR USUARIO
        // =====================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            Usuario modelo)
        {
            var usuario =
                await _context.Usuarios
                    .FirstOrDefaultAsync(u =>
                        u.IdUsuario ==
                        modelo.IdUsuario);

            if (usuario == null)
            {
                TempData["error"] =
                    "No se encontró el usuario seleccionado.";

                return RedirectToAction(
                    nameof(Index));
            }

            if (modelo.IdRol != 1 &&
                modelo.IdRol != 2)
            {
                TempData["error"] =
                    "El rol seleccionado no es válido.";

                return RedirectToAction(
                    nameof(Index));
            }

            var existeRol =
                await _context.Roles
                    .AnyAsync(r =>
                        r.IdRol == modelo.IdRol &&
                        r.Estado);

            if (!existeRol)
            {
                TempData["error"] =
                    "El rol seleccionado no existe o está desactivado.";

                return RedirectToAction(
                    nameof(Index));
            }

            modelo.UsuarioAcceso =
                modelo.UsuarioAcceso?.Trim();

            modelo.Nombres =
                modelo.Nombres?.Trim();

            modelo.Apellidos =
                modelo.Apellidos?.Trim();

            modelo.Correo =
                modelo.Correo?.Trim();

            modelo.Celular =
                modelo.Celular?.Trim();

            modelo.Zona =
                modelo.Zona?.Trim();

            var usuarioAccesoExiste =
                await _context.Usuarios
                    .AnyAsync(u =>
                        u.UsuarioAcceso ==
                            modelo.UsuarioAcceso &&
                        u.IdUsuario !=
                            modelo.IdUsuario);

            if (usuarioAccesoExiste)
            {
                TempData["error"] =
                    "El usuario de acceso ya pertenece a otro usuario.";

                return RedirectToAction(
                    nameof(Index));
            }

            var correoExiste =
                await _context.Usuarios
                    .AnyAsync(u =>
                        u.Correo ==
                            modelo.Correo &&
                        u.IdUsuario !=
                            modelo.IdUsuario);

            if (correoExiste)
            {
                TempData["error"] =
                    "El correo ya pertenece a otro usuario.";

                return RedirectToAction(
                    nameof(Index));
            }

            usuario.UsuarioAcceso =
                modelo.UsuarioAcceso;

            usuario.IdRol =
                modelo.IdRol;

            usuario.Nombres =
                modelo.Nombres;

            usuario.Apellidos =
                modelo.Apellidos;

            usuario.Correo =
                modelo.Correo;

            usuario.Celular =
                modelo.Celular;

            usuario.Zona =
                modelo.Zona;

            // Solo cambia la contraseña
            // si se envió una nueva
            if (!string.IsNullOrWhiteSpace(
                    modelo.Contrasenia))
            {
                usuario.Contrasenia =
                    modelo.Contrasenia;
            }

            await _context.SaveChangesAsync();

            TempData["mensaje"] =
                "Usuario actualizado correctamente.";

            return RedirectToAction(
                nameof(Index));
        }

        // =====================================
        // DESACTIVAR
        // =====================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Desactivar(
            int id)
        {
            var usuario =
                await _context.Usuarios
                    .FirstOrDefaultAsync(u =>
                        u.IdUsuario == id);

            if (usuario == null)
            {
                TempData["error"] =
                    "No se encontró el usuario.";

                return RedirectToAction(
                    nameof(Index));
            }

            usuario.Estado = false;

            await _context.SaveChangesAsync();

            TempData["mensaje"] =
                "Usuario desactivado correctamente.";

            return RedirectToAction(
                nameof(Index));
        }

        // =====================================
        // ACTIVAR
        // =====================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Activar(
            int id)
        {
            var usuario =
                await _context.Usuarios
                    .FirstOrDefaultAsync(u =>
                        u.IdUsuario == id);

            if (usuario == null)
            {
                TempData["error"] =
                    "No se encontró el usuario.";

                return RedirectToAction(
                    nameof(Index));
            }

            usuario.Estado = true;

            await _context.SaveChangesAsync();

            TempData["mensaje"] =
                "Usuario activado correctamente.";

            return RedirectToAction(
                nameof(Index));
        }
    }
}