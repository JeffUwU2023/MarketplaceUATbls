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
                .AsNoTracking()
                .Where(c => c.Activo)
                .OrderBy(c => c.Nombre)
                .Select(c => new CategoriaResponseDto
                {
                    IdCategoria = c.IdCategoria,
                    Nombre = c.Nombre,
                    Descripcion = c.Descripcion,
                    Activo = c.Activo
                })
                .ToListAsync();

            return Ok(categorias);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<CategoriaResponseDto>> GetCategoria(int id)
        {
            if (id <= 0)
            {
                return BadRequest(
                    "El identificador de la categoría no es válido.");
            }

            var categoria = await _context.Categorias
                .AsNoTracking()
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

            return Ok(categoria);
        }

        [HttpPost]
        [Authorize(Roles = "Administrador")]
        public async Task<ActionResult<CategoriaResponseDto>> CrearCategoria(
            [FromBody] CategoriaCreateDto dto)
        {
            if (dto == null)
            {
                return BadRequest("Los datos de la categoría son obligatorios.");
            }

            string nombre = dto.Nombre?.Trim() ?? string.Empty;
            string? descripcion = string.IsNullOrWhiteSpace(dto.Descripcion)
                ? null
                : dto.Descripcion.Trim();

            if (nombre.Length < 3 || nombre.Length > 30)
            {
                return BadRequest(
                    "El nombre debe tener entre 3 y 30 caracteres.");
            }

            if (descripcion?.Length > 200)
            {
                return BadRequest(
                    "La descripción no puede superar los 200 caracteres.");
            }

            var nombreExiste = await _context.Categorias
                .AnyAsync(c =>
                    c.Nombre == nombre &&
                    c.Activo);

            if (nombreExiste)
            {
                return Conflict("La categoría ya está registrada.");
            }

            var categoria = new Categoria
            {
                Nombre = nombre,
                Descripcion = descripcion,
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
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> ActualizarCategoria(
            int id,
            [FromBody] CategoriaUpdateDto dto)
        {
            if (id <= 0)
            {
                return BadRequest(
                    "El identificador de la categoría no es válido.");
            }

            if (dto == null)
            {
                return BadRequest(
                    "Los datos de la categoría son obligatorios.");
            }

            string nombre = dto.Nombre?.Trim() ?? string.Empty;
            string? descripcion = string.IsNullOrWhiteSpace(dto.Descripcion)
                ? null
                : dto.Descripcion.Trim();

            if (nombre.Length < 3 || nombre.Length > 30)
            {
                return BadRequest(
                    "El nombre debe tener entre 3 y 30 caracteres.");
            }

            if (descripcion?.Length > 200)
            {
                return BadRequest(
                    "La descripción no puede superar los 200 caracteres.");
            }

            var categoriaExistente = await _context.Categorias
                .FirstOrDefaultAsync(c =>
                    c.IdCategoria == id &&
                    c.Activo);

            if (categoriaExistente == null)
            {
                return NotFound();
            }

            var nombreExiste = await _context.Categorias
                .AnyAsync(c =>
                    c.IdCategoria != id &&
                    c.Nombre == nombre &&
                    c.Activo);

            if (nombreExiste)
            {
                return Conflict("La categoría ya está registrada.");
            }

            categoriaExistente.Nombre = nombre;
            categoriaExistente.Descripcion = descripcion;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> EliminarCategoria(int id)
        {
            if (id <= 0)
            {
                return BadRequest(
                    "El identificador de la categoría no es válido.");
            }

            var categoria = await _context.Categorias
                .FirstOrDefaultAsync(c =>
                    c.IdCategoria == id &&
                    c.Activo);

            if (categoria == null)
            {
                return NotFound();
            }

            var tieneProductosAsociados = await _context.Productos
                .AnyAsync(p => p.IdCategoria == id);

            if (tieneProductosAsociados)
            {
                return Conflict(
                    "No puedes eliminar esta categoría porque tiene productos asociados.");
            }

            categoria.Activo = false;

            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
