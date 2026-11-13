namespace PortIA.Core.Tests.Component.Shipments;

using System.Text.Json;
using FluentValidation;
using Microsoft.Extensions.Logging;
using MockQueryable.NSubstitute;
using NSubstitute;
using PortIA.Core.Infrastructure.Data.Entities;
using PortIA.Core.Infrastructure.Data.Interfaces;
using PortIA.Core.Infrastructure.Data.UnitOfWork;
using PortIA.Core.Services.Common.Results;
using PortIA.Core.Services.Component.Mensajeria.Publishers.Interfaces;
using PortIA.Core.Services.Component.Shipments.DTOs;
using PortIA.Core.Services.Component.Shipments.Services;
using PortIA.Core.Services.Component.Shipments.Validators;

public class DocumentReviewServiceTests
{
    private readonly IDocumentoRepository _documentoRepository;
    private readonly ICargaDocumentosRepository _cargaDocumentosRepository;
    private readonly IContainerRepository _containerRepository;
    private readonly ITerminalRepository _terminalRepository;
    private readonly IAccionRepository _accionRepository;
    private readonly ILogAuditoriaRepository _logAuditoriaRepository;
    private readonly IHitoRepository _hitoRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IInspectContainerPublisher _inspectContainerPublisher;
    private readonly DocumentReviewService _sut;

    public DocumentReviewServiceTests()
    {
        _documentoRepository = Substitute.For<IDocumentoRepository>();
        _cargaDocumentosRepository = Substitute.For<ICargaDocumentosRepository>();
        _containerRepository = Substitute.For<IContainerRepository>();
        _terminalRepository = Substitute.For<ITerminalRepository>();
        _accionRepository = Substitute.For<IAccionRepository>();
        _logAuditoriaRepository = Substitute.For<ILogAuditoriaRepository>();
        _hitoRepository = Substitute.For<IHitoRepository>();

        _unitOfWork = Substitute.For<IUnitOfWork>();
        _unitOfWork.Documentos.Returns(_documentoRepository);
        _unitOfWork.CargasDocumentos.Returns(_cargaDocumentosRepository);
        _unitOfWork.Containers.Returns(_containerRepository);
        _unitOfWork.Terminales.Returns(_terminalRepository);
        _unitOfWork.Acciones.Returns(_accionRepository);
        _unitOfWork.LogsAuditoria.Returns(_logAuditoriaRepository);
        _unitOfWork.Hitos.Returns(_hitoRepository);

        _accionRepository.GetByDescripcionAsync("ACTUALIZAR", Arg.Any<CancellationToken>())
            .Returns(new Accion { Id = 2, Descripcion = "ACTUALIZAR" });

        var terminalesMock = new List<Terminal>().BuildMockDbSet();
        _terminalRepository.Query().Returns(terminalesMock);

        var containersMock = new List<Container>().BuildMockDbSet();
        _containerRepository.Query().Returns(containersMock);

        _containerRepository
            .AddAsync(Arg.Any<Container>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask)
            .AndDoes(ci => ci.Arg<Container>().Id = 50);

        _inspectContainerPublisher = Substitute.For<IInspectContainerPublisher>();
        _inspectContainerPublisher.PublishAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(Result.Success());

        _sut = new DocumentReviewService(
            _unitOfWork, _inspectContainerPublisher, new ValidateDocumentRequestValidator(), Substitute.For<ILogger<DocumentReviewService>>());
    }

    private static ValidateDocumentRequest BuildRequest(string containerNumber = "MSCU1234567") =>
        new(containerNumber, "BL-1", "MSC", "San Antonio", DateTimeOffset.UtcNow, 7, 5, "Agencia");

    private static Documento BuildDocumentoPendiente(int id = 1, int idCarga = 10) => new()
    {
        Id = id,
        IdCargaDocumentos = idCarga,
        TipoDocumento = "BL",
        StorageUrl = "docs/a.pdf",
        HuellaHash = "hash",
        EstadoExtraccion = "PENDIENTE_REVISION",
        DatosExtraidosRaw = JsonSerializer.Serialize(new ExtractedDataSnapshot("MSCU0000000", "BL-IA", "MSC", "San Antonio", null, 5, 5, "Agencia IA")),
    };

    [Fact]
    public async Task ValidarAsync_WithInvalidContainerNumberFormat_ReturnsValidationError()
    {
        var result = await _sut.ValidarAsync(1, userId: 7, BuildRequest("malformado"), CancellationToken.None);

        Assert.Equal(ResultStatus.ValidationError, result.Status);
        await _containerRepository.DidNotReceive().AddAsync(Arg.Any<Container>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ValidarAsync_WhenDocumentDoesNotExist_ReturnsNotFound()
    {
        _documentoRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns((Documento?)null);

        var result = await _sut.ValidarAsync(1, userId: 7, BuildRequest(), CancellationToken.None);

        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task ValidarAsync_WhenDocumentBelongsToAnotherUser_ReturnsNotFound()
    {
        _documentoRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(BuildDocumentoPendiente());
        _cargaDocumentosRepository.GetByIdForUserAsync(10, 7, Arg.Any<CancellationToken>()).Returns((CargaDocumentos?)null);

        var result = await _sut.ValidarAsync(1, userId: 7, BuildRequest(), CancellationToken.None);

        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task ValidarAsync_WhenDocumentNotPendingReview_ReturnsBusinessError()
    {
        var documento = BuildDocumentoPendiente();
        documento.EstadoExtraccion = "COMPLETO";
        _documentoRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(documento);
        _cargaDocumentosRepository.GetByIdForUserAsync(10, 7, Arg.Any<CancellationToken>()).Returns(new CargaDocumentos { Id = 10, UserId = 7 });

        var result = await _sut.ValidarAsync(1, userId: 7, BuildRequest(), CancellationToken.None);

        Assert.Equal(ResultStatus.BusinessError, result.Status);
    }

    [Fact]
    public async Task ValidarAsync_WhenContainerNumberAlreadyRegistered_ReturnsConflict()
    {
        _documentoRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(BuildDocumentoPendiente());
        _cargaDocumentosRepository.GetByIdForUserAsync(10, 7, Arg.Any<CancellationToken>()).Returns(new CargaDocumentos { Id = 10, UserId = 7 });

        var existentes = new List<Container> { new() { Id = 99, NumeroContainer = "MSCU1234567" } };
        var existentesMock = existentes.BuildMockDbSet();
        _containerRepository.Query().Returns(existentesMock);

        var result = await _sut.ValidarAsync(1, userId: 7, BuildRequest("MSCU1234567"), CancellationToken.None);

        Assert.Equal(ResultStatus.Conflict, result.Status);
        await _containerRepository.DidNotReceive().AddAsync(Arg.Any<Container>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ValidarAsync_WhenValid_CreatesContainerMarksDocumentValidatedAndTriggersInspection()
    {
        _documentoRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(BuildDocumentoPendiente());
        _cargaDocumentosRepository.GetByIdForUserAsync(10, 7, Arg.Any<CancellationToken>()).Returns(new CargaDocumentos { Id = 10, UserId = 7 });

        var result = await _sut.ValidarAsync(1, userId: 7, BuildRequest("MSCU1234567"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(50, result.Value.ContainerId);
        await _containerRepository.Received(1).AddAsync(
            Arg.Is<Container>(c => c.NumeroContainer == "MSCU1234567" && c.IdCargaDocumentos == 10),
            Arg.Any<CancellationToken>());
        await _inspectContainerPublisher.Received(1).PublishAsync(50, Arg.Any<CancellationToken>());
        await _hitoRepository.Received(1).AddAsync(
            Arg.Is<Hito>(h => h.IdContainer == 50 && h.Origen == "USUARIO" && h.CreadoPor == 7),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ValidarAsync_WhenValid_RegistersAuditWithOriginalIaValueAndCorrectedValue()
    {
        var documento = BuildDocumentoPendiente();
        _documentoRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(documento);
        _cargaDocumentosRepository.GetByIdForUserAsync(10, 7, Arg.Any<CancellationToken>()).Returns(new CargaDocumentos { Id = 10, UserId = 7 });

        await _sut.ValidarAsync(1, userId: 7, BuildRequest("MSCU1234567"), CancellationToken.None);

        await _logAuditoriaRepository.Received(1).AddAsync(
            Arg.Is<LogAuditoria>(l =>
                l.NombreEntidad == nameof(Documento) &&
                l.IdEntidad == 1 &&
                l.Responsable == 7 &&
                l.ValoresAnteriores!.Contains("MSCU0000000") &&
                l.ValoresNuevos!.Contains("MSCU1234567")),
            Arg.Any<CancellationToken>());
        Assert.Equal("VALIDADO", documento.EstadoExtraccion);
    }

    [Fact]
    public async Task GetPendientesAsync_ReturnsOnlyPendingDocumentsForUser()
    {
        var documentos = new List<Documento>
        {
            new()
            {
                Id = 1, IdCargaDocumentos = 10, TipoDocumento = "BL", StorageUrl = "a.pdf", HuellaHash = "h1",
                EstadoExtraccion = "PENDIENTE_REVISION", Confidence = 0.4f,
                DatosExtraidosRaw = JsonSerializer.Serialize(new ExtractedDataSnapshot("MSCU0000000", null, null, null, null, null, null, null)),
                CargaDocumentos = new CargaDocumentos { Id = 10, UserId = 7 },
            },
            new()
            {
                Id = 2, IdCargaDocumentos = 11, TipoDocumento = "BL", StorageUrl = "b.pdf", HuellaHash = "h2",
                EstadoExtraccion = "COMPLETO", Confidence = 0.95f,
                CargaDocumentos = new CargaDocumentos { Id = 11, UserId = 7 },
            },
            new()
            {
                Id = 3, IdCargaDocumentos = 12, TipoDocumento = "BL", StorageUrl = "c.pdf", HuellaHash = "h3",
                EstadoExtraccion = "PENDIENTE_REVISION", Confidence = 0.3f,
                CargaDocumentos = new CargaDocumentos { Id = 12, UserId = 99 },
            },
        };
        var documentosMock = documentos.BuildMockDbSet();
        _documentoRepository.Query().Returns(documentosMock);

        var result = await _sut.GetPendientesAsync(userId: 7, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value);
        Assert.Equal(1, result.Value[0].DocumentId);
        Assert.Equal("MSCU0000000", result.Value[0].ContainerNumberSugerido);
    }
}
