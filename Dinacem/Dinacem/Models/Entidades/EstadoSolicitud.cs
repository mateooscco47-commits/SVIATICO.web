using System.ComponentModel.DataAnnotations;

namespace Dinacem.Models
{
    public class EstadoSolicitud
    {
        [Key]
        public int IdEstadoSolicitud { get; set; }

        [Required]
        [StringLength(30)]
        public string Nombre { get; set; }
    }
}