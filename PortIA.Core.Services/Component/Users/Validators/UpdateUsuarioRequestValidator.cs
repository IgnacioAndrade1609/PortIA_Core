namespace PortIA.Core.Services.Component.Users.Validators;

using FluentValidation;
using PortIA.Core.Services.Component.Users.DTOs;

public class UpdateUsuarioRequestValidator : AbstractValidator<UpdateUsuarioRequest>
{
    public UpdateUsuarioRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre es obligatorio.")
            .MaximumLength(250).WithMessage("El nombre supera el máximo de 250 caracteres.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("El correo es obligatorio.")
            .EmailAddress().WithMessage("El correo no tiene un formato válido.")
            .MaximumLength(250).WithMessage("El correo supera el máximo de 250 caracteres.");
    }
}