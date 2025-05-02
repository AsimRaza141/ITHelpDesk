using Microsoft.AspNetCore.Mvc.Filters;

namespace ITHelpDesk_Updated
{
    public class NoCacheAttribute : ActionFilterAttribute
    {
        public override void OnResultExecuting(ResultExecutingContext filterContext)
        {
            filterContext.HttpContext.Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
            filterContext.HttpContext.Response.Headers["Pragma"] = "no-cache";
            filterContext.HttpContext.Response.Headers["Expires"] = "0";
            base.OnResultExecuting(filterContext);
        }
    }
}
