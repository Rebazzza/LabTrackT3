using System.Data;
using CalidadYPrueba_T3.Data;
using CalidadYPrueba_T3.Models;
using CalidadYPrueba_T3.ViewModels;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace CalidadYPrueba_T3.Services;

public record ResultadoSolicitud(int? ReservaId, string? Error);

public class ReservaService(LabTrackDbContext db, TimeProvider reloj)
{
    public static readonly TimeSpan DesfaseLima = TimeSpan.FromHours(-5);
    private const int PorPagina = 10;

    public DateOnly Hoy => DateOnly.FromDateTime(reloj.GetUtcNow().ToOffset(DesfaseLima).DateTime);

    public async Task<bool> PrepararFormularioAsync(CrearReservaViewModel model, CancellationToken ct)
    {
        var ahora = reloj.GetUtcNow();
        model.Hoy = Hoy;
        model.Equipo = await db.Equipos.AsNoTracking().Where(x => x.Id == model.EquipoId)
            .Select(x => new EquipoReservaInfo(x.Id, x.Nombre, x.CodigoInventario, x.Categoria.Nombre,
                x.Reservas.Any(r => r.Estado == EstadoReserva.Aprobada && r.Inicio <= ahora)))
            .SingleOrDefaultAsync(ct);
        if (model.Equipo is null) return false;
        model.HorariosAprobados = await db.Reservas.AsNoTracking()
            .Where(x => x.EquipoId == model.EquipoId && x.Estado == EstadoReserva.Aprobada && x.Fin > ahora)
            .OrderBy(x => x.Inicio).Take(10).Select(x => new HorarioAprobado(x.Inicio, x.Fin)).ToListAsync(ct);
        return true;
    }

    public async Task<ResultadoSolicitud> SolicitarAsync(string estudianteId, CrearReservaViewModel model, CancellationToken ct)
    {
        if (model.Fecha is null || model.HoraInicio is null || model.HoraFin is null)
            return new(null, "Completa la fecha y las horas de uso.");
        DateTimeOffset inicio, fin;
        try
        {
            inicio = new DateTimeOffset(model.Fecha.Value.ToDateTime(model.HoraInicio.Value), DesfaseLima).ToUniversalTime();
            fin = new DateTimeOffset(model.Fecha.Value.ToDateTime(model.HoraFin.Value), DesfaseLima).ToUniversalTime();
        }
        catch (ArgumentOutOfRangeException)
        {
            return new(null, "La fecha seleccionada no es válida.");
        }
        if (inicio >= fin) return new(null, "La hora de fin debe ser posterior a la hora de inicio en la misma fecha.");
        var ahora = reloj.GetUtcNow();
        if (inicio <= ahora) return new(null, "La reserva debe comenzar en una fecha y hora futuras.");

        try
        {
            // Las lecturas de intervalos y el alta se mantienen en una misma transacción.
            // Serializable impide que dos solicitudes simultáneas del estudiante eludan el límite.
            await using var transaccion = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
            if (!await db.Users.AnyAsync(x => x.Id == estudianteId, ct))
                return new(null, "No se encontró la cuenta del estudiante.");
            if (!await db.Equipos.AnyAsync(x => x.Id == model.EquipoId, ct))
                return new(null, "El equipo seleccionado ya no existe.");
            if (await db.Reservas.AnyAsync(x => x.EquipoId == model.EquipoId &&
                x.Estado == EstadoReserva.Aprobada && x.Inicio <= ahora, ct))
                return new(null, "El equipo está prestado y aún no se ha registrado su devolución.");
            if (await db.Reservas.AnyAsync(x => x.EquipoId == model.EquipoId &&
                x.Estado == EstadoReserva.Aprobada && x.Inicio < fin && inicio < x.Fin, ct))
                return new(null, "El equipo ya tiene una reserva aprobada que se cruza con ese horario.");
            if (await db.Reservas.AnyAsync(x => x.EstudianteId == estudianteId &&
                (x.Estado == EstadoReserva.Pendiente || x.Estado == EstadoReserva.Aprobada) &&
                x.Inicio < fin && inicio < x.Fin, ct))
                return new(null, "Ya tienes una solicitud pendiente o aprobada en ese horario. Solo puedes reservar un equipo a la vez.");

            var reserva = new Reserva { EstudianteId = estudianteId, EquipoId = model.EquipoId,
                Inicio = inicio, Fin = fin, CreadaEn = reloj.GetUtcNow(), Estado = EstadoReserva.Pendiente };
            db.Reservas.Add(reserva);
            await db.SaveChangesAsync(ct);
            await transaccion.CommitAsync(ct);
            return new(reserva.Id, null);
        }
        catch (Exception ex) when (EsConflictoTemporal(ex))
        {
            return new(null, "Otra solicitud se procesó al mismo tiempo. Vuelve a intentar para comprobar la disponibilidad actualizada.");
        }
    }

    private static bool EsConflictoTemporal(Exception ex) =>
        ex is SqlException { Number: 1205 or 1222 } ||
        ex is DbUpdateException { InnerException: SqlException { Number: 1205 or 1222 } };

    public async Task<MisReservasViewModel> ConsultarPropiasAsync(string estudianteId, int pagina, CancellationToken ct)
    {
        var consulta = db.Reservas.AsNoTracking().Where(x => x.EstudianteId == estudianteId);
        var total = await consulta.CountAsync(ct);
        var totalPaginas = Math.Max(1, (int)Math.Ceiling(total / (double)PorPagina));
        pagina = Math.Clamp(pagina, 1, totalPaginas);
        var reservas = await consulta.OrderByDescending(x => x.CreadaEn).ThenByDescending(x => x.Id)
            .Skip((pagina - 1) * PorPagina).Take(PorPagina)
            .Select(x => new ReservaListaItem(x.Id, x.Equipo.Nombre, x.Equipo.CodigoInventario,
                x.Inicio, x.Fin, x.Estado, x.MotivoRechazo, x.DevueltaEn)).ToListAsync(ct);
        return new(reservas, total, pagina, totalPaginas);
    }
}
