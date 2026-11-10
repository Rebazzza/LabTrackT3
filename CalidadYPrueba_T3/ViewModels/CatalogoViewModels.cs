using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace CalidadYPrueba_T3.ViewModels;

public record CategoriaOpcion(int Id, string Nombre);

public class EquipoCatalogoItem
{
    public int Id { get; init; }
    public string Nombre { get; init; } = string.Empty;
    public string CodigoInventario { get; init; } = string.Empty;
    public string Categoria { get; init; } = string.Empty;
    public bool Prestado { get; init; }
}

public class CatalogoViewModel
{
    public int? CategoriaId { get; init; }
    public IReadOnlyList<CategoriaOpcion> Categorias { get; init; } = [];
    public IReadOnlyList<EquipoCatalogoItem> Equipos { get; init; } = [];
    public int Total { get; init; }
    public int Prestados { get; init; }
    public int Disponibles => Total - Prestados;
    public int Pagina { get; init; } = 1;
    public int TotalPaginas { get; init; } = 1;
    public DateTimeOffset ConsultadoEn { get; init; }
}

public class CrearEquipoViewModel
{
    [Required(ErrorMessage = "Ingresa el nombre del equipo.")]
    [StringLength(150, ErrorMessage = "El nombre admite hasta 150 caracteres.")]
    [Display(Name = "Nombre del equipo")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingresa el código de inventario.")]
    [StringLength(30, ErrorMessage = "El código admite hasta 30 caracteres.")]
    [Display(Name = "Código de inventario")]
    public string CodigoInventario { get; set; } = string.Empty;

    [Required(ErrorMessage = "Selecciona una categoría.")]
    [Display(Name = "Categoría")]
    public int? CategoriaId { get; set; }

    [ValidateNever]
    public IReadOnlyList<CategoriaOpcion> Categorias { get; set; } = [];
}
