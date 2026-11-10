using CalidadYPrueba_T3.Security;
using CalidadYPrueba_T3.Services;
using CalidadYPrueba_T3.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CalidadYPrueba_T3.Controllers;

[Authorize(Roles = Roles.Estudiante + "," + Roles.Encargado)]
[ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
public class CatalogoController(CatalogoService catalogo) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(int? categoriaId, int pagina = 1, CancellationToken ct = default)
    {
        var categorias = await catalogo.ObtenerCategoriasAsync(ct);
        if (!ModelState.IsValid || pagina < 1 ||
            (categoriaId.HasValue && !categorias.Any(x => x.Id == categoriaId)))
        {
            ModelState.AddModelError(string.Empty, "El filtro o la página no son válidos. Selecciona una categoría de la lista.");
            Response.StatusCode = StatusCodes.Status400BadRequest;
            return View(new CatalogoViewModel { Categorias = categorias });
        }
        return View(await catalogo.ConsultarAsync(categoriaId, pagina, categorias, ct));
    }

    [Authorize(Roles = Roles.Encargado)]
    [HttpGet]
    public async Task<IActionResult> Crear(CancellationToken ct) =>
        View(new CrearEquipoViewModel { Categorias = await catalogo.ObtenerCategoriasAsync(ct) });

    [Authorize(Roles = Roles.Encargado)]
    [HttpPost]
    public async Task<IActionResult> Crear(CrearEquipoViewModel model, CancellationToken ct)
    {
        model.Categorias = await catalogo.ObtenerCategoriasAsync(ct);
        if (model.CategoriaId.HasValue && !model.Categorias.Any(x => x.Id == model.CategoriaId))
            ModelState.AddModelError(nameof(model.CategoriaId), "Selecciona una categoría válida.");
        if (!ModelState.IsValid) return View(model);
        if (!await catalogo.CrearEquipoAsync(model, ct))
        {
            ModelState.AddModelError(nameof(model.CodigoInventario), "Ya existe un equipo con este código de inventario.");
            return View(model);
        }
        TempData["EquipoCreado"] = "El equipo se registró correctamente.";
        return RedirectToAction(nameof(Index), new { categoriaId = model.CategoriaId });
    }
}
