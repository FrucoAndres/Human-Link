﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Human_Link_Web.Server.Models;
using Human_Link_Web.Server.Custom;
using Microsoft.AspNetCore.Authorization;
using System.Transactions;

namespace Human_Link_Web.Server.Controllers
{
    [Route("HumanLink/[controller]")]
    [ApiController]
    public class UsuarioController : ControllerBase
    {
        private readonly HumanLinkContext _context;
        private readonly PasswordHasher _passwordHasher;

        public UsuarioController(HumanLinkContext context, PasswordHasher passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;
        }
        //Endpoint para obtener todos los usuarios y sus datos
        // GET: HumanLink/Usuario
        [HttpGet]
        [Authorize(Policy = "AdminPolicy")] // Solo permite el consumo del endpoint a los usuarios logeados y con rol administrador
        public async Task<ActionResult<IEnumerable<Usuario>>> GetUsuarios()
        {
            return await _context.Usuarios.ToListAsync();
        }


        //Endpoint para obtener el usuario mediante ID
        //Cambiar a uso restringido del JWT solamente del administrador
        // GET: HumanLink/Usuario/5
        [HttpGet("{id}")]
        [Authorize(Policy = "AdminPolicy")] // Solo permite el consumo del endpoint a los usuarios logeados y con rol administrador
        public async Task<ActionResult<Usuario>> GetUsuario(int id)
        {
            var usuario = await _context.Usuarios.FindAsync(id);

            if (usuario == null)
            {
                return NotFound();
            }

            return usuario;
        }


        //Endpoint para actualizar la información del Usuario
        //Aquí tambien se encripta la contraseña antes de ser añadida a la base de datos
        //Cambiar a uso restringido del JWT solamente del propio usuario y admin, recomendación cambiar a PATCH
        // PUT: HumanLink/Usuario/id
        [HttpPut("{id}")]
        [Authorize(Policy = "AdminPolicy")]
        public async Task<IActionResult> PutUsuario(int id, [FromBody] Usuario usuario)
        {
            // Verifica si la clave está vacía
            if (string.IsNullOrWhiteSpace(usuario.Clave))
            {
                return BadRequest("La clave no puede estar vacía.");
            }

            // Verifica que el ID coincida entre la URL y el cuerpo del request
            if (id != usuario.Idusuario)
            {
                return BadRequest("El ID del usuario en la URL no coincide con el ID proporcionado en el cuerpo.");
            }

            // Encuentra el usuario existente en la base de datos
            var usuarioExistente = await _context.Usuarios.FindAsync(id);
            if (usuarioExistente == null)
            {
                return NotFound("Usuario no encontrado.");
            }

            // Verificar las propiedades antes de la actualización
            Console.WriteLine($"Recibiendo Usuario1: {usuario.Usuario1}, Correo: {usuario.Correo}");
            Console.WriteLine($"Usuario Existente - Usuario1: {usuarioExistente.Usuario1}, Correo: {usuarioExistente.Correo}");

            // Aquí agregamos el hash de la clave
            usuarioExistente.Clave = _passwordHasher.Hash(usuario.Clave);

            // Actualizamos los campos que se pueden modificar
            usuarioExistente.Usuario1 = usuario.Usuario1;
            usuarioExistente.Correo = usuario.Correo;
            usuarioExistente.Isadmin = usuario.Isadmin;
            usuarioExistente.Isemailverified = usuario.Isemailverified;

            // Actualizamos el estado de la entidad para que se guarden los cambios
            _context.Entry(usuarioExistente).State = EntityState.Modified;

            try
            {
                // Guardamos los cambios en la base de datos
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                if (!UsuarioExists(id))
                {
                    return NotFound("El usuario ya no existe.");
                }
                else
                {
                    throw;
                }
            }

            // Retorna un estado 204 No Content si la operación fue exitosa
            return NoContent();
        }


        //Endpoint para añadir usuarios a la base de datos
        //Aquí tambien se encripta la contraseña antes de ser añadida a la base de datos
        // POST: HumanLink/Usuario
        [HttpPost]
        [Authorize(Policy = "AdminPolicy")]
        public async Task<ActionResult<Usuario>> PostUsuario([FromBody] Usuario usuario)
        {
            using var transaction = _context.Database.BeginTransaction();

            try
            {
                // Validar que el usuario no sea nulo
                if (usuario == null)
                {
                    return BadRequest("El usuario no puede ser nulo.");
                }

                // Verificar si la contraseña está presente y no es vacía
                if (string.IsNullOrEmpty(usuario.Clave))
                {
                    return BadRequest("La contraseña no puede estar vacía.");
                }

                // Hash de la contraseña
                usuario.Clave = _passwordHasher.Hash(usuario.Clave);

                // Guardar el usuario
                _context.Usuarios.Add(usuario);
                await _context.SaveChangesAsync();

                // Si hay empleados asociados, actualizar su referencia
                if (usuario.Empleados != null && usuario.Empleados.Any())
                {
                    foreach (var empleado in usuario.Empleados)
                    {
                        var empleadoExistente = await _context.Empleados.FindAsync(empleado.Idempleado);
                        if (empleadoExistente != null)
                        {
                            empleadoExistente.EmpleadoUsuario = usuario.Idusuario;
                            _context.Entry(empleadoExistente).State = EntityState.Modified;
                        }
                    }
                    await _context.SaveChangesAsync();
                }

                await transaction.CommitAsync();
                return CreatedAtAction("GetUsuario", new { id = usuario.Idusuario }, usuario);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, "Error interno del servidor al procesar la solicitud: " + ex.Message);
            }
        }


        //EnvPoint para encriptar las claves de la base de datos --BORRAR CUANDO TERMINE EL PROCESO DE DESARROLLO, ES SÓLO PARA AHORRAR TRABAJO--
        private const int MAX_RETRY_ATTEMPTS = 3;
        private const int RETRY_DELAY_MS = 1000; // 1 segundo entre reintentos
        [HttpPost("encriptar-claves")]
        [Authorize(Policy = "AdminPolicy")]
        public async Task<ActionResult> EncryptExistingPasswords()
        {
            int usuariosActualizados = 0;
            var errores = new List<string>();
            int intentosConexion = 0;

            while (intentosConexion < MAX_RETRY_ATTEMPTS)
            {
                try
                {
                    // Verificar la conexión
                    if (!await _context.Database.CanConnectAsync())
                    {
                        throw new Exception("No se puede establecer conexión con la base de datos");
                    }

                    // Obtener usuarios en bloques de 10 para evitar sobrecarga
                    var usuarios = await _context.Usuarios
                        .Where(u => !u.Clave.Contains(";") || u.Clave.Length < 20)
                        .ToListAsync();

                    // Procesar en bloques pequeños
                    foreach (var usuario in usuarios)
                    {
                        int intentosUsuario = 0;
                        bool procesoExitoso = false;

                        while (intentosUsuario < MAX_RETRY_ATTEMPTS && !procesoExitoso)
                        {
                            try
                            {
                                // Crear un nuevo contexto para cada operación
                                using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
                                {
                                    // Recargar el usuario
                                    var usuarioActual = await _context.Usuarios
                                        .FirstOrDefaultAsync(u => u.Idusuario == usuario.Idusuario);

                                    if (usuarioActual != null)
                                    {
                                        // Cifrar la clave
                                        var claveOriginal = usuarioActual.Clave;
                                        usuarioActual.Clave = _passwordHasher.Hash(claveOriginal);

                                        await _context.SaveChangesAsync();
                                        scope.Complete();
                                        usuariosActualizados++;
                                        procesoExitoso = true;
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                intentosUsuario++;
                                if (intentosUsuario >= MAX_RETRY_ATTEMPTS)
                                {
                                    errores.Add($"Error al procesar usuario {usuario.Idusuario} después de {MAX_RETRY_ATTEMPTS} intentos: {ex.Message}");
                                }
                                await Task.Delay(RETRY_DELAY_MS);
                            }
                        }
                    }

                    // Si llegamos aquí, el proceso fue exitoso
                    return Ok(new
                    {
                        mensaje = $"Proceso completado. {usuariosActualizados} usuarios actualizados.",
                        totalUsuarios = usuarios.Count,
                        usuariosActualizados,
                        errores = errores.Any() ? errores : null
                    });
                }
                catch (Exception ex)
                {
                    intentosConexion++;
                    if (intentosConexion >= MAX_RETRY_ATTEMPTS)
                    {
                        return StatusCode(500, new
                        {
                            mensaje = "Error al procesar el cifrado masivo de claves",
                            error = ex.Message,
                            detalleError = ex.InnerException?.Message,
                            intentosRealizados = intentosConexion
                        });
                    }
                    await Task.Delay(RETRY_DELAY_MS);
                }
            }

            return StatusCode(500, new
            {
                mensaje = "No se pudo completar el proceso después de múltiples intentos",
                intentosRealizados = intentosConexion
            });
        }


        // Endpoint para eliminar un usuario de la base de datos
        // DELETE: HumanLink/Usuario/id
        [HttpDelete("{id}")]
        [Authorize(Policy = "AdminPolicy")]
        public async Task<IActionResult> DeleteUsuario(int id)
        {
            try
            {
                Console.WriteLine($"Recibida solicitud para eliminar el usuario con ID: {id}");

                var usuario = await _context.Usuarios.FindAsync(id);

                if (usuario == null)
                {
                    Console.WriteLine($"No se encontró el usuario con ID: {id}");
                    return NotFound();
                }

                // Verificar si hay dependencias que puedan causar el error
                Console.WriteLine($"Eliminando usuario con ID: {id}...");
                _context.Usuarios.Remove(usuario);
                await _context.SaveChangesAsync();

                Console.WriteLine($"Usuario con ID: {id} eliminado exitosamente.");
                return NoContent();
            }
            catch (Exception ex)
            {
                // Detallar información adicional en el catch
                Console.WriteLine($"Error al eliminar el usuario con ID: {id}. Excepción: {ex.Message}");
                Console.WriteLine($"Pila de la excepción: {ex.StackTrace}");

                // Si es un error de base de datos (posibles problemas de referencia)
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Detalles internos del error: {ex.InnerException.Message}");
                }

                return StatusCode(500, $"Error interno del servidor: {ex.Message}");
            }
        }

        private bool UsuarioExists(int id)
        {
            return _context.Usuarios.Any(e => e.Idusuario == id);
        }
    }
}
