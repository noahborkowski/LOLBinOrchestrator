using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using LOLBinOrchestrator.Techniques;

namespace LOLBinOrchestrator
{
    /// <summary>
    /// Maps string names to LOTL (Living Off The Land) modules for dynamic technique execution
    /// </summary>
    public class TechniqueRegistry
    {
        private readonly Dictionary<string, Type> _techniqueMap;
        private readonly Dictionary<string, ILOLBinTechnique> _techniqueInstances;

        public TechniqueRegistry()
        {
            _techniqueMap = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
            _techniqueInstances = new Dictionary<string, ILOLBinTechnique>(StringComparer.OrdinalIgnoreCase);
            RegisterDefaultTechniques();
        }

        /// <summary>
        /// Registers all available LOLBin techniques
        /// </summary>
        private void RegisterDefaultTechniques()
        {
            RegisterTechnique("Regsvr32", typeof(Regsvr32));
            RegisterTechnique("Msbuild", typeof(Msbuild));
            RegisterTechnique("Rundll32", typeof(Rundll32));
        }

        /// <summary>
        /// Registers a technique type with a specific name
        /// </summary>
        /// <param name="name">Name to register the technique under</param>
        /// <param name="techniqueType">Type implementing ILOLBinTechnique</param>
        public void RegisterTechnique(string name, Type techniqueType)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Technique name cannot be null or empty", nameof(name));

            if (techniqueType == null)
                throw new ArgumentNullException(nameof(techniqueType));

            if (!typeof(ILOLBinTechnique).IsAssignableFrom(techniqueType))
                throw new ArgumentException($"Type {techniqueType.Name} must implement ILOLBinTechnique", nameof(techniqueType));

            _techniqueMap[name] = techniqueType;
        }

        /// <summary>
        /// Gets a technique instance by name
        /// </summary>
        /// <param name="name">Name of the technique</param>
        /// <returns>Technique instance</returns>
        public ILOLBinTechnique GetTechnique(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Technique name cannot be null or empty", nameof(name));

            if (!_techniqueMap.ContainsKey(name))
                throw new KeyNotFoundException($"Technique '{name}' not found in registry");

            // Return cached instance if available
            if (_techniqueInstances.ContainsKey(name))
                return _techniqueInstances[name];

            // Create new instance
            var techniqueType = _techniqueMap[name];
            var instance = (ILOLBinTechnique)Activator.CreateInstance(techniqueType)!;
            _techniqueInstances[name] = instance;

            return instance;
        }

        /// <summary>
        /// Gets all registered technique names
        /// </summary>
        /// <returns>List of registered technique names</returns>
        public IEnumerable<string> GetRegisteredTechniques()
        {
            return _techniqueMap.Keys.ToList();
        }

        /// <summary>
        /// Checks if a technique is registered
        /// </summary>
        /// <param name="name">Name of the technique</param>
        /// <returns>True if registered, false otherwise</returns>
        public bool IsTechniqueRegistered(string name)
        {
            return !string.IsNullOrWhiteSpace(name) && _techniqueMap.ContainsKey(name);
        }

        /// <summary>
        /// Gets technique information including description and parameters
        /// </summary>
        /// <param name="name">Name of the technique</param>
        /// <returns>Technique information</returns>
        public TechniqueInfo GetTechniqueInfo(string name)
        {
            var technique = GetTechnique(name);
            return new TechniqueInfo
            {
                Name = name,
                Description = technique.Description,
                RequiredParameters = technique.RequiredParameters,
                OptionalParameters = technique.OptionalParameters
            };
        }

        /// <summary>
        /// Validates that all required parameters are provided for a technique
        /// </summary>
        /// <param name="name">Name of the technique</param>
        /// <param name="parameters">Provided parameters</param>
        /// <returns>Validation result</returns>
        public ValidationResult ValidateParameters(string name, Dictionary<string, string> parameters)
        {
            var technique = GetTechnique(name);
            var requiredParams = technique.RequiredParameters;
            var missingParams = requiredParams.Where(param => !parameters.ContainsKey(param)).ToList();

            return new ValidationResult
            {
                IsValid = !missingParams.Any(),
                MissingParameters = missingParams,
                Message = missingParams.Any() 
                    ? $"Missing required parameters: {string.Join(", ", missingParams)}"
                    : "All required parameters provided"
            };
        }

        /// <summary>
        /// Dynamically discovers and registers techniques from assemblies
        /// </summary>
        /// <param name="assembly">Assembly to scan for techniques</param>
        public void DiscoverTechniques(Assembly assembly)
        {
            var techniqueTypes = assembly.GetTypes()
                .Where(t => typeof(ILOLBinTechnique).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

            foreach (var techniqueType in techniqueTypes)
            {
                var name = techniqueType.Name;
                RegisterTechnique(name, techniqueType);
            }
        }
    }

    /// <summary>
    /// Information about a LOLBin technique
    /// </summary>
    public class TechniqueInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> RequiredParameters { get; set; } = new List<string>();
        public List<string> OptionalParameters { get; set; } = new List<string>();
    }

    /// <summary>
    /// Result of parameter validation
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> MissingParameters { get; set; } = new List<string>();
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    /// Interface that all LOLBin techniques must implement
    /// </summary>
    public interface ILOLBinTechnique
    {
        string Name { get; }
        string Description { get; }
        List<string> RequiredParameters { get; }
        List<string> OptionalParameters { get; }
        
        Task<ExecutionResult> ExecuteAsync(Dictionary<string, string> parameters);
        bool ValidateParameters(Dictionary<string, string> parameters);
    }

    /// <summary>
    /// Result of technique execution
    /// </summary>
    public class ExecutionResult
    {
        public bool Success { get; set; }
        public string Output { get; set; } = string.Empty;
        public string Error { get; set; } = string.Empty;
        public int ExitCode { get; set; }
        public TimeSpan ExecutionTime { get; set; }
    }
} 