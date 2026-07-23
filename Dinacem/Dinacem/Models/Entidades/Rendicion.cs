using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dinacem.Models
{
    public class Rendicion
    {
        [Key]
        public int IdRendicion { get; set; }

        [Required]
        public int IdSolicitud { get; set; }

        [ForeignKey("IdSolicitud")]
        public Solicitud? Solicitud { get; set; }

        [Required]
        public int IdUsuario { get; set; }

        [ForeignKey("IdUsuario")]
        public Usuario? Usuario { get; set; }

        [Required]
        public DateTime Fecha { get; set; } = DateTime.Now;

        [Required]
        public DateTime FechaInicio { get; set; }

        [Required]
        public DateTime FechaFin { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Total { get; set; } = 0;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Saldo { get; set; } = 0;

        [Required]
        public int IdEstadoRendicion { get; set; }

        [ForeignKey("IdEstadoRendicion")]
        public EstadoRendicion? EstadoRendicion { get; set; }
        public DevolucionSaldo? DevolucionSaldo { get; set; }
        [StringLength(500)]
        public string? ArchivoPdf { get; set; }

        public DateTime? FechaEnvioRevision { get; set; }
        [StringLength(1000)]
        public string? Observaciones { get; set; }


    }
}