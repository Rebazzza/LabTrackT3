namespace CalidadYPrueba_T3.Models;

public class Reserva
{
    public int Id { get; set; }
    public string EstudianteId { get; set; } = string.Empty;
    public Usuario Estudiante { get; set; } = null!;
    public int EquipoId { get; set; }
    public Equipo Equipo { get; set; } = null!;
    public DateTimeOffset Inicio { get; set; }
    public DateTimeOffset Fin { get; set; }
    public DateTimeOffset CreadaEn { get; set; } = DateTimeOffset.UtcNow;
    public EstadoReserva Estado { get; set; } = EstadoReserva.Pendiente;
    public string? ResueltaPorId { get; set; }
    public Usuario? ResueltaPor { get; set; }
    public DateTimeOffset? ResueltaEn { get; set; }
    public string? MotivoRechazo { get; set; }
    public DateTimeOffset? DevueltaEn { get; set; }
    public string? DevolucionRegistradaPorId { get; set; }
    public Usuario? DevolucionRegistradaPor { get; set; }
    public Penalizacion? Penalizacion { get; set; }
    public byte[] Version { get; set; } = [];
}
