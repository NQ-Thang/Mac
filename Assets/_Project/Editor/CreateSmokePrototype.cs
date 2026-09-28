using System.IO;
using UnityEngine;
using UnityEditor;

[InitializeOnLoad]
public static class CreateSmokePrototype
{
    static CreateSmokePrototype()
    {
        EditorApplication.delayCall += GeneratePrototype;
    }

    [MenuItem("Tools/Generate Smoke Prototype")]
    public static void GeneratePrototype()
    {
        // 1. Create soft circular smoke texture
        string graphicsDir = "Assets/_Project/Graphics";
        if (!Directory.Exists(graphicsDir)) Directory.CreateDirectory(graphicsDir);

        string texturePath = Path.Combine(graphicsDir, "smoke_particle.png");
        if (!File.Exists(texturePath))
        {
            int size = 128;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Vector2 center = new Vector2(size / 2f, size / 2f);
            float radius = size / 2f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    if (dist >= radius)
                    {
                        tex.SetPixel(x, y, new Color(1f, 1f, 1f, 0f));
                    }
                    else
                    {
                        float t = 1f - (dist / radius);
                        // Smoothstep curve for soft cloud puff
                        float alpha = t * t * (3f - 2f * t);
                        tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                    }
                }
            }

            tex.Apply();
            byte[] bytes = tex.EncodeToPNG();
            File.WriteAllBytes(texturePath, bytes);
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(texturePath, ImportAssetOptions.ForceUpdate);

            TextureImporter importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
            }
        }

        // 2. Create Smoke Material
        string materialPath = Path.Combine(graphicsDir, "M_Smoke_Prototype.mat");
        Material smokeMat = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (smokeMat == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit") ??
                           Shader.Find("Universal Render Pipeline/Particles/Unlit") ??
                           Shader.Find("Sprites/Default");
            if (shader != null)
            {
                smokeMat = new Material(shader);
                Texture2D smokeTex = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
                if (smokeTex != null)
                {
                    smokeMat.mainTexture = smokeTex;
                }
                AssetDatabase.CreateAsset(smokeMat, materialPath);
            }
        }

        // 3. Create Standalone Prototype Prefab
        string prefabsDir = "Assets/_Project/Prefabs";
        if (!Directory.Exists(prefabsDir)) Directory.CreateDirectory(prefabsDir);

        string prefabPath = Path.Combine(prefabsDir, "Smoke_Prototype.prefab");

        GameObject smokeGo = new GameObject("Smoke_Prototype");
        ParticleSystem ps = smokeGo.AddComponent<ParticleSystem>();
        ParticleSystemRenderer psr = smokeGo.GetComponent<ParticleSystemRenderer>();

        // Configure Particle System
        // Main Module
        var main = ps.main;
        main.loop = true;
        main.playOnAwake = true;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.95f, 1.25f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.60f, 0.95f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.55f, 0.75f);
        main.startColor = new Color(0.03f, 0.03f, 0.04f, 1.0f);
        main.startRotation = new ParticleSystem.MinMaxCurve(-Mathf.PI, Mathf.PI);
        main.gravityModifier = -0.05f; // Slight upward buoyancy
        main.maxParticles = 140;

        // Emission Module (high density at base and lower body)
        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = 70f;

        // Shape Module (base emitter sized to Mặc's body width)
        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.position = new Vector3(0f, 0.06f, 0f);
        shape.scale = new Vector3(0.55f, 0.14f, 0.05f);

        // Size Over Lifetime (billows and expands to cover torso, softens at top)
        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 0.85f);
        sizeCurve.AddKey(0.45f, 1.35f);
        sizeCurve.AddKey(0.80f, 1.05f);
        sizeCurve.AddKey(1.0f, 0.70f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        // Color Over Lifetime: Extra thick & dense at feet and mid body, gradually fades upward, virtually zero at head
        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(0.03f, 0.03f, 0.04f), 0.0f),
                new GradientColorKey(new Color(0.04f, 0.04f, 0.05f), 1.0f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0.0f, 0.0f),
                new GradientAlphaKey(1.0f, 0.06f),  // Chân & thân dưới: cực kỳ dày và đậm
                new GradientAlphaKey(0.95f, 0.45f), // Thân giữa: rất dày
                new GradientAlphaKey(0.45f, 0.72f), // Thân trên: giảm dần
                new GradientAlphaKey(0.12f, 0.90f), // Gần đầu: rất ít
                new GradientAlphaKey(0.0f, 1.0f)    // Trên đầu: tan biến hoàn toàn
            }
        );
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(grad);

        // Rotation Over Lifetime (gentle natural swirling maintaining organic silhouette)
        var rotationOverLifetime = ps.rotationOverLifetime;
        rotationOverLifetime.enabled = true;
        rotationOverLifetime.z = new ParticleSystem.MinMaxCurve(-25f * Mathf.Deg2Rad, 25f * Mathf.Deg2Rad);

        // Noise Module (organic curling motion preserving distorted silhouette)
        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.10f;
        noise.frequency = 0.50f;
        noise.scrollSpeed = 0.35f;
        noise.damping = true;

        // Renderer Module
        psr.renderMode = ParticleSystemRenderMode.Billboard;
        psr.sortingLayerName = "Default";
        psr.sortingOrder = 1;
        if (smokeMat != null)
        {
            psr.sharedMaterial = smokeMat;
        }

        PrefabUtility.SaveAsPrefabAsset(smokeGo, prefabPath);
        Object.DestroyImmediate(smokeGo);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Smoke_Prototype.prefab successfully created at: " + prefabPath);
    }
}
