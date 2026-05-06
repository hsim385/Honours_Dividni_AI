#pragma warning disable OPENAI001
using Honours_Dividni_AI.Models;
using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
// using Google.GenAI;
// using Google.GenAI.Types;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Mscc.GenerativeAI;
using System.Collections.Generic;


namespace Honours_Dividni_AI.Back;

public class Backend : IBackend
{
    private readonly GoogleAI _client;
	private readonly AIOptions _options;
    private readonly IWebHostEnvironment _webHost;

    public Backend(IOptions<AIOptions> options, IWebHostEnvironment webHost)
    {
        _options = options.Value;
        _client = new GoogleAI(_options.ApiKey);
        _webHost = webHost;
    }
    public string test()
    {
        return "0";
    }
    public async Task<QuestionFields> AskAI(string prompt)
    {
		// System.Console.WriteLine(_options.Instructions);
		string fullPrompt = _options.Instructions +
                            "Using the Dividni template, create a new question with these instructions:\n" +
        "\n" +
        "\n" +
        "Generate a complete C# method, filling in the template appropriately and following all Dividni rules.\n" +
        $"Make a question about {prompt}";
        
        //Console.WriteLine(fullPrompt);
        
//         var requestBody = new
//         {
//             contents = new[]
//             {
//                 new
//                 {
//                     role = "user",
//                     parts = new[]
//                     {
//                         new { text = _options.Instructions },
//                         new { text = fullPrompt }
//                     }
//                 }
//             }
//         };
//         
//         string json = JsonSerializer.Serialize(requestBody);
//
//         var httpClient = _client; // assume already initialized with API key headers
//
//         var response = await httpClient.PostAsync(
//             "https://generativelanguage.googleapis.com/v1/models/gemini-1.5-pro:generateContent",
//             new StringContent(json, Encoding.UTF8, "application/json")
//         );
//
//         response.EnsureSuccessStatusCode();
//
//         var responseJson = await response.Content.ReadAsStringAsync();
//
// // Deserialize if needed
//         var result = JsonSerializer.Deserialize<JsonElement>(responseJson);
//
// // Extract text
//         string output = result
//             .GetProperty("candidates")[0]
//             .GetProperty("content")
//             .GetProperty("parts")[0]
//             .GetProperty("text")
//             .GetString();

        // var response = await _client.Models.GenerateContentAsync(
        //     model: _options.Model, contents: fullPrompt
        // );
        //
        // var output = response.Candidates[0].Content.Parts[0].Text;
        //     
        // Console.WriteLine(output);

        var model = _client.GenerativeModel("gemini-2.5-flash");
        
        var response = await model.GenerateContent(fullPrompt);
        
        //Console.WriteLine(response.Text);
        
        string trimmedResponse = RemoveCodeFences(response.Text); //RemoveCodeFences(
        
        // var folderPath = Path.Combine(_webHost.WebRootPath, "QuestionOutput");
        //
        // if (!Directory.Exists(folderPath))
        // {
        //     Directory.CreateDirectory(folderPath);
        // }
        
        QuestionFields fields = new QuestionFields();

        string QID = FindSubstring(trimmedResponse, "q.Id = ", ";");
        
        fields.QID = QID.Substring(1, QID.Length - 2);

		fields.QType = FindSubstring(trimmedResponse, "var q = new ", "Question(random, isProof);");
        
        string QStem = FindSubstring(trimmedResponse, "q.Stem = ", ";");
        
        fields.QStem = QStem.Substring(1, QStem.Length - 2);

        string correctString = FindSubstring(trimmedResponse, " q.AddCorrects(", ");");
        
        fields.Correct = processQuestions(correctString).ToList();

		string incorrectString = FindSubstring(trimmedResponse, " q.AddIncorrects(", ");");
        
        fields.Incorrect = processQuestions(incorrectString).ToList();
        
        return fields;

        string[] processQuestions(string text)
        {
            string newText = text.Replace("\t", "").Replace("   ", "");

            string[] textSplit = newText.Split('\n');

            int newLength = 0;

            for (int i = 0; i < textSplit.Length; i++)
            {
                if (!string.IsNullOrWhiteSpace(textSplit[i]))
                {
                    newLength++;
                }
            }

            string[] temp = new String[newLength];
            int index = 0;
            
            for (int i = 0; i < textSplit.Length; i++)
            {
                if (!string.IsNullOrWhiteSpace(textSplit[i]))
                {
                    temp[index++] = textSplit[i][1..^2];
                }
            }

            return temp;
        }
        
        string RemoveCodeFences(string text)
        {
            return Regex.Replace(text, @"```[a-zA-Z]*\n?|```", "").Trim();
        }

        string FindSubstring(string text, string start, string end)
        {
            int startIndex = text.IndexOf(start);
            if (startIndex != -1)
            {
                startIndex += start.Length;
                int endIndex = text.IndexOf(end, startIndex);

                if (endIndex != -1)
                {
                    string result = text.Substring(startIndex, endIndex - startIndex);
                    Console.WriteLine(result);
                    return result;
                }
            }
            return "Placeholder";
        }
    }

    public async Task<bool> saveQuestion(HomeViewModel model)
    {
        var folderPath = Path.Combine(_webHost.WebRootPath, "QuestionOutput");
        
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        string file = "using System;\n" +
                "\n" +
                "namespace Utilities.Courses\n" +
                "{\n" +
                "\tpublic partial class QHelper : IQHelper\n" +
                "\t{\n" +
                $"\t\tpublic static QuestionBase {model.Question.QID}(Random random, bool isProof)\n" +
                "\t\t{\n" +
                $"\t\t\tvar q = new {model.Question.QType}Question(random, isProof);\n" +
                "\n" +
                $"\t\t\tq.Id = \"{model.Question.QID}\";\n" +
                "\n" +
                "\t\t\tq.Marks = 1;\n" +
                "\n" +
                $"\t\t\tq.Stem = \"{model.Question.QStem}\";\n" +
                "\n" +
                "\t\t\tq.AddCorrects(\n";

        file += string.Join(",\n",
            model.Question.Correct.Select(x => $"\t\t\t\t\"{x}\"")
        ) + "\n";
        
        file += "\t\t\t);\n" +
                "\n" +
                "\t\t\tq.AddIncorrects(\n";
        
        file += string.Join(",\n",
            model.Question.Incorrect.Select(x => $"\t\t\t\t\"{x}\"")
        ) + "\n";

        file += "\t\t\t);\n" +
                "\n" +
                "\t\t\treturn q;\n" +
                "\t\t}\n" +
                "\t}\n" +
                "}";
        
        var filePath = Path.Combine(folderPath, $"{model.Question.QID}.cs");

        await System.IO.File.WriteAllTextAsync(filePath, file);
        return true;
    }
}

// "AISettings": {
//     "Model": "gpt-5.4",
//     "Instructions": "You are an assistant that helps generate Dividni multiple-choice questions in C#.\nUse upper or intermediate levels of Bloom's taxonomy.\n\nDividni supports two question types:\n\nTruthQuestion\n- At least 1 correct answer (Dividni will randomly select ONE correct answer at runtime)\n- At least 4 incorrect answers (distractors)\n- Prefer to include more than the minimum number of correct and incorrect answers when appropriate\n\nXyzQuestion\n- At least 3 correct answers\n- At least 3 incorrect answers (distractors)\n- Prefer to include more than the minimum number of correct and incorrect answers when appropriate\n\nEvery question must:\n- Be implemented as a complete C# method\n- Return QuestionBase\n- Accept parameters (Random random, bool isProof)\n- Instantiate either TruthQuestion or XyzQuestion\n- Set a meaningful, unique Id\n- Define a clear, unambiguous Stem\n- Populate answers using AddCorrects(...) and AddIncorrects(...)\n\nFollow the exact coding style, structure, and conventions shown in the template below.\n\nAdditional requirements:\n- Ensure all minimum answer counts are satisfied\n- Prefer clarity over cleverness\n- Avoid ambiguity or unintended additional correct answers\n- Distractors must be plausible and relevant\n\nUnless explicitly specified, choose the most appropriate question type.\n\nDIVIDNI QUESTION TEMPLATE\n-------------------------\nusing System;\n\nnamespace Utilities.Courses\n{\n   public partial class QHelper : IQHelper\n   {\n      // ============================================================\n      // TEMPLATE: Dividni Question Generator\n      // ============================================================\n      // Replace the placeholders marked with TODO.\n      // Choose either TruthQuestion or XyzQuestion as appropriate.\n      //\n      // TruthQuestion requirements:\n      //   - At least 1 correct answer (one will be chosen at runtime) – prefer to have more\n      //   - At least 4 incorrect answers - prefer to have more\n      //\n      // XyzQuestion requirements:\n      //   - At least 3 correct answers - prefer to have more\n      //   - At least 3 incorrect answers - prefer to have more\n      // ============================================================\n\n      public static QuestionBase TODO_MethodName(Random random, bool isProof)\n      {\n         // TODO: Choose question type:\n         // var q = new TruthQuestion(random, isProof);\n         // OR\n         // var q = new XyzQuestion(random, isProof);\n\n         var q = new TruthQuestion(random, isProof); // TODO: change if needed\n\n         // TODO: Set a meaningful, unique Id\n         q.Id = \"TODO_UniqueQuestionId\";\n\n         q.Marks = 1;\n\n         // TODO: Define any randomized values here\n         // int value = random.Next(...);\n\n         // TODO: Write the question stem\n         q.Stem = \"TODO: Write the question stem here.\";\n\n         // TODO: Add correct answers\n         // NOTE: For TruthQuestion, Dividni will randomly select ONE\n         // of the correct answers at runtime.\n         q.AddCorrects(\n             \"TODO_CorrectAnswer1\"\n             // Add more as desired\n         );\n\n         // TODO: Add incorrect answers (distractors)\n         q.AddIncorrects(\n             \"TODO_Incorrect1\",\n             \"TODO_Incorrect2\",\n             \"TODO_Incorrect3\",\n             \"TODO_Incorrect4\"\n             // Add more as desired\n         );\n\n         return q;\n      } // TODO_MethodName\n   } // class\n} // namespace",
//     "ApiKey": "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"
//   }