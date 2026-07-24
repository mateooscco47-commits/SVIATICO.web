using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dinacem.Models
{
    public class Reembolso
    {
        [Key]
        public int IdReembolso { get; set; }

        [Required]
        public int IdRendicion { get; set; }

        [ForeignKey(nameof(IdRendicion))]
        public Rendicion? Rendicion { get; set; }

        [Required]
        public int IdUsuario { get; set; }

        [ForeignKey(nameof(IdUsuario))]
        public Usuario? Usuario { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Monto { get; set; }

        [Required]
        public DateTime FechaSolicitud { get; set; }

        public DateTime? FechaAprobacion { get; set; }

        public DateTime? FechaPago { get; set; }

        [Required]
        public int IdEstadoReembolso { get; set; }

        [StringLength(100)]
        public string? Banco { get; set; }

        [StringLength(100)]
        public string? NumeroOperacion { get; set; }

        [StringLength(500)]
        public string? ComprobantePago { get; set; }

        [StringLength(1000)]
        public string? Observaciones { get; set; }
    }
}