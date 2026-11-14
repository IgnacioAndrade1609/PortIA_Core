namespace PortIA.Core.Services.Component.Teams.Validators;

using FluentValidation;
using PortIA.Core.Services.Component.Teams.DTOs;

public class TeamDashboardFilterValidator : AbstractValidator<TeamDashboardFilter>
{
    public TeamDashboardFilterValidator()
    {
        RuleFor(x => x)
            .Must(x => !x.FromDate.HasValue || !x.ToDate.HasValue || x.FromDate.Value <= x.ToDate.Value)
            .WithMessage("La fecha de inicio no puede ser posterior a la fecha de término.")
            .WithName("FromDate");
    }
}
