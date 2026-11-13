namespace PortIA.Core.Services.Component.Users.Validators;

using FluentValidation;
using PortIA.Core.Services.Component.Users.DTOs;

public class UpdateUsuarioSupervisorRequestValidator : AbstractValidator<UpdateUsuarioSupervisorRequest>
{
    public UpdateUsuarioSupervisorRequestValidator()
    {
        RuleFor(x => x.JefeId)
            .GreaterThan(0).WithMessage("Debe indicar un jefe válido.")
            .When(x => x.JefeId.HasValue);
    }
}
