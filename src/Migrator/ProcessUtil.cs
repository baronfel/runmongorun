
using Chessie.ErrorHandling;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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

        public static Tuple<string, string> ExecProcess(string fqnMongoPath, string args, ConsoleOps op)
        {
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

            var outBuilder = new StringBuilder();
            var errBuilder = new StringBuilder();
            process.OutputDataReceived += (o, d) =>
            {
                if (string.IsNullOrEmpty(d.Data)) return;
                string errorData;
                if (TryParseErrorLine(d.Data, out errorData))
                {
                    errBuilder.AppendLine(errorData);
                }
                else
                {
                    outBuilder.AppendLine(d.Data);
                }
            };
            process.ErrorDataReceived += (o, d) =>
            {
                if (string.IsNullOrEmpty(d.Data)) return;
                errBuilder.AppendLine(d.Data);
            };
            process.Start();

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.StandardInput.WriteLine(op.Header);
            process.StandardInput.WriteLine(string.Concat(string.Empty, op.Ops));
            process.StandardInput.WriteLine(op.Footer);
            process.StandardInput.WriteLine("exit");
            process.WaitForExit();

            var error = errBuilder.ToString();
            if (!string.IsNullOrEmpty(error) && !error.Equals(Environment.NewLine)) {
                return Tuple.Create(outBuilder.ToString(), error);
            }
            else {
                return Tuple.Create(outBuilder.ToString(), string.Empty);
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
