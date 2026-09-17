using System.ComponentModel.DataAnnotations;

namespace TelegramCatalogo.Models
{
    public class Categoria
    {
        [Key]
        public int IdCategoria { get; set; }

        [Required]
        [StringLength(100)]
        public string Nombre { get; set; } = string.Empty;

        [StringLength(150)]
        public string? Slug { get; set; }

        [StringLength(300)]
        public string? Descripcion { get; set; }

        [StringLength(100)]
        public string? Icono { get; set; }

        public bool Estado { get; set; } = true;

        public ICollection<Canal>? Canales { get; set; }
    }
}