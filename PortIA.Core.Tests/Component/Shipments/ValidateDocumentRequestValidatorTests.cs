namespace PortIA.Core.Tests.Component.Shipments;

using PortIA.Core.Services.Component.Shipments.DTOs;
using PortIA.Core.Services.Component.Shipments.Validators;

public class ValidateDocumentRequestValidatorTests
{
    private readonly ValidateDocumentRequestValidator _sut = new();

    private static ValidateDocumentRequest BuildRequest(string containerNumber) =>
        new(containerNumber, "BL-1", "MSC", "San Antonio", DateTimeOffset.UtcNow, 7, 5, "Agencia");

    [Theory]
    [InlineData("MSCU1234567")]
    [InlineData("TCLU9876543")]
    [InlineData("CMAU0000001")]
    public void Validate_WithValidContainerNumberFormat_HasNoErrorsForContainerNumber(string containerNumber)
    {
        var result = _sut.Validate(BuildRequest(containerNumber));

        Assert.DoesNotContain(result.Errors, e => e.PropertyName == nameof(ValidateDocumentRequest.ContainerNumber));
    }

    [Theory]
    [InlineData("")]
    [InlineData("MSCU123456")]
    [InlineData("MSCU12345678")]
    [InlineData("MSC1234567")]
    [InlineData("mscu1234567")]
    [InlineData("MSCU123456A")]
    [InlineData("1234567MSCU")]
    public void Validate_WithInvalidContainerNumberFormat_HasErrorForContainerNumber(string containerNumber)
    {
        var result = _sut.Validate(BuildRequest(containerNumber));

        Assert.Contains(result.Errors, e => e.PropertyName == nameof(ValidateDocumentRequest.ContainerNumber));
    }
}
