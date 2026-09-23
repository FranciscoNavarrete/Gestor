namespace GestorPOS.Application.Tareas.Dtos;

public record CrearTareaRequest(string Titulo, string? Notas, DateOnly Fecha, TimeOnly Hora, int? MinutosAntesAviso);

public record EditarTareaRequest(string Titulo, string? Notas, DateOnly Fecha, TimeOnly Hora, int? MinutosAntesAviso);

public record TareaDto(
    Guid Id,
    string Titulo,
    string? Notas,
    DateTime FechaHora,
    int? MinutosAntesAviso,
    bool Completada);
