using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TelegramCatalogo.Models
{
    public class Canal
    {
        [Key]
        public int IdCanal { get; set; }

        [Required]
        public int IdCategoria { get; set; }

        [Required]
        [StringLength(150)]
        public string Nombre { get; set; } = string.Empty;

        [StringLength(200)]
        public string? Slug { get; set; }

        [StringLength(150)]
        public string? UsernameTelegram { get; set; }

        [StringLength(1000)]
        public string? Descripcion { get; set; }

        [StringLength(500)]
        public string? Imagen { get; set; }

        [Required]
        [StringLength(500)]
        public string EnlaceTelegram { get; set; } = string.Empty;

        public int CantidadMiembros { get; set; }

        [StringLength(50)]
        public string? Idioma { get; set; }

        [StringLength(100)]
        public string? Pais { get; set; }

        public bool EsDestacado { get; set; }

        [StringLength(30)]
        public string Estado { get; set; } = "Activo";

        public DateTime FechaRegistro { get; set; } = DateTime.Now;
        public DateTime? FechaActualizacion { get; set; }

        public int Visitas { get; set; }

        public int Clicks { get; set; }

        [ForeignKey("IdCategoria")]
        public Categoria? Categoria { get; set; }
    }
}