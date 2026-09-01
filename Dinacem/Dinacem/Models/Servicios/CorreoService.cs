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
            adjuntos.Add(
                (
                    rutaAdjunto,
                    nombreAdjunto
                    ?? Path.GetFileName(rutaAdjunto)
                ));
        }

        return await EnviarInternoAsync(
            destinatarios,
            asunto,
            contenidoHtml,
            adjuntos);
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
            adjuntos);
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
        // VALIDAR CONFIGURACIÓN
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
            destinatarios
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

            mensaje.From.Add(
                new MailboxAddress(
                    _configuracion.NombreRemitente,
                    _configuracion.Remitente.Trim()));


            // =================================================
            // DESTINATARIOS
            // =================================================

            foreach (var correo in correos)
            {
                try
                {
                    mensaje.To.Add(
                        MailboxAddress.Parse(
                            correo));
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Correo destinatario inválido: {Correo}",
                        correo);

                    return false;
                }
            }


            // =================================================
            // ASUNTO
            // =================================================

            mensaje.Subject =
                asunto ?? string.Empty;


            // =================================================
            // CUERPO
            // =================================================

            var bodyBuilder =
                new BodyBuilder();


            // =================================================
            // LOGO
            // =================================================

            var rutaLogo =
                Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot",
                    "images",
                    "logo-dinacen.png");


            if (System.IO.File.Exists(
                    rutaLogo))
            {
                var logo =
                    bodyBuilder.LinkedResources
                        .Add(rutaLogo);

                logo.ContentId =
                    "logoDinacen";

                logo.ContentDisposition =
                    new ContentDisposition(
                        ContentDisposition.Inline);

                logo.ContentType.MediaType =
                    "image";

                logo.ContentType.MediaSubtype =
                    "png";


                _logger.LogInformation(
                    "Logo DINACEN cargado correctamente.");
            }
            else
            {
                _logger.LogWarning(
                    "No se encontró el logo en {Ruta}",
                    rutaLogo);
            }


            // =================================================
            // HTML
            // =================================================

            bodyBuilder.HtmlBody =
                contenidoHtml ?? string.Empty;


            // =================================================
            // ADJUNTOS
            // =================================================

            var listaAdjuntos =
                adjuntos?
                    .Where(a =>
                        !string.IsNullOrWhiteSpace(
                            a.Ruta))
                    .ToList()
                ??
                new List<
                    (string Ruta, string Nombre)>();


            foreach (var adjunto in listaAdjuntos)
            {
                if (!System.IO.File.Exists(
                        adjunto.Ruta))
                {
                    _logger.LogWarning(
                        "Adjunto no encontrado: {Ruta}",
                        adjunto.Ruta);

                    continue;
                }


                var nombreArchivo =
                    !string.IsNullOrWhiteSpace(
                        adjunto.Nombre)
                        ? adjunto.Nombre
                        : Path.GetFileName(
                            adjunto.Ruta);


                var bytes =
                    await System.IO.File
                        .ReadAllBytesAsync(
                            adjunto.Ruta);


                bodyBuilder.Attachments.Add(
                    nombreArchivo,
                    bytes);


                _logger.LogInformation(
                    "Adjunto agregado: {Nombre}",
                    nombreArchivo);
            }


            mensaje.Body =
                bodyBuilder.ToMessageBody();


            // =================================================
            // IMPORTANTE:
            // ELIMINAR ESPACIOS DE CONTRASEÑA DE APLICACIÓN
            // =================================================

            var password =
                _configuracion.Contrasenia
                    .Replace(" ", "")
                    .Trim();


            var usuario =
                _configuracion.Usuario
                    .Trim();


            // =================================================
            // LOG ANTES DE CONECTAR
            // NO MOSTRAMOS LA CONTRASEÑA
            // =================================================

            _logger.LogInformation(
                "Conectando SMTP. Servidor: {Servidor}, Puerto: {Puerto}, Usuario: {Usuario}",
                _configuracion.Servidor,
                _configuracion.Puerto,
                usuario);


            // =================================================
            // SMTP
            // =================================================

            using var cliente =
                new SmtpClient();


            // =================================================
            // CONECTAR
            // Gmail puerto 587 = STARTTLS
            // =================================================

            await cliente.ConnectAsync(
                _configuracion.Servidor.Trim(),
                _configuracion.Puerto,
                SecureSocketOptions.StartTls);


            _logger.LogInformation(
                "Conexión SMTP establecida correctamente.");


            // =================================================
            // AUTENTICAR
            // =================================================

            await cliente.AuthenticateAsync(
                usuario,
                password);


            _logger.LogInformation(
                "Autenticación SMTP realizada correctamente.");


            // =================================================
            // ENVIAR
            // =================================================

            var respuesta =
                await cliente.SendAsync(
                    mensaje);


            _logger.LogInformation(
                "Respuesta SMTP: {Respuesta}",
                respuesta);


            // =================================================
            // DESCONECTAR
            // =================================================

            await cliente.DisconnectAsync(
                true);


            _logger.LogInformation(
                "Correo enviado correctamente a: {Destinatarios}",
                string.Join(
                    ", ",
                    correos));


            return true;
        }


        // =====================================================
        // ERROR DE AUTENTICACIÓN
        // =====================================================

        catch (MailKit.Security.AuthenticationException ex)
        {
            _logger.LogError(
                ex,
                "Gmail rechazó la autenticación SMTP. Revise la contraseña de aplicación.");

            return false;
        }


        // =====================================================
        // ERROR SMTP
        // =====================================================

        catch (SmtpCommandException ex)
        {
            _logger.LogError(
                ex,
                "Error SMTP. Código: {StatusCode}. Mensaje: {Mensaje}",
                ex.StatusCode,
                ex.Message);

            return false;
        }


        // =====================================================
        // ERROR DE PROTOCOLO
        // =====================================================

        catch (SmtpProtocolException ex)
        {
            _logger.LogError(
                ex,
                "Error de protocolo SMTP: {Mensaje}",
                ex.Message);

            return false;
        }


        // =====================================================
        // OTROS ERRORES
        // =====================================================

        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error inesperado enviando correo: {Mensaje}",
                ex.Message);

            return false;
        }
    }
}