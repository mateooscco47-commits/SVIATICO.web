using Microsoft.EntityFrameworkCore;

namespace Dinacem.Models
{
    public class AplicacionDbContexto : DbContext
    {
        public AplicacionDbContexto(
            DbContextOptions<AplicacionDbContexto> options)
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
        public DbSet<Reembolso> Reembolsos { get; set; }
        public DbSet<EstadoReembolso> EstadoReembolsos { get; set; }

        protected override void OnModelCreating(
            ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // =====================================
            // NOMBRES DE TABLAS
            // =====================================

            modelBuilder.Entity<Rol>()
                .ToTable("Roles");

            modelBuilder.Entity<Usuario>()
                .ToTable("Usuarios");

            modelBuilder.Entity<EstadoSolicitud>()
                .ToTable("EstadoSolicitud");

            modelBuilder.Entity<Solicitud>()
                .ToTable("Solicitudes");

            modelBuilder.Entity<EstadoRendicion>()
                .ToTable("EstadoRendicion");

            modelBuilder.Entity<Rendicion>()
                .ToTable("Rendiciones");

            modelBuilder.Entity<TipoGasto>()
                .ToTable("TipoGasto");

            modelBuilder.Entity<TipoComprobante>()
                .ToTable("TipoComprobante");

            modelBuilder.Entity<Gasto>()
                .ToTable("Gastos");

            modelBuilder.Entity<DevolucionSaldo>()
                .ToTable("DevolucionesSaldo");

            modelBuilder.Entity<Reembolso>()
                .ToTable("Reembolsos");

            modelBuilder.Entity<EstadoReembolso>()
                .ToTable("EstadoReembolso");

            // =====================================
            // LLAVES PRIMARIAS
            // =====================================

            modelBuilder.Entity<Rol>()
                .HasKey(x => x.IdRol);

            modelBuilder.Entity<Usuario>()
                .HasKey(x => x.IdUsuario);

            modelBuilder.Entity<EstadoSolicitud>()
                .HasKey(x => x.IdEstadoSolicitud);

            modelBuilder.Entity<Solicitud>()
                .HasKey(x => x.IdSolicitud);

            modelBuilder.Entity<EstadoRendicion>()
                .HasKey(x => x.IdEstadoRendicion);

            modelBuilder.Entity<Rendicion>()
                .HasKey(x => x.IdRendicion);

            modelBuilder.Entity<TipoGasto>()
                .HasKey(x => x.IdTipoGasto);

            modelBuilder.Entity<TipoComprobante>()
                .HasKey(x => x.IdTipoComprobante);

            modelBuilder.Entity<Gasto>()
                .HasKey(x => x.IdGasto);

            modelBuilder.Entity<DevolucionSaldo>()
                .HasKey(x => x.IdDevolucionSaldo);

            modelBuilder.Entity<Reembolso>()
                .HasKey(x => x.IdReembolso);

            modelBuilder.Entity<EstadoReembolso>()
                .HasKey(x => x.IdEstadoReembolso);

            // =====================================
            // RELACIONES
            // =====================================

            // Usuario -> Rol
            modelBuilder.Entity<Usuario>()
                .HasOne(x => x.Rol)
                .WithMany()
                .HasForeignKey(x => x.IdRol)
                .OnDelete(DeleteBehavior.NoAction);

            // Solicitud -> Usuario
            modelBuilder.Entity<Solicitud>()
                .HasOne(x => x.Usuario)
                .WithMany()
                .HasForeignKey(x => x.IdUsuario)
                .OnDelete(DeleteBehavior.NoAction);

            // Solicitud -> EstadoSolicitud
            modelBuilder.Entity<Solicitud>()
                .HasOne(x => x.EstadoSolicitud)
                .WithMany()
                .HasForeignKey(x => x.IdEstadoSolicitud)
                .OnDelete(DeleteBehavior.NoAction);

            // Solicitud -> Rendicion
            // Una solicitud tiene como máximo una rendición
            modelBuilder.Entity<Solicitud>()
                .HasOne(s => s.Rendicion)
                .WithOne(r => r.Solicitud)
                .HasForeignKey<Rendicion>(
                    r => r.IdSolicitud)
                .OnDelete(DeleteBehavior.NoAction);

            // Rendicion -> Usuario
            modelBuilder.Entity<Rendicion>()
                .HasOne(r => r.Usuario)
                .WithMany()
                .HasForeignKey(r => r.IdUsuario)
                .OnDelete(DeleteBehavior.NoAction);

            // Rendicion -> EstadoRendicion
            modelBuilder.Entity<Rendicion>()
                .HasOne(r => r.EstadoRendicion)
                .WithMany()
                .HasForeignKey(r => r.IdEstadoRendicion)
                .OnDelete(DeleteBehavior.NoAction);

            // Gasto -> Rendicion
            modelBuilder.Entity<Gasto>()
                .HasOne(x => x.Rendicion)
                .WithMany(x => x.Gastos)
                .HasForeignKey(x => x.IdRendicion)
                .OnDelete(DeleteBehavior.NoAction);

            // Gasto -> TipoGasto
            modelBuilder.Entity<Gasto>()
                .HasOne(x => x.TipoGasto)
                .WithMany()
                .HasForeignKey(x => x.IdTipoGasto)
                .OnDelete(DeleteBehavior.NoAction);

            // Gasto -> TipoComprobante
            modelBuilder.Entity<Gasto>()
                .HasOne(x => x.TipoComprobante)
                .WithMany()
                .HasForeignKey(x => x.IdTipoComprobante)
                .OnDelete(DeleteBehavior.NoAction);

            // Reembolso -> Rendicion
            modelBuilder.Entity<Reembolso>()
                .HasOne(x => x.Rendicion)
                .WithMany()
                .HasForeignKey(x => x.IdRendicion)
                .OnDelete(DeleteBehavior.NoAction);

            // Reembolso -> Usuario
            modelBuilder.Entity<Reembolso>()
                .HasOne(x => x.Usuario)
                .WithMany()
                .HasForeignKey(x => x.IdUsuario)
                .OnDelete(DeleteBehavior.NoAction);

            // Reembolso -> EstadoReembolso
            modelBuilder.Entity<Reembolso>()
                .HasOne(x => x.EstadoReembolso)
                .WithMany()
                .HasForeignKey(x => x.IdEstadoReembolso)
                .OnDelete(DeleteBehavior.NoAction);

            // Devolución -> Rendición
            modelBuilder.Entity<DevolucionSaldo>()
                .HasOne(x => x.Rendicion)
                .WithMany()
                .HasForeignKey(x => x.IdRendicion)
                .OnDelete(DeleteBehavior.NoAction);

            // =====================================
            // ÍNDICES ÚNICOS
            // =====================================

            // Una rendición solo puede tener una devolución
            modelBuilder.Entity<DevolucionSaldo>()
                .HasIndex(x => x.IdRendicion)
                .IsUnique();

            // Una rendición solo puede tener un reembolso
            modelBuilder.Entity<Reembolso>()
                .HasIndex(x => x.IdRendicion)
                .IsUnique();

            // =====================================
            // DECIMALES
            // =====================================

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
                .Property(x => x.ValorVenta)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Gasto>()
                .Property(x => x.IGV)
                .HasPrecision(18, 2);

            modelBuilder.Entity<DevolucionSaldo>()
                .Property(x => x.Monto)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Reembolso>()
                .Property(x => x.Monto)
                .HasPrecision(18, 2);
        }
    }
}