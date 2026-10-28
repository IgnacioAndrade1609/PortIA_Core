namespace PortIA.Core.WebApi.Extensions;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using PortIA.Core.Services.Common.Results;

public static class ResultExtensions
{
    public static IActionResult ToActionResult<T>(
        this Result<T> result,
        ControllerBase controller,
        Func<T, IActionResult> success)
    {
        return result.Status switch
        {
            ResultStatus.Success => success(result.Value),
            ResultStatus.ValidationError => controller.BadRequest(BuildValidationProblem(controller, result.ValidationErrors)),
            ResultStatus.NotFound => controller.NotFound(BuildProblem(controller, StatusCodes.Status404NotFound, "No encontrado", result.Error)),
            ResultStatus.BusinessError => controller.Conflict(BuildProblem(controller, StatusCodes.Status409Conflict, "Error de negocio", result.Error)),
            ResultStatus.Unauthorized => controller.Unauthorized(BuildProblem(controller, StatusCodes.Status401Unauthorized, "No autorizado", result.Error)),
            _ => controller.StatusCode(StatusCodes.Status500InternalServerError)
        };
    }

    public static IActionResult ToActionResult(
        this Result result,
        ControllerBase controller,
        Func<IActionResult> success)
    {
        return result.Status switch
        {
            ResultStatus.Success => success(),
            ResultStatus.ValidationError => controller.BadRequest(BuildValidationProblem(controller, result.ValidationErrors)),
            ResultStatus.NotFound => controller.NotFound(BuildProblem(controller, StatusCodes.Status404NotFound, "No encontrado", result.Error)),
            ResultStatus.BusinessError => controller.Conflict(BuildProblem(controller, StatusCodes.Status409Conflict, "Error de negocio", result.Error)),
            ResultStatus.Unauthorized => controller.Unauthorized(BuildProblem(controller, StatusCodes.Status401Unauthorized, "No autorizado", result.Error)),
            _ => controller.StatusCode(StatusCodes.Status500InternalServerError)
        };
    }

    private static ProblemDetails BuildProblem(ControllerBase controller, int statusCode, string title, string? detail)
    {
        return controller.ProblemDetailsFactory.CreateProblemDetails(
            controller.HttpContext,
            statusCode: statusCode,
            title: title,
            detail: detail);
    }

    private static ValidationProblemDetails BuildValidationProblem(ControllerBase controller, IDictionary<string, string[]>? errors)
    {
        var modelState = new ModelStateDictionary();
        foreach (var (field, messages) in errors ?? new Dictionary<string, string[]>())
        {
            foreach (var message in messages)
            {
                modelState.AddModelError(field, message);
            }
        }

        return controller.ProblemDetailsFactory.CreateValidationProblemDetails(
            controller.HttpContext,
            modelState,
            statusCode: StatusCodes.Status400BadRequest,
            title: "Error de validación");
    }
}
