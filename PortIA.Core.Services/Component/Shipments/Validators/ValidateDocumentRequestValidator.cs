namespace PortIA.Core.Services.Component.Shipments.Validators;

using FluentValidation;
using PortIA.Core.Services.Component.Shipments.DTOs;

public class ValidateDocumentRequestValidator : AbstractValidator<ValidateDocumentRequest>
{
    public ValidateDocumentRequestValidator()
    {
        RuleFor(x => x.ContainerNumber)
            .NotEmpty()
            .WithMessage("Debe indicar el número de contenedor.")
            .Matches(@"^[A-Z]{4}\d{7}$")
            .WithMessage("El número de contenedor debe tener el formato estándar: 4 letras y 7 dígitos (ej: MSCU1234567).");
    }
}
