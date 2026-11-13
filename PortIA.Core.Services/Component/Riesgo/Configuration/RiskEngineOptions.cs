namespace PortIA.Core.Services.Component.Riesgo.Configuration;

public class RiskEngineOptions
{
    public const string SectionName = "RiskEngine";

    public double HorasUmbralOk { get; set; } = 96;
    public double HorasUmbralWarning { get; set; } = 48;
    public double HorasUmbralCritical { get; set; } = 0;
}
