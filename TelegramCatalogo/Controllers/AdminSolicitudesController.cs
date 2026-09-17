using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using TelegramCatalogo.Data;
using TelegramCatalogo.Models;
using TelegramCatalogo.Services;

namespace TelegramCatalogo.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class AdminSolicitudesController : Controller
    {
        private readonly AplicacionDbContexto _context;
        private readonly SlugService _slugService;

        public AdminSolicitudesController(
    AplicacionDbContexto context,
    SlugService slugService)
        {
            _context = context;
            _slugService = slugService;
        }


        // ==========================================
        // LISTADO
        // ==========================================

        [HttpGet("/admin/solicitudes")]
        public async Task<IActionResult> Index(
            string estado = "Pendiente")
        {
            var consulta =
                _context.SolicitudesCanal
                    .Include(s => s.Categoria)
                    .AsQueryable();

            if (!string.IsNullOrWhiteSpace(estado) &&
                estado != "Todas")
            {
                consulta = consulta.Where(
                    s => s.Estado == estado
                );
            }

            ViewBag.Estado = estado;

            var solicitudes =
                await consulta
                    .OrderByDescending(
                        s => s.FechaSolicitud)
                    .ToListAsync();

            return View(solicitudes);
        }


        // ==========================================
        // DETALLE
        // ==========================================

        [HttpGet("/admin/solicitudes/{id:int}")]
        public async Task<IActionResult> Detalle(int id)
        {
            var solicitud =
                await _context.SolicitudesCanal
                    .Include(s => s.Categoria)
                    .FirstOrDefaultAsync(
                        s => s.IdSolicitud == id);

            if (solicitud == null)
            {
                return NotFound();
            }

            return View(solicitud);
        }


        // ==========================================
        // APROBAR
        // ==========================================

        [HttpPost("/admin/solicitudes/aprobar/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Aprobar(int id)
        {
            await using var transaccion =
                await _context.Database
                    .BeginTransactionAsync();

            try
            {
                var solicitud =
                    await _context.SolicitudesCanal
                        .FirstOrDefaultAsync(
                            s => s.IdSolicitud == id);

                if (solicitud == null)
                {
                    return NotFound();
                }

                // Solo solicitudes pendientes
                // pueden ser aprobadas.
                if (solicitud.Estado != "Pendiente")
                {
                    TempData["Error"] =
                        "Esta solicitud ya fue procesada.";

                    return RedirectToAction(
                        nameof(Detalle),
                        new { id }
                    );
                }


                // ==================================
                // COMPROBAR DUPLICADO
                // ==================================

                var enlace =
                    solicitud.EnlaceTelegram.Trim();

                var yaExiste =
                    await _context.Canales
                        .AnyAsync(c =>
                            c.EnlaceTelegram == enlace);

                if (yaExiste)
                {
                    TempData["Error"] =
                        "Este canal ya existe en el catálogo.";

                    return RedirectToAction(
                        nameof(Detalle),
                        new { id }
                    );
                }


                // ==================================
                // GENERAR SLUG
                // ==================================

                var slug =
    await _slugService
        .GenerarSlugCanalAsync(
            solicitud.NombreCanal
        );


                // ==================================
                // EXTRAER USERNAME
                // ==================================

                var username =
                    ExtraerUsernameTelegram(enlace);


                // ==================================
                // CREAR CANAL
                // ==================================

                var canal = new Canal
                {
                    IdCategoria =
                        solicitud.IdCategoria!.Value,

                    Nombre =
                        solicitud.NombreCanal.Trim(),

                    Slug =
                        slug,

                    UsernameTelegram =
                        username,

                    Descripcion =
                        solicitud.Descripcion?.Trim(),

                    EnlaceTelegram =
                        enlace,

                    CantidadMiembros =
                        0,

                    Idioma =
                        "Español",

                    Pais =
                        "Perú",

                    EsDestacado =
                        false,

                    Estado =
                        "Activo",

                    FechaRegistro =
                        DateTime.Now,

                    FechaActualizacion =
                        DateTime.Now,

                    Visitas =
                        0,

                    Clicks =
                        0
                };


                _context.Canales.Add(canal);


                // ==================================
                // ACTUALIZAR SOLICITUD
                // ==================================

                solicitud.Estado =
                    "Aprobada";

                solicitud.Observaciones =
                    "Solicitud aprobada y canal publicado.";


                // ==================================
                // GUARDAR
                // ==================================

                await _context.SaveChangesAsync();

                await transaccion.CommitAsync();


                TempData["Exito"] =
                    "La solicitud fue aprobada y el canal fue publicado.";

                return RedirectToAction(
                    nameof(Detalle),
                    new { id }
                );
            }
            catch
            {
                await transaccion.RollbackAsync();

                TempData["Error"] =
                    "No se pudo aprobar la solicitud.";

                return RedirectToAction(
                    nameof(Detalle),
                    new { id }
                );
            }
        }


        // ==========================================
        // RECHAZAR
        // ==========================================

        [HttpPost("/admin/solicitudes/rechazar/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Rechazar(
            int id,
            string? observaciones)
        {
            var solicitud =
                await _context.SolicitudesCanal
                    .FirstOrDefaultAsync(
                        s => s.IdSolicitud == id);

            if (solicitud == null)
            {
                return NotFound();
            }

            if (solicitud.Estado != "Pendiente")
            {
                TempData["Error"] =
                    "Esta solicitud ya fue procesada.";

                return RedirectToAction(
                    nameof(Detalle),
                    new { id }
                );
            }

            if (string.IsNullOrWhiteSpace(observaciones))
            {
                TempData["Error"] =
                    "Debes indicar el motivo del rechazo.";

                return RedirectToAction(
                    nameof(Detalle),
                    new { id }
                );
            }

            observaciones = observaciones.Trim();

            if (observaciones.Length > 500)
            {
                TempData["Error"] =
                    "La observación no puede superar 500 caracteres.";

                return RedirectToAction(
                    nameof(Detalle),
                    new { id }
                );
            }

            solicitud.Estado =
                "Rechazada";

            solicitud.Observaciones =
                observaciones;

            await _context.SaveChangesAsync();

            TempData["Exito"] =
                "La solicitud fue rechazada.";

            return RedirectToAction(
                nameof(Detalle),
                new { id }
            );
        }


        // ==========================================
        // EXTRAER USERNAME
        // ==========================================

        private static string? ExtraerUsernameTelegram(
            string enlace)
        {
            if (string.IsNullOrWhiteSpace(enlace))
            {
                return null;
            }

            if (!Uri.TryCreate(
                enlace,
                UriKind.Absolute,
                out var uri))
            {
                return null;
            }

            if (uri.Host != "t.me" &&
                uri.Host != "www.t.me" &&
                uri.Host != "telegram.me" &&
                uri.Host != "www.telegram.me")
            {
                return null;
            }

            var partes =
                uri.AbsolutePath
                    .Trim('/')
                    .Split(
                        '/',
                        StringSplitOptions
                            .RemoveEmptyEntries
                    );

            if (partes.Length == 0)
            {
                return null;
            }

            var username =
                partes[0].TrimStart('@');

            // Los enlaces privados del tipo
            // t.me/+xxxx no tienen username público.
            if (username.StartsWith("+"))
            {
                return null;
            }

            return username;
        }

    }
}