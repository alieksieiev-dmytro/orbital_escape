namespace OrbitalEscape.Movement
{
    public interface IMovementSpeedService
    {
        float CurrentLinearSpeed { get; }
        float CurrentSpeedMultiplier { get; }
    }
}
