using System;

namespace OrbitalEscape.Orbits.Generators
{
    public readonly struct RiftGenerationSettings
    {
        public float MinCenterDistanceBetweenRifts { get; }
        public float RiftLength { get; }
        public float RiftCount { get; }
        public float BaseRiftCountRadius { get; }
        public float AdditionalRiftRadiusStep { get; }
        public float NeighborRiftAvoidanceDistance { get; }
        public float AlignedNeighborRiftWeightMultiplier { get; }
        public float RiftPadding { get; }
        public int MaxPlacementAttempts { get; }

        public RiftGenerationSettings(
            float minCenterDistanceBetweenRifts,
            float riftLength,
            float riftCount,
            float baseRiftCountRadius,
            float additionalRiftRadiusStep,
            float neighborRiftAvoidanceDistance,
            float alignedNeighborRiftWeightMultiplier,
            float riftPadding,
            int maxPlacementAttempts)
        {
            RiftLength = Math.Max(0.01f, riftLength);
            MinCenterDistanceBetweenRifts = Math.Max(RiftLength, minCenterDistanceBetweenRifts);
            RiftCount = riftCount;
            BaseRiftCountRadius = Math.Max(0.01f, baseRiftCountRadius);
            AdditionalRiftRadiusStep = Math.Max(0.01f, additionalRiftRadiusStep);
            NeighborRiftAvoidanceDistance = Math.Max(0.01f, neighborRiftAvoidanceDistance);
            AlignedNeighborRiftWeightMultiplier = Math.Min(
                1.0f,
                Math.Max(0.01f, alignedNeighborRiftWeightMultiplier));
            RiftPadding = riftPadding;
            MaxPlacementAttempts = Math.Max(1, maxPlacementAttempts);
        }

        public int GetTargetCount(Orbit orbit)
        {
            int baseCount = Math.Max(0, (int)Math.Ceiling(RiftCount));
            if (orbit == null)
            {
                return baseCount;
            }

            float radiusDelta = Math.Max(0.0f, orbit.Radius - BaseRiftCountRadius);
            int additionalCount = (int)Math.Floor(radiusDelta / AdditionalRiftRadiusStep);
            return baseCount + additionalCount;
        }

        public float GetEffectiveMinCenterDistanceBetweenRifts(Orbit orbit)
        {
            int targetCount = GetTargetCount(orbit);
            if (orbit == null || targetCount <= 1)
            {
                return MinCenterDistanceBetweenRifts;
            }

            float feasibleCenterDistance = Math.Max(0.01f, orbit.Circumference) / targetCount;
            float preferredDistance = Math.Max(RiftLength, MinCenterDistanceBetweenRifts);
            return Math.Max(RiftLength, Math.Min(preferredDistance, feasibleCenterDistance));
        }

        public float GetNeighborWeightMultiplier(float centerDistance)
        {
            float normalizedDistance = Math.Min(
                1.0f,
                Math.Max(0.0f, centerDistance) / NeighborRiftAvoidanceDistance);
            return AlignedNeighborRiftWeightMultiplier +
                   (1.0f - AlignedNeighborRiftWeightMultiplier) * normalizedDistance;
        }
    }
}
