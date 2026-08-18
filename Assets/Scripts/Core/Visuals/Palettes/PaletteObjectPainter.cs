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
            if (background != null && !TryApplyPaletteTarget(background, palette))
            {
                background.color = WithAlpha(Color.Lerp(Color.black, palette.BackgroundColor, 0.18f), background.color.a);
            }

            if (accent != null && !TryApplyPaletteTarget(accent, palette))
            {
                accent.color = WithAlpha(Color.Lerp(Color.black, palette.EnvironmentColor, 0.35f), accent.color.a);
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
                if (HasPaletteTarget(renderer.gameObject))
                {
                    continue;
                }

                renderer.color = WithAlpha(Color.white, renderer.color.a);
                ApplyExistingOutlineProperties(renderer, palette.AccentColor);
            }

            foreach (MonoBehaviour behaviour in player.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (behaviour is IPaletteTarget paletteTarget)
                {
                    paletteTarget.ApplyPalette(palette);
                }
            }

            foreach (ParticleSystem particles in player.GetComponentsInChildren<ParticleSystem>(true))
            {
                if (HasPaletteTarget(particles.gameObject))
                {
                    continue;
                }

                ParticleSystem.MainModule main = particles.main;
                main.startColor = palette.AccentColor;
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

            foreach (MonoBehaviour behaviour in levelObject.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (behaviour is IPaletteTarget paletteTarget)
                {
                    paletteTarget.ApplyPalette(palette);
                }
            }

            foreach (SpriteRenderer renderer in levelObject.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (renderer.GetComponent<ObstacleCollider>() == null)
                {
                    continue;
                }

                renderer.color = WithAlpha(Color.white, renderer.color.a);
                ApplyExistingOutlineProperties(renderer, palette.AccentColor);
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

        private Color WithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }

        private bool HasPaletteTarget(GameObject gameObject)
        {
            foreach (MonoBehaviour behaviour in gameObject.GetComponents<MonoBehaviour>())
            {
                if (behaviour is IPaletteTarget || behaviour is IPaletteExclusion)
                {
                    return true;
                }
            }

            return false;
        }

        private bool TryApplyPaletteTarget(SpriteRenderer renderer, PaletteSnapshot palette)
        {
            bool applied = false;

            foreach (MonoBehaviour behaviour in renderer.GetComponents<MonoBehaviour>())
            {
                if (behaviour is IPaletteExclusion)
                {
                    return true;
                }

                if (behaviour is IPaletteTarget paletteTarget)
                {
                    paletteTarget.ApplyPalette(palette);
                    applied = true;
                }
            }

            return applied;
        }
    }
}
