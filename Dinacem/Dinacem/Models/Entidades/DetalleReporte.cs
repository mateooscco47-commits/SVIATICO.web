namespace Dinacem.Models
{
    public class DetalleReporte
    {
        public int IdSolicitud { get; set; }

        public string Usuario { get; set; } = string.Empty;

        public string Motivo { get; set; } = string.Empty;

        public string Destino { get; set; } = string.Empty;

        public DateTime FechaInicio { get; set; }

        public DateTime FechaFin { get; set; }


        public decimal MontoSolicitado { get; set; }


        public decimal MontoRendido { get; set; }


        public decimal Saldo { get; set; }


        public string Estado { get; set; } = string.Empty;
    }
}