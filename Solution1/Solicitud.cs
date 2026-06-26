using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dinacem.Dominio.Entidades
{
    public class Solicitud
    {
        [Key]
        public int IdSolicitud { get; set; }

        [Required]
        public int IdUsuario { get; set; }

        [ForeignKey("IdUsuario")]
        public Usuario Usuario { get; set; }

        [Required]
        public DateTime Fecha { get; set; }

        [Required]
        public DateTime FechaInicio { get; set; }

        [Required]
        public DateTime FechaFin { get; set; }

        [Required]
        [StringLength(500)]
        public string Motivo { get; set; }

        [Required]
        [StringLength(200)]
        public string Destino { get; set; }

        [Required]
        public decimal Monto { get; set; }

        [Required]
        public int IdEstadoSolicitud { get; set; }

        [ForeignKey("IdEstadoSolicitud")]
        public EstadoSolicitud EstadoSolicitud { get; set; }

        [StringLength(500)]
        public string Observaciones { get; set; }
    }
}