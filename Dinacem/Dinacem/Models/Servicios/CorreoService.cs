using Dinacem.Models;
using System.Net;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

public class CorreoService
{
    private readonly CorreoConfiguracion _configuracion;
    private readonly ILogger<CorreoService> _logger;
    private readonly IWebHostEnvironment _environment;


    // =========================================================
    // CONSTRUCTOR
    // =========================================================

    public CorreoService(
        IOptions<CorreoConfiguracion> configuracion,
        ILogger<CorreoService> logger,
        IWebHostEnvironment environment)
    {
        _configuracion = configuracion.Value;
        _logger = logger;
        _environment = environment;
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

            bodyBuilder.HtmlBody =
                contenidoHtml ?? string.Empty;


            // =================================================
            // LOGO DINACEN EMBEBIDO
            // =================================================
            //
            // El HTML debe utilizar:
            //
            // <img src="cid:logoDinacen">
            //
            // =================================================

            var rutaLogo =
                Path.Combine(
                    _environment.WebRootPath,
                    "images",
                    "logo-dinacen.png"
                );


            if (System.IO.File.Exists(rutaLogo))
            {
                try
                {
                    var logo =
                        bodyBuilder.LinkedResources.Add(
                            rutaLogo
                        );

                    // =========================================
                    // CONTENT-ID
                    // =========================================

                    logo.ContentId =
                        "logoDinacen";


                    // =========================================
                    // TIPO MIME
                    // =========================================

                    logo.ContentType.MediaType =
                        "image";

                    logo.ContentType.MediaSubtype =
                        "png";


                    _logger.LogInformation(
                        "Logo DINACEN agregado correctamente. Ruta: {RutaLogo}",
                        rutaLogo
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Error al agregar el logo DINACEN al correo."
                    );
                }
            }
            else
            {
                _logger.LogWarning(
                    "No se encontró el logo DINACEN. Ruta: {RutaLogo}",
                    rutaLogo
                );
            }


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
                // =============================================
                // VALIDAR EXISTENCIA
                // =============================================

                if (!System.IO.File.Exists(
                        adjunto.Ruta))
                {
                    _logger.LogWarning(
                        "Adjunto no encontrado: {Ruta}",
                        adjunto.Ruta
                    );

                    continue;
                }


                // =============================================
                // NOMBRE DEL ARCHIVO
                // =============================================

                var nombreArchivo =
                    !string.IsNullOrWhiteSpace(
                        adjunto.Nombre)
                        ? adjunto.Nombre.Trim()
                        : Path.GetFileName(
                            adjunto.Ruta);


                // =============================================
                // LEER ARCHIVO
                // =============================================

                byte[] bytes;

                try
                {
                    bytes =
                        await System.IO.File.ReadAllBytesAsync(
                            adjunto.Ruta
                        );
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "No se pudo leer el archivo adjunto: {Ruta}",
                        adjunto.Ruta
                    );

                    continue;
                }


                // =============================================
                // AGREGAR ADJUNTO
                // =============================================

                bodyBuilder.Attachments.Add(
                    nombreArchivo,
                    bytes
                );


                _logger.LogInformation(
                    "Adjunto agregado al correo: {Nombre}",
                    nombreArchivo
                );
            }


            // =================================================
            // CONSTRUIR BODY FINAL
            // =================================================

            mensaje.Body =
                bodyBuilder.ToMessageBody();


            // =================================================
            // DATOS SMTP
            // =================================================

            var servidor =
                _configuracion.Servidor.Trim();

            var usuario =
                _configuracion.Usuario.Trim();

            var password =
                _configuracion.Contrasenia
                    .Trim()
                    .Replace(" ", "");


            _logger.LogInformation(
                "Conectando SMTP. Servidor: {Servidor}, Puerto: {Puerto}, Usuario: {Usuario}",
                servidor,
                _configuracion.Puerto,
                usuario
            );


            // =================================================
            // CLIENTE SMTP
            // =================================================

            using var cliente =
                new SmtpClient();


            // =================================================
            // TIMEOUT
            // =================================================
            //
            // Evita que la aplicación quede esperando
            // indefinidamente al servidor SMTP.
            //
            // 30 segundos.
            // =================================================

            cliente.Timeout =
                30000;


            // =================================================
            // SEGURIDAD SMTP
            // =================================================
            //
            // Puerto 465:
            // SSL desde el inicio.
            //
            // Puerto 587:
            // STARTTLS.
            //
            // =================================================

            var seguridad =
                _configuracion.Puerto == 465
                    ? SecureSocketOptions.SslOnConnect
                    : SecureSocketOptions.StartTls;


            _logger.LogInformation(
                "Seguridad SMTP seleccionada: {Seguridad}",
                seguridad
            );


            // =================================================
            // CONEXIÓN SMTP
            // =================================================

            await cliente.ConnectAsync(
                servidor,
                _configuracion.Puerto,
                seguridad
            );


            _logger.LogInformation(
                "Conexión SMTP establecida correctamente."
            );


            // =================================================
            // AUTENTICACIÓN
            // =================================================

            await cliente.AuthenticateAsync(
                usuario,
                password
            );


            _logger.LogInformation(
                "Autenticación SMTP realizada correctamente."
            );


            // =================================================
            // ENVIAR CORREO
            // =================================================

            var respuesta =
                await cliente.SendAsync(
                    mensaje
                );


            _logger.LogInformation(
                "Respuesta SMTP: {Respuesta}",
                respuesta
            );


            // =================================================
            // DESCONECTAR
            // =================================================

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


        // =====================================================
        // ERROR DE AUTENTICACIÓN
        // =====================================================

        catch (MailKit.Security.AuthenticationException ex)
        {
            _logger.LogError(
                ex,
                "El servidor SMTP rechazó la autenticación. " +
                "Revise el usuario y la contraseña de aplicación."
            );

            return false;
        }


        // =====================================================
        // ERROR DE COMANDO SMTP
        // =====================================================

        catch (MailKit.Net.Smtp.SmtpCommandException ex)
        {
            _logger.LogError(
                ex,
                "Error SMTP. Código: {StatusCode}. Mensaje: {Mensaje}",
                ex.StatusCode,
                ex.Message
            );

            return false;
        }


        // =====================================================
        // ERROR DE PROTOCOLO SMTP
        // =====================================================

        catch (MailKit.Net.Smtp.SmtpProtocolException ex)
        {
            _logger.LogError(
                ex,
                "Error de protocolo SMTP: {Mensaje}",
                ex.Message
            );

            return false;
        }


        // =====================================================
        // ERROR DE CONEXIÓN
        // =====================================================

        catch (MailKit.ServiceNotConnectedException ex)
        {
            _logger.LogError(
                ex,
                "No fue posible establecer conexión con el servidor SMTP."
            );

            return false;
        }


        // =====================================================
        // TIMEOUT
        // =====================================================

        catch (TimeoutException ex)
        {
            _logger.LogError(
                ex,
                "Tiempo de espera agotado al comunicarse con el servidor SMTP."
            );

            return false;
        }


        // =====================================================
        // ERROR GENERAL
        // =====================================================

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