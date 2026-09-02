#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Mindforge.Chassis.Editor
{
    public static class MindforgeWorldGrammarAuditV32
    {
        [MenuItem("Mindforge/World V0.32/Audit Chunk Grammar", priority = 30)]
        public static void AuditActiveScene()
        {
            MindforgeChunkDescriptorV32[] chunks = UnityEngine.Object.FindObjectsOfType<MindforgeChunkDescriptorV32>(true);
            HashSet<string> chunkIds = new HashSet<string>(StringComparer.Ordinal);
            HashSet<string> socketIds = new HashSet<string>(StringComparer.Ordinal);
            int invalid = 0;
            int socketCount = 0;

            for (int i = 0; i < chunks.Length; i++)
            {
                MindforgeChunkDescriptorV32 chunk = chunks[i];
                if (chunk == null) continue;
                string reason;
                if (!chunk.IsValid(out reason))
                {
                    invalid++;
                    Debug.LogError($"[Mindforge:V32] Invalid chunk {chunk.name}: {reason}", chunk);
                    continue;
                }

                if (!chunkIds.Add(chunk.StableId))
                {
                    invalid++;
                    Debug.LogError($"[Mindforge:V32] Duplicate chunk stableId: {chunk.StableId}", chunk);
                }

                IReadOnlyList<MindforgeWorldSocketV32> sockets = chunk.GetSockets();
                for (int s = 0; s < sockets.Count; s++)
                {
                    MindforgeWorldSocketV32 socket = sockets[s];
                    if (socket == null) continue;
                    socketCount++;
                    if (!socketIds.Add(socket.StableId))
                    {
                        invalid++;
                        Debug.LogError($"[Mindforge:V32] Duplicate world socket stableId: {socket.StableId}", socket);
                    }
                }
            }

            if (invalid > 0)
            {
                Debug.LogError(
                    $"[Mindforge:V32] Chunk grammar FAIL: chunks={chunks.Length}, sockets={socketCount}, invalid={invalid}."
                );
                return;
            }

            Debug.Log(
                $"[Mindforge:V32] Chunk grammar PASS: chunks={chunks.Length}, sockets={socketCount}, " +
                $"regions={MindforgeWorldGrammarV32.AllRegions.Count}."
            );
        }
    }
}
#endif
