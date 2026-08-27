using System.Collections.Generic;

namespace Dinacem.Models
{
    public class Zona
    {
        public int IdZona { get; set; }

        public string CodigoZona { get; set; } = string.Empty;

        public bool Estado { get; set; }

        // Relación: una zona puede estar asociada
        // a varios usuarios en la BD.
        public ICollection<Usuario> Usuarios { get; set; }
            = new List<Usuario>();
    }
}