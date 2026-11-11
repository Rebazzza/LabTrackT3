using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace CalidadYPrueba_T3.ViewModels;

public record SolicitudPendienteItem(int Id, string Estudiante, string CodigoEstudiante,
    string Equipo, string Inventario, DateTimeOffset Inicio, DateTimeOffset Fin, string Version);

public record SolicitudesViewModel(IReadOnlyList<SolicitudPendienteItem> Solicitudes,
    int Total, int Pagina, int TotalPaginas, DateTimeOffset Ahora);

public class ResolverSolicitudViewModel
{
    [Range(1, int.MaxValue)]
    public int Id { get; set; }
    [Required]
    [StringLength(24)]
    public string Version { get; set; } = string.Empty;
    [Range(1, int.MaxValue)]
    public int Pagina { get; set; } = 1;
}

public class RechazarSolicitudViewModel : ResolverSolicitudViewModel
{
    [Required(ErrorMessage = "Escribe el motivo del rechazo.")]
    [StringLength(500, ErrorMessage = "El motivo admite hasta 500 caracteres.")]
    [Display(Name = "Motivo del rechazo")]
    public string Motivo { get; set; } = string.Empty;
    [ValidateNever]
    public SolicitudPendienteItem? Solicitud { get; set; }
}
