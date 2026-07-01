using System.ComponentModel.DataAnnotations;

namespace Dinacem.Models
{
    public class EstadoRendicion
    {
        [Key]
        public int IdEstadoRendicion { get; set; }

        [Required]
        [StringLength(30)]
        public string Nombre { get; set; }
    }
}