namespace OrbitalEscape.Orbits
{
    public readonly struct ArcObject
    {
        public Arc Arc { get; }
        public IOrbitObject Object { get; }
        public ArcType Type { get; }
        
        public ArcObject(Arc arc, IOrbitObject obj, ArcType type)
        {
            Arc = arc;
            Object = obj;
            Type = type;
        }
    }
}
