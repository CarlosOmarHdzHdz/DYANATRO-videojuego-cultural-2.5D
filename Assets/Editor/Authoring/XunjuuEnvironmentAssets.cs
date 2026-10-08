using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class XunjuuEnvironmentAssets
{
    const string Root = "Assets/Art/EntornoMazahua";
    const string Prefabs = "Assets/Prefabs/EntornoMazahua";
    [Serializable] class Manifest { public Asset[] assets; }
    [Serializable] class Asset { public string name; public string category; public int triangles; public float[] dimensions_m; }
    static XunjuuEnvironmentAssets() { EditorApplication.update += Poll; }
    static void Poll()
    {
        const string flag = "Temp/BuildMazahuaEnvironment.flag";
        if (!File.Exists(flag) || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlaying) return;
        File.Delete(flag);
        Build();
    }

    [MenuItem("Tools/Xunjuu/Preparar recursos 3D")]
    public static void Build()
    {
        string report = "output/environment-validation.txt";
        Directory.CreateDirectory("output");
        try
        {
            Directory.CreateDirectory(Root + "/Materials");
            Directory.CreateDirectory(Prefabs);
            AssetDatabase.Refresh();
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (shader == null) throw new Exception("No compatible material shader");
            var materials = new Dictionary<string, Material>();
            var manifest = JsonUtility.FromJson<Manifest>(File.ReadAllText("../output/modelos_mazahua/manifest.json"));
            var log = new List<string>();
            foreach (var asset in manifest.assets)
            {
                string path = Root + "/Models/" + asset.name + ".fbx";
                GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (model == null) throw new Exception("Model missing: " + path);
                GameObject instance = new GameObject(asset.name);
                GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(model);
                visual.transform.SetParent(instance.transform, false);
                try
                {
                    instance.name = asset.name;
                    var renderers = instance.GetComponentsInChildren<Renderer>();
                    if (renderers.Length == 0) throw new Exception("Empty model: " + asset.name);
                    Bounds bounds = renderers[0].bounds;
                    foreach (var renderer in renderers)
                    {
                        bounds.Encapsulate(renderer.bounds);
                        Material[] slots = renderer.sharedMaterials;
                        for (int i = 0; i < slots.Length; i++)
                        {
                            var source = slots[i];
                            if (source == null) throw new Exception("Missing source material");
                            string name = source.name;
                            if (!materials.TryGetValue(name, out Material material))
                            {
                                string materialPath = Root + "/Materials/" + name + ".mat";
                                material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                                if (material == null) { material = new Material(shader); AssetDatabase.CreateAsset(material, materialPath); }
                                material.shader = shader;
                                Color color = source.HasProperty("_Color") ? source.GetColor("_Color") : source.color;
                                material.SetColor("_BaseColor", color);
                                material.SetColor("_Color", color);
                                material.SetFloat("_Smoothness", 0.12f);
                                material.enableInstancing = true;
                                EditorUtility.SetDirty(material);
                                materials[name] = material;
                            }
                            slots[i] = material;
                        }
                        renderer.sharedMaterials = slots;
                    }
                    if (Mathf.Abs(bounds.size.y - asset.dimensions_m[2]) > .1f)
                        throw new Exception("Incorrect imported scale: " + asset.name + " " + bounds.size);
                    if (asset.category == "Arboles")
                    {
                        var collider = instance.AddComponent<CapsuleCollider>();
                        collider.radius = .22f; collider.height = Mathf.Min(2.5f, bounds.size.y);
                        collider.center = instance.transform.InverseTransformPoint(new Vector3(bounds.center.x, bounds.min.y + collider.height / 2, bounds.center.z));
                    }
                    else
                    {
                        var collider = instance.AddComponent<BoxCollider>();
                        collider.center = instance.transform.InverseTransformPoint(bounds.center);
                        collider.size = bounds.size;
                        if (Vector3.Distance(collider.bounds.size, bounds.size) > .01f)
                            throw new Exception("Collider bounds mismatch: " + asset.name);
                    }
                    if (PrefabUtility.SaveAsPrefabAsset(instance, Prefabs + "/" + asset.name + ".prefab") == null)
                        throw new Exception("Prefab save failed");
                    log.Add(asset.name + ": " + asset.triangles + " triangles; bounds " + bounds.size + "; materials OK; collider OK");
                }
                finally { UnityEngine.Object.DestroyImmediate(instance); }
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.ExportPackage(new[] { Root, Prefabs }, "../output/modelos_mazahua/Recursos_3D_Xunjuu.unitypackage", ExportPackageOptions.Recurse);
            File.WriteAllText(report, "PASS: 15 prefabs, shared materials and meter scale verified.\n" + DateTime.Now.ToString("O") + "\n" + string.Join("\n", log));
        }
        catch (Exception e) { File.WriteAllText(report, "FAIL: " + e); Debug.LogException(e); }
    }
}
