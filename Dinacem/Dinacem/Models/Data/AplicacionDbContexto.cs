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

        public DbSet<DevolucionSaldo> DevolucionesSaldo { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Nombres reales de tablas en SQL Server
            modelBuilder.Entity<Rol>().ToTable("Roles");
            modelBuilder.Entity<Usuario>().ToTable("Usuarios");
            modelBuilder.Entity<EstadoSolicitud>().ToTable("EstadoSolicitud");
            modelBuilder.Entity<Solicitud>().ToTable("Solicitudes");
            modelBuilder.Entity<EstadoRendicion>().ToTable("EstadoRendicion");
            modelBuilder.Entity<Rendicion>().ToTable("Rendiciones");
            modelBuilder.Entity<TipoGasto>().ToTable("TipoGasto");
            modelBuilder.Entity<TipoComprobante>().ToTable("TipoComprobante");
            modelBuilder.Entity<Gasto>().ToTable("Gastos");

            // Llaves primarias
            modelBuilder.Entity<Rol>().HasKey(x => x.IdRol);
            modelBuilder.Entity<Usuario>().HasKey(x => x.IdUsuario);
            modelBuilder.Entity<EstadoSolicitud>().HasKey(x => x.IdEstadoSolicitud);
            modelBuilder.Entity<Solicitud>().HasKey(x => x.IdSolicitud);
            modelBuilder.Entity<EstadoRendicion>().HasKey(x => x.IdEstadoRendicion);
            modelBuilder.Entity<Rendicion>().HasKey(x => x.IdRendicion);
            modelBuilder.Entity<TipoGasto>().HasKey(x => x.IdTipoGasto);
            modelBuilder.Entity<TipoComprobante>().HasKey(x => x.IdTipoComprobante);
            modelBuilder.Entity<Gasto>().HasKey(x => x.IdGasto);

            // Relaciones
            modelBuilder.Entity<Usuario>()
                .HasOne(x => x.Rol)
                .WithMany()
                .HasForeignKey(x => x.IdRol)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Solicitud>()
                .HasOne(x => x.Usuario)
                .WithMany()
                .HasForeignKey(x => x.IdUsuario)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Solicitud>()
                .HasOne(x => x.EstadoSolicitud)
                .WithMany()
                .HasForeignKey(x => x.IdEstadoSolicitud)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Rendicion>()
                .HasOne(x => x.Solicitud)
                .WithMany()
                .HasForeignKey(x => x.IdSolicitud)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Rendicion>()
                .HasOne(x => x.Usuario)
                .WithMany()
                .HasForeignKey(x => x.IdUsuario)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Rendicion>()
                .HasOne(x => x.EstadoRendicion)
                .WithMany()
                .HasForeignKey(x => x.IdEstadoRendicion)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Gasto>()
                .HasOne(x => x.Rendicion)
                .WithMany()
                .HasForeignKey(x => x.IdRendicion)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Gasto>()
                .HasOne(x => x.TipoGasto)
                .WithMany()
                .HasForeignKey(x => x.IdTipoGasto)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Gasto>()
                .HasOne(x => x.TipoComprobante)
                .WithMany()
                .HasForeignKey(x => x.IdTipoComprobante)
                .OnDelete(DeleteBehavior.Restrict);

            // Decimales
            modelBuilder.Entity<Solicitud>()
                .Property(x => x.Monto)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Rendicion>()
                .Property(x => x.Total)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Rendicion>()
                .Property(x => x.Saldo)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Gasto>()
                .Property(x => x.MontoTotal)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Gasto>()
                .Property(x => x.IGV)
                .HasPrecision(18, 2);
            modelBuilder.Entity<DevolucionSaldo>()
    .HasIndex(d => d.IdRendicion)
    .IsUnique();

            modelBuilder.Entity<DevolucionSaldo>()
                .Property(d => d.Monto)
                .HasPrecision(18, 2);
        }
    }
}