using UnityEditor;
using UnityEngine;

namespace Uncrowned.EditorTools
{
    /// <summary>
    /// Import settings for everything under Assets/Resources/Art: crisp pixel sprites at 16 PPU.
    /// Character frames pivot at the feet; weapons pivot at the grip.
    /// </summary>
    public class PixelArtImporter : AssetPostprocessor
    {
        public const int PixelsPerUnit = 16;

        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith("Assets/Resources/Art/")) return;

            var ti = (TextureImporter)assetImporter;
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.spritePixelsPerUnit = PixelsPerUnit;
            ti.filterMode = FilterMode.Point;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.mipmapEnabled = false;
            ti.alphaIsTransparency = true;

            var settings = new TextureImporterSettings();
            ti.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            if (assetPath.Contains("/Hero/knight_"))
            {
                // Little Fantasy knight: 112x64 frames, body centred on x=54, feet 8 px above the bottom.
                settings.spriteAlignment = (int)SpriteAlignment.Custom;
                settings.spritePivot = new Vector2(54f / 112f, 8f / 64f);
            }
            else
            {
                settings.spriteAlignment = (int)SpriteAlignment.BottomCenter; // feet
            }
            ti.SetTextureSettings(settings);
        }
    }
}
