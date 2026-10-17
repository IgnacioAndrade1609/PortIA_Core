namespace PortIA.Core.Services.Component.Mensajeria.Events;

public record InspectContainer(
    int ContainerId,
    string ContainerNumber,
    string TerminalCode,
    Dictionary<string, object> TerminalConfig
);
