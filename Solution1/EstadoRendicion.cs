using System.ComponentModel.DataAnnotations;

namespace Dinacem.Dominio.Entidades
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