using System;
using Microsoft.AspNetCore.Mvc;
using BaseLibrary.Exceptions;

namespace ErrorApp.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index() => View();

        //Error examples
        public IActionResult Test1() => View();//not view file
        public IActionResult Test2() => throw new HttpStatusCodeException(400);
        public IActionResult Test3() => throw new HttpStatusCodeException(401,"This is test Unauthorized");
        public IActionResult Test4() => throw new Exception();

    }
}
