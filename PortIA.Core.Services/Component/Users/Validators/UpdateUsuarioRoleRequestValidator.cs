namespace PortIA.Core.Services.Component.Users.Validators;

using FluentValidation;
using PortIA.Core.Services.Component.Users.DTOs;

public class UpdateUsuarioRoleRequestValidator : AbstractValidator<UpdateUsuarioRoleRequest>
{
    public UpdateUsuarioRoleRequestValidator()
    {
        RuleFor(x => x.RoleId)
            .GreaterThan(0).WithMessage("Debe indicar un rol válido.");
    }
}