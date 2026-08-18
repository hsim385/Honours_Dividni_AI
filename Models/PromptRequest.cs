namespace Honours_Dividni_AI.Models;

public class PromptRequest
{ 
    public string Prompt { get; set; } = "";
    public string AIModel { get; set; } = "";
    public string File { get; set; } = "";
    public int NOOFQuestions { get; set; } = 1;
}