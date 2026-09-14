using UnityEngine;

[CreateAssetMenu(menuName = "Xunjuu/Environment catalog")]
public sealed class XunjuuEnvironmentCatalog : ScriptableObject
{
    public GameObject[] trees;
    public GameObject[] rocks;
    public GameObject[] houses;
    public GameObject flower;
    public GameObject corn;
    public Material foliageMaterial;
    public Material groundMaterial;
}
