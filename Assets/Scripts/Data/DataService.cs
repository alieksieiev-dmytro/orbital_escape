using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json;
using UnityEngine;

namespace OrbitalEscape.Data
{
    public class DataService : IDataService
    {
        private readonly SaveData _data;
        private readonly Dictionary<IConvertible, IDataSection> _sections = new Dictionary<IConvertible, IDataSection>();
        
        public DataService()
        {
            if (PlayerPrefs.HasKey(Constants.SERIALIZED_PLAYER_DATA_KEY))
            {
                string jsonData = PlayerPrefs.GetString(Constants.SERIALIZED_PLAYER_DATA_KEY);
                _data = JsonConvert.DeserializeObject<SaveData>(jsonData);
            }
            else
            {
                _data = new SaveData();
            }
        }
        
        public void Dispose()
        {
            string jsonData = JsonConvert.SerializeObject(_data);
            PlayerPrefs.SetString(Constants.SERIALIZED_PLAYER_DATA_KEY, jsonData);
            PlayerPrefs.Save();
        }
        
        public void SaveData(IDataSection dataSection)
        {
            _sections[dataSection.Key] = dataSection;
            dataSection.Save();
        }
        
        public T LoadData<T>(IConvertible key) where T : IDataSection
        {
            if (_sections.TryGetValue(key, out IDataSection section))
            {
                return (T)section;
            }
            
            if (_data.SerializedData.TryGetValue(key.ToString(CultureInfo.InvariantCulture), out string serializedData))
            { 
                var dataSection = JsonConvert.DeserializeObject<T>(serializedData);
                _sections[key] = dataSection;
                return dataSection;
            }

            throw new KeyNotFoundException($"No data section found for key: {key}");
        }
    }
}
