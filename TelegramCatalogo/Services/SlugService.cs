using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using TelegramCatalogo.Data;

namespace TelegramCatalogo.Services
{
    public class SlugService
    {
        private readonly AplicacionDbContexto _context;

        public SlugService(AplicacionDbContexto context)
        {
            _context = context;
        }

        public async Task<string> GenerarSlugCanalAsync(
            string texto,
            int? ignorarId = null)
        {
            var slugBase = Generar(texto);

            if (string.IsNullOrWhiteSpace(slugBase))
            {
                slugBase = "canal";
            }

            var slug = slugBase;
            var numero = 2;

            while (await _context.Canales.AnyAsync(c =>
                c.Slug == slug &&
                (!ignorarId.HasValue ||
                 c.IdCanal != ignorarId.Value)))
            {
                slug = $"{slugBase}-{numero}";
                numero++;
            }

            return slug;
        }

        public async Task<string> GenerarSlugCategoriaAsync(
            string texto,
            int? ignorarId = null)
        {
            var slugBase = Generar(texto);

            if (string.IsNullOrWhiteSpace(slugBase))
            {
                slugBase = "categoria";
            }

            var slug = slugBase;
            var numero = 2;

            while (await _context.Categorias.AnyAsync(c =>
                c.Slug == slug &&
                (!ignorarId.HasValue ||
                 c.IdCategoria != ignorarId.Value)))
            {
                slug = $"{slugBase}-{numero}";
                numero++;
            }

            return slug;
        }

        private static string Generar(string texto)
        {
            texto = texto
                .Trim()
                .ToLowerInvariant();

            var normalizado =
                texto.Normalize(
                    NormalizationForm.FormD);

            var resultado = new StringBuilder();

            foreach (var caracter in normalizado)
            {
                var categoria =
                    CharUnicodeInfo.GetUnicodeCategory(
                        caracter);

                if (categoria !=
                    UnicodeCategory.NonSpacingMark)
                {
                    resultado.Append(caracter);
                }
            }

            var slug = resultado
                .ToString()
                .Normalize(
                    NormalizationForm.FormC);

            slug = Regex.Replace(
                slug,
                @"[^a-z0-9]+",
                "-"
            );

            return slug.Trim('-');
        }
    }
}