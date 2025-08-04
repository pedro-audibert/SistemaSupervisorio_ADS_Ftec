using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class ApiKeyAttribute : Attribute, IAsyncActionFilter
{
    private const string ApiKeyHeaderName = "X-API-Key";

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        // Pega a chave da API que está no cabeçalho (header) da requisição.
        if (!context.HttpContext.Request.Headers.TryGetValue(ApiKeyHeaderName, out var potentialApiKey))
        {
            // Se o cabeçalho não existir, rejeita a requisição.
            context.Result = new UnauthorizedObjectResult("Chave de API ausente.");
            return;
        }

        // Pega a chave da API correta que guardamos no appsettings.json.
        var configuration = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
        var apiKey = configuration.GetValue<string>("Authentication:ApiKey");

        // Compara a chave da requisição com a chave correta.
        if (!apiKey.Equals(potentialApiKey))
        {
            // Se as chaves não forem iguais, rejeita a requisição.
            context.Result = new UnauthorizedObjectResult("Chave de API inválida.");
            return;
        }

        // Se a chave estiver correta, permite que a requisição continue para o controller.
        await next();
    }
}