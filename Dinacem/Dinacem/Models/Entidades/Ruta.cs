using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dinacen.Models.Entidades
{
    public class Ruta
    {
        [Key]
        public int IdRuta { get; set; }

        [Required]
        [StringLength(200)]
        public string Origen { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string Destino { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Kilometros { get; set; }

        [Required]
        public bool Estado { get; set; } = true;
    }
}