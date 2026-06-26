using System.ComponentModel.DataAnnotations;

namespace Dinacem.Dominio.Entidades
{
    public class TipoComprobante
    {
        [Key]
        public int IdTipoComprobante { get; set; }

        [Required]
        [StringLength(50)]
        public string Nombre { get; set; }
    }
}