using MarketplaceUAT.API.DTOs;
using MarketplaceUAT.Data.Context;
using MarketplaceUAT.Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using System.Security.Claims;

namespace MarketplaceUAT.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TransaccionesController : ControllerBase
    {
        private readonly MarketplaceContext _context;

        public TransaccionesController(MarketplaceContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> RegistrarVenta(
            [FromBody] TransaccionCreateDto dto)
        {
            var claimIdUsuario =
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(claimIdUsuario, out int idVendedor) ||
                idVendedor <= 0)
            {
                return Unauthorized();
            }

            if (dto == null)
            {
                return BadRequest("Los datos de la transacción son obligatorios.");
            }

            if (dto.IdProducto <= 0 || dto.IdUsuarioComprador <= 0)
            {
                return BadRequest(
                    "El producto y el comprador deben tener identificadores válidos.");
            }

            var producto = await _context.Productos
                .FirstOrDefaultAsync(p =>
                    p.IdProducto == dto.IdProducto);

            if (producto == null)
            {
                return NotFound("No se encontró el producto.");
            }

            if (producto.IdUsuario != idVendedor)
            {
                return Forbid();
            }

            if (!producto.Activo ||
                producto.Estado != "Disponible")
            {
                return BadRequest("Esta publicación ya no está disponible.");
            }

            if (dto.Cantidad < 1)
            {
                return BadRequest(
                    "La cantidad debe ser de al menos una unidad.");
            }

            if (producto.EsProductoUnico && dto.Cantidad != 1)
            {
                return BadRequest(
                    "Este artículo es único y solo se puede comprar una unidad.");
            }

            if (dto.Cantidad > producto.CantidadDisponible)
            {
                return BadRequest(
                    $"Solo hay {producto.CantidadDisponible} unidades disponibles.");
            }

            if (dto.IdUsuarioComprador == idVendedor)
            {
                return BadRequest(
                    "No puedes registrarte como comprador de tu propio producto.");
            }

            var compradorExiste = await _context.Usuarios
                .AnyAsync(u =>
                    u.IdUsuario == dto.IdUsuarioComprador &&
                    u.Activo);

            if (!compradorExiste)
            {
                return BadRequest(
                    "El comprador no existe o está inactivo.");
            }

            var tuvoConversacion = await _context.Conversaciones
                .AnyAsync(c =>
                    c.IdProducto == dto.IdProducto &&
                    c.IdUsuarioVendedor == idVendedor &&
                    c.IdUsuarioComprador == dto.IdUsuarioComprador &&
                    c.Activa &&
                    !c.Archivada);

            if (!tuvoConversacion)
            {
                return BadRequest(
                    "El comprador debe haber conversado contigo sobre esta publicación.");
            }

            await using var transaccionDb =
                await _context.Database.BeginTransactionAsync();

            var venta = new Transaccion
            {
                IdProducto = producto.IdProducto,
                IdUsuarioComprador = dto.IdUsuarioComprador,
                IdUsuarioVendedor = idVendedor,
                PrecioVenta = producto.Precio,
                Cantidad = dto.Cantidad,
                FechaTransaccion = DateTime.Now,
                EstadoTransaccion = "Registrada",
                ConfirmacionComprador = false,
                ConfirmacionVendedor = false,
                SolicitudCancelacionComprador = false,
                SolicitudCancelacionVendedor = false,
                AceptacionCancelacionComprador = false,
                AceptacionCancelacionVendedor = false
            };

            producto.CantidadDisponible -= dto.Cantidad;

            if (producto.CantidadDisponible == 0)
            {
                producto.Estado = "Vendido";
            }

            _context.Transacciones.Add(venta);

            try
            {
                // 1. Guardar la venta y obtener el ID generado por SQL Server.
                await _context.SaveChangesAsync();

                // 2. Crear la notificación usando el ID real de la transacción.
                var notificacion = new NotificacionTransaccion
                {
                    IdUsuario = dto.IdUsuarioComprador,
                    IdTransaccion = venta.IdTransaccion,
                    Tipo = "SolicitudCompra",
                    Mensaje =
                        $"Tienes una nueva solicitud de compra para {producto.Nombre}.",
                    FechaCreacion = DateTime.Now,
                    Leida = false
                };

                _context.NotificacionesTransacciones.Add(notificacion);

                var notificacionVendedor = new NotificacionTransaccion
                {
                    IdUsuario = idVendedor,
                    IdTransaccion = venta.IdTransaccion,
                    Tipo = "NuevaSolicitudVenta",
                    Mensaje =
        $"Tienes una nueva solicitud de compra de {producto.Nombre}.",
                    FechaCreacion = DateTime.Now,
                    Leida = false
                };

                _context.NotificacionesTransacciones.Add(notificacionVendedor);

                // 3. Guardar la notificación antes de confirmar la transacción.
                await _context.SaveChangesAsync();

                // 4. Confirmar ambos cambios en la base de datos.
                await transaccionDb.CommitAsync();
            }
            catch (DbUpdateException ex)
                when (ex.InnerException is SqlException sqlEx &&
                      (sqlEx.Number == 2601 || sqlEx.Number == 2627))
            {
                await transaccionDb.RollbackAsync();

                return Conflict(
                    "Este producto ya tiene una transacción registrada. " +
                    "Actualiza la información e inténtalo nuevamente.");
            }
            catch (DbUpdateConcurrencyException)
            {
                await transaccionDb.RollbackAsync();

                return Conflict(
                    "El inventario fue modificado por otra operación. " +
                    "Actualiza la información e inténtalo nuevamente.");
            }

            return Ok(new
            {
                mensaje = "La venta se registró correctamente.",
                idTransaccion = venta.IdTransaccion,
                idProducto = venta.IdProducto,
                idUsuarioComprador = venta.IdUsuarioComprador,
                idUsuarioVendedor = venta.IdUsuarioVendedor,
                precioVenta = venta.PrecioVenta,
                cantidad = venta.Cantidad,
                cantidadDisponible = producto.CantidadDisponible,
                fechaTransaccion = venta.FechaTransaccion
            });
        }

        [HttpGet("MisCompras")]
        public async Task<IActionResult> ObtenerMisCompras()
        {
            var claimIdUsuario =
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(claimIdUsuario, out int idUsuario))
                return Unauthorized();

            var compras = await _context.Transacciones
               .AsNoTracking()
               .Where(t => t.IdUsuarioComprador == idUsuario)
               .OrderByDescending(t => t.FechaTransaccion)
               .Select(t => new
                {
                    t.IdTransaccion,
                    t.IdProducto,
                    NombreProducto = _context.Productos
                        .Where(p => p.IdProducto == t.IdProducto)
                        .Select(p => p.Nombre)
                        .FirstOrDefault() ?? "Producto no disponible",
                    t.IdUsuarioComprador,
                    t.IdUsuarioVendedor,
                    NombreVendedor = _context.Usuarios
                        .Where(u => u.IdUsuario == t.IdUsuarioVendedor)
                        .Select(u => u.Nombres + " " + u.Apellidos)
                        .FirstOrDefault() ?? "Usuario",
                    t.PrecioVenta,
                    t.FechaTransaccion,
                    EstadoTransaccion = t.EstadoTransaccion,
                    t.ConfirmacionComprador,
                    t.ConfirmacionVendedor,
                    t.SolicitudCancelacionComprador,
                    t.SolicitudCancelacionVendedor,
                    t.AceptacionCancelacionComprador,
                    t.AceptacionCancelacionVendedor
                })
                .ToListAsync();

            return Ok(compras);
        }

        [HttpGet("MisVentas")]
        public async Task<IActionResult> ObtenerMisVentas()
        {
            var claimIdUsuario =
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(claimIdUsuario, out int idUsuario))
                return Unauthorized();

            var ventas = await _context.Transacciones
                .AsNoTracking()
                .Where(t => t.IdUsuarioVendedor == idUsuario)
                .OrderByDescending(t => t.FechaTransaccion)
                .Select(t => new
                {
                    t.IdTransaccion,
                    t.IdProducto,
                    NombreProducto = _context.Productos
                        .Where(p => p.IdProducto == t.IdProducto)
                        .Select(p => p.Nombre)
                        .FirstOrDefault() ?? "Producto no disponible",
                    t.IdUsuarioComprador,
                    t.IdUsuarioVendedor,
                    NombreComprador = _context.Usuarios
                        .Where(u => u.IdUsuario == t.IdUsuarioComprador)
                        .Select(u => u.Nombres + " " + u.Apellidos)
                        .FirstOrDefault() ?? "Usuario",
                    t.PrecioVenta,
                    t.FechaTransaccion,
                    EstadoTransaccion = t.EstadoTransaccion,
                    t.ConfirmacionComprador,
                    t.ConfirmacionVendedor,
                    t.SolicitudCancelacionComprador,
                    t.SolicitudCancelacionVendedor,
                    t.AceptacionCancelacionComprador,
                    t.AceptacionCancelacionVendedor
                })
                .ToListAsync();

            return Ok(ventas);
        }

        [HttpGet("Compradores/{idProducto}")]
        public async Task<IActionResult> ObtenerCompradores(int idProducto)
        {
            if (idProducto <= 0)
                return BadRequest("El identificador del producto no es válido.");

            var claimIdUsuario =
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(claimIdUsuario, out int idVendedor))
                return Unauthorized();

            var producto = await _context.Productos
    .AsNoTracking()
    .FirstOrDefaultAsync(p =>
        p.IdProducto == idProducto &&
        p.Activo);

            if (producto == null)
                return NotFound("No se encontró el producto.");

            if (producto.IdUsuario != idVendedor)
                return Forbid();

            var compradores = await _context.Conversaciones
                .Where(c =>
                    c.IdProducto == idProducto &&
                    c.IdUsuarioVendedor == idVendedor &&
                    c.Activa &&
                    !c.Archivada)
                .Select(c => c.IdUsuarioComprador)
                .Distinct()
                .Join(
                    _context.Usuarios.Where(u => u.Activo),
                    idComprador => idComprador,
                    usuario => usuario.IdUsuario,
                    (idComprador, usuario) => new
                    {
                        IdUsuario = usuario.IdUsuario,
                        Nombre = usuario.Nombres + " " + usuario.Apellidos
                    })
                .OrderBy(u => u.Nombre)
                .ToListAsync();

            return Ok(compradores);
        }

        [HttpPut("{idTransaccion}/Confirmar")]
        public async Task<IActionResult> ConfirmarTransaccion(int idTransaccion)
        {
            var claimIdUsuario =
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(claimIdUsuario, out int idUsuario) ||
                idUsuario <= 0)
            {
                return Unauthorized();
            }

            var transaccion = await _context.Transacciones
                .FirstOrDefaultAsync(t => t.IdTransaccion == idTransaccion);

            if (transaccion == null)
            {
                return NotFound("No se encontró la transacción.");
            }

            if (transaccion.EstadoTransaccion != "Registrada")
            {
                return BadRequest(
                    "Solo puedes confirmar una transacción que esté registrada.");
            }

            if (transaccion.SolicitudCancelacionComprador ||
                transaccion.SolicitudCancelacionVendedor)
            {
                return Conflict(
                    "No puedes confirmar la compraventa porque existe una solicitud de cancelación pendiente.");
            }

            int idUsuarioNotificado;
            string mensajeNotificacion;

            if (transaccion.IdUsuarioComprador == idUsuario)
            {
                if (transaccion.ConfirmacionComprador)
                {
                    return BadRequest(
                        "Ya confirmaste esta compraventa.");
                }

                transaccion.ConfirmacionComprador = true;

                idUsuarioNotificado = transaccion.IdUsuarioVendedor;
                mensajeNotificacion =
                    "El comprador confirmó la compraventa. Confirma tú también para completarla.";
            }
            else if (transaccion.IdUsuarioVendedor == idUsuario)
            {
                if (transaccion.ConfirmacionVendedor)
                {
                    return BadRequest(
                        "Ya confirmaste esta compraventa.");
                }

                transaccion.ConfirmacionVendedor = true;

                idUsuarioNotificado = transaccion.IdUsuarioComprador;
                mensajeNotificacion =
                    "El vendedor confirmó la compraventa. Confirma tú también para completarla.";
            }
            else
            {
                return Forbid();
            }

            bool compraventaCompletada =
                transaccion.ConfirmacionComprador &&
                transaccion.ConfirmacionVendedor;

            if (compraventaCompletada)
            {
                transaccion.EstadoTransaccion = "Completada";

                var conversacion = await _context.Conversaciones
                    .FirstOrDefaultAsync(c =>
                        c.IdProducto == transaccion.IdProducto &&
                        c.IdUsuarioComprador == transaccion.IdUsuarioComprador &&
                        c.IdUsuarioVendedor == transaccion.IdUsuarioVendedor &&
                        c.Activa &&
                        !c.Archivada);

                if (conversacion != null)
                {
                    conversacion.Archivada = true;
                    conversacion.Activa = false;
                }

                // Ocultar los avisos pendientes de esta transacción.
                var notificacionesPendientes =
                    await _context.NotificacionesTransacciones
                        .Where(n =>
                            n.IdTransaccion == transaccion.IdTransaccion &&
                            !n.Leida)
                        .ToListAsync();

                foreach (var notificacionPendiente in notificacionesPendientes)
                {
                    notificacionPendiente.Leida = true;
                }
            }
            else
            {
                // Avisar a la otra persona que debe confirmar.
                var notificacion = new NotificacionTransaccion
                {
                    IdUsuario = idUsuarioNotificado,
                    IdTransaccion = transaccion.IdTransaccion,
                    Tipo = "ConfirmacionCompraventa",
                    Mensaje = mensajeNotificacion,
                    FechaCreacion = DateTime.Now,
                    Leida = false
                };

                _context.NotificacionesTransacciones.Add(notificacion);
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                return Conflict(
                    "La transacción fue modificada por otra solicitud. Actualiza su estado e inténtalo nuevamente.");
            }

            return Ok(new
            {
                mensaje = compraventaCompletada
                    ? "La compraventa fue confirmada por ambas partes."
                    : "Tu confirmación se registró correctamente. Falta la confirmación de la otra parte.",
                idTransaccion = transaccion.IdTransaccion,
                estadoTransaccion = transaccion.EstadoTransaccion,
                confirmacionComprador = transaccion.ConfirmacionComprador,
                confirmacionVendedor = transaccion.ConfirmacionVendedor
            });
        }

        [HttpPut("{idTransaccion}/SolicitarCancelacion")]
        public async Task<IActionResult> SolicitarCancelacion(int idTransaccion)
        {
            if (idTransaccion <= 0)
            {
                return BadRequest(
                    "El identificador de la transacción no es válido.");
            }

            var claimIdUsuario =
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(claimIdUsuario, out int idUsuario) ||
                idUsuario <= 0)
            {
                return Unauthorized();
            }

            var transaccion = await _context.Transacciones
                .FirstOrDefaultAsync(t =>
                    t.IdTransaccion == idTransaccion);

            if (transaccion == null)
            {
                return NotFound("No se encontró la transacción.");
            }

            if (transaccion.EstadoTransaccion != "Registrada")
            {
                return BadRequest(
                    "Solo puedes solicitar la cancelación de una transacción registrada.");
            }

            if (transaccion.SolicitudCancelacionComprador ||
                transaccion.SolicitudCancelacionVendedor)
            {
                return Conflict(
                    "Ya existe una solicitud de cancelación para esta transacción. Espera la respuesta de la otra parte.");
            }

            int idUsuarioNotificado;
            string mensajeNotificacion;

            if (transaccion.IdUsuarioComprador == idUsuario)
            {
                transaccion.SolicitudCancelacionComprador = true;

                idUsuarioNotificado = transaccion.IdUsuarioVendedor;
                mensajeNotificacion =
                    "El comprador ha solicitado cancelar la compraventa. Revisa la solicitud y responde.";
            }
            else if (transaccion.IdUsuarioVendedor == idUsuario)
            {
                transaccion.SolicitudCancelacionVendedor = true;

                idUsuarioNotificado = transaccion.IdUsuarioComprador;
                mensajeNotificacion =
                    "El vendedor ha solicitado cancelar la compraventa. Revisa la solicitud y responde.";
            }
            else
            {
                return Forbid();
            }

            var notificacion = new NotificacionTransaccion
            {
                IdUsuario = idUsuarioNotificado,
                IdTransaccion = transaccion.IdTransaccion,
                Tipo = "Cancelacion",
                Mensaje = mensajeNotificacion,
                FechaCreacion = DateTime.Now,
                Leida = false
            };

            _context.NotificacionesTransacciones.Add(notificacion);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                return Conflict(
                    "La transacción fue modificada por otra solicitud. Actualiza su estado e inténtalo nuevamente.");
            }

            return Ok(new
            {
                mensaje = "La solicitud de cancelación se registró. Falta la respuesta de la otra parte.",
                idTransaccion = transaccion.IdTransaccion,
                estadoTransaccion = transaccion.EstadoTransaccion,
                solicitudCancelacionComprador = transaccion.SolicitudCancelacionComprador,
                solicitudCancelacionVendedor = transaccion.SolicitudCancelacionVendedor
            });
        }

        [HttpPut("{idTransaccion}/AceptarCancelacion")]
        public async Task<IActionResult> AceptarCancelacion(int idTransaccion)
        {
            if (idTransaccion <= 0)
                return BadRequest("El identificador de la transacción no es válido.");

            var claimIdUsuario =
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(claimIdUsuario, out int idUsuario))
                return Unauthorized();

            var transaccion = await _context.Transacciones
                .FirstOrDefaultAsync(t => t.IdTransaccion == idTransaccion);

            if (transaccion == null)
                return NotFound("No se encontró la transacción.");

            if (transaccion.EstadoTransaccion != "Registrada")
            {
                return BadRequest(
                    "Solo puedes cancelar una transacción que esté registrada.");
            }

            if (transaccion.IdUsuarioComprador == idUsuario)
            {
                if (!transaccion.SolicitudCancelacionVendedor)
                {
                    return BadRequest(
                        "El vendedor no ha solicitado cancelar esta transacción.");
                }

                transaccion.AceptacionCancelacionComprador = true;
            }
            else if (transaccion.IdUsuarioVendedor == idUsuario)
            {
                if (!transaccion.SolicitudCancelacionComprador)
                {
                    return BadRequest(
                        "El comprador no ha solicitado cancelar esta transacción.");
                }

                transaccion.AceptacionCancelacionVendedor = true;
            }
            else
            {
                return Forbid();
            }

            bool cancelacionAceptada =
                (transaccion.SolicitudCancelacionComprador &&
                 transaccion.AceptacionCancelacionVendedor) ||
                (transaccion.SolicitudCancelacionVendedor &&
                 transaccion.AceptacionCancelacionComprador);

            if (cancelacionAceptada)
            {
                transaccion.EstadoTransaccion = "Cancelada";

                var producto = await _context.Productos
                    .FirstOrDefaultAsync(p =>
                        p.IdProducto == transaccion.IdProducto);

                if (producto != null && producto.Activo)
                {
                    producto.CantidadDisponible += transaccion.Cantidad;

                    if (producto.Estado == "Vendido")
                    {
                        producto.Estado = "Disponible";
                    }
                }

                var notificacionesPendientes =
                    await _context.NotificacionesTransacciones
                        .Where(n =>
                            n.IdTransaccion == transaccion.IdTransaccion &&
                            !n.Leida)
                        .ToListAsync();

                foreach (var notificacion in notificacionesPendientes)
                {
                    notificacion.Leida = true;
                }
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                return Conflict(
                    "La transacción fue modificada por otra solicitud. Actualiza su estado e inténtalo nuevamente.");
            }

            return Ok(new
            {
                mensaje = transaccion.EstadoTransaccion == "Cancelada"
                    ? "La cancelación fue aceptada por ambas partes. La publicación vuelve a estar disponible."
                    : "Aceptaste la solicitud de cancelación. Falta la respuesta de la otra parte.",
                idTransaccion = transaccion.IdTransaccion,
                estadoTransaccion = transaccion.EstadoTransaccion,
                aceptacionCancelacionComprador = transaccion.AceptacionCancelacionComprador,
                aceptacionCancelacionVendedor = transaccion.AceptacionCancelacionVendedor
            });
        }
    }
}
