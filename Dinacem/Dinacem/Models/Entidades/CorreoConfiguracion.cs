namespace Dinacem.Models
{
    public class CorreoConfiguracion
    {
        public string Servidor { get; set; } = string.Empty;

        public int Puerto { get; set; }

        public string Usuario { get; set; } = string.Empty;

        public string Contrasenia { get; set; } = string.Empty;

        public string Remitente { get; set; } = string.Empty;

        public string NombreRemitente { get; set; } = string.Empty;
    }
}