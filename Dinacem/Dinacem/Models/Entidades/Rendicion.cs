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
        public Solicitud Solicitud { get; set; }

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
        public decimal Total { get; set; }

        [Required]
        public decimal Saldo { get; set; }

        [Required]
        public int IdEstadoRendicion { get; set; }

        [ForeignKey("IdEstadoRendicion")]
        public EstadoRendicion EstadoRendicion { get; set; }
    }
}