using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dinacem.Models
{
    public class Solicitud
    {
        [Key]
        public int IdSolicitud { get; set; }

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
        [StringLength(500)]
        public string Motivo { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string Destino { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Monto { get; set; }

        [Required]
        public int IdEstadoSolicitud { get; set; }

        [ForeignKey("IdEstadoSolicitud")]
        public EstadoSolicitud? EstadoSolicitud { get; set; }

        [StringLength(500)]
        public string? Observaciones { get; set; }
    }
}