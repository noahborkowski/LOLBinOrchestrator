using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace LOLBinOrchestrator.Techniques
{
    /// <summary>
    /// Rundll32 technique for DLL execution via rundll32.exe
    /// </summary>
    public class Rundll32 : ILOLBinTechnique
    {
        public string Name => "Rundll32";
        public string Description => "Executes DLL payloads using rundll32.exe";

        public List<string> RequiredParameters => new List<string> { "dll_path" };
        public List<string> OptionalParameters => new List<string> { "function", "arguments", "timeout" };

        /// <summary>
        /// Executes the Rundll32 technique
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

                var dllPath = parameters["dll_path"];
                var function = parameters.GetValueOrDefault("function", "DllMain");
                var arguments = parameters.GetValueOrDefault("arguments", "");
                var timeoutStr = parameters.GetValueOrDefault("timeout", "30000");
                
                if (!int.TryParse(timeoutStr, out var timeout))
                    timeout = 30000;

                // Validate DLL path
                if (!File.Exists(dllPath))
                {
                    result.Success = false;
                    result.Error = $"DLL file not found: {dllPath}";
                    return result;
                }

                // Build command line arguments
                var args = $"\"{dllPath}\",{function}";
                if (!string.IsNullOrEmpty(arguments))
                {
                    args += $",{arguments}";
                }

                // Execute rundll32
                using var process = new Process();
                process.StartInfo.FileName = "rundll32.exe";
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
Rundll32 Technique Help:
========================

Required Parameters:
- dll_path: Path to the DLL file to execute

Optional Parameters:
- function: Function to call in the DLL (default: DllMain)
- arguments: Additional arguments to pass to the function
- timeout: Execution timeout in milliseconds (default: 30000)

Example Usage:
{
  ""name"": ""Rundll32"",
  ""parameters"": {
    ""dll_path"": ""C:\\payload.dll"",
    ""function"": ""EntryPoint"",
    ""arguments"": ""arg1,arg2"",
    ""timeout"": ""60000""
  }
}

Note: This technique uses rundll32.exe to load and execute DLL payloads.
The command format is: rundll32.exe ""dll_path"",function,arguments
";
        }
    }
} 