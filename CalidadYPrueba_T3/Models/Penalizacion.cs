namespace CalidadYPrueba_T3.Models;

public class Penalizacion
{
    public int Id { get; set; }
    public int ReservaId { get; set; }
    public Reserva Reserva { get; set; } = null!;
    public string Motivo { get; set; } = string.Empty;
    public DateTimeOffset RegistradaEn { get; set; } = DateTimeOffset.UtcNow;
}
