    Asp.Net Core Custom Error Handling

my custom Exception class in BaseLibrary

```csharp
public class HttpStatusCodeException : Exception
{
        public int StatusCode { get; set; }
        public string ContentType { get; set; } = @"text/plain";

        public HttpStatusCodeException(int statusCode):base("")
        {
            this.StatusCode = statusCode;
        }

        public HttpStatusCodeException(int statusCode, string message) : base(message)
        {
            this.StatusCode = statusCode;
        }

        public HttpStatusCodeException(int statusCode, Exception inner) : this(statusCode, inner.ToString()) { }

        public HttpStatusCodeException(int statusCode, JObject errorObject) : this(statusCode, errorObject.ToString())
        {
            this.ContentType = @"application/json";
        }
}
```

my custom Middleware
```csharp
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
```

add <code>app.UseHttpStatusCodeExceptionMiddleware($"/Errors/Error")</code> in Startup.cs
```csharp
if (env.IsDevelopment())
{
    app.UseBrowserLink();
    app.UseDeveloperExceptionPage();
}
else
{
    //custom error middleware
    app.UseHttpStatusCodeExceptionMiddleware($"/Errors/Error");
}
```

in ErrorsController.cs
```csharp
// for custom middleware
[Route("Errors/Error/{statusCode}/{message?}")]
public IActionResult Error(string statusCode, string message)
{
    ViewData["StatusCode"]=statusCode;
    ViewData["Message"]=message;

    return View();
}
```

in Views > Shared > Error.cshtml
```csharp
@{
    string statusCode = ViewData["StatusCode"]?.ToString();
    string message = ViewData["Message"]?.ToString();
    if (string.IsNullOrWhiteSpace(message))
    {
        switch (statusCode)
        {
            case "400":
                message = "Bad Request";
                break;
            case "404":
                message = "Not Found";
                break;
            case "401":
                message = "Unauthorized";
                break;
            case "403":
                message = "Forbidden";
                break;
            case "500":
                message = "Internal Server Error";
                break;
            default:
                statusCode = "404";
                message = "Not Found";
                break;
        }
    }

    ViewData["Title"] = "Error " + statusCode;
}

<h1 class="text-danger">Error @statusCode</h1>
<h4 class="text-danger">
    @message
</h4>
<!-- ... -->
```

some test action in HomeController.cs
```csharp
public class HomeController : Controller
{
    public IActionResult Index() => View();

    //Error examples
    public IActionResult Test1() => View();//not view file
    public IActionResult Test2() => throw new HttpStatusCodeException(400);
    public IActionResult Test3() => throw new HttpStatusCodeException(401,"This is test Unauthorized");
    public IActionResult Test4() => throw new Exception();

}
```

some ready middleware in Startup.cs
```csharp
app.UseStatusCodePages();
app.UseExceptionHandler($"/Errors");
app.UseStatusCodePagesWithReExecute("/Errors/Index/{0}");
```

for system middleware and get errors in ErrorsController.cs
```csharp
// for system middleware
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
```