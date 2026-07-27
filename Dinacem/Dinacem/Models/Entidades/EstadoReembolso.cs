using System.ComponentModel.DataAnnotations;

namespace Dinacem.Models
{
    public class EstadoReembolso
    {
        

            [Key]
            public int IdEstadoReembolso { get; set; }


            [Required]
            [StringLength(30)]
            public string Nombre { get; set; } = string.Empty;

        
    }
}

