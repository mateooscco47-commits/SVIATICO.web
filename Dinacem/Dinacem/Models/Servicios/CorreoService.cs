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


    // =========================================================
    // CONSTRUCTOR
    // =========================================================

    public CorreoService(
        IOptions<CorreoConfiguracion> configuracion,
        ILogger<CorreoService> logger)
    {
        _configuracion = configuracion.Value;
        _logger = logger;
    }


    // =========================================================
    // ENVIAR - UN SOLO ADJUNTO
    // =========================================================

    public async Task<bool> EnviarAsync(
        IEnumerable<string> destinatarios,
        string asunto,
        string contenidoHtml,
        string? rutaAdjunto = null,
        string? nombreAdjunto = null)
    {
        var adjuntos =
            new List<(string Ruta, string Nombre)>();


        if (!string.IsNullOrWhiteSpace(rutaAdjunto))
        {
            var nombreArchivo =
                !string.IsNullOrWhiteSpace(nombreAdjunto)
                    ? nombreAdjunto.Trim()
                    : Path.GetFileName(rutaAdjunto);


            adjuntos.Add(
                (
                    rutaAdjunto,
                    nombreArchivo
                )
            );
        }


        return await EnviarInternoAsync(
            destinatarios,
            asunto,
            contenidoHtml,
            adjuntos
        );
    }


    // =========================================================
    // ENVIAR - MÚLTIPLES ADJUNTOS
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
            adjuntos
        );
    }


    // =========================================================
    // MÉTODO INTERNO
    // =========================================================

    private async Task<bool> EnviarInternoAsync(
        IEnumerable<string> destinatarios,
        string asunto,
        string contenidoHtml,
        IEnumerable<(string Ruta, string Nombre)> adjuntos)
    {
        // =====================================================
        // VALIDAR CONFIGURACIÓN SMTP
        // =====================================================

        if (string.IsNullOrWhiteSpace(
                _configuracion.Servidor))
        {
            _logger.LogError(
                "No se configuró el servidor SMTP.");

            return false;
        }

        if (_configuracion.Puerto <= 0)
        {
            _logger.LogError(
                "El puerto SMTP no es válido.");

            return false;
        }

        if (string.IsNullOrWhiteSpace(
                _configuracion.Usuario))
        {
            _logger.LogError(
                "No se configuró el usuario SMTP.");

            return false;
        }

        if (string.IsNullOrWhiteSpace(
                _configuracion.Contrasenia))
        {
            _logger.LogError(
                "No se configuró la contraseña SMTP.");

            return false;
        }

        if (string.IsNullOrWhiteSpace(
                _configuracion.Remitente))
        {
            _logger.LogError(
                "No se configuró el remitente.");

            return false;
        }

        // =====================================================
        // LIMPIAR DESTINATARIOS
        // =====================================================

        var correos =
            (destinatarios ?? Enumerable.Empty<string>())
                .Where(c =>
                    !string.IsNullOrWhiteSpace(c))
                .Select(c =>
                    c.Trim())
                .Distinct(
                    StringComparer.OrdinalIgnoreCase)
                .ToList();

        if (correos.Count == 0)
        {
            _logger.LogWarning(
                "No existen destinatarios para el correo.");

            return false;
        }

        try
        {
            // =================================================
            // CREAR MENSAJE
            // =================================================

            var mensaje =
                new MimeMessage();

            // =================================================
            // REMITENTE
            // =================================================

            var remitente =
                _configuracion.Remitente.Trim();

            var nombreRemitente =
                string.IsNullOrWhiteSpace(
                    _configuracion.NombreRemitente)
                    ? "DINACEN"
                    : _configuracion.NombreRemitente.Trim();

            mensaje.From.Add(
                new MailboxAddress(
                    nombreRemitente,
                    remitente
                )
            );

            // =================================================
            // DESTINATARIOS
            // =================================================

            foreach (var correo in correos)
            {
                try
                {
                    mensaje.To.Add(
                        MailboxAddress.Parse(
                            correo
                        )
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Correo destinatario inválido: {Correo}",
                        correo
                    );

                    return false;
                }
            }

            // =================================================
            // ASUNTO
            // =================================================

            mensaje.Subject =
                asunto?.Trim() ?? string.Empty;

            // =================================================
            // BODY BUILDER
            // =================================================

            var bodyBuilder =
                new BodyBuilder();

            var contenidoOriginal =
                contenidoHtml ?? string.Empty;

            // =================================================
            // HTML FINAL (SIN DUPLICAR CABECERA DE LOGO)
            // =================================================

            string htmlFinal = $@"
<!DOCTYPE html>
<html lang=""es"">

<head>
    <meta charset=""UTF-8"">
    <meta
        name=""viewport""
        content=""width=device-width, initial-scale=1.0""
    >
    <title>DINACEN</title>
</head>

<body style=""
    margin:0;
    padding:0;
    background-color:#ffffff;
    font-family:Arial, Helvetica, sans-serif;
    color:#333333;
"">

    <div style=""
        width:100%;
        max-width:700px;
        margin:0 auto;
        padding:20px;
        box-sizing:border-box;
    "">

        {contenidoOriginal}

    </div>

</body>
</html>";

            bodyBuilder.HtmlBody = htmlFinal;

            // =================================================
            // PROCESAR ADJUNTOS
            // =================================================

            var listaAdjuntos =
                (adjuntos ??
                    Enumerable.Empty<
                        (string Ruta, string Nombre)>())
                .Where(a =>
                    !string.IsNullOrWhiteSpace(a.Ruta))
                .ToList();

            foreach (var adjunto in listaAdjuntos)
            {
                if (!System.IO.File.Exists(
                        adjunto.Ruta))
                {
                    _logger.LogWarning(
                        "Adjunto no encontrado: {Ruta}",
                        adjunto.Ruta
                    );

                    continue;
                }

                var nombreArchivo =
                    !string.IsNullOrWhiteSpace(
                        adjunto.Nombre)
                        ? adjunto.Nombre.Trim()
                        : Path.GetFileName(
                            adjunto.Ruta);

                var bytes =
                    await System.IO.File.ReadAllBytesAsync(
                        adjunto.Ruta
                    );

                bodyBuilder.Attachments.Add(
                    nombreArchivo,
                    bytes
                );

                _logger.LogInformation(
                    "Adjunto agregado: {Nombre}",
                    nombreArchivo
                );
            }

            mensaje.Body =
                bodyBuilder.ToMessageBody();

            var usuario =
                _configuracion.Usuario.Trim();

            var password =
                _configuracion.Contrasenia
                    .Trim()
                    .Replace(" ", "");

            _logger.LogInformation(
                "Conectando SMTP. Servidor: {Servidor}, Puerto: {Puerto}, Usuario: {Usuario}",
                _configuracion.Servidor,
                _configuracion.Puerto,
                usuario
            );

            using var cliente =
                new SmtpClient();

            await cliente.ConnectAsync(
                _configuracion.Servidor.Trim(),
                _configuracion.Puerto,
                SecureSocketOptions.StartTls
            );

            _logger.LogInformation(
                "Conexión SMTP establecida correctamente."
            );

            await cliente.AuthenticateAsync(
                usuario,
                password
            );

            _logger.LogInformation(
                "Autenticación SMTP realizada correctamente."
            );

            var respuesta =
                await cliente.SendAsync(
                    mensaje
                );

            _logger.LogInformation(
                "Respuesta SMTP: {Respuesta}",
                respuesta
            );

            await cliente.DisconnectAsync(
                true
            );

            _logger.LogInformation(
                "Correo enviado correctamente a: {Destinatarios}",
                string.Join(
                    ", ",
                    correos)
            );

            return true;
        }
        catch (MailKit.Security.AuthenticationException ex)
        {
            _logger.LogError(
                ex,
                "El servidor SMTP rechazó la autenticación. Revise el usuario y la contraseña de aplicación."
            );

            return false;
        }
        catch (SmtpCommandException ex)
        {
            _logger.LogError(
                ex,
                "Error SMTP. Código: {StatusCode}. Mensaje: {Mensaje}",
                ex.StatusCode,
                ex.Message
            );

            return false;
        }
        catch (SmtpProtocolException ex)
        {
            _logger.LogError(
                ex,
                "Error de protocolo SMTP: {Mensaje}",
                ex.Message
            );

            return false;
        }
        catch (MailKit.ServiceNotConnectedException ex)
        {
            _logger.LogError(
                ex,
                "No fue posible establecer conexión con el servidor SMTP."
            );

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error inesperado enviando correo: {Mensaje}",
                ex.Message
            );

            return false;
        }
    }
}