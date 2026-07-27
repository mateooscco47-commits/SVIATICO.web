namespace Dinacem.Models.Reporte
{
    public class ReporteUsuario
    {
        public string Usuario { get; set; } = string.Empty;


        public int Solicitudes { get; set; }


        public decimal TotalViaticos { get; set; }


        public int RendicionesPendientes { get; set; }
    }
}
