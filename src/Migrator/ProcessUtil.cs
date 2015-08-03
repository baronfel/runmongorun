
using Chessie.ErrorHandling;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Migrator
{
    public static class ProcessUtil
    {
        public struct ConsoleOps
        {
            public ConsoleOps(string header, IEnumerable<string> ops, string footer)
            {
                Header = header;
                Ops = ops;
                Footer = footer;
            }
            public string Header { get; }
            public IEnumerable<string> Ops { get; }
            public string Footer { get; }
        }

        public static Result<string, string> ExecProcess(string fileToExec, string workingDir, string args, ConsoleOps op)
        {
            var startInfo = new ProcessStartInfo()
            {
                FileName = fileToExec,
                WorkingDirectory = workingDir,
                Arguments = args,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                RedirectStandardInput = true,
                UseShellExecute = false
            };

            var process = new Process
            {
                StartInfo = startInfo,
            };

            var outBuilder = new StringBuilder();
            var errBuilder = new StringBuilder();
            process.OutputDataReceived += (o, d) => outBuilder.AppendLine(d.Data);
            process.ErrorDataReceived += (o, d) => errBuilder.AppendLine(d.Data);
            process.Start();
            process.StandardInput.WriteLine(op.Header);

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            foreach (var line in op.Ops)
            {
                process.StandardInput.WriteLine(line);
            }

            process.StandardInput.WriteLine(op.Footer);
            process.StandardInput.WriteLine("exit");
            process.WaitForExit();

            if (!string.IsNullOrEmpty(errBuilder.ToString())) return Result<string, string>.FailWith(errBuilder.ToString());
            return Result<string,string>.Succeed(outBuilder.ToString());
        }

    }
}
