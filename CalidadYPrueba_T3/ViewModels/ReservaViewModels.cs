using System.ComponentModel.DataAnnotations;
using CalidadYPrueba_T3.Models;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace CalidadYPrueba_T3.ViewModels;

public record EquipoReservaInfo(int Id, string Nombre, string Codigo, string Categoria, bool Prestado);
public record HorarioAprobado(DateTimeOffset Inicio, DateTimeOffset Fin);

public class CrearReservaViewModel
{
    [Range(1, int.MaxValue, ErrorMessage = "Selecciona un equipo válido.")]
    public int EquipoId { get; set; }

    [Required(ErrorMessage = "Selecciona la fecha de uso.")]
    [DataType(DataType.Date)]
    [Display(Name = "Fecha de uso")]
    public DateOnly? Fecha { get; set; }

    [Required(ErrorMessage = "Selecciona la hora de inicio.")]
    [DataType(DataType.Time)]
    [Display(Name = "Hora de inicio")]
    public TimeOnly? HoraInicio { get; set; }

    [Required(ErrorMessage = "Selecciona la hora de fin.")]
    [DataType(DataType.Time)]
    [Display(Name = "Hora de fin")]
    public TimeOnly? HoraFin { get; set; }

    [ValidateNever]
    public EquipoReservaInfo? Equipo { get; set; }
    [ValidateNever]
    public IReadOnlyList<HorarioAprobado> HorariosAprobados { get; set; } = [];
    [ValidateNever]
    public DateOnly Hoy { get; set; }
}

public record ReservaListaItem(int Id, string Equipo, string Inventario, DateTimeOffset Inicio,
    DateTimeOffset Fin, EstadoReserva Estado, string? MotivoRechazo, DateTimeOffset? DevueltaEn);

public record MisReservasViewModel(IReadOnlyList<ReservaListaItem> Reservas, int Total, int Pagina, int TotalPaginas);
