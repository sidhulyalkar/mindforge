using UnityEngine;
using Mindforge.Presentation;
using Mindforge.SoulWisp;

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

            Color sight = new Color(0.20f, 0.55f, 1f);
            Color guard = new Color(0.18f, 1f, 0.52f);
            Material swordMaterial = CreateEmissionMaterial("MindforgeRuntimeSword", new Color(0.48f, 0.62f, 0.92f), sight);
            Material shieldMaterial = CreateEmissionMaterial("MindforgeRuntimeShield", new Color(0.24f, 0.42f, 0.34f), guard);
            Material trailMaterial = CreateTrailMaterial();

            Transform swordRoot = NewChild("SwordRoot", arsenalRoot.transform, new Vector3(0.34f, 0.55f, 0.16f));
            GameObject blade = GameObject.CreatePrimitive(PrimitiveType.Cube);
            blade.name = "Aetherblade";
            blade.transform.SetParent(swordRoot, false);
            blade.transform.localPosition = new Vector3(0f, 0f, 0.92f);
            blade.transform.localScale = new Vector3(0.11f, 0.055f, 0.92f);
            DisableCollider(blade);
            Renderer swordRenderer = blade.GetComponent<Renderer>();
            if (swordRenderer != null) swordRenderer.sharedMaterial = swordMaterial;

            GameObject hilt = GameObject.CreatePrimitive(PrimitiveType.Cube);
            hilt.name = "AetherbladeHilt";
            hilt.transform.SetParent(swordRoot, false);
            hilt.transform.localPosition = new Vector3(0f, 0f, -0.04f);
            hilt.transform.localScale = new Vector3(0.38f, 0.09f, 0.08f);
            DisableCollider(hilt);
            Renderer hiltRenderer = hilt.GetComponent<Renderer>();
            if (hiltRenderer != null) hiltRenderer.sharedMaterial = swordMaterial;

            Transform swordTip = NewChild("SwordEnergyTip", swordRoot, new Vector3(0f, 0f, 1.86f));
            TrailRenderer trail = swordTip.gameObject.AddComponent<TrailRenderer>();
            trail.sharedMaterial = trailMaterial;
            trail.time = 0.18f;
            trail.minVertexDistance = 0.035f;
            trail.emitting = false;
            Light swordLight = swordTip.gameObject.AddComponent<Light>();
            swordLight.type = LightType.Point;
            swordLight.range = 2.4f;
            swordLight.intensity = 0.2f;

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
            Transform target = Object.FindObjectOfType<FracturedSignalDirector>(true)?.transform;
            FluxMeter flux = guardian.GetComponent<FluxMeter>();
            HitStopController hitStop = Object.FindObjectOfType<HitStopController>(true);
            CombatTuning tuning = FindAsset<CombatTuning>();
            physical.ConfigureRuntime(resonance, flux, target, shieldHitbox, hitStop, tuning);
            shieldHitbox.Configure(physical, shieldCollider);

            GuardianArmamentPresentationDriver driver = arsenalRoot.AddComponent<GuardianArmamentPresentationDriver>();
            driver.Configure(
                physical,
                rig,
                input,
                loadout,
                guardian.GetComponent<AuraBuffController>(),
                resonance);

            Debug.Log("[Mindforge] Physical arsenal v1 installed: Aetherblade Longsword + Verdant Ward Shield + Warden Weave.");
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

        private static Material CreateEmissionMaterial(string name, Color baseColor, Color emission)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material material = new Material(shader) { name = name };
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", baseColor);
            else if (material.HasProperty("_Color")) material.SetColor("_Color", baseColor);
            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", emission * 1.2f);
            }
            return material;
        }

        private static Material CreateTrailMaterial()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default") ?? Shader.Find("Standard");
            return new Material(shader) { name = "MindforgeRuntimeEnergyTrail" };
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
