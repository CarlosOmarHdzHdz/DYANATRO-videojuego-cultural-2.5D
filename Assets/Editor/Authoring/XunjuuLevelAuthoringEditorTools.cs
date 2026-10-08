using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// ============================================================================
// Xunjuú v0.1 - Herramientas de autoría del nivel
// Acción: convertir contenido procedural en objetos editables de Hierarchy.
// ============================================================================
public static class XunjuuLevelAuthoringEditorTools
{
    [MenuItem("Tools/Xunjuu/Preparar Nivel 1 editable v0.1", priority = 1)]
    public static void PrepareLevelOne()
    {
        DyanatroGameDirector director = Object.FindFirstObjectByType<DyanatroGameDirector>();
        if (director == null)
        {
            EditorUtility.DisplayDialog("Xunjuú v0.1", "No se encontró DyanatroGameDirector en la escena.", "Entendido");
            return;
        }

        Undo.RegisterFullObjectHierarchyUndo(director.gameObject, "Preparar Nivel 1 editable Xunjuú");
        DisablePreviousForestGenerators();
        ApplyDirectorPerformanceDefaults(director);
        EnsurePlayerSystems();
        director.GenerateEditableMazahuaWordCollectibles();
        director.GenerateEditableForest();
        director.GenerateEditableCornFields();
        EnsureImportantObjectLabels();
        EnsureOptimizer(director.gameObject);
        OptimizeTerrains();
        DisableExcessGeneratedObjects();

        EditorUtility.SetDirty(director);
        EditorSceneManager.MarkSceneDirty(director.gameObject.scene);
        Selection.activeGameObject = director.gameObject;
        EditorUtility.DisplayDialog(
            "Xunjuú v0.1",
            "Nivel preparado. Coleccionables, árboles y milpas ya aparecen como objetos editables en Hierarchy. Revisa y guarda la escena.",
            "Listo");
    }

    // ACCIÓN: aplicar límites de rendimiento sin regenerar ni borrar contenido.
    [MenuItem("Tools/Xunjuu/Aplicar optimización del Nivel 1 v0.1", priority = 2)]
    public static void ApplyLevelOneOptimization()
    {
        DyanatroGameDirector director = Object.FindFirstObjectByType<DyanatroGameDirector>();
        if (director == null)
            return;

        DisablePreviousForestGenerators();
        ApplyDirectorPerformanceDefaults(director);
        EnsureOptimizer(director.gameObject);
        OptimizeTerrains();
        DisableExcessGeneratedObjects();
        EditorSceneManager.MarkSceneDirty(director.gameObject.scene);
        Selection.activeGameObject = director.gameObject;
    }

    // ACCIÓN: convertir el objeto seleccionado en un árbol talable.
    [MenuItem("GameObject/Xunjuú v0.1/Convertir en árbol interactivo", false, 20)]
    public static void ConvertSelectionToTree()
    {
        GameObject selected = Selection.activeGameObject;
        if (selected == null)
            return;

        Undo.AddComponent<TreeInteractivo>(selected);
        XunjuuWorldLabel label = selected.GetComponent<XunjuuWorldLabel>();
        if (label == null)
            label = Undo.AddComponent<XunjuuWorldLabel>(selected);
        label.Configure("Árbol", false, new Vector3(0f, 3.2f, 0f));
        EditorSceneManager.MarkSceneDirty(selected.scene);
    }

    [MenuItem("GameObject/Xunjuú v0.1/Convertir en árbol interactivo", true)]
    private static bool ValidateConvertSelectionToTree()
    {
        return Selection.activeGameObject != null && Selection.activeGameObject.GetComponent<TreeInteractivo>() == null;
    }

    // ACCIÓN: convertir el objeto seleccionado en un cultivo cosechable.
    [MenuItem("GameObject/Xunjuú v0.1/Convertir en cultivo interactivo", false, 21)]
    public static void ConvertSelectionToCrop()
    {
        GameObject selected = Selection.activeGameObject;
        if (selected == null)
            return;

        Undo.AddComponent<XunjuuCrop>(selected);
        XunjuuWorldLabel label = selected.GetComponent<XunjuuWorldLabel>();
        if (label == null)
            label = Undo.AddComponent<XunjuuWorldLabel>(selected);
        label.Configure("Cultivo", true, new Vector3(0f, 1.7f, 0f));
        EditorSceneManager.MarkSceneDirty(selected.scene);
    }

    [MenuItem("GameObject/Xunjuú v0.1/Convertir en cultivo interactivo", true)]
    private static bool ValidateConvertSelectionToCrop()
    {
        return Selection.activeGameObject != null && Selection.activeGameObject.GetComponent<XunjuuCrop>() == null;
    }

    private static void DisablePreviousForestGenerators()
    {
        ForestOptimized[] generators = Object.FindObjectsByType<ForestOptimized>(FindObjectsSortMode.None);
        foreach (ForestOptimized generator in generators)
        {
            if (generator == null)
                continue;
            Undo.RecordObject(generator, "Desactivar generador anterior");
            generator.enabled = false;
            EditorUtility.SetDirty(generator);
        }
    }

    private static void EnsurePlayerSystems()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
            return;

        if (player.GetComponent<XunjuuInventory>() == null)
            Undo.AddComponent<XunjuuInventory>(player);
        if (player.GetComponent<XunjuuInventoryUI>() == null)
            Undo.AddComponent<XunjuuInventoryUI>(player);
        AddOrUpdateLabel(player, "Protagonista", true, new Vector3(0f, 1.35f, 0f));
    }

    private static void EnsureImportantObjectLabels()
    {
        foreach (MazahuaWordCollectible collectible in Object.FindObjectsByType<MazahuaWordCollectible>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            AddOrUpdateLabel(collectible.gameObject, collectible.MazahuaWord + "\n" + collectible.SpanishMeaning, true, new Vector3(0f, 1.28f, 0f));

        foreach (TreeInteractivo tree in Object.FindObjectsByType<TreeInteractivo>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            AddOrUpdateLabel(tree.gameObject, "Árbol", false, new Vector3(0f, 3.2f, 0f));

        foreach (XunjuuCropField field in Object.FindObjectsByType<XunjuuCropField>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            AddOrUpdateLabel(field.gameObject, field.gameObject.name.Replace('_', ' '), true, new Vector3(0f, 2.8f, 0f));

        foreach (GameObject enemy in GameObject.FindGameObjectsWithTag("Enemy"))
            AddOrUpdateLabel(enemy, enemy.name.Replace('_', ' '), true, new Vector3(0f, 1.8f, 0f));

        foreach (Animal animal in Object.FindObjectsByType<Animal>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            AddOrUpdateLabel(animal.gameObject, animal.gameObject.name.Replace('_', ' '), true, new Vector3(0f, 1.2f, 0f));
    }

    private static void AddOrUpdateLabel(GameObject target, string displayName, bool visible, Vector3 offset)
    {
        XunjuuWorldLabel label = target.GetComponent<XunjuuWorldLabel>();
        if (label == null)
            label = Undo.AddComponent<XunjuuWorldLabel>(target);
        Undo.RecordObject(label, "Configurar nombre visible");
        label.Configure(displayName, visible, offset);
        EditorUtility.SetDirty(label);
    }

    private static void EnsureOptimizer(GameObject directorObject)
    {
        if (directorObject.GetComponent<XunjuuLevelOptimizer>() == null)
            Undo.AddComponent<XunjuuLevelOptimizer>(directorObject);
    }

    private static void ApplyDirectorPerformanceDefaults(DyanatroGameDirector director)
    {
        SerializedObject serializedDirector = new SerializedObject(director);
        serializedDirector.FindProperty("treeCount").intValue = 180;
        serializedDirector.FindProperty("bushCount").intValue = 260;
        serializedDirector.FindProperty("cloudCount").intValue = 26;
        serializedDirector.FindProperty("tallGrassPatchCount").intValue = 180;
        serializedDirector.FindProperty("cornFieldCount").intValue = 4;
        serializedDirector.ApplyModifiedProperties();
        EditorUtility.SetDirty(director);
    }

    private static void OptimizeTerrains()
    {
        foreach (Terrain terrain in Object.FindObjectsByType<Terrain>(FindObjectsSortMode.None))
        {
            Undo.RecordObject(terrain, "Optimizar Terrain Xunjuú");
            terrain.drawInstanced = true;
            terrain.detailObjectDistance = Mathf.Min(terrain.detailObjectDistance, 60f);
            terrain.detailObjectDensity = Mathf.Min(terrain.detailObjectDensity, 0.7f);
            terrain.heightmapPixelError = Mathf.Max(terrain.heightmapPixelError, 8f);
            terrain.basemapDistance = Mathf.Min(terrain.basemapDistance, 650f);
            EditorUtility.SetDirty(terrain);
        }
    }

    private static void DisableExcessGeneratedObjects()
    {
        SetChildrenActiveLimit(GameObject.Find("Bosque_Dyanatro_Generado"), 180);
        SetChildrenActiveLimit(GameObject.Find("Milpas_Maiz_Dyanatro"), 4);
    }

    private static void SetChildrenActiveLimit(GameObject root, int activeLimit)
    {
        if (root == null)
            return;

        for (int i = 0; i < root.transform.childCount; i++)
        {
            GameObject child = root.transform.GetChild(i).gameObject;
            bool shouldBeActive = i < activeLimit;
            if (child.activeSelf == shouldBeActive)
                continue;
            Undo.RecordObject(child, "Optimizar objetos generados Xunjuú");
            child.SetActive(shouldBeActive);
            EditorUtility.SetDirty(child);
        }
    }
}
