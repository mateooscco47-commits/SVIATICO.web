using Dinacem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Dinacem.Controllers
{
    public class DevolucionSaldoController : Controller
    {
        private readonly AplicacionDbContexto _context;

        public DevolucionSaldoController(AplicacionDbContexto context)
        {
            _context = context;
        }

        // =========================================
        // REGISTRAR DEVOLUCIÓN
        // =========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Registrar(
            DevolucionSaldo devolucion,
            IFormFile? voucher)
        {
            var rendicion = await _context.Rendiciones
                .Include(r => r.Solicitud)
                .FirstOrDefaultAsync(r =>
                    r.IdRendicion == devolucion.IdRendicion);

            if (rendicion == null)
            {
                TempData["error"] = "No se encontró la rendición.";

                return RedirectToAction(
                    "Index",
                    "Rendicion");
            }

            if (rendicion.IdEstadoRendicion != 1)
            {
                TempData["error"] =
                    "La rendición ya fue enviada y no permite registrar una devolución.";

                return RedirectToAction(
                    "Index",
                    "Gasto",
                    new { idRendicion = devolucion.IdRendicion });
            }

            var devolucionExistente =
                await _context.DevolucionesSaldo
                    .AnyAsync(d =>
                        d.IdRendicion == devolucion.IdRendicion);

            if (devolucionExistente)
            {
                TempData["error"] =
                    "Esta rendición ya tiene una devolución registrada.";

                return RedirectToAction(
                    "Index",
                    "Gasto",
                    new { idRendicion = devolucion.IdRendicion });
            }

            if (rendicion.Saldo <= 0)
            {
                TempData["error"] =
                    "La rendición no tiene saldo pendiente por devolver.";

                return RedirectToAction(
                    "Index",
                    "Gasto",
                    new { idRendicion = devolucion.IdRendicion });
            }

            if (devolucion.Monto != rendicion.Saldo)
            {
                ModelState.AddModelError(
                    nameof(devolucion.Monto),
                    $"El monto de devolución debe ser exactamente S/ {rendicion.Saldo:N2}.");
            }

            if (voucher == null || voucher.Length == 0)
            {
                ModelState.AddModelError(
                    "Voucher",
                    "Debe adjuntar el voucher de devolución.");
            }

            if (!ModelState.IsValid)
            {
                var errores = ModelState
                    .Where(x =>
                        x.Value != null &&
                        x.Value.Errors.Count > 0)
                    .Select(x =>
                        $"{x.Key}: {string.Join(", ",
                            x.Value!.Errors.Select(e =>
                                string.IsNullOrWhiteSpace(e.ErrorMessage)
                                    ? "Valor no válido."
                                    : e.ErrorMessage))}");

                TempData["error"] = string.Join("<br>", errores);

                return RedirectToAction(
                    "Index",
                    "Gasto",
                    new { idRendicion = devolucion.IdRendicion });
            }

            string[] extensionesPermitidas =
            {
                ".pdf",
                ".jpg",
                ".jpeg",
                ".png"
            };

            var extension = Path
                .GetExtension(voucher!.FileName)
                .ToLowerInvariant();

            if (!extensionesPermitidas.Contains(extension))
            {
                TempData["error"] =
                    "El voucher debe ser PDF, JPG, JPEG o PNG.";

                return RedirectToAction(
                    "Index",
                    "Gasto",
                    new { idRendicion = devolucion.IdRendicion });
            }

            const long tamanioMaximo = 5 * 1024 * 1024;

            if (voucher.Length > tamanioMaximo)
            {
                TempData["error"] =
                    "El voucher no debe superar los 5 MB.";

                return RedirectToAction(
                    "Index",
                    "Gasto",
                    new { idRendicion = devolucion.IdRendicion });
            }

            var nombreArchivo =
                $"{Guid.NewGuid()}{extension}";

            var carpeta = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot",
                "vouchers-devolucion");

            Directory.CreateDirectory(carpeta);

            var rutaCompleta = Path.Combine(
                carpeta,
                nombreArchivo);

            await using var stream =
                new FileStream(
                    rutaCompleta,
                    FileMode.Create);

            await voucher.CopyToAsync(stream);

            devolucion.Voucher =
                $"/vouchers-devolucion/{nombreArchivo}";

            devolucion.Fecha =
                devolucion.Fecha == default
                    ? DateTime.Now
                    : devolucion.Fecha;

            _context.DevolucionesSaldo.Add(devolucion);
            await _context.SaveChangesAsync();

            TempData["mensaje"] =
                "Devolución registrada correctamente.";

            return RedirectToAction(
                "Index",
                "Gasto",
                new { idRendicion = devolucion.IdRendicion });
        }

        // =========================================
        // ELIMINAR DEVOLUCIÓN
        // =========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Eliminar(
            int id,
            int idRendicion)
        {
            var rendicion = await _context.Rendiciones
                .FirstOrDefaultAsync(r =>
                    r.IdRendicion == idRendicion);

            if (rendicion == null)
            {
                TempData["error"] =
                    "No se encontró la rendición.";

                return RedirectToAction(
                    "Index",
                    "Rendicion");
            }

            if (rendicion.IdEstadoRendicion != 1)
            {
                TempData["error"] =
                    "No se puede eliminar una devolución de una rendición enviada.";

                return RedirectToAction(
                    "Index",
                    "Gasto",
                    new { idRendicion });
            }

            var devolucion =
                await _context.DevolucionesSaldo
                    .FirstOrDefaultAsync(d =>
                        d.IdDevolucionSaldo == id &&
                        d.IdRendicion == idRendicion);

            if (devolucion == null)
            {
                TempData["error"] =
                    "No se encontró la devolución.";

                return RedirectToAction(
                    "Index",
                    "Gasto",
                    new { idRendicion });
            }

            if (!string.IsNullOrWhiteSpace(devolucion.Voucher))
            {
                var rutaRelativa = devolucion.Voucher
                    .TrimStart('/')
                    .Replace(
                        '/',
                        Path.DirectorySeparatorChar);

                var rutaCompleta = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot",
                    rutaRelativa);

                if (System.IO.File.Exists(rutaCompleta))
                {
                    System.IO.File.Delete(rutaCompleta);
                }
            }

            _context.DevolucionesSaldo.Remove(devolucion);
            await _context.SaveChangesAsync();

            TempData["mensaje"] =
                "Devolución eliminada correctamente.";

            return RedirectToAction(
                "Index",
                "Gasto",
                new { idRendicion });
        }
    }
}