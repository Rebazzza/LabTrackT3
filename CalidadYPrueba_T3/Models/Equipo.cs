namespace CalidadYPrueba_T3.Models;

public class Equipo
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string CodigoInventario { get; set; } = string.Empty;
    public int CategoriaId { get; set; }
    public Categoria Categoria { get; set; } = null!;
    public byte[] Version { get; set; } = [];
    // Disponible/Prestado se calculará por horario y por devolución física.
    public ICollection<Reserva> Reservas { get; set; } = new List<Reserva>();
}
