using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Dinacem.Models;

namespace Dinacem.Controllers
{
    public class UsuarioController : Controller
    {
        private readonly AplicacionDbContexto _context;

        public UsuarioController(AplicacionDbContexto context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var usuarios = _context.Usuarios
                .Include(u => u.Rol)
                .ToList();

            return View(usuarios);
        }

        [HttpPost]
        public IActionResult Create(Usuario usuario)
        {
            if (!ModelState.IsValid)
            {
                TempData["error"] = "Complete correctamente los datos del usuario.";
                return RedirectToAction(nameof(Index));
            }

            _context.Usuarios.Add(usuario);
            _context.SaveChanges();

            TempData["mensaje"] = "Usuario registrado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Usuario modelo)
        {
            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u =>
                    u.IdUsuario == modelo.IdUsuario);

            if (usuario == null)
            {
                TempData["error"] =
                    "No se encontró el usuario seleccionado.";

                return RedirectToAction(nameof(Index));
            }

            usuario.UsuarioAcceso = modelo.UsuarioAcceso;
            usuario.IdRol = modelo.IdRol;
            usuario.Nombres = modelo.Nombres;
            usuario.Apellidos = modelo.Apellidos;
            usuario.Correo = modelo.Correo;
            usuario.Celular = modelo.Celular;
            usuario.Vehiculo = modelo.Vehiculo;
            usuario.Contrasenia = modelo.Contrasenia;

            await _context.SaveChangesAsync();

            TempData["mensaje"] =
                "Usuario actualizado correctamente.";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public IActionResult Desactivar(int id)
        {
            var usuario = _context.Usuarios.Find(id);

            if (usuario == null)
            {
                return NotFound();
            }

            usuario.Estado = false;

            _context.SaveChanges();

            TempData["mensaje"] = "Usuario desactivado correctamente.";

            return RedirectToAction(nameof(Index));
        }
        [HttpPost]
        public IActionResult Activar(int id)
        {
            var usuario = _context.Usuarios.Find(id);

            if (usuario == null)
            {
                return NotFound();
            }

            usuario.Estado = true;

            _context.SaveChanges();

            TempData["mensaje"] = "Usuario activado correctamente.";

            return RedirectToAction(nameof(Index));
        }
    }
}