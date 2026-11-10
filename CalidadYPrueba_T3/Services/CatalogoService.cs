using CalidadYPrueba_T3.Data;
using CalidadYPrueba_T3.Models;
using CalidadYPrueba_T3.ViewModels;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace CalidadYPrueba_T3.Services;

public class CatalogoService(LabTrackDbContext db, TimeProvider reloj)
{
    public const int EquiposPorPagina = 12;

    public Task<List<CategoriaOpcion>> ObtenerCategoriasAsync(CancellationToken ct) =>
        db.Categorias.AsNoTracking().OrderBy(x => x.Id)
            .Select(x => new CategoriaOpcion(x.Id, x.Nombre)).ToListAsync(ct);

    public async Task<CatalogoViewModel> ConsultarAsync(int? categoriaId, int pagina,
        IReadOnlyList<CategoriaOpcion> categorias, CancellationToken ct)
    {
        var ahora = reloj.GetUtcNow();
        var consulta = db.Equipos.AsNoTracking()
            .Where(x => categoriaId == null || x.CategoriaId == categoriaId)
            .Select(x => new EquipoCatalogoItem
            {
                Id = x.Id, Nombre = x.Nombre, CodigoInventario = x.CodigoInventario,
                Categoria = x.Categoria.Nombre,
                // Incluye atrasos: el fin del horario no equivale a una devolución física.
                Prestado = x.Reservas.Any(r => r.Estado == EstadoReserva.Aprobada && r.Inicio <= ahora)
            });
        var resumen = await consulta.GroupBy(x => 1)
            .Select(x => new { Total = x.Count(), Prestados = x.Count(e => e.Prestado) })
            .SingleOrDefaultAsync(ct);
        var total = resumen?.Total ?? 0;
        var totalPaginas = Math.Max(1, (int)Math.Ceiling(total / (double)EquiposPorPagina));
        pagina = Math.Clamp(pagina, 1, totalPaginas);
        var equipos = await consulta.OrderBy(x => x.Nombre).ThenBy(x => x.Id)
            .Skip((pagina - 1) * EquiposPorPagina).Take(EquiposPorPagina).ToListAsync(ct);

        return new CatalogoViewModel
        {
            CategoriaId = categoriaId, Categorias = categorias, Equipos = equipos,
            Total = total, Prestados = resumen?.Prestados ?? 0, Pagina = pagina,
            TotalPaginas = totalPaginas, ConsultadoEn = ahora
        };
    }

    public async Task<bool> CrearEquipoAsync(CrearEquipoViewModel model, CancellationToken ct)
    {
        var codigo = model.CodigoInventario.Trim().ToUpperInvariant();
        if (await db.Equipos.AnyAsync(x => x.CodigoInventario == codigo, ct)) return false;
        var equipo = new Equipo { Nombre = model.Nombre.Trim(), CodigoInventario = codigo,
            CategoriaId = model.CategoriaId!.Value };
        db.Equipos.Add(equipo);
        try
        {
            await db.SaveChangesAsync(ct);
            return true;
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
        {
            // El índice único también protege altas concurrentes con el mismo inventario.
            db.Entry(equipo).State = EntityState.Detached;
            return false;
        }
    }
}
