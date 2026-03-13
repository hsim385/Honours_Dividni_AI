#pragma warning disable OPENAI001
using ex3.Models;
using Microsoft.Extensions.Options;
using OpenAI.Responses;
using System;
using System.Threading.Tasks;


namespace ex3.Back;

public class Backend : IBackend
{
    private readonly ResponsesClient _client;
	private readonly OpenAIOptions _options;

    public Backend(IOptions<OpenAIOptions> options)
    {
        _options = options.Value;
        _client = new ResponsesClient(
            Environment.GetEnvironmentVariable("OPENAI_API_KEY")
        );
    }
    public string test()
    {
        return Environment.GetEnvironmentVariable("OPENAI_API_KEY");
    }
    public async Task<string> AskAI(string prompt)
    {
		System.Console.WriteLine(_options.Instructions);
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

        return response.GetOutputText();
    }
}