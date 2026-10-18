namespace PortIA.Core.Tests.Component.Shipments;

using System.Text;
using FluentValidation;
using FluentValidation.Results;
using MockQueryable.NSubstitute;
using NSubstitute;
using PortIA.Core.Infrastructure.Data.Entities;
using PortIA.Core.Infrastructure.Data.Interfaces;
using PortIA.Core.Infrastructure.Data.UnitOfWork;
using PortIA.Core.Services.Common.Results;
using PortIA.Core.Services.Component.Shipments.DTOs;
using PortIA.Core.Services.Component.Shipments.Services;
using PortIA.Core.Services.Component.Storage.Interfaces;

public class ShipmentServiceTests
{
    private readonly ICargaDocumentosRepository _cargaDocumentosRepository;
    private readonly IDocumentoRepository _documentoRepository;
    private readonly IContainerRepository _containerRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMinioStorageService _minioStorageService;
    private readonly IValidator<CreateShipmentRequest> _createShipmentValidator;
    private readonly ShipmentService _sut;

    public ShipmentServiceTests()
    {
        _cargaDocumentosRepository = Substitute.For<ICargaDocumentosRepository>();
        _documentoRepository = Substitute.For<IDocumentoRepository>();
        _containerRepository = Substitute.For<IContainerRepository>();

        _unitOfWork = Substitute.For<IUnitOfWork>();
        _unitOfWork.CargasDocumentos.Returns(_cargaDocumentosRepository);
        _unitOfWork.Documentos.Returns(_documentoRepository);
        _unitOfWork.Containers.Returns(_containerRepository);

        _minioStorageService = Substitute.For<IMinioStorageService>();
        _createShipmentValidator = Substitute.For<IValidator<CreateShipmentRequest>>();

        _sut = new ShipmentService(_unitOfWork, _minioStorageService, _createShipmentValidator);

        var emptyDocumentos = new List<Documento>().BuildMockDbSet();
        _documentoRepository.Query().Returns(emptyDocumentos);

        var emptyContainers = new List<Container>().BuildMockDbSet();
        _containerRepository.Query().Returns(emptyContainers);
    }

    private static MemoryStream CreateFakeFileStream() => new(Encoding.UTF8.GetBytes("contenido-de-prueba"));

    private void SetValidationResult(bool isValid, string? propertyName = null, string? errorMessage = null)
    {
        var failures = isValid
            ? []
            : new List<ValidationFailure> { new(propertyName ?? "FileSize", errorMessage ?? "Inválido") };

        _createShipmentValidator
            .ValidateAsync(Arg.Any<CreateShipmentRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult(failures));
    }

    // ---------- CreateShipmentAsync ----------

    [Fact]
    public async Task CreateShipmentAsync_WithNullStream_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _sut.CreateShipmentAsync(null!, "archivo.pdf", "application/pdf", 1024, userId: 1, CancellationToken.None));
    }

    [Fact]
    public async Task CreateShipmentAsync_WithInvalidRequest_ReturnsValidationError()
    {
        SetValidationResult(isValid: false, propertyName: "FileSize", errorMessage: "El archivo está vacío o es nulo.");

        using var fileStream = CreateFakeFileStream();
        var result = await _sut.CreateShipmentAsync(fileStream, "archivo.pdf", "application/pdf", 0, userId: 1, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.ValidationError, result.Status);
        Assert.Contains("FileSize", result.ValidationErrors!.Keys);
        await _minioStorageService.DidNotReceive().UploadFileAsync(
            Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateShipmentAsync_WithDuplicateHash_ReturnsBusinessError()
    {
        SetValidationResult(isValid: true);

        var existingDocument = new Documento { Id = 5, IdCargaDocumentos = 42, HuellaHash = "hash-existente" };
        _documentoRepository
            .GetByHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(existingDocument);

        using var fileStream = CreateFakeFileStream();
        var result = await _sut.CreateShipmentAsync(fileStream, "archivo.pdf", "application/pdf", 1024, userId: 1, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.BusinessError, result.Status);
        Assert.Contains("#42", result.Error);
        await _minioStorageService.DidNotReceive().UploadFileAsync(
            Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateShipmentAsync_WithValidNewFile_ReturnsSuccessWithCreatedShipment()
    {
        SetValidationResult(isValid: true);
        _documentoRepository.GetByHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((Documento?)null);
        _minioStorageService
            .UploadFileAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("documentos/archivo.pdf");

        _cargaDocumentosRepository
            .AddAsync(Arg.Any<CargaDocumentos>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask)
            .AndDoes(callInfo => callInfo.Arg<CargaDocumentos>().Id = 10);

        _documentoRepository
            .AddAsync(Arg.Any<Documento>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask)
            .AndDoes(callInfo => callInfo.Arg<Documento>().Id = 20);

        using var fileStream = CreateFakeFileStream();
        var result = await _sut.CreateShipmentAsync(fileStream, "archivo.pdf", "application/pdf", 1024, userId: 7, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(10, result.Value.ShipmentId);
        Assert.Equal(20, result.Value.DocumentId);
        Assert.Equal("PROCESANDO", result.Value.Status);
    }

    [Fact]
    public async Task CreateShipmentAsync_WhenPersistenceFails_RemovesUploadedFileAndRethrows()
    {
        SetValidationResult(isValid: true);
        _documentoRepository.GetByHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((Documento?)null);
        _minioStorageService
            .UploadFileAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("documentos/archivo.pdf");

        _cargaDocumentosRepository
            .AddAsync(Arg.Any<CargaDocumentos>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new InvalidOperationException("La base de datos no responde.")));

        using var fileStream = CreateFakeFileStream();
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sut.CreateShipmentAsync(fileStream, "archivo.pdf", "application/pdf", 1024, userId: 7, CancellationToken.None));

        await _minioStorageService.Received(1).RemoveFileAsync("documentos/archivo.pdf", Arg.Any<CancellationToken>());
    }

    // ---------- GetShipmentsAsync ----------

    private void SetCargasQueryResult(List<CargaDocumentos> cargas)
    {
        var mockQueryable = cargas.BuildMockDbSet();
        _cargaDocumentosRepository.Query().Returns(mockQueryable);
    }

    private static List<CargaDocumentos> BuildCargasDeDosUsuarios()
    {
        var now = DateTimeOffset.UtcNow;
        return
        [
            new CargaDocumentos { Id = 1, UserId = 7, Status = "COMPLETO", CreacionFecha = now.AddDays(-2), UpdateAt = now.AddDays(-2) },
            new CargaDocumentos { Id = 2, UserId = 7, Status = "PROCESANDO", CreacionFecha = now.AddDays(-1), UpdateAt = now.AddDays(-1) },
            new CargaDocumentos { Id = 3, UserId = 7, Status = "COMPLETO", CreacionFecha = now, UpdateAt = now },
            new CargaDocumentos { Id = 99, UserId = 1, Status = "COMPLETO", CreacionFecha = now, UpdateAt = now }
        ];
    }

    [Fact]
    public async Task GetShipmentsAsync_ReturnsOnlyShipmentsBelongingToUser()
    {
        SetCargasQueryResult(BuildCargasDeDosUsuarios());

        var result = await _sut.GetShipmentsAsync(userId: 7, status: null, page: 1, pageSize: 20, sort: null, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value.TotalItems);
        Assert.All(result.Value.Items, item => Assert.NotEqual(99, item.Id));
    }

    [Fact]
    public async Task GetShipmentsAsync_WithoutSort_OrdersByCreatedAtDescending()
    {
        SetCargasQueryResult(BuildCargasDeDosUsuarios());

        var result = await _sut.GetShipmentsAsync(userId: 7, status: null, page: 1, pageSize: 20, sort: null, CancellationToken.None);

        Assert.Equal(3, result.Value.Items[0].Id);
    }

    [Fact]
    public async Task GetShipmentsAsync_WithCreatedAtSort_OrdersByCreatedAtAscending()
    {
        SetCargasQueryResult(BuildCargasDeDosUsuarios());

        var result = await _sut.GetShipmentsAsync(userId: 7, status: null, page: 1, pageSize: 20, sort: "createdAt", CancellationToken.None);

        Assert.Equal(1, result.Value.Items[0].Id);
    }

    [Fact]
    public async Task GetShipmentsAsync_WithStatusFilter_ReturnsOnlyMatchingStatus()
    {
        SetCargasQueryResult(BuildCargasDeDosUsuarios());

        var result = await _sut.GetShipmentsAsync(userId: 7, status: "completo", page: 1, pageSize: 20, sort: null, CancellationToken.None);

        Assert.Equal(2, result.Value.TotalItems);
        Assert.All(result.Value.Items, item => Assert.Equal("COMPLETO", item.Status));
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-5, 1)]
    [InlineData(3, 3)]
    public async Task GetShipmentsAsync_NormalizesPageNumber(int requestedPage, int expectedPage)
    {
        SetCargasQueryResult(BuildCargasDeDosUsuarios());

        var result = await _sut.GetShipmentsAsync(userId: 7, status: null, page: requestedPage, pageSize: 20, sort: null, CancellationToken.None);

        Assert.Equal(expectedPage, result.Value.Page);
    }

    [Theory]
    [InlineData(0, 20)]
    [InlineData(-10, 20)]
    [InlineData(500, 100)]
    [InlineData(50, 50)]
    public async Task GetShipmentsAsync_NormalizesPageSize(int requestedPageSize, int expectedPageSize)
    {
        SetCargasQueryResult(BuildCargasDeDosUsuarios());

        var result = await _sut.GetShipmentsAsync(userId: 7, status: null, page: 1, pageSize: requestedPageSize, sort: null, CancellationToken.None);

        Assert.Equal(expectedPageSize, result.Value.PageSize);
    }

    // ---------- GetShipmentByIdAsync ----------

    [Fact]
    public async Task GetShipmentByIdAsync_WhenShipmentDoesNotExist_ReturnsNotFound()
    {
        _cargaDocumentosRepository
            .GetByIdForUserAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns((CargaDocumentos?)null);

        var result = await _sut.GetShipmentByIdAsync(id: 123, userId: 7, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task GetShipmentByIdAsync_WhenShipmentExists_ReturnsDetailWithDocuments()
    {
        var carga = new CargaDocumentos { Id = 123, UserId = 7, Status = "COMPLETO" };
        _cargaDocumentosRepository
            .GetByIdForUserAsync(123, 7, Arg.Any<CancellationToken>())
            .Returns(carga);

        var documentos = new List<Documento>
        {
            new() { Id = 1, IdCargaDocumentos = 123, TipoDocumento = "BL", StorageUrl = "docs/a.pdf", Confidence = 0.95f, EstadoExtraccion = "COMPLETO", ContieneDatosPersonales = true },
            new() { Id = 2, IdCargaDocumentos = 123, TipoDocumento = "BL", StorageUrl = "docs/b.pdf", Confidence = 0.4f, EstadoExtraccion = "COMPLETO", ContieneDatosPersonales = false }
        };
        var mockDocumentos = documentos.BuildMockDbSet();
        _documentoRepository.Query().Returns(mockDocumentos);

        var result = await _sut.GetShipmentByIdAsync(id: 123, userId: 7, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Documents.Count);
        Assert.Equal("COMPLETO", result.Value.Documents.Single(d => d.Id == 1).ExtractionStatus);
        Assert.Equal("NEEDS_REVIEW", result.Value.Documents.Single(d => d.Id == 2).ExtractionStatus);
    }
}
