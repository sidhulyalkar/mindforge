using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Mindforge.Telemetry;

namespace Mindforge.Combat
{
    public enum GuardianInputTapeMode
    {
        Live = 0,
        Record = 1,
        Replay = 2,
    }

    [Serializable]
    public sealed class GuardianCommandFrame
    {
        public long tick;
        public float move_x;
        public float move_y;
        public float aim_x;
        public float aim_y;
        public float aim_z;
        public bool fire_held;
        public bool cleave_down;
        public bool counter_down;
        public bool dash_down;
        public bool bloom_down;

        // v2 physical-arsenal controls. Old v1 tapes deserialize these as false.
        public bool sword_attack_down;
        public bool guard_held;
        public bool guard_down;

        public Vector2 Move => new Vector2(move_x, move_y);
        public Vector3 Aim => new Vector3(aim_x, aim_y, aim_z);

        public GuardianCommandFrame CopyForTick(long newTick)
        {
            return new GuardianCommandFrame
            {
                tick = newTick,
                move_x = move_x,
                move_y = move_y,
                aim_x = aim_x,
                aim_y = aim_y,
                aim_z = aim_z,
                fire_held = fire_held,
                cleave_down = cleave_down,
                counter_down = counter_down,
                dash_down = dash_down,
                bloom_down = bloom_down,
                sword_attack_down = sword_attack_down,
                guard_held = guard_held,
                guard_down = guard_down,
            };
        }

        public static GuardianCommandFrame Neutral(long tick)
            => new GuardianCommandFrame { tick = tick };
    }

    [Serializable]
    public sealed class GuardianInputTapeEnvelope
    {
        public string schema = GuardianInputTape.SchemaV2;
        public string session_id;
        public string generated_utc;
        public int fixed_hz;
        public List<GuardianCommandFrame> frames = new List<GuardianCommandFrame>();
    }

    /// <summary>
    /// Fixed-tick conventional-input recorder/replayer used to make P4 reproducible.
    /// Recording is memory-backed during play so evidence capture cannot add per-tick
    /// filesystem stalls. Replay fails neutral when exhausted; it never falls back to
    /// live input because that would silently destroy determinism.
    /// </summary>
    public sealed class GuardianInputTape : MonoBehaviour
    {
        public const string SchemaV1 = "mindforge.guardian_input_tape.v1";
        public const string SchemaV2 = "mindforge.guardian_input_tape.v2";

        [SerializeField] private GuardianInputTapeMode mode = GuardianInputTapeMode.Live;
        [SerializeField] private string tapePath;
        [SerializeField] private bool saveRecordingOnQuit = true;

        private GuardianInputTapeEnvelope _tape;
        private int _replayIndex;
        private bool _replayExhaustionLogged;
        private int _fixedHz = 120;

        public GuardianInputTapeMode Mode => mode;
        public int ReplayIndex => _replayIndex;
        public int FrameCount => _tape != null && _tape.frames != null ? _tape.frames.Count : 0;
        public string LatestSavedPath { get; private set; }

        private void Awake()
        {
            ApplyCommandLineOverrides();
            if (mode == GuardianInputTapeMode.Replay)
                LoadReplay();
        }

        public GuardianCommandFrame Resolve(GuardianCommandFrame live, int fixedHz)
        {
            if (live == null) throw new ArgumentNullException(nameof(live));
            _fixedHz = Mathf.Max(1, fixedHz);

            if (mode == GuardianInputTapeMode.Live)
                return live;

            if (mode == GuardianInputTapeMode.Record)
            {
                EnsureRecordingTape();
                _tape.frames.Add(live.CopyForTick(live.tick));
                return live;
            }

            if (_tape == null || _tape.frames == null || _replayIndex >= _tape.frames.Count)
            {
                if (!_replayExhaustionLogged)
                {
                    _replayExhaustionLogged = true;
                    Debug.LogWarning("[Mindforge] Guardian input replay exhausted; returning neutral commands.");
                }
                return GuardianCommandFrame.Neutral(live.tick);
            }

            GuardianCommandFrame recorded = _tape.frames[_replayIndex++];
            return recorded != null ? recorded.CopyForTick(live.tick) : GuardianCommandFrame.Neutral(live.tick);
        }

        public string SaveRecording()
        {
            if (mode != GuardianInputTapeMode.Record || _tape == null)
                return null;

            string path = ResolveTapePath(forReplay: false);
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
            File.WriteAllText(path, JsonUtility.ToJson(_tape, true));
            LatestSavedPath = path;
            Debug.Log($"[Mindforge] Guardian input tape saved: {path}");
            return path;
        }

        private void EnsureRecordingTape()
        {
            if (_tape != null) return;
            _tape = new GuardianInputTapeEnvelope
            {
                schema = SchemaV2,
                session_id = MindforgeSessionContext.GameSessionId,
                generated_utc = DateTime.UtcNow.ToString("O"),
                fixed_hz = _fixedHz,
            };
        }

        private void LoadReplay()
        {
            string path = ResolveTapePath(forReplay: true);
            if (!File.Exists(path))
                throw new FileNotFoundException("Guardian input replay tape not found", path);
            _tape = JsonUtility.FromJson<GuardianInputTapeEnvelope>(File.ReadAllText(path));
            if (_tape == null || (_tape.schema != SchemaV1 && _tape.schema != SchemaV2) || _tape.frames == null)
                throw new InvalidDataException($"Unsupported or malformed Guardian input tape: {path}");
            _replayIndex = 0;
            Debug.Log($"[Mindforge] Guardian input replay loaded: {path} schema={_tape.schema} frames={_tape.frames.Count}");
        }

        private string ResolveTapePath(bool forReplay)
        {
            if (!string.IsNullOrWhiteSpace(tapePath))
                return Path.GetFullPath(tapePath);
            if (forReplay)
                throw new InvalidOperationException("Replay mode requires -mindforgeInputTape <path> or a serialized tapePath.");
            string directory = Path.Combine(Application.persistentDataPath, "mindforge_input_tapes");
            string name = $"guardian-{MindforgeSessionContext.GameSessionId}.json";
            return Path.Combine(directory, name);
        }

        private void ApplyCommandLineOverrides()
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "-mindforgeInputMode" && i + 1 < args.Length)
                {
                    string value = args[++i].Trim().ToLowerInvariant();
                    if (value == "live") mode = GuardianInputTapeMode.Live;
                    else if (value == "record") mode = GuardianInputTapeMode.Record;
                    else if (value == "replay") mode = GuardianInputTapeMode.Replay;
                    else throw new ArgumentException($"Unsupported -mindforgeInputMode '{value}'");
                }
                else if (args[i] == "-mindforgeInputTape" && i + 1 < args.Length)
                {
                    tapePath = args[++i];
                }
            }
        }

        private void OnApplicationQuit()
        {
            if (saveRecordingOnQuit && mode == GuardianInputTapeMode.Record)
                SaveRecording();
        }
    }

    /// <summary>Installs the tape service before scene Start callbacks run.</summary>
    public static class GuardianInputTapeBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (UnityEngine.Object.FindObjectOfType<GuardianInputTape>(true) != null) return;
            GameObject root = GameObject.Find("MindforgeInputReplay");
            if (root == null) root = new GameObject("MindforgeInputReplay");
            root.AddComponent<GuardianInputTape>();
        }
    }
}
