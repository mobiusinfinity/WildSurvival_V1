using System.Diagnostics;
using UnityEngine;

namespace WildSurvival.Editor.Git
{
    public static class GitUtilsV2
    {
        public struct Result
        {
            public int code;
            public string stdout;
            public string stderr;
        }

        public static Result Run(string args, string workingDir)
        {
            var p = new Process();
            p.StartInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = args,
                WorkingDirectory = string.IsNullOrEmpty(workingDir) ? Application.dataPath + "/.." : workingDir,
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            try
            {
                p.Start();
            }
            catch (System.Exception ex)
            {
                return new Result { code = -1, stdout = "", stderr = "Failed to start git: " + ex.Message };
            }
            string so = p.StandardOutput.ReadToEnd();
            string se = p.StandardError.ReadToEnd();
            p.WaitForExit();
            return new Result { code = p.ExitCode, stdout = so, stderr = se };
        }
    }
}
