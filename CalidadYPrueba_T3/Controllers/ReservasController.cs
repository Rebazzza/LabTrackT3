using System.Security.Claims;
using CalidadYPrueba_T3.Security;
using CalidadYPrueba_T3.Services;
using CalidadYPrueba_T3.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CalidadYPrueba_T3.Controllers;

[Authorize(Roles = Roles.Estudiante)]
[ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
public class ReservasController(ReservaService reservas) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(int pagina = 1, CancellationToken ct = default)
    {
        if (!ModelState.IsValid || pagina < 1) return BadRequest("La página solicitada no es válida.");
        var estudianteId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (estudianteId is null) return Challenge();
        return View(await reservas.ConsultarPropiasAsync(estudianteId, pagina, ct));
    }

    [HttpGet]
    public async Task<IActionResult> Crear(int equipoId, CancellationToken ct)
    {
        if (!ModelState.IsValid || equipoId < 1) return NotFound();
        var model = new CrearReservaViewModel { EquipoId = equipoId, Fecha = reservas.Hoy };
        if (!await reservas.PrepararFormularioAsync(model, ct)) return NotFound();
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Crear(CrearReservaViewModel model, CancellationToken ct)
    {
        var estudianteId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (estudianteId is null) return Challenge();
        if (!await reservas.PrepararFormularioAsync(model, ct)) return NotFound();
        if (!ModelState.IsValid) return View(model);
        var resultado = await reservas.SolicitarAsync(estudianteId, model, ct);
        if (resultado.Error is not null)
        {
            ModelState.AddModelError(string.Empty, resultado.Error);
            return View(model);
        }
        TempData["ReservaCreada"] = $"Solicitud #{resultado.ReservaId} registrada. Está pendiente de aprobación por el encargado.";
        return RedirectToAction(nameof(Index));
    }
}
