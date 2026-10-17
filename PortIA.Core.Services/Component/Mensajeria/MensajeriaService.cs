using MassTransit;
using PortIA.Core.Services.Component.Mensajeria.DTOs;
using PortIA.Core.Services.Component.Mensajeria.Events;
using PortIA.Core.Services.Component.Mensajeria.Interfaces;

namespace PortIA.Core.Services.Component.Mensajeria;

/// <summary>
/// Envía eventos a los agentes (Python) vía RabbitMQ. Usa ISendEndpointProvider en vez de
/// IPublishEndpoint porque los agentes consumen colas simples declaradas por nombre
/// (sin exchange propio) — Send entrega directo a "queue:{nombre}" por el exchange por
/// defecto, que es exactamente el mismo mecanismo que usa aio-pika del lado Python.
/// </summary>
public class MensajeriaService(ISendEndpointProvider sendEndpointProvider) : IMensajeriaService
{
    public async Task PublishShipmentCreatedAsync(PublishShipmentCreatedRequest request, CancellationToken ct)
    {
        // bind=false: la cola "shipment_created" ya la declaró el agente Python con su
        // propio argumento x-dead-letter-exchange. Si MassTransit intenta redeclararla
        // (comportamiento por defecto), RabbitMQ rechaza el mensaje porque los argumentos
        // no calzan exactamente (PRECONDITION_FAILED). bind=false le dice que solo envíe,
        // sin tocar la declaración de la cola.
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
