using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace RealWorldApi.Core.Domain.Validation;

public class FluentValidationFilter(IServiceProvider serviceProvider, ProblemDetailsFactory problemDetailsFactory)
    : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        foreach (var parameter in context.ActionDescriptor.Parameters)
        {
            if (!context.ActionArguments.TryGetValue(parameter.Name, out var argumentValue) ||
                argumentValue == null) continue;
            var argumentType = argumentValue.GetType();

            var validatorType = typeof(IValidator<>).MakeGenericType(argumentType);

            if (serviceProvider.GetService(validatorType) is not IValidator validator) continue;
            // Validate the argument
            var validationResult = await validator.ValidateAsync(new ValidationContext<object>(argumentValue));

            if (validationResult.IsValid) continue;
            validationResult.AddToModelState(context.ModelState);
            var problemDetails =
                problemDetailsFactory.CreateValidationProblemDetails(context.HttpContext,
                    context.ModelState);
            context.Result = new BadRequestObjectResult(problemDetails);

            return;
        }

        await next();
    }

    public void OnActionExecuted(ActionExecutedContext context) { }
}