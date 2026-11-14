namespace PortIA.Core.Services.Component.Terminales.Validators;

using FluentValidation;
using PortIA.Core.Services.Component.Terminales.DTOs;

public class UpdateTerminalRequestValidator : AbstractValidator<UpdateTerminalRequest>
{
    public UpdateTerminalRequestValidator()
    {
        RuleFor(x => x.CodigoTerminal)
            .NotEmpty().WithMessage("El código del terminal es obligatorio.")
            .MaximumLength(10).WithMessage("El código del terminal supera el máximo de 10 caracteres.");

        RuleFor(x => x.NombreTerminal)
            .NotEmpty().WithMessage("El nombre del terminal es obligatorio.")
            .MaximumLength(150).WithMessage("El nombre del terminal supera el máximo de 150 caracteres.");

        RuleFor(x => x.IdPais)
            .GreaterThan(0).WithMessage("Debe indicar un país válido.")
            .When(x => x.IdPais.HasValue);

        RuleFor(x => x.ConfigScraper)
            .MaximumLength(500).WithMessage("La configuración del scraper supera el máximo de 500 caracteres.");
    }
}
