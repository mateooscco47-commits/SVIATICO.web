using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dinacem.Models
{
    public class Usuario
    {
        [Key]
        public int IdUsuario { get; set; }

        // =========================================
        // ROL
        // =========================================

        [Required(ErrorMessage = "Debe seleccionar un rol.")]
        public int IdRol { get; set; }

        [ForeignKey(nameof(IdRol))]
        public Rol? Rol { get; set; }


        // =========================================
        // DATOS PERSONALES
        // =========================================

        [Required(ErrorMessage = "Ingrese los nombres.")]
        [StringLength(100)]
        public string Nombres { get; set; } = string.Empty;

        [Required(ErrorMessage = "Ingrese los apellidos.")]
        [StringLength(100)]
        public string Apellidos { get; set; } = string.Empty;

        [Required(ErrorMessage = "Ingrese el correo.")]
        [EmailAddress(ErrorMessage = "Ingrese un correo válido.")]
        [StringLength(150)]
        public string Correo { get; set; } = string.Empty;

        [RegularExpression(
            @"^9\d{8}$",
            ErrorMessage =
                "El celular debe tener 9 dígitos y comenzar con 9.")]
        [StringLength(20)]
        public string? Celular { get; set; }


        // =========================================
        // ZONA
        // =========================================

        public int? IdZona { get; set; }

        [ForeignKey(nameof(IdZona))]
        public Zona? Zona { get; set; }


        // =========================================
        // ACCESO
        // =========================================

        [Required(ErrorMessage = "Ingrese el usuario de acceso.")]
        [StringLength(
            50,
            MinimumLength = 4,
            ErrorMessage =
                "Debe tener entre 4 y 50 caracteres.")]
        public string UsuarioAcceso { get; set; } =
            string.Empty;

        [Required(ErrorMessage = "Ingrese la contraseña.")]
        [StringLength(
            100,
            MinimumLength = 8,
            ErrorMessage =
                "La contraseña debe tener mínimo 8 caracteres.")]
        [RegularExpression(
            @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z0-9]).+$",
            ErrorMessage =
                "Debe contener mayúscula, minúscula, número y carácter especial.")]
        public string Contrasenia { get; set; } =
            string.Empty;


        // =========================================
        // ESTADO
        // =========================================

        public bool Estado { get; set; } = true;
    }
}