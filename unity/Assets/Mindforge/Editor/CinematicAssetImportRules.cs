#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace Mindforge.Editor
{
    /// <summary>
    /// Opinionated production-art import defaults for Assets/Mindforge/Art. Final art
    /// can override settings in the inspector, but new drops enter with texture detail,
    /// correct color-space semantics and non-destructive mesh quality by default.
    /// </summary>
    public sealed class CinematicAssetImportRules : AssetPostprocessor
    {
        private const string ArtRoot = "Assets/Mindforge/Art/";

        private bool InArtRoot => assetPath.Replace('\\', '/').StartsWith(ArtRoot, StringComparison.OrdinalIgnoreCase);

        private void OnPreprocessTexture()
        {
            if (!InArtRoot) return;
            TextureImporter importer = assetImporter as TextureImporter;
            if (importer == null) return;

            string lower = assetPath.ToLowerInvariant();
            bool normal = lower.Contains("_normal") || lower.EndsWith("_n.png") || lower.EndsWith("_n.tga") || lower.EndsWith("_n.exr");
            bool linearData = normal || lower.Contains("_orm") || lower.Contains("_mask") || lower.Contains("_rough") || lower.Contains("_metal") || lower.Contains("_ao");

            importer.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            importer.sRGBTexture = !linearData;
            importer.mipmapEnabled = true;
            importer.mipmapFilter = TextureImporterMipFilter.KaiserFilter;
            importer.anisoLevel = 8;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.crunchedCompression = false;
            importer.maxTextureSize = IsCharacterOrHeroAsset(lower) ? 4096 : 2048;
        }

        private void OnPreprocessModel()
        {
            if (!InArtRoot) return;
            ModelImporter importer = assetImporter as ModelImporter;
            if (importer == null) return;

            importer.importCameras = false;
            importer.importLights = false;
            importer.importVisibility = true;
            importer.importBlendShapes = true;
            importer.importNormals = ModelImporterNormals.Import;
            importer.importTangents = ModelImporterTangents.CalculateMikk;
            importer.meshCompression = ModelImporterMeshCompression.Off;
            importer.isReadable = false;
            importer.optimizeMeshPolygons = true;
            importer.optimizeMeshVertices = true;
            importer.weldVertices = true;
            importer.addCollider = false;

            string lower = assetPath.ToLowerInvariant();
            if (lower.Contains("/characters/") || lower.Contains("guardian") || lower.Contains("fracturedsignal"))
            {
                importer.animationType = ModelImporterAnimationType.Generic;
                importer.importAnimation = true;
                importer.resampleCurves = false;
            }
        }

        private static bool IsCharacterOrHeroAsset(string lower)
            => lower.Contains("/characters/") || lower.Contains("/hero/") || lower.Contains("guardian") || lower.Contains("boss") || lower.Contains("fracturedsignal");
    }
}
#endif
