using System.Diagnostics;

namespace Schach.Engine;

public sealed class UciEngineClient : IDisposable
{
    private Process? process;

    public bool IsRunning => process is { HasExited: false };

    public void Start(string enginePath)
    {
        Stop();
        process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = enginePath,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();
        Send("uci");
    }

    public void SendPositionFen(string fen)
    {
        Send($"position fen {fen}");
    }

    public void SearchBestMove(int moveTimeMilliseconds = 500)
    {
        Send($"go movetime {moveTimeMilliseconds}");
    }

    public string? ReadLine()
    {
        return process?.StandardOutput.ReadLine();
    }

    public void Stop()
    {
        if (process is null)
        {
            return;
        }

        if (!process.HasExited)
        {
            Send("quit");
            process.WaitForExit(300);
        }

        process.Dispose();
        process = null;
    }

    public void Dispose()
    {
        Stop();
    }

    private void Send(string command)
    {
        if (process is { HasExited: false })
        {
            process.StandardInput.WriteLine(command);
            process.StandardInput.Flush();
        }
    }
}
