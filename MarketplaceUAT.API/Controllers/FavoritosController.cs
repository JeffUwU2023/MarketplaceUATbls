using MarketplaceUAT.API.DTOs;
using MarketplaceUAT.Data.Context;
using MarketplaceUAT.Data.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace MarketplaceUAT.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FavoritosController : ControllerBase
    {
        private readonly MarketplaceContext _context;

        public FavoritosController(MarketplaceContext context)
        {
            _context = context;
        }

        [HttpGet]
        [Authorize]
        public async Task<ActionResult<IEnumerable<FavoritoResponseDto>>> GetFavoritos()
        {
            var claimIdUsuario = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(claimIdUsuario, out int idUsuario))
            {
                return Unauthorized();
            }

            var favoritos = await _context.Favoritos
                .Where(f => f.IdUsuario == idUsuario)
                .Select(f => new FavoritoResponseDto
                {
                    IdFavorito = f.IdFavorito,
                    IdUsuario = f.IdUsuario,
                    IdProducto = f.IdProducto,
                    FechaAgregado = f.FechaAgregado
                })
                .ToListAsync();

            return favoritos;
        }
        [HttpGet("{id}")]
        [Authorize]
        public async Task<ActionResult<FavoritoResponseDto>> GetFavorito(int id)
        {
            var claimIdUsuario = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(claimIdUsuario, out int idUsuario))
            {
                return Unauthorized();
            }

            var favorito = await _context.Favoritos
                .Where(f =>
                    f.IdFavorito == id &&
                    f.IdUsuario == idUsuario)
                .Select(f => new FavoritoResponseDto
                {
                    IdFavorito = f.IdFavorito,
                    IdUsuario = f.IdUsuario,
                    IdProducto = f.IdProducto,
                    FechaAgregado = f.FechaAgregado
                })
                .FirstOrDefaultAsync();

            if (favorito == null)
            {
                return NotFound();
            }

            return favorito;
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> ActualizarFavorito(
    int id,
    FavoritoUpdateDto dto)
        {
            var claimIdUsuario = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(claimIdUsuario, out int idUsuario))
            {
                return Unauthorized();
            }

            var favoritoExistente = await _context.Favoritos
                .FirstOrDefaultAsync(f =>
                    f.IdFavorito == id &&
                    f.IdUsuario == idUsuario);

            if (favoritoExistente == null)
            {
                return NotFound();
            }

            var usuarioExiste = await _context.Usuarios
                .AnyAsync(u => u.IdUsuario == idUsuario && u.Activo);

            if (!usuarioExiste)
            {
                return NotFound("El usuario no existe o está inactivo.");
            }

            var productoExiste = await _context.Productos
                .AnyAsync(p => p.IdProducto == dto.IdProducto && p.Activo);

            if (!productoExiste)
            {
                return NotFound("El producto no existe o está inactivo.");
            }

            var duplicado = await _context.Favoritos
                .AnyAsync(f =>
                    f.IdFavorito != id &&
                    f.IdUsuario == idUsuario &&
                    f.IdProducto == dto.IdProducto);

            if (duplicado)
            {
                return Conflict("Ese usuario ya tiene ese producto en favoritos.");
            }

            favoritoExistente.IdUsuario = idUsuario;
            favoritoExistente.IdProducto = dto.IdProducto;

            await _context.SaveChangesAsync();

            return NoContent();
        }
        [HttpPost]
        [Authorize]
        public async Task<ActionResult<FavoritoResponseDto>> CrearFavorito(
    FavoritoCreateDto dto)
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

            var productoExiste = await _context.Productos
                .AnyAsync(p => p.IdProducto == dto.IdProducto && p.Activo);

            if (!productoExiste)
            {
                return NotFound("El producto no existe o está inactivo.");
            }

            var favoritoExiste = await _context.Favoritos
                .AnyAsync(f =>
                    f.IdUsuario == idUsuario &&
                    f.IdProducto == dto.IdProducto);

            if (favoritoExiste)
            {
                return Conflict("El producto ya está en favoritos.");
            }

            var favorito = new Favorito
            {
                IdUsuario = idUsuario,
                IdProducto = dto.IdProducto,
                FechaAgregado = DateTime.Now
            };

            _context.Favoritos.Add(favorito);

            await _context.SaveChangesAsync();

            var respuesta = new FavoritoResponseDto
            {
                IdFavorito = favorito.IdFavorito,
                IdUsuario = favorito.IdUsuario,
                IdProducto = favorito.IdProducto
            };

            return CreatedAtAction(
                nameof(GetFavorito),
                new { id = favorito.IdFavorito },
                respuesta);
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> EliminarFavorito(int id)
        {
            var claimIdUsuario = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(claimIdUsuario, out int idUsuario))
            {
                return Unauthorized();
            }

            var favorito = await _context.Favoritos
                .FirstOrDefaultAsync(f =>
                    f.IdFavorito == id &&
                    f.IdUsuario == idUsuario);

            if (favorito == null)
            {
                return NotFound();
            }

            _context.Favoritos.Remove(favorito);

            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}