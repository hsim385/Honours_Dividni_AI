using Honours_Dividni_AI.Models;

namespace Honours_Dividni_AI.Back;

public interface IBackend {
    public abstract string test();
    public Task<QuestionFields> AskAI(string prompt);
    public Task<bool> saveQuestion(HomeViewModel model);
    public Task<HomeViewModel> completeQuestion(HomeViewModel model);
}