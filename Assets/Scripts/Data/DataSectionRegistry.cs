using System;
using System.Collections.Generic;

namespace OrbitalEscape.Data
{
    public sealed class DataSectionRegistry : IDataSectionRegistry<string>
    {
        private readonly Dictionary<Type, string> _typeToKey;
        
        public DataSectionRegistry(IReadOnlyList<(string key, Type dataType)> bindings)
        {
            _typeToKey = new Dictionary<Type, string>();
            
            foreach (var (key, dataType) in bindings)
            {
                if (!typeof(IDataSection).IsAssignableFrom(dataType))
                {
                    throw new ArgumentException($"Type '{dataType.FullName}' does not implement IDataSection.");
                }
                
                _typeToKey[dataType] = key;
            }
        }

        public string GetKey(Type dataSection)
        {
            return _typeToKey.TryGetValue(dataSection, out var key)
                ? key
                : throw new KeyNotFoundException($"No key found for data section type '{dataSection.FullName}'.");
        }
    }
}