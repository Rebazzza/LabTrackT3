using System.Data;
using CalidadYPrueba_T3.Data;
using CalidadYPrueba_T3.Models;
using CalidadYPrueba_T3.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace CalidadYPrueba_T3.Services;

public record ResultadoResolucion(bool Exito, string Mensaje, bool NoEncontrada = false);

public class SolicitudService(LabTrackDbContext db, TimeProvider reloj)
{
    private const int PorPagina = 10;

    public async Task<SolicitudesViewModel> ConsultarPendientesAsync(int pagina, CancellationToken ct)
    {
        var consulta = db.Reservas.AsNoTracking().Where(x => x.Estado == EstadoReserva.Pendiente);
        var total = await consulta.CountAsync(ct);
        var totalPaginas = Math.Max(1, (int)Math.Ceiling(total / (double)PorPagina));
        pagina = Math.Clamp(pagina, 1, totalPaginas);
        var filas = await consulta.OrderBy(x => x.CreadaEn).ThenBy(x => x.Id)
            .Skip((pagina - 1) * PorPagina).Take(PorPagina)
            .Select(x => new { x.Id, Estudiante = x.Estudiante.NombreCompleto,
                Codigo = x.Estudiante.UserName!, Equipo = x.Equipo.Nombre,
                Inventario = x.Equipo.CodigoInventario, x.Inicio, x.Fin, x.Version }).ToListAsync(ct);
        var solicitudes = filas.Select(x => new SolicitudPendienteItem(x.Id, x.Estudiante,
            x.Codigo, x.Equipo, x.Inventario, x.Inicio, x.Fin, Convert.ToBase64String(x.Version))).ToList();
        return new(solicitudes, total, pagina, totalPaginas, reloj.GetUtcNow());
    }

    public async Task<SolicitudPendienteItem?> ObtenerPendienteAsync(int id, CancellationToken ct)
    {
        var fila = await db.Reservas.AsNoTracking().Where(x => x.Id == id && x.Estado == EstadoReserva.Pendiente)
            .Select(x => new { x.Id, Estudiante = x.Estudiante.NombreCompleto,
                Codigo = x.Estudiante.UserName!, Equipo = x.Equipo.Nombre,
                Inventario = x.Equipo.CodigoInventario, x.Inicio, x.Fin, x.Version }).SingleOrDefaultAsync(ct);
        return fila is null ? null : new(fila.Id, fila.Estudiante, fila.Codigo, fila.Equipo,
            fila.Inventario, fila.Inicio, fila.Fin, Convert.ToBase64String(fila.Version));
    }

    public async Task<ResultadoResolucion> ResolverAsync(int id, string version, string encargadoId,
        bool aprobar, string? motivo, CancellationToken ct)
    {
        var versionOriginal = new byte[8];
        if (!Convert.TryFromBase64String(version, versionOriginal, out var bytes) || bytes != 8)
            return new(false, "La solicitud no es válida. Actualiza la lista antes de continuar.");
        if (!aprobar && (string.IsNullOrWhiteSpace(motivo) || motivo.Length > 500))
            return new(false, "Escribe un motivo de rechazo de hasta 500 caracteres.");

        try
        {
            await using var transaccion = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
            var reserva = await db.Reservas.SingleOrDefaultAsync(x => x.Id == id, ct);
            if (reserva is null) return new(false, "La solicitud no existe.", true);
            if (reserva.Estado != EstadoReserva.Pendiente)
                return new(false, "La solicitud ya fue resuelta. No se realizó ningún cambio.");
            if (!reserva.Version.SequenceEqual(versionOriginal))
                return new(false, "La solicitud cambió desde que abriste la página. Actualiza la lista y vuelve a revisarla.");
            if (!await db.Users.AnyAsync(x => x.Id == encargadoId, ct))
                return new(false, "No se encontró la cuenta del encargado.");

            var ahora = reloj.GetUtcNow();
            if (aprobar)
            {
                if (reserva.Fin <= ahora)
                    return new(false, "El horario solicitado ya finalizó. Puedes rechazar la solicitud indicando el motivo.");
                if (await db.Reservas.AnyAsync(x => x.Id != id && x.EquipoId == reserva.EquipoId &&
                    x.Estado == EstadoReserva.Aprobada && x.Inicio <= ahora, ct))
                    return new(false, "El equipo tiene un préstamo iniciado sin devolución. No se puede aprobar esta solicitud.");
                if (await db.Reservas.AnyAsync(x => x.Id != id && x.EquipoId == reserva.EquipoId &&
                    x.Estado == EstadoReserva.Aprobada && x.Inicio < reserva.Fin && reserva.Inicio < x.Fin, ct))
                    return new(false, "El equipo ya tiene una reserva aprobada que se cruza con este horario.");
                if (await db.Reservas.AnyAsync(x => x.Id != id && x.EstudianteId == reserva.EstudianteId &&
                    x.Estado == EstadoReserva.Aprobada && x.Inicio < reserva.Fin && reserva.Inicio < x.Fin, ct))
                    return new(false, "El estudiante ya tiene otro equipo aprobado en ese horario.");
            }

            reserva.Estado = aprobar ? EstadoReserva.Aprobada : EstadoReserva.Rechazada;
            reserva.MotivoRechazo = aprobar ? null : motivo!.Trim();
            reserva.ResueltaPorId = encargadoId;
            reserva.ResueltaEn = ahora;
            await db.SaveChangesAsync(ct);
            await transaccion.CommitAsync(ct);
            return new(true, aprobar ? $"Solicitud #{id} aprobada correctamente."
                : $"Solicitud #{id} rechazada. El estudiante podrá consultar el motivo.");
        }
        catch (DbUpdateConcurrencyException)
        {
            return new(false, "Otro encargado modificó la solicitud. Actualiza la lista antes de continuar.");
        }
        catch (Exception ex) when (ConflictosBaseDatos.EsBloqueoTemporal(ex))
        {
            return new(false, "Otra operación se procesó al mismo tiempo. Actualiza la lista y vuelve a intentar.");
        }
    }
}
