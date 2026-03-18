namespace Honours_Dvidni_AI.Back;

public interface IBackend {
    public abstract string test();
    public Task<string> AskAI(string prompt);
}