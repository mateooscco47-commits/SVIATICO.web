namespace Dinacem.Models.Reporte
{
    public class ReporteRendicion
    {
        public string Usuario { get; set; } = string.Empty;

        public string Solicitud { get; set; } = string.Empty;

        public decimal MontoEntregado { get; set; }

        public decimal Gastado { get; set; }

        public decimal Devuelto { get; set; }

        public decimal Diferencia { get; set; }

        public string Estado { get; set; } = string.Empty;
    }
}
    

