using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LuckySeven
{
    // Jam-day playable greybox. All character, stage and scenery objects are
    // generated here, rather than copying scenes from the earlier Mindforge game.
    public sealed class LuckyGlade : MonoBehaviour
    {
        private sealed class Bridge
        {
            public GameObject Object;
            public SpriteRenderer Renderer;
            public Collider2D Collider;
            public AnimalSense Required;
            public bool Retained;
        }
        private sealed class Seed
        {
            public GameObject Object;
            public AnimalSense Required;
        }

        private static Sprite square, disc;
        private readonly List<Bridge> bridges = new List<Bridge>();
        private readonly List<Seed> seeds = new List<Seed>();
        private Rigidbody2D body;
        private GameObject squirrel;
        private Camera view;
        private AnimalSense sense = AnimalSense.None;
        private bool live, finished;
        private int collected;
        private float jumpQueuedAt = -100f, lastGroundAt = -100f, facing = 1;
        private const int SolidLayer = 8;

        private void Awake()
        {
            live = Application.platform != RuntimePlatform.WebGLPlayer
                && Array.IndexOf(Environment.GetCommandLineArgs(), "-lucky-seven-live") >= 0;
            BuildWorld();
            if (live)
            {
                var receiver = gameObject.AddComponent<LuckyUdpBridge>();
                receiver.OnSense += SetSense;
            }
        }

        private static Sprite Sprite(bool circle)
        {
            if (!circle && square != null) return square;
            if (circle && disc != null) return disc;
            const int n = 32;
            var texture = new Texture2D(n, n, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;
            var pixels = new Color[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float dx = (x + 0.5f - n / 2f) / (n / 2f);
                    float dy = (y + 0.5f - n / 2f) / (n / 2f);
                    pixels[y * n + x] = (!circle || dx * dx + dy * dy <= 1f)
                        ? Color.white : Color.clear;
                }
            texture.SetPixels(pixels);
            texture.Apply();
            var sprite = UnityEngine.Sprite.Create(texture, new Rect(0, 0, n, n),
                new Vector2(0.5f, 0.5f), n);
            if (circle) disc = sprite; else square = sprite;
            return sprite;
        }

        private GameObject Shape(string name, Vector2 at, Vector2 size, Color color, bool round = false, int sorting = 0)
        {
            var obj = new GameObject(name);
            obj.transform.position = at;
            obj.transform.localScale = new Vector3(size.x, size.y, 1);
            var render = obj.AddComponent<SpriteRenderer>();
            render.sprite = Sprite(round);
            render.color = color;
            render.sortingOrder = sorting;
            return obj;
        }

        private GameObject Ground(string name, float x, float y, float w, Color color)
        {
            var block = Shape(name, new Vector2(x, y), new Vector2(w, 0.7f), color, false, 2);
            block.layer = SolidLayer;
            block.AddComponent<BoxCollider2D>();
            return block;
        }

        private void MakeBridge(string name, float center, AnimalSense required)
        {
            var obj = Ground(name, center, -2.85f, 6.05f,
                required == AnimalSense.Wind ? new Color(0.21f, 0.76f, 0.85f)
                                             : new Color(0.68f, 0.53f, 0.94f));
            bridges.Add(new Bridge {
                Object = obj, Renderer = obj.GetComponent<SpriteRenderer>(),
                Collider = obj.GetComponent<BoxCollider2D>(), Required = required
            });
        }

        private void MakeSeed(float x, float y, AnimalSense required = AnimalSense.None)
        {
            var obj = Shape("Lucky seed " + (seeds.Count + 1), new Vector2(x, y),
                new Vector2(0.52f, 0.52f), new Color(1f, 0.81f, 0.38f), true, 9);
            var glow = Shape("Seed shine", new Vector2(x, y), new Vector2(0.85f, 0.85f),
                new Color(1f, 0.8f, 0.4f, 0.22f), true, 5);
            glow.transform.SetParent(obj.transform, true);
            seeds.Add(new Seed { Object = obj, Required = required });
        }

        private void MakeTree(float x, float top, float height)
        {
            Shape("Tree trunk", new Vector2(x, top - height * 0.5f),
                new Vector2(0.55f, height), new Color(0.35f, 0.29f, 0.32f), false, -4);
            Shape("Canopy", new Vector2(x, top), new Vector2(3f, 2.5f),
                new Color(0.22f, 0.43f, 0.46f, 0.75f), true, -5);
            Shape("Canopy", new Vector2(x - 0.9f, top - 0.2f), new Vector2(2f, 1.7f),
                new Color(0.25f, 0.5f, 0.46f, 0.8f), true, -4);
        }

        private void BuildWorld()
        {
            var cameraObject = new GameObject("Camera");
            view = cameraObject.AddComponent<Camera>();
            cameraObject.tag = "MainCamera";
            view.orthographic = true;
            view.orthographicSize = 5.7f;
            view.backgroundColor = new Color(0.08f, 0.16f, 0.24f);
            cameraObject.transform.position = new Vector3(-7, 0, -10);

            // A route with three gaps too wide for a normal same-height jump.
            Ground("Ancient oak starting perch", -8, -2.85f, 8.0f, new Color(0.42f, 0.62f, 0.45f));
            Ground("Mossy island", 4, -2.85f, 4.0f, new Color(0.41f, 0.62f, 0.45f));
            Ground("Fern lookout", 14, -2.85f, 4.0f, new Color(0.41f, 0.62f, 0.45f));
            Ground("Home branch", 24, -2.85f, 4.5f, new Color(0.43f, 0.66f, 0.48f));
            MakeBridge("Wind bridge A", -1, AnimalSense.Wind);  // x=-4 to 2
            MakeBridge("Root bridge B", 9, AnimalSense.Root);   // x=6 to 12
            MakeBridge("Wind bridge C", 19, AnimalSense.Wind); // x=16 to 22

            // Raised safe branches create a short optional vertical challenge.
            Ground("Acorn ledge", -6.3f, -0.7f, 2.0f, new Color(0.62f, 0.47f, 0.38f));
            Ground("Fern shelf", 4.2f, -0.7f, 2.0f, new Color(0.62f, 0.47f, 0.38f));
            Ground("Lookout shelf", 14.0f, -0.7f, 2.0f, new Color(0.62f, 0.47f, 0.38f));

            // Exactly seven seeds are required. Three demand an active sense.
            MakeSeed(-10f, -1.4f);
            MakeSeed(-6.3f, 0.1f);
            MakeSeed(-1f, -1.4f, AnimalSense.Wind);
            MakeSeed(4.2f, 0.1f);
            MakeSeed(9f, -1.4f, AnimalSense.Root);
            MakeSeed(19f, -1.4f, AnimalSense.Wind);
            MakeSeed(24f, -1.4f);

            foreach (float x in new float[] { -12f, -7f, 3f, 13f, 23f, 27f })
                MakeTree(x, 2.8f, 8f);
            Shape("Lucky moon", new Vector2(8, 3.9f), new Vector2(2.3f, 2.3f),
                new Color(0.94f, 0.9f, 0.73f, 0.25f), true, -8);
            Shape("Nest tree", new Vector2(25.9f, -0.5f), new Vector2(1.3f, 4.9f),
                new Color(0.3f, 0.26f, 0.32f), false, -2);
            Shape("Nest", new Vector2(25.2f, -1.2f), new Vector2(1.25f, 0.65f),
                new Color(1f, 0.64f, 0.3f), true, 4);

            squirrel = Shape("Flying squirrel", new Vector2(-10.7f, -1.5f),
                new Vector2(0.8f, 0.84f), new Color(0.85f, 0.74f, 0.64f), true, 8);
            body = squirrel.AddComponent<Rigidbody2D>();
            body.gravityScale = 2.2f;
            body.freezeRotation = true;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            var capsule = squirrel.AddComponent<CapsuleCollider2D>();
            capsule.size = new Vector2(0.95f, 1.0f);

            // Features are children so they travel with the animal.
            var tail = Shape("Fluffy tail", Vector2.zero, new Vector2(0.6f, 0.48f),
                new Color(0.67f, 0.55f, 0.5f), true, 6);
            tail.transform.SetParent(squirrel.transform, false);
            tail.transform.localPosition = new Vector3(-0.68f, -0.1f, 0);
            var eye = Shape("Big bright eye", Vector2.zero, new Vector2(0.16f, 0.21f),
                new Color(0.1f, 0.13f, 0.19f), true, 10);
            eye.transform.SetParent(squirrel.transform, false);
            eye.transform.localPosition = new Vector3(0.22f, 0.2f, 0);
            UpdateBridges();
        }

        public void SetSense(AnimalSense selected)
        {
            if (selected == AnimalSense.None || finished) return;
            sense = selected;
            UpdateBridges();
        }

        private void UpdateBridges()
        {
            foreach (Bridge bridge in bridges)
            {
                bool active = bridge.Required == sense;
                // Do not erase a supporting platform beneath the character when a
                // new choice arrives: delay its removal until the player leaves.
                Vector2 at = bridge.Object.transform.position;
                bool supporting = squirrel != null
                    && Mathf.Abs(squirrel.transform.position.x - at.x) < 3.35f
                    && squirrel.transform.position.y > at.y
                    && squirrel.transform.position.y < at.y + 1.5f;
                // A new selection may not erase an existing floor under the player.
                // Never create a floor on approach unless its sense was selected.
                if (active) bridge.Retained = false;
                else if (supporting && bridge.Collider.enabled) bridge.Retained = true;
                else if (!supporting) bridge.Retained = false;
                bridge.Collider.enabled = active || bridge.Retained;
                Color c = bridge.Required == AnimalSense.Wind
                    ? new Color(0.21f, 0.76f, 0.85f)
                    : new Color(0.68f, 0.53f, 0.94f);
                c.a = active ? 1f : bridge.Retained ? 0.55f : 0.17f;
                bridge.Renderer.color = c;
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.R) && finished)
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
                return;
            }
            if (!finished && !live)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1)) SetSense(AnimalSense.Wind);
                if (Input.GetKeyDown(KeyCode.Alpha2)) SetSense(AnimalSense.Root);
            }
            if (Input.GetKeyDown(KeyCode.Space)) jumpQueuedAt = Time.time;
            UpdateBridges();

            if (!finished)
            {
                foreach (Seed seed in seeds)
                {
                    if (seed.Object == null || (seed.Required != AnimalSense.None && seed.Required != sense))
                        continue;
                    if ((seed.Object.transform.position - squirrel.transform.position).sqrMagnitude < 0.65f)
                    {
                        Destroy(seed.Object);
                        seed.Object = null;
                        collected++;
                    }
                }
                if (collected == 7 && Vector2.Distance(squirrel.transform.position,
                    new Vector2(25.2f, -1.5f)) < 1.45f)
                    finished = true;
                if (squirrel.transform.position.y < -9f)
                {
                    squirrel.transform.position = new Vector3(
                        squirrel.transform.position.x < 1 ? -10.7f :
                        squirrel.transform.position.x < 11 ? 4f :
                        squirrel.transform.position.x < 21 ? 14f : 23f, -1.2f, 0);
                    body.velocity = Vector2.zero;
                }
            }
            Vector3 c = view.transform.position;
            Vector3 target = new Vector3(
                Mathf.Clamp(squirrel.transform.position.x + facing * 1.9f, -7f, 22.5f),
                Mathf.Clamp(squirrel.transform.position.y + 1.2f, 0f, 3f), -10);
            view.transform.position = Vector3.Lerp(c, target, 1f - Mathf.Exp(-5f * Time.deltaTime));
        }

        private void FixedUpdate()
        {
            if (finished) { body.velocity = Vector2.zero; return; }
            float direction = Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow) ? -1 :
                Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow) ? 1 : 0;
            if (direction != 0) facing = direction;
            bool grounded = Physics2D.OverlapBox(
                (Vector2)squirrel.transform.position + Vector2.down * 0.57f,
                new Vector2(0.54f, 0.11f), 0, 1 << SolidLayer) != null;
            if (grounded) lastGroundAt = Time.time;
            Vector2 velocity = body.velocity;
            velocity.x = direction * 6.3f;
            if (Time.time - jumpQueuedAt < 0.14f && Time.time - lastGroundAt < 0.11f)
            {
                velocity.y = 10.6f;
                jumpQueuedAt = -100f;
                lastGroundAt = -100f;
            }
            bool glide = !grounded && Input.GetKey(KeyCode.Space) && velocity.y < 0;
            body.gravityScale = glide ? 0.62f : 2.2f;
            if (glide) velocity.y = Mathf.Max(velocity.y, -3.2f);
            body.velocity = velocity;
        }

        private void OnGUI()
        {
            var style = new GUIStyle(GUI.skin.label) {
                fontSize = Mathf.RoundToInt(Mathf.Clamp(Screen.height / 42f, 13f, 22f)),
                wordWrap = true
            };
            style.normal.textColor = new Color(0.99f, 0.96f, 0.84f);
            GUI.Box(new Rect(15, 15, Mathf.Min(530, Screen.width - 30), 120), "");
            GUI.Label(new Rect(27, 19, Screen.width - 54, 33),
                "LUCKY SEVEN  |  Seeds " + collected + "/7", style);
            GUI.Label(new Rect(27, 51, Screen.width - 54, 34),
                "Sense: " + sense + (live ? "   |   LIVE EEG (unqualified)" : "   |   KEYBOARD DEMO (not EEG)"), style);
            GUI.Label(new Rect(27, 84, Screen.width - 54, 46),
                live ? "A/D move, SPACE jump/glide. Neural decisions: Wind/Root."
                     : "A/D move, SPACE jump/glide, 1 Wind, 2 Root.", style);
            if (finished)
            {
                GUI.Box(new Rect(Screen.width / 2f - 190, Screen.height / 2f - 65, 380, 130),
                    "7/7!  HOME AT LAST\nYour little squirrel is lucky indeed.\nPress R to restart.");
            }
        }
    }
}
