using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace contract_claim_system.Filters
{
    public class AuthorizationFilter : ActionFilterAttribute
    {
        private readonly string[] _allowedRoles;

        public AuthorizationFilter(params string[] allowedRoles)
        {
            _allowedRoles = allowedRoles;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var isAuthenticated = context.HttpContext.Session.GetString("IsAuthenticated");
            var userRole = context.HttpContext.Session.GetString("UserRole");

            if (isAuthenticated != "true")
            {
                context.Result = new RedirectToActionResult("Login", "Account", null);
                return;
            }

            if (_allowedRoles.Length > 0 && !_allowedRoles.Contains(userRole))
            {
                context.Result = new RedirectToActionResult("AccessDenied", "Home", null);
                return;
            }

            base.OnActionExecuting(context);
        }
    }
}