using Dinacem.Models;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

public class CorreoService
{
    private readonly CorreoConfiguracion _configuracion;
    private readonly ILogger<CorreoService> _logger;

    public CorreoService(
        IOptions<CorreoConfiguracion> configuracion,
        ILogger<CorreoService> logger)
    {
        _configuracion = configuracion.Value;
        _logger = logger;
    }

    // =========================================================
    // MÉTODO ACTUAL - UN SOLO ADJUNTO
    // =========================================================

    public async Task<bool> EnviarAsync(
        IEnumerable<string> destinatarios,
        string asunto,
        string contenidoHtml,
        string? rutaAdjunto = null,
        string? nombreAdjunto = null)
    {
        return await EnviarInternoAsync(
            destinatarios,
            asunto,
            contenidoHtml,
            rutaAdjunto != null
                ? new List<(string Ruta, string Nombre)>
                {
                    (
                        rutaAdjunto,
                        nombreAdjunto ?? Path.GetFileName(rutaAdjunto)
                    )
                }
                : new List<(string Ruta, string Nombre)>());
    }


    // =========================================================
    // NUEVO MÉTODO - MÚLTIPLES ADJUNTOS
    // =========================================================

    public async Task<bool> EnviarAsync(
        IEnumerable<string> destinatarios,
        string asunto,
        string contenidoHtml,
        IEnumerable<(string Ruta, string Nombre)> adjuntos)
    {
        return await EnviarInternoAsync(
            destinatarios,
            asunto,
            contenidoHtml,
            adjuntos);
    }


    // =========================================================
    // MÉTODO INTERNO PARA CONSTRUIR Y ENVIAR EL CORREO
    // =========================================================

    private async Task<bool> EnviarInternoAsync(
        IEnumerable<string> destinatarios,
        string asunto,
        string contenidoHtml,
        IEnumerable<(string Ruta, string Nombre)> adjuntos)
    {
        // ==========================================
        // VALIDAR DESTINATARIOS
        // ==========================================

        var correos = destinatarios
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (correos.Count == 0)
        {
            _logger.LogWarning(
                "No se encontraron destinatarios para enviar el correo.");

            return false;
        }

        try
        {
            // ==========================================
            // CREAR MENSAJE
            // ==========================================

            var mensaje = new MimeMessage();


            // ==========================================
            // REMITENTE
            // ==========================================

            mensaje.From.Add(
                new MailboxAddress(
                    _configuracion.NombreRemitente,
                    _configuracion.Remitente));


            // ==========================================
            // DESTINATARIOS
            // ==========================================

            foreach (var correo in correos)
            {
                mensaje.To.Add(
                    MailboxAddress.Parse(correo));
            }


            // ==========================================
            // ASUNTO
            // ==========================================

            mensaje.Subject = asunto;


            // ==========================================
            // CONSTRUCTOR DEL CUERPO
            // ==========================================

            var bodyBuilder = new BodyBuilder();


            // ==========================================
            // LOGO DINACEN
            // ==========================================

            string rutaLogo = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot",
                "images",
                "logo-dinacen.png"
            );


            if (System.IO.File.Exists(rutaLogo))
            {
                var logo =
                    bodyBuilder.LinkedResources.Add(rutaLogo);

                // ==========================================
                // IDENTIFICADOR DEL LOGO
                // ==========================================

                logo.ContentId = "logoDinacen";


                // ==========================================
                // IMAGEN INLINE
                // ==========================================

                logo.ContentDisposition =
                    new ContentDisposition(
                        ContentDisposition.Inline);


                // ==========================================
                // ASEGURAR TIPO MIME
                // ==========================================

                logo.ContentType.MediaType = "image";
                logo.ContentType.MediaSubtype = "png";


                _logger.LogInformation(
                    "Logo DINACEN agregado correctamente al correo.");
            }
            else
            {
                _logger.LogWarning(
                    "No se encontró el logo de DINACEN en: {RutaLogo}",
                    rutaLogo);
            }


            // ==========================================
            // CONTENIDO HTML
            // ==========================================

            bodyBuilder.HtmlBody = contenidoHtml;


            // ==========================================
            // ARCHIVOS ADJUNTOS
            // ==========================================

            var listaAdjuntos =
                adjuntos?
                    .Where(a =>
                        !string.IsNullOrWhiteSpace(a.Ruta))
                    .ToList()
                ?? new List<(string Ruta, string Nombre)>();


            foreach (var adjunto in listaAdjuntos)
            {
                // ==========================================
                // VALIDAR QUE EXISTA EL ARCHIVO
                // ==========================================

                if (!System.IO.File.Exists(adjunto.Ruta))
                {
                    _logger.LogWarning(
                        "No se encontró el archivo adjunto: {Ruta}",
                        adjunto.Ruta);

                    continue;
                }


                // ==========================================
                // NOMBRE DEL ARCHIVO
                // ==========================================

                string nombreArchivo =
                    !string.IsNullOrWhiteSpace(adjunto.Nombre)
                        ? adjunto.Nombre
                        : Path.GetFileName(adjunto.Ruta);


                // ==========================================
                // AGREGAR PDF
                // ==========================================

                bodyBuilder.Attachments.Add(
                    nombreArchivo,
                    await System.IO.File.ReadAllBytesAsync(
                        adjunto.Ruta));


                _logger.LogInformation(
                    "Archivo adjunto agregado al correo: {NombreArchivo}",
                    nombreArchivo);
            }


            // ==========================================
            // CONSTRUIR CUERPO FINAL
            // ==========================================

            mensaje.Body =
                bodyBuilder.ToMessageBody();


            // ==========================================
            // CONEXIÓN SMTP
            // ==========================================

            using var cliente =
                new MailKit.Net.Smtp.SmtpClient();


            await cliente.ConnectAsync(
                _configuracion.Servidor,
                _configuracion.Puerto,
                SecureSocketOptions.StartTls);


            // ==========================================
            // AUTENTICACIÓN
            // ==========================================

            await cliente.AuthenticateAsync(
                _configuracion.Usuario,
                _configuracion.Contrasenia);


            // ==========================================
            // ENVIAR
            // ==========================================

            await cliente.SendAsync(mensaje);


            // ==========================================
            // DESCONECTAR
            // ==========================================

            await cliente.DisconnectAsync(true);


            // ==========================================
            // LOG
            // ==========================================

            _logger.LogInformation(
                "Correo enviado correctamente a: {Destinatarios}",
                string.Join(", ", correos));


            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error al enviar correo.");

            return false;
        }
    }
}