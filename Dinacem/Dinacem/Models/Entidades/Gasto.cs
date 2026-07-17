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

        [ForeignKey("IdRendicion")]
        public Rendicion? Rendicion { get; set; }

        [Required]
        public DateTime Fecha { get; set; }

        [Required]
        public int IdTipoGasto { get; set; }

        [ForeignKey("IdTipoGasto")]
        public TipoGasto? TipoGasto { get; set; }

        [Required]
        public int IdTipoComprobante { get; set; }

        [ForeignKey("IdTipoComprobante")]
        public TipoComprobante? TipoComprobante { get; set; }

        [StringLength(20)]
        public string? Serie { get; set; }

        [StringLength(30)]
        public string? Numero { get; set; }

        [StringLength(500)]
        public string? Detalle { get; set; }

        [StringLength(300)]
        public string? Comprobante { get; set; }

        [Required]
        public decimal MontoTotal { get; set; }

        public decimal IGV { get; set; }

        public bool ExoneracionIGV { get; set; }

        [StringLength(11)]
        public string? Ruc { get; set; }

        [StringLength(250)]
        public string? RazonSocial { get; set; }

        [StringLength(300)]
        public string? DomicilioFiscal { get; set; }
    }
}