using System;
using System.Collections.Generic;
using UnityEngine;

// Catalogo unico para fauna jugable, capturas y fichas de la Ludoteca.
// Los nombres mazahuas se dejan fuera hasta contar con revision linguistica.
public static class XunjuuFaunaCatalog
{
    public sealed class Entry
    {
        public readonly string Id;
        public readonly string DisplayName;
        public readonly string ScientificName;
        public readonly string Habitat;
        public readonly string Fact;
        public readonly int AtlasIndex;
        public readonly float WorldScale;
        public readonly Vector3 ColliderCenter;
        public readonly Vector3 ColliderSize;
        public readonly Animal.AnimalSpecies Species;

        public Entry(
            string id,
            string displayName,
            string scientificName,
            string habitat,
            string fact,
            int atlasIndex,
            float worldScale,
            Vector3 colliderCenter,
            Vector3 colliderSize,
            Animal.AnimalSpecies species)
        {
            Id = id;
            DisplayName = displayName;
            ScientificName = scientificName;
            Habitat = habitat;
            Fact = fact;
            AtlasIndex = atlasIndex;
            WorldScale = worldScale;
            ColliderCenter = colliderCenter;
            ColliderSize = colliderSize;
            Species = species;
        }
    }

    private const string AtlasResource = "Sprites/Animals/FaunaMazahua_3x2";
    private const string SavePrefix = "xunjuu.fauna.capturada.";
    private static readonly Dictionary<string, Sprite> SpriteCache = new Dictionary<string, Sprite>();

    private static readonly Entry[] entries =
    {
        new Entry("venado_cola_blanca", "Venado cola blanca", "Odocoileus virginianus", "Bosque templado y claros", "Al levantar la cola muestra una señal blanca de alerta.", 0, .72f, new Vector3(0f, 1.55f, 0f), new Vector3(3.25f, 3.1f, .72f), Animal.AnimalSpecies.Deer),
        new Entry("conejo_serrano", "Conejo serrano", "Sylvilagus floridanus", "Pastizal y borde de bosque", "Busca refugio entre hierbas y vegetación baja.", 1, .56f, new Vector3(0f, .82f, 0f), new Vector3(2.05f, 1.65f, .68f), Animal.AnimalSpecies.Rabbit),
        new Entry("coyote", "Coyote", "Canis latrans", "Bosque abierto y pastizal", "Recorre grandes distancias y suele mantenerse atento al entorno.", 2, .68f, new Vector3(0f, 1.05f, 0f), new Vector3(3.25f, 2.1f, .72f), Animal.AnimalSpecies.Coyote),
        new Entry("zorra_gris", "Zorra gris", "Urocyon cinereoargenteus", "Bosque y matorral", "Es una de las pocas especies de cánidos capaces de trepar árboles.", 3, .64f, new Vector3(0f, .92f, 0f), new Vector3(3.0f, 1.85f, .68f), Animal.AnimalSpecies.Fox),
        new Entry("tlacuache", "Tlacuache", "Didelphis virginiana", "Bosque, matorral y parcelas", "Es un marsupial nocturno; sus crías crecen dentro del marsupio.", 4, .55f, new Vector3(0f, .65f, 0f), new Vector3(2.6f, 1.3f, .64f), Animal.AnimalSpecies.Opossum),
        new Entry("ardilla_gris", "Ardilla gris", "Sciurus aureogaster", "Bosque templado", "Al esconder alimento también contribuye a dispersar semillas.", 5, .53f, new Vector3(0f, .88f, 0f), new Vector3(1.9f, 1.75f, .62f), Animal.AnimalSpecies.Squirrel)
    };

    public static IReadOnlyList<Entry> Entries => entries;
    public static string ExpandedInformation(Entry entry)
    {
        if(entry==null)return string.Empty;
        string food, activity, role;
        switch(entry.Id)
        {
            case "venado_cola_blanca":
                food="Herbívoro: hojas, brotes, ramas tiernas y frutos, según la estación.";
                activity="Suele alimentarse al amanecer y al atardecer. Detecta peligros con el olfato y el oído y escapa corriendo y saltando.";
                role="Forma parte de las redes alimentarias del bosque y consume vegetación.";break;
            case "conejo_serrano":
                food="Herbívoro: pastos, hierbas y otras partes de plantas; también brotes y corteza cuando escasea el alimento.";
                activity="Busca cobertura entre hierbas y matorrales. Ante un peligro puede quedarse quieto y después huir a saltos.";
                role="Es alimento de distintos depredadores y participa en la red alimentaria del pastizal.";break;
            case "coyote":
                food="Dieta variada: pequeños mamíferos, insectos, carroña y frutos disponibles.";
                activity="Puede recorrer bosques, pastizales y zonas agrícolas. Utiliza el olfato y el oído para explorar; también se comunica mediante aullidos.";
                role="Es depredador y carroñero; participa en el control de poblaciones de pequeños animales.";break;
            case "zorra_gris":
                food="Omnívora: pequeños mamíferos, insectos y frutos; su dieta cambia con la estación.";
                activity="Busca cobertura en bosques y matorrales. Puede trepar árboles para refugiarse.";
                role="Consume pequeños animales y frutos y forma parte de las redes alimentarias del bosque.";break;
            case "tlacuache":
                food="Omnívoro oportunista: frutos, insectos, otros pequeños animales y carroña.";
                activity="Principalmente nocturno. Usa refugios y vegetación; es un marsupial, y sus crías pasan parte del desarrollo en el marsupio.";
                role="Aprovecha muchos recursos alimentarios y participa en el consumo de restos orgánicos.";break;
            default:
                food="Semillas y frutos; en bosques mexicanos consume semillas de pinos y bellotas de encinos.";
                activity="Se desplaza y busca alimento entre los árboles. Su cola ayuda al equilibrio al trepar y saltar.";
                role="El transporte y almacenamiento de semillas puede favorecer su dispersión.";break;
        }
        return "<b>ZONAS Y HÁBITAT</b>\n"+entry.Habitat+".\nAmbientes representados: bosques templados y sus bordes en el centro de México. La presencia depende de la localidad.\n\n"
            +"<b>ALIMENTACIÓN</b>\n"+food+"\n\n<b>HÁBITOS Y COMPORTAMIENTO</b>\n"+activity
            +"\n\n<b>PAPEL EN EL ECOSISTEMA</b>\n"+role+"\n\n<b>¿SABÍAS QUE…?</b>\n"+entry.Fact
            +"\n\n<b>OBSERVACIÓN RESPONSABLE</b>\nLa captura es una mecánica del juego. En la naturaleza observa a distancia, sin perseguir, alimentar ni retirar animales."
            +"\n\n<size=18>Fuentes: CONABIO, Bosques templados; Animal Diversity Web, ficha de "+entry.ScientificName+".</size>";
    }
    public static event Action CollectionChanged;

    public static Entry Find(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return null;
        foreach (Entry entry in entries)
            if (string.Equals(entry.Id, id, StringComparison.OrdinalIgnoreCase))
                return entry;
        return null;
    }

    public static Entry FindForName(string objectName)
    {
        if (string.IsNullOrWhiteSpace(objectName))
            return entries[0];

        string normalized = objectName.ToLowerInvariant();
        if (normalized.Contains("conejo") || normalized.Contains("pato")) return Find("conejo_serrano");
        if (normalized.Contains("coyote")) return Find("coyote");
        if (normalized.Contains("zorr")) return Find("zorra_gris");
        if (normalized.Contains("tlacuache")) return Find("tlacuache");
        if (normalized.Contains("ardilla")) return Find("ardilla_gris");
        return Find("venado_cola_blanca");
    }

    public static bool IsCaptured(string id)
    {
        return Find(id) != null && PlayerPrefs.GetInt(SavePrefix + id, 0) == 1;
    }

    public static bool RegisterCapture(string id)
    {
        Entry entry = Find(id);
        if (entry == null)
            return false;

        bool isNew = !IsCaptured(id);
        if (isNew)
        {
            PlayerPrefs.SetInt(SavePrefix + id, 1);
            PlayerPrefs.Save();
            CollectionChanged?.Invoke();
        }
        return isNew;
    }

    public static int CapturedCount
    {
        get
        {
            int count = 0;
            foreach (Entry entry in entries)
                if (IsCaptured(entry.Id)) count++;
            return count;
        }
    }

    public static Sprite GetSprite(Entry entry)
    {
        if (entry == null)
            return null;
        if (SpriteCache.TryGetValue(entry.Id, out Sprite cached) && cached != null)
            return cached;

        string expectedName = "Fauna_" + entry.Id;
        foreach (Sprite sprite in Resources.LoadAll<Sprite>(AtlasResource))
        {
            if (sprite != null && sprite.name == expectedName)
            {
                SpriteCache[entry.Id] = sprite;
                return sprite;
            }
        }

        Texture2D atlas = Resources.Load<Texture2D>(AtlasResource);
        if (atlas == null)
            return null;

        int cellWidth = atlas.width / 3;
        int cellHeight = atlas.height / 2;
        int column = entry.AtlasIndex % 3;
        int rowFromTop = entry.AtlasIndex / 3;
        Rect rect = new Rect(column * cellWidth, atlas.height - (rowFromTop + 1) * cellHeight, cellWidth, cellHeight);
        Sprite created = Sprite.Create(atlas, rect, new Vector2(.5f, 0f), 120f, 0, SpriteMeshType.FullRect);
        created.name = expectedName;
        SpriteCache[entry.Id] = created;
        return created;
    }

    public static void ApplyAppearance(GameObject target, Entry entry)
    {
        if (target == null || entry == null)
            return;

        SpriteRenderer renderer = target.GetComponentInChildren<SpriteRenderer>(true);
        if (renderer != null)
        {
            renderer.sprite = GetSprite(entry);
            renderer.color = Color.white;
            renderer.sortingOrder = 710;
        }

        Animator animator = target.GetComponent<Animator>();
        if (animator != null)
            animator.enabled = false;

        target.transform.localScale = Vector3.one * entry.WorldScale;
        BoxCollider box = target.GetComponent<BoxCollider>();
        if (box != null)
        {
            box.center = entry.ColliderCenter;
            box.size = entry.ColliderSize;
        }

        Animal animal = target.GetComponent<Animal>();
        if (animal != null)
            animal.ConfigureSpecies(entry.Species);

        XunjuuAnimalCapture capture = target.GetComponent<XunjuuAnimalCapture>();
        if (capture == null)
            capture = target.AddComponent<XunjuuAnimalCapture>();
        capture.Configure(entry.Id);
        if (target.GetComponent<XunjuuAnimalSpriteMotion>() == null)
            target.AddComponent<XunjuuAnimalSpriteMotion>();

        XunjuuWorldLabel label = target.GetComponent<XunjuuWorldLabel>();
        if (label == null)
            label = target.AddComponent<XunjuuWorldLabel>();
        label.Configure("Español: " + entry.DisplayName + "\nC: capturar y registrar", true, entry.ColliderCenter + Vector3.up * 1.05f);
    }
}
