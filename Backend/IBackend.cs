namespace ex3.Back;

public interface IBackend {
    public abstract string test();
    public Task<string> AskAI(string prompt);
}