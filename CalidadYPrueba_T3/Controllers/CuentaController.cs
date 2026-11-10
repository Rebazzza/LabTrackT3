using CalidadYPrueba_T3.Models;
using CalidadYPrueba_T3.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CalidadYPrueba_T3.Controllers;

[ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
public class CuentaController(SignInManager<Usuario> signInManager) : Controller
{
    [AllowAnonymous]
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Home");
        return View(new LoginViewModel { ReturnUrl = Url.IsLocalUrl(returnUrl) ? returnUrl : null });
    }

    [AllowAnonymous]
    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Home");
        if (!ModelState.IsValid) return View(model);

        var resultado = await signInManager.PasswordSignInAsync(
            model.Codigo.Trim(), model.Password, model.Recordarme, lockoutOnFailure: true);
        if (resultado.Succeeded)
        {
            if (Url.IsLocalUrl(model.ReturnUrl)) return LocalRedirect(model.ReturnUrl!);
            return RedirectToAction("Index", "Home");
        }

        ModelState.AddModelError(string.Empty, resultado.IsLockedOut
            ? "Cuenta bloqueada temporalmente por seguridad. Intenta nuevamente más tarde."
            : "Credenciales inválidas");
        model.Password = string.Empty;
        ModelState.Remove(nameof(model.Password));
        return View(model);
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Salir()
    {
        await signInManager.SignOutAsync();
        TempData["SesionCerrada"] = "Tu sesión se cerró correctamente.";
        return RedirectToAction(nameof(Login));
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult AccesoDenegado()
    {
        Response.StatusCode = StatusCodes.Status403Forbidden;
        return View();
    }
}
