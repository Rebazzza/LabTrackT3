namespace CalidadYPrueba_T3.ViewModels;

public record PrestamoActivo(int Id, string Estudiante, string CodigoEstudiante, string Equipo,
    string Inventario, DateTimeOffset Fin, byte[] Version);

public record DevolucionesViewModel(IReadOnlyList<PrestamoActivo> Prestamos, string? Buscar,
    int Total, int Pagina, int TotalPaginas, DateTimeOffset Ahora);
