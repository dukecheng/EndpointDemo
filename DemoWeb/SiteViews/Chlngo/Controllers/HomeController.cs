using Microsoft.AspNetCore.Mvc;

namespace DemoWeb.SiteViews.Chlngo.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}