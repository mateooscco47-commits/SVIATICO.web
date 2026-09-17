using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Xml;
using TelegramCatalogo.Data;

namespace TelegramCatalogo.Controllers
{
    public class SitemapController : Controller
    {
        private readonly AplicacionDbContexto _context;

        public SitemapController(AplicacionDbContexto context)
        {
            _context = context;
        }

        [HttpGet("/sitemap.xml")]
        [ResponseCache(Duration = 3600)]
        public async Task<IActionResult> Index()
        {
            var categorias = await _context.Categorias
                .AsNoTracking()
                .Where(c =>
                    c.Estado &&
                    c.Slug != null &&
                    c.Slug != "")
                .OrderBy(c => c.Nombre)
                .ToListAsync();

            var canales = await _context.Canales
                .AsNoTracking()
                .Where(c =>
                    c.Estado == "Activo" &&
                    c.Slug != null &&
                    c.Slug != "")
                .OrderByDescending(c => c.FechaRegistro)
                .ToListAsync();

            var configuracion = new XmlWriterSettings
            {
                Encoding = new UTF8Encoding(false),
                Indent = true
            };

            using var memoria = new MemoryStream();

            using (var xml = XmlWriter.Create(memoria, configuracion))
            {
                xml.WriteStartDocument();

                xml.WriteStartElement(
                    "urlset",
                    "http://www.sitemaps.org/schemas/sitemap/0.9"
                );

                // INICIO
                EscribirUrl(
                    xml,
                    UrlAbsoluta("/")
                );

                // CATÁLOGO GENERAL
                EscribirUrl(
                    xml,
                    UrlAbsoluta("/canales")
                );

                // CATEGORÍAS
                foreach (var categoria in categorias)
                {
                    EscribirUrl(
                        xml,
                        UrlAbsoluta(
                            $"/canales/{categoria.Slug}"
                        )
                    );
                }

                // CANALES
                foreach (var canal in canales)
                {
                    EscribirUrl(
                        xml,
                        UrlAbsoluta(
                            $"/canal/{canal.Slug}"
                        ),
                        canal.FechaActualizacion ?? canal.FechaRegistro
                    );
                }

                xml.WriteEndElement();
                xml.WriteEndDocument();
            }

            return File(
                memoria.ToArray(),
                "application/xml; charset=utf-8"
            );
        }

        private string UrlAbsoluta(string ruta)
        {
            return $"{Request.Scheme}://{Request.Host}{ruta}";
        }

        private static void EscribirUrl(
            XmlWriter xml,
            string url,
            DateTime? ultimaModificacion = null)
        {
            xml.WriteStartElement("url");

            xml.WriteElementString(
                "loc",
                url
            );

            if (ultimaModificacion.HasValue)
            {
                xml.WriteElementString(
                    "lastmod",
                    ultimaModificacion.Value
                        .ToString("yyyy-MM-dd")
                );
            }

            xml.WriteEndElement();
        }
    }
}