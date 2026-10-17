using PortIA.Core.Services.Component.Mensajeria.DTOs;

namespace PortIA.Core.Services.Component.Mensajeria.Interfaces;

public interface IMensajeriaService
{
    Task PublishShipmentCreatedAsync(PublishShipmentCreatedRequest request, CancellationToken ct);
}
