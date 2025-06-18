using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace LOLBinOrchestrator
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("=== LOLBin Orchestrator ===");
            Console.WriteLine("Starting up...\n");

            try
            {
                // Initialize components
                var logger = new Logger();
                var obfuscator = new Obfuscator();
                var payloadLoader = new PayloadLoader(obfuscator);
                var configLoader = new ConfigLoader();
                var techniqueRegistry = new TechniqueRegistry();

                await logger.InfoAsync("LOLBin Orchestrator initialized successfully");

                // Check if config file exists, create default if not
                if (!File.Exists("config.json"))
                {
                    await logger.InfoAsync("No config.json found, creating default configuration");
                    var defaultConfig = configLoader.CreateDefaultConfig();
                    await configLoader.SaveConfigAsync(defaultConfig);
                    await logger.InfoAsync("Default configuration saved to config.json");
                }

                // Load configuration
                var config = await configLoader.LoadConfigAsync();
                await logger.LogConfigLoadAsync("config.json", config);

                // Display available techniques
                Console.WriteLine("Available Techniques:");
                foreach (var techniqueName in techniqueRegistry.GetRegisteredTechniques())
                {
                    var info = techniqueRegistry.GetTechniqueInfo(techniqueName);
                    Console.WriteLine($"  - {info.Name}: {info.Description}");
                }
                Console.WriteLine();

                // Execute techniques from configuration
                foreach (var techniqueConfig in config.Techniques)
                {
                    if (!techniqueConfig.Enabled)
                    {
                        await logger.InfoAsync($"Skipping disabled technique: {techniqueConfig.Name}");
                        continue;
                    }

                    Console.WriteLine($"Executing technique: {techniqueConfig.Name}");
                    
                    try
                    {
                        var technique = techniqueRegistry.GetTechnique(techniqueConfig.Name);
                        var startTime = DateTime.UtcNow;
                        
                        var result = await technique.ExecuteAsync(techniqueConfig.Parameters);
                        var executionTime = DateTime.UtcNow - startTime;

                        await logger.LogTechniqueExecutionAsync(
                            techniqueConfig.Name, 
                            techniqueConfig.Parameters, 
                            result, 
                            executionTime
                        );

                        Console.WriteLine($"  Result: {(result.Success ? "SUCCESS" : "FAILED")}");
                        if (!result.Success)
                        {
                            Console.WriteLine($"  Error: {result.Error}");
                        }
                    }
                    catch (Exception ex)
                    {
                        await logger.ErrorAsync($"Failed to execute technique {techniqueConfig.Name}", ex);
                        Console.WriteLine($"  Error: {ex.Message}");
                    }
                }

                // Load and validate payload if configured
                if (!string.IsNullOrEmpty(config.PayloadConfig.Path))
                {
                    Console.WriteLine($"\nLoading payload: {config.PayloadConfig.Path}");
                    
                    var validationResult = await payloadLoader.ValidatePayloadAsync(
                        config.PayloadConfig.Path, 
                        config.PayloadConfig.Type
                    );

                    if (validationResult.IsValid)
                    {
                        var payloadResult = await payloadLoader.LoadPayloadAsync(config.PayloadConfig);
                        await logger.LogPayloadLoadAsync(
                            config.PayloadConfig.Path, 
                            config.PayloadConfig.Type, 
                            payloadResult
                        );

                        Console.WriteLine($"  Payload load result: {(payloadResult.Success ? "SUCCESS" : "FAILED")}");
                    }
                    else
                    {
                        await logger.WarningAsync($"Payload validation failed: {validationResult.Error}");
                        Console.WriteLine($"  Payload validation failed: {validationResult.Error}");
                    }
                }

                await logger.InfoAsync("LOLBin Orchestrator execution completed");
                Console.WriteLine("\nExecution completed. Check the log file for details.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fatal error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }
    }
} 