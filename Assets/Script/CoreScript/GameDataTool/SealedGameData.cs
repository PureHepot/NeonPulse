using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameDataTool
{
    public class SealedGameData
    {
        private readonly Dictionary<string, object> cachedProperties = new Dictionary<string, object>();
        private readonly List<string> properties = new List<string>();

        public IEnumerable<string> AllProperties => properties;

        public SealedGameData(object data)
        {
            foreach (var property in data.GetType().GetProperties())
            {
                if (!property.PropertyType.IsGenericType ||
                    property.PropertyType.GetGenericTypeDefinition() != typeof(SealedValue<>))
                {
                    continue;
                }

                var value = property.GetValue(data);
                cachedProperties.Add(property.Name, value);
                properties.Add(property.Name);
            }
        }

        public bool TryGetPropertyValue<T>(string name, out T value)
        {
            if (cachedProperties.TryGetValue(name, out var obj) && obj is SealedValue<T> sealedValue)
            {
                value = sealedValue.Value;
                return true;
            }

            value = default;
            return false;
        }

        public bool TryGetProperty(string name, out object value)
        {
            return cachedProperties.TryGetValue(name, out value);
        }

        public void AddModifier<TValue, TModifier>(string name, string group, Modifier<TModifier> modifier)
            where TModifier : IEquatable<TModifier>
        {
            if (cachedProperties.TryGetValue(name, out var obj) && obj is SealedValue<TValue> sealedValue)
            {
                if (sealedValue.TryAddModifier(group, modifier))
                    return;
            }

            Debug.LogWarning($"[GameDataTool] Failed to add modifier '{group}' to property '{name}'.");
        }
    }
}
