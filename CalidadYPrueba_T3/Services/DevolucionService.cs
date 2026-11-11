using System.Data;
using CalidadYPrueba_T3.Data;
using CalidadYPrueba_T3.Models;
using CalidadYPrueba_T3.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace CalidadYPrueba_T3.Services;

public class DevolucionService(LabTrackDbContext db, TimeProvider reloj)
{
    private const int PorPagina = 10;

    public async Task<DevolucionesViewModel> ConsultarAsync(string? buscar, int pagina, CancellationToken ct)
    {
        var ahora = reloj.GetUtcNow();
        var consulta = db.Reservas.AsNoTracking()
            .Where(x => x.Estado == EstadoReserva.Aprobada && x.Inicio <= ahora);
        if (!string.IsNullOrEmpty(buscar))
        {
            consulta = consulta.Where(x => x.Estudiante.NombreCompleto.Contains(buscar)
                || x.Estudiante.UserName!.Contains(buscar)
                || x.Equipo.Nombre.Contains(buscar)
                || x.Equipo.CodigoInventario.Contains(buscar));
        }

        var total = await consulta.CountAsync(ct);
        var totalPaginas = Math.Max(1, (int)Math.Ceiling(total / (double)PorPagina));
        pagina = Math.Clamp(pagina, 1, totalPaginas);
        var prestamos = await consulta.OrderBy(x => x.Fin).ThenBy(x => x.Id)
            .Skip((pagina - 1) * PorPagina).Take(PorPagina)
            .Select(x => new PrestamoActivo(x.Id, x.Estudiante.NombreCompleto, x.Estudiante.UserName!,
                x.Equipo.Nombre, x.Equipo.CodigoInventario, x.Fin, x.Version))
            .ToListAsync(ct);
        return new(prestamos, buscar, total, pagina, totalPaginas, ahora);
    }

    public async Task<ResultadoResolucion> RegistrarAsync(int id, string version, string encargadoId, CancellationToken ct)
    {
        var versionOriginal = new byte[8];
        if (!Convert.TryFromBase64String(version, versionOriginal, out var bytes) || bytes != 8)
            return new(false, "La solicitud no es válida. Actualiza la lista.");

        try
        {
            await using var transaccion = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
            var reserva = await db.Reservas.Include(x => x.Estudiante)
                .SingleOrDefaultAsync(x => x.Id == id, ct);
            if (reserva is null) return new(false, "El préstamo no existe.", true);
            if (reserva.Estado == EstadoReserva.Devuelta) return new(false, "La devolución ya está registrada.");
            var ahora = reloj.GetUtcNow();
            if (reserva.Estado != EstadoReserva.Aprobada || reserva.Inicio > ahora)
                return new(false, "Solo se pueden devolver préstamos activos.");
            if (!reserva.Version.SequenceEqual(versionOriginal))
                return new(false, "El préstamo cambió desde que abriste la página. Actualiza la lista.");
            if (!await db.Users.AnyAsync(x => x.Id == encargadoId, ct))
                return new(false, "No se encontró la cuenta del encargado.");

            // Usa la misma hora para la devolución y su penalización.
            ahora = reloj.GetUtcNow();
            reserva.Estado = EstadoReserva.Devuelta;
            reserva.DevueltaEn = ahora;
            reserva.DevolucionRegistradaPorId = encargadoId;
            var tardia = ahora > reserva.Fin;
            if (tardia)
            {
                reserva.Estudiante.Estado = EstadoEstudiante.Penalizado;
                reserva.Penalizacion = new Penalizacion
                {
                    Motivo = "Devolución posterior al horario acordado.",
                    RegistradaEn = ahora
                };
            }

            // EF guarda juntos la devolución, la penalización y el estado del estudiante.
            await db.SaveChangesAsync(ct);
            await transaccion.CommitAsync(ct);
            return new(true, tardia
                ? $"Devolución #{id} registrada. Se aplicó una penalización por retraso."
                : $"Devolución #{id} registrada a tiempo.");
        }
        catch (DbUpdateConcurrencyException)
        {
            return new(false, "Otro registro cambió durante la devolución. Actualiza la lista.");
        }
        catch (Exception ex) when (ConflictosBaseDatos.EsBloqueoTemporal(ex))
        {
            return new(false, "Otra operación se procesó al mismo tiempo. Actualiza la lista y vuelve a intentar.");
        }
    }
}
