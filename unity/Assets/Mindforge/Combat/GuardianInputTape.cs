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

        // v3 traversal controls. Old v1/v2 tapes deserialize these as false and remain
        // replayable; jump authority is still resolved only on fixed simulation ticks.
        public bool jump_down;
        public bool jump_held;

        // v4 mounted traversal controls. Movement/aim continue to use the canonical
        // move/aim vectors so a tape has one spatial command vocabulary. These edges
        // describe the state transition and actions that only have meaning while riding.
        public bool mount_toggle_down;
        public bool mounted_attack_down;
        public bool mounted_boost_down;

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
                jump_down = jump_down,
                jump_held = jump_held,
                mount_toggle_down = mount_toggle_down,
                mounted_attack_down = mounted_attack_down,
                mounted_boost_down = mounted_boost_down,
            };
        }

        /// <summary>
        /// Multiple conventional-input consumers may contribute to one simulation tick.
        /// One-shot edges are unioned; held movement/aim are replaced only by meaningful
        /// values. This keeps one command frame authoritative across foot and mounted mode
        /// without creating a second vehicle tape or advancing replay twice per tick.
        /// </summary>
        public void MergeFrom(GuardianCommandFrame other)
        {
            if (other == null) return;

            Vector2 otherMove = other.Move;
            if (otherMove.sqrMagnitude > 0.000001f || Move.sqrMagnitude <= 0.000001f)
            {
                move_x = other.move_x;
                move_y = other.move_y;
            }

            if (other.Aim.sqrMagnitude > 0.000001f)
            {
                aim_x = other.aim_x;
                aim_y = other.aim_y;
                aim_z = other.aim_z;
            }

            fire_held |= other.fire_held;
            cleave_down |= other.cleave_down;
            counter_down |= other.counter_down;
            dash_down |= other.dash_down;
            bloom_down |= other.bloom_down;
            sword_attack_down |= other.sword_attack_down;
            guard_held |= other.guard_held;
            guard_down |= other.guard_down;
            jump_down |= other.jump_down;
            jump_held |= other.jump_held;
            mount_toggle_down |= other.mount_toggle_down;
            mounted_attack_down |= other.mounted_attack_down;
            mounted_boost_down |= other.mounted_boost_down;
        }

        public static GuardianCommandFrame Neutral(long tick)
            => new GuardianCommandFrame { tick = tick };
    }

    [Serializable]
    public sealed class GuardianInputTapeEnvelope
    {
        public string schema = GuardianInputTape.SchemaV4;
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
    ///
    /// V4 makes resolution idempotent per absolute simulation tick. Foot and mounted
    /// consumers may both ask for the same tick without duplicating a recorded frame or
    /// consuming two replay frames.
    /// </summary>
    public sealed class GuardianInputTape : MonoBehaviour
    {
        public const string SchemaV1 = "mindforge.guardian_input_tape.v1";
        public const string SchemaV2 = "mindforge.guardian_input_tape.v2";
        public const string SchemaV3 = "mindforge.guardian_input_tape.v3";
        public const string SchemaV4 = "mindforge.guardian_input_tape.v4";

        [SerializeField] private GuardianInputTapeMode mode = GuardianInputTapeMode.Live;
        [SerializeField] private string tapePath;
        [SerializeField] private bool saveRecordingOnQuit = true;

        private GuardianInputTapeEnvelope _tape;
        private int _replayIndex;
        private bool _replayExhaustionLogged;
        private int _fixedHz = 120;
        private long _lastResolvedTick = long.MinValue;
        private GuardianCommandFrame _lastResolvedFrame;

        public GuardianInputTapeMode Mode => mode;
        public int ReplayIndex => _replayIndex;
        public int FrameCount => _tape != null && _tape.frames != null ? _tape.frames.Count : 0;
        public string LatestSavedPath { get; private set; }

        public static long FixedTickNow
        {
            get
            {
                float dt = Mathf.Max(0.0001f, Time.fixedDeltaTime);
                return (long)Math.Round(Time.fixedTime / dt);
            }
        }

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

            if (_lastResolvedFrame != null && _lastResolvedTick == live.tick)
            {
                if (mode == GuardianInputTapeMode.Record)
                    _lastResolvedFrame.MergeFrom(live);
                return _lastResolvedFrame.CopyForTick(live.tick);
            }

            if (mode == GuardianInputTapeMode.Record)
            {
                EnsureRecordingTape();
                GuardianCommandFrame recorded = live.CopyForTick(live.tick);
                _tape.frames.Add(recorded);
                _lastResolvedTick = live.tick;
                _lastResolvedFrame = recorded;
                return recorded.CopyForTick(live.tick);
            }

            if (_tape == null || _tape.frames == null || _replayIndex >= _tape.frames.Count)
            {
                if (!_replayExhaustionLogged)
                {
                    _replayExhaustionLogged = true;
                    Debug.LogWarning("[Mindforge] Guardian input replay exhausted; returning neutral commands.");
                }
                GuardianCommandFrame neutral = GuardianCommandFrame.Neutral(live.tick);
                _lastResolvedTick = live.tick;
                _lastResolvedFrame = neutral;
                return neutral.CopyForTick(live.tick);
            }

            GuardianCommandFrame source = _tape.frames[_replayIndex++];
            GuardianCommandFrame resolved = source != null
                ? source.CopyForTick(live.tick)
                : GuardianCommandFrame.Neutral(live.tick);
            _lastResolvedTick = live.tick;
            _lastResolvedFrame = resolved;
            return resolved.CopyForTick(live.tick);
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
                schema = SchemaV4,
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
            if (_tape == null ||
                (_tape.schema != SchemaV1 && _tape.schema != SchemaV2 && _tape.schema != SchemaV3 && _tape.schema != SchemaV4) ||
                _tape.frames == null)
                throw new InvalidDataException($"Unsupported or malformed Guardian input tape: {path}");
            _replayIndex = 0;
            _lastResolvedTick = long.MinValue;
            _lastResolvedFrame = null;
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
