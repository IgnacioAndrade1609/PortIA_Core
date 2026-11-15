namespace PortIA.Core.Services.Component.Notifications.Validators;

using FluentValidation;
using PortIA.Core.Services.Component.Notifications.DTOs;

public class SendNotificationRequestValidator : AbstractValidator<SendNotificationRequest>
{
    private static readonly int[] CanalesValidos = [1, 2];

    public SendNotificationRequestValidator()
    {
        RuleFor(x => x.CanalId)
            .Must(id => CanalesValidos.Contains(id))
            .WithMessage("El canal debe ser 1 (EMAIL) o 2 (TELEGRAM).");

        RuleFor(x => x.Mensaje)
            .NotEmpty().WithMessage("El mensaje no puede estar vacío.")
            .MaximumLength(4000).WithMessage("El mensaje supera el máximo de 4000 caracteres.");

        When(x => !string.IsNullOrEmpty(x.Destinatario), () =>
        {
            RuleFor(x => x.Destinatario)
                .MaximumLength(100).WithMessage("El destinatario supera el límite de 100 caracteres.");
        });
    }
}

public class RejectNotificationRequestValidator : AbstractValidator<RejectNotificationRequest>
{
    public RejectNotificationRequestValidator()
    {
        RuleFor(x => x.CanalId)
            .Must(id => id is 1 or 2)
            .WithMessage("El canal debe ser 1 (EMAIL) o 2 (TELEGRAM).");

        RuleFor(x => x.Motivo)
            .MaximumLength(500).WithMessage("El motivo supera el máximo de 500 caracteres.");
    }
}
