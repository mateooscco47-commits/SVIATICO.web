using Microsoft.EntityFrameworkCore;

namespace Dinacem.Models
{
    public class AplicacionDbContexto : DbContext
    {
        public AplicacionDbContexto(DbContextOptions<AplicacionDbContexto> options)
            : base(options)
        {
        }

        public DbSet<Rol> Roles { get; set; }
        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<EstadoSolicitud> EstadoSolicitudes { get; set; }
        public DbSet<Solicitud> Solicitudes { get; set; }
        public DbSet<EstadoRendicion> EstadoRendiciones { get; set; }
        public DbSet<Rendicion> Rendiciones { get; set; }
        public DbSet<TipoGasto> TipoGastos { get; set; }
        public DbSet<TipoComprobante> TipoComprobantes { get; set; }
        public DbSet<Gasto> Gastos { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }
    }
}