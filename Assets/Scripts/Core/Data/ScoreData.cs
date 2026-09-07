using Newtonsoft.Json;

namespace OrbitalEscape.Data
{
    public class ScoreData : IDataSection
    {
        private int _bestScore;
        
        [JsonConverter(typeof(WholeNumberJsonConverter))]
        public int BestScore
        {
            get => _bestScore;
            set
            {
                if (value > _bestScore)
                {
                    _bestScore = value;
                }
            }
        }
    }
}
