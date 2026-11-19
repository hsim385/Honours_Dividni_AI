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
            model.Response = false;
            return View(model);
        }
        return RedirectToAction("QuestionMaker", new { prompt = model.Prompt, file = model.File, AIModel = model.AIModel });
        //
        // model.Response = await _backend.AskAI(model.Prompt);
        // return View(model);
    }

    [HttpPost("/api/upload")]
    public async Task<IActionResult> UploadAPI([FromForm] IFormFile file)
    {
	    if (file == null || file.Length == 0)
	    {
		    return BadRequest("No file was provided.");
	    }
	    string UploadsFolder = Path.Combine(_webHost.WebRootPath, "uploads");
	    if (!Directory.Exists(UploadsFolder))
	    {
		    Directory.CreateDirectory(UploadsFolder);
	    }

	    string fileName = Path.GetFileName(file.FileName);
	    string fileSavePath = Path.Combine(UploadsFolder, fileName);

	    await using (FileStream stream = new FileStream(fileSavePath, FileMode.Create))
	    {
		    await file.CopyToAsync(stream);
	    }
	    
	    return Ok(new
	    {
		    fileName = fileName,
		    message = $"{fileName} uploaded successfully"
	    });
    }

	[HttpPost("/api/prompt")]
	public async Task<IActionResult> IndexAPI([FromBody] PromptRequest model)
	{
		if (string.IsNullOrWhiteSpace(model.Prompt))
		{
			return BadRequest("Invalid prompt");
		}

		string file = await _backend.AskAI(model.Prompt, model.File, model.AIModel);

		byte[] fileBytes = System.Text.Encoding.UTF8.GetBytes(file);

		return File(
			fileBytes,
			"text/plain",
			"GeneratedCode.cs"
		);
		//
		// model.Response = await _backend.AskAI(model.Prompt);
		// return View(model);
	}
	
    [HttpGet]
    public async Task<IActionResult> QuestionMaker(string prompt, string file, string AIModel)
    {
        // if (string.IsNullOrWhiteSpace(prompt))
        // {
        //     model.Response = "Please enter a prompt";
        //     return View(model);
        // }
        var model = new HomeViewModel();

        if (!string.IsNullOrWhiteSpace(prompt))
        {
	        model.AIInvolvement = " AI Created";
            model.Prompt = prompt;
			model.Response = true;
			model.File = file;
			model.AIModel = AIModel;
            model.Question = await _backend.cSharpToJson(await _backend.AskAI(prompt, file, AIModel));
        }
		else
		{
			model.Prompt = prompt;
			model.Response = false;
		}

        return View(model);
    }

	[HttpPost]
	public async Task<IActionResult> QuestionMaker(HomeViewModel model)
    {
		if (model.CompleteQuestion) {
			await _backend.completeQuestion(model);
			ModelState.Clear();
			model.AIInvolvement += " AI Assisted";
		}
		else
		{
			model.AIInvolvement = (model.AIInvolvement == "") ? "Human Made" : model.AIInvolvement;
			await _backend.saveQuestion(model);
		}
        return View(model);
    }

    [HttpPost]
    [RequestSizeLimit(200_000_000)]
    [RequestFormLimits(MultipartBodyLengthLimit = 200_000_000)]
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

	public IActionResult RemoveFiles()
    {
        return View();
    }

	[HttpPost]
	public async Task<IActionResult> RemoveFiles(string File)
    {
		bool success = await _backend.DeleteFile(File);
		if (success)
		{
			ViewBag.Message = $"{File} deleted.";
		}
		else
		{
			ViewBag.Message = "File not found.";
		}
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}