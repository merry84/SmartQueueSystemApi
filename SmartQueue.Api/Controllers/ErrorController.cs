using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace SmartQueue.Api.Controllers
{
    public class ErrorController : Controller
    {
        [Route("Error/{statusCode}")]
        public IActionResult StatusCodeHandler(int statusCode)
        {
            return statusCode switch
            {
                404 => View("NotFound"),
                403 => View("AccessDenied"),
                _ => View("GeneralError")
            };
        }

        [Route("Error")]
        public IActionResult Error()
        {
            var exceptionFeature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();

            ViewBag.ErrorMessage = exceptionFeature?.Error.Message;

            return View("GeneralError");
        }
    }
}