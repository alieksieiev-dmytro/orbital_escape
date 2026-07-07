using System;

namespace OrbitalEscape.Orbits.Generators
{
    public readonly struct LevelContentSettings
    {
        public int FirstOrbitIndexWithContent { get; }

        public LevelContentSettings(int firstOrbitIndexWithContent)
        {
            FirstOrbitIndexWithContent = Math.Max(0, firstOrbitIndexWithContent);
        }
    }
}
