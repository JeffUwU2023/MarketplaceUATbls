using MarketplaceUAT.API.DTOs;
using MarketplaceUAT.Data.Context;
using MarketplaceUAT.Data.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace MarketplaceUAT.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ConversacionesController : ControllerBase
    {
        private readonly MarketplaceContext _context;

        public ConversacionesController(MarketplaceContext context)
        {
            _context = context;
        }

        [HttpGet("Usuario/{idUsuario}")]
        [Authorize]
        public IActionResult GetConversacionesUsuario(int idUsuario)
        {
            var claimIdUsuario = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(claimIdUsuario, out int idUsuarioAutenticado))
            {
                return Unauthorized();
            }
            var conversaciones = _context.Conversaciones
                .Where(c =>
                       c.IdUsuarioComprador == idUsuarioAutenticado ||
                  c.IdUsuarioVendedor == idUsuarioAutenticado)
                .OrderByDescending(c => c.UltimaActividad)
                .Select(c => new ConversacionResponseDto
                {
                    IdConversacion = c.IdConversacion,
                    IdProducto = c.IdProducto,

                    NombreProducto = _context.Productos
                        .Where(p => p.IdProducto == c.IdProducto)
                        .Select(p => p.Nombre)
                        .FirstOrDefault() ?? "Producto no disponible",

                    NombreOtroUsuario = _context.Usuarios
                           .Where(u =>
                                  u.IdUsuario ==
                                 (c.IdUsuarioComprador == idUsuarioAutenticado
                                ? c.IdUsuarioVendedor
                                : c.IdUsuarioComprador))
                        .Select(u => u.Nombres + " " + u.Apellidos)
                        .FirstOrDefault() ?? "Usuario",

                    IdUsuarioComprador = c.IdUsuarioComprador,
                    IdUsuarioVendedor = c.IdUsuarioVendedor,
                    FechaCreacion = c.FechaCreacion,
                    UltimaActividad = c.UltimaActividad,
                    Archivada = c.Archivada,
                    Activa = c.Activa
                })
                .ToList();

            return Ok(conversaciones);
        }


        [HttpGet("Producto/{idProducto}")]
        [Authorize]
        public IActionResult GetConversacionPorProducto(int idProducto)
        {
            var claimIdUsuario = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(claimIdUsuario, out int idUsuarioAutenticado))
            {
                return Unauthorized();
            }

            var producto = _context.Productos
                .FirstOrDefault(p => p.IdProducto == idProducto);

            if (producto == null)
            {
                return NotFound("No se encontró el producto.");
            }

            if (producto.IdUsuario == idUsuarioAutenticado)
            {
                return BadRequest("No puedes consultar una conversación contigo mismo.");
            }

            int idUsuarioVendedor = producto.IdUsuario;

            var conversacion = _context.Conversaciones
                .Where(c =>
                    c.IdProducto == idProducto &&
                    c.IdUsuarioComprador == idUsuarioAutenticado &&
                    c.IdUsuarioVendedor == idUsuarioVendedor)
                .OrderByDescending(c => c.UltimaActividad)
                .Select(c => new ConversacionResponseDto
                {
                    IdConversacion = c.IdConversacion,
                    IdProducto = c.IdProducto,
                    NombreProducto = _context.Productos
                        .Where(p => p.IdProducto == c.IdProducto)
                        .Select(p => p.Nombre)
                        .FirstOrDefault() ?? "Producto no disponible",
                    NombreOtroUsuario = _context.Usuarios
                        .Where(u => u.IdUsuario == c.IdUsuarioVendedor)
                        .Select(u => u.Nombres + " " + u.Apellidos)
                        .FirstOrDefault() ?? "Usuario",
                    IdUsuarioComprador = c.IdUsuarioComprador,
                    IdUsuarioVendedor = c.IdUsuarioVendedor,
                    FechaCreacion = c.FechaCreacion,
                    UltimaActividad = c.UltimaActividad,
                    Archivada = c.Archivada,
                    Activa = c.Activa
                })
                .FirstOrDefault();

            if (conversacion == null)
            {
                return NoContent();
            }

            return Ok(conversacion);
        }

        [HttpPost]
        [Authorize]
        public IActionResult CrearConversacion(ConversacionCreateDto dto)
        {
            var claimIdUsuario = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(claimIdUsuario, out int idUsuarioComprador))
            {
                return Unauthorized();
            }

            var producto = _context.Productos
                .FirstOrDefault(p => p.IdProducto == dto.IdProducto);

            if (producto == null || !producto.Activo)
            {
                return BadRequest(
                    "El producto no existe o ya no está disponible.");
            }

            if (producto.IdUsuario == idUsuarioComprador)
            {
                return BadRequest(
                    "No puedes iniciar una conversación contigo mismo.");
            }

            var idUsuarioVendedor = producto.IdUsuario;

            
            var conversacionExistente = _context.Conversaciones
               .FirstOrDefault(c =>
              c.IdProducto == dto.IdProducto &&
              c.IdUsuarioComprador == idUsuarioComprador &&
              c.IdUsuarioVendedor == idUsuarioVendedor);

            if (conversacionExistente != null)
            {
                return Ok(conversacionExistente);
            }

            var conversacion = new Conversacion
            {
                IdProducto = dto.IdProducto,
                IdUsuarioComprador = idUsuarioComprador,
                IdUsuarioVendedor = idUsuarioVendedor,
                FechaCreacion = DateTime.Now,
                UltimaActividad = DateTime.Now,
                Archivada = false,
                Activa = true
            };

            _context.Conversaciones.Add(conversacion);
            _context.SaveChanges();

            return Ok(conversacion);
        }
    }
}