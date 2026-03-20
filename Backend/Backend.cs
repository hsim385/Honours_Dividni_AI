#pragma warning disable OPENAI001
using ex3.Models;
using Microsoft.Extensions.Options;
using OpenAI.Responses;
using System;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;


namespace ex3.Back;

public class Backend : IBackend
{
    private readonly ResponsesClient _client;
	private readonly OpenAIOptions _options;
    private readonly IWebHostEnvironment _webHost;

    public Backend(IOptions<OpenAIOptions> options, IWebHostEnvironment webHost)
    {
        _options = options.Value;
        _client = new ResponsesClient(
            _options.OpenAiApiKey
        );
        _webHost = webHost;
    }
    public string test()
    {
        return Environment.GetEnvironmentVariable("OPENAI_API_KEY");
    }
    public async Task<string> AskAI(string prompt)
    {
		// System.Console.WriteLine(_options.Instructions);
		string fullPrompt = "Using the Dividni template, create a new question with these instructions:\n" +
        "\n" +
        "\n" +
        "Generate a complete C# method, filling in the template appropriately and following all Dividni rules.\n" +
        $"Make a question about {prompt}";
		var options = new CreateResponseOptions
		{
            Model = _options.Model,
            Instructions = _options.Instructions
		};
		options.InputItems.Add(ResponseItem.CreateUserMessageItem(fullPrompt));
        ResponseResult response = await _client.CreateResponseAsync(options);
        
        string trimmedResponse = RemoveCodeFences(response.GetOutputText());
        
        var folderPath = Path.Combine(_webHost.WebRootPath, "QuestionOutput");
        
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        var filePath = Path.Combine(folderPath, "filename.cs");

        await File.WriteAllTextAsync(filePath, trimmedResponse);
        
        return trimmedResponse;
        
        string RemoveCodeFences(string text)
        {
            return Regex.Replace(text, @"```[a-zA-Z]*\n?|```", "").Trim();
        }
    }
}