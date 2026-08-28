using UnityEditor;

namespace OrbitalEscape.EditorTools
{
    public sealed class SpriteRadialGradientShaderGUI : ShaderGUI
    {
        private const string MaterialTextureProperty = "_MaterialTex";
        private const string HasMaterialTextureProperty = "_HasMaterialTex";

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            base.OnGUI(materialEditor, properties);

            MaterialProperty materialTexture = FindProperty(MaterialTextureProperty, properties);
            MaterialProperty hasMaterialTexture = FindProperty(HasMaterialTextureProperty, properties);
            if (!materialTexture.hasMixedValue)
            {
                hasMaterialTexture.floatValue = materialTexture.textureValue != null ? 1.0f : 0.0f;
            }
        }
    }
}
