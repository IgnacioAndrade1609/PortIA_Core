namespace PortIA.Core.Services.Component.Mensajeria.Events;

/// <summary>
/// Publica: Core. Consume: Agente Inspector.
/// Contrato completo en PortIA_Agentes/EVENTOS.md.
/// </summary>
public record InspectContainer(
    int ContainerId,
    string ContainerNumber,
    string TerminalCode,
    Dictionary<string, object> TerminalConfig
);
