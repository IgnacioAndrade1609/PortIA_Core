namespace PortIA.Core.Tests.Component.Containers;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using PortIA.Core.Infrastructure.Data.Entities;
using PortIA.Core.Infrastructure.Data.Interfaces;
using PortIA.Core.Infrastructure.Data.UnitOfWork;
using PortIA.Core.Services.Common.Results;
using PortIA.Core.Services.Component.Containers.Services;
using PortIA.Core.Services.Component.Mensajeria.Publishers.Interfaces;
using PortIA.Core.Services.Component.Riesgo.Configuration;
using PortIA.Core.Services.Component.Riesgo.Interfaces;

public class ContainerAssignTests
{
    private const int AdminId = 1;
    private const int ContainerId = 7;
    private const int AnalistaId = 20;

    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IContainerRepository _containers = Substitute.For<IContainerRepository>();
    private readonly IUsuarioRepository _usuarios = Substitute.For<IUsuarioRepository>();
    private readonly IHitoRepository _hitos = Substitute.For<IHitoRepository>();
    private readonly IAccionRepository _acciones = Substitute.For<IAccionRepository>();
    private readonly ILogAuditoriaRepository _auditoria = Substitute.For<ILogAuditoriaRepository>();
    private readonly ContainerService _sut;

    public ContainerAssignTests()
    {
        _unitOfWork.Containers.Returns(_containers);
        _unitOfWork.Usuarios.Returns(_usuarios);
        _unitOfWork.Hitos.Returns(_hitos);
        _unitOfWork.Acciones.Returns(_acciones);
        _unitOfWork.LogsAuditoria.Returns(_auditoria);
        _acciones.GetByDescripcionAsync("ACTUALIZAR", Arg.Any<CancellationToken>()).Returns(new Accion { Id = 2, Descripcion = "ACTUALIZAR" });

        _sut = new ContainerService(
            _unitOfWork,
            Substitute.For<IInspectContainerPublisher>(),
            Substitute.For<IRiskEngineService>(),
            Options.Create(new RiskEngineOptions()),
            Substitute.For<ILogger<ContainerService>>());
    }

    private void ConContenedor(int? asignadoA = null, string? nombreActual = null) =>
        _containers.GetByIdAsync(ContainerId, Arg.Any<CancellationToken>()).Returns(new Container
        {
            Id = ContainerId,
            Vigente = true,
            AsignadoA = asignadoA,
            Asignado = nombreActual is null ? null : new Usuario { Id = asignadoA!.Value, NombreUsuario = nombreActual }
        });

    private void ConAnalista(bool vigente = true, string rol = "Analista") =>
        _usuarios.GetByIdWithRoleAsync(AnalistaId, Arg.Any<CancellationToken>()).Returns(new Usuario
        {
            Id = AnalistaId,
            NombreUsuario = "Nuevo Analista",
            Vigente = vigente,
            Rol = new Rol { NombreRol = rol }
        });

    [Fact]
    public async Task AssignAsync_ByNonAdmin_ReturnsUnauthorized()
    {
        var result = await _sut.AssignAsync(ContainerId, AnalistaId, AdminId, "Jefe", CancellationToken.None);

        Assert.Equal(ResultStatus.Unauthorized, result.Status);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AssignAsync_WithInactiveTarget_ReturnsBusinessError()
    {
        ConContenedor();
        ConAnalista(vigente: false);

        var result = await _sut.AssignAsync(ContainerId, AnalistaId, AdminId, "Admin", CancellationToken.None);

        Assert.Equal(ResultStatus.BusinessError, result.Status);
    }

    [Fact]
    public async Task AssignAsync_WithNonAnalystTarget_ReturnsBusinessError()
    {
        ConContenedor();
        ConAnalista(rol: "Jefe");

        var result = await _sut.AssignAsync(ContainerId, AnalistaId, AdminId, "Admin", CancellationToken.None);

        Assert.Equal(ResultStatus.BusinessError, result.Status);
    }

    [Fact]
    public async Task AssignAsync_WhenValid_ReassignsAndRegistersHitoAndAudit()
    {
        ConContenedor(asignadoA: 30, nombreActual: "Analista Anterior");
        ConAnalista();

        var result = await _sut.AssignAsync(ContainerId, AnalistaId, AdminId, "Admin", CancellationToken.None);

        Assert.Equal(ResultStatus.Success, result.Status);
        await _hitos.Received(1).AddAsync(
            Arg.Is<Hito>(h => h.Descripcion.Contains("Analista Anterior") && h.Descripcion.Contains("Nuevo Analista") && h.CreadoPor == AdminId),
            Arg.Any<CancellationToken>());
        await _auditoria.Received(1).AddAsync(
            Arg.Is<LogAuditoria>(a => a.Responsable == AdminId && a.NombreEntidad == "Container"),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AssignAsync_WhenAlreadyAssignedToTarget_ReturnsBusinessError()
    {
        ConContenedor(asignadoA: AnalistaId, nombreActual: "Nuevo Analista");
        ConAnalista();

        var result = await _sut.AssignAsync(ContainerId, AnalistaId, AdminId, "Admin", CancellationToken.None);

        Assert.Equal(ResultStatus.BusinessError, result.Status);
    }
}
