using CalidadYPrueba_T3.Models;

namespace CalidadYPrueba_T3.ViewModels;

public record PanelEstudianteViewModel(string Nombre, string Codigo, EstadoEstudiante Estado,
    int Pendientes, int Aprobadas, int Devueltas);
public record PanelEncargadoViewModel(string Nombre, int Pendientes, int PorDevolver, int Equipos);
