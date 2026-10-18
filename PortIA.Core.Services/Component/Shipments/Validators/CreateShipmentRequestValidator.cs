namespace PortIA.Core.Services.Component.Shipments.Validators;

using FluentValidation;
using PortIA.Core.Services.Component.Shipments.DTOs;

public class CreateShipmentRequestValidator : AbstractValidator<CreateShipmentRequest>
{
    private static readonly string[] AllowedExtensions = [".pdf", ".png", ".jpg", ".jpeg"];
    private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB

    public CreateShipmentRequestValidator()
    {
        RuleFor(x => x.FileSize)
            .GreaterThan(0)
            .WithMessage("El archivo está vacío o es nulo.");

        RuleFor(x => x.FileSize)
            .LessThanOrEqualTo(MaxFileSizeBytes)
            .WithMessage("El archivo supera el tamaño máximo permitido de 10 MB.")
            .When(x => x.FileSize > 0);

        RuleFor(x => x.FileName)
            .Must(fileName => AllowedExtensions.Contains(Path.GetExtension(fileName).ToLowerInvariant()))
            .WithMessage("Formato de archivo no permitido. Solo se aceptan PDF, PNG y JPG.");
    }
}
