using CalidadYPrueba_T3.Data;
using CalidadYPrueba_T3.Models;
using CalidadYPrueba_T3.Security;
using CalidadYPrueba_T3.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CalidadYPrueba_T3.Controllers;

[Authorize(Roles = Roles.Encargado)]
[ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
public class EncargadoController(LabTrackDbContext db, UserManager<Usuario> usuarios) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var usuario = await usuarios.GetUserAsync(User);
        if (usuario is null) return Challenge();
        var cantidades = await db.Reservas.AsNoTracking().GroupBy(x => x.Estado)
            .Select(x => new { Estado = x.Key, Cantidad = x.Count() })
            .ToDictionaryAsync(x => x.Estado, x => x.Cantidad, cancellationToken);
        var equipos = await db.Equipos.CountAsync(cancellationToken);
        return View(new PanelEncargadoViewModel(usuario.NombreCompleto,
            cantidades.GetValueOrDefault(EstadoReserva.Pendiente),
            cantidades.GetValueOrDefault(EstadoReserva.Aprobada), equipos));
    }
}
