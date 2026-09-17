using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TelegramCatalogo.Models
{
    public class SolicitudCanal
    {
        [Key]
        public int IdSolicitud { get; set; }

        [Required(ErrorMessage = "El nombre del canal es obligatorio.")]
        [StringLength(150)]
        public string NombreCanal { get; set; } = string.Empty;

        [Required(ErrorMessage = "El enlace de Telegram es obligatorio.")]
        [StringLength(500)]
        public string EnlaceTelegram { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Descripcion { get; set; }

        [EmailAddress(ErrorMessage = "Ingresa un correo válido.")]
        [StringLength(150)]
        public string? CorreoContacto { get; set; }

        [Required(ErrorMessage = "Selecciona una categoría.")]
        public int? IdCategoria { get; set; }

        [StringLength(30)]
        public string Estado { get; set; } = "Pendiente";

        [StringLength(500)]
        public string? Observaciones { get; set; }

        public DateTime FechaSolicitud { get; set; } = DateTime.Now;

        [ForeignKey("IdCategoria")]
        public Categoria? Categoria { get; set; }
    }
}