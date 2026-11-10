using CalidadYPrueba_T3.Data;
using CalidadYPrueba_T3.Models;
using Microsoft.AspNetCore.Identity;

namespace CalidadYPrueba_T3.Security;

public static class CrearUsuarioCommand
{
    public static async Task<int> EjecutarAsync(IServiceProvider services, IConfiguration configuration)
    {
        var datos = configuration.GetSection("AltaUsuario");
        var codigo = datos["Codigo"]?.Trim();
        var nombre = datos["Nombre"]?.Trim();
        var password = datos["Password"];
        var rol = datos["Rol"];
        if (string.IsNullOrWhiteSpace(codigo) || codigo.Length > 256 ||
            string.IsNullOrWhiteSpace(nombre) || nombre.Length > 150 ||
            string.IsNullOrEmpty(password) || password.Length > 256 ||
            rol is not (Roles.Estudiante or Roles.Encargado))
        {
            Console.Error.WriteLine("Configura AltaUsuario__Codigo, AltaUsuario__Nombre, AltaUsuario__Password y AltaUsuario__Rol (Estudiante o Encargado).");
            return 1;
        }

        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LabTrackDbContext>();
        var usuarios = scope.ServiceProvider.GetRequiredService<UserManager<Usuario>>();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        if (!await roles.RoleExistsAsync(rol))
        {
            Console.Error.WriteLine("El rol no existe. Aplica primero las migraciones de la base de datos.");
            return 1;
        }

        await using var transaction = await db.Database.BeginTransactionAsync();
        var usuario = new Usuario { UserName = codigo, NombreCompleto = nombre, LockoutEnabled = true };
        var resultado = await usuarios.CreateAsync(usuario, password);
        if (resultado.Succeeded) resultado = await usuarios.AddToRoleAsync(usuario, rol);
        if (!resultado.Succeeded)
        {
            // No registrar credenciales ni modificar cuentas existentes.
            Console.Error.WriteLine("No se pudo crear la cuenta: " +
                string.Join(", ", resultado.Errors.Select(x => x.Code)));
            return 1;
        }

        await transaction.CommitAsync();
        Console.WriteLine("Cuenta creada con el rol solicitado.");
        return 0;
    }
}
