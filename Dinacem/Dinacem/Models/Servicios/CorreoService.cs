using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Dinacem.Models.Servicios
{
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
                var mensaje = new MimeMessage();

                mensaje.From.Add(
                    new MailboxAddress(
                        _configuracion.NombreRemitente,
                        _configuracion.Remitente));

                foreach (var correo in correos)
                {
                    mensaje.To.Add(
                        MailboxAddress.Parse(correo));
                }

                mensaje.Subject = asunto;

                var bodyBuilder = new BodyBuilder
                {
                    HtmlBody = contenidoHtml
                };

                if (!string.IsNullOrWhiteSpace(rutaAdjunto) &&
    System.IO.File.Exists(rutaAdjunto))
                {
                    bodyBuilder.Attachments.Add(rutaAdjunto);
                }

                mensaje.Body = bodyBuilder.ToMessageBody();

                using var cliente = new MailKit.Net.Smtp.SmtpClient();

                await cliente.ConnectAsync(
                    _configuracion.Servidor,
                    _configuracion.Puerto,
                    MailKit.Security.SecureSocketOptions.StartTls);

                await cliente.AuthenticateAsync(
                    _configuracion.Usuario,
                    _configuracion.Contrasenia);

                await cliente.SendAsync(mensaje);

                await cliente.DisconnectAsync(true);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al enviar correo con archivo adjunto.");

                return false;
            }
        }
    }
}