using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using MyLib;
using ScientificResearch.Models;
using System;
using System.Linq;

namespace ScientificResearch.Infrastucture
{
    /// <summary>
    /// Rejects requests that do not have a server-side session user.
    /// </summary>
    public sealed class MyAuthorizationFilter : IAuthorizationFilter
    {
        public void OnAuthorization(AuthorizationFilterContext context)
        {
            if (context.ActionDescriptor.EndpointMetadata?.OfType<IAllowAnonymous>().Any() == true)
            {
                return;
            }

            var hasKeycloakUser = context.HttpContext.Items.TryGetValue(KeycloakUserMiddleware.CurrentUserItemKey, out var itemUser);
            var keycloakEnabled = context.HttpContext.RequestServices.GetService<KeycloakOptions>()?.Enabled == true;
            var user = keycloakEnabled
                ? (hasKeycloakUser ? itemUser as CurrentUser : null)
                : (hasKeycloakUser ? itemUser as CurrentUser : context.HttpContext.Session.Get<CurrentUser>("user"));
            if (user == null)
            {
                context.Result = new UnauthorizedObjectResult(new { error = "未登录或登录已过期" });
            }
        }
    }

    /// <summary>
    /// Converts model binding failures into a stable client error response.
    /// </summary>
    public sealed class MyActionFilter : IActionFilter
    {
        public void OnActionExecuted(ActionExecutedContext context) { }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            if (!context.ModelState.IsValid)
            {
                var message = string.Join(",", context.ModelState
                    .SelectMany(item => item.Value?.Errors ?? Enumerable.Empty<ModelError>())
                    .Select(error => error.ErrorMessage));

                context.Result = new BadRequestObjectResult(new
                {
                    error = string.IsNullOrWhiteSpace(message) ? "接口数据格式有误" : message
                });
            }
        }
    }

    /// <summary>
    /// Preserves the historical { data: ... } success envelope without wrapping errors.
    /// </summary>
    public sealed class MyResultFilter : IResultFilter
    {
        public void OnResultExecuted(ResultExecutedContext context) { }

        public void OnResultExecuting(ResultExecutingContext context)
        {
            if (context.Result is ObjectResult result &&
                (result.StatusCode == null || (result.StatusCode >= 200 && result.StatusCode < 300)))
            {
                context.Result = new ObjectResult(new { data = result.Value })
                {
                    StatusCode = result.StatusCode
                };
            }
        }
    }
}
