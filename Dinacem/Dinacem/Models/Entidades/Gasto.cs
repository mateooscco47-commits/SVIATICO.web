using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dinacem.Models
{
    public class Gasto
    {
        [Key]
        public int IdGasto { get; set; }

        [Required]
        public int IdRendicion { get; set; }

        [ForeignKey(nameof(IdRendicion))]
        public Rendicion? Rendicion { get; set; }

        [Required]
        public DateTime Fecha { get; set; }

        [Required]
        public int IdTipoGasto { get; set; }

        [ForeignKey(nameof(IdTipoGasto))]
        public TipoGasto? TipoGasto { get; set; }

        [Required]
        public int IdTipoComprobante { get; set; }

        [ForeignKey(nameof(IdTipoComprobante))]
        public TipoComprobante? TipoComprobante { get; set; }

        [StringLength(11)]
        public string? Ruc { get; set; }

        [StringLength(250)]
        public string? RazonSocial { get; set; }

        [StringLength(300)]
        public string? DomicilioFiscal { get; set; }

        [StringLength(20)]
        public string? Serie { get; set; }

        [StringLength(30)]
        public string? Numero { get; set; }

        [StringLength(500)]
        public string? Detalle { get; set; }

        [StringLength(300)]
        public string? Comprobante { get; set; }

        // Importe total del comprobante, incluido IGV.
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Range(typeof(decimal), "0.01", "999999999.99",
            ErrorMessage = "El monto total debe ser mayor que cero.")]
        public decimal MontoTotal { get; set; }

        // Base imponible o valor de venta.
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal ValorVenta { get; set; }

        // IGV calculado.
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal IGV { get; set; }

        // true: comprobante exonerado; false: comprobante gravado.
        public bool ExoneracionIGV { get; set; }
    }
}