using Dinnacem.Dominio.Entidades;
using Microsoft.EntityFrameworkCore;

namespace DinacemInfraestructura.Data
{
    public class AplicacionDbContexto : DbContext
    {
        public AplicacionDbContexto(DbContextOptions<AplicacionDbContexto> options)
            : base(options)
        {
        }

        // Seguridad
        public DbSet<Rol> Roles { get; set; }
        public DbSet<Usuario> Usuarios { get; set; }

        // Solicitudes
        public DbSet<EstadoSolicitud> EstadoSolicitudes { get; set; }
        public DbSet<Solicitud> Solicitudes { get; set; }

        // Rendiciones
        public DbSet<EstadoRendicion> EstadoRendiciones { get; set; }
        public DbSet<Rendicion> Rendiciones { get; set; }

        // Catálogos
        public DbSet<TipoGasto> TipoGastos { get; set; }
        public DbSet<TipoComprobante> TipoComprobantes { get; set; }

        // Gastos
        public DbSet<Gasto> Gastos { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Evitar eliminación en cascada
            foreach (var relationship in modelBuilder.Model.GetEntityTypes()
                         .SelectMany(e => e.GetForeignKeys()))
            {
                relationship.DeleteBehavior = DeleteBehavior.Restrict;
            }
        }
    }
}