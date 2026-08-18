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

    public async Task<bool> EnviarAsync(
        IEnumerable<string> destinatarios,
        string asunto,
        string contenidoHtml,
        string? rutaAdjunto = null,
        string? nombreAdjunto = null)
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

                // Este mismo identificador se utiliza
                // en el HTML:
                //
                // <img src="cid:logoDinacen">

                logo.ContentId = "logoDinacen";

                // Indicar que la imagen es interna
                // y debe mostrarse dentro del correo.

                logo.ContentDisposition =
                    new ContentDisposition(
                        ContentDisposition.Inline);

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
            // ARCHIVO ADJUNTO
            // ==========================================

            if (!string.IsNullOrWhiteSpace(rutaAdjunto) &&
                System.IO.File.Exists(rutaAdjunto))
            {
                if (!string.IsNullOrWhiteSpace(nombreAdjunto))
                {
                    bodyBuilder.Attachments.Add(
                        nombreAdjunto,
                        await System.IO.File.ReadAllBytesAsync(
                            rutaAdjunto));
                }
                else
                {
                    bodyBuilder.Attachments.Add(
                        rutaAdjunto);
                }

                _logger.LogInformation(
                    "Archivo adjunto agregado al correo: {RutaAdjunto}",
                    rutaAdjunto);
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