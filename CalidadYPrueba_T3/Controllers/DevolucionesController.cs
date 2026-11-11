using System.Security.Claims;
using CalidadYPrueba_T3.Security;
using CalidadYPrueba_T3.Services;
using CalidadYPrueba_T3.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CalidadYPrueba_T3.Controllers;

[Authorize(Roles = Roles.Encargado)]
[ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
public class DevolucionesController(DevolucionService devoluciones) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(string? buscar, int pagina = 1, CancellationToken ct = default)
    {
        buscar = buscar?.Trim();
        if (!ModelState.IsValid || pagina < 1 || buscar is { Length: > 100 })
            return BadRequest("La búsqueda o la página no son válidas.");

        return View(await devoluciones.ConsultarAsync(buscar, pagina, ct));
    }

    [HttpPost]
    public async Task<IActionResult> Registrar(ResolverSolicitudViewModel model, string? buscar, CancellationToken ct)
    {
        if (!ModelState.IsValid || buscar is { Length: > 100 }) return BadRequest("La devolución no es válida.");
        var encargadoId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (encargadoId is null) return Challenge();

        var resultado = await devoluciones.RegistrarAsync(model.Id, model.Version, encargadoId, ct);
        if (resultado.NoEncontrada) return NotFound();
        TempData[resultado.Exito ? "DevolucionRegistrada" : "ErrorDevolucion"] = resultado.Mensaje;
        return RedirectToAction(nameof(Index), new { model.Pagina, buscar });
    }
}
