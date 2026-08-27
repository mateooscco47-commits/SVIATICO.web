using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dinacem.Models
{
    public class BitacoraVehiculo
    {
        [Key]
        public int IdBitacoraVehiculo { get; set; }

        // =========================================
        // RENDICIÓN
        // =========================================

        [Required]
        public int IdRendicion { get; set; }

        [ForeignKey(nameof(IdRendicion))]
        public Rendicion? Rendicion { get; set; }


        // =========================================
        // FECHA DEL RECORRIDO
        // =========================================

        [Required]
        [DataType(DataType.Date)]
        public DateTime Fecha { get; set; }


        // =========================================
        // ORIGEN
        // =========================================

        [Required]
        [StringLength(200)]
        public string Origen { get; set; } = string.Empty;


        // =========================================
        // DESTINO
        // =========================================

        [Required]
        [StringLength(200)]
        public string Destino { get; set; } = string.Empty;


        // =========================================
        // DISTANCIA RECORRIDA
        // =========================================

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal DistanciaKm { get; set; }


        // =========================================
        // MONTO ASIGNADO
        // =========================================

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal MontoAsignado { get; set; }


        // =========================================
        // OBSERVACIONES
        // =========================================

        [StringLength(500)]
        public string? Observaciones { get; set; }
    }
}