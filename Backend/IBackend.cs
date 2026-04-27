using Honours_Dividni_AI.Models;

namespace Honours_Dividni_AI.Back;

public interface IBackend {
    public abstract string test();
    public Task<QuestionFields> AskAI(string prompt);
}