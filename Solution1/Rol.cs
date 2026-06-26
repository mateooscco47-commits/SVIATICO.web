using System.ComponentModel.DataAnnotations;

namespace Dinacem.Dominio.Entidades
{
    public class Rol
    {
        [Key]
        public int IdRol { get; set; }

        [Required]
        [StringLength(50)]
        public string Nombre { get; set; }

        [Required]
        public bool Estado { get; set; }
    }
}