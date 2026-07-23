using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dinacem.Models
{
    public class DevolucionSaldo
    {
        [Key]
        public int IdDevolucionSaldo { get; set; }

        [Required]
        public int IdRendicion { get; set; }

        [ForeignKey(nameof(IdRendicion))]
        public Rendicion? Rendicion { get; set; }

        [Required]
        public DateTime Fecha { get; set; }

        [Required]
        [Range(0.01, 999999999)]
        public decimal Monto { get; set; }

        [Required]
        [StringLength(100)]
        public string Banco { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string NumeroOperacion { get; set; } = string.Empty;

        [StringLength(300)]
        public string? Voucher { get; set; }

        [StringLength(500)]
        public string? Observaciones { get; set; }
    }
}