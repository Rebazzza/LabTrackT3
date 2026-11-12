using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace CalidadYPrueba_T3.ViewModels;

public class LoginViewModel
{
    [Required(ErrorMessage = "Ingresa tu código de usuario.")]
    [StringLength(256, ErrorMessage = "El código no puede superar los 256 caracteres.")]
    [Display(Name = "Código de usuario")]
    public string Codigo { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingresa tu contraseña.")]
    [StringLength(256, ErrorMessage = "La contraseña no puede superar los 256 caracteres.")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña")]
    public string Password { get; set; } = string.Empty;

    [BindRequired]
    [Required(ErrorMessage = "Indica si deseas mantener la sesión.")]
    [Display(Name = "Mantener mi sesión en este equipo")]
    public bool Recordarme { get; set; }
    public string? ReturnUrl { get; set; }
}
