namespace MeuCrudCsharp.Features.Videos.Interfaces;

public interface IProcessRunnerService
{
    Task<(
        string StandardOutput,
        string StandardError
        )> RunProcessAndGetOutputAsync(string filePath, string arguments);
    
    Task RunProcessWithProgressAsync(
        string filePath,
        string arguments,
        Func<string, Task> onProgress
    );
}