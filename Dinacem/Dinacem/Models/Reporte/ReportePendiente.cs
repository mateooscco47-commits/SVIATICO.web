namespace Dinacem.Models.Reporte
{
    public class ReportePendiente
    {
        public int IdSolicitud { get; set; }


        public string Codigo { get; set; } = string.Empty;


        public string Solicitante { get; set; } = string.Empty;


        public string Destino { get; set; } = string.Empty;


        public decimal Monto { get; set; }


        public DateTime Fecha { get; set; }

    }
}
