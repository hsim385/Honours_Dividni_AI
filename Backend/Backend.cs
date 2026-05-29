#pragma warning disable OPENAI001
using Honours_Dividni_AI.Models;
using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Google.GenAI;
using Google.GenAI.Types;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;



namespace Honours_Dividni_AI.Back;

public class Backend : IBackend
{
    private readonly Client _client;
	private readonly AIOptions _options;
    private readonly IWebHostEnvironment _webHost;

    public Backend(IOptions<AIOptions> options, IWebHostEnvironment webHost)
    {
        _options = options.Value;
        // _client = new GoogleAI(_options.ApiKey);
        _client = new Client();
        _webHost = webHost;
    }
    public string test()
    {
        return "0";
    }
    public async Task<QuestionFields> AskAI(string prompt, string filename)
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




        ProcessStartInfo start = new ProcessStartInfo
        {
            FileName = "python.exe",
            Arguments = "Backend\\AIAccess.py",
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        var payload = new
        {
          filename = filename,
          prompt = fullPrompt + "Here is a pdf of lecture slides, try and make the question from information present here"
        };
        string json = JsonSerializer.Serialize(payload);
        string output = "";
        using (Process process = Process.Start(start))
        {
            // Send JSON to Python
            await process.StandardInput.WriteAsync(json);
            process.StandardInput.Close();

            // Read Python output
            Task<string> outputTask = process.StandardOutput.ReadToEndAsync();
            Task<string> errorTask = process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync();
            output = await outputTask;
            string errors = await errorTask;
            if (!string.IsNullOrWhiteSpace(errors))
            {
	            Console.WriteLine(errors);
            }
        }

       // var response = await _client.Models.GenerateContentAsync(
        //    model: _options.Model, contents: fullPrompt
        //);
        
       // var output = response.Candidates[0].Content.Parts[0].Text;
            
        Console.WriteLine(output);

        // var model = _client.GenerativeModel("gemini-2.5-flash");
        //
        // var response = await model.GenerateContent(fullPrompt);
        
        //Console.WriteLine(response.Text);
        
        string trimmedResponse = RemoveCodeFences(output); 
        
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

//		Console.WriteLine(string.Join("\n", fields.Incorrect));
        
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
                "}\n" +
				$"//{model.AIInvolvement}";

        var filePath = Path.Combine(folderPath, $"{model.Question.QID}.cs");

        await System.IO.File.WriteAllTextAsync(filePath, file);
        return true;
    }

	public async Task<HomeViewModel> completeQuestion(HomeViewModel model)
	{
		string prompt = "This question is partially complete, you will be given the values for the fields and require to make decesions for the remaining fields and return the full C# file for the question. ensure the file respects the requirements given to you. Also note that the way the program is setup, it will always give you the correct number of questions, however it is possible that they will be empty, these empty strigns have been replaced by the phrase \"empty string\" so please replace that phrase whereever you find it in the question field you are expected to fill them.\n";
		prompt += "The prompt provided with the question was: ";
		prompt += (string.IsNullOrWhiteSpace(model.Prompt)) ? "Not Provided\n" : (model.Prompt + "\n");
		prompt += "Question ID: ";
		prompt += (string.IsNullOrWhiteSpace(model.Question.QID)) ? "Not Provided\n" : (model.Question.QID + "\n");
        prompt += "Xyz or Truth: ";
        prompt += (string.IsNullOrWhiteSpace(model.Question.QType)) ? "Not Specified\n" : (model.Question.QType + "\n");
		prompt += "Question Stem: ";
		prompt += (string.IsNullOrWhiteSpace(model.Question.QStem)) ? "Not Provided\n" : (model.Question.QStem + "\n");

        prompt += "Correct Questions: ";
		if (model.Question.Correct.Count == 0)
		{
			prompt += "None Given\n";
		}
		else 
		{
			prompt += string.Join(",\n",
                model.Question.Correct.Select(x =>
        			"\t" + (string.IsNullOrWhiteSpace(x) ? "empty string" : x)
    			)
                ) + "\n";
		}

		prompt += "Incorrect Questions: ";
		if (model.Question.Incorrect.Count == 0) 
		{
			prompt += "None Given";
		}
		else 
		{
			prompt += string.Join(",\n",
            	model.Question.Incorrect.Select(x =>
        			"\t" + (string.IsNullOrWhiteSpace(x) ? "empty string" : x)
    			)
        	);
		}
        
		model.Question = await AskAI(prompt);
        
        return model;
	}

	public async Task<bool> DeleteFile(string File)
	{
    	string path = System.IO.Path.Combine(Directory.GetCurrentDirectory(), @"wwwroot\uploads", File);
	if (System.IO.File.Exists(path))
    {
        System.IO.File.Delete(path);
        return true;
    }
    else
    {
        return false;
    }
	}
}