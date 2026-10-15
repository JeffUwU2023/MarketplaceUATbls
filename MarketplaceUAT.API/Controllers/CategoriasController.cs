using MarketplaceUAT.API.DTOs;
using MarketplaceUAT.Data.Context;
using MarketplaceUAT.Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MarketplaceUAT.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoriasController : ControllerBase
    {
        private readonly MarketplaceContext _context;

        public CategoriasController(MarketplaceContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<CategoriaResponseDto>>> GetCategorias()
        {
            var categorias = await _context.Categorias
                .Where(c => c.Activo)
                .Select(c => new CategoriaResponseDto
                {
                    IdCategoria = c.IdCategoria,
                    Nombre = c.Nombre,
                    Descripcion = c.Descripcion,
                    Activo = c.Activo
                })
                .ToListAsync();

            return categorias;
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<CategoriaResponseDto>> GetCategoria(int id)
        {
            var categoria = await _context.Categorias
                .Where(c => c.IdCategoria == id && c.Activo)
                .Select(c => new CategoriaResponseDto
                {
                    IdCategoria = c.IdCategoria,
                    Nombre = c.Nombre,
                    Descripcion = c.Descripcion,
                    Activo = c.Activo
                })
                .FirstOrDefaultAsync();

            if (categoria == null)
            {
                return NotFound();
            }

            return categoria;
        }

        [HttpPost]
        [Authorize]
        public async Task<ActionResult<CategoriaResponseDto>> CrearCategoria(
            CategoriaCreateDto dto)
        {
            var nombreExiste = await _context.Categorias
            .AnyAsync(c =>
             c.Nombre == dto.Nombre &&
             c.Activo);

            if (nombreExiste)
            {
                return Conflict("La categoría ya está registrada.");
            }
            var categoria = new Categoria
            {
                Nombre = dto.Nombre,
                Descripcion = dto.Descripcion,
                Activo = true
            };

            _context.Categorias.Add(categoria);

            await _context.SaveChangesAsync();

            var respuesta = new CategoriaResponseDto
            {
                IdCategoria = categoria.IdCategoria,
                Nombre = categoria.Nombre,
                Descripcion = categoria.Descripcion,
                Activo = categoria.Activo
            };

            return CreatedAtAction(
                nameof(GetCategoria),
                new { id = categoria.IdCategoria },
                respuesta);
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> ActualizarCategoria(
            int id,
            CategoriaUpdateDto dto)
        {
            var categoriaExistente = await _context.Categorias
                .FirstOrDefaultAsync(c => c.IdCategoria == id && c.Activo);

            if (categoriaExistente == null)
            {
                return NotFound();
            }

            var nombreExiste = await _context.Categorias
               .AnyAsync(c =>
              c.IdCategoria != id &&
              c.Nombre == dto.Nombre &&
              c.Activo);

            if (nombreExiste)
            {
                return Conflict("La categoría ya está registrada.");
            }

            categoriaExistente.Nombre = dto.Nombre;
            categoriaExistente.Descripcion = dto.Descripcion;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> EliminarCategoria(int id)
        {
            var categoria = await _context.Categorias
                .FirstOrDefaultAsync(c => c.IdCategoria == id && c.Activo);

            if (categoria == null)
            {
                return NotFound();
            }

            categoria.Activo = false;

            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
