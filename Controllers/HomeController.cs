using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ex3.Models;
using ex3.Back;
using System.Threading.Tasks;

namespace ex3.Controllers;

public class HomeController : Controller
{
    private readonly IBackend _backend;

    public HomeController(IBackend backend)
    {
        _backend = backend;
    }

	[HttpGet]
    public IActionResult Index()
    {
		var model = new HomeViewModel();
        //ViewData["response"] = await _backend.AskAI("Write a once sentence story about a unicorn.");
        return View(model);
    }

	[HttpPost]
	public async Task<IActionResult> Index(HomeViewModel model)
	{
		if (string.IsNullOrWhiteSpace(model.Prompt))
		{
			model.Response = "Please enter a prompt";
			return View(model);
		}

		model.Response = await _backend.AskAI(model.Prompt);
		return View(model);
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
}
