using System;
using UnityEngine;
using Mindforge.Combat;
using Mindforge.Traversal;

namespace Mindforge.Presentation
{
    /// <summary>
    /// Lightweight procedural audio punctuation for Aetheria V2. Clips are synthesized
    /// once at startup and triggered only from authoritative gameplay events. This layer
    /// never samples input, applies damage, moves actors, or changes neural state.
    /// </summary>
    [DefaultExecutionOrder(1500)]
    public sealed class AetheriaCombatAudioV2 : MonoBehaviour
    {
        [SerializeField] private GuardianMotor motor;
        [SerializeField] private GuardianSwordShieldController blade;
        [SerializeField] private GuardianHoverbikeController bike;
        [SerializeField] private FracturedSignalDirector boss;
        [SerializeField] private FracturedSignalMeleeDirector bossMelee;
        [SerializeField, Range(0f, 1f)] private float masterVolume = 0.48f;
        [SerializeField, Range(0f, 1f)] private float motorVolume = 0.18f;

        private AudioSource _oneShot;
        private AudioSource _motorLoop;
        private AudioClip _jump;
        private AudioClip _doubleJump;
        private AudioClip _dash;
        private AudioClip _land;
        private AudioClip _bladeStart;
        private AudioClip _bladeHit;
        private AudioClip _parry;
        private AudioClip _mount;
        private AudioClip _boost;
        private AudioClip _bossCharge;
        private AudioClip _bossFire;
        private AudioClip _bossMelee;
        private AudioClip _motor;
        private int _variationCounter;
        private bool _subscribed;

        private void Awake()
        {
            Resolve();
            BuildSources();
            BuildClips();
        }

        private void OnEnable()
        {
            Resolve();
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
            if (_motorLoop != null) _motorLoop.Stop();
        }

        private void OnDestroy()
        {
            DestroyClip(_jump);
            DestroyClip(_doubleJump);
            DestroyClip(_dash);
            DestroyClip(_land);
            DestroyClip(_bladeStart);
            DestroyClip(_bladeHit);
            DestroyClip(_parry);
            DestroyClip(_mount);
            DestroyClip(_boost);
            DestroyClip(_bossCharge);
            DestroyClip(_bossFire);
            DestroyClip(_bossMelee);
            DestroyClip(_motor);
        }

        private void Update()
        {
            if (_motorLoop == null || bike == null) return;
            bool mounted = bike.Mounted;
            if (mounted && !_motorLoop.isPlaying) _motorLoop.Play();
            else if (!mounted && _motorLoop.isPlaying) _motorLoop.Stop();

            if (!mounted) return;
            float speed = bike.Speed01;
            _motorLoop.volume = Mathf.Clamp01(masterVolume * motorVolume * (0.45f + speed * 0.55f));
            _motorLoop.pitch = 0.82f + speed * 0.34f + (bike.Boosting ? 0.15f : 0f);
        }

        private void Resolve()
        {
            if (motor == null) motor = GetComponent<GuardianMotor>();
            if (blade == null) blade = GetComponent<GuardianSwordShieldController>();
            if (bike == null) bike = GetComponent<GuardianHoverbikeController>();
            if (boss == null) boss = FindObjectOfType<FracturedSignalDirector>(true);
            if (bossMelee == null && boss != null) bossMelee = boss.GetComponent<FracturedSignalMeleeDirector>();
        }

        private void BuildSources()
        {
            if (_oneShot == null)
            {
                _oneShot = gameObject.AddComponent<AudioSource>();
                _oneShot.playOnAwake = false;
                _oneShot.loop = false;
                _oneShot.spatialBlend = 0.08f;
                _oneShot.dopplerLevel = 0f;
            }
            if (_motorLoop == null)
            {
                _motorLoop = gameObject.AddComponent<AudioSource>();
                _motorLoop.playOnAwake = false;
                _motorLoop.loop = true;
                _motorLoop.spatialBlend = 0.18f;
                _motorLoop.dopplerLevel = 0f;
            }
        }

        private void BuildClips()
        {
            if (_jump != null) return;
            _jump = Tone("Aetheria_Jump", 0.11f, 260f, 520f, 0.32f, Wave.Sine);
            _doubleJump = Tone("Aetheria_DoubleJump", 0.13f, 410f, 810f, 0.30f, Wave.Triangle);
            _dash = Tone("Aetheria_Dash", 0.10f, 170f, 70f, 0.34f, Wave.Noise);
            _land = Tone("Aetheria_Land", 0.12f, 105f, 58f, 0.40f, Wave.Noise);
            _bladeStart = Tone("Aetheria_BladeStart", 0.09f, 310f, 910f, 0.26f, Wave.Saw);
            _bladeHit = Tone("Aetheria_BladeHit", 0.12f, 145f, 520f, 0.36f, Wave.Square);
            _parry = Tone("Aetheria_Parry", 0.15f, 760f, 1540f, 0.28f, Wave.Triangle);
            _mount = Tone("Aetheria_Mount", 0.16f, 120f, 360f, 0.30f, Wave.Sine);
            _boost = Tone("Aetheria_Boost", 0.20f, 90f, 680f, 0.34f, Wave.Saw);
            _bossCharge = Tone("Malatract_Charge", 0.22f, 82f, 240f, 0.24f, Wave.Sine);
            _bossFire = Tone("Malatract_Fire", 0.15f, 195f, 72f, 0.34f, Wave.Square);
            _bossMelee = Tone("Malatract_Melee", 0.18f, 115f, 48f, 0.36f, Wave.Noise);

            // 75 Hz * 0.32 s = exactly 24 cycles at the authored clip duration. Keeping
            // the base motor tone constant closes the phase at the loop seam; perceived
            // RPM still comes from AudioSource pitch, which is presentation-only.
            _motor = Tone("PrismBike_MotorLoop", 0.32f, 75f, 75f, 0.16f, Wave.Saw, loopFriendly: true);
            _motorLoop.clip = _motor;
        }

        private void Subscribe()
        {
            if (_subscribed) return;
            Resolve();
            if (motor != null)
            {
                motor.Jumped += OnJumped;
                motor.DoubleJumped += OnDoubleJumped;
                // GuardianMotor emits DashStarted for both ground and air dashes and then
                // additionally emits AirDashStarted for air dashes. Subscribe once to the
                // canonical event and inspect the already-authoritative motor state so an
                // air dash cannot produce two stacked one-shots.
                motor.DashStarted += OnDash;
                motor.Landed += OnLanded;
            }
            if (blade != null)
            {
                blade.SwordAttackStarted += OnBladeStart;
                blade.SwordHit += OnBladeHit;
                blade.SwordProjectileParried += OnParry;
                blade.PerfectGuard += OnPerfectGuard;
            }
            if (bike != null)
            {
                bike.MountedChanged += OnMountedChanged;
                bike.BoostStarted += OnBoost;
            }
            if (boss != null)
            {
                boss.AttackTelegraphed += OnBossTelegraph;
                boss.AttackFired += OnBossFired;
            }
            if (bossMelee != null)
                bossMelee.MeleeTelegraphed += OnBossMeleeTelegraph;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            if (motor != null)
            {
                motor.Jumped -= OnJumped;
                motor.DoubleJumped -= OnDoubleJumped;
                motor.DashStarted -= OnDash;
                motor.Landed -= OnLanded;
            }
            if (blade != null)
            {
                blade.SwordAttackStarted -= OnBladeStart;
                blade.SwordHit -= OnBladeHit;
                blade.SwordProjectileParried -= OnParry;
                blade.PerfectGuard -= OnPerfectGuard;
            }
            if (bike != null)
            {
                bike.MountedChanged -= OnMountedChanged;
                bike.BoostStarted -= OnBoost;
            }
            if (boss != null)
            {
                boss.AttackTelegraphed -= OnBossTelegraph;
                boss.AttackFired -= OnBossFired;
            }
            if (bossMelee != null)
                bossMelee.MeleeTelegraphed -= OnBossMeleeTelegraph;
            _subscribed = false;
        }

        private void Play(AudioClip clip, float gain, float pitch = 1f)
        {
            if (_oneShot == null || clip == null) return;
            _variationCounter++;
            float deterministicJitter = ((_variationCounter * 37) % 9 - 4) * 0.006f;
            _oneShot.pitch = Mathf.Clamp(pitch + deterministicJitter, 0.5f, 1.8f);
            _oneShot.PlayOneShot(clip, Mathf.Clamp01(masterVolume * gain));
        }

        private void OnJumped() => Play(_jump, 0.56f, 1f);
        private void OnDoubleJumped() => Play(_doubleJump, 0.62f, 1.02f);
        private void OnDash()
        {
            bool air = motor != null && motor.IsAirDashing;
            Play(_dash, air ? 0.60f : 0.54f, air ? 1.18f : 0.98f);
        }
        private void OnLanded(float impact) => Play(_land, Mathf.Lerp(0.18f, 0.62f, Mathf.Clamp01(impact / 16f)), 0.92f);
        private void OnBladeStart() => Play(_bladeStart, 0.42f, 1f);
        private void OnBladeHit(float damage, float poise) => Play(_bladeHit, Mathf.Clamp(0.42f + damage / 90f, 0.42f, 0.75f), 0.96f);
        private void OnParry(float damage) => Play(_parry, 0.68f, 1.08f);
        private void OnPerfectGuard() => Play(_parry, 0.74f, 0.92f);
        private void OnMountedChanged(bool mounted) { if (mounted) Play(_mount, 0.48f, 1f); }
        private void OnBoost() => Play(_boost, 0.58f, 1f);
        private void OnBossTelegraph(string pattern, int count, bool heavy) => Play(_bossCharge, heavy ? 0.48f : 0.30f, heavy ? 0.86f : 1f);
        private void OnBossFired(string pattern, int count, bool heavy) => Play(_bossFire, heavy ? 0.58f : 0.40f, heavy ? 0.88f : 1.04f);
        private void OnBossMeleeTelegraph(string pattern, Vector3 direction, float range, float arc, bool heavy) => Play(_bossMelee, heavy ? 0.50f : 0.34f, heavy ? 0.82f : 0.96f);

        private enum Wave { Sine, Triangle, Square, Saw, Noise }

        private static AudioClip Tone(string name, float seconds, float startHz, float endHz, float amplitude, Wave wave, bool loopFriendly = false)
        {
            const int sampleRate = 24000;
            int samples = Mathf.Max(64, Mathf.RoundToInt(seconds * sampleRate));
            float[] data = new float[samples];
            uint noise = 0xA53C91E5u;
            float phase = 0f;
            for (int i = 0; i < samples; i++)
            {
                float t = i / (float)Mathf.Max(1, samples - 1);
                float hz = Mathf.Lerp(startHz, endHz, t);
                phase += hz / sampleRate;
                phase -= Mathf.Floor(phase);
                float value;
                switch (wave)
                {
                    case Wave.Triangle: value = 1f - 4f * Mathf.Abs(phase - 0.5f); break;
                    case Wave.Square: value = phase < 0.5f ? 1f : -1f; break;
                    case Wave.Saw: value = phase * 2f - 1f; break;
                    case Wave.Noise:
                        noise = noise * 1664525u + 1013904223u;
                        value = ((noise >> 8) & 0xFFFF) / 32767.5f - 1f;
                        break;
                    default: value = Mathf.Sin(phase * Mathf.PI * 2f); break;
                }
                float envelope = loopFriendly ? 0.88f : Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI);
                data[i] = value * amplitude * envelope;
            }
            AudioClip clip = AudioClip.Create(name, samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static void DestroyClip(AudioClip clip)
        {
            if (clip != null) UnityEngine.Object.Destroy(clip);
        }
    }
}
