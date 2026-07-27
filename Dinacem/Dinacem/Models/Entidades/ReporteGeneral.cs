namespace Dinacem.Models
{
    public class ReporteGeneral
    {

        // ==========================================
        // CANTIDAD DE SOLICITUDES
        // ==========================================

        public int TotalSolicitudes { get; set; }


        public int SolicitudesPendientes { get; set; }


        public int SolicitudesAprobadas { get; set; }


        public int SolicitudesRechazadas { get; set; }


        public int SolicitudesFinalizadas { get; set; }



        // ==========================================
        // RENDICIONES
        // ==========================================

        public int RendicionesPendientes { get; set; }



        // ==========================================
        // MONTOS
        // ==========================================

        public decimal TotalSolicitado { get; set; }


        public decimal TotalRendido { get; set; }


        public decimal TotalGastado { get; set; }


        public decimal SaldoPendiente { get; set; }



        // ==========================================
        // FILTROS
        // ==========================================

        public DateTime? FechaInicio { get; set; }


        public DateTime? FechaFin { get; set; }



        // ==========================================
        // DATOS PARA GRÁFICOS
        // ==========================================

        public List<string> Meses { get; set; }
            = new List<string>();


        public List<decimal> MontosMensuales { get; set; }
            = new List<decimal>();



        // ==========================================
        // DETALLE TABLA
        // ==========================================

        public List<DetalleReporte> Detalles { get; set; }
            = new List<DetalleReporte>();

    }
}

