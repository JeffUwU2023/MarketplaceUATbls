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
        public DbSet<Carrito> Carritos { get; set; }
        public DbSet<Conversacion> Conversaciones { get; set; }
        public DbSet<Mensaje> Mensajes { get; set; }
        public DbSet<ImagenMensaje> ImagenesMensajes { get; set; }
        public DbSet<Transaccion> Transacciones { get; set; }
        public DbSet<NotificacionTransaccion> NotificacionesTransacciones { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Usuario
            modelBuilder.Entity<Usuario>()
                .ToTable("Usuarios", "dbo");

            modelBuilder.Entity<Usuario>()
                .HasKey(u => u.IdUsuario);

            // Configuración de propiedades de Usuario
            modelBuilder.Entity<Usuario>()
                .Property(u => u.Nombres)
                .HasMaxLength(100)
                .IsRequired();

            modelBuilder.Entity<Usuario>()
                .Property(u => u.Apellidos)
                .HasMaxLength(100)
                .IsRequired();

            modelBuilder.Entity<Usuario>()
                .Property(u => u.TipoUsuario)
                .HasMaxLength(10)
                .IsRequired();

            modelBuilder.Entity<Usuario>()
                .Property(u => u.Matricula)
                .HasMaxLength(30)
                .IsRequired(false);

            modelBuilder.Entity<Usuario>()
                .Property(u => u.Correo)
                .HasMaxLength(150)
                .IsRequired();

            modelBuilder.Entity<Usuario>()
                .Property(u => u.UrlFotoPerfil)
                .IsRequired(false);

            modelBuilder.Entity<Usuario>()
                .Property(u => u.PasswordHash)
                .HasMaxLength(255)
                .IsRequired();

            // Evitar correos duplicados
            modelBuilder.Entity<Usuario>()
                .HasIndex(u => u.Correo)
                .IsUnique()
                .HasDatabaseName("UQ_Usuarios_Correo");

            // Categoria
            modelBuilder.Entity<Categoria>()
                .ToTable("Categorias", "dbo");

            modelBuilder.Entity<Categoria>()
                .HasKey(c => c.IdCategoria);

            // Configuración de propiedades de Categoria
            modelBuilder.Entity<Categoria>()
                .Property(c => c.Nombre)
                .HasMaxLength(50)
                .IsUnicode(false)
                .IsRequired();

            modelBuilder.Entity<Categoria>()
                .Property(c => c.Descripcion)
                .HasMaxLength(200)
                .IsUnicode(false)
                .IsRequired(false);

            // Producto
            modelBuilder.Entity<Producto>()
                .ToTable("Productos", "dbo");

            modelBuilder.Entity<Producto>()
                .HasKey(p => p.IdProducto);

            // Configuración de propiedades de Producto
            modelBuilder.Entity<Producto>()
                .Property(p => p.Nombre)
                .HasMaxLength(100)
                .IsRequired();

            modelBuilder.Entity<Producto>()
                .Property(p => p.Descripcion)
                .HasMaxLength(500)
                .IsRequired(false);

            modelBuilder.Entity<Producto>()
                .Property(p => p.Condicion)
                .HasMaxLength(30)
                .IsRequired();

            modelBuilder.Entity<Producto>()
                .Property(p => p.Estado)
                .HasMaxLength(20)
                .IsRequired();

            modelBuilder.Entity<Producto>()
                .Property(p => p.Precio)
                .HasPrecision(10, 2);

            modelBuilder.Entity<Producto>()
                .Property(p => p.EsProductoUnico)
                .HasDefaultValue(true);

            modelBuilder.Entity<Producto>()
                .Property(p => p.CantidadDisponible)
                .HasDefaultValue(1);

            modelBuilder.Entity<Producto>()
                .Property(p => p.RowVersion)
                .IsRowVersion();

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

            modelBuilder.Entity<Producto>()
                .HasIndex(p => new
             {
                   p.IdUsuario,
                   p.Activo,
                   p.FechaPublicacion
             })
                .IsDescending(false, false, true)
                .HasDatabaseName("IX_Productos_Usuario_Activo_Fecha");

            // ImagenProducto
            modelBuilder.Entity<ImagenProducto>()
                .ToTable("ImagenesProducto", "dbo");

            modelBuilder.Entity<ImagenProducto>()
                .HasKey(i => i.IdImagen);

            // Configuración de propiedades de ImagenProducto
            modelBuilder.Entity<ImagenProducto>()
                .Property(i => i.UrlImagen)
                .HasMaxLength(500)
                .IsUnicode(false)
                .IsRequired();

            // Relación Producto -> ImagenesProducto
            modelBuilder.Entity<ImagenProducto>()
                .HasOne<Producto>()
                .WithMany()
                .HasForeignKey(i => i.IdProducto)
                .OnDelete(DeleteBehavior.Restrict);

            // Índice para consultar imágenes activas por producto
            modelBuilder.Entity<ImagenProducto>()
                .HasIndex(i => new { i.IdProducto, i.Activo })
                .HasDatabaseName("IX_ImagenesProducto_Producto_Activo");

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

            // Carrito
            modelBuilder.Entity<Carrito>()
                .ToTable("Carrito", "dbo");

            modelBuilder.Entity<Carrito>()
                .HasKey(c => c.IdCarrito);

            // Configuración de la cantidad
            modelBuilder.Entity<Carrito>()
                .Property(c => c.Cantidad)
                .IsRequired()
                .HasDefaultValue(1);

            // Fecha en que se agregó el producto
            modelBuilder.Entity<Carrito>()
                .Property(c => c.FechaAgregado)
                .IsRequired()
                .HasDefaultValueSql("GETDATE()");

            // Relación Usuario -> Carrito
            modelBuilder.Entity<Carrito>()
                .HasOne<Usuario>()
                .WithMany()
                .HasForeignKey(c => c.IdUsuario)
                .OnDelete(DeleteBehavior.Restrict);

            // Relación Producto -> Carrito
            modelBuilder.Entity<Carrito>()
                .HasOne<Producto>()
                .WithMany()
                .HasForeignKey(c => c.IdProducto)
                .OnDelete(DeleteBehavior.Restrict);

            // Evitar productos duplicados en el carrito de un usuario
            modelBuilder.Entity<Carrito>()
                .HasIndex(c => new { c.IdUsuario, c.IdProducto })
                .IsUnique()
                .HasDatabaseName("UQ_Carrito_UsuarioProducto");

            // Conversacion
            modelBuilder.Entity<Conversacion>()
                .ToTable("Conversaciones", "dbo");

            modelBuilder.Entity<Conversacion>()
                .HasKey(c => c.IdConversacion);

            // Relación Producto -> Conversaciones
            modelBuilder.Entity<Conversacion>()
                .HasOne<Producto>()
                .WithMany()
                .HasForeignKey(c => c.IdProducto)
                .OnDelete(DeleteBehavior.Restrict);

            // Relación Usuario comprador -> Conversaciones
            modelBuilder.Entity<Conversacion>()
                .HasOne<Usuario>()
                .WithMany()
                .HasForeignKey(c => c.IdUsuarioComprador)
                .OnDelete(DeleteBehavior.Restrict);

            // Relación Usuario vendedor -> Conversaciones
            modelBuilder.Entity<Conversacion>()
                .HasOne<Usuario>()
                .WithMany()
                .HasForeignKey(c => c.IdUsuarioVendedor)
                .OnDelete(DeleteBehavior.Restrict);

            // Evitar conversaciones duplicadas por producto y participantes
            modelBuilder.Entity<Conversacion>()
                .HasIndex(c => new
                {
                    c.IdProducto,
                    c.IdUsuarioComprador,
                    c.IdUsuarioVendedor
                })
                .IsUnique()
                .HasDatabaseName("UQ_Conversaciones_ProductoCompradorVendedor");

            // Mensaje
            modelBuilder.Entity<Mensaje>()
                .ToTable("Mensajes", "dbo");

            modelBuilder.Entity<Mensaje>()
                .HasKey(m => m.IdMensaje);

            // Configuración de propiedades de Mensaje
            modelBuilder.Entity<Mensaje>()
                .Property(m => m.Contenido)
                .IsRequired(false);

            modelBuilder.Entity<Mensaje>()
                .Property(m => m.UrlImagen)
                .HasMaxLength(500)
                .IsRequired(false);

            // Relación Conversacion -> Mensajes
            modelBuilder.Entity<Mensaje>()
                .HasOne<Conversacion>()
                .WithMany()
                .HasForeignKey(m => m.IdConversacion)
                .OnDelete(DeleteBehavior.Restrict);

            // Relación Usuario -> Mensajes
            modelBuilder.Entity<Mensaje>()
                .HasOne<Usuario>()
                .WithMany()
                .HasForeignKey(m => m.IdUsuario)
                .OnDelete(DeleteBehavior.Restrict);

            // Índice para consultar mensajes por conversación y fecha
            modelBuilder.Entity<Mensaje>()
                .HasIndex(m => new
                {
                    m.IdConversacion,
                    m.FechaEnvio
                })
                .HasDatabaseName("IX_Mensajes_Conversacion_Fecha");

            // ImagenMensaje
            modelBuilder.Entity<ImagenMensaje>()
                .ToTable("ImagenesMensaje", "dbo");

            modelBuilder.Entity<ImagenMensaje>()
                .HasKey(i => i.IdImagenMensaje);

            // Configuración de propiedades de ImagenMensaje
            modelBuilder.Entity<ImagenMensaje>()
                .Property(i => i.UrlImagen)
                .HasMaxLength(1000)
                .IsRequired();

            // Relación Mensaje -> ImagenesMensaje
            modelBuilder.Entity<ImagenMensaje>()
                .HasOne<Mensaje>()
                .WithMany()
                .HasForeignKey(i => i.IdMensaje)
                .OnDelete(DeleteBehavior.Restrict);

            // Índice para consultar imágenes activas por mensaje
            modelBuilder.Entity<ImagenMensaje>()
                .HasIndex(i => new
                {
                    i.IdMensaje,
                    i.Activo
                })
                .HasDatabaseName("IX_ImagenesMensaje_Mensaje_Activo");

            // Transaccion
            modelBuilder.Entity<Transaccion>()
                .ToTable("Transacciones", "dbo");

            modelBuilder.Entity<Transaccion>()
                .HasKey(t => t.IdTransaccion);

            // Configuración del precio de venta
            modelBuilder.Entity<Transaccion>()
                .Property(t => t.PrecioVenta)
                .HasPrecision(10, 2);

            // Cantidad de unidades compradas
            modelBuilder.Entity<Transaccion>()
                .Property(t => t.Cantidad)
                .IsRequired()
                .HasDefaultValue(1);

            // Estado de la transacción
            modelBuilder.Entity<Transaccion>()
                .Property(t => t.EstadoTransaccion)
                .HasMaxLength(20)
                .IsRequired()
                .HasDefaultValue("Registrada");

            // Confirmaciones de la compraventa
            modelBuilder.Entity<Transaccion>()
                .Property(t => t.ConfirmacionComprador)
                .HasDefaultValue(false);

            modelBuilder.Entity<Transaccion>()
                .Property(t => t.ConfirmacionVendedor)
                .HasDefaultValue(false);

            // Solicitudes de cancelación
            modelBuilder.Entity<Transaccion>()
                .Property(t => t.SolicitudCancelacionComprador)
                .HasDefaultValue(false);

            modelBuilder.Entity<Transaccion>()
                .Property(t => t.SolicitudCancelacionVendedor)
                .HasDefaultValue(false);

            // Aceptaciones de cancelación
            modelBuilder.Entity<Transaccion>()
                .Property(t => t.AceptacionCancelacionComprador)
                .HasDefaultValue(false);

            modelBuilder.Entity<Transaccion>()
                .Property(t => t.AceptacionCancelacionVendedor)
                .HasDefaultValue(false);

            // Relación Producto -> Transacciones
            modelBuilder.Entity<Transaccion>()
                .HasOne<Producto>()
                .WithMany()
                .HasForeignKey(t => t.IdProducto)
                .OnDelete(DeleteBehavior.Restrict);

            // Relación Usuario comprador -> Transacciones
            modelBuilder.Entity<Transaccion>()
                .HasOne<Usuario>()
                .WithMany()
                .HasForeignKey(t => t.IdUsuarioComprador)
                .OnDelete(DeleteBehavior.Restrict);

            // Relación Usuario vendedor -> Transacciones
            modelBuilder.Entity<Transaccion>()
                .HasOne<Usuario>()
                .WithMany()
                .HasForeignKey(t => t.IdUsuarioVendedor)
                .OnDelete(DeleteBehavior.Restrict);

            // Índice para consultar las compras y ventas de un usuario
            modelBuilder.Entity<Transaccion>()
                .HasIndex(t => new
                {
                    t.IdUsuarioComprador,
                    t.FechaTransaccion
                })
                .HasDatabaseName("IX_Transacciones_Comprador_Fecha");

            modelBuilder.Entity<Transaccion>()
                .HasIndex(t => new
                {
                    t.IdUsuarioVendedor,
                    t.FechaTransaccion
                })
                .HasDatabaseName("IX_Transacciones_Vendedor_Fecha");

            // NotificacionTransaccion
            modelBuilder.Entity<NotificacionTransaccion>()
                .ToTable("NotificacionesTransacciones", "dbo");

            modelBuilder.Entity<NotificacionTransaccion>()
                .HasKey(n => n.IdNotificacion);

            // Configuración del tipo de notificación
            modelBuilder.Entity<NotificacionTransaccion>()
                .Property(n => n.Tipo)
                .HasMaxLength(30)
                .IsRequired();

            // Configuración del mensaje
            modelBuilder.Entity<NotificacionTransaccion>()
                .Property(n => n.Mensaje)
                .HasMaxLength(250)
                .IsRequired();

            // Fecha de creación
            modelBuilder.Entity<NotificacionTransaccion>()
                .Property(n => n.FechaCreacion)
                .IsRequired()
                .HasDefaultValueSql("GETDATE()");

            // Estado de lectura
            modelBuilder.Entity<NotificacionTransaccion>()
                .Property(n => n.Leida)
                .IsRequired()
                .HasDefaultValue(false);

            // Relación Usuario -> Notificaciones
            modelBuilder.Entity<NotificacionTransaccion>()
                .HasOne<Usuario>()
                .WithMany()
                .HasForeignKey(n => n.IdUsuario)
                .OnDelete(DeleteBehavior.Restrict);

            // Relación Transacción -> Notificaciones
            modelBuilder.Entity<NotificacionTransaccion>()
                .HasOne<Transaccion>()
                .WithMany()
                .HasForeignKey(n => n.IdTransaccion)
                .OnDelete(DeleteBehavior.Restrict);

            // Índice para consultar notificaciones por usuario y lectura
            modelBuilder.Entity<NotificacionTransaccion>()
                .HasIndex(n => new
                {
                    n.IdUsuario,
                    n.Leida,
                    n.FechaCreacion
                })
                .HasDatabaseName("IX_NotificacionesTransacciones_Usuario_Leida_Fecha");

            // RefreshToken
            modelBuilder.Entity<RefreshToken>()
                .ToTable("RefreshTokens", "dbo");

            modelBuilder.Entity<RefreshToken>()
                .HasKey(r => r.IdRefreshToken);

            modelBuilder.Entity<RefreshToken>()
                .Property(r => r.TokenHash)
                .HasMaxLength(255)
                .IsUnicode(false)
                .IsRequired();

            modelBuilder.Entity<RefreshToken>()
                .Property(r => r.IdFamilia)
                .IsRequired();

            modelBuilder.Entity<RefreshToken>()
                .Property(r => r.TokenReemplazo)
                .HasMaxLength(255)
                .IsUnicode(false)
                .IsRequired(false);

            modelBuilder.Entity<RefreshToken>()
                .Property(r => r.RespuestaRenovacionCifrada)
                .HasColumnType("nvarchar(max)")
                .IsRequired(false);

            modelBuilder.Entity<RefreshToken>()
                .Property(r => r.FechaLimiteReintento)
                .IsRequired(false);

            modelBuilder.Entity<RefreshToken>()
                .Property(r => r.FechaCreacion)
                .HasDefaultValueSql("GETUTCDATE()");

            modelBuilder.Entity<RefreshToken>()
                .HasOne<Usuario>()
                .WithMany()
                .HasForeignKey(r => r.IdUsuario)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<RefreshToken>()
                .HasIndex(r => r.IdFamilia)
                .HasDatabaseName("IX_RefreshTokens_IdFamilia");

            modelBuilder.Entity<RefreshToken>()
                .HasIndex(r => r.TokenHash)
                .IsUnique()
                .HasDatabaseName("IX_RefreshTokens_TokenHash");

            modelBuilder.Entity<RefreshToken>()
                .HasIndex(r => r.IdUsuario)
                .HasDatabaseName("IX_RefreshTokens_IdUsuario");
        }
    }
}

