using System;
using System.Collections.Generic;

namespace OrbitalEscape.Data
{
    [Serializable]
    public sealed class SaveData
    {
        public int Version;
        public Dictionary<string, string> SerializedData = new Dictionary<string, string>();
    }
}
