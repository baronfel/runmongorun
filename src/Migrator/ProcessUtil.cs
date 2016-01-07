
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
                string errorData;
                if (TryParseErrorLine(d.Data, out errorData))
                {
                    errored = true;
                    errFunc(errorData);
                }
                else
                {
                    logFunc(d.Data);
                }
            };
            process.ErrorDataReceived += (o, d) =>
            {
                if (string.IsNullOrEmpty(d.Data) || string.IsNullOrWhiteSpace(d.Data)) return;
                errored = true;
                errFunc(d.Data);
            };
            process.Start();

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.StandardInput.WriteLine(op.Header);
            process.StandardInput.WriteLine(string.Concat(string.Empty, op.Ops));
            process.StandardInput.WriteLine(op.Footer);
            process.StandardInput.WriteLine("exit");
            process.WaitForExit();


            if (errored)
            {
                return Result<bool, int>.FailWith(process.ExitCode);
            }
            else
            {
                return Result<bool, int>.Succeed(true, 0);
            }
        }

        static bool TryParseErrorLine(string data, out string err)
        {
            var parts = data.Split(' ');
            if(parts.Length > 3 && parts[1] == "E")
            {
                // error line! 
                err = data.Substring((parts.Take(3).Sum(x => x.Length) + 2)).Trim();
                return true;
            }
            err = string.Empty;
            return false;
        }
    }
}
