using DemoWeb.Models;
using DemoWeb.Services;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace DemoWeb.Controllers
{
    public class ErrorController : Controller
    {
        public IActionResult Fallback()
        {
            return Content("Fallback");
        }
    }
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly StaticSiteGenerator _staticSiteGenerator;

        public HomeController(ILogger<HomeController> logger, StaticSiteGenerator staticSiteGenerator)
        {
            _logger = logger;
            this._staticSiteGenerator = staticSiteGenerator;
        }

        public IActionResult Index()
        {
            ViewData["Title"] = "Home";
            ViewBag.Test = "test";
            ViewData["Message"] = "Welcome to the Demo Web Application!";
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [HttpGet]
        public async Task<IActionResult> GenerateStaticPage()
        {
            var model = new { Title = "Hello World", Content = "This is a static page." };
            string outputPath = Path.Combine("wwwroot", "static", "hello-world.html");

            await _staticSiteGenerator.GenerateStaticPage("HelloWorld", model, outputPath);

            return Ok("Static page generated successfully!");
        }
    }
}
