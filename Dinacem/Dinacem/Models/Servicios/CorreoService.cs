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
            string contenidoHtml)
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

            var mensaje = new MimeMessage();

            mensaje.From.Add(new MailboxAddress(
                _configuracion.NombreRemitente,
                _configuracion.Remitente));

            foreach (var correo in correos)
            {
                mensaje.To.Add(MailboxAddress.Parse(correo));
            }

            mensaje.Subject = asunto;

            mensaje.Body = new BodyBuilder
            {
                HtmlBody = contenidoHtml
            }.ToMessageBody();

            try
            {
                using var cliente = new SmtpClient();

                await cliente.ConnectAsync(
                    _configuracion.Servidor,
                    _configuracion.Puerto,
                    SecureSocketOptions.StartTls);

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
                    "Error al enviar el correo de notificación.");

                return false;
            }
        }
    }
}