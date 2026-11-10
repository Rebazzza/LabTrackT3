using CalidadYPrueba_T3.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CalidadYPrueba_T3.Data;

public class LabTrackDbContext(DbContextOptions<LabTrackDbContext> options)
    : IdentityDbContext<Usuario>(options)
{
    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<Equipo> Equipos => Set<Equipo>();
    public DbSet<Reserva> Reservas => Set<Reserva>();
    public DbSet<Penalizacion> Penalizaciones => Set<Penalizacion>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(LabTrackDbContext).Assembly);
        DatosIniciales.Configurar(builder);
    }
}
