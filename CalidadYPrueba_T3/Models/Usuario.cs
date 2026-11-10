using Microsoft.AspNetCore.Identity;

namespace CalidadYPrueba_T3.Models;

public class Usuario : IdentityUser
{
    // UserName será el código con el que el usuario inicia sesión.
    public string NombreCompleto { get; set; } = string.Empty;
    public EstadoEstudiante Estado { get; set; } = EstadoEstudiante.Activo;
    public byte[] Version { get; set; } = [];
    public ICollection<Reserva> Reservas { get; set; } = new List<Reserva>();
}
