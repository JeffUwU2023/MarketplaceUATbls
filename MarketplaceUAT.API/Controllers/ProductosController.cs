using MarketplaceUAT.API.DTOs;
using MarketplaceUAT.Data.Context;
using MarketplaceUAT.Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace MarketplaceUAT.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductosController : ControllerBase
    {
        private readonly MarketplaceContext _context;

        public ProductosController(MarketplaceContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ProductoResponseDto>>> GetProductos()
        {
            var productos = await _context.Productos
                .Where(p => p.Activo && p.Estado != "Vendido")
                .Select(p => new ProductoResponseDto
                {
                    IdProducto = p.IdProducto,
                    IdUsuario = p.IdUsuario,
                    IdCategoria = p.IdCategoria,
                    EsProductoUnico = p.EsProductoUnico,
                    CantidadDisponible = p.CantidadDisponible,
                    Nombre = p.Nombre,
                    Descripcion = p.Descripcion,
                    Precio = p.Precio,
                    Condicion = p.Condicion,
                    Estado = p.Estado,
                    FechaPublicacion = p.FechaPublicacion,
                    Activo = p.Activo
                })
                .ToListAsync();

            return Ok(productos);
        }

        [HttpGet("Usuario/{idUsuario}")]
        public async Task<ActionResult<IEnumerable<ProductoResponseDto>>>
            GetProductosPorUsuario(int idUsuario)
        {
            if (idUsuario <= 0)
            {
                return BadRequest("El identificador del usuario no es válido.");
            }

            var usuarioExiste = await _context.Usuarios
                .AnyAsync(u => u.IdUsuario == idUsuario && u.Activo);

            if (!usuarioExiste)
            {
                return NotFound("El usuario no existe o no está disponible.");
            }

            var productos = await _context.Productos
                .Where(p =>
                    p.IdUsuario == idUsuario &&
                    p.Activo &&
                    (p.Estado == "Disponible" ||
                     p.Estado == "Reservado"))
                .OrderByDescending(p => p.FechaPublicacion)
                .Select(p => new ProductoResponseDto
                {
                    IdProducto = p.IdProducto,
                    IdUsuario = p.IdUsuario,
                    IdCategoria = p.IdCategoria,
                    EsProductoUnico = p.EsProductoUnico,
                    CantidadDisponible = p.CantidadDisponible,
                    Nombre = p.Nombre,
                    Descripcion = p.Descripcion,
                    Precio = p.Precio,
                    Condicion = p.Condicion,
                    Estado = p.Estado,
                    FechaPublicacion = p.FechaPublicacion,
                    Activo = p.Activo
                })
                .ToListAsync();

            return Ok(productos);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ProductoResponseDto>> GetProducto(int id)
        {
            var producto = await _context.Productos
                .Where(p => p.IdProducto == id && p.Activo)
                .Select(p => new ProductoResponseDto
                {
                    IdProducto = p.IdProducto,
                    IdUsuario = p.IdUsuario,
                    IdCategoria = p.IdCategoria,
                    EsProductoUnico = p.EsProductoUnico,
                    CantidadDisponible = p.CantidadDisponible,
                    Nombre = p.Nombre,
                    Descripcion = p.Descripcion,
                    Precio = p.Precio,
                    Condicion = p.Condicion,
                    Estado = p.Estado,
                    FechaPublicacion = p.FechaPublicacion,
                    Activo = p.Activo
                })
                .FirstOrDefaultAsync();

            if (producto == null)
            {
                return NotFound();
            }

            return Ok(producto);
        }

        [HttpGet("DetalleFavorito/{id}")]
        [Authorize]
        public async Task<ActionResult<ProductoResponseDto>> GetProductoDetalleFavorito(int id)
        {
            var claimIdUsuario = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(claimIdUsuario, out int idUsuario))
            {
                return Unauthorized();
            }

            var producto = await _context.Productos
                .FirstOrDefaultAsync(p => p.IdProducto == id);

            if (producto == null)
            {
                return NotFound("El producto no existe.");
            }

            if (producto.Estado == "Eliminado" &&
                producto.Activo)
            {
                return NotFound("La publicación ya no está disponible.");
            }

            if (!producto.Activo ||
    producto.Estado == "Vendido" ||
    producto.Estado == "Eliminado")
            {
                bool esVendedor = producto.IdUsuario == idUsuario;

                bool esFavorito = await _context.Favoritos
                    .AnyAsync(f =>
                        f.IdUsuario == idUsuario &&
                        f.IdProducto == id);

                if (!esVendedor && !esFavorito)
                {
                    return NotFound(
                        "No tienes permiso para consultar esta publicación.");
                }
            }

            var respuesta = new ProductoResponseDto
            {
                IdProducto = producto.IdProducto,
                IdUsuario = producto.IdUsuario,
                IdCategoria = producto.IdCategoria,
                EsProductoUnico = producto.EsProductoUnico,
                CantidadDisponible = producto.CantidadDisponible,
                Nombre = producto.Nombre,
                Descripcion = producto.Descripcion,
                Precio = producto.Precio,
                Condicion = producto.Condicion,
                Estado = producto.Estado,
                FechaPublicacion = producto.FechaPublicacion,
                Activo = producto.Activo
            };

            return Ok(respuesta);
        }

        [HttpPost]
        [Authorize]
        public async Task<ActionResult<ProductoResponseDto>> CrearProducto 
            (ProductoCreateDto dto)
        {
            var claimIdUsuario = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(claimIdUsuario, out int idUsuario))
            {
                return Unauthorized();
            }

            var usuarioExiste = await _context.Usuarios
                .AnyAsync(u => u.IdUsuario == idUsuario && u.Activo);

            if (!usuarioExiste)
            {
                return NotFound("El usuario no existe o está inactivo.");
            }
            var categoriaExiste = await _context.Categorias
    .AnyAsync(c => c.IdCategoria == dto.IdCategoria && c.Activo);

            if (!categoriaExiste)
            {
                return NotFound("La categoría no existe o está inactiva.");
            }

            // Validar el tipo de producto y las existencias iniciales.
            if (dto.EsProductoUnico && dto.CantidadDisponible != 1)
            {
                return BadRequest(
                    "Un artículo único debe tener exactamente una unidad disponible.");
            }

            if (dto.CantidadDisponible < 1)
            {
                return BadRequest(
                    "La cantidad disponible debe ser de al menos una unidad.");
            }

            var producto = new Producto
            {
                IdUsuario = idUsuario,
                IdCategoria = dto.IdCategoria,
                Nombre = dto.Nombre,
                Descripcion = dto.Descripcion,
                Precio = dto.Precio,
                Condicion = dto.Condicion,

                EsProductoUnico = dto.EsProductoUnico,
                CantidadDisponible = dto.CantidadDisponible,

                Estado = "Disponible",
                FechaPublicacion = DateTime.Now,
                Activo = true
            };
            _context.Productos.Add(producto);

            await _context.SaveChangesAsync();

            var respuesta = new ProductoResponseDto
            {
                IdProducto = producto.IdProducto,
                IdUsuario = producto.IdUsuario,
                IdCategoria = producto.IdCategoria,
                EsProductoUnico = producto.EsProductoUnico,
                CantidadDisponible = producto.CantidadDisponible,
                Nombre = producto.Nombre,
                Descripcion = producto.Descripcion,
                Precio = producto.Precio,
                Condicion = producto.Condicion,
                Estado = producto.Estado,
                FechaPublicacion = producto.FechaPublicacion,
                Activo = producto.Activo
            };

            return CreatedAtAction(
                nameof(GetProducto),
                new { id = producto.IdProducto },
                respuesta); 
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> ActualizarProducto(
            int id,
            ProductoUpdateDto dto)
        {
            var claimIdUsuario =
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(claimIdUsuario, out int idUsuario))
            {
                return Unauthorized();
            }

            var productoExistente = await _context.Productos
    .FirstOrDefaultAsync(p =>
        p.IdProducto == id &&
        p.IdUsuario == idUsuario &&
        p.Activo);

            if (productoExistente == null)
            {
                return NotFound(
                    "No se encontró la publicación o no te pertenece.");
            }

            var usuarioExiste = await _context.Usuarios
                .AnyAsync(u =>
                    u.IdUsuario == idUsuario &&
                    u.Activo);

            if (!usuarioExiste)
            {
                return NotFound(
                    "El usuario no existe o está inactivo.");
            }

            var categoriaExiste = await _context.Categorias
                .AnyAsync(c =>
                    c.IdCategoria == dto.IdCategoria &&
                    c.Activo);

            if (!categoriaExiste)
            {
                return NotFound(
                    "La categoría no existe o está inactiva.");
            }

            var yaExisteVenta = await _context.Transacciones
    .AnyAsync(t =>
        t.IdProducto == id &&
        t.EstadoTransaccion == "Registrada");

            if (yaExisteVenta)
            {
                return Conflict(
                    "No puedes editar una publicación que ya tiene una venta registrada.");
            }

            if (!productoExistente.Activo ||
                productoExistente.Estado == "Vendido" ||
                productoExistente.Estado == "Eliminado")
            {
                return BadRequest(
                    "Esta publicación no se puede editar porque no está disponible.");
            }

            if (dto.CantidadDisponible < 1)
            {
                return BadRequest(
                    "La cantidad disponible debe ser de al menos una unidad.");
            }

            if (dto.EsProductoUnico && dto.CantidadDisponible != 1)
            {
                return BadRequest(
                    "Un artículo único debe tener exactamente una unidad disponible.");
            }

            productoExistente.IdCategoria = dto.IdCategoria;
            productoExistente.EsProductoUnico = dto.EsProductoUnico;
            productoExistente.CantidadDisponible = dto.CantidadDisponible;
            productoExistente.Nombre = dto.Nombre;
            productoExistente.Descripcion = dto.Descripcion;
            productoExistente.Precio = dto.Precio;
            productoExistente.Condicion = dto.Condicion;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpPut("{id}/Disponible")]
        [Authorize]
        public async Task<IActionResult> MarcarComoDisponible(int id)
        {
            var claimIdUsuario =
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(claimIdUsuario, out int idUsuario))
            {
                return Unauthorized();
            }

            var producto = await _context.Productos
    .FirstOrDefaultAsync(p =>
        p.IdProducto == id &&
        p.IdUsuario == idUsuario &&
        p.Activo);

            if (producto == null)
            {
                return NotFound(
                    "No se encontró la publicación o no te pertenece.");
            }

            if (producto.Estado != "Reservado")
            {
                return BadRequest(
                    "Esta acción solo se permite para publicaciones reservadas.");
            }

            var yaExisteVenta = await _context.Transacciones
    .AnyAsync(t =>
        t.IdProducto == id &&
        t.EstadoTransaccion == "Registrada");

            if (yaExisteVenta)
            {
                return Conflict(
                    "No puedes volver a activar esta publicación porque ya tiene una venta registrada.");
            }

            producto.Estado = "Disponible";

            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpPut("{id}/Reactivar")]
        [Authorize]
        public async Task<IActionResult> ReactivarPublicacion(int id)
        {
            var claimIdUsuario =
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(claimIdUsuario, out int idUsuario))
            {
                return Unauthorized();
            }

            var producto = await _context.Productos
    .FirstOrDefaultAsync(p =>
        p.IdProducto == id &&
        p.IdUsuario == idUsuario &&
        p.Activo);

            if (producto == null)
            {
                return NotFound(
                    "No se encontró la publicación o no te pertenece.");
            }

            if (producto.Estado != "Vendido")
            {
                return BadRequest(
                    "Solo puedes reactivar publicaciones vendidas.");
            }

            var transaccionPendiente = await _context.Transacciones
    .AnyAsync(t =>
        t.IdProducto == id &&
        t.EstadoTransaccion == "Registrada");

            if (transaccionPendiente)
            {
                return Conflict(
                    "No puedes reactivar esta publicación porque tiene una compraventa pendiente.");
            }

            var tieneVentaCompletada = await _context.Transacciones
                .AnyAsync(t =>
                    t.IdProducto == id &&
                    t.EstadoTransaccion == "Completada");

            if (!tieneVentaCompletada)
            {
                return Conflict(
                    "Solo puedes reactivar una publicación que tenga al menos una compraventa completada.");
            }

            producto.Estado = "Disponible";
            producto.Activo = true;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("{id}/BorrarPublicacionVendida")]
        [Authorize]
        public async Task<IActionResult> BorrarPublicacionVendida(int id)
        {
            var claimIdUsuario =
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(claimIdUsuario, out int idUsuario))
            {
                return Unauthorized();
            }

            var producto = await _context.Productos
    .FirstOrDefaultAsync(p =>
        p.IdProducto == id &&
        p.IdUsuario == idUsuario &&
        p.Activo);

            if (producto == null)
            {
                return NotFound(
                    "No se encontró la publicación o no te pertenece.");
            }

            if (producto.Estado != "Vendido")
            {
                return BadRequest(
                    "Esta acción solo se permite para publicaciones vendidas.");
            }

            var transaccionPendiente = await _context.Transacciones
    .AnyAsync(t =>
        t.IdProducto == id &&
        t.EstadoTransaccion == "Registrada");

            if (transaccionPendiente)
            {
                return Conflict(
                    "No puedes retirar esta publicación mientras tenga una compraventa pendiente.");
            }

            // Retirar definitivamente la publicación de la aplicación.
            // Se conserva el registro para no perder la transacción.
            producto.Estado = "Eliminado";
            producto.Activo = false;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> EliminarProducto(int id)
        {
            var claimIdUsuario = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(claimIdUsuario, out int idUsuario))
            {
                return Unauthorized();
            }
            var producto = await _context.Productos
               .FirstOrDefaultAsync(p =>
                p.IdProducto == id &&
                p.IdUsuario == idUsuario &&
                p.Activo);

            if (producto == null)
            {
                return NotFound();
            }

            producto.Activo = false;

            var conversaciones = await _context.Conversaciones
                .Where(c => c.IdProducto == id && c.Activa)
                .ToListAsync();

            foreach (var conversacion in conversaciones)
            {
                conversacion.Archivada = true;
                conversacion.Activa = false;
            }

            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}