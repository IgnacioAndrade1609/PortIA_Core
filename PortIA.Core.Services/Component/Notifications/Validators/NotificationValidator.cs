namespace PortIA.Core.Services.Component.Notifications.Validators;

using FluentValidation;
using PortIA.Core.Services.Component.Notifications.DTOs;

public class CreateDraftRequestValidator : AbstractValidator<CreateDraftRequest>
{
    private static readonly int[] CanalesValidos = [1, 2];

    public CreateDraftRequestValidator()
    {
        RuleFor(x => x.AlertaId)
            .GreaterThan(0).WithMessage("Debe indicar una alerta válida.");

        RuleFor(x => x.CanalId)
            .Must(id => CanalesValidos.Contains(id))
            .WithMessage("El canal debe ser 1 (EMAIL) o 2 (TELEGRAM).");

        RuleFor(x => x.Destinatario)
            .NotEmpty().WithMessage("El destinatario es obligatorio.")
            .MaximumLength(250).WithMessage("El destinatario supera el máximo de 250 caracteres.");
    }
}

public class UpdateDraftRequestValidator : AbstractValidator<UpdateDraftRequest>
{
    public UpdateDraftRequestValidator()
    {
        RuleFor(x => x.Mensaje)
            .NotEmpty().WithMessage("El mensaje no puede estar vacío.")
            .MaximumLength(4000).WithMessage("El mensaje supera el máximo de 4000 caracteres.");
    }
}

public class ApproveNotificationRequestValidator : AbstractValidator<ApproveNotificationRequest>
{
    public ApproveNotificationRequestValidator()
    {
        RuleFor(x => x.Mensaje)
            .NotEmpty().WithMessage("El mensaje aprobado no puede estar vacío.")
            .MaximumLength(4000).WithMessage("El mensaje supera el máximo de 4000 caracteres.");
    }
}