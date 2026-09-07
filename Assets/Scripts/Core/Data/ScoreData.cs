namespace OrbitalEscape.Data
{
    public class ScoreData : IDataSection
    {
        private int _bestScore;
        
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
