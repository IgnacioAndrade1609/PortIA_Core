namespace PortIA.Core.Services.Component.Audit.Validators;

using FluentValidation;
using PortIA.Core.Services.Component.Audit.DTOs;

public class AuditQueryFilterValidator : AbstractValidator<AuditQueryFilter>
{
    public AuditQueryFilterValidator()
    {
        RuleFor(x => x)
            .Must(x => !x.FromDate.HasValue || !x.ToDate.HasValue || x.FromDate.Value <= x.ToDate.Value)
            .WithMessage("La fecha de inicio no puede ser posterior a la fecha de término.")
            .WithName("FromDate");

        RuleFor(x => x.Page).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 200);
    }
}
