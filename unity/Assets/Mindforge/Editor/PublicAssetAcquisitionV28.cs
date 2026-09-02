#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Mindforge.Editor
{
    /// <summary>
    /// Deterministic V0.28 public-art acquisition.
    ///
    /// Source assets are fetched only in the Editor, from immutable upstream commit URLs. Every
    /// response is verified against the upstream Git blob SHA-1 before it is admitted into the
    /// Generated/V28 cache. KayKit OBJ files are then normalized by removing their mtllib line
    /// because Mindforge deliberately supplies its own cathedral materials.
    ///
    /// This is build tooling only. It creates no runtime network request and no gameplay authority.
    /// </summary>
    public static class PublicAssetAcquisitionV28
    {
        public const string Root = "Assets/Mindforge/Generated/V28/ThirdParty";
        public const string GobkitRoot = Root + "/Gobkit";
        public const string KayKitRoot = Root + "/KayKitDungeon";

        public const string GobkitCommit = "0d654ab3306515b1b63621a5c6548554034482dc";
        public const string KayKitCommit = "b0ca9bd96a8072ab36a3a5464f00ed1e06a16d07";

        public const string RhinoPath = GobkitRoot + "/Rhino.glb";
        public const string BannerPath = KayKitRoot + "/banner_white.obj";
        public const string TorchPath = KayKitRoot + "/torch_mounted.obj";
        public const string ChairPath = KayKitRoot + "/chair.obj";
        public const string TablePath = KayKitRoot + "/table_small_decorated_A.obj";
        public const string ChestPath = KayKitRoot + "/chest_gold.obj";

        private sealed class SourceAsset
        {
            public string LocalPath;
            public string Url;
            public string GitBlobSha1;
            public bool NormalizeObj;

            public SourceAsset(string localPath, string url, string gitBlobSha1, bool normalizeObj)
            {
                LocalPath = localPath;
                Url = url;
                GitBlobSha1 = gitBlobSha1;
                NormalizeObj = normalizeObj;
            }
        }

        private static readonly SourceAsset[] Sources =
        {
            new SourceAsset(
                RhinoPath,
                "https://raw.githubusercontent.com/Ariescar/gobkit-free-assets/" + GobkitCommit + "/animal/Rhino.glb",
                "f638b1cf00a6472192beb85b1a4162535bfc189e",
                false),
            new SourceAsset(
                BannerPath,
                "https://raw.githubusercontent.com/KayKit-Game-Assets/KayKit-Dungeon-Remastered-1.0/" + KayKitCommit + "/addons/kaykit_dungeon_remastered/Assets/obj/banner_white.obj",
                "caf89af21053f2aa8081421d05b4d393f5b06fc7",
                true),
            new SourceAsset(
                TorchPath,
                "https://raw.githubusercontent.com/KayKit-Game-Assets/KayKit-Dungeon-Remastered-1.0/" + KayKitCommit + "/addons/kaykit_dungeon_remastered/Assets/obj/torch_mounted.obj",
                "b29c171929a5995de358a35bad91c63f475cab2b",
                true),
            new SourceAsset(
                ChairPath,
                "https://raw.githubusercontent.com/KayKit-Game-Assets/KayKit-Dungeon-Remastered-1.0/" + KayKitCommit + "/addons/kaykit_dungeon_remastered/Assets/obj/chair.obj",
                "0532f0992b7ce9cadcdc8921e3762760ca87441f",
                true),
            new SourceAsset(
                TablePath,
                "https://raw.githubusercontent.com/KayKit-Game-Assets/KayKit-Dungeon-Remastered-1.0/" + KayKitCommit + "/addons/kaykit_dungeon_remastered/Assets/obj/table_small_decorated_A.obj",
                "f49a5780d08533f1fa17b7506b847b18be28a8f5",
                true),
            new SourceAsset(
                ChestPath,
                "https://raw.githubusercontent.com/KayKit-Game-Assets/KayKit-Dungeon-Remastered-1.0/" + KayKitCommit + "/addons/kaykit_dungeon_remastered/Assets/obj/chest_gold.obj",
                "9d0bf6592588cca750940b0f0688e7158e2e51fa",
                true),
        };

        public static IReadOnlyList<string> EnsureAll()
        {
            EnsureFolder(Root);
            EnsureFolder(GobkitRoot);
            EnsureFolder(KayKitRoot);

            List<string> imported = new List<string>(Sources.Length);
            using (HttpClient client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromSeconds(30);
                client.DefaultRequestHeaders.UserAgent.ParseAdd("Mindforge-V28-Editor-Asset-Acquisition/1.0");

                for (int i = 0; i < Sources.Length; i++)
                {
                    SourceAsset source = Sources[i];
                    if (!File.Exists(source.LocalPath)) DownloadVerified(client, source);
                    AssetDatabase.ImportAsset(source.LocalPath, ImportAssetOptions.ForceSynchronousImport);
                    NormalizeImporter(source);
                    imported.Add(source.LocalPath);
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return imported;
        }

        private static void DownloadVerified(HttpClient client, SourceAsset source)
        {
            byte[] bytes;
            try
            {
                bytes = client.GetByteArrayAsync(source.Url).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                throw new UnityEditor.Build.BuildFailedException(
                    $"V0.28 could not acquire pinned public asset '{source.Url}'. " +
                    "The professional creature/world pass fails closed rather than silently restoring proxy art. " + ex.Message);
            }

            string blobHash = ComputeGitBlobSha1(bytes);
            if (!string.Equals(blobHash, source.GitBlobSha1, StringComparison.OrdinalIgnoreCase))
            {
                throw new UnityEditor.Build.BuildFailedException(
                    $"V0.28 public asset hash mismatch for '{source.Url}'. expected={source.GitBlobSha1} actual={blobHash}");
            }

            string local = source.LocalPath;
            string directory = Path.GetDirectoryName(local);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory)) Directory.CreateDirectory(directory);

            if (source.NormalizeObj)
            {
                string text = Encoding.UTF8.GetString(bytes);
                string[] lines = text.Replace("\r\n", "\n").Split('\n');
                StringBuilder normalized = new StringBuilder(text.Length);
                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].StartsWith("mtllib ", StringComparison.OrdinalIgnoreCase)) continue;
                    normalized.Append(lines[i]);
                    normalized.Append('\n');
                }
                File.WriteAllText(local, normalized.ToString(), new UTF8Encoding(false));
            }
            else
            {
                File.WriteAllBytes(local, bytes);
            }
        }

        private static void NormalizeImporter(SourceAsset source)
        {
            if (!source.NormalizeObj) return;
            ModelImporter importer = AssetImporter.GetAtPath(source.LocalPath) as ModelImporter;
            if (importer == null) return;
            bool dirty = false;
            if (importer.materialImportMode != ModelImporterMaterialImportMode.None)
            {
                importer.materialImportMode = ModelImporterMaterialImportMode.None;
                dirty = true;
            }
            if (!importer.isReadable)
            {
                importer.isReadable = true;
                dirty = true;
            }
            if (dirty) importer.SaveAndReimport();
        }

        public static string ComputeGitBlobSha1(byte[] bytes)
        {
            byte[] header = Encoding.ASCII.GetBytes("blob " + bytes.Length + "\0");
            byte[] input = new byte[header.Length + bytes.Length];
            Buffer.BlockCopy(header, 0, input, 0, header.Length);
            Buffer.BlockCopy(bytes, 0, input, header.Length, bytes.Length);
            using (SHA1 sha = SHA1.Create())
            {
                byte[] hash = sha.ComputeHash(input);
                StringBuilder sb = new StringBuilder(hash.Length * 2);
                for (int i = 0; i < hash.Length; i++) sb.Append(hash[i].ToString("x2"));
                return sb.ToString();
            }
        }

        private static void EnsureFolder(string folder)
        {
            string[] parts = folder.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
#endif
