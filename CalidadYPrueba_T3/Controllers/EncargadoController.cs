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
public class EncargadoController(LabTrackDbContext db, UserManager<Usuario> usuarios, TimeProvider reloj) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var usuario = await usuarios.GetUserAsync(User);
        if (usuario is null) return Challenge();
        var ahora = reloj.GetUtcNow();
        var pendientes = await db.Reservas.CountAsync(x => x.Estado == EstadoReserva.Pendiente, cancellationToken);
        var porDevolver = await db.Reservas.CountAsync(
            x => x.Estado == EstadoReserva.Aprobada && x.Inicio <= ahora, cancellationToken);
        var equipos = await db.Equipos.CountAsync(cancellationToken);
        return View(new PanelEncargadoViewModel(usuario.NombreCompleto, pendientes, porDevolver, equipos));
    }
}
