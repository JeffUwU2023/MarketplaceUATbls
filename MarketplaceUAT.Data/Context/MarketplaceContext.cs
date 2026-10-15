using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MarketplaceUAT.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace MarketplaceUAT.Data.Context
{
    public class MarketplaceContext : DbContext
    {
        public MarketplaceContext(DbContextOptions<MarketplaceContext> options)
            : base(options)
        {
        }

        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Categoria> Categorias { get; set; }
        public DbSet<Producto> Productos { get; set; }
        public DbSet<ImagenProducto> ImagenesProducto { get; set; }
        public DbSet<Favorito> Favoritos { get; set; }
        public DbSet<Conversacion> Conversaciones { get; set; }
        public DbSet<Mensaje> Mensajes { get; set; }
        public DbSet<ImagenMensaje> ImagenesMensajes { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Usuario
            modelBuilder.Entity<Usuario>()
                .ToTable("Usuarios", "dbo");

            modelBuilder.Entity<Usuario>()
                .HasKey(u => u.IdUsuario);

            // Categoria
            modelBuilder.Entity<Categoria>()
                .ToTable("Categorias", "dbo");

            modelBuilder.Entity<Categoria>()
                .HasKey(c => c.IdCategoria);

            // Producto
            modelBuilder.Entity<Producto>()
                .ToTable("Productos", "dbo");

            modelBuilder.Entity<Producto>()
                .HasKey(p => p.IdProducto);

            // Relación Usuario -> Productos
            modelBuilder.Entity<Producto>()
                .HasOne<Usuario>()
                .WithMany()
                .HasForeignKey(p => p.IdUsuario)
                .OnDelete(DeleteBehavior.Restrict);

            // Relación Categoria -> Productos
            modelBuilder.Entity<Producto>()
                .HasOne<Categoria>()
                .WithMany()
                .HasForeignKey(p => p.IdCategoria)
                .OnDelete(DeleteBehavior.Restrict);

            // ImagenProducto
            modelBuilder.Entity<ImagenProducto>()
                .ToTable("ImagenesProducto", "dbo");

            modelBuilder.Entity<ImagenProducto>()
                .HasKey(i => i.IdImagen);

            // Relación Producto -> ImagenesProducto
            modelBuilder.Entity<ImagenProducto>()
                .HasOne<Producto>()
                .WithMany()
                .HasForeignKey(i => i.IdProducto)
                .OnDelete(DeleteBehavior.Restrict);

            // Favorito
            modelBuilder.Entity<Favorito>()
                .ToTable("Favoritos", "dbo");

            modelBuilder.Entity<Favorito>()
                .HasKey(f => f.IdFavorito);

            // Relación Usuario -> Favoritos
            modelBuilder.Entity<Favorito>()
                .HasOne<Usuario>()
                .WithMany()
                .HasForeignKey(f => f.IdUsuario)
                .OnDelete(DeleteBehavior.Restrict);

            // Relación Producto -> Favoritos
            modelBuilder.Entity<Favorito>()
                .HasOne<Producto>()
                .WithMany()
                .HasForeignKey(f => f.IdProducto)
                .OnDelete(DeleteBehavior.Restrict);

            // Evitar favoritos duplicados
            modelBuilder.Entity<Favorito>()
                .HasIndex(f => new { f.IdUsuario, f.IdProducto })
                .IsUnique()
                .HasDatabaseName("UQ_Favoritos_UsuarioProducto");

            modelBuilder.Entity<ImagenMensaje>()
                .ToTable("ImagenesMensaje");

            modelBuilder.Entity<Mensaje>()
                 .HasIndex(m => new
           {
                  m.IdConversacion,
                  m.FechaEnvio
           })
                   .HasDatabaseName("IX_Mensajes_Conversacion_Fecha");
        }
    }
}

