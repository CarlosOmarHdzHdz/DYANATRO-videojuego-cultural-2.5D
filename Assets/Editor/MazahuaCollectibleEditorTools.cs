using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class MazahuaCollectibleEditorTools
{
    [MenuItem("Tools/Xunjuu/Generar coleccionables editables")]
    public static void GenerateEditableCollectibles()
    {
        DyanatroGameDirector director = Object.FindFirstObjectByType<DyanatroGameDirector>();
        if (director == null)
        {
            EditorUtility.DisplayDialog(
                "DyanatroGameDirector no encontrado",
                "No se encontro un objeto con DyanatroGameDirector en la escena.",
                "Entendido"
            );
            return;
        }

        Undo.RegisterFullObjectHierarchyUndo(director.gameObject, "Generar coleccionables editables");
        director.ConfigureWordPlacement(45f, 155f);
        director.GenerateEditableMazahuaWordCollectibles();
        EditorSceneManager.MarkSceneDirty(director.gameObject.scene);
        Selection.activeGameObject = GameObject.Find("Palabras_Mazahuas_Dyanatro");
    }

    [MenuItem("Tools/Xunjuu/Actualizar palabras, animales y bosque %&m")]
    public static void UpdateLevelExploration()
    {
        DyanatroGameDirector director = Object.FindFirstObjectByType<DyanatroGameDirector>();
        if (director == null)
        {
            EditorUtility.DisplayDialog("Xunjuu", "No se encontro DyanatroGameDirector en la escena.", "Entendido");
            return;
        }

        Undo.RegisterFullObjectHierarchyUndo(director.gameObject, "Actualizar exploracion Xunjuu");
        director.ConfigureWordPlacement(45f, 155f);
        director.GenerateEditableMazahuaWordCollectibles();
        director.GenerateEditableForest();
        EditorSceneManager.MarkSceneDirty(director.gameObject.scene);
        EditorSceneManager.SaveOpenScenes();
        Selection.activeGameObject = GameObject.Find("Palabras_Mazahuas_Dyanatro");
        Debug.Log("[XUNJUU MAPA PASS] Palabras a 45 unidades, inventario secuencial, animales editables y Zona_Bosque regenerada.");
    }

    [MenuItem("Tools/Xunjuu/Limpiar coleccionables editables")]
    public static void ClearEditableCollectibles()
    {
        GameObject root = GameObject.Find("Palabras_Mazahuas_Dyanatro");
        if (root == null)
        {
            EditorUtility.DisplayDialog(
                "Sin coleccionables",
                "No existe la carpeta Palabras_Mazahuas_Dyanatro en la escena.",
                "Entendido"
            );
            return;
        }

        Undo.DestroyObjectImmediate(root);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }
}
