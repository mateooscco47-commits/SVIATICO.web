using System.Text.Json;
using Dinacem.Models;

namespace Dinacem.Models.Servicios
{
    public class RucService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<RucService> _logger;

        public RucService(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<RucService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<ConsultaRucResultado> ConsultarAsync(string ruc)
        {
            ruc = ruc?.Trim() ?? string.Empty;

            if (ruc.Length != 11 || !ruc.All(char.IsDigit))
            {
                return new ConsultaRucResultado
                {
                    Exito = false,
                    Mensaje = "El RUC debe contener exactamente 11 dígitos."
                };
            }

            var apiUrl = _configuration["ConsultaRuc:Url"];
            var token = _configuration["ConsultaRuc:Token"];

            if (string.IsNullOrWhiteSpace(apiUrl))
            {
                return new ConsultaRucResultado
                {
                    Exito = false,
                    Mensaje = "No se configuró ConsultaRuc:Url."
                };
            }

            if (string.IsNullOrWhiteSpace(token))
            {
                return new ConsultaRucResultado
                {
                    Exito = false,
                    Mensaje = "No se configuró ConsultaRuc:Token."
                };
            }

            try
            {
                var url =
                    $"{apiUrl.TrimEnd('/')}/{Uri.EscapeDataString(ruc)}" +
                    $"?token={Uri.EscapeDataString(token)}";

                using var response = await _httpClient.GetAsync(url);

                var json = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "La consulta RUC falló. Código: {StatusCode}. Respuesta: {Respuesta}",
                        response.StatusCode,
                        json);

                    return new ConsultaRucResultado
                    {
                        Exito = false,
                        Mensaje = ObtenerMensajeError(json)
                                  ?? $"No se pudo consultar el RUC. Código HTTP: {(int)response.StatusCode}."
                    };
                }

                using var document = JsonDocument.Parse(json);
                var root = document.RootElement;

                var rucRespuesta = ObtenerTexto(
                    root,
                    "ruc",
                    "numeroDocumento",
                    "numero_documento");

                var razonSocial = ObtenerTexto(
                    root,
                    "razonSocial",
                    "razon_social",
                    "nombre",
                    "nombre_o_razon_social");

                var domicilioFiscal = ObtenerTexto(
                    root,
                    "direccion",
                    "domicilioFiscal",
                    "domicilio_fiscal");

                var estado = ObtenerTexto(
                    root,
                    "estado");

                var condicion = ObtenerTexto(
                    root,
                    "condicion");

                if (string.IsNullOrWhiteSpace(razonSocial))
                {
                    return new ConsultaRucResultado
                    {
                        Exito = false,
                        Mensaje = "La API respondió, pero no devolvió la razón social del RUC."
                    };
                }

                return new ConsultaRucResultado
                {
                    Exito = true,
                    Ruc = string.IsNullOrWhiteSpace(rucRespuesta)
                        ? ruc
                        : rucRespuesta,
                    RazonSocial = razonSocial,
                    DomicilioFiscal = domicilioFiscal,
                    Estado = estado,
                    Condicion = condicion,
                    Mensaje = "RUC consultado correctamente."
                };
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(
                    ex,
                    "Tiempo de espera agotado al consultar el RUC {Ruc}.",
                    ruc);

                return new ConsultaRucResultado
                {
                    Exito = false,
                    Mensaje = "La consulta del RUC tardó demasiado. Inténtelo nuevamente."
                };
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(
                    ex,
                    "Error de conexión al consultar el RUC {Ruc}.",
                    ruc);

                return new ConsultaRucResultado
                {
                    Exito = false,
                    Mensaje = "No fue posible conectar con el servicio de consulta RUC."
                };
            }
            catch (JsonException ex)
            {
                _logger.LogError(
                    ex,
                    "La API devolvió un JSON inválido para el RUC {Ruc}.",
                    ruc);

                return new ConsultaRucResultado
                {
                    Exito = false,
                    Mensaje = "La respuesta del servicio de consulta RUC no tiene un formato válido."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error inesperado al consultar el RUC {Ruc}.",
                    ruc);

                return new ConsultaRucResultado
                {
                    Exito = false,
                    Mensaje = "Ocurrió un error inesperado al consultar el RUC."
                };
            }
        }

        private static string? ObtenerTexto(
            JsonElement root,
            params string[] propiedades)
        {
            foreach (var propiedad in propiedades)
            {
                if (!root.TryGetProperty(propiedad, out var valor))
                {
                    continue;
                }

                if (valor.ValueKind == JsonValueKind.String)
                {
                    return valor.GetString();
                }

                if (valor.ValueKind == JsonValueKind.Number ||
                    valor.ValueKind == JsonValueKind.True ||
                    valor.ValueKind == JsonValueKind.False)
                {
                    return valor.ToString();
                }
            }

            return null;
        }

        private static string? ObtenerMensajeError(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            try
            {
                using var document = JsonDocument.Parse(json);
                var root = document.RootElement;

                return ObtenerTexto(
                    root,
                    "mensaje",
                    "message",
                    "error");
            }
            catch (JsonException)
            {
                return null;
            }
        }
    }
}