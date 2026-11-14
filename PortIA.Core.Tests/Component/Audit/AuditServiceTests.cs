namespace PortIA.Core.Tests.Component.Audit;

using FluentValidation;
using FluentValidation.Results;
using MockQueryable.NSubstitute;
using NSubstitute;
using PortIA.Core.Infrastructure.Data.Entities;
using PortIA.Core.Infrastructure.Data.Interfaces;
using PortIA.Core.Infrastructure.Data.UnitOfWork;
using PortIA.Core.Services.Common.Results;
using PortIA.Core.Services.Component.Audit.DTOs;
using PortIA.Core.Services.Component.Audit.Services;

public class AuditServiceTests
{
    private readonly ILogAuditoriaRepository _logAuditoriaRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<AuditQueryFilter> _validator;
    private readonly AuditService _sut;

    public AuditServiceTests()
    {
        _logAuditoriaRepository = Substitute.For<ILogAuditoriaRepository>();

        _unitOfWork = Substitute.For<IUnitOfWork>();
        _unitOfWork.LogsAuditoria.Returns(_logAuditoriaRepository);

        _validator = Substitute.For<IValidator<AuditQueryFilter>>();
        _validator.ValidateAsync(Arg.Any<AuditQueryFilter>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());

        _sut = new AuditService(_unitOfWork, _validator);

        var emptyLogs = new List<LogAuditoria>().BuildMockDbSet();
        _logAuditoriaRepository.Query().Returns(emptyLogs);
    }

    private static LogAuditoria BuildLog(int id, int? responsable, string entidad = "Container", string accion = "ACTUALIZAR") => new()
    {
        Id = id,
        NombreEntidad = entidad,
        IdEntidad = 1,
        Responsable = responsable,
        ResponsableUsuario = responsable.HasValue ? new Usuario { Id = responsable.Value, NombreUsuario = $"Usuario {responsable}" } : null,
        Accion = new Accion { Id = 1, Descripcion = accion },
        CreateAt = DateTimeOffset.UtcNow
    };

    [Fact]
    public async Task GetPagedAsync_WithInvalidFilter_ReturnsValidationError()
    {
        _validator.ValidateAsync(Arg.Any<AuditQueryFilter>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult(new[] { new ValidationFailure("FromDate", "inválido") }));

        var result = await _sut.GetPagedAsync(new AuditQueryFilter(), callerUserId: 1, callerRole: "Admin", CancellationToken.None);

        Assert.Equal(ResultStatus.ValidationError, result.Status);
    }

    [Fact]
    public async Task GetPagedAsync_AsAdmin_SeesAllResponsables()
    {
        var logs = new List<LogAuditoria> { BuildLog(1, responsable: 7), BuildLog(2, responsable: 99) };
        var mockLogs = logs.BuildMockDbSet();
        _logAuditoriaRepository.Query().Returns(mockLogs);

        var result = await _sut.GetPagedAsync(new AuditQueryFilter(), callerUserId: 1, callerRole: "Admin", CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Items.Count);
    }

    [Fact]
    public async Task GetPagedAsync_AsNonAdmin_OnlySeesOwnLogs_EvenIfAnotherResponsableIdRequested()
    {
        var logs = new List<LogAuditoria> { BuildLog(1, responsable: 7), BuildLog(2, responsable: 99) };
        var mockLogs = logs.BuildMockDbSet();
        _logAuditoriaRepository.Query().Returns(mockLogs);

        var filter = new AuditQueryFilter { ResponsableId = 99 };
        var result = await _sut.GetPagedAsync(filter, callerUserId: 7, callerRole: "Analista", CancellationToken.None);

        Assert.True(result.IsSuccess);
        var log = Assert.Single(result.Value.Items);
        Assert.Equal(7, log.ResponsableId);
    }

    [Fact]
    public async Task GetPagedAsync_FiltersByEntidadYAccion()
    {
        var logs = new List<LogAuditoria>
        {
            BuildLog(1, responsable: 7, entidad: "Container", accion: "ACTUALIZAR"),
            BuildLog(2, responsable: 7, entidad: "Usuario", accion: "CREAR"),
        };
        var mockLogs = logs.BuildMockDbSet();
        _logAuditoriaRepository.Query().Returns(mockLogs);

        var filter = new AuditQueryFilter { NombreEntidad = "Usuario", Accion = "CREAR" };
        var result = await _sut.GetPagedAsync(filter, callerUserId: 1, callerRole: "Admin", CancellationToken.None);

        Assert.True(result.IsSuccess);
        var log = Assert.Single(result.Value.Items);
        Assert.Equal(2, log.Id);
    }

    [Fact]
    public async Task GetPagedAsync_PaginatesResults()
    {
        var logs = Enumerable.Range(1, 5).Select(i => BuildLog(i, responsable: 1)).ToList();
        var mockLogs = logs.BuildMockDbSet();
        _logAuditoriaRepository.Query().Returns(mockLogs);

        var filter = new AuditQueryFilter { Page = 2, PageSize = 2 };
        var result = await _sut.GetPagedAsync(filter, callerUserId: 1, callerRole: "Admin", CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Items.Count);
        Assert.Equal(5, result.Value.TotalItems);
        Assert.Equal(3, result.Value.TotalPages);
    }
}
