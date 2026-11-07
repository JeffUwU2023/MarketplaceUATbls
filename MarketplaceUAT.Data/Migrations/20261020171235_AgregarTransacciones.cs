using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MarketplaceUAT.Data.Migrations
{
    /// <inheritdoc />
    public partial class AgregarTransacciones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "dbo");

            migrationBuilder.CreateTable(
                name: "Categorias",
                schema: "dbo",
                columns: table => new
                {
                    IdCategoria = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categorias", x => x.IdCategoria);
                });

            migrationBuilder.CreateTable(
                name: "Usuarios",
                schema: "dbo",
                columns: table => new
                {
                    IdUsuario = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombres = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Apellidos = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TipoUsuario = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    EsAdministrador = table.Column<bool>(type: "bit", nullable: false),
                    Matricula = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Correo = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    UrlFotoPerfil = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PasswordHash = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    FechaRegistro = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuarios", x => x.IdUsuario);
                });

            migrationBuilder.CreateTable(
                name: "Productos",
                schema: "dbo",
                columns: table => new
                {
                    IdProducto = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdUsuario = table.Column<int>(type: "int", nullable: false),
                    IdCategoria = table.Column<int>(type: "int", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Precio = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    Condicion = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    FechaPublicacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Productos", x => x.IdProducto);
                    table.ForeignKey(
                        name: "FK_Productos_Categorias_IdCategoria",
                        column: x => x.IdCategoria,
                        principalSchema: "dbo",
                        principalTable: "Categorias",
                        principalColumn: "IdCategoria",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Productos_Usuarios_IdUsuario",
                        column: x => x.IdUsuario,
                        principalSchema: "dbo",
                        principalTable: "Usuarios",
                        principalColumn: "IdUsuario",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Conversaciones",
                schema: "dbo",
                columns: table => new
                {
                    IdConversacion = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdProducto = table.Column<int>(type: "int", nullable: false),
                    IdUsuarioComprador = table.Column<int>(type: "int", nullable: false),
                    IdUsuarioVendedor = table.Column<int>(type: "int", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UltimaActividad = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Archivada = table.Column<bool>(type: "bit", nullable: false),
                    Activa = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Conversaciones", x => x.IdConversacion);
                    table.ForeignKey(
                        name: "FK_Conversaciones_Productos_IdProducto",
                        column: x => x.IdProducto,
                        principalSchema: "dbo",
                        principalTable: "Productos",
                        principalColumn: "IdProducto",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Conversaciones_Usuarios_IdUsuarioComprador",
                        column: x => x.IdUsuarioComprador,
                        principalSchema: "dbo",
                        principalTable: "Usuarios",
                        principalColumn: "IdUsuario",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Conversaciones_Usuarios_IdUsuarioVendedor",
                        column: x => x.IdUsuarioVendedor,
                        principalSchema: "dbo",
                        principalTable: "Usuarios",
                        principalColumn: "IdUsuario",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Favoritos",
                schema: "dbo",
                columns: table => new
                {
                    IdFavorito = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdUsuario = table.Column<int>(type: "int", nullable: false),
                    IdProducto = table.Column<int>(type: "int", nullable: false),
                    FechaAgregado = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Favoritos", x => x.IdFavorito);
                    table.ForeignKey(
                        name: "FK_Favoritos_Productos_IdProducto",
                        column: x => x.IdProducto,
                        principalSchema: "dbo",
                        principalTable: "Productos",
                        principalColumn: "IdProducto",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Favoritos_Usuarios_IdUsuario",
                        column: x => x.IdUsuario,
                        principalSchema: "dbo",
                        principalTable: "Usuarios",
                        principalColumn: "IdUsuario",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ImagenesProducto",
                schema: "dbo",
                columns: table => new
                {
                    IdImagen = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdProducto = table.Column<int>(type: "int", nullable: false),
                    UrlImagen = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    EsPrincipal = table.Column<bool>(type: "bit", nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImagenesProducto", x => x.IdImagen);
                    table.ForeignKey(
                        name: "FK_ImagenesProducto_Productos_IdProducto",
                        column: x => x.IdProducto,
                        principalSchema: "dbo",
                        principalTable: "Productos",
                        principalColumn: "IdProducto",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Transacciones",
                schema: "dbo",
                columns: table => new
                {
                    IdTransaccion = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdProducto = table.Column<int>(type: "int", nullable: false),
                    IdUsuarioComprador = table.Column<int>(type: "int", nullable: false),
                    IdUsuarioVendedor = table.Column<int>(type: "int", nullable: false),
                    PrecioVenta = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    FechaTransaccion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Transacciones", x => x.IdTransaccion);
                    table.ForeignKey(
                        name: "FK_Transacciones_Productos_IdProducto",
                        column: x => x.IdProducto,
                        principalSchema: "dbo",
                        principalTable: "Productos",
                        principalColumn: "IdProducto",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Transacciones_Usuarios_IdUsuarioComprador",
                        column: x => x.IdUsuarioComprador,
                        principalSchema: "dbo",
                        principalTable: "Usuarios",
                        principalColumn: "IdUsuario",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Transacciones_Usuarios_IdUsuarioVendedor",
                        column: x => x.IdUsuarioVendedor,
                        principalSchema: "dbo",
                        principalTable: "Usuarios",
                        principalColumn: "IdUsuario",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Mensajes",
                schema: "dbo",
                columns: table => new
                {
                    IdMensaje = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdConversacion = table.Column<int>(type: "int", nullable: false),
                    IdUsuario = table.Column<int>(type: "int", nullable: false),
                    Contenido = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UrlImagen = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    FechaEnvio = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Visto = table.Column<bool>(type: "bit", nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Mensajes", x => x.IdMensaje);
                    table.ForeignKey(
                        name: "FK_Mensajes_Conversaciones_IdConversacion",
                        column: x => x.IdConversacion,
                        principalSchema: "dbo",
                        principalTable: "Conversaciones",
                        principalColumn: "IdConversacion",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Mensajes_Usuarios_IdUsuario",
                        column: x => x.IdUsuario,
                        principalSchema: "dbo",
                        principalTable: "Usuarios",
                        principalColumn: "IdUsuario",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ImagenesMensaje",
                schema: "dbo",
                columns: table => new
                {
                    IdImagenMensaje = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdMensaje = table.Column<int>(type: "int", nullable: false),
                    UrlImagen = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImagenesMensaje", x => x.IdImagenMensaje);
                    table.ForeignKey(
                        name: "FK_ImagenesMensaje_Mensajes_IdMensaje",
                        column: x => x.IdMensaje,
                        principalSchema: "dbo",
                        principalTable: "Mensajes",
                        principalColumn: "IdMensaje",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Conversaciones_IdUsuarioComprador",
                schema: "dbo",
                table: "Conversaciones",
                column: "IdUsuarioComprador");

            migrationBuilder.CreateIndex(
                name: "IX_Conversaciones_IdUsuarioVendedor",
                schema: "dbo",
                table: "Conversaciones",
                column: "IdUsuarioVendedor");

            migrationBuilder.CreateIndex(
                name: "UQ_Conversaciones_ProductoCompradorVendedor",
                schema: "dbo",
                table: "Conversaciones",
                columns: new[] { "IdProducto", "IdUsuarioComprador", "IdUsuarioVendedor" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Favoritos_IdProducto",
                schema: "dbo",
                table: "Favoritos",
                column: "IdProducto");

            migrationBuilder.CreateIndex(
                name: "UQ_Favoritos_UsuarioProducto",
                schema: "dbo",
                table: "Favoritos",
                columns: new[] { "IdUsuario", "IdProducto" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ImagenesMensaje_Mensaje_Activo",
                schema: "dbo",
                table: "ImagenesMensaje",
                columns: new[] { "IdMensaje", "Activo" });

            migrationBuilder.CreateIndex(
                name: "IX_ImagenesProducto_IdProducto",
                schema: "dbo",
                table: "ImagenesProducto",
                column: "IdProducto");

            migrationBuilder.CreateIndex(
                name: "IX_Mensajes_Conversacion_Fecha",
                schema: "dbo",
                table: "Mensajes",
                columns: new[] { "IdConversacion", "FechaEnvio" });

            migrationBuilder.CreateIndex(
                name: "IX_Mensajes_IdUsuario",
                schema: "dbo",
                table: "Mensajes",
                column: "IdUsuario");

            migrationBuilder.CreateIndex(
                name: "IX_Productos_IdCategoria",
                schema: "dbo",
                table: "Productos",
                column: "IdCategoria");

            migrationBuilder.CreateIndex(
                name: "IX_Productos_IdUsuario",
                schema: "dbo",
                table: "Productos",
                column: "IdUsuario");

            migrationBuilder.CreateIndex(
                name: "IX_Transacciones_Comprador_Fecha",
                schema: "dbo",
                table: "Transacciones",
                columns: new[] { "IdUsuarioComprador", "FechaTransaccion" });

            migrationBuilder.CreateIndex(
                name: "IX_Transacciones_IdProducto",
                schema: "dbo",
                table: "Transacciones",
                column: "IdProducto");

            migrationBuilder.CreateIndex(
                name: "IX_Transacciones_Vendedor_Fecha",
                schema: "dbo",
                table: "Transacciones",
                columns: new[] { "IdUsuarioVendedor", "FechaTransaccion" });

            migrationBuilder.CreateIndex(
                name: "UQ_Usuarios_Correo",
                schema: "dbo",
                table: "Usuarios",
                column: "Correo",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Favoritos",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "ImagenesMensaje",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "ImagenesProducto",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Transacciones",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Mensajes",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Conversaciones",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Productos",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Categorias",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Usuarios",
                schema: "dbo");
        }
    }
}
