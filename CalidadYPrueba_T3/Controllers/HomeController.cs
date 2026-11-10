using CalidadYPrueba_T3.Models;
using CalidadYPrueba_T3.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace CalidadYPrueba_T3.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            if (User.IsInRole(Roles.Encargado)) return RedirectToAction("Index", "Encargado");
            if (User.IsInRole(Roles.Estudiante)) return RedirectToAction("Index", "Estudiante");
            return RedirectToAction("AccesoDenegado", "Cuenta");
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
