#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Mindforge.Chassis.Editor
{
    /// <summary>
    /// Exports a compact, machine-readable snapshot of the native V0.30 world audits.
    /// Reports are written outside Assets so they never become part of the overlay or
    /// mutate the materialized game project.
    /// </summary>
    public static class MindforgeWorldQualificationExporterV30
    {
        [Serializable]
        public sealed class QualificationReport
        {
            public string schema = "mindforge.world_qualification.v30";
            public string generatedUtc;
            public string unityVersion;
            public string scene;
            public bool playMode;
            public bool readinessPassed;
            public int readinessPassedChecks;
            public int readinessFailedChecks;
            public int readinessDeferredChecks;
            public string[] readinessFailures;
            public string[] readinessDeferred;
            public bool geometryPassed;
            public bool navMeshObserved;
            public bool playerAnchorObserved;
            public bool bossAnchorObserved;
            public bool pathComplete;
            public int pathCornerCount;
            public int clearanceSamples;
            public int chokeSamples;
            public float minimumPathClearWidth;
            public Vector3 narrowestPathPosition;
            public float minimumBossClearRadius;
            public float minimumBossClearAngleDegrees;
            public int largeInvisibleColliderCandidates;
        }

        [MenuItem("Mindforge/World V0.30/Export Qualification Report", priority = 22)]
        public static void ExportMenu()
        {
            string path = ExportCurrentState();
            Debug.Log($"[Mindforge:V30] Qualification report written to {path}");
            EditorUtility.RevealInFinder(path);
        }

        public static string ExportCurrentState()
        {
            MindforgeWorldReadinessV30.Report readiness = MindforgeWorldReadinessV30.AuditActiveScene();
            MindforgeWorldGeometryAuditV30.Report geometry = MindforgeWorldGeometryAuditV30.AuditActiveScene();

            List<string> failures = new List<string>();
            List<string> deferred = new List<string>();
            int passed = 0;
            int failed = 0;
            for (int i = 0; i < readiness.checks.Count; i++)
            {
                MindforgeWorldReadinessV30.Check check = readiness.checks[i];
                if (!check.observed)
                {
                    deferred.Add(check.id + ": " + check.detail);
                    continue;
                }
                if (check.passed) passed++;
                else
                {
                    failed++;
                    failures.Add(check.id + ": " + check.detail);
                }
            }

            QualificationReport report = new QualificationReport
            {
                generatedUtc = DateTime.UtcNow.ToString("O"),
                unityVersion = Application.unityVersion,
                scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path,
                playMode = EditorApplication.isPlaying,
                readinessPassed = readiness.passed,
                readinessPassedChecks = passed,
                readinessFailedChecks = failed,
                readinessDeferredChecks = deferred.Count,
                readinessFailures = failures.ToArray(),
                readinessDeferred = deferred.ToArray(),
                geometryPassed = geometry.passed,
                navMeshObserved = geometry.navMeshObserved,
                playerAnchorObserved = geometry.playerAnchorObserved,
                bossAnchorObserved = geometry.bossAnchorObserved,
                pathComplete = geometry.pathComplete,
                pathCornerCount = geometry.pathCornerCount,
                clearanceSamples = geometry.clearanceSamples,
                chokeSamples = geometry.chokeSamples,
                minimumPathClearWidth = geometry.minimumPathClearWidth,
                narrowestPathPosition = geometry.narrowestPathPosition,
                minimumBossClearRadius = geometry.minimumBossClearRadius,
                minimumBossClearAngleDegrees = geometry.minimumBossClearAngleDegrees,
                largeInvisibleColliderCandidates = geometry.largeInvisibleColliderCandidates,
            };

            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string reportsRoot = Path.Combine(projectRoot, "MindforgeReports");
            Directory.CreateDirectory(reportsRoot);
            string stamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
            string filePath = Path.Combine(reportsRoot, $"v30-world-qualification-{stamp}.json");
            File.WriteAllText(filePath, JsonUtility.ToJson(report, true));
            return filePath;
        }
    }
}
#endif
