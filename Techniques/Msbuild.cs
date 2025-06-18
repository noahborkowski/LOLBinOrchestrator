using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace LOLBinOrchestrator.Techniques
{
    /// <summary>
    /// MSBuild technique for executing payloads via project files
    /// </summary>
    public class Msbuild : ILOLBinTechnique
    {
        public string Name => "Msbuild";
        public string Description => "Executes payloads using MSBuild project files";

        public List<string> RequiredParameters => new List<string> { "project_file" };
        public List<string> OptionalParameters => new List<string> { "target", "properties", "verbosity", "timeout" };

        /// <summary>
        /// Executes the MSBuild technique
        /// </summary>
        /// <param name="parameters">Execution parameters</param>
        /// <returns>Execution result</returns>
        public async Task<ExecutionResult> ExecuteAsync(Dictionary<string, string> parameters)
        {
            var startTime = DateTime.UtcNow;
            var result = new ExecutionResult();

            try
            {
                if (!ValidateParameters(parameters))
                {
                    result.Success = false;
                    result.Error = "Invalid parameters provided";
                    return result;
                }

                var projectFile = parameters["project_file"];
                var target = parameters.GetValueOrDefault("target", "Build");
                var properties = parameters.GetValueOrDefault("properties", "");
                var verbosity = parameters.GetValueOrDefault("verbosity", "minimal");
                var timeoutStr = parameters.GetValueOrDefault("timeout", "60000");
                
                if (!int.TryParse(timeoutStr, out var timeout))
                    timeout = 60000;

                // Validate project file
                if (!File.Exists(projectFile))
                {
                    result.Success = false;
                    result.Error = $"Project file not found: {projectFile}";
                    return result;
                }

                // Find MSBuild executable
                var msbuildPath = FindMsbuildPath();
                if (string.IsNullOrEmpty(msbuildPath))
                {
                    result.Success = false;
                    result.Error = "MSBuild executable not found";
                    return result;
                }

                // Build command line arguments
                var args = $"\"{projectFile}\"";
                if (!string.IsNullOrEmpty(target))
                {
                    args += $" /t:{target}";
                }
                if (!string.IsNullOrEmpty(properties))
                {
                    args += $" /p:{properties}";
                }
                args += $" /v:{verbosity}";

                // Execute MSBuild
                using var process = new Process();
                process.StartInfo.FileName = msbuildPath;
                process.StartInfo.Arguments = args;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.CreateNoWindow = true;

                process.Start();
                
                var completed = await Task.Run(() => process.WaitForExit(timeout));
                
                if (!completed)
                {
                    process.Kill();
                    result.Success = false;
                    result.Error = "Process timed out";
                    result.ExitCode = -1;
                }
                else
                {
                    result.Success = process.ExitCode == 0;
                    result.Output = await process.StandardOutput.ReadToEndAsync();
                    result.Error = await process.StandardError.ReadToEndAsync();
                    result.ExitCode = process.ExitCode;
                }

                result.ExecutionTime = DateTime.UtcNow - startTime;
                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Error = $"Exception during execution: {ex.Message}";
                result.ExecutionTime = DateTime.UtcNow - startTime;
                return result;
            }
        }

        /// <summary>
        /// Finds the MSBuild executable path
        /// </summary>
        /// <returns>Path to MSBuild executable</returns>
        private string FindMsbuildPath()
        {
            // Common MSBuild locations
            var possiblePaths = new List<string>
            {
                @"C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe",
                @"C:\Program Files\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe",
                @"C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe",
                @"C:\Program Files (x86)\Microsoft Visual Studio\2019\Professional\MSBuild\Current\Bin\MSBuild.exe",
                @"C:\Program Files (x86)\Microsoft Visual Studio\2019\Enterprise\MSBuild\Current\Bin\MSBuild.exe",
                @"C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe",
                @"C:\Program Files (x86)\Microsoft Visual Studio\2017\Professional\MSBuild\15.0\Bin\MSBuild.exe",
                @"C:\Program Files (x86)\Microsoft Visual Studio\2017\Enterprise\MSBuild\15.0\Bin\MSBuild.exe",
                @"C:\Program Files (x86)\Microsoft Visual Studio\2017\Community\MSBuild\15.0\Bin\MSBuild.exe",
                @"C:\Windows\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe",
                @"C:\Windows\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe"
            };

            foreach (var path in possiblePaths)
            {
                if (File.Exists(path))
                    return path;
            }

            // Try to find via PATH
            try
            {
                using var process = new Process();
                process.StartInfo.FileName = "where";
                process.StartInfo.Arguments = "msbuild";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.CreateNoWindow = true;
                process.Start();
                
                var output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
                
                if (process.ExitCode == 0 && !string.IsNullOrWhiteSpace(output))
                {
                    var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                    if (lines.Length > 0)
                        return lines[0].Trim();
                }
            }
            catch
            {
                // Ignore errors when searching PATH
            }

            return string.Empty;
        }

        /// <summary>
        /// Validates the provided parameters
        /// </summary>
        /// <param name="parameters">Parameters to validate</param>
        /// <returns>True if valid, false otherwise</returns>
        public bool ValidateParameters(Dictionary<string, string> parameters)
        {
            if (parameters == null)
                return false;

            // Check required parameters
            foreach (var requiredParam in RequiredParameters)
            {
                if (!parameters.ContainsKey(requiredParam) || string.IsNullOrWhiteSpace(parameters[requiredParam]))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Gets help information for this technique
        /// </summary>
        /// <returns>Help text</returns>
        public string GetHelp()
        {
            return @"
MSBuild Technique Help:
======================

Required Parameters:
- project_file: Path to the MSBuild project file (.csproj, .vbproj, .xml, etc.)

Optional Parameters:
- target: MSBuild target to execute (default: Build)
- properties: MSBuild properties in format 'Property1=Value1;Property2=Value2'
- verbosity: MSBuild verbosity level (quiet, minimal, normal, detailed, diagnostic)
- timeout: Execution timeout in milliseconds (default: 60000)

Example Usage:
{
  ""name"": ""Msbuild"",
  ""parameters"": {
    ""project_file"": ""C:\\payload.xml"",
    ""target"": ""Build"",
    ""properties"": ""Configuration=Release;Platform=x64"",
    ""verbosity"": ""minimal""
  }
}

Note: This technique uses MSBuild to execute payloads embedded in project files.
";
        }
    }
} 