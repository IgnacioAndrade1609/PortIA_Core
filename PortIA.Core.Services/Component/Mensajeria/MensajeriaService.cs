using MassTransit;
using PortIA.Core.Services.Component.Mensajeria.DTOs;
using PortIA.Core.Services.Component.Mensajeria.Events;
using PortIA.Core.Services.Component.Mensajeria.Interfaces;

namespace PortIA.Core.Services.Component.Mensajeria;

public class MensajeriaService(ISendEndpointProvider sendEndpointProvider) : IMensajeriaService
{
    public async Task PublishShipmentCreatedAsync(PublishShipmentCreatedRequest request, CancellationToken ct)
    {
        var endpoint = await sendEndpointProvider.GetSendEndpoint(new Uri("queue:shipment_created?bind=false"));

        await endpoint.Send(
            new ShipmentCreated(
                request.ShipmentId,
                request.DocumentId,
                request.DocumentUrl,
                request.DocumentType
            ),
            ct
        );
    }
}
