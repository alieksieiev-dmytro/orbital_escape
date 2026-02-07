using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using Zenject;

namespace OrbitalEscape.Data
{
    public class PlayerPrefsDataService : IDataService
    {
        private readonly IDataSectionRegistry<string> _registry;
        private readonly List<IDataSection> _dataSections;
        
        [Inject]
        public PlayerPrefsDataService(IDataSectionRegistry<string> registry) => _registry = registry;

        public void Dispose() => _dataSections.Clear();
        
        public TData Load<TData>() where TData : IDataSection
        {
            if (_dataSections.Find(section => section is TData) is TData data)
            {
                return data;
            }

            var rawData = PlayerPrefs.GetString(_registry.GetKey(typeof(TData)), string.Empty);
            
            if (!string.IsNullOrEmpty(rawData))
            {
                data = JsonConvert.DeserializeObject<TData>(rawData);
                _dataSections.Add(data);
                return data;
            }
            
            data = Activator.CreateInstance<TData>();
            _dataSections.Add(data);
            return data;
        }
        
        public void Save<TData>(TData data) where TData : IDataSection
        {
            var rawData = JsonConvert.SerializeObject(data);
            PlayerPrefs.SetString(_registry.GetKey(typeof(TData)), rawData);
            PlayerPrefs.Save();
        }
    }
}
