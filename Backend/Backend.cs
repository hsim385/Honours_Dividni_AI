#pragma warning disable OPENAI001
using Microsoft.Extensions.Options;
using OpenAI.Responses;
using System;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;
using Honours_Dvidni_AI.Models;


namespace Honours_Dvidni_AI.Back;

public class Backend : IBackend
{
    private readonly ResponsesClient _client;
	private readonly OpenAIOptions _options;

    public Backend(IOptions<OpenAIOptions> options)
    {
        _options = options.Value;
        _client = new ResponsesClient(
            _options.OpenAiApiKey
        );
    }
    public string test()
    {
        return "0";
    }
    public async Task<string> AskAI(string prompt)
    {
		//System.Console.WriteLine(_options.Instructions);
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
		// options.InputItems.Add(ResponseItem.CreateUserMessageItem(fullPrompt));

        // var message = ResponseItem.CreateUserMessageItem(new List<ResponseContentItem>
        // {
        //   ResponseContentItem.CreateInputFile(fileId),
        //   ResponseContentItem.CreateInputText(fullPrompt)
        // });
        
        var uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");

        var fileIds = new List<string>();

        foreach (var filePath in Directory.GetFiles(uploadFolder, "*.pdf"))
        {
            await using var stream = File.OpenRead(filePath);

            var uploaded = await _client.Files.UploadAsync(
                stream,
                Path.GetFileName(filePath),
                "application/pdf"
            );

            fileIds.Add(uploaded.Id);
        }
        
        var contentItems = new List<ResponseContentPart>();

// Add all PDFs
        foreach (var fileId in fileIds)
        {
            contentItems.Add(ResponseContentPart.CreateInputFile(fileId));
        }

// Add your prompt LAST
        contentItems.Add(ResponseContentPart.CreateInputText(fullPrompt));

// Create message
        var message = ResponseItem.CreateUserMessageItem(contentItems);

        options.InputItems.Add(message);

        ResponseResult response = await _client.CreateResponseAsync(options);
        
        string trimmedResponse = RemoveCodeFences(response.GetOutputText());

        await File.WriteAllTextAsync("filename.cs", trimmedResponse);
        
        return trimmedResponse;
        
        string RemoveCodeFences(string text)
        {
            return Regex.Replace(text, @"```[a-zA-Z]*\n?|```", "").Trim();
        }
    }
}

//"Instructions": "You are an assistant that helps generate Dividni multiple-choice questions in C#.\nUse upper or intermediate levels of Bloom's taxonomy.\n\nDividni supports two question types:\n\nTruthQuestion\n- At least 1 correct answer (Dividni will randomly select ONE correct answer at runtime)\n- At least 4 incorrect answers (distractors)\n- Prefer to include more than the minimum number of correct and incorrect answers when appropriate\n\nXyzQuestion\n- At least 3 correct answers\n- At least 3 incorrect answers (distractors)\n- Prefer to include more than the minimum number of correct and incorrect answers when appropriate\n\nEvery question must:\n- Be implemented as a complete C# method\n- Return QuestionBase\n- Accept parameters (Random random, bool isProof)\n- Instantiate either TruthQuestion or XyzQuestion\n- Set a meaningful, unique Id\n- Define a clear, unambiguous Stem\n- Populate answers using AddCorrects(...) and AddIncorrects(...)\n\nFollow the exact coding style, structure, and conventions shown in the template below.\n\nAdditional requirements:\n- Ensure all minimum answer counts are satisfied\n- Prefer clarity over cleverness\n- Avoid ambiguity or unintended additional correct answers\n- Distractors must be plausible and relevant\n\nUnless explicitly specified, choose the most appropriate question type.\n\nDIVIDNI QUESTION TEMPLATE\n-------------------------\nusing System;\n\nnamespace Utilities.Courses\n{\n   public partial class QHelper : IQHelper\n   {\n      // ============================================================\n      // TEMPLATE: Dividni Question Generator\n      // ============================================================\n      // Replace the placeholders marked with TODO.\n      // Choose either TruthQuestion or XyzQuestion as appropriate.\n      //\n      // TruthQuestion requirements:\n      //   - At least 1 correct answer (one will be chosen at runtime) – prefer to have more\n      //   - At least 4 incorrect answers - prefer to have more\n      //\n      // XyzQuestion requirements:\n      //   - At least 3 correct answers - prefer to have more\n      //   - At least 3 incorrect answers - prefer to have more\n      // ============================================================\n\n      public static QuestionBase TODO_MethodName(Random random, bool isProof)\n      {\n         // TODO: Choose question type:\n         // var q = new TruthQuestion(random, isProof);\n         // OR\n         // var q = new XyzQuestion(random, isProof);\n\n         var q = new TruthQuestion(random, isProof); // TODO: change if needed\n\n         // TODO: Set a meaningful, unique Id\n         q.Id = \"TODO_UniqueQuestionId\";\n\n         q.Marks = 1;\n\n         // TODO: Define any randomized values here\n         // int value = random.Next(...);\n\n         // TODO: Write the question stem\n         q.Stem = \"TODO: Write the question stem here.\";\n\n         // TODO: Add correct answers\n         // NOTE: For TruthQuestion, Dividni will randomly select ONE\n         // of the correct answers at runtime.\n         q.AddCorrects(\n             \"TODO_CorrectAnswer1\"\n             // Add more as desired\n         );\n\n         // TODO: Add incorrect answers (distractors)\n         q.AddIncorrects(\n             \"TODO_Incorrect1\",\n             \"TODO_Incorrect2\",\n             \"TODO_Incorrect3\",\n             \"TODO_Incorrect4\"\n             // Add more as desired\n         );\n\n         return q;\n      } // TODO_MethodName\n   } // class\n} // namespace",