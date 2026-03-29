using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Honours_Dividni_AI.Back;
using Honours_Dividni_AI.Models;

namespace Honours_Dividni_AI.Controllers;

public class HomeController : Controller
{
    private readonly IBackend _backend;
    private readonly IWebHostEnvironment _webHost;

    public HomeController(IBackend backend, IWebHostEnvironment webHost)
    {
        _backend = backend;
        _webHost = webHost;
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

    [HttpPost]
    public async Task<IActionResult> Privacy(IFormFile file)
    {
        string UploadsFolder = Path.Combine(_webHost.WebRootPath, "uploads");
        if (!Directory.Exists(UploadsFolder))
        {
            Directory.CreateDirectory(UploadsFolder);
        }

        string fileName = Path.GetFileName(file.FileName);
        string fileSavePath = Path.Combine(UploadsFolder, fileName);

        using (FileStream stream = new FileStream(fileSavePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }
		
        ViewBag.Message = fileName + " uploaded successfully";
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
}