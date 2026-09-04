using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dinacem.Models
{
    public class BitacoraVehiculo
    {
        [Key]
        public int IdBitacoraVehiculo { get; set; }

        [Required]
        public int IdRendicion { get; set; }

        [ForeignKey(nameof(IdRendicion))]
        public Rendicion? Rendicion { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime Fecha { get; set; }

        [Required]
        [StringLength(200)]
        public string Origen { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string Destino { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal DistanciaKm { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TarifaKilometro { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal MontoAsignado { get; set; }

        [StringLength(500)]
        public string? Observaciones { get; set; }
    }
}