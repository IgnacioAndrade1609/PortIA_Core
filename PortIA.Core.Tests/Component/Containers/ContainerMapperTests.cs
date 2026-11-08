namespace PortIA.Core.Tests.Component.Containers;

using PortIA.Core.Services.Component.Containers.DTOs;
using PortIA.Core.Services.Component.Containers.Mappers;
using PortIA.Core.Services.Component.Riesgo.Configuration;

public class ContainerMapperTests
{
    private static readonly RiskEngineOptions Umbrales = new()
    {
        HorasUmbralOk = 96,
        HorasUmbralWarning = 48,
        HorasUmbralCritical = 0,
    };

    private static ContainerSummaryProjection BuildProjection(
        string? riskLevelGuardado = "OK",
        string? estado = null,
        DateTimeOffset? fechaArriboReal = null,
        DateTimeOffset? fechaRetiroContainer = null,
        DateTimeOffset? deadlineDemurrage = null,
        DateTimeOffset? deadlineDetention = null) => new(
        Id: 1,
        NumeroContainer: "MSCU1234567",
        NumeroBl: null,
        NombreForwarder: null,
        IdNaviera: null,
        NavieraNombre: null,
        IdTerminalOrigen: null,
        TerminalOrigenNombre: null,
        IdTerminalLlegada: null,
        TerminalLlegadaNombre: null,
        FechaLlegadaEstimada: null,
        FechaArriboReal: fechaArriboReal,
        FechaRetiroContainer: fechaRetiroContainer,
        DiasLibresDemurrage: 7,
        DiasLibresDetention: 5,
        DeadlineDemurrage: deadlineDemurrage,
        DeadlineDetention: deadlineDetention,
        RiskLevel: riskLevelGuardado,
        NeedsReview: false,
        Managed: false,
        EstadoPuerto: null,
        Estado: estado,
        AsignadoA: null,
        AsignadoANombre: null,
        CreateAt: DateTimeOffset.UtcNow);

    [Fact]
    public void ToSummaryResponse_WhenNotArrivedYet_ReturnsOkRegardlessOfStoredValue()
    {
        var projection = BuildProjection(riskLevelGuardado: "OVERDUE", fechaArriboReal: null);

        var response = projection.ToSummaryResponse(Umbrales);

        Assert.Equal("OK", response.RiskLevel);
    }

    [Fact]
    public void ToSummaryResponse_WhenStoredValueIsStaleAndDeadlineIsLongPassed_IgnoresStoredValueAndReturnsOverdue()
    {
        // Reproduce exactamente el bug real: la BD dice "OK" (último recálculo hace días),
        // pero el deadline ya pasó hace casi 20 días. El valor mostrado nunca debe mentir.
        var projection = BuildProjection(
            riskLevelGuardado: "OK",
            fechaArriboReal: DateTimeOffset.UtcNow.AddDays(-30),
            deadlineDemurrage: DateTimeOffset.UtcNow.AddHours(-454));

        var response = projection.ToSummaryResponse(Umbrales);

        Assert.Equal("OVERDUE", response.RiskLevel);
    }

    [Theory]
    [InlineData(100, "OK")]
    [InlineData(72, "WARNING")]
    [InlineData(24, "CRITICAL")]
    [InlineData(-10, "OVERDUE")]
    public void ToSummaryResponse_WhenArrivedAndNotRetired_ClassifiesByHoursToDemurrageDeadline(double horas, string nivelEsperado)
    {
        var projection = BuildProjection(
            fechaArriboReal: DateTimeOffset.UtcNow.AddDays(-1),
            deadlineDemurrage: DateTimeOffset.UtcNow.AddHours(horas));

        var response = projection.ToSummaryResponse(Umbrales);

        Assert.Equal(nivelEsperado, response.RiskLevel);
    }

    [Fact]
    public void ToSummaryResponse_WhenRetired_UsesDetentionDeadlineInsteadOfDemurrage()
    {
        var projection = BuildProjection(
            fechaArriboReal: DateTimeOffset.UtcNow.AddDays(-10),
            deadlineDemurrage: DateTimeOffset.UtcNow.AddHours(-500), // ya vencido, pero ya no aplica
            fechaRetiroContainer: DateTimeOffset.UtcNow.AddDays(-1),
            deadlineDetention: DateTimeOffset.UtcNow.AddHours(72));

        var response = projection.ToSummaryResponse(Umbrales);

        Assert.Equal("WARNING", response.RiskLevel);
    }

    [Fact]
    public void ToSummaryResponse_WhenReturnedEmpty_ReturnsOkRegardlessOfDeadlines()
    {
        var projection = BuildProjection(
            estado: "DEVUELTO_VACIO",
            fechaArriboReal: DateTimeOffset.UtcNow.AddDays(-10),
            fechaRetiroContainer: DateTimeOffset.UtcNow.AddDays(-3),
            deadlineDetention: DateTimeOffset.UtcNow.AddHours(-200));

        var response = projection.ToSummaryResponse(Umbrales);

        Assert.Equal("OK", response.RiskLevel);
    }
}
