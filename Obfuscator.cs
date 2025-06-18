using System;
using System.Text;
using System.Threading.Tasks;

namespace LOLBinOrchestrator
{
    /// <summary>
    /// Handles payload obfuscation using various encoding methods
    /// </summary>
    public class Obfuscator
    {
        /// <summary>
        /// Obfuscates data using the specified method
        /// </summary>
        /// <param name="data">Data to obfuscate</param>
        /// <param name="config">Obfuscation configuration</param>
        /// <returns>Obfuscated data</returns>
        public async Task<byte[]> ObfuscateAsync(byte[] data, ObfuscationConfig config)
        {
            if (data == null || data.Length == 0)
                return data;

            if (!config.Enabled)
                return data;

            return config.Method.ToLowerInvariant() switch
            {
                "xor" => await XorObfuscateAsync(data, config.Key),
                "base64" => await Base64ObfuscateAsync(data),
                "rot13" => await Rot13ObfuscateAsync(data),
                "caesar" => await CaesarObfuscateAsync(data, config.Key),
                _ => throw new ArgumentException($"Unsupported obfuscation method: {config.Method}")
            };
        }

        /// <summary>
        /// Deobfuscates data using the specified method
        /// </summary>
        /// <param name="data">Data to deobfuscate</param>
        /// <param name="config">Obfuscation configuration</param>
        /// <returns>Deobfuscated data</returns>
        public async Task<byte[]> DeobfuscateAsync(byte[] data, ObfuscationConfig config)
        {
            if (data == null || data.Length == 0)
                return data;

            if (!config.Enabled)
                return data;

            return config.Method.ToLowerInvariant() switch
            {
                "xor" => await XorDeobfuscateAsync(data, config.Key),
                "base64" => await Base64DeobfuscateAsync(data),
                "rot13" => await Rot13DeobfuscateAsync(data),
                "caesar" => await CaesarDeobfuscateAsync(data, config.Key),
                _ => throw new ArgumentException($"Unsupported obfuscation method: {config.Method}")
            };
        }

        /// <summary>
        /// XOR obfuscation
        /// </summary>
        /// <param name="data">Data to obfuscate</param>
        /// <param name="key">XOR key</param>
        /// <returns>Obfuscated data</returns>
        private async Task<byte[]> XorObfuscateAsync(byte[] data, string key)
        {
            if (string.IsNullOrEmpty(key))
                key = "default_key";

            var keyBytes = Encoding.UTF8.GetBytes(key);
            var result = new byte[data.Length];

            for (int i = 0; i < data.Length; i++)
            {
                result[i] = (byte)(data[i] ^ keyBytes[i % keyBytes.Length]);
            }

            return await Task.FromResult(result);
        }

        /// <summary>
        /// XOR deobfuscation
        /// </summary>
        /// <param name="data">Data to deobfuscate</param>
        /// <param name="key">XOR key</param>
        /// <returns>Deobfuscated data</returns>
        private async Task<byte[]> XorDeobfuscateAsync(byte[] data, string key)
        {
            // XOR is symmetric, so deobfuscation is the same as obfuscation
            return await XorObfuscateAsync(data, key);
        }

        /// <summary>
        /// Base64 obfuscation
        /// </summary>
        /// <param name="data">Data to obfuscate</param>
        /// <returns>Obfuscated data</returns>
        private async Task<byte[]> Base64ObfuscateAsync(byte[] data)
        {
            var base64String = Convert.ToBase64String(data);
            return await Task.FromResult(Encoding.UTF8.GetBytes(base64String));
        }

        /// <summary>
        /// Base64 deobfuscation
        /// </summary>
        /// <param name="data">Data to deobfuscate</param>
        /// <returns>Deobfuscated data</returns>
        private async Task<byte[]> Base64DeobfuscateAsync(byte[] data)
        {
            var base64String = Encoding.UTF8.GetString(data);
            return await Task.FromResult(Convert.FromBase64String(base64String));
        }

        /// <summary>
        /// ROT13 obfuscation (for text data)
        /// </summary>
        /// <param name="data">Data to obfuscate</param>
        /// <returns>Obfuscated data</returns>
        private async Task<byte[]> Rot13ObfuscateAsync(byte[] data)
        {
            var text = Encoding.UTF8.GetString(data);
            var result = new StringBuilder();

            foreach (char c in text)
            {
                if (char.IsLetter(c))
                {
                    var offset = char.IsUpper(c) ? 'A' : 'a';
                    result.Append((char)(((c - offset + 13) % 26) + offset));
                }
                else
                {
                    result.Append(c);
                }
            }

            return await Task.FromResult(Encoding.UTF8.GetBytes(result.ToString()));
        }

        /// <summary>
        /// ROT13 deobfuscation (for text data)
        /// </summary>
        /// <param name="data">Data to deobfuscate</param>
        /// <returns>Deobfuscated data</returns>
        private async Task<byte[]> Rot13DeobfuscateAsync(byte[] data)
        {
            // ROT13 is symmetric, so deobfuscation is the same as obfuscation
            return await Rot13ObfuscateAsync(data);
        }

        /// <summary>
        /// Caesar cipher obfuscation
        /// </summary>
        /// <param name="data">Data to obfuscate</param>
        /// <param name="key">Shift key</param>
        /// <returns>Obfuscated data</returns>
        private async Task<byte[]> CaesarObfuscateAsync(byte[] data, string key)
        {
            if (!int.TryParse(key, out var shift))
                shift = 3;

            var result = new byte[data.Length];

            for (int i = 0; i < data.Length; i++)
            {
                result[i] = (byte)((data[i] + shift) % 256);
            }

            return await Task.FromResult(result);
        }

        /// <summary>
        /// Caesar cipher deobfuscation
        /// </summary>
        /// <param name="data">Data to deobfuscate</param>
        /// <param name="key">Shift key</param>
        /// <returns>Deobfuscated data</returns>
        private async Task<byte[]> CaesarDeobfuscateAsync(byte[] data, string key)
        {
            if (!int.TryParse(key, out var shift))
                shift = 3;

            var result = new byte[data.Length];

            for (int i = 0; i < data.Length; i++)
            {
                result[i] = (byte)((data[i] - shift + 256) % 256);
            }

            return await Task.FromResult(result);
        }

        /// <summary>
        /// Generates a random obfuscation key
        /// </summary>
        /// <param name="length">Key length</param>
        /// <returns>Random key</returns>
        public string GenerateRandomKey(int length = 16)
        {
            var random = new Random();
            var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var result = new char[length];

            for (int i = 0; i < length; i++)
            {
                result[i] = chars[random.Next(chars.Length)];
            }

            return new string(result);
        }

        /// <summary>
        /// Gets information about available obfuscation methods
        /// </summary>
        /// <returns>Array of obfuscation method information</returns>
        public ObfuscationMethodInfo[] GetAvailableMethods()
        {
            return new[]
            {
                new ObfuscationMethodInfo
                {
                    Name = "XOR",
                    Description = "XOR encryption with a key",
                    RequiresKey = true,
                    KeyType = "string"
                },
                new ObfuscationMethodInfo
                {
                    Name = "Base64",
                    Description = "Base64 encoding",
                    RequiresKey = false,
                    KeyType = null
                },
                new ObfuscationMethodInfo
                {
                    Name = "ROT13",
                    Description = "ROT13 character rotation (for text)",
                    RequiresKey = false,
                    KeyType = null
                },
                new ObfuscationMethodInfo
                {
                    Name = "Caesar",
                    Description = "Caesar cipher with numeric shift",
                    RequiresKey = true,
                    KeyType = "integer"
                }
            };
        }

        /// <summary>
        /// Validates obfuscation configuration
        /// </summary>
        /// <param name="config">Configuration to validate</param>
        /// <returns>Validation result</returns>
        public ObfuscationValidationResult ValidateConfig(ObfuscationConfig config)
        {
            var result = new ObfuscationValidationResult();

            if (config == null)
            {
                result.IsValid = false;
                result.Error = "Configuration is null";
                return result;
            }

            if (!config.Enabled)
            {
                result.IsValid = true;
                return result;
            }

            var availableMethods = GetAvailableMethods();
            var method = availableMethods.FirstOrDefault(m => m.Name.Equals(config.Method, StringComparison.OrdinalIgnoreCase));

            if (method == null)
            {
                result.IsValid = false;
                result.Error = $"Unsupported obfuscation method: {config.Method}";
                return result;
            }

            if (method.RequiresKey && string.IsNullOrEmpty(config.Key))
            {
                result.IsValid = false;
                result.Error = $"Method {config.Method} requires a key";
                return result;
            }

            if (!string.IsNullOrEmpty(config.Key) && method.KeyType == "integer" && !int.TryParse(config.Key, out _))
            {
                result.IsValid = false;
                result.Error = $"Method {config.Method} requires an integer key";
                return result;
            }

            result.IsValid = true;
            return result;
        }
    }

    /// <summary>
    /// Information about an obfuscation method
    /// </summary>
    public class ObfuscationMethodInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool RequiresKey { get; set; }
        public string? KeyType { get; set; }
    }

    /// <summary>
    /// Result of obfuscation configuration validation
    /// </summary>
    public class ObfuscationValidationResult
    {
        public bool IsValid { get; set; }
        public string Error { get; set; } = string.Empty;
    }
} 