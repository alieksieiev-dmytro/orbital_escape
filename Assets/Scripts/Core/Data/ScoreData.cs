namespace OrbitalEscape.Data
{
    public class ScoreData : IDataSection
    {
        private float _bestScore;
        
        public float BestScore
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
