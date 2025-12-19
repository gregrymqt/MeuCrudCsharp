using System.Diagnostics;
using MeuCrudCsharp.Features.Exceptions;
using MeuCrudCsharp.Features.Videos.Interfaces;

namespace MeuCrudCsharp.Features.Videos.Services;

public class ProgressRunnerService : IProcessRunnerService
{
    public async Task RunProcessWithProgressAsync(
        string filePath,
        string arguments,
        Func<string, Task> onProgress
    )
    {
        var processStartInfo = new ProcessStartInfo
        {
            FileName = filePath,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true, // FFMPEG manda logs no Error
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        using var process = Process.Start(processStartInfo);
        if (process == null)
            throw new AppServiceException($"Não foi possível iniciar o processo '{filePath}'.");

        // Lê o stream em tempo real para pegar a duração/tempo
        while (!process.StandardError.EndOfStream)
        {
            var line = await process.StandardError.ReadLineAsync();
            if (line != null)
            {
                await onProgress(line);
            }
        }

        string error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            throw new AppServiceException(
                $"Processo '{filePath}' falhou (Code {process.ExitCode}). Detalhes: {error}"
            );
        }
    }

    public async Task<(string StandardOutput, string StandardError)> RunProcessAndGetOutputAsync(
        string filePath,
        string arguments
    )
    {
        var processStartInfo = new ProcessStartInfo
        {
            FileName = filePath,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        using var process = Process.Start(processStartInfo);
        if (process == null)
            throw new AppServiceException($"Não foi possível iniciar '{filePath}'.");

        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            string error = await errorTask;
            throw new AppServiceException(
                $"Erro ao executar '{filePath}' (Code {process.ExitCode}): {error}"
            );
        }

        return (await outputTask, await errorTask);
    }
}
