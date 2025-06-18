using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace LOLBinOrchestrator
{
    /// <summary>
    /// Parses JSON templates to define execution chains for LOLBin techniques
    /// </summary>
    public class ConfigLoader
    {
        private readonly string _configPath;
        private readonly JsonSerializerOptions _jsonOptions;

        public ConfigLoader(string configPath = "config.json")
        {
            _configPath = configPath;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                WriteIndented = true
            };
        }

        /// <summary>
        /// Loads execution chain configuration from JSON file
        /// </summary>
        /// <returns>Execution chain configuration</returns>
        public async Task<ExecutionChainConfig> LoadConfigAsync()
        {
            try
            {
                if (!File.Exists(_configPath))
                {
                    throw new FileNotFoundException($"Configuration file not found: {_configPath}");
                }

                var jsonContent = await File.ReadAllTextAsync(_configPath);
                var config = JsonSerializer.Deserialize<ExecutionChainConfig>(jsonContent, _jsonOptions);
                
                if (config == null)
                {
                    throw new InvalidOperationException("Failed to deserialize configuration");
                }

                return config;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error loading configuration: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Saves execution chain configuration to JSON file
        /// </summary>
        /// <param name="config">Configuration to save</param>
        public async Task SaveConfigAsync(ExecutionChainConfig config)
        {
            try
            {
                var jsonContent = JsonSerializer.Serialize(config, _jsonOptions);
                await File.WriteAllTextAsync(_configPath, jsonContent);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error saving configuration: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Creates a default configuration template
        /// </summary>
        /// <returns>Default configuration</returns>
        public ExecutionChainConfig CreateDefaultConfig()
        {
            return new ExecutionChainConfig
            {
                Name = "Default LOLBin Chain",
                Description = "Default execution chain configuration",
                Techniques = new List<TechniqueConfig>
                {
                    new TechniqueConfig
                    {
                        Name = "Regsvr32",
                        Enabled = true,
                        Parameters = new Dictionary<string, string>
                        {
                            ["dll_path"] = "payload.dll",
                            ["function"] = "DllRegisterServer"
                        }
                    },
                    new TechniqueConfig
                    {
                        Name = "Msbuild",
                        Enabled = true,
                        Parameters = new Dictionary<string, string>
                        {
                            ["project_file"] = "payload.xml",
                            ["target"] = "Build"
                        }
                    }
                },
                PayloadConfig = new PayloadConfig
                {
                    Type = "DLL",
                    Path = "payload.dll",
                    Obfuscation = new ObfuscationConfig
                    {
                        Enabled = false,
                        Method = "XOR",
                        Key = "default_key"
                    }
                }
            };
        }
    }

    /// <summary>
    /// Configuration for execution chains
    /// </summary>
    public class ExecutionChainConfig
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<TechniqueConfig> Techniques { get; set; } = new List<TechniqueConfig>();
        public PayloadConfig PayloadConfig { get; set; } = new PayloadConfig();
    }

    /// <summary>
    /// Configuration for individual techniques
    /// </summary>
    public class TechniqueConfig
    {
        public string Name { get; set; } = string.Empty;
        public bool Enabled { get; set; } = true;
        public Dictionary<string, string> Parameters { get; set; } = new Dictionary<string, string>();
    }

    /// <summary>
    /// Configuration for payload handling
    /// </summary>
    public class PayloadConfig
    {
        public string Type { get; set; } = "DLL"; // DLL, Shellcode, etc.
        public string Path { get; set; } = string.Empty;
        public ObfuscationConfig Obfuscation { get; set; } = new ObfuscationConfig();
    }

    /// <summary>
    /// Configuration for payload obfuscation
    /// </summary>
    public class ObfuscationConfig
    {
        public bool Enabled { get; set; } = false;
        public string Method { get; set; } = "XOR"; // XOR, Base64, etc.
        public string Key { get; set; } = string.Empty;
    }
} 