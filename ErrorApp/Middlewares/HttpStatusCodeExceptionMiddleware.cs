using BaseLibrary.Exceptions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace ErrorApp.Middlewares
{
    public class HttpStatusCodeExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<HttpStatusCodeExceptionMiddleware> _logger;
        private readonly string _errorPath;


        public HttpStatusCodeExceptionMiddleware(RequestDelegate next,
            ILoggerFactory loggerFactory, string errorPath)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _logger = loggerFactory?.CreateLogger<HttpStatusCodeExceptionMiddleware>()
                ?? throw new ArgumentNullException(nameof(loggerFactory));
            _errorPath = errorPath;
        }

        public async Task Invoke(HttpContext context)
        {
            int statusCode;
            string message = "";

            try
            {
                await _next(context);

                if (context.Response.StatusCode == StatusCodes.Status404NotFound)
                {
                    _logger.LogInformation("Not Found Page 404 status code.");
                    throw new HttpStatusCodeException(404, $"Not Found");
                }
            }
            catch (Exception ex)
            {
                var httpStatusCodeException = ex as HttpStatusCodeException;
                if (httpStatusCodeException != null)/// my errors
                {
                    _logger.LogInformation("I throw HttpStatusCodeException");
                    _logger.LogInformation(httpStatusCodeException.Message);

                    statusCode = httpStatusCodeException.StatusCode;
                    message = httpStatusCodeException.Message;
                }
                else ///system errors
                {
                    // TODO: Do something with the exception
                    statusCode = context.Response.StatusCode;
                }

                try
                {
                    context.Response.Headers.Clear();
                    context.Response.StatusCode = statusCode;
                    context.Request.Path = $"{_errorPath}/{statusCode}/{message}";

                    await _next(context);
                }
                catch (Exception ex2)//if there are something errors routing
                {
                    _logger.LogInformation("ex2", ex2.Message);
                    await context.Response.WriteAsync($"Error Status Code: {statusCode}, Message: {message}");
                    return;
                }
            }
        }
    }

    // Extension method used to add the middleware to the HTTP request pipeline.
    public static class HttpStatusCodeExceptionMiddlewareExtensions
    {
        public static IApplicationBuilder UseHttpStatusCodeExceptionMiddleware(this IApplicationBuilder builder, string errorPath=null)
        {
            return builder.UseMiddleware<HttpStatusCodeExceptionMiddleware>(errorPath);
        }
    }

}