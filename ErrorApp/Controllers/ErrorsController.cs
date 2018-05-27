using System;
using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ErrorApp.Controllers
{
    public class ErrorsController : Controller
    {
        /// for system middleware
        public IActionResult Index(string id)
        {
            // when an exception occurs, route to /Errors/Index
            ///app.UseStatusCodePages();
            ///app.UseExceptionHandler($"/Errors/Index");
            ///app.UseStatusCodePagesWithReExecute("/Errors/Index/{0}");

            // Get the details of the exception that occurred
            var exceptionFeature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();

            if (exceptionFeature != null)
            {
                // Get which route the exception occurred at
                string routeWhereExceptionOccurred = exceptionFeature.Path;

                // Get the exception that occurred
                Exception exceptionThatOccurred = exceptionFeature.Error;

                // TODO: Do something with the exception
                // Log it with Serilog?
                // Send an e-mail, text, fax, or carrier pidgeon?  Maybe all of the above?
                // Whatever you do, be careful to catch any exceptions, otherwise you'll end up with a blank page and throwing a 500
            }

            return View("~/Views/Shared/Error.cshtml");
        }

        // for custom middleware
        [Route("Errors/Error/{statusCode}/{message?}")]
        public IActionResult Error(string statusCode, string message)
        {
            ViewData["StatusCode"]=statusCode;
            ViewData["Message"]=message;

            return View();
        }

    }
}
