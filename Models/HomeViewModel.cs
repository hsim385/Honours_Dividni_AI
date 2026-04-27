namespace Honours_Dividni_AI.Models;

public class HomeViewModel
{
    public string Prompt { get; set; } = "";
    public bool Response { get; set; } = false;
    public QuestionFields Question { get; set; } = new QuestionFields();
}