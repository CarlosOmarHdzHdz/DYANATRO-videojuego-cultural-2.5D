using UnityEditor;
using UnityEngine;

// ============================================================================
// Xunjuu v0.1 - Constructor de recompensa del nivel 2
// Accion: crear un prefab editable que entrega el macuahuitl por mision.
// ============================================================================
public static class XunjuuLevel2RewardBuilder
{
    private const string WeaponPath = "Assets/Prefabs/SwordFloating.prefab";
    private const string RewardPath = "Assets/Prefabs/MisionSecundaria_Macuahuitl.prefab";

    [MenuItem("Xunjuu v0.1/Nivel 2/Crear recompensa Macuahuitl")]
    public static void CreateRewardPrefab()
    {
        GameObject weaponPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(WeaponPath);
        if (weaponPrefab == null)
        {
            Debug.LogError("Xunjuu v0.1: falta SwordFloating.prefab.");
            return;
        }

        GameObject root = new GameObject("MisionSecundaria_Macuahuitl");
        try
        {
            Transform rewardPoint = new GameObject("Punto_Recompensa").transform;
            rewardPoint.SetParent(root.transform, false);
            rewardPoint.localPosition = Vector3.up;

            XunjuuSecondaryMissionWeaponReward reward = root.AddComponent<XunjuuSecondaryMissionWeaponReward>();
            SerializedObject serializedReward = new SerializedObject(reward);
            serializedReward.FindProperty("weaponPrefab").objectReferenceValue = weaponPrefab;
            serializedReward.FindProperty("rewardPoint").objectReferenceValue = rewardPoint;
            serializedReward.FindProperty("equipImmediately").boolValue = true;
            serializedReward.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, RewardPath);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(RewardPath);
        EditorGUIUtility.PingObject(Selection.activeObject);
        Debug.Log("[XUNJUU NIVEL2 PASS] Recompensa Macuahuitl creada y editable.");
    }
}
