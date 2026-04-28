namespace Honours_Dividni_AI.Models;
using System.Collections.Generic;

public class QuestionFields
{
    public string QID { get; set; } = "";
    public string QStem { get; set; } = "";
    public string QType { get; set; } = "";
    public List<string> Correct { get; set; } = new();
    public List<string> Incorrect { get; set; } = new();
}