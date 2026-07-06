namespace OrbitalEscape.Events
{
    public readonly struct OrbitFocusRequestedEvent : IEvent
    {
        public float CameraRadius { get; }

        public OrbitFocusRequestedEvent(float cameraRadius)
        {
            CameraRadius = cameraRadius;
        }
    }
}
