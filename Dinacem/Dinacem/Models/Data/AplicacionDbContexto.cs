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


        // =========================================
        // DBSETS
        // =========================================

        public DbSet<Rol> Roles { get; set; }

        public DbSet<Usuario> Usuarios { get; set; }

        public DbSet<Zona> Zonas { get; set; }

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

        public DbSet<BitacoraVehiculo> BitacorasVehiculo { get; set; }

        public DbSet<ConfiguracionSistema> ConfiguracionesSistema { get; set; }


        protected override void OnModelCreating(
            ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);


            // =========================================
            // NOMBRES DE TABLAS
            // =========================================

            modelBuilder.Entity<Rol>()
                .ToTable("Roles");

            modelBuilder.Entity<Usuario>()
                .ToTable("Usuarios");

            modelBuilder.Entity<Zona>()
                .ToTable("Zonas");

            modelBuilder.Entity<EstadoSolicitud>()
                .ToTable("EstadoSolicitudes");

            modelBuilder.Entity<Solicitud>()
                .ToTable("Solicitudes");

            modelBuilder.Entity<EstadoRendicion>()
                .ToTable("EstadoRendiciones");

            modelBuilder.Entity<Rendicion>()
                .ToTable("Rendiciones");

            modelBuilder.Entity<TipoGasto>()
                .ToTable("TipoGastos");

            modelBuilder.Entity<TipoComprobante>()
                .ToTable("TipoComprobantes");

            modelBuilder.Entity<Gasto>()
                .ToTable("Gastos");

            modelBuilder.Entity<DevolucionSaldo>()
                .ToTable("DevolucionesSaldo");

            modelBuilder.Entity<Reembolso>()
                .ToTable("Reembolsos");

            modelBuilder.Entity<EstadoReembolso>()
                .ToTable("EstadoReembolsos");

            modelBuilder.Entity<BitacoraVehiculo>()
                .ToTable("BitacorasVehiculo");

            modelBuilder.Entity<ConfiguracionSistema>()
                .ToTable("ConfiguracionSistema");


            // =========================================
            // LLAVES PRIMARIAS
            // =========================================

            modelBuilder.Entity<Rol>()
                .HasKey(x => x.IdRol);

            modelBuilder.Entity<Usuario>()
                .HasKey(x => x.IdUsuario);

            modelBuilder.Entity<Zona>()
                .HasKey(x => x.IdZona);

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

            modelBuilder.Entity<BitacoraVehiculo>()
                .HasKey(x => x.IdBitacoraVehiculo);

            modelBuilder.Entity<ConfiguracionSistema>()
                .HasKey(x => x.IdConfiguracion);


            // =========================================
            // ZONA
            // =========================================

            modelBuilder.Entity<Zona>()
                .Property(x => x.CodigoZona)
                .HasMaxLength(10)
                .IsRequired();

            modelBuilder.Entity<Zona>()
                .HasIndex(x => x.CodigoZona)
                .IsUnique();


            // =========================================
            // USUARIO -> ROL
            // =========================================

            modelBuilder.Entity<Usuario>()
                .HasOne(x => x.Rol)
                .WithMany()
                .HasForeignKey(x => x.IdRol)
                .OnDelete(DeleteBehavior.NoAction);


            // =========================================
            // USUARIO -> ZONA
            // =========================================

            modelBuilder.Entity<Usuario>()
                .HasOne(x => x.Zona)
                .WithMany(x => x.Usuarios)
                .HasForeignKey(x => x.IdZona)
                .OnDelete(DeleteBehavior.NoAction);


            // =========================================
            // SOLICITUD -> USUARIO
            // =========================================

            modelBuilder.Entity<Solicitud>()
                .HasOne(x => x.Usuario)
                .WithMany()
                .HasForeignKey(x => x.IdUsuario)
                .OnDelete(DeleteBehavior.NoAction);


            // =========================================
            // SOLICITUD -> ESTADO
            // =========================================

            modelBuilder.Entity<Solicitud>()
                .HasOne(x => x.EstadoSolicitud)
                .WithMany()
                .HasForeignKey(x => x.IdEstadoSolicitud)
                .OnDelete(DeleteBehavior.NoAction);


            // =========================================
            // SOLICITUD -> RENDICIÓN
            // 1 : 0..1
            // =========================================

            modelBuilder.Entity<Solicitud>()
                .HasOne(s => s.Rendicion)
                .WithOne(r => r.Solicitud)
                .HasForeignKey<Rendicion>(
                    r => r.IdSolicitud)
                .OnDelete(DeleteBehavior.NoAction);


            // =========================================
            // RENDICIÓN -> USUARIO
            // =========================================

            modelBuilder.Entity<Rendicion>()
                .HasOne(r => r.Usuario)
                .WithMany()
                .HasForeignKey(r => r.IdUsuario)
                .OnDelete(DeleteBehavior.NoAction);


            // =========================================
            // RENDICIÓN -> ESTADO
            // =========================================

            modelBuilder.Entity<Rendicion>()
                .HasOne(r => r.EstadoRendicion)
                .WithMany()
                .HasForeignKey(r => r.IdEstadoRendicion)
                .OnDelete(DeleteBehavior.NoAction);


            // =========================================
            // RENDICIÓN -> DEVOLUCIÓN
            // 1 : 0..1
            // =========================================

            modelBuilder.Entity<Rendicion>()
                .HasOne(r => r.DevolucionSaldo)
                .WithOne(d => d.Rendicion)
                .HasForeignKey<DevolucionSaldo>(
                    d => d.IdRendicion)
                .OnDelete(DeleteBehavior.NoAction);


            // =========================================
            // RENDICIÓN -> GASTOS
            // 1 : N
            // =========================================

            modelBuilder.Entity<Gasto>()
                .HasOne(g => g.Rendicion)
                .WithMany(r => r.Gastos)
                .HasForeignKey(g => g.IdRendicion)
                .OnDelete(DeleteBehavior.NoAction);


            // =========================================
            // GASTO -> TIPO GASTO
            // =========================================

            modelBuilder.Entity<Gasto>()
                .HasOne(g => g.TipoGasto)
                .WithMany()
                .HasForeignKey(g => g.IdTipoGasto)
                .OnDelete(DeleteBehavior.NoAction);


            // =========================================
            // GASTO -> TIPO COMPROBANTE
            // =========================================

            modelBuilder.Entity<Gasto>()
                .HasOne(g => g.TipoComprobante)
                .WithMany()
                .HasForeignKey(g => g.IdTipoComprobante)
                .OnDelete(DeleteBehavior.NoAction);


            // =========================================
            // RENDICIÓN -> REEMBOLSO
            // 1 : N
            // =========================================

            modelBuilder.Entity<Reembolso>()
                .HasOne(r => r.Rendicion)
                .WithMany()
                .HasForeignKey(r => r.IdRendicion)
                .OnDelete(DeleteBehavior.NoAction);


            // =========================================
            // USUARIO -> REEMBOLSOS
            // =========================================

            modelBuilder.Entity<Reembolso>()
                .HasOne(r => r.Usuario)
                .WithMany()
                .HasForeignKey(r => r.IdUsuario)
                .OnDelete(DeleteBehavior.NoAction);


            // =========================================
            // REEMBOLSO -> ESTADO
            // =========================================

            modelBuilder.Entity<Reembolso>()
                .HasOne(r => r.EstadoReembolso)
                .WithMany()
                .HasForeignKey(r => r.IdEstadoReembolso)
                .OnDelete(DeleteBehavior.NoAction);


            // =========================================
            // RENDICIÓN -> BITÁCORAS DE VEHÍCULO
            // 1 : N
            // =========================================

            modelBuilder.Entity<BitacoraVehiculo>()
                .HasOne(b => b.Rendicion)
                .WithMany(r => r.BitacorasVehiculo)
                .HasForeignKey(b => b.IdRendicion)
                .OnDelete(DeleteBehavior.NoAction);


            // =========================================
            // ÍNDICE ÚNICO DEVOLUCIÓN
            // =========================================

            modelBuilder.Entity<DevolucionSaldo>()
                .HasIndex(x => x.IdRendicion)
                .IsUnique();


            // =========================================
            // DECIMALES - SOLICITUD
            // =========================================

            modelBuilder.Entity<Solicitud>()
                .Property(x => x.Monto)
                .HasPrecision(18, 2);


            // =========================================
            // DECIMALES - RENDICIÓN
            // =========================================

            modelBuilder.Entity<Rendicion>()
                .Property(x => x.Total)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Rendicion>()
                .Property(x => x.Saldo)
                .HasPrecision(18, 2);


            // =========================================
            // DECIMALES - GASTOS
            // =========================================

            modelBuilder.Entity<Gasto>()
                .Property(x => x.MontoTotal)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Gasto>()
                .Property(x => x.ValorVenta)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Gasto>()
                .Property(x => x.IGV)
                .HasPrecision(18, 2);


            // =========================================
            // DECIMALES - DEVOLUCIÓN
            // =========================================

            modelBuilder.Entity<DevolucionSaldo>()
                .Property(x => x.Monto)
                .HasPrecision(18, 2);


            // =========================================
            // DECIMALES - REEMBOLSO
            // =========================================

            modelBuilder.Entity<Reembolso>()
                .Property(x => x.Monto)
                .HasPrecision(18, 2);


            // =========================================
            // BITÁCORA VEHÍCULO
            // =========================================

            modelBuilder.Entity<BitacoraVehiculo>()
                .Property(x => x.DistanciaKm)
                .HasPrecision(18, 2);

            modelBuilder.Entity<BitacoraVehiculo>()
                .Property(x => x.TarifaKilometro)
                .HasPrecision(18, 2);

            modelBuilder.Entity<BitacoraVehiculo>()
                .Property(x => x.MontoAsignado)
                .HasPrecision(18, 2);


            // =========================================
            // CONFIGURACIÓN DEL SISTEMA
            // =========================================

            modelBuilder.Entity<ConfiguracionSistema>()
                .Property(x => x.TarifaKilometro)
                .HasPrecision(18, 2);
        }
    }
}