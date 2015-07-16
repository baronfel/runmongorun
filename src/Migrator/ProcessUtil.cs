
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

        public static ExecResult ExecProcess(string exepath, string args, ConsoleOps op)
        {
            var startInfo = new ProcessStartInfo()
            {
                FileName = exepath,
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
            return new ExecResult(process.ExitCode, outBuilder.ToString(), errBuilder.ToString());
        }

        public struct ExecResult
        {
            public ExecResult(int exitCode, string info, string err)
            {
                ExitCode = exitCode;
                Info = info;
                Error = err;
            }
            public int ExitCode { get; }
            public string Info { get; }
            public string Error { get; }
        }

        public static Result<ExecResult> Lift(ExecResult r)
        {
            if (r.ExitCode != 0) return Result<ExecResult>.Fail(r, new System.Exception(r.Error));
            return Result<ExecResult>.Pass(r);
        }
    }
}
