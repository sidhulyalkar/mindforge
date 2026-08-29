using UnityEngine;
using Mindforge.Presentation;
using Mindforge.SoulWisp;
using Mindforge.Telemetry;

namespace Mindforge.Combat
{
    /// <summary>
    /// Installs the physical-arsenal vertical slice on the competition scene without
    /// making presentation a second source of gameplay truth. Grounded World V1 uses a
    /// single resonant energy blade plus conventional dodge roll; the former physical
    /// shield is intentionally absent from the visible/control surface.
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
            if (guardian.GetComponent<GuardianDodgeRollPresentation>() == null)
                guardian.AddComponent<GuardianDodgeRollPresentation>();

            GameObject arsenalRoot = new GameObject("PhysicalArsenalRig");
            arsenalRoot.transform.SetParent(guardian.transform, false);

            Color sight = new Color(0.12f, 0.58f, 1f);
            Color sightHot = new Color(0.52f, 0.96f, 1f);
            Material bladeCoreMaterial = CreatePbrMaterial(
                "AetherbladeWhiteCore",
                new Color(0.92f, 0.98f, 1f),
                0.05f,
                0.98f,
                new Color(0.82f, 0.96f, 1f) * 7.0f);
            Material bladeAuraMaterial = CreatePbrMaterial(
                "AetherbladeResonantSheath",
                new Color(0.06f, 0.30f, 0.72f),
                0.18f,
                0.90f,
                sightHot * 4.5f);
            Material hiltMaterial = CreatePbrMaterial(
                "AetherbladeHilt",
                new Color(0.055f, 0.065f, 0.082f),
                0.88f,
                0.62f,
                new Color(0.02f, 0.08f, 0.14f));
            Material gripMaterial = CreatePbrMaterial(
                "AetherbladeGrip",
                new Color(0.040f, 0.035f, 0.050f),
                0.12f,
                0.32f,
                Color.black);
            Material trailMaterial = CreateTrailMaterial();

            Transform swordRoot = NewChild("SwordRoot", arsenalRoot.transform, new Vector3(0.34f, 0.55f, 0.16f));
            Transform bladeScaleRoot = NewChild("AetherbladeEnergyScale", swordRoot, Vector3.zero);

            // A white-hot narrow core and a slightly wider cyan sheath read like one
            // coherent energy blade under bloom. The empty scale root is what resonance
            // stretches, so core, sheath, tip and trail remain spatially coherent.
            GameObject aura = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            aura.name = "AetherbladeResonantSheath";
            aura.transform.SetParent(bladeScaleRoot, false);
            aura.transform.localPosition = new Vector3(0f, 0f, 1.02f);
            aura.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            aura.transform.localScale = new Vector3(0.095f, 0.78f, 0.095f);
            DisableCollider(aura);
            Renderer auraRenderer = aura.GetComponent<Renderer>();
            if (auraRenderer != null) auraRenderer.sharedMaterial = bladeAuraMaterial;

            GameObject core = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            core.name = "AetherbladeWhiteCore";
            core.transform.SetParent(bladeScaleRoot, false);
            core.transform.localPosition = new Vector3(0f, 0f, 1.02f);
            core.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            core.transform.localScale = new Vector3(0.048f, 0.80f, 0.048f);
            DisableCollider(core);
            Renderer coreRenderer = core.GetComponent<Renderer>();
            if (coreRenderer != null) coreRenderer.sharedMaterial = bladeCoreMaterial;

            GameObject emitter = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            emitter.name = "AetherbladeEmitter";
            emitter.transform.SetParent(swordRoot, false);
            emitter.transform.localPosition = new Vector3(0f, 0f, 0.18f);
            emitter.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            emitter.transform.localScale = new Vector3(0.16f, 0.14f, 0.16f);
            DisableCollider(emitter);
            Renderer emitterRenderer = emitter.GetComponent<Renderer>();
            if (emitterRenderer != null) emitterRenderer.sharedMaterial = hiltMaterial;

            GameObject guardBar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            guardBar.name = "AetherbladeCrossguard";
            guardBar.transform.SetParent(swordRoot, false);
            guardBar.transform.localPosition = new Vector3(0f, 0f, 0.05f);
            guardBar.transform.localScale = new Vector3(0.34f, 0.075f, 0.075f);
            DisableCollider(guardBar);
            Renderer guardRenderer = guardBar.GetComponent<Renderer>();
            if (guardRenderer != null) guardRenderer.sharedMaterial = hiltMaterial;

            GameObject grip = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            grip.name = "AetherbladeGrip";
            grip.transform.SetParent(swordRoot, false);
            grip.transform.localPosition = new Vector3(0f, 0f, -0.20f);
            grip.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            grip.transform.localScale = new Vector3(0.070f, 0.27f, 0.070f);
            DisableCollider(grip);
            Renderer gripRenderer = grip.GetComponent<Renderer>();
            if (gripRenderer != null) gripRenderer.sharedMaterial = gripMaterial;

            GameObject pommel = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            pommel.name = "AetherbladePommel";
            pommel.transform.SetParent(swordRoot, false);
            pommel.transform.localPosition = new Vector3(0f, 0f, -0.53f);
            pommel.transform.localScale = Vector3.one * 0.13f;
            DisableCollider(pommel);
            Renderer pommelRenderer = pommel.GetComponent<Renderer>();
            if (pommelRenderer != null) pommelRenderer.sharedMaterial = bladeAuraMaterial;

            Transform swordTip = NewChild("SwordEnergyTip", bladeScaleRoot, new Vector3(0f, 0f, 1.84f));
            TrailRenderer trail = swordTip.gameObject.AddComponent<TrailRenderer>();
            trail.sharedMaterial = trailMaterial;
            trail.time = 0.16f;
            trail.minVertexDistance = 0.024f;
            trail.emitting = false;
            trail.startColor = sightHot;
            trail.endColor = new Color(sight.r, sight.g, sight.b, 0f);
            Light swordLight = swordTip.gameObject.AddComponent<Light>();
            swordLight.type = LightType.Point;
            swordLight.color = sightHot;
            swordLight.range = 3.0f;
            swordLight.intensity = 0.62f;
            swordLight.shadows = LightShadows.None;

            GuardianSwordShieldRig rig = arsenalRoot.AddComponent<GuardianSwordShieldRig>();
            rig.ConfigureRuntime(
                swordRoot,
                bladeScaleRoot,
                auraRenderer,
                trail,
                swordLight,
                null,
                null,
                null);

            GuardianSwordShieldController physical = guardian.AddComponent<GuardianSwordShieldController>();
            FracturedSignalDirector boss = Object.FindObjectOfType<FracturedSignalDirector>(true);
            Transform target = boss != null ? boss.transform : null;
            FluxMeter flux = guardian.GetComponent<FluxMeter>();
            HitStopController hitStop = Object.FindObjectOfType<HitStopController>(true);
            CombatTuning tuning = FindAsset<CombatTuning>();
            physical.ConfigureRuntime(resonance, flux, target, null, hitStop, tuning);
            physical.SetGuardHeld(false, guardian.transform.forward);

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

            Debug.Log(
                "[Mindforge] Grounded arsenal installed: resonant Aetherblade + endurance dodge roll. " +
                "Pulse fire and physical shield are retired from the normal control surface; sword parry remains blade-authentic.");
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
            Color color = new Color(0.28f, 0.82f, 1f, 0.92f);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            return material;
        }

        private static T FindAsset<T>() where T : Object
        {
            T[] assets = Resources.FindObjectsOfTypeAll<T>();
            return assets != null && assets.Length > 0 ? assets[0] : null;
        }
    }
}
