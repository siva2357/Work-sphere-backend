using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using WorkSphere.API.Services;

namespace WorkSphere.API.Authorization
{
    /// <summary>
    /// Enforces centralized database permissions on any controller or action:
    /// [RequirePermission(PermissionModules.Tasks, PermissionActions.Assign)]
    /// Also stores the resolved DataScope in HttpContext.Items["PermissionScope"].
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class RequirePermissionAttribute : TypeFilterAttribute
    {
        public RequirePermissionAttribute(string module, string action)
            : base(typeof(PermissionFilter))
        {
            Arguments = [module, action];
        }
    }

    public class PermissionFilter : IAsyncAuthorizationFilter
    {
        private readonly string _module;
        private readonly string _action;
        private readonly IPermissionService _permissionService;

        public PermissionFilter(string module, string action, IPermissionService permissionService)
        {
            _module = module;
            _action = action;
            _permissionService = permissionService;
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var user = context.HttpContext.User;
            if (user?.Identity?.IsAuthenticated != true)
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            var idClaim = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(idClaim, out var employeeId))
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            var (isAllowed, scope) = await _permissionService.CheckPermissionAsync(employeeId, _module, _action);
            if (!isAllowed)
            {
                context.Result = new ObjectResult(new
                {
                    message = $"Access denied. Missing required permission: {_module}.{_action}"
                })
                {
                    StatusCode = StatusCodes.Status403Forbidden
                };
                return;
            }

            context.HttpContext.Items["PermissionScope"] = scope;
        }
    }
}

