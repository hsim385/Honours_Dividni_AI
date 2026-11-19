using Honours_Dividni_AI.Models;

namespace Honours_Dividni_AI.Back;

public interface IBackend {
    public abstract string test();
    public Task<string> AskAI(string prompt, string filename, string AIModel);
    public Task<bool> saveQuestion(HomeViewModel model);
    public Task<HomeViewModel> completeQuestion(HomeViewModel model);
	public Task<bool> DeleteFile(string File);
	public Task<QuestionFields> cSharpToJson(string cSharp);
}