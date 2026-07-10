namespace OrbitalEscape.Profiling
{
    public sealed class DoubleSampleAccumulator
    {
        private double _sum;
        private double _max;

        public int Count { get; private set; }

        public double? Average => Count > 0 ? _sum / Count : null;
        public double? Max => Count > 0 ? _max : null;

        public void Add(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value < 0.0)
            {
                return;
            }

            if (Count == 0 || value > _max)
            {
                _max = value;
            }

            _sum += value;
            Count++;
        }

        public void Clear()
        {
            _sum = 0.0;
            _max = 0.0;
            Count = 0;
        }
    }
}
