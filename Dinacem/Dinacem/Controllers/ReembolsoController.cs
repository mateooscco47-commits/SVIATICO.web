using Dinacem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Dinacem.Controllers
{
    public class ReembolsoController : Controller
    {
        private readonly AplicacionDbContexto _context;
        private readonly IWebHostEnvironment _environment;

        public ReembolsoController(
            AplicacionDbContexto context,
            IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegistrarPago(
            int idReembolso,
            string banco,
            string numeroOperacion,
            DateTime fechaPago,
            string? observaciones,
            IFormFile? comprobante)
        {
            var reembolso = await _context.Reembolsos
                .FirstOrDefaultAsync(r =>
                    r.IdReembolso == idReembolso);

            if (reembolso == null)
            {
                TempData["error"] =
                    "No se encontró el reembolso.";

                return RedirectToAction(
                    "IndexAdmin",
                    "Rendicion");
            }

            if (reembolso.IdEstadoReembolso != 2)
            {
                TempData["error"] =
                    "El reembolso no está aprobado o ya fue procesado.";

                return RedirectToAction(
                    "DetalleAdmin",
                    "Rendicion",
                    new
                    {
                        id = reembolso.IdRendicion
                    });
            }

            banco = banco?.Trim() ?? string.Empty;

            numeroOperacion =
                numeroOperacion?.Trim() ??
                string.Empty;

            observaciones =
                observaciones?.Trim();

            if (string.IsNullOrWhiteSpace(banco))
            {
                TempData["error"] =
                    "Debe ingresar el banco.";

                return RedirectToAction(
                    "DetalleAdmin",
                    "Rendicion",
                    new
                    {
                        id = reembolso.IdRendicion
                    });
            }

            if (banco.Length > 100)
            {
                TempData["error"] =
                    "El nombre del banco no puede superar los 100 caracteres.";

                return RedirectToAction(
                    "DetalleAdmin",
                    "Rendicion",
                    new
                    {
                        id = reembolso.IdRendicion
                    });
            }

            if (string.IsNullOrWhiteSpace(
                    numeroOperacion))
            {
                TempData["error"] =
                    "Debe ingresar el número de operación.";

                return RedirectToAction(
                    "DetalleAdmin",
                    "Rendicion",
                    new
                    {
                        id = reembolso.IdRendicion
                    });
            }

            if (numeroOperacion.Length > 100)
            {
                TempData["error"] =
                    "El número de operación no puede superar los 100 caracteres.";

                return RedirectToAction(
                    "DetalleAdmin",
                    "Rendicion",
                    new
                    {
                        id = reembolso.IdRendicion
                    });
            }

            if (fechaPago == default)
            {
                TempData["error"] =
                    "Debe ingresar la fecha de pago.";

                return RedirectToAction(
                    "DetalleAdmin",
                    "Rendicion",
                    new
                    {
                        id = reembolso.IdRendicion
                    });
            }

            if (fechaPago.Date > DateTime.Today)
            {
                TempData["error"] =
                    "La fecha de pago no puede ser futura.";

                return RedirectToAction(
                    "DetalleAdmin",
                    "Rendicion",
                    new
                    {
                        id = reembolso.IdRendicion
                    });
            }

            if (observaciones?.Length > 1000)
            {
                TempData["error"] =
                    "Las observaciones no pueden superar los 1000 caracteres.";

                return RedirectToAction(
                    "DetalleAdmin",
                    "Rendicion",
                    new
                    {
                        id = reembolso.IdRendicion
                    });
            }

            if (comprobante == null ||
                comprobante.Length == 0)
            {
                TempData["error"] =
                    "Debe adjuntar el comprobante de pago.";

                return RedirectToAction(
                    "DetalleAdmin",
                    "Rendicion",
                    new
                    {
                        id = reembolso.IdRendicion
                    });
            }

            string[] extensionesPermitidas =
            {
                ".pdf",
                ".jpg",
                ".jpeg",
                ".png"
            };

            var extension = Path
                .GetExtension(comprobante.FileName)
                .ToLowerInvariant();

            if (!extensionesPermitidas.Contains(
                    extension))
            {
                TempData["error"] =
                    "El comprobante debe ser PDF, JPG, JPEG o PNG.";

                return RedirectToAction(
                    "DetalleAdmin",
                    "Rendicion",
                    new
                    {
                        id = reembolso.IdRendicion
                    });
            }

            const long tamanioMaximo =
                5 * 1024 * 1024;

            if (comprobante.Length >
                tamanioMaximo)
            {
                TempData["error"] =
                    "El comprobante no debe superar los 5 MB.";

                return RedirectToAction(
                    "DetalleAdmin",
                    "Rendicion",
                    new
                    {
                        id = reembolso.IdRendicion
                    });
            }

            var nombreArchivo =
                $"{Guid.NewGuid()}{extension}";

            var webRootPath =
                _environment.WebRootPath;

            if (string.IsNullOrWhiteSpace(
                    webRootPath))
            {
                webRootPath = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot");
            }

            var carpeta = Path.Combine(
                webRootPath,
                "reembolsos");

            Directory.CreateDirectory(carpeta);

            var rutaCompleta = Path.Combine(
                carpeta,
                nombreArchivo);

            try
            {
                await using var stream =
                    new FileStream(
                        rutaCompleta,
                        FileMode.Create);

                await comprobante.CopyToAsync(
                    stream);
            }
            catch
            {
                TempData["error"] =
                    "No se pudo guardar el comprobante de pago.";

                return RedirectToAction(
                    "DetalleAdmin",
                    "Rendicion",
                    new
                    {
                        id = reembolso.IdRendicion
                    });
            }

            reembolso.Banco =
                banco;

            reembolso.NumeroOperacion =
                numeroOperacion;

            reembolso.FechaPago =
                fechaPago.Date;

            reembolso.Observaciones =
                observaciones;

            reembolso.ComprobantePago =
                $"/reembolsos/{nombreArchivo}";

            // 3 = Reembolso pagado
            reembolso.IdEstadoReembolso = 3;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch
            {
                if (System.IO.File.Exists(
                        rutaCompleta))
                {
                    System.IO.File.Delete(
                        rutaCompleta);
                }

                TempData["error"] =
                    "No se pudo registrar el pago del reembolso.";

                return RedirectToAction(
                    "DetalleAdmin",
                    "Rendicion",
                    new
                    {
                        id = reembolso.IdRendicion
                    });
            }

            TempData["mensaje"] =
                "El reembolso fue registrado como pagado correctamente.";

            return RedirectToAction(
                "DetalleAdmin",
                "Rendicion",
                new
                {
                    id = reembolso.IdRendicion
                });
        }
    }
}