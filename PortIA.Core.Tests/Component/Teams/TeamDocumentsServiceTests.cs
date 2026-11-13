namespace PortIA.Core.Tests.Component.Teams;

using FluentValidation;
using FluentValidation.Results;
using MockQueryable.NSubstitute;
using NSubstitute;
using PortIA.Core.ExternalServices.Component.Storage.Interfaces;
using PortIA.Core.Infrastructure.Data.Entities;
using PortIA.Core.Infrastructure.Data.Interfaces;
using PortIA.Core.Infrastructure.Data.UnitOfWork;
using PortIA.Core.Services.Common.Results;
using PortIA.Core.Services.Component.Teams.DTOs;
using PortIA.Core.Services.Component.Teams.Services;

public class TeamDocumentsServiceTests
{
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IDocumentoRepository _documentoRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMinioStorageClient _minioStorageClient;
    private readonly IValidator<TeamDocumentsFilter> _validator;
    private readonly TeamDocumentsService _sut;

    public TeamDocumentsServiceTests()
    {
        _usuarioRepository = Substitute.For<IUsuarioRepository>();
        _documentoRepository = Substitute.For<IDocumentoRepository>();

        _unitOfWork = Substitute.For<IUnitOfWork>();
        _unitOfWork.Usuarios.Returns(_usuarioRepository);
        _unitOfWork.Documentos.Returns(_documentoRepository);

        _minioStorageClient = Substitute.For<IMinioStorageClient>();
        _minioStorageClient.GetPresignedUrlAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("https://minio.local/presigned");

        _validator = Substitute.For<IValidator<TeamDocumentsFilter>>();
        _validator.ValidateAsync(Arg.Any<TeamDocumentsFilter>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());

        _sut = new TeamDocumentsService(_unitOfWork, _minioStorageClient, _validator);

        var emptyDocumentos = new List<Documento>().BuildMockDbSet();
        _documentoRepository.Query().Returns(emptyDocumentos);
    }

    private static Usuario BuildJefe(int id) => new()
    {
        Id = id,
        NombreUsuario = $"Jefe {id}",
        Rol = new Rol { Id = 2, NombreRol = "Jefe" }
    };

    private static (CargaDocumentos Carga, Usuario Analista) BuildCarga(int analistaId, int jefeId)
    {
        var analista = new Usuario { Id = analistaId, NombreUsuario = $"Analista {analistaId}", JefeId = jefeId };
        var carga = new CargaDocumentos { Id = 1, UserId = analistaId, Usuario = analista };
        return (carga, analista);
    }

    [Fact]
    public async Task GetDocumentsAsync_WithInvalidFilter_ReturnsValidationError()
    {
        _validator.ValidateAsync(Arg.Any<TeamDocumentsFilter>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult(new[] { new ValidationFailure("FromDate", "inválido") }));

        var result = await _sut.GetDocumentsAsync(2, callerUserId: 2, callerRole: "Jefe", new TeamDocumentsFilter(), CancellationToken.None);

        Assert.Equal(ResultStatus.ValidationError, result.Status);
    }

    [Fact]
    public async Task GetDocumentsAsync_WithUnauthorizedRole_ReturnsUnauthorized()
    {
        var result = await _sut.GetDocumentsAsync(2, callerUserId: 1, callerRole: "Analista", new TeamDocumentsFilter(), CancellationToken.None);

        Assert.Equal(ResultStatus.Unauthorized, result.Status);
    }

    [Fact]
    public async Task GetDocumentsAsync_WhenJefeNotFound_ReturnsNotFound()
    {
        _usuarioRepository.GetByIdWithRoleAsync(2, Arg.Any<CancellationToken>()).Returns((Usuario?)null);

        var result = await _sut.GetDocumentsAsync(2, callerUserId: 2, callerRole: "Jefe", new TeamDocumentsFilter(), CancellationToken.None);

        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task GetDocumentsAsync_AsJefe_IgnoresRouteJefeIdAndUsesOwnId()
    {
        _usuarioRepository.GetByIdWithRoleAsync(2, Arg.Any<CancellationToken>()).Returns(BuildJefe(2));

        var result = await _sut.GetDocumentsAsync(7, callerUserId: 2, callerRole: "Jefe", new TeamDocumentsFilter(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        await _usuarioRepository.Received(1).GetByIdWithRoleAsync(2, Arg.Any<CancellationToken>());
        await _usuarioRepository.DidNotReceive().GetByIdWithRoleAsync(7, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetDocumentsAsync_ReturnsOnlyDocumentsFromOwnTeam_WithDownloadUrlAndContainer()
    {
        var jefe = BuildJefe(2);
        var (carga, analista) = BuildCarga(analistaId: 1, jefeId: 2);
        var (otraCarga, otroAnalista) = BuildCarga(analistaId: 99, jefeId: 50);

        var container = new Container { Id = 10, NumeroContainer = "MSCU1234567" };
        carga.Containers = new List<Container> { container };

        var documentos = new List<Documento>
        {
            new() { Id = 1, TipoDocumento = "BL", StorageUrl = "bucket/doc1", EstadoExtraccion = "VALIDADO", CargaDocumentos = carga, IdCargaDocumentos = carga.Id },
            new() { Id = 2, TipoDocumento = "BL", StorageUrl = "bucket/doc2", EstadoExtraccion = "PENDIENTE_REVISION", CargaDocumentos = otraCarga, IdCargaDocumentos = otraCarga.Id },
        };

        _usuarioRepository.GetByIdWithRoleAsync(2, Arg.Any<CancellationToken>()).Returns(jefe);
        var mockDocs = documentos.BuildMockDbSet();
        _documentoRepository.Query().Returns(mockDocs);

        var result = await _sut.GetDocumentsAsync(2, callerUserId: 2, callerRole: "Jefe", new TeamDocumentsFilter(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var doc = Assert.Single(result.Value);
        Assert.Equal(1, doc.Id);
        Assert.Equal(1, doc.AnalystId);
        Assert.Equal(10, doc.ContainerId);
        Assert.Equal("MSCU1234567", doc.ContainerNumber);
        Assert.Equal("https://minio.local/presigned", doc.DownloadUrl);
    }

    [Fact]
    public async Task GetDocumentsAsync_FiltersByAnalystId()
    {
        var jefe = BuildJefe(2);
        var (cargaCamila, _) = BuildCarga(analistaId: 1, jefeId: 2);
        var (cargaDiego, _) = BuildCarga(analistaId: 5, jefeId: 2);

        var documentos = new List<Documento>
        {
            new() { Id = 1, TipoDocumento = "BL", StorageUrl = "bucket/doc1", EstadoExtraccion = "VALIDADO", CargaDocumentos = cargaCamila, IdCargaDocumentos = cargaCamila.Id },
            new() { Id = 2, TipoDocumento = "BL", StorageUrl = "bucket/doc2", EstadoExtraccion = "VALIDADO", CargaDocumentos = cargaDiego, IdCargaDocumentos = cargaDiego.Id },
        };

        _usuarioRepository.GetByIdWithRoleAsync(2, Arg.Any<CancellationToken>()).Returns(jefe);
        var mockDocs = documentos.BuildMockDbSet();
        _documentoRepository.Query().Returns(mockDocs);

        var filter = new TeamDocumentsFilter { AnalystId = 5 };
        var result = await _sut.GetDocumentsAsync(2, callerUserId: 2, callerRole: "Jefe", filter, CancellationToken.None);

        Assert.True(result.IsSuccess);
        var doc = Assert.Single(result.Value);
        Assert.Equal(5, doc.AnalystId);
    }
}
