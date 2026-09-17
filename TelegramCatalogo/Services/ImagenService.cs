namespace TelegramCatalogo.Services
{
    public class ImagenService
    {
        private readonly IWebHostEnvironment _environment;

        private readonly string[] _extensionesPermitidas =
        {
            ".jpg",
            ".jpeg",
            ".png",
            ".webp"
        };

        private const long TamanoMaximo =
            5 * 1024 * 1024;

        public ImagenService(
            IWebHostEnvironment environment)
        {
            _environment = environment;
        }


        public async Task<string> GuardarCanalAsync(
            IFormFile archivo)
        {
            if (archivo == null ||
                archivo.Length == 0)
            {
                throw new InvalidOperationException(
                    "Selecciona una imagen.");
            }

            if (archivo.Length > TamanoMaximo)
            {
                throw new InvalidOperationException(
                    "La imagen no puede superar los 5 MB.");
            }

            var extension =
                Path.GetExtension(
                    archivo.FileName)
                .ToLowerInvariant();

            if (!_extensionesPermitidas.Contains(extension))
            {
                throw new InvalidOperationException(
                    "Solo se permiten imágenes JPG, PNG o WebP.");
            }


            // Validar tipo MIME
            var tiposPermitidos = new[]
            {
                "image/jpeg",
                "image/png",
                "image/webp"
            };

            if (!tiposPermitidos.Contains(
                archivo.ContentType.ToLowerInvariant()))
            {
                throw new InvalidOperationException(
                    "El archivo seleccionado no es una imagen válida.");
            }


            var carpeta =
                Path.Combine(
                    _environment.WebRootPath,
                    "uploads",
                    "canales"
                );

            Directory.CreateDirectory(carpeta);


            var nombreArchivo =
                $"{Guid.NewGuid():N}{extension}";


            var rutaFisica =
                Path.Combine(
                    carpeta,
                    nombreArchivo
                );


            await using var stream =
                new FileStream(
                    rutaFisica,
                    FileMode.CreateNew);

            await archivo.CopyToAsync(stream);


            return $"/uploads/canales/{nombreArchivo}";
        }


        public void Eliminar(string? ruta)
        {
            if (string.IsNullOrWhiteSpace(ruta))
            {
                return;
            }


            // Seguridad:
            // solamente eliminamos imágenes de canales.
            if (!ruta.StartsWith(
                "/uploads/canales/",
                StringComparison.OrdinalIgnoreCase))
            {
                return;
            }


            var nombreArchivo =
                Path.GetFileName(ruta);


            if (string.IsNullOrWhiteSpace(
                nombreArchivo))
            {
                return;
            }


            var carpeta =
                Path.Combine(
                    _environment.WebRootPath,
                    "uploads",
                    "canales"
                );


            var rutaFisica =
                Path.Combine(
                    carpeta,
                    nombreArchivo
                );


            if (File.Exists(rutaFisica))
            {
                File.Delete(rutaFisica);
            }
        }
    }
}