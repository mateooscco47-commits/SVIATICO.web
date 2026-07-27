namespace Dinacem.Models.Reporte
{
    public class ReporteReembolso
    {
        public string Usuario { get; set; } = string.Empty;


        public string Motivo { get; set; } = string.Empty;


        public decimal Monto { get; set; }


        public string Estado { get; set; } = string.Empty;


        public DateTime Fecha { get; set; }


    }
}
