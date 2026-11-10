using CalidadYPrueba_T3.Models;
using CalidadYPrueba_T3.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CalidadYPrueba_T3.Data;

internal static class DatosIniciales
{
    public static void Configurar(ModelBuilder builder)
    {
        builder.Entity<IdentityRole>().HasData(
            new IdentityRole { Id = "rol-estudiante", Name = Roles.Estudiante,
                NormalizedName = "ESTUDIANTE", ConcurrencyStamp = "rol-estudiante-v1" },
            new IdentityRole { Id = "rol-encargado", Name = Roles.Encargado,
                NormalizedName = "ENCARGADO", ConcurrencyStamp = "rol-encargado-v1" });

        builder.Entity<Categoria>().HasData(
            new Categoria { Id = 1, Nombre = "Cómputo" },
            new Categoria { Id = 2, Nombre = "Electrónica" },
            new Categoria { Id = 3, Nombre = "Redes" });
    }
}
