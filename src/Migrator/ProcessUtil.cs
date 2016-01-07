
using Chessie.ErrorHandling;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

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

        public static Result<bool, int> ExecProcess(string fqnMongoPath, string args, ConsoleOps op, Action<string> logFunc, Action<string> errFunc)
        {
            bool errored = false;
            var startInfo = new ProcessStartInfo()
            {
                FileName = fqnMongoPath,
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

            process.OutputDataReceived += (o, d) =>
            {
                if (string.IsNullOrEmpty(d.Data)) return;
                if (TryParseErrorLine(d.Data))
                {
                    errored = true;
                    errFunc("-----" + d.Data);
                }
                else
                {
                    logFunc("-----" + d.Data);
                }
            };
            process.ErrorDataReceived += (o, d) =>
            {
                if (string.IsNullOrEmpty(d.Data) || string.IsNullOrWhiteSpace(d.Data)) return;
                errored = true;
                errFunc("-----" + d.Data);
            };
            process.Start();

            logFunc(op.Header);
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            var command = string.Join(string.Empty, op.Ops);
            process.StandardInput.WriteLine(command);
            process.StandardInput.WriteLine("exit");
            process.WaitForExit();
            logFunc(op.Footer);

            if (errored)
            {
                return Result<bool, int>.FailWith(process.ExitCode);
            }
            else
            {
                return Result<bool, int>.Succeed(true, 0);
            }
        }

        static bool TryParseErrorLine(string data)
        {
            var parts = data.Split(' ');
            if (parts.Length >= 2 && parts[1] == "E")
            {
                return true;
            }
            if (parts.Length >= 3 && parts[2].Equals("SyntaxError:", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            return false;
        }
    }
}
