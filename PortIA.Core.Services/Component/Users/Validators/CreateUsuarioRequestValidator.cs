namespace PortIA.Core.Services.Component.Users.Validators;

using FluentValidation;
using PortIA.Core.Services.Component.Users.DTOs;

public class CreateUsuarioRequestValidator : AbstractValidator<CreateUsuarioRequest>
{
    public CreateUsuarioRequestValidator()
    {
        RuleFor(x => x.NombreUsuario)
            .NotEmpty().WithMessage("El nombre de usuario es obligatorio.")
            .MaximumLength(250).WithMessage("El nombre de usuario supera el máximo de 250 caracteres.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("El correo es obligatorio.")
            .EmailAddress().WithMessage("El correo no tiene un formato válido.")
            .MaximumLength(250).WithMessage("El correo supera el máximo de 250 caracteres.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("La contraseña es obligatoria.")
            .MinimumLength(8).WithMessage("La contraseña debe tener al menos 8 caracteres.");

        RuleFor(x => x.RolId)
            .GreaterThan(0).WithMessage("Debe indicar un rol válido.");
    }
}
