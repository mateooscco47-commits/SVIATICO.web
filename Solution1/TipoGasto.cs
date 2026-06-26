using System.ComponentModel.DataAnnotations;

namespace Dinacem.Dominio.Entidades
{
    public class TipoGasto
    {
        [Key]
        public int IdTipoGasto { get; set; }

        [Required]
        [StringLength(50)]
        public string Nombre { get; set; }
    }
}