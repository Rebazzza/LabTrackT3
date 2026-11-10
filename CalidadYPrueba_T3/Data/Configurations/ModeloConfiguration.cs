using CalidadYPrueba_T3.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CalidadYPrueba_T3.Data.Configurations;

public class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> builder)
    {
        builder.Property(x => x.NombreCompleto).HasMaxLength(150).IsRequired();
        builder.Property(x => x.UserName).IsRequired();
        builder.Property(x => x.NormalizedUserName).IsRequired();
        builder.Property(x => x.Estado).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.Version).IsRowVersion();
        builder.ToTable("AspNetUsers", table => table.HasCheckConstraint(
            "CK_Usuarios_Estado", "[Estado] IN ('Activo', 'Penalizado')"));
    }
}

public class CategoriaConfiguration : IEntityTypeConfiguration<Categoria>
{
    public void Configure(EntityTypeBuilder<Categoria> builder)
    {
        builder.Property(x => x.Nombre).HasMaxLength(50).IsRequired();
        builder.HasIndex(x => x.Nombre).IsUnique();
    }
}

public class EquipoConfiguration : IEntityTypeConfiguration<Equipo>
{
    public void Configure(EntityTypeBuilder<Equipo> builder)
    {
        builder.Property(x => x.Nombre).HasMaxLength(150).IsRequired();
        builder.Property(x => x.CodigoInventario).HasMaxLength(30).IsRequired();
        builder.HasIndex(x => x.CodigoInventario).IsUnique();
        builder.Property(x => x.Version).IsRowVersion();
        builder.HasOne(x => x.Categoria).WithMany(x => x.Equipos)
            .HasForeignKey(x => x.CategoriaId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class ReservaConfiguration : IEntityTypeConfiguration<Reserva>
{
    public void Configure(EntityTypeBuilder<Reserva> builder)
    {
        builder.Property(x => x.Estado).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.MotivoRechazo).HasMaxLength(500);
        builder.Property(x => x.Version).IsRowVersion();
        builder.HasOne(x => x.Estudiante).WithMany(x => x.Reservas)
            .HasForeignKey(x => x.EstudianteId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Equipo).WithMany(x => x.Reservas)
            .HasForeignKey(x => x.EquipoId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ResueltaPor).WithMany()
            .HasForeignKey(x => x.ResueltaPorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.DevolucionRegistradaPor).WithMany()
            .HasForeignKey(x => x.DevolucionRegistradaPorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.EquipoId, x.Estado, x.Inicio, x.Fin });
        builder.HasIndex(x => new { x.EstudianteId, x.Estado, x.Inicio, x.Fin });
        builder.HasIndex(x => new { x.Estado, x.CreadaEn });
        builder.ToTable("Reservas", table =>
        {
            table.HasCheckConstraint("CK_Reservas_Horario", "[Inicio] < [Fin]");
            table.HasCheckConstraint("CK_Reservas_Estado",
                "[Estado] IN ('Pendiente', 'Aprobada', 'Rechazada', 'Devuelta')");
            table.HasCheckConstraint("CK_Reservas_MotivoRechazo",
                "[Estado] <> 'Rechazada' OR ([MotivoRechazo] IS NOT NULL AND LEN(LTRIM(RTRIM([MotivoRechazo]))) > 0)");
            table.HasCheckConstraint("CK_Reservas_Resolucion",
                "([Estado] = 'Pendiente' AND [ResueltaPorId] IS NULL AND [ResueltaEn] IS NULL) OR " +
                "([Estado] <> 'Pendiente' AND [ResueltaPorId] IS NOT NULL AND [ResueltaEn] IS NOT NULL)");
            table.HasCheckConstraint("CK_Reservas_Devolucion",
                "([Estado] = 'Devuelta' AND [DevueltaEn] IS NOT NULL AND [DevolucionRegistradaPorId] IS NOT NULL) OR " +
                "([Estado] <> 'Devuelta' AND [DevueltaEn] IS NULL AND [DevolucionRegistradaPorId] IS NULL)");
        });
    }
}

public class PenalizacionConfiguration : IEntityTypeConfiguration<Penalizacion>
{
    public void Configure(EntityTypeBuilder<Penalizacion> builder)
    {
        builder.Property(x => x.Motivo).HasMaxLength(500).IsRequired();
        builder.HasOne(x => x.Reserva).WithOne(x => x.Penalizacion)
            .HasForeignKey<Penalizacion>(x => x.ReservaId).OnDelete(DeleteBehavior.Restrict);
        builder.ToTable("Penalizaciones", table => table.HasCheckConstraint(
            "CK_Penalizaciones_Motivo", "LEN(LTRIM(RTRIM([Motivo]))) > 0"));
    }
}
