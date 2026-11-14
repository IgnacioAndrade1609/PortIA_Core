namespace PortIA.Core.Services.Component.Navieras.Validators;

using FluentValidation;
using PortIA.Core.Services.Component.Navieras.DTOs;

public class UpdateNavieraRequestValidator : AbstractValidator<UpdateNavieraRequest>
{
    public UpdateNavieraRequestValidator()
    {
        RuleFor(x => x.NombreNaviera)
            .NotEmpty().WithMessage("El nombre de la naviera es obligatorio.")
            .MaximumLength(150).WithMessage("El nombre de la naviera supera el máximo de 150 caracteres.");

        RuleFor(x => x.CodigoNaviera)
            .MaximumLength(10).WithMessage("El código de la naviera supera el máximo de 10 caracteres.");

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("El correo no tiene un formato válido.")
            .MaximumLength(250).WithMessage("El correo supera el máximo de 250 caracteres.")
            .When(x => !string.IsNullOrWhiteSpace(x.Email));

        RuleFor(x => x.Telefono)
            .MaximumLength(50).WithMessage("El teléfono supera el máximo de 50 caracteres.");
    }
}
