using System.ComponentModel.DataAnnotations;

namespace Dinacem.Models
{
    public class ConfiguracionSistema
    {
        [Key]
        public int IdConfiguracion { get; set; }

        public decimal TarifaKilometro { get; set; }
    }
}