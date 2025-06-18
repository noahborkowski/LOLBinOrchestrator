# LOLBin Orchestrator

A .NET 6.0 application for orchestrating and demonstrating various Living Off the Land Binary (LOLBin) techniques for security research and testing purposes.

## Overview

LOLBin Orchestrator is a framework designed to showcase and test various LOLBin techniques commonly used in security research. It provides a structured way to execute, log, and analyze different living-off-the-land binary execution methods.

## Features

- **Modular Technique System**: Easily extensible framework for adding new LOLBin techniques
- **Configuration-Driven**: JSON-based configuration for technique parameters and execution
- **Comprehensive Logging**: Detailed execution logs with timing and results
- **Payload Management**: Support for loading and validating different payload types
- **Obfuscation Support**: Built-in payload obfuscation capabilities
- **Parameter Validation**: Automatic validation of technique parameters

## Supported Techniques

### Regsvr32
Executes DLL payloads using `regsvr32.exe` COM registration.

**Parameters:**
- `dll_path` (required): Path to the DLL file to execute
- `function` (optional): Function to call in the DLL (default: DllRegisterServer)
- `arguments` (optional): Additional arguments to pass
- `timeout` (optional): Execution timeout in milliseconds (default: 30000)

### Msbuild
Executes payloads using MSBuild project files.

**Parameters:**
- `project_file` (required): Path to the MSBuild project file
- `target` (optional): Build target to execute (default: Build)
- `verbosity` (optional): MSBuild verbosity level (default: minimal)

### Rundll32
Executes DLL payloads using `rundll32.exe`.

**Parameters:**
- `dll_path` (required): Path to the DLL file to execute
- `function` (optional): Function to call in the DLL (default: DllMain)
- `timeout` (optional): Execution timeout in milliseconds (default: 30000)

## Installation

### Prerequisites
- .NET 6.0 Runtime or SDK
- Windows operating system (for LOLBin techniques)

### Building from Source
```bash
git clone <repository-url>
cd LOLBinOrchestrator
dotnet build
```

### Running the Application
```bash
dotnet run
```

## Configuration

The application uses a `config.json` file for configuration. If no configuration file exists, a default one will be created on first run.

### Configuration Structure

```json
{
  "name": "Test LOLBin Chain",
  "description": "Test configuration for LOLBin Orchestrator",
  "techniques": [
    {
      "name": "Regsvr32",
      "enabled": false,
      "parameters": {
        "dll_path": "test_payload.dll",
        "function": "DllRegisterServer",
        "timeout": "30000"
      }
    }
  ],
  "payloadConfig": {
    "type": "DLL",
    "path": "test_payload.dll",
    "obfuscation": {
      "enabled": false,
      "method": "XOR",
      "key": "test_key"
    }
  }
}
```

### Configuration Options

- **name**: Name of the configuration
- **description**: Description of the configuration
- **techniques**: Array of technique configurations
  - **name**: Name of the technique to execute
  - **enabled**: Whether the technique should be executed
  - **parameters**: Technique-specific parameters
- **payloadConfig**: Payload configuration
  - **type**: Type of payload (DLL, etc.)
  - **path**: Path to the payload file
  - **obfuscation**: Obfuscation settings

## Usage Examples

### Basic Usage
1. Create or modify `config.json` with your desired techniques
2. Ensure payload files are in the correct location
3. Run the application: `dotnet run`

### Adding a New Technique
1. Create a new class in the `Techniques/` directory
2. Implement the `ILOLBinTechnique` interface
3. Register the technique in `TechniqueRegistry.cs`
4. Add configuration for the technique in `config.json`

### Example Technique Implementation
```csharp
public class MyTechnique : ILOLBinTechnique
{
    public string Name => "MyTechnique";
    public string Description => "Description of my technique";
    
    public List<string> RequiredParameters => new List<string> { "param1" };
    public List<string> OptionalParameters => new List<string> { "param2" };
    
    public async Task<ExecutionResult> ExecuteAsync(Dictionary<string, string> parameters)
    {
        // Implementation here
    }
}
```

## Logging

The application generates detailed logs in `lolbin_orchestrator.log` including:
- Application startup and initialization
- Configuration loading
- Technique execution details
- Execution results and timing
- Errors and warnings

## Security Considerations

⚠️ **Important**: This tool is designed for security research and testing purposes only. 

- Only use in controlled, authorized environments
- Ensure you have proper authorization before testing
- Be aware that LOLBin techniques may trigger security monitoring systems
- Use responsibly and in accordance with applicable laws and policies

## Project Structure

```
LOLBinOrchestrator/
├── Program.cs              # Main application entry point
├── ConfigLoader.cs         # Configuration management
├── TechniqueRegistry.cs    # Technique registration and management
├── Logger.cs              # Logging functionality
├── Obfuscator.cs          # Payload obfuscation
├── PayloadLoader.cs       # Payload loading and validation
├── Techniques/            # Technique implementations
│   ├── Regsvr32.cs
│   ├── Msbuild.cs
│   └── Rundll32.cs
├── config.json            # Configuration file
└── README.md             # This file
```

## Contributing

1. Fork the repository
2. Create a feature branch
3. Implement your changes
4. Add appropriate tests
5. Submit a pull request

## License

This project is licensed under the terms specified in the LICENSE file.

## Disclaimer

This tool is provided for educational and research purposes only. Users are responsible for ensuring they have proper authorization before using this tool in any environment. The authors are not responsible for any misuse of this software.
