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
        public IActionResult Edit(Usuario usuario)
        {
            var usuarioBD = _context.Usuarios
                .FirstOrDefault(u => u.IdUsuario == usuario.IdUsuario);

            if (usuarioBD == null)
            {
                TempData["error"] = "Usuario no encontrado.";
                return RedirectToAction(nameof(Index));
            }

            usuarioBD.IdRol = usuario.IdRol;
            usuarioBD.Nombres = usuario.Nombres;
            usuarioBD.Apellidos = usuario.Apellidos;
            usuarioBD.Correo = usuario.Correo;
            usuarioBD.Celular = usuario.Celular;
            usuarioBD.Vehiculo = usuario.Vehiculo;
            usuarioBD.Contrasenia = usuario.Contrasenia;

            _context.Usuarios.Update(usuarioBD);
            _context.SaveChanges();

            TempData["mensaje"] = "Usuario actualizado correctamente.";
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