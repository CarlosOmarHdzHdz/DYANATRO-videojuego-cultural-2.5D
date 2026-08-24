using UnityEditor;
using UnityEngine;

public static class MazahuaMotifVoxelPrefabBuilder
{
    private const string PrefabPath = "Assets/Prefabs/MotivoMazahuaVoxel2_5D.prefab";

    [MenuItem("Xunjuu/Crear Motivo Mazahua Voxel 2.5D")]
    public static void CreatePrefab()
    {
        GameObject root = new GameObject("MotivoMazahuaVoxel2_5D");
        MazahuaMotifVoxel2_5D motif = root.AddComponent<MazahuaMotifVoxel2_5D>();
        motif.Rebuild();

        EnsureFolder("Assets/Prefabs");
        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);

        AssetDatabase.Refresh();
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        Selection.activeObject = prefab;
        EditorGUIUtility.PingObject(prefab);

        Debug.Log("Motivo Mazahua voxel creado en " + PrefabPath);
    }

    [MenuItem("GameObject/Xunjuu/Motivo Mazahua Voxel 2.5D", false, 10)]
    public static void CreateInScene(MenuCommand command)
    {
        GameObject root = new GameObject("MotivoMazahuaVoxel2_5D");
        MazahuaMotifVoxel2_5D motif = root.AddComponent<MazahuaMotifVoxel2_5D>();
        motif.Rebuild();
        GameObjectUtility.SetParentAndAlign(root, command.context as GameObject);
        Undo.RegisterCreatedObjectUndo(root, "Create Motivo Mazahua Voxel 2.5D");
        Selection.activeObject = root;
    }

    private static void EnsureFolder(string path)
    {
        if (!AssetDatabase.IsValidFolder(path))
            AssetDatabase.CreateFolder("Assets", "Prefabs");
    }
}
