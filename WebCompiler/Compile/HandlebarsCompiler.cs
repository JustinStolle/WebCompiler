﻿using System;
using System.IO;
using System.Text.RegularExpressions;

namespace WebCompiler
{
    internal class HandlebarsCompiler : ICompiler
    {
        private static readonly Regex _errorRx = new Regex("Error: (?<message>.+) on line (?<line>[0-9]+):", RegexOptions.Compiled);
        private string _mapPath;
        private readonly string _path;
        private string _name = string.Empty;
        private string _extension = string.Empty;
        private readonly string _output = string.Empty;
        private readonly string _error = string.Empty;
        private bool _partial = false;

        public HandlebarsCompiler(string path)
        {
            _path = path;
        }

        public CompilerResult Compile(Config config)
        {
            string baseFolder = Path.GetDirectoryName(config.FileName);
            string inputFile = Path.Combine(baseFolder, config.inputFile);

            FileInfo info = new FileInfo(inputFile);
            string content = File.ReadAllText(info.FullName);

            CompilerResult result = new CompilerResult
            {
                FileName = info.FullName,
                OriginalContent = content,
            };

            string extension = Path.GetExtension(inputFile);
            if (!string.IsNullOrWhiteSpace(extension))
            {
                _extension = extension.Substring(1);
            }

            string name = Path.GetFileNameWithoutExtension(inputFile);
            if (!string.IsNullOrWhiteSpace(name) && name.StartsWith("_"))
            {
                _name = name.Substring(1);
                _partial = true;

                // Temporarily Fix
                // TODO: Remove after actual fix
                string tempFilename = Path.Combine(Path.GetDirectoryName(inputFile), _name + ".handlebarstemp");
                info.CopyTo(tempFilename);
                info = new FileInfo(tempFilename);
                _extension = "handlebarstemp";
            }

            _mapPath = Path.ChangeExtension(inputFile, ".js.map.tmp");

            try
            {
                RunCompilerProcess(config, info);

                result.CompiledContent = _output;

                HandlebarsOptions options = HandlebarsOptions.FromConfig(config);
                if ((options.sourceMap || config.sourceMap) && File.Exists(_mapPath))
                {
                    result.SourceMap = File.ReadAllText(_mapPath);
                }

                if (_error.Length > 0)
                {
                    CompilerError ce = new CompilerError
                    {
                        FileName = inputFile,
                        Message = _error.Replace(baseFolder, string.Empty),
                        IsWarning = !string.IsNullOrEmpty(_output)
                    };

                    Match match = _errorRx.Match(_error);

                    if (match.Success)
                    {
                        ce.Message = match.Groups["message"].Value.Replace(baseFolder, string.Empty);
                        ce.LineNumber = int.Parse(match.Groups["line"].Value);
                        ce.ColumnNumber = 0;
                    }

                    result.Errors.Add(ce);
                }
            }
            catch (Exception ex)
            {
                CompilerError error = new CompilerError
                {
                    FileName = inputFile,
                    Message = string.IsNullOrEmpty(_error) ? ex.Message : _error,
                    LineNumber = 0,
                    ColumnNumber = 0,
                };

                result.Errors.Add(error);
            }
            finally
            {
                if (File.Exists(_mapPath))
                {
                    File.Delete(_mapPath);
                }
                // Temporarily Fix
                // TODO: Remove after actual fix
                if (info.Extension == ".handlebarstemp")
                {
                    info.Delete();
                }
            }

            return result;
        }

        private void RunCompilerProcess(Config config, FileInfo info)
        {
            string arguments = ConstructArguments(config);

            //ProcessStartInfo start = new ProcessStartInfo
            //{
            //    WorkingDirectory = info.Directory.FullName,
            //    UseShellExecute = false,
            //    WindowStyle = ProcessWindowStyle.Hidden,
            //    CreateNoWindow = true,
            //    FileName = "cmd.exe",
            //    Arguments = $"/c \"\"{Path.Combine(_path, "node_modules\\.bin\\handlebars.cmd")}\" \"{info.FullName}\" {arguments}\"",
            //    StandardOutputEncoding = Encoding.UTF8,
            //    StandardErrorEncoding = Encoding.UTF8,
            //    RedirectStandardOutput = true,
            //    RedirectStandardError = true,
            //};

            //start.EnvironmentVariables["PATH"] = _path + ";" + start.EnvironmentVariables["PATH"];

            //using (Process p = Process.Start(start))
            //{
            //    var stdout = p.StandardOutput.ReadToEndAsync();
            //    var stderr = p.StandardError.ReadToEndAsync();
            //    p.WaitForExit();

            //    _output = stdout.Result.Trim();
            //    _error = stderr.Result.Trim();
            //}
        }

        private string ConstructArguments(Config config)
        {
            string arguments = "";

            HandlebarsOptions options = HandlebarsOptions.FromConfig(config);

            if (options.amd)
            {
                arguments += " --amd";
            }
            else if (!string.IsNullOrEmpty(options.commonjs))
            {
                arguments += $" --commonjs \"{options.commonjs}\"";
            }

            foreach (string knownHelper in options.knownHelpers)
            {
                arguments += $" --known \"{knownHelper}\"";
            }

            if (options.knownHelpersOnly)
            {
                arguments += " --knownOnly";
            }

            if (options.forcePartial || _partial)
            {
                arguments += " --partial";
            }

            if (options.noBOM)
            {
                arguments += " --bom";
            }

            if ((options.sourceMap || config.sourceMap) && !string.IsNullOrWhiteSpace(_mapPath))
            {
                arguments += $" --map \"{_mapPath}\"";
            }

            if (!string.IsNullOrEmpty(options.@namespace))
            {
                arguments += $" --namespace \"{options.@namespace}\"";
            }

            if (!string.IsNullOrEmpty(options.root))
            {
                arguments += $" --root \"{options.root}\"";
            }

            if (!string.IsNullOrEmpty(options.name))
            {
                arguments += $" --name \"{options.name}\"";
            }
            else if (!string.IsNullOrEmpty(_name))
            {
                arguments += $" --name \"{_name}\"";
            }

            if (!string.IsNullOrEmpty(_extension))
            {
                arguments += $" --extension \"{_extension}\"";
            }

            return arguments;
        }
    }
}