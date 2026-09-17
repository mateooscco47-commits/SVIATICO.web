using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TelegramCatalogo.Data;
using TelegramCatalogo.Models;

namespace TelegramCatalogo.Controllers
{
    public class SolicitudesController : Controller
    {
        private readonly AplicacionDbContexto _context;

        public SolicitudesController(
            AplicacionDbContexto context)
        {
            _context = context;
        }

        // ==========================================
        // FORMULARIO PÚBLICO
        // ==========================================

        [HttpGet("/agregar-canal")]
        public async Task<IActionResult> Crear()
        {
            await CargarCategorias();

            return View(new SolicitudCanal());
        }


        // ==========================================
        // ENVIAR SOLICITUD
        // ==========================================

        [HttpPost("/agregar-canal")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(
            SolicitudCanal solicitud)
        {
            // Nunca confiamos en el estado enviado
            // desde el navegador.
            solicitud.Estado = "Pendiente";
            solicitud.Observaciones = null;
            solicitud.FechaSolicitud = DateTime.Now;
            if (!EsEnlaceTelegramValido(
    solicitud.EnlaceTelegram))
            {
                ModelState.AddModelError(
                    nameof(solicitud.EnlaceTelegram),
                    "Ingresa un enlace válido de Telegram, por ejemplo https://t.me/nombrecanal."
                );
            }

            if (!ModelState.IsValid)
            {
                await CargarCategorias(
                    solicitud.IdCategoria
                );

                return View(solicitud);
            }

            solicitud.NombreCanal =
                solicitud.NombreCanal.Trim();

            solicitud.EnlaceTelegram =
                solicitud.EnlaceTelegram.Trim();

            solicitud.Descripcion =
                solicitud.Descripcion?.Trim();

            solicitud.CorreoContacto =
                solicitud.CorreoContacto?.Trim();

            // Evitar que el mismo enlace se envíe
            // repetidamente mientras está pendiente.
            var existePendiente =
                await _context.SolicitudesCanal
                    .AnyAsync(s =>
                        s.EnlaceTelegram ==
                            solicitud.EnlaceTelegram &&
                        s.Estado == "Pendiente");

            if (existePendiente)
            {
                ModelState.AddModelError(
                    nameof(solicitud.EnlaceTelegram),
                    "Ya existe una solicitud pendiente para este canal."
                );

                await CargarCategorias(
                    solicitud.IdCategoria
                );

                return View(solicitud);
            }

            // Tampoco aceptamos un canal
            // que ya está publicado.
            var yaPublicado =
                await _context.Canales
                    .AnyAsync(c =>
                        c.EnlaceTelegram ==
                            solicitud.EnlaceTelegram &&
                        c.Estado == "Activo");

            if (yaPublicado)
            {
                ModelState.AddModelError(
                    nameof(solicitud.EnlaceTelegram),
                    "Este canal ya está publicado en el catálogo."
                );

                await CargarCategorias(
                    solicitud.IdCategoria
                );

                return View(solicitud);
            }

            _context.SolicitudesCanal.Add(solicitud);

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Enviada));
        }


        // ==========================================
        // CONFIRMACIÓN
        // ==========================================

        [HttpGet("/solicitud-enviada")]
        public IActionResult Enviada()
        {
            return View();
        }


        private async Task CargarCategorias(
            int? seleccionada = null)
        {
            var categorias =
                await _context.Categorias
                    .Where(c => c.Estado)
                    .OrderBy(c => c.Nombre)
                    .ToListAsync();

            ViewBag.Categorias =
                new SelectList(
                    categorias,
                    "IdCategoria",
                    "Nombre",
                    seleccionada
                );
        }

        private static bool EsEnlaceTelegramValido(
        string? enlace)
        {
            if (string.IsNullOrWhiteSpace(enlace))
            {
                return false;
            }

            if (!Uri.TryCreate(
                enlace.Trim(),
                UriKind.Absolute,
                out var uri))
            {
                return false;
            }

            if (uri.Scheme != Uri.UriSchemeHttps)
            {
                return false;
            }

            var host =
                uri.Host.ToLowerInvariant();

            if (host != "t.me" &&
                host != "www.t.me" &&
                host != "telegram.me" &&
                host != "www.telegram.me")
            {
                return false;
            }

            return !string.IsNullOrWhiteSpace(
                uri.AbsolutePath.Trim('/')
            );
        }
    }
}