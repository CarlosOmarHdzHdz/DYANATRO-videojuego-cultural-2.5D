using System;
using System.Collections.Generic;
using UnityEngine;

// Información cultural separada del balance ficticio de recuperación de vida.
public static class XunjuuMedicinalPlantCatalog
{
    public sealed class Entry
    {
        public readonly string Id, DisplayName, ScientificName, Description, Type, Source;
        public Entry(string id, string name, string scientificName, string description, string type, string source = "https://www.gob.mx/inifap/es/articulos/plantas-medicinales-tradicion-ancestral")
        { Id = id; DisplayName = name; ScientificName = scientificName; Description = description; Type = type; Source = source; }
    }

    public const string SavePrefix = "xunjuu.planta.registrada.";
    private static readonly Entry[] entries = {
        new Entry("manzanilla", "Manzanilla", "Matricaria chamomilla",
            "Hierba representada con pequeñas flores de centro amarillo y pétalos blancos. La manzanilla forma parte de la herbolaria mexicana; sus usos tradicionales no equivalen a eficacia médica comprobada.", "Hierba de flor"),
        new Entry("hierbabuena", "Hierbabuena", "Mentha spicata",
            "Planta aromática representada con hojas verdes opuestas. Se utiliza en alimentos y forma parte de la herbolaria mexicana. El nombre común puede referirse a distintas plantas; esta ficha representa Mentha spicata.", "Hierba aromática"),
        new Entry("arnica", "Árnica mexicana", "Heterotheca inuloides",
            "Ilustración de una hierba con flores amarillas agrupadas. El nombre árnica se utiliza para distintas especies; aquí se representa la árnica mexicana. Su presencia en la medicina tradicional no convierte esta ilustración en una guía de uso.", "Hierba de flor"),
        new Entry("sabila", "Sábila", "Aloe vera",
            "Planta suculenta de hojas carnosas dispuestas en roseta y con bordes dentados. Es una especie cultivada en México, no exclusiva de la región mazahua. Forma parte de referencias nacionales de herbolaria.", "Planta suculenta"),
        new Entry("romero", "Romero", "Salvia rosmarinus",
            "Arbusto aromático de hojas estrechas, representado con ramas leñosas. También se conoce por el nombre científico Rosmarinus officinalis. Su origen es mediterráneo y se cultiva en otras regiones; no se presenta como una especie nativa mazahua.", "Arbusto aromático", "https://www.cibnor.gob.mx/personal/bmurillo/hierbas/romero.php"),
        new Entry("toronjil", "Toronjil morado", "Agastache mexicana",
            "Hierba aromática representada con flores morado-rosadas agrupadas en la parte alta de los tallos. El nombre toronjil puede identificar distintas plantas; esta ficha corresponde a Agastache mexicana, citada en materiales de herbolaria mexicana.", "Hierba de flor", "https://www.gob.mx/semarnat/articulos/medicina-tradicional-mexicana-valioso-conocimiento-ancestral-y-legado-biocultural")
    };
    private static readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();
    public static IReadOnlyList<Entry> Entries => entries;
    public static event Action CollectionChanged;
    public static event Action<string> CardViewed;
    public static void RecordCardViewed(string id) { if (IsRegistered(id)) CardViewed?.Invoke(id); }
    public static Entry Find(string id) => Array.Find(entries, e => string.Equals(e.Id, id, StringComparison.OrdinalIgnoreCase));
    public static bool IsRegistered(string id) => Find(id) != null && PlayerPrefs.GetInt(SavePrefix + Find(id).Id, 0) == 1;
    public static int RegisteredCount { get { int count = 0; foreach (var e in entries) if (IsRegistered(e.Id)) count++; return count; } }

    public static bool Register(string id)
    {
        Entry entry = Find(id);
        if (entry == null || IsRegistered(entry.Id)) return false;
        PlayerPrefs.SetInt(SavePrefix + entry.Id, 1);
        PlayerPrefs.Save();
        CollectionChanged?.Invoke();
        return true;
    }

    public static string Information(Entry e)
    {
        if (e == null) return string.Empty;
        return "<b>IDENTIFICACIÓN</b>\n<i>" + e.ScientificName + "</i>\n" + e.Description
            + "\n\n<b>UBICACIÓN EN EL JUEGO</b>\nEjemplar de un huerto cultivado. El estudio de García y Guzmán Mendoza documenta nombres comunes como manzanilla, hierbabuena, árnica, sábila, romero y toronjil en San Sebastián, San José del Rincón. No acredita por sí solo la especie científica exacta de cada nombre ni que todas sean nativas de San Felipe del Progreso."
            + "\n\n<b>MEMORIA BIOCULTURAL</b>\nLa herbolaria está vinculada con saberes familiares y comunitarios. García y Guzmán Mendoza documentaron el conocimiento de plantas medicinales en San Sebastián, San José del Rincón. No se atribuye una receta única a todas las comunidades mazahuas."
            + "\n\n<b>REGISTRO EN EL VIDEOJUEGO</b>\nAcércate y pulsa G para recolectar una unidad y desbloquear esta ficha. El registro permanece en la ludoteca y el recurso se guarda en el morral. Este archivo se dedica a la identificación ilustrativa y al contexto cultural de las plantas."
            + "\n\n<b>CUIDADO DEL ENTORNO</b>\nLa recolección es una mecánica lúdica. Las ilustraciones no permiten identificar plantas reales. No arranques ni consumas plantas sin orientación especializada."
            + "\n\n<b>IMPORTANTE</b>\nContenido educativo, no recomendaciones de consumo ni tratamiento médico. El catálogo no afirma que las seis plantas sean nativas o de uso universal en comunidades mazahuas. Los nombres en mazahua y los usos locales requieren revisión lingüística y comunitaria."
            + "\n\n<size=18>Fuentes:\nContexto biocultural: García y Guzmán Mendoza. Conocimiento tradicional asociado al uso de plantas medicinales en migrantes mazahuas. Huellas de la Migración, 1(1), 195–220.\nhttps://huellasdelamigracion.uaemex.mx/article/view/4438\nReferencia de la planta (INIFAP, CIBNOR o SEMARNAT):\n" + e.Source + "</size>";
    }

    // Láminas detalladas compartidas por el mundo, las fichas y el morral.
    public static Sprite GetSprite(Entry entry)
    {
        if (entry == null) return null;
        if (sprites.TryGetValue(entry.Id, out var cached) && cached != null) return cached;
        var texture = Resources.Load<Texture2D>("Sprites/Plants/HerbalRealistic/" + entry.Id);
        if (texture == null)
        {
            Debug.LogError("Falta la lámina de herbolaria: " + entry.Id);
            return null;
        }
        // Se calcula una sola vez por especie. El recorte mantiene el alfa
        // original y coloca la base visible sobre el suelo, sin alterar el PNG.
        var pixels = texture.GetPixels32();
        int minX = texture.width, minY = texture.height, maxX = -1, maxY = -1;
        for (int y = 0; y < texture.height; y++) for (int x = 0; x < texture.width; x++)
        {
            if (pixels[y * texture.width + x].a < 16) continue;
            minX = Mathf.Min(minX, x); maxX = Mathf.Max(maxX, x);
            minY = Mathf.Min(minY, y); maxY = Mathf.Max(maxY, y);
        }
        if (maxX < minX || maxY < minY) return null;
        var rect = new Rect(minX, minY, maxX - minX + 1, maxY - minY + 1);
        var sprite = Sprite.Create(texture, rect, new Vector2(.5f, 0), rect.height / 2.25f, 0, SpriteMeshType.FullRect);
        sprite.name = "Planta_" + entry.Id + "_Realista";
        sprites[entry.Id] = sprite;
        return sprite;
    }
    public static Sprite RemedySprite
    {
        get
        {
            if (sprites.TryGetValue("preparado_camino", out var cached) && cached != null) return cached;
            cached = XunjuuPresentationSprites.Load("Sprites/Items/InfusionHierbas_v2",1f,Vector2.one*.5f);
            if(cached!=null){sprites["preparado_camino"]=cached;return cached;}
            var texture = new Texture2D(48, 48, TextureFormat.RGBA32, false) { name = "Preparado_del_camino", filterMode = FilterMode.Point };
            texture.SetPixels(new Color[48 * 48]);
            for (int y = 7; y < 36; y++) for (int x = 10; x < 38; x++)
                Pixel(texture, x, y, y > 29 ? new Color(.54f, .29f, .16f) : new Color(.27f, .49f, .28f));
            for (int y = 18; y < 27; y++) for (int x = 20; x < 28; x++) Pixel(texture, x, y, new Color(1f, .89f, .51f));
            texture.Apply();
            cached = Sprite.Create(texture, new Rect(0, 0, 48, 48), Vector2.one * .5f, 40);
            sprites["preparado_camino"] = cached; return cached;
        }
    }
    private static void Pixel(Texture2D texture, int x, int y, Color color)
    { if (x >= 0 && y >= 0 && x < texture.width && y < texture.height) texture.SetPixel(x, y, color); }
    private static void Disk(Texture2D texture, int x, int y, int radius, Color color)
    { for (int dx = -radius; dx <= radius; dx++) for (int dy = -radius; dy <= radius; dy++) if (dx * dx + dy * dy <= radius * radius) Pixel(texture, x + dx, y + dy, color); }
    private static void Line(Texture2D texture, int x, int y, int endX, int endY, Color color)
    {
        int steps = Mathf.Max(Mathf.Abs(endX - x), Mathf.Abs(endY - y));
        for (int i = 0; i <= steps; i++) { float t = steps == 0 ? 0 : i / (float)steps; Pixel(texture, Mathf.RoundToInt(Mathf.Lerp(x, endX, t)), Mathf.RoundToInt(Mathf.Lerp(y, endY, t)), color); }
    }
    private static void Triangle(Texture2D texture, Vector2Int a, Vector2Int b, Vector2Int c, Color color)
    {
        for (int y = 0; y < texture.height; y++) for (int x = 0; x < texture.width; x++)
        {
            int ab = (x - b.x) * (a.y - b.y) - (a.x - b.x) * (y - b.y);
            int bc = (x - c.x) * (b.y - c.y) - (b.x - c.x) * (y - c.y);
            int ca = (x - a.x) * (c.y - a.y) - (c.x - a.x) * (y - a.y);
            if (!((ab < 0 || bc < 0 || ca < 0) && (ab > 0 || bc > 0 || ca > 0))) Pixel(texture, x, y, color);
        }
    }
}
