namespace CalidadYPrueba_T3.Models;

public class Categoria
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public ICollection<Equipo> Equipos { get; set; } = new List<Equipo>();
}
