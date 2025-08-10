using System;
using System.Diagnostics;
using System.IO;
using System.Text;

internal static class GitUtilsV1
{
    public static bool IsGitAvailable(out string version)
    {
        version = string.Empty;
        try
        {
            if (Run("git", "--version", Directory.GetCurrentDirectory(), out var so, out var se, 5000, out var exit) && exit == 0)
            {
                version = (so ?? string.Empty).Trim();
                return true;
            }
        }
        catch { }
        return false;
    }

    public static bool RunGit(string workingDir, string args, out string stdout, out string stderr, int timeoutMs = 60000)
    {
        stdout = string.Empty;
        stderr = string.Empty;
        try
        {
            if (Run("git", args, workingDir, out var so, out var se, timeoutMs, out var exit))
            {
                stdout = so ?? string.Empty;
                stderr = se ?? string.Empty;
                return exit == 0;
            }
        }
        catch (Exception ex)
        {
            stderr = ex.Message;
        }
        return false;
    }

    private static bool Run(string fileName, string args, string workingDir, out string stdout, out string stderr, int timeoutMs, out int exitCode)
    {
        stdout = string.Empty;
        stderr = string.Empty;
        exitCode = -1;

        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = args,
            WorkingDirectory = string.IsNullOrEmpty(workingDir) ? Directory.GetCurrentDirectory() : workingDir,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        using (var p = new Process { StartInfo = psi })
        {
            var sbOut = new StringBuilder();
            var sbErr = new StringBuilder();
            p.OutputDataReceived += (s, e) => { if (e.Data != null) sbOut.AppendLine(e.Data); };
            p.ErrorDataReceived  += (s, e) => { if (e.Data != null) sbErr.AppendLine(e.Data); };

            if (!p.Start())
                return false;

            p.BeginOutputReadLine();
            p.BeginErrorReadLine();

            if (!p.WaitForExit(timeoutMs))
            {
                try { p.Kill(); } catch {}
                stderr = "Timeout exceeded";
                return false;
            }
            exitCode = p.ExitCode;
            stdout = sbOut.ToString();
            stderr = sbErr.ToString();
            return true;
        }
    }
}
