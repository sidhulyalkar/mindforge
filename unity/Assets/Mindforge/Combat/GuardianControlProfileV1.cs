using System;
using UnityEngine;

namespace Mindforge.Combat
{
    public enum GuardianControlAction
    {
        Interact = 0,
        TargetLock = 1,
        JumpHover = 2,
        EvadeBoost = 3,
        Blade = 4,
        Cleave = 5,
        Counter = 6,
        Bloom = 7,
        Menu = 8,
        JudgeLens = 9,
        ChannelWisp = 10,
    }

    /// <summary>
    /// Canonical conventional-control vocabulary for Mindforge. Gameplay systems may sample
    /// this profile, while tutorials/HUDs use the same profile for labels. Neural evidence is
    /// intentionally absent: this component describes only explicit player-owned controls.
    ///
    /// Channel Wisp is a neutral WHEN input. It opens/cancels a short decision window but
    /// never says WHICH neural aura the player wants. Sight/Guard selection remains EEG-owned.
    /// </summary>
    [DefaultExecutionOrder(-950)]
    public sealed class GuardianControlProfileV1 : MonoBehaviour
    {
        [Header("Context + targeting")]
        [SerializeField] private KeyCode interact = KeyCode.E;
        [SerializeField] private KeyCode targetLock = KeyCode.T;

        [Header("Traversal")]
        [SerializeField] private KeyCode jumpHover = KeyCode.Space;
        [SerializeField] private KeyCode evadeBoostPrimary = KeyCode.LeftShift;
        [SerializeField] private KeyCode evadeBoostSecondary = KeyCode.RightShift;
        [SerializeField] private bool rightMouseEvades = true;

        [Header("Combat")]
        [SerializeField] private KeyCode blade = KeyCode.F;
        [SerializeField] private bool leftMouseBlade = true;
        [SerializeField] private KeyCode cleave = KeyCode.Q;
        [SerializeField] private KeyCode counter = KeyCode.C;
        [SerializeField] private KeyCode bloom = KeyCode.R;
        [SerializeField] private KeyCode channelWisp = KeyCode.V;

        [Header("Information")]
        [SerializeField] private KeyCode menu = KeyCode.Tab;
        [SerializeField] private KeyCode judgeLens = KeyCode.F10;

        public static GuardianControlProfileV1 Instance { get; private set; }

        public KeyCode InteractKey => interact;
        public KeyCode TargetLockKey => targetLock;
        public KeyCode JumpHoverKey => jumpHover;
        public KeyCode ChannelWispKey => channelWisp;
        public KeyCode MenuKey => menu;
        public KeyCode JudgeLensKey => judgeLens;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogError("[Mindforge:Controls] Multiple GuardianControlProfileV1 instances exist.");
                enabled = false;
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public Vector2 SampleMovement()
        {
            float x = 0f;
            float y = 0f;
            if (Input.GetKey(KeyCode.A)) x -= 1f;
            if (Input.GetKey(KeyCode.D)) x += 1f;
            if (Input.GetKey(KeyCode.S)) y -= 1f;
            if (Input.GetKey(KeyCode.W)) y += 1f;
            return Vector2.ClampMagnitude(new Vector2(x, y), 1f);
        }

        public bool Pressed(GuardianControlAction action)
        {
            switch (action)
            {
                case GuardianControlAction.Interact: return Input.GetKeyDown(interact);
                case GuardianControlAction.TargetLock: return Input.GetKeyDown(targetLock);
                case GuardianControlAction.JumpHover: return Input.GetKeyDown(jumpHover);
                case GuardianControlAction.EvadeBoost:
                    return Input.GetKeyDown(evadeBoostPrimary) || Input.GetKeyDown(evadeBoostSecondary) ||
                           (rightMouseEvades && Input.GetMouseButtonDown(1));
                case GuardianControlAction.Blade:
                    return Input.GetKeyDown(blade) || (leftMouseBlade && Input.GetMouseButtonDown(0));
                case GuardianControlAction.Cleave: return Input.GetKeyDown(cleave);
                case GuardianControlAction.Counter: return Input.GetKeyDown(counter);
                case GuardianControlAction.Bloom: return Input.GetKeyDown(bloom);
                case GuardianControlAction.ChannelWisp: return Input.GetKeyDown(channelWisp);
                case GuardianControlAction.Menu: return Input.GetKeyDown(menu);
                case GuardianControlAction.JudgeLens: return Input.GetKeyDown(judgeLens);
                default: return false;
            }
        }

        public bool Held(GuardianControlAction action)
        {
            switch (action)
            {
                case GuardianControlAction.JumpHover: return Input.GetKey(jumpHover);
                case GuardianControlAction.EvadeBoost:
                    return Input.GetKey(evadeBoostPrimary) || Input.GetKey(evadeBoostSecondary) ||
                           (rightMouseEvades && Input.GetMouseButton(1));
                case GuardianControlAction.Blade:
                    return Input.GetKey(blade) || (leftMouseBlade && Input.GetMouseButton(0));
                case GuardianControlAction.ChannelWisp: return Input.GetKey(channelWisp);
                default: return false;
            }
        }

        public string Label(GuardianControlAction action)
        {
            switch (action)
            {
                case GuardianControlAction.Interact: return KeyLabel(interact);
                case GuardianControlAction.TargetLock: return KeyLabel(targetLock);
                case GuardianControlAction.JumpHover: return KeyLabel(jumpHover);
                case GuardianControlAction.EvadeBoost:
                    return rightMouseEvades ? KeyLabel(evadeBoostPrimary) + " / RMB" : KeyLabel(evadeBoostPrimary);
                case GuardianControlAction.Blade:
                    return leftMouseBlade ? KeyLabel(blade) + " / LMB" : KeyLabel(blade);
                case GuardianControlAction.Cleave: return KeyLabel(cleave);
                case GuardianControlAction.Counter: return KeyLabel(counter);
                case GuardianControlAction.Bloom: return KeyLabel(bloom);
                case GuardianControlAction.ChannelWisp: return KeyLabel(channelWisp);
                case GuardianControlAction.Menu: return KeyLabel(menu);
                case GuardianControlAction.JudgeLens: return KeyLabel(judgeLens);
                default: return string.Empty;
            }
        }

        public static GuardianControlProfileV1 ResolveOrCreate()
        {
            if (Instance != null) return Instance;
            GuardianControlProfileV1 existing = FindObjectOfType<GuardianControlProfileV1>(true);
            if (existing != null) return existing;
            GameObject root = GameObject.Find("MindforgeControls");
            if (root == null) root = new GameObject("MindforgeControls");
            return root.AddComponent<GuardianControlProfileV1>();
        }

        private static string KeyLabel(KeyCode key)
        {
            string text = key.ToString();
            if (text.StartsWith("Left", StringComparison.Ordinal)) text = text.Substring(4);
            if (text.StartsWith("Right", StringComparison.Ordinal)) text = text.Substring(5);
            return text.ToUpperInvariant();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install() => ResolveOrCreate();
    }
}
