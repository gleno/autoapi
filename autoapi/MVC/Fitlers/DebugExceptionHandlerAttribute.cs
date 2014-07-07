using System.Diagnostics;
using System.Web.Http;
using System.Web.Http.Filters;
using zeco.autoapi.Extensions;

namespace zeco.autoapi.MVC.Fitlers
{
    public class DebugExceptionHandlerAttribute : ExceptionFilterAttribute
    {
        public override void OnException(HttpActionExecutedContext context)
        {
            if (context.Exception is HttpResponseException)
            {
                if (Debugger.IsAttached)
                    Debugger.Break();
            }
            else
            {
                if (this.IsDebug())
                {
                    var innermost = context.Exception;
                    while (innermost.InnerException != null)
                        innermost = innermost.InnerException;

                    if (Debugger.IsAttached)
                        Debugger.Break();

                    throw innermost;
                }
            }
        }
    }
}