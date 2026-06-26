using System.ComponentModel.DataAnnotations;

namespace Dinacem.Dominio.Entidades
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