using UnityEngine;

namespace Mindforge.World
{
    /// <summary>
    /// Presentation-only world objective/location layer. World interactions retain their
    /// own conventional input authority; this component only observes events and state.
    /// </summary>
    public sealed class NullWardHud : MonoBehaviour
    {
        [SerializeField] private NullWardEncounterDirector world;
        [SerializeField] private MemoryForgeCheckpoint checkpoint;
        [SerializeField] private WorldShortcut shortcut;
        [SerializeField] private float bannerSeconds = 2.1f;
        [SerializeField] private float controlsHintSeconds = 16f;

        private GUIStyle _objective;
        private GUIStyle _small;
        private GUIStyle _banner;
        private string _bannerText;
        private double _bannerUntil;
        private double _enteredAt;

        public void ConfigureRuntime(
            NullWardEncounterDirector director,
            MemoryForgeCheckpoint memoryForge,
            WorldShortcut worldShortcut)
        {
            Unsubscribe();
            world = director;
            checkpoint = memoryForge;
            shortcut = worldShortcut;
            Subscribe();
        }

        private void OnEnable()
        {
            Resolve();
            Subscribe();
            _enteredAt = Time.realtimeSinceStartupAsDouble;
        }

        private void OnDisable() => Unsubscribe();

        private void Update()
        {
            if (world != null) return;
            Unsubscribe();
            Resolve();
            Subscribe();
        }

        private void Resolve()
        {
            if (world == null) world = FindObjectOfType<NullWardEncounterDirector>(true);
            if (checkpoint == null) checkpoint = FindObjectOfType<MemoryForgeCheckpoint>(true);
            if (shortcut == null) shortcut = FindObjectOfType<WorldShortcut>(true);
        }

        private void Subscribe()
        {
            if (world != null)
            {
                world.ZoneStarted -= OnZoneStarted;
                world.ZoneCleared -= OnZoneCleared;
                world.ProtocolUnlocked -= OnProtocolUnlocked;
                world.BossStarted -= OnBossStarted;
                world.WorldCompleted -= OnWorldCompleted;
                world.ZoneStarted += OnZoneStarted;
                world.ZoneCleared += OnZoneCleared;
                world.ProtocolUnlocked += OnProtocolUnlocked;
                world.BossStarted += OnBossStarted;
                world.WorldCompleted += OnWorldCompleted;
            }
            if (checkpoint != null)
            {
                checkpoint.Activated -= OnCheckpointActivated;
                checkpoint.Respawned -= OnRespawned;
                checkpoint.Activated += OnCheckpointActivated;
                checkpoint.Respawned += OnRespawned;
            }
            if (shortcut != null)
            {
                shortcut.Unlocked -= OnShortcutUnlocked;
                shortcut.Unlocked += OnShortcutUnlocked;
            }
        }

        private void Unsubscribe()
        {
            if (world != null)
            {
                world.ZoneStarted -= OnZoneStarted;
                world.ZoneCleared -= OnZoneCleared;
                world.ProtocolUnlocked -= OnProtocolUnlocked;
                world.BossStarted -= OnBossStarted;
                world.WorldCompleted -= OnWorldCompleted;
            }
            if (checkpoint != null)
            {
                checkpoint.Activated -= OnCheckpointActivated;
                checkpoint.Respawned -= OnRespawned;
            }
            if (shortcut != null) shortcut.Unlocked -= OnShortcutUnlocked;
        }

        private void OnZoneStarted(int index, string title, string lesson) => Show(title);
        private void OnZoneCleared(int index, string id) => Show("SIGNAL PATH STABILIZED", 1.15f);
        private void OnProtocolUnlocked() => Show("PROTOCOL VEIL OPEN", 2.0f);
        private void OnBossStarted() => Show("THE FRACTURED SIGNAL", 2.6f);
        private void OnWorldCompleted() => Show("NULL WARD RECONNECTED", 3.2f);
        private void OnCheckpointActivated() => Show("MEMORY FORGE ONLINE", 1.8f);
        private void OnRespawned() => Show("PATTERN RECONSTRUCTED", 1.5f);
        private void OnShortcutUnlocked(string id) => Show("MEMORY CONDUIT OPEN", 1.8f);

        private void Show(string text, float seconds = -1f)
        {
            _bannerText = text ?? string.Empty;
            _bannerUntil = Time.realtimeSinceStartupAsDouble + (seconds > 0f ? seconds : Mathf.Max(0.2f, bannerSeconds));
        }

        private void OnGUI()
        {
            if (world == null) return;
            EnsureStyles();

            const float left = 18f;
            const float top = 18f;
            bool showControls = Time.realtimeSinceStartupAsDouble - _enteredAt <= Mathf.Max(0f, controlsHintSeconds);
            float width = Mathf.Min(showControls ? 470f : 390f, Screen.width * 0.44f);
            float height = showControls ? 58f : 38f;
            GUI.Box(new Rect(left, top, width, height), string.Empty);
            GUI.Label(new Rect(left + 12f, top + 5f, width - 24f, 26f),
                string.IsNullOrWhiteSpace(world.CurrentObjective) ? "EXPLORE THE NULL WARD" : world.CurrentObjective,
                _objective);

            if (showControls)
            {
                GUI.Label(new Rect(left + 12f, top + 32f, width - 24f, 18f),
                    "SPACE jump · CTRL/ALT dodge · T lock · F sword · RMB/E guard · G interact",
                    _small);
            }

            if (!string.IsNullOrEmpty(_bannerText) && Time.realtimeSinceStartupAsDouble < _bannerUntil)
            {
                float bannerWidth = Mathf.Min(560f, Screen.width - 80f);
                GUI.Box(new Rect((Screen.width - bannerWidth) * 0.5f, 88f, bannerWidth, 42f), string.Empty);
                GUI.Label(new Rect((Screen.width - bannerWidth) * 0.5f, 88f, bannerWidth, 42f), _bannerText, _banner);
            }
        }

        private void EnsureStyles()
        {
            if (_objective == null)
            {
                _objective = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 14,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleLeft,
                    wordWrap = true,
                };
            }
            if (_small == null)
                _small = new GUIStyle(GUI.skin.label) { fontSize = 10, alignment = TextAnchor.MiddleLeft };
            if (_banner == null)
            {
                _banner = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 19,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                };
            }
        }
    }
}
