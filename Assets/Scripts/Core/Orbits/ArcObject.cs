namespace OrbitalEscape.Orbits
{
    public readonly struct ArcObject
    {
        public Arc Arc { get; }
        public IOrbitObject Object { get; }
        
        public ArcObject(Arc arc, IOrbitObject obj)
        {
            Arc = arc;
            Object = obj;
        }
    }
}
