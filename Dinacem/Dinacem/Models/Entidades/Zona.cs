using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Dinacem.Models
{
    public class Zona
    {
        [Key]
        public int IdZona { get; set; }

        [Required]
        [StringLength(100)]
        public string CodigoZona { get; set; } = string.Empty;

        public bool Estado { get; set; } = true;

        public ICollection<Usuario> Usuarios { get; set; }
            = new List<Usuario>();
    }
}