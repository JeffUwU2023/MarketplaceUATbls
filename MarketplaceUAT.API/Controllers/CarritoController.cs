using MarketplaceUAT.API.DTOs;
using MarketplaceUAT.Data.Context;
using MarketplaceUAT.Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace MarketplaceUAT.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class CarritoController : ControllerBase
    {
        private readonly MarketplaceContext _context;

        public CarritoController(MarketplaceContext context)
        {
            _context = context;
        }

        private bool ObtenerIdUsuario(out int idUsuario)
        {
            var claimId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            return int.TryParse(claimId, out idUsuario);
        }

        // GET: api/Carrito
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CarritoResponseDto>>> GetCarrito()
        {
            if (!ObtenerIdUsuario(out int idUsuario))
                return Unauthorized();

            var carrito = await _context.Carritos
                .Where(c => c.IdUsuario == idUsuario)
                .Join(
                    _context.Productos,
                    c => c.IdProducto,
                    p => p.IdProducto,
                    (c, p) => new { c, p })
                .Join(
                    _context.Usuarios,
                    x => x.p.IdUsuario,
                    u => u.IdUsuario,
                    (x, u) => new { x.c, x.p, u })
                .Select(x => new CarritoResponseDto
                {
                    IdCarrito = x.c.IdCarrito,
                    IdProducto = x.p.IdProducto,
                    IdUsuarioVendedor = x.p.IdUsuario,
                    NombreVendedor = (x.u.Nombres + " " + x.u.Apellidos).Trim(),
                    Nombre = x.p.Nombre,
                    Precio = x.p.Precio,
                    Condicion = x.p.Condicion,
                    Estado = x.p.Estado,
                    Activo = x.p.Activo,
                    EsProductoUnico = x.p.EsProductoUnico,
                    CantidadDisponible = x.p.CantidadDisponible,
                    Cantidad = x.c.Cantidad,
                    FechaAgregado = x.c.FechaAgregado
                })
                .OrderByDescending(c => c.FechaAgregado)
                .ToListAsync();

            return Ok(carrito);
        }

        // POST: api/Carrito
        [HttpPost]
        public async Task<ActionResult<CarritoResponseDto>> AgregarAlCarrito(
            CarritoCreateDto dto)
        {
            if (!ObtenerIdUsuario(out int idUsuario))
                return Unauthorized();

            var usuarioExiste = await _context.Usuarios
                .AnyAsync(u => u.IdUsuario == idUsuario && u.Activo);

            if (!usuarioExiste)
                return NotFound("El usuario no existe o está inactivo.");

            var producto = await _context.Productos
                .FirstOrDefaultAsync(p =>
                    p.IdProducto == dto.IdProducto &&
                    p.Activo &&
                    p.Estado != "Eliminado");

            if (producto == null)
                return NotFound("El producto no existe o está inactivo.");

            if (producto.IdUsuario == idUsuario)
                return BadRequest("No puedes agregar tu propia publicación al carrito.");

            if (producto.Estado != "Disponible" ||
                producto.CantidadDisponible <= 0)
            {
                return Conflict("El producto no está disponible.");
            }

            if (producto.EsProductoUnico && dto.Cantidad != 1)
            {
                return BadRequest(
                    "Los productos únicos solo permiten una unidad.");
            }

            if (dto.Cantidad > producto.CantidadDisponible)
            {
                return Conflict(
                    $"Solo hay {producto.CantidadDisponible} unidades disponibles.");
            }

            var registro = await _context.Carritos
                .FirstOrDefaultAsync(c =>
                    c.IdUsuario == idUsuario &&
                    c.IdProducto == dto.IdProducto);

            if (registro == null)
            {
                registro = new Carrito
                {
                    IdUsuario = idUsuario,
                    IdProducto = dto.IdProducto,
                    Cantidad = dto.Cantidad,
                    FechaAgregado = DateTime.Now
                };

                _context.Carritos.Add(registro);
            }
            else
            {
                // La cantidad enviada representa la cantidad deseada,
                // no unidades adicionales.
                registro.Cantidad = dto.Cantidad;
            }

            await _context.SaveChangesAsync();


            var vendedor = await _context.Usuarios
                .Where(u => u.IdUsuario == producto.IdUsuario)
                .Select(u => new
                {
                    u.Nombres,
                    u.Apellidos
                })
                .FirstOrDefaultAsync();

            var respuesta = new CarritoResponseDto
            {
                IdCarrito = registro.IdCarrito,
                IdProducto = producto.IdProducto,
                IdUsuarioVendedor = producto.IdUsuario,
                NombreVendedor = vendedor == null
                    ? string.Empty
                    : (vendedor.Nombres + " " + vendedor.Apellidos).Trim(),
                Nombre = producto.Nombre,
                Precio = producto.Precio,
                Condicion = producto.Condicion,
                Estado = producto.Estado,
                Activo = producto.Activo,
                EsProductoUnico = producto.EsProductoUnico,
                CantidadDisponible = producto.CantidadDisponible,
                Cantidad = registro.Cantidad,
                FechaAgregado = registro.FechaAgregado
            };

            return Ok(respuesta);
        }

        // PUT: api/Carrito/{id}/cantidad?cantidad=2
        [HttpPut("{id}/cantidad")]
        public async Task<IActionResult> ActualizarCantidad(
            int id,
            [FromQuery] int cantidad)
        {
            if (!ObtenerIdUsuario(out int idUsuario))
                return Unauthorized();

            if (cantidad < 1)
                return BadRequest("La cantidad debe ser al menos una unidad.");

            var registro = await _context.Carritos
                .FirstOrDefaultAsync(c =>
                    c.IdCarrito == id &&
                    c.IdUsuario == idUsuario);

            if (registro == null)
                return NotFound("El producto no está en tu carrito.");

            var producto = await _context.Productos
                .FirstOrDefaultAsync(p =>
                    p.IdProducto == registro.IdProducto &&
                    p.Activo &&
                    p.Estado != "Eliminado");

            if (producto == null)
                return Conflict("La publicación ya no está disponible.");

            if (producto.Estado != "Disponible" ||
                producto.CantidadDisponible <= 0)
            {
                return Conflict("El producto no está disponible.");
            }

            if (producto.EsProductoUnico && cantidad != 1)
            {
                return BadRequest(
                    "Los productos únicos solo permiten una unidad.");
            }

            if (cantidad > producto.CantidadDisponible)
            {
                return Conflict(
                    $"Solo hay {producto.CantidadDisponible} unidades disponibles.");
            }

            registro.Cantidad = cantidad;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/Carrito/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> EliminarDelCarrito(int id)
        {
            if (!ObtenerIdUsuario(out int idUsuario))
                return Unauthorized();

            var registro = await _context.Carritos
                .FirstOrDefaultAsync(c =>
                    c.IdCarrito == id &&
                    c.IdUsuario == idUsuario);

            if (registro == null)
                return NotFound("El producto no está en tu carrito.");

            _context.Carritos.Remove(registro);

            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
