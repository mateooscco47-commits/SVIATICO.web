using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dinacem.Dominio.Entidades
{
    public class Usuario
    {
        [Key]
        public int IdUsuario { get; set; }

        [Required]
        public int IdRol { get; set; }

        [ForeignKey("IdRol")]
        public Rol Rol { get; set; }

        [Required]
        [StringLength(100)]
        public string Nombres { get; set; }

        [Required]
        [StringLength(100)]
        public string Apellidos { get; set; }

        [Required]
        [StringLength(150)]
        public string Correo { get; set; }

        [Required]
        [StringLength(255)]
        public string Contrasenia { get; set; }

        [StringLength(20)]
        public string Celular { get; set; }

        [StringLength(100)]
        public string Vehiculo { get; set; }
    }
}