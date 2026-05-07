using OrbitalEscape.Orbits;
using OrbitalEscape.Visuals;
using UnityEngine;

namespace OrbitalEscape.Visuals.Palettes
{
    public sealed class PaletteObjectPainter : IPaletteObjectPainter
    {
        private static readonly int EdgeColorId = Shader.PropertyToID("_EdgeColor");
        private static readonly int GlowColorId = Shader.PropertyToID("_GlowColor");

        private readonly MaterialPropertyBlock _block = new MaterialPropertyBlock();

        public void ApplyEnvironment(SpriteRenderer background, SpriteRenderer accent, PaletteSnapshot palette)
        {
            if (background != null)
            {
                background.color = WithAlpha(Color.Lerp(Color.black, palette.MainColor, 0.18f), background.color.a);
            }

            if (accent != null)
            {
                accent.color = WithAlpha(Color.Lerp(Color.black, palette.MainColor, 0.35f), accent.color.a);
            }
        }

        public void ApplyPlayer(Transform player, PaletteSnapshot palette)
        {
            if (player == null)
            {
                return;
            }

            foreach (SpriteRenderer renderer in player.GetComponentsInChildren<SpriteRenderer>(true))
            {
                renderer.color = WithAlpha(Color.white, renderer.color.a);
                ApplyExistingOutlineProperties(renderer, palette.OutlineColor);
            }

            foreach (TrailRenderer trail in player.GetComponentsInChildren<TrailRenderer>(true))
            {
                trail.colorGradient = BuildFadeGradient(palette.OutlineColor);
            }

            foreach (ParticleSystem particles in player.GetComponentsInChildren<ParticleSystem>(true))
            {
                ParticleSystem.MainModule main = particles.main;
                main.startColor = palette.OutlineColor;
            }
        }

        public void ApplyLevelObject(GameObject levelObject, PaletteSnapshot palette)
        {
            if (levelObject == null)
            {
                return;
            }

            foreach (AuroraFadeInOut aurora in levelObject.GetComponentsInChildren<AuroraFadeInOut>(true))
            {
                aurora.ApplyPalette(palette);
            }

            foreach (RiftController riftController in levelObject.GetComponentsInChildren<RiftController>(true))
            {
                riftController.ApplyPalette(palette);
            }

            foreach (SpriteRenderer renderer in levelObject.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (renderer.GetComponent<ObstacleCollider>() == null)
                {
                    continue;
                }

                renderer.color = WithAlpha(Color.white, renderer.color.a);
                ApplyExistingOutlineProperties(renderer, palette.OutlineColor);
            }
        }

        private void ApplyExistingOutlineProperties(Renderer renderer, Color color)
        {
            Material material = renderer.sharedMaterial;
            if (material == null)
            {
                return;
            }

            _block.Clear();
            renderer.GetPropertyBlock(_block);

            bool changed = TrySetMaterialBackedColor(material, EdgeColorId, color);
            changed |= TrySetMaterialBackedColor(material, GlowColorId, color);

            if (changed)
            {
                renderer.SetPropertyBlock(_block);
            }
        }

        private bool TrySetMaterialBackedColor(Material material, int propertyId, Color color)
        {
            if (!material.HasProperty(propertyId))
            {
                return false;
            }

            Color materialColor = material.GetColor(propertyId);
            color.a = materialColor.a;
            _block.SetColor(propertyId, color);
            return true;
        }

        private Gradient BuildFadeGradient(Color color)
        {
            Color transparent = color;
            transparent.a = 0.0f;

            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(color, 0.0f), new GradientColorKey(color, 1.0f) },
                new[] { new GradientAlphaKey(color.a, 0.0f), new GradientAlphaKey(transparent.a, 1.0f) });
            return gradient;
        }

        private Color WithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }
    }
}
