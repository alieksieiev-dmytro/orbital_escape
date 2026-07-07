namespace OrbitalEscape.Orbits
{
    public readonly struct LaserGate : IOrbitObject
    {
        public float PassTime { get; }
        public float ChargingTime { get; }
        public float ActiveTime { get; }

        public ArcObjectType Type => ArcObjectType.LaserGate;

        public LaserGate(float passTime, float chargingTime, float activeTime)
        {
            PassTime = passTime;
            ChargingTime = chargingTime;
            ActiveTime = activeTime;
        }
    }
}
