using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using TelegramCatalogo.Models;

namespace TelegramCatalogo.Data
{
    public class AplicacionDbContexto : DbContext
    {
        public AplicacionDbContexto(DbContextOptions<AplicacionDbContexto> options)
            : base(options)
        {
        }

        public DbSet<Categoria> Categorias { get; set; }
        public DbSet<Canal> Canales { get; set; }
        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<SolicitudCanal> SolicitudesCanal { get; set; }
    }
}