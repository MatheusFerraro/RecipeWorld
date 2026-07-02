using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;

namespace worldRecipeMvc.Controllers.Api
{
    /// <summary>
    /// Authorization for API endpoints: accepts JWT Bearer tokens only.
    /// The Identity cookie deliberately does not satisfy this policy, which keeps
    /// API write endpoints out of reach of cross-site request forgery.
    /// </summary>
    public class ApiAuthorizeAttribute : AuthorizeAttribute
    {
        public ApiAuthorizeAttribute()
        {
            AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme;
        }
    }
}
