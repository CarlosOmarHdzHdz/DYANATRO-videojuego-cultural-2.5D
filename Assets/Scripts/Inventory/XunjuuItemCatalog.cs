using System.Collections.Generic;
using UnityEngine;

// Clasifica objetos sin cambiar sus identificadores ni los datos serializados.
public static class XunjuuItemCatalog
{
    public enum ItemKind { MemoryFlower, MedicinalIngredient, Preparation, Material }
    private static readonly Dictionary<string,string> memories=new Dictionary<string,string>();
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset(){memories.Clear();}
    public static void Remember(string id,string word,string meaning)
        => memories[id]="«"+word+"»  /  "+meaning;
    public static ItemKind Kind(string id)
    {
        if(id.StartsWith("palabra_"))return ItemKind.MemoryFlower;
        if(id==XunjuuHerbalCrafting.RemedyId)return ItemKind.Preparation;
        if(XunjuuMedicinalPlantCatalog.Find(id)!=null)return ItemKind.MedicinalIngredient;
        return ItemKind.Material;
    }
    public static string Category(string id)
    {
        switch(Kind(id))
        {
            case ItemKind.MemoryFlower:return "FLOR DE MEMORIA";
            case ItemKind.MedicinalIngredient:return "INGREDIENTE DEL HUERTO";
            case ItemKind.Preparation:return "PREPARADO / CONSUMIBLE";
            default:return "RECURSO DEL RECORRIDO";
        }
    }
    public static string Description(string id,string fallbackName)
    {
        if(Kind(id)==ItemKind.MemoryFlower)
            return (memories.TryGetValue(id,out var text)?text:fallbackName)
                +"\nObjeto de misión: conserva una palabra. No se consume al preparar medicinas. Su flor es un símbolo fantástico, no una especie botánica.";
        var plant=XunjuuMedicinalPlantCatalog.Find(id);
        if(plant!=null)
            return plant.ScientificName+"\n"+(id=="manzanilla"?"Usa 2 unidades con 1 hierbabuena para la infusión del juego.":
                id=="hierbabuena"?"Usa 1 unidad con 2 manzanillas para la infusión del juego.":"Ejemplar del huerto y registro educativo; no forma parte de la receta actual.");
        if(id==XunjuuHerbalCrafting.RemedyId)
            return "H / Usar cuando falte salud. Hasta +40 puntos en 5 segundos. No se consume con vida completa ni permite revivir. Efecto ficticio del videojuego.";
        return "Recurso obtenido durante la aventura. Las flores de memoria y los ingredientes medicinales se guardan por separado.";
    }
}
