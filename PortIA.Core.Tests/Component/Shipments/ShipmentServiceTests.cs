namespace PortIA.Core.Tests.Component.Shipments;

using System.Text;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using MockQueryable.NSubstitute;
using NSubstitute;
using PortIA.Core.Infrastructure.Data.Entities;
using PortIA.Core.Infrastructure.Data.Interfaces;
using PortIA.Core.Infrastructure.Data.UnitOfWork;
using PortIA.Core.Services.Common.Results;
using PortIA.Core.Services.Component.Mensajeria.Events;
using PortIA.Core.Services.Component.Shipments.DTOs;
using PortIA.Core.Services.Component.Shipments.Services;
using PortIA.Core.ExternalServices.Component.Storage.Interfaces;
using IPublishEndpoint = MassTransit.IPublishEndpoint;

public class ShipmentServiceTests
{
    private readonly ICargaDocumentosRepository _cargaDocumentosRepository;
    private readonly IDocumentoRepository _documentoRepository;
    private readonly IContainerRepository _containerRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMinioStorageClient _minioStorageService;
    private readonly IPublishEndpoint _publishEndpoint;
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

        _minioStorageService = Substitute.For<IMinioStorageClient>();
        _publishEndpoint = Substitute.For<IPublishEndpoint>();
        _createShipmentValidator = Substitute.For<IValidator<CreateShipmentRequest>>();

        _sut = new ShipmentService(_unitOfWork, _minioStorageService, _createShipmentValidator, _publishEndpoint, Substitute.For<ILogger<ShipmentService>>());

        var emptyDocumentos = new List<Documento>().BuildMockDbSet();
        _documentoRepository.Query().Returns(emptyDocumentos);

        var emptyContainers = new List<Container>().BuildMockDbSet();
        _containerRepository.Query().Returns(emptyContainers);
    }

    private static ShipmentFileInput BuildFileInput(string fileName = "archivo.pdf", string contenido = "contenido-de-prueba") =>
        new(new MemoryStream(Encoding.UTF8.GetBytes(contenido)), fileName, "application/pdf", contenido.Length);

    private void SetValidationResult(bool isValid, string? errorMessage = null)
    {
        var failures = isValid
            ? []
            : new List<ValidationFailure> { new("FileSize", errorMessage ?? "Inválido") };

        _createShipmentValidator
            .ValidateAsync(Arg.Any<CreateShipmentRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult(failures));
    }

    private void SetupSuccessfulPersistence(int cargaId = 10, params int[] documentIds)
    {
        _documentoRepository.GetByHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((Documento?)null);
        _minioStorageService
            .UploadFileAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ci => $"documentos/{ci.ArgAt<string>(1)}");
        _minioStorageService
            .GetPresignedUrlAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(ci => $"https://minio.local/{ci.ArgAt<string>(0)}");

        _cargaDocumentosRepository
            .AddAsync(Arg.Any<CargaDocumentos>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask)
            .AndDoes(callInfo => callInfo.Arg<CargaDocumentos>().Id = cargaId);

        var idsRestantes = new Queue<int>(documentIds);
        _documentoRepository
            .AddAsync(Arg.Any<Documento>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask)
            .AndDoes(callInfo => callInfo.Arg<Documento>().Id = idsRestantes.Dequeue());
    }

    // ---------- CreateShipmentAsync ----------

    [Fact]
    public async Task CreateShipmentAsync_WithNullFiles_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _sut.CreateShipmentAsync(null!, userId: 1, CancellationToken.None));
    }

    [Fact]
    public async Task CreateShipmentAsync_WithEmptyFileList_ReturnsValidationError()
    {
        var result = await _sut.CreateShipmentAsync([], userId: 1, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.ValidationError, result.Status);
        Assert.Contains("files", result.ValidationErrors!.Keys);
    }

    [Fact]
    public async Task CreateShipmentAsync_WithSingleInvalidFile_ReturnsValidationErrorAndDoesNotUpload()
    {
        SetValidationResult(isValid: false, errorMessage: "El archivo está vacío o es nulo.");

        var result = await _sut.CreateShipmentAsync([BuildFileInput()], userId: 1, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.ValidationError, result.Status);
        await _minioStorageService.DidNotReceive().UploadFileAsync(
            Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateShipmentAsync_WithSingleDuplicateFile_ReturnsValidationErrorAndDoesNotUpload()
    {
        SetValidationResult(isValid: true);

        var existingDocument = new Documento { Id = 5, IdCargaDocumentos = 42, HuellaHash = "hash-existente" };
        _documentoRepository
            .GetByHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(existingDocument);

        var result = await _sut.CreateShipmentAsync([BuildFileInput()], userId: 1, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.ValidationError, result.Status);
        await _minioStorageService.DidNotReceive().UploadFileAsync(
            Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateShipmentAsync_WithSingleValidFile_ReturnsSuccessWithCreatedShipment()
    {
        SetValidationResult(isValid: true);
        SetupSuccessfulPersistence(cargaId: 10, documentIds: 20);

        var result = await _sut.CreateShipmentAsync([BuildFileInput()], userId: 7, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(10, result.Value.ShipmentId);
        Assert.Equal("PROCESANDO", result.Value.Status);
        Assert.Single(result.Value.Documents);
        Assert.Equal(20, result.Value.Documents[0].DocumentId);
        Assert.Equal("PENDIENTE", result.Value.Documents[0].Status);
    }

    [Fact]
    public async Task CreateShipmentAsync_WithMultipleValidFiles_CreatesOneCargaWithAllDocuments()
    {
        SetValidationResult(isValid: true);
        SetupSuccessfulPersistence(cargaId: 10, documentIds: [20, 21, 22]);

        var files = new[]
        {
            BuildFileInput("a.pdf", "contenido-a"),
            BuildFileInput("b.pdf", "contenido-b"),
            BuildFileInput("c.pdf", "contenido-c"),
        };
        var result = await _sut.CreateShipmentAsync(files, userId: 7, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(10, result.Value.ShipmentId);
        Assert.Equal(3, result.Value.Documents.Count);
        Assert.All(result.Value.Documents, d => Assert.Equal("PENDIENTE", d.Status));
        await _cargaDocumentosRepository.Received(1).AddAsync(Arg.Any<CargaDocumentos>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateShipmentAsync_WithMultipleValidFiles_PublishesOneShipmentCreatedPerDocument()
    {
        SetValidationResult(isValid: true);
        SetupSuccessfulPersistence(cargaId: 10, documentIds: [20, 21, 22]);

        var files = new[]
        {
            BuildFileInput("a.pdf", "contenido-a"),
            BuildFileInput("b.pdf", "contenido-b"),
            BuildFileInput("c.pdf", "contenido-c"),
        };

        var result = await _sut.CreateShipmentAsync(files, userId: 7, CancellationToken.None);

        Assert.True(result.IsSuccess);
        await _publishEndpoint.Received(3).Publish(
            Arg.Is<ShipmentCreated>(e => e.ShipmentId == 10 && e.DocumentType == "BL"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateShipmentAsync_WithSameFileTwiceInSameBatch_MarksSecondOneAsDuplicateAndKeepsFirst()
    {
        SetValidationResult(isValid: true);
        SetupSuccessfulPersistence(cargaId: 10, documentIds: 20);

        var files = new[] { BuildFileInput("a.pdf", "mismo-contenido"), BuildFileInput("b.pdf", "mismo-contenido") };
        var result = await _sut.CreateShipmentAsync(files, userId: 7, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Documents.Count);
        Assert.Equal("PENDIENTE", result.Value.Documents.Single(d => d.FileName == "a.pdf").Status);
        Assert.Equal("DUPLICADO", result.Value.Documents.Single(d => d.FileName == "b.pdf").Status);
    }

    [Fact]
    public async Task CreateShipmentAsync_WithMixOfValidAndInvalidFiles_PersistsOnlyValidOnesAndReportsBoth()
    {
        _createShipmentValidator
            .ValidateAsync(Arg.Is<CreateShipmentRequest>(r => r.FileName == "bueno.pdf"), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());
        _createShipmentValidator
            .ValidateAsync(Arg.Is<CreateShipmentRequest>(r => r.FileName == "malo.pdf"), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult(new List<ValidationFailure> { new("FileName", "Formato no permitido.") }));

        SetupSuccessfulPersistence(cargaId: 10, documentIds: 20);

        var files = new[] { BuildFileInput("bueno.pdf"), BuildFileInput("malo.pdf") };
        var result = await _sut.CreateShipmentAsync(files, userId: 7, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Documents.Count);
        Assert.Equal("PENDIENTE", result.Value.Documents.Single(d => d.FileName == "bueno.pdf").Status);
        Assert.Equal("RECHAZADO", result.Value.Documents.Single(d => d.FileName == "malo.pdf").Status);
    }

    [Fact]
    public async Task CreateShipmentAsync_WhenAllFilesAreInvalid_ReturnsValidationErrorWithoutCreatingCarga()
    {
        SetValidationResult(isValid: false, errorMessage: "Formato de archivo no permitido.");

        var files = new[] { BuildFileInput("a.pdf"), BuildFileInput("b.pdf") };
        var result = await _sut.CreateShipmentAsync(files, userId: 7, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.ValidationError, result.Status);
        Assert.Equal(2, result.ValidationErrors!.Count);
        await _cargaDocumentosRepository.DidNotReceive().AddAsync(Arg.Any<CargaDocumentos>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateShipmentAsync_WhenPersistenceFails_RemovesUploadedFilesAndRethrows()
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
            .Returns(Task.FromException(new InvalidOperationException("La base de datos no responde.")));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sut.CreateShipmentAsync([BuildFileInput()], userId: 7, CancellationToken.None));

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
            new() { Id = 2, IdCargaDocumentos = 123, TipoDocumento = "BL", StorageUrl = "docs/b.pdf", Confidence = 0.4f, EstadoExtraccion = "PENDIENTE_REVISION", ContieneDatosPersonales = false }
        };
        var mockDocumentos = documentos.BuildMockDbSet();
        _documentoRepository.Query().Returns(mockDocumentos);

        var result = await _sut.GetShipmentByIdAsync(id: 123, userId: 7, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Documents.Count);
        Assert.Equal("COMPLETO", result.Value.Documents.Single(d => d.Id == 1).ExtractionStatus);
        Assert.Equal("PENDIENTE_REVISION", result.Value.Documents.Single(d => d.Id == 2).ExtractionStatus);
    }
}
