using UnityEngine;
using UnityEditor;

/// <summary>
/// Visual effects customization for fire instances
/// </summary>
public class FireVisualEffectsEditor : EditorWindow
{
    private FireInstance targetFire;
    private ParticleSystem fireParticles;
    private ParticleSystem smokeParticles;
    private Light fireLight;

    // Presets
    private int selectedPreset = 0;
    private string[] presetNames = { "Campfire", "Torch", "Bonfire", "Forge", "Wildfire" };

    [MenuItem("Tools/Wild Survival/Fire System/Visual Effects Editor")]
    public static void ShowWindow()
    {
        var window = GetWindow<FireVisualEffectsEditor>("🎨 Fire VFX");
        window.minSize = new Vector2(400, 600);
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Fire Visual Effects Editor", EditorStyles.boldLabel);
        EditorGUILayout.Space(10);

        targetFire = (FireInstance)EditorGUILayout.ObjectField(
            "Target Fire", targetFire, typeof(FireInstance), true);

        if (targetFire == null)
        {
            EditorGUILayout.HelpBox("Select a FireInstance to edit visual effects", MessageType.Info);
            return;
        }

        GetComponents();

        EditorGUILayout.Space(10);

        DrawPresets();
        EditorGUILayout.Space(10);
        DrawParticleSettings();
        EditorGUILayout.Space(10);
        DrawLightSettings();
    }

    private void GetComponents()
    {
        if (targetFire != null)
        {
            fireParticles = targetFire.GetComponentInChildren<ParticleSystem>();
            var allParticles = targetFire.GetComponentsInChildren<ParticleSystem>();

            foreach (var ps in allParticles)
            {
                if (ps.name.Contains("Smoke"))
                    smokeParticles = ps;
                else if (ps.name.Contains("Fire") && fireParticles == null)
                    fireParticles = ps;
            }

            fireLight = targetFire.GetComponentInChildren<Light>();
        }
    }

    private void DrawPresets()
    {
        EditorGUILayout.BeginVertical(GUI.skin.box);
        EditorGUILayout.LabelField("Effect Presets", EditorStyles.boldLabel);

        selectedPreset = GUILayout.SelectionGrid(selectedPreset, presetNames, 3);

        if (GUILayout.Button("Apply Preset"))
        {
            ApplyPreset(selectedPreset);
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawParticleSettings()
    {
        EditorGUILayout.BeginVertical(GUI.skin.box);
        EditorGUILayout.LabelField("Particle Settings", EditorStyles.boldLabel);

        if (fireParticles != null)
        {
            var main = fireParticles.main;

            EditorGUILayout.LabelField("Fire Particles", EditorStyles.boldLabel);

            main.startLifetime = EditorGUILayout.Slider("Lifetime", main.startLifetime.constant, 0.5f, 5f);
            main.startSpeed = EditorGUILayout.Slider("Speed", main.startSpeed.constant, 0.5f, 10f);
            main.startSize = EditorGUILayout.Slider("Size", main.startSize.constant, 0.1f, 2f);
            main.maxParticles = EditorGUILayout.IntSlider("Max Particles", main.maxParticles, 10, 200);

            var emission = fireParticles.emission;
            emission.rateOverTime = EditorGUILayout.Slider("Emission Rate", emission.rateOverTime.constant, 5f, 100f);
        }

        if (smokeParticles != null)
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Smoke Particles", EditorStyles.boldLabel);

            var main = smokeParticles.main;
            main.startLifetime = EditorGUILayout.Slider("Lifetime", main.startLifetime.constant, 2f, 10f);
            main.startSize = EditorGUILayout.Slider("Size", main.startSize.constant, 0.5f, 5f);

            var emission = smokeParticles.emission;
            emission.rateOverTime = EditorGUILayout.Slider("Emission Rate", emission.rateOverTime.constant, 1f, 20f);
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawLightSettings()
    {
        EditorGUILayout.BeginVertical(GUI.skin.box);
        EditorGUILayout.LabelField("Light Settings", EditorStyles.boldLabel);

        if (fireLight != null)
        {
            fireLight.intensity = EditorGUILayout.Slider("Intensity", fireLight.intensity, 0f, 10f);
            fireLight.range = EditorGUILayout.Slider("Range", fireLight.range, 1f, 20f);
            fireLight.color = EditorGUILayout.ColorField("Color", fireLight.color);

            EditorGUILayout.Space(10);

            if (GUILayout.Button("Add Flicker Script"))
            {
                if (fireLight.GetComponent<FireLightFlicker>() == null)
                {
                    fireLight.gameObject.AddComponent<FireLightFlicker>();
                }
            }
        }

        EditorGUILayout.EndVertical();
    }

    private void ApplyPreset(int preset)
    {
        switch (preset)
        {
            case 0: // Campfire
                ApplyCampfirePreset();
                break;
            case 1: // Torch
                ApplyTorchPreset();
                break;
            case 2: // Bonfire
                ApplyBonfirePreset();
                break;
            case 3: // Forge
                ApplyForgePreset();
                break;
            case 4: // Wildfire
                ApplyWildfirePreset();
                break;
        }

        EditorUtility.SetDirty(targetFire);
    }

    private void ApplyCampfirePreset()
    {
        if (fireParticles != null)
        {
            var main = fireParticles.main;
            main.startLifetime = 1.5f;
            main.startSpeed = 2f;
            main.startSize = 0.5f;
            main.maxParticles = 50;

            var emission = fireParticles.emission;
            emission.rateOverTime = 30f;
        }

        if (fireLight != null)
        {
            fireLight.intensity = 3f;
            fireLight.range = 8f;
            fireLight.color = new Color(1f, 0.5f, 0.2f);
        }
    }

    private void ApplyTorchPreset()
    {
        if (fireParticles != null)
        {
            var main = fireParticles.main;
            main.startLifetime = 0.8f;
            main.startSpeed = 1f;
            main.startSize = 0.3f;
            main.maxParticles = 20;

            var emission = fireParticles.emission;
            emission.rateOverTime = 15f;
        }

        if (fireLight != null)
        {
            fireLight.intensity = 2f;
            fireLight.range = 5f;
            fireLight.color = new Color(1f, 0.7f, 0.3f);
        }
    }

    private void ApplyBonfirePreset()
    {
        if (fireParticles != null)
        {
            var main = fireParticles.main;
            main.startLifetime = 2f;
            main.startSpeed = 3f;
            main.startSize = 1f;
            main.maxParticles = 100;

            var emission = fireParticles.emission;
            emission.rateOverTime = 50f;
        }

        if (fireLight != null)
        {
            fireLight.intensity = 5f;
            fireLight.range = 15f;
            fireLight.color = new Color(1f, 0.4f, 0.1f);
        }
    }

    private void ApplyForgePreset()
    {
        if (fireParticles != null)
        {
            var main = fireParticles.main;
            main.startLifetime = 1f;
            main.startSpeed = 1.5f;
            main.startSize = 0.4f;
            main.startColor = new Color(0.5f, 0.7f, 1f); // Blue flame
            main.maxParticles = 40;

            var emission = fireParticles.emission;
            emission.rateOverTime = 25f;
        }

        if (fireLight != null)
        {
            fireLight.intensity = 4f;
            fireLight.range = 6f;
            fireLight.color = new Color(0.7f, 0.8f, 1f);
        }
    }

    private void ApplyWildfirePreset()
    {
        if (fireParticles != null)
        {
            var main = fireParticles.main;
            main.startLifetime = 3f;
            main.startSpeed = 5f;
            main.startSize = 1.5f;
            main.maxParticles = 150;

            var emission = fireParticles.emission;
            emission.rateOverTime = 80f;
        }

        if (fireLight != null)
        {
            fireLight.intensity = 8f;
            fireLight.range = 20f;
            fireLight.color = new Color(1f, 0.3f, 0f);
        }
    }
}