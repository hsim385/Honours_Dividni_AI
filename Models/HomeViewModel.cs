namespace Honours_Dividni_AI.Models;

public class HomeViewModel
{
    public string Prompt { get; set; } = "";
    public string AIModel { get; set; } = "";
    public string File { get; set; } = "";
	public string AIInvolvement { get; set; } = "";
    public bool Response { get; set; } = false;
	public bool CompleteQuestion { get; set; } = false;
    public QuestionFields Question { get; set; } = new();
}