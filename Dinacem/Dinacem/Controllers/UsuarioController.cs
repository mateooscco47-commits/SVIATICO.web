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

        // =========================================================
        // LISTADO DE USUARIOS
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            // -----------------------------------------------------
            // OBTENER USUARIOS
            // -----------------------------------------------------

            var usuarios = await _context.Usuarios
                .Include(u => u.Rol)
                .Include(u => u.Zona)
                .OrderBy(u => u.Nombres)
                .ThenBy(u => u.Apellidos)
                .ToListAsync();

            // -----------------------------------------------------
            // OBTENER ZONAS ACTIVAS
            // -----------------------------------------------------

            var zonas = await _context.Zonas
                .Where(z => z.Estado)
                .OrderBy(z => z.CodigoZona)
                .ToListAsync();

            // -----------------------------------------------------
            // OBTENER ROLES ACTIVOS
            // -----------------------------------------------------

            var roles = await _context.Roles
                .Where(r => r.Estado)
                .OrderBy(r => r.IdRol)
                .ToListAsync();

            // -----------------------------------------------------
            // ENVIAR DATOS A LA VISTA
            // -----------------------------------------------------

            ViewBag.Zonas = zonas;
            ViewBag.Roles = roles;

            // -----------------------------------------------------
            // MOSTRAR EL SIGUIENTE USUARIO DE ACCESO
            // -----------------------------------------------------

            ViewBag.SiguienteUsuarioAcceso =
                await GenerarSiguienteUsuarioAccesoAsync();

            return View(usuarios);
        }


        // =========================================================
        // CREAR USUARIO
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Usuario usuario)
        {
            // -----------------------------------------------------
            // LIMPIAR DATOS
            // -----------------------------------------------------

            usuario.Nombres = usuario.Nombres?.Trim();
            usuario.Apellidos = usuario.Apellidos?.Trim();
            usuario.Correo = usuario.Correo?.Trim();
            usuario.Celular = usuario.Celular?.Trim();

            // -----------------------------------------------------
            // VALIDAR ZONA
            // -----------------------------------------------------

            if (!usuario.IdZona.HasValue)
            {
                TempData["UsuarioError"] =
                    "Debe seleccionar una zona.";

                return RedirectToAction(nameof(Index));
            }

            var zonaExiste = await _context.Zonas
                .AnyAsync(z =>
                    z.IdZona == usuario.IdZona.Value &&
                    z.Estado);

            if (!zonaExiste)
            {
                TempData["UsuarioError"] =
                    "La zona seleccionada no existe o está desactivada.";

                return RedirectToAction(nameof(Index));
            }

            // -----------------------------------------------------
            // VALIDAR ROL
            // -----------------------------------------------------

            if (usuario.IdRol <= 0)
            {
                TempData["UsuarioError"] =
                    "Debe seleccionar un rol.";

                return RedirectToAction(nameof(Index));
            }

            var rolExiste = await _context.Roles
                .AnyAsync(r =>
                    r.IdRol == usuario.IdRol &&
                    r.Estado);

            if (!rolExiste)
            {
                TempData["UsuarioError"] =
                    "El rol seleccionado no existe o está desactivado.";

                return RedirectToAction(nameof(Index));
            }

            // -----------------------------------------------------
            // USUARIO DE ACCESO AUTOMÁTICO
            // -----------------------------------------------------
            // Se generará más abajo con formato p001, p002, p003...

            // -----------------------------------------------------
            // VALIDAR NOMBRES
            // -----------------------------------------------------

            if (string.IsNullOrWhiteSpace(usuario.Nombres))
            {
                TempData["UsuarioError"] =
                    "Debe ingresar los nombres.";

                return RedirectToAction(nameof(Index));
            }

            // -----------------------------------------------------
            // VALIDAR APELLIDOS
            // -----------------------------------------------------

            if (string.IsNullOrWhiteSpace(usuario.Apellidos))
            {
                TempData["UsuarioError"] =
                    "Debe ingresar los apellidos.";

                return RedirectToAction(nameof(Index));
            }

            // -----------------------------------------------------
            // VALIDAR CORREO
            // -----------------------------------------------------

            if (string.IsNullOrWhiteSpace(usuario.Correo))
            {
                TempData["UsuarioError"] =
                    "Debe ingresar el correo.";

                return RedirectToAction(nameof(Index));
            }

            // -----------------------------------------------------
            // VALIDAR CONTRASEÑA
            // -----------------------------------------------------

            if (string.IsNullOrWhiteSpace(usuario.Contrasenia))
            {
                TempData["UsuarioError"] =
                    "Debe ingresar una contraseña.";

                return RedirectToAction(nameof(Index));
            }

            // -----------------------------------------------------
            // VALIDAR CELULAR
            // -----------------------------------------------------

            if (!string.IsNullOrWhiteSpace(usuario.Celular) &&
                (usuario.Celular.Length != 9 ||
                 !usuario.Celular.All(char.IsDigit)))
            {
                TempData["UsuarioError"] =
                    "El celular debe contener exactamente 9 dígitos.";

                return RedirectToAction(nameof(Index));
            }

            // -----------------------------------------------------
            // GENERAR USUARIO DE ACCESO AUTOMÁTICAMENTE
            // -----------------------------------------------------

            usuario.UsuarioAcceso =
                await GenerarSiguienteUsuarioAccesoAsync();

            // -----------------------------------------------------
            // VALIDAR CORREO ÚNICO
            // -----------------------------------------------------

            var correoExiste = await _context.Usuarios
                .AnyAsync(u =>
                    u.Correo.ToLower() ==
                    usuario.Correo.ToLower());

            if (correoExiste)
            {
                TempData["UsuarioError"] =
                    "El correo ya está registrado.";

                return RedirectToAction(nameof(Index));
            }

            // -----------------------------------------------------
            // CONFIGURAR USUARIO
            // -----------------------------------------------------

            usuario.Estado = true;

            // Las relaciones se establecen mediante las FK.
            usuario.Rol = null;
            usuario.Zona = null;

            _context.Usuarios.Add(usuario);

            await _context.SaveChangesAsync();

            TempData["UsuarioMensaje"] =
                $"Usuario registrado correctamente. Usuario de acceso: {usuario.UsuarioAcceso}";

            return RedirectToAction(nameof(Index));
        }


        // =========================================================
        // EDITAR USUARIO - GET
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            // -----------------------------------------------------
            // OBTENER USUARIO + ZONA
            // -----------------------------------------------------

            var usuario = await _context.Usuarios
                .Include(u => u.Zona)
                .FirstOrDefaultAsync(u =>
                    u.IdUsuario == id);

            if (usuario == null)
            {
                TempData["UsuarioError"] =
                    "No se encontró el usuario seleccionado.";

                return RedirectToAction(nameof(Index));
            }

            // -----------------------------------------------------
            // OBTENER ROLES ACTIVOS
            // -----------------------------------------------------

            var roles = await _context.Roles
                .Where(r => r.Estado)
                .OrderBy(r => r.IdRol)
                .ToListAsync();

            // -----------------------------------------------------
            // ENVIAR ROLES A EDIT.CSHTML
            // -----------------------------------------------------

            ViewBag.Roles = roles;

            return View(usuario);
        }


        // =========================================================
        // EDITAR USUARIO - POST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Usuario modelo)
        {
            // -----------------------------------------------------
            // BUSCAR USUARIO
            // -----------------------------------------------------

            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u =>
                    u.IdUsuario == modelo.IdUsuario);

            if (usuario == null)
            {
                TempData["UsuarioError"] =
                    "No se encontró el usuario seleccionado.";

                return RedirectToAction(nameof(Index));
            }

            // -----------------------------------------------------
            // VALIDAR ROL
            // -----------------------------------------------------

            if (modelo.IdRol <= 0)
            {
                TempData["UsuarioError"] =
                    "Debe seleccionar un rol.";

                return RedirectToAction(nameof(Index));
            }

            var rolExiste = await _context.Roles
                .AnyAsync(r =>
                    r.IdRol == modelo.IdRol &&
                    r.Estado);

            if (!rolExiste)
            {
                TempData["UsuarioError"] =
                    "El rol seleccionado no existe o está desactivado.";

                return RedirectToAction(nameof(Index));
            }

            // -----------------------------------------------------
            // LIMPIAR DATOS
            // -----------------------------------------------------

            modelo.UsuarioAcceso = modelo.UsuarioAcceso?.Trim();
            modelo.Nombres = modelo.Nombres?.Trim();
            modelo.Apellidos = modelo.Apellidos?.Trim();
            modelo.Correo = modelo.Correo?.Trim();
            modelo.Celular = modelo.Celular?.Trim();

            // -----------------------------------------------------
            // VALIDAR USUARIO DE ACCESO
            // -----------------------------------------------------

            if (string.IsNullOrWhiteSpace(modelo.UsuarioAcceso))
            {
                TempData["UsuarioError"] =
                    "Debe ingresar el usuario de acceso.";

                return RedirectToAction(nameof(Index));
            }

            // -----------------------------------------------------
            // VALIDAR NOMBRES
            // -----------------------------------------------------

            if (string.IsNullOrWhiteSpace(modelo.Nombres))
            {
                TempData["UsuarioError"] =
                    "Debe ingresar los nombres.";

                return RedirectToAction(nameof(Index));
            }

            // -----------------------------------------------------
            // VALIDAR APELLIDOS
            // -----------------------------------------------------

            if (string.IsNullOrWhiteSpace(modelo.Apellidos))
            {
                TempData["UsuarioError"] =
                    "Debe ingresar los apellidos.";

                return RedirectToAction(nameof(Index));
            }

            // -----------------------------------------------------
            // VALIDAR CORREO
            // -----------------------------------------------------

            if (string.IsNullOrWhiteSpace(modelo.Correo))
            {
                TempData["UsuarioError"] =
                    "Debe ingresar el correo.";

                return RedirectToAction(nameof(Index));
            }

            // -----------------------------------------------------
            // VALIDAR CELULAR
            // -----------------------------------------------------

            if (!string.IsNullOrWhiteSpace(modelo.Celular) &&
                (modelo.Celular.Length != 9 ||
                 !modelo.Celular.All(char.IsDigit)))
            {
                TempData["UsuarioError"] =
                    "El celular debe contener exactamente 9 dígitos.";

                return RedirectToAction(nameof(Index));
            }

            // -----------------------------------------------------
            // VALIDAR USUARIO ÚNICO
            // -----------------------------------------------------

            var usuarioAccesoExiste = await _context.Usuarios
                .AnyAsync(u =>
                    u.UsuarioAcceso.ToLower() ==
                    modelo.UsuarioAcceso.ToLower() &&
                    u.IdUsuario != modelo.IdUsuario);

            if (usuarioAccesoExiste)
            {
                TempData["UsuarioError"] =
                    "El usuario de acceso ya pertenece a otro usuario.";

                return RedirectToAction(nameof(Index));
            }

            // -----------------------------------------------------
            // VALIDAR CORREO ÚNICO
            // -----------------------------------------------------

            var correoExiste = await _context.Usuarios
                .AnyAsync(u =>
                    u.Correo.ToLower() ==
                    modelo.Correo.ToLower() &&
                    u.IdUsuario != modelo.IdUsuario);

            if (correoExiste)
            {
                TempData["UsuarioError"] =
                    "El correo ya pertenece a otro usuario.";

                return RedirectToAction(nameof(Index));
            }

            // -----------------------------------------------------
            // ACTUALIZAR DATOS
            // -----------------------------------------------------

            usuario.UsuarioAcceso = modelo.UsuarioAcceso;
            usuario.IdRol = modelo.IdRol;
            usuario.Nombres = modelo.Nombres;
            usuario.Apellidos = modelo.Apellidos;
            usuario.Correo = modelo.Correo;
            usuario.Celular = modelo.Celular;

            // -----------------------------------------------------
            // LA ZONA NO SE MODIFICA
            // -----------------------------------------------------
            // Se conserva la zona que ya tiene el usuario.

            // -----------------------------------------------------
            // CONTRASEÑA
            // -----------------------------------------------------

            if (!string.IsNullOrWhiteSpace(modelo.Contrasenia))
            {
                usuario.Contrasenia = modelo.Contrasenia;
            }

            // -----------------------------------------------------
            // GUARDAR CAMBIOS
            // -----------------------------------------------------

            await _context.SaveChangesAsync();

            TempData["UsuarioMensaje"] =
                "Usuario actualizado correctamente.";

            return RedirectToAction(nameof(Index));
        }


        // =========================================================
        // GENERAR SIGUIENTE USUARIO DE ACCESO
        // =========================================================

        private async Task<string> GenerarSiguienteUsuarioAccesoAsync()
        {
            var usuariosAcceso = await _context.Usuarios
                .AsNoTracking()
                .Where(u =>
                    u.UsuarioAcceso != null &&
                    u.UsuarioAcceso.StartsWith("p"))
                .Select(u => u.UsuarioAcceso)
                .ToListAsync();

            int ultimoNumero = 0;

            foreach (var usuarioAcceso in usuariosAcceso)
            {
                if (string.IsNullOrWhiteSpace(usuarioAcceso))
                {
                    continue;
                }

                string valor = usuarioAcceso.Trim();

                if (valor.Length < 2)
                {
                    continue;
                }

                string parteNumerica = valor.Substring(1);

                if (int.TryParse(parteNumerica, out int numero) &&
                    numero > ultimoNumero)
                {
                    ultimoNumero = numero;
                }
            }

            int siguienteNumero = ultimoNumero + 1;
            string siguienteUsuario = $"p{siguienteNumero:D3}";

            while (await _context.Usuarios
                .AnyAsync(u =>
                    u.UsuarioAcceso == siguienteUsuario))
            {
                siguienteNumero++;
                siguienteUsuario = $"p{siguienteNumero:D3}";
            }

            return siguienteUsuario;
        }


        // =========================================================
        // DESACTIVAR USUARIO
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Desactivar(int id)
        {
            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u =>
                    u.IdUsuario == id);

            if (usuario == null)
            {
                TempData["UsuarioError"] =
                    "No se encontró el usuario.";

                return RedirectToAction(nameof(Index));
            }

            usuario.Estado = false;

            await _context.SaveChangesAsync();

            TempData["UsuarioMensaje"] =
                "Usuario desactivado correctamente.";

            return RedirectToAction(nameof(Index));
        }


        // =========================================================
        // ACTIVAR USUARIO
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Activar(int id)
        {
            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u =>
                    u.IdUsuario == id);

            if (usuario == null)
            {
                TempData["UsuarioError"] =
                    "No se encontró el usuario.";

                return RedirectToAction(nameof(Index));
            }

            usuario.Estado = true;

            await _context.SaveChangesAsync();

            TempData["UsuarioMensaje"] =
                "Usuario activado correctamente.";

            return RedirectToAction(nameof(Index));
        }
    }
}