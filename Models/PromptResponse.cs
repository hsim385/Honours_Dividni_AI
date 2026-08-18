namespace Honours_Dividni_AI.Models;

public class PromptResponse
{
    public string Name { get; set; } = "";
    public string Type { get; set; } = "";
    public string Id { get; set; } = "";
    public int Marks { get; set; } = 1;
    public string Stem { get; set; } = "";
    public string[] Corrects { get; set; } = [];
    public string[] Incorrects { get; set; } = [];
}