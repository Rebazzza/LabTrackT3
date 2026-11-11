using System.Security.Claims;
using CalidadYPrueba_T3.Security;
using CalidadYPrueba_T3.Services;
using CalidadYPrueba_T3.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CalidadYPrueba_T3.Controllers;

[Authorize(Roles = Roles.Encargado)]
[ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
public class SolicitudesController(SolicitudService solicitudes) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(int pagina = 1, CancellationToken ct = default)
    {
        if (!ModelState.IsValid || pagina < 1) return BadRequest("La página solicitada no es válida.");
        return View(await solicitudes.ConsultarPendientesAsync(pagina, ct));
    }

    [HttpPost]
    public async Task<IActionResult> Aprobar(ResolverSolicitudViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest("La solicitud no es válida. Actualiza la lista.");
        var encargadoId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (encargadoId is null) return Challenge();
        var resultado = await solicitudes.ResolverAsync(model.Id, model.Version, encargadoId, true, null, ct);
        if (resultado.NoEncontrada) return NotFound();
        TempData[resultado.Exito ? "SolicitudResuelta" : "ErrorSolicitud"] = resultado.Mensaje;
        return RedirectToAction(nameof(Index), new { pagina = model.Pagina });
    }

    [HttpGet]
    public async Task<IActionResult> Rechazar(int id, int pagina = 1, CancellationToken ct = default)
    {
        if (!ModelState.IsValid || id < 1 || pagina < 1) return BadRequest("La solicitud no es válida.");
        var solicitud = await solicitudes.ObtenerPendienteAsync(id, ct);
        if (solicitud is null) return NotFound();
        return View(new RechazarSolicitudViewModel { Id = id, Version = solicitud.Version, Pagina = pagina, Solicitud = solicitud });
    }

    [HttpPost]
    public async Task<IActionResult> Rechazar(RechazarSolicitudViewModel model, CancellationToken ct)
    {
        var encargadoId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (encargadoId is null) return Challenge();
        if (!ModelState.IsValid)
        {
            model.Solicitud = await solicitudes.ObtenerPendienteAsync(model.Id, ct);
            if (model.Solicitud is null) return NotFound();
            return View(model);
        }
        var resultado = await solicitudes.ResolverAsync(model.Id, model.Version, encargadoId, false, model.Motivo, ct);
        if (resultado.NoEncontrada) return NotFound();
        TempData[resultado.Exito ? "SolicitudResuelta" : "ErrorSolicitud"] = resultado.Mensaje;
        return RedirectToAction(nameof(Index), new { pagina = model.Pagina });
    }
}
