using UnityEngine;
using Mindforge.Presentation;
using Mindforge.SoulWisp;
using Mindforge.Telemetry;

namespace Mindforge.Combat
{
    /// <summary>
    /// Installs the physical-arsenal vertical slice on the existing competition scene
    /// without making the editor scene assembler a second source of gameplay truth.
    /// Procedural geometry is intentionally replaceable presentation scaffolding.
    /// </summary>
    public static class PhysicalArsenalBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            GuardianCombatInput input = Object.FindObjectOfType<GuardianCombatInput>(true);
            if (input == null) return;
            GameObject guardian = input.gameObject;
            if (guardian.GetComponent<GuardianSwordShieldController>() != null) return;

            GuardianEquipmentLoadout loadout = guardian.GetComponent<GuardianEquipmentLoadout>();
            if (loadout == null) loadout = guardian.AddComponent<GuardianEquipmentLoadout>();
            GuardianStamina stamina = guardian.GetComponent<GuardianStamina>();
            if (stamina == null) stamina = guardian.AddComponent<GuardianStamina>();
            NeuralFocusResonance resonance = guardian.GetComponent<NeuralFocusResonance>();
            if (resonance == null) resonance = guardian.AddComponent<NeuralFocusResonance>();

            GameObject arsenalRoot = new GameObject("PhysicalArsenalRig");
            arsenalRoot.transform.SetParent(guardian.transform, false);

            Color sight = new Color(0.18f, 0.62f, 1f);
            Color sightHot = new Color(0.38f, 0.92f, 1f);
            Color guard = new Color(0.18f, 1f, 0.52f);
            Material bladeCoreMaterial = CreatePbrMaterial(
                "AetherbladeForgedCore",
                new Color(0.025f, 0.050f, 0.095f),
                0.92f,
                0.72f,
                new Color(0.04f, 0.18f, 0.42f));
            Material bladeEdgeMaterial = CreatePbrMaterial(
                "AetherbladeEnergyEdge",
                new Color(0.06f, 0.28f, 0.52f),
                0.35f,
                0.86f,
                sightHot * 3.2f);
            Material hiltMaterial = CreatePbrMaterial(
                "AetherbladeHilt",
                new Color(0.055f, 0.065f, 0.085f),
                0.88f,
                0.58f,
                new Color(0.03f, 0.08f, 0.16f));
            Material gripMaterial = CreatePbrMaterial(
                "AetherbladeGrip",
                new Color(0.055f, 0.040f, 0.050f),
                0.12f,
                0.32f,
                Color.black);
            Material shieldMaterial = CreatePbrMaterial(
                "MindforgeRuntimeShield",
                new Color(0.08f, 0.20f, 0.16f),
                0.72f,
                0.54f,
                guard * 1.15f);
            Material trailMaterial = CreateTrailMaterial();

            Transform swordRoot = NewChild("SwordRoot", arsenalRoot.transform, new Vector3(0.34f, 0.55f, 0.16f));

            GameObject blade = GameObject.CreatePrimitive(PrimitiveType.Cube);
            blade.name = "AetherbladeCore";
            blade.transform.SetParent(swordRoot, false);
            blade.transform.localPosition = new Vector3(0f, 0f, 1.05f);
            blade.transform.localScale = new Vector3(0.13f, 0.055f, 1.55f);
            DisableCollider(blade);
            Renderer swordRenderer = blade.GetComponent<Renderer>();
            if (swordRenderer != null) swordRenderer.sharedMaterial = bladeCoreMaterial;

            // Thin luminous rails provide a readable blade silhouette without making
            // the whole weapon a uniformly glowing rectangle.
            CreateBladeRail("AetherbladeEdgeL", blade.transform, -0.43f, bladeEdgeMaterial);
            CreateBladeRail("AetherbladeEdgeR", blade.transform, 0.43f, bladeEdgeMaterial);
            CreateBladeRail("AetherbladeSpine", blade.transform, 0f, bladeEdgeMaterial, 0.075f, 0.20f);

            GameObject guardBar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            guardBar.name = "AetherbladeCrossguard";
            guardBar.transform.SetParent(swordRoot, false);
            guardBar.transform.localPosition = new Vector3(0f, 0f, 0.18f);
            guardBar.transform.localScale = new Vector3(0.58f, 0.105f, 0.10f);
            DisableCollider(guardBar);
            Renderer guardRenderer = guardBar.GetComponent<Renderer>();
            if (guardRenderer != null) guardRenderer.sharedMaterial = hiltMaterial;

            GameObject grip = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            grip.name = "AetherbladeGrip";
            grip.transform.SetParent(swordRoot, false);
            grip.transform.localPosition = new Vector3(0f, 0f, -0.12f);
            grip.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            grip.transform.localScale = new Vector3(0.075f, 0.25f, 0.075f);
            DisableCollider(grip);
            Renderer gripRenderer = grip.GetComponent<Renderer>();
            if (gripRenderer != null) gripRenderer.sharedMaterial = gripMaterial;

            GameObject pommel = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            pommel.name = "AetherbladePommel";
            pommel.transform.SetParent(swordRoot, false);
            pommel.transform.localPosition = new Vector3(0f, 0f, -0.48f);
            pommel.transform.localScale = Vector3.one * 0.16f;
            DisableCollider(pommel);
            Renderer pommelRenderer = pommel.GetComponent<Renderer>();
            if (pommelRenderer != null) pommelRenderer.sharedMaterial = bladeEdgeMaterial;

            Transform swordTip = NewChild("SwordEnergyTip", swordRoot, new Vector3(0f, 0f, 1.88f));
            TrailRenderer trail = swordTip.gameObject.AddComponent<TrailRenderer>();
            trail.sharedMaterial = trailMaterial;
            trail.time = 0.20f;
            trail.minVertexDistance = 0.028f;
            trail.emitting = false;
            trail.startColor = sightHot;
            trail.endColor = new Color(sight.r, sight.g, sight.b, 0f);
            Light swordLight = swordTip.gameObject.AddComponent<Light>();
            swordLight.type = LightType.Point;
            swordLight.color = sightHot;
            swordLight.range = 2.8f;
            swordLight.intensity = 0.28f;

            Transform shieldRoot = NewChild("ShieldRoot", arsenalRoot.transform, new Vector3(-0.28f, 0.54f, 0.75f));
            GameObject shieldVisual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            shieldVisual.name = "VerdantWard";
            shieldVisual.transform.SetParent(shieldRoot, false);
            shieldVisual.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            shieldVisual.transform.localScale = new Vector3(0.68f, 0.09f, 0.86f);
            DisableCollider(shieldVisual);
            Renderer shieldRenderer = shieldVisual.GetComponent<Renderer>();
            if (shieldRenderer != null) shieldRenderer.sharedMaterial = shieldMaterial;

            BoxCollider shieldCollider = shieldRoot.gameObject.AddComponent<BoxCollider>();
            shieldCollider.isTrigger = true;
            shieldCollider.center = Vector3.zero;
            shieldCollider.size = new Vector3(1.32f, 1.52f, 0.24f);
            shieldCollider.enabled = false;
            GuardianShieldHitbox shieldHitbox = shieldRoot.gameObject.AddComponent<GuardianShieldHitbox>();
            Light shieldLight = shieldRoot.gameObject.AddComponent<Light>();
            shieldLight.type = LightType.Point;
            shieldLight.range = 2.6f;
            shieldLight.intensity = 0.1f;
            CreateShieldOutline(shieldRoot, trailMaterial, guard);

            GuardianSwordShieldRig rig = arsenalRoot.AddComponent<GuardianSwordShieldRig>();
            rig.ConfigureRuntime(
                swordRoot,
                blade.transform,
                swordRenderer,
                trail,
                swordLight,
                shieldRoot,
                shieldRenderer,
                shieldLight);

            GuardianSwordShieldController physical = guardian.AddComponent<GuardianSwordShieldController>();
            FracturedSignalDirector boss = Object.FindObjectOfType<FracturedSignalDirector>(true);
            Transform target = boss != null ? boss.transform : null;
            FluxMeter flux = guardian.GetComponent<FluxMeter>();
            HitStopController hitStop = Object.FindObjectOfType<HitStopController>(true);
            CombatTuning tuning = FindAsset<CombatTuning>();
            physical.ConfigureRuntime(resonance, flux, target, shieldHitbox, hitStop, tuning);
            shieldHitbox.Configure(physical, shieldCollider);

            if (boss != null)
            {
                FracturedSignalMeleeDirector melee = boss.GetComponent<FracturedSignalMeleeDirector>();
                if (melee == null) melee = boss.gameObject.AddComponent<FracturedSignalMeleeDirector>();
                melee.ConfigureRuntime(boss, guardian.transform, physical, guardian.GetComponent<GuardianMotor>());
            }

            GuardianArmamentPresentationDriver driver = arsenalRoot.AddComponent<GuardianArmamentPresentationDriver>();
            driver.Configure(
                physical,
                rig,
                input,
                loadout,
                guardian.GetComponent<AuraBuffController>(),
                resonance);

            if (guardian.GetComponent<PhysicalArsenalMarkerBridge>() == null)
                guardian.AddComponent<PhysicalArsenalMarkerBridge>();

            Debug.Log("[Mindforge] Physical arsenal installed: forged Aetherblade + Verdant Ward + Warden Weave. Basic sword/dodge actions are unrestricted; shield pressure uses Guard Integrity.");
        }

        private static void CreateBladeRail(string name, Transform parent, float x, Material material, float width = 0.12f, float height = 0.72f)
        {
            GameObject rail = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rail.name = name;
            rail.transform.SetParent(parent, false);
            rail.transform.localPosition = new Vector3(x, 0f, 0f);
            rail.transform.localScale = new Vector3(width, height, 0.985f);
            DisableCollider(rail);
            Renderer renderer = rail.GetComponent<Renderer>();
            if (renderer != null) renderer.sharedMaterial = material;
        }

        private static Transform NewChild(string name, Transform parent, Vector3 localPosition)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            return go.transform;
        }

        private static void DisableCollider(GameObject go)
        {
            Collider collider = go != null ? go.GetComponent<Collider>() : null;
            if (collider != null) collider.enabled = false;
        }

        private static Material CreatePbrMaterial(string name, Color baseColor, float metallic, float smoothness, Color emission)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material material = new Material(shader) { name = name };
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", baseColor);
            else if (material.HasProperty("_Color")) material.SetColor("_Color", baseColor);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", Mathf.Clamp01(metallic));
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", Mathf.Clamp01(smoothness));
            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", emission);
            }
            return material;
        }

        private static Material CreateTrailMaterial()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default") ?? Shader.Find("Standard");
            Material material = new Material(shader) { name = "MindforgeRuntimeEnergyTrail" };
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", new Color(0.18f, 0.72f, 1f, 0.88f));
            if (material.HasProperty("_Color")) material.SetColor("_Color", new Color(0.18f, 0.72f, 1f, 0.88f));
            return material;
        }

        private static void CreateShieldOutline(Transform parent, Material material, Color color)
        {
            GameObject go = new GameObject("VerdantWardOutline");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0f, 0f, -0.14f);
            LineRenderer line = go.AddComponent<LineRenderer>();
            line.sharedMaterial = material;
            line.useWorldSpace = false;
            line.loop = true;
            line.widthMultiplier = 0.035f;
            line.positionCount = 48;
            line.startColor = color;
            line.endColor = color;
            for (int i = 0; i < line.positionCount; i++)
            {
                float angle = i / (float)line.positionCount * Mathf.PI * 2f;
                line.SetPosition(i, new Vector3(Mathf.Cos(angle) * 0.72f, Mathf.Sin(angle) * 0.86f, 0f));
            }
        }

        private static T FindAsset<T>() where T : Object
        {
            T[] assets = Resources.FindObjectsOfTypeAll<T>();
            return assets != null && assets.Length > 0 ? assets[0] : null;
        }
    }
}
