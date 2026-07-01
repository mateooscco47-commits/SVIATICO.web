using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dinacem.Models
{
    public class Usuario
    {
        [Key]
        public int IdUsuario { get; set; }

        public int IdRol { get; set; }

        [ForeignKey("IdRol")]
        public Rol? Rol { get; set; }

        public string Nombres { get; set; }
        public string Apellidos { get; set; }
        public string Correo { get; set; }
        public string Contrasenia { get; set; }
        public string Celular { get; set; }
        public string Vehiculo { get; set; }
        [Required]
        [StringLength(50)]
        public string UsuarioAcceso { get; set; } = string.Empty;
    }
}