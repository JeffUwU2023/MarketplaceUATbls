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
                .Where(p => p.Activo)
                .Select(p => new ProductoResponseDto
                {
                    IdProducto = p.IdProducto,
                    IdUsuario = p.IdUsuario,
                    IdCategoria = p.IdCategoria,
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

            var producto = new Producto
            {
                IdUsuario = idUsuario,
                IdCategoria = dto.IdCategoria,
                Nombre = dto.Nombre,
                Descripcion = dto.Descripcion,
                Precio = dto.Precio,
                Condicion = dto.Condicion,
                Estado = dto.Estado,
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
            var claimIdUsuario = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

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
                return NotFound(); 
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
            productoExistente.IdUsuario = idUsuario;
            productoExistente.IdCategoria = dto.IdCategoria; 
            productoExistente.Nombre = dto.Nombre; 
            productoExistente.Descripcion = dto.Descripcion; 
            productoExistente.Precio = dto.Precio; 
            productoExistente.Condicion = dto.Condicion; 
            productoExistente.Estado = dto.Estado; 
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