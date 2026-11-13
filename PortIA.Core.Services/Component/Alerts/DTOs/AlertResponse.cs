namespace PortIA.Core.Services.Component.Alerts.DTOs;

public record AlertResponse(
    int Id,
    int ContainerId,
    string NumeroContainer,
    string TipoAlerta,
    string NivelRiesgo,
    string Mensaje,
    bool Leida,
    DateTimeOffset CreateAt,
    string? AnalistaAsignado
);
