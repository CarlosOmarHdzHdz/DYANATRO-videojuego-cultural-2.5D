using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

// Explicit, repeatable migration. Unity moves assets and their .meta files together.
public static class XunjuuProjectOrganizer
{
    [Serializable] public class Move { public string from, to, guid; }
    [Serializable] public class Manifest { public List<Move> moves = new List<Move>(); }
    private const string Report = "output/project-organization";

    public static void VerifyMigratedLayout()
    {
        string run=Directory.GetDirectories(Report).Where(p=>File.Exists(p+"/RESULT.txt") && File.ReadAllText(p+"/RESULT.txt").StartsWith("PASS:")).OrderByDescending(p=>p).First();
        var manifest=JsonUtility.FromJson<Manifest>(File.ReadAllText(run+"/moves.json"));
        var moves=manifest.moves.OrderByDescending(m=>m.from.Length).ToArray();
        foreach(var m in moves)
            if(AssetDatabase.AssetPathToGUID(m.to)!=m.guid)throw new Exception("GUID mismatch: "+m.to);
        // Verification must never restore historical source over current work.
        var sourceBackups=Directory.Exists(run+"/source-before") ? Directory.GetFiles(run+"/source-before","*.cs",SearchOption.AllDirectories) : new string[0];
        var edited=new HashSet<string>(sourceBackups.Select(p=>p.Replace('\\','/').Substring((run.Replace('\\','/')+"/source-before/").Length)));
        foreach(string line in File.ReadAllLines(run+"/hashes-before.tsv"))
        {
            var parts=line.Split('\t');
            string path=Map(parts[0],moves);
            if(!File.Exists(path))throw new Exception("Missing: "+path);
            if(!edited.Contains(parts[0]) && !path.EndsWith("/XunjuuProjectOrganizer.cs") && Hash(path)!=parts[1])throw new Exception("Hash mismatch: "+path);
        }
        File.WriteAllText(run+"/VERIFIED.txt",$"PASS: {moves.Length} moves; GUIDs verified after import; original asset bytes preserved; {edited.Count} source path updates. Resources unchanged.");
        Debug.Log("[ORGANIZE VERIFIED] "+moves.Length+" moves");
        AssetDatabase.Refresh();
    }

    [MenuItem("Tools/Xunjuu/Organizar proyecto %&o")]
    public static void Organize()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Salir de Play antes de organizar.");
        var plan = new Manifest();
        Action<string,string> add = (from,to) => {
            if (!File.Exists(from) && !Directory.Exists(from)) return;
            if (!from.StartsWith("Assets/") || !to.StartsWith("Assets/") || from.Contains("..") || to.Contains("..")) throw new Exception("Ruta fuera de Assets");
            if (File.Exists(to) || Directory.Exists(to)) throw new Exception("Destino ocupado: " + to);
            string guid = AssetDatabase.AssetPathToGUID(from);
            if (string.IsNullOrEmpty(guid)) throw new Exception("Asset no importado: " + from);
            plan.moves.Add(new Move { from=from, to=to, guid=guid });
        };
        var groups = new Dictionary<string,string[]> {
            {"Core", new[]{"DyanatroGameDirector","GameStateManager","XunjuuOpeningJourney"}},
            {"Player", new[]{"PlayerController","XunjuuCompleteSpriteAnimator"}},
            {"Camera", new[]{"CameraFollow","XunjuuTreeCameraOccluder"}},
            {"UI", new[]{"HealthBarUI","XunjuuHealthBarVisual","XunjuuDisplayController"}},
            {"Audio", new[]{"XunjuuProceduralAudio"}},
            {"Rendering", new[]{"DyanatroSpriteDepthSorter","XunjuuSceneVisualPolicy"}},
            {"Environment", new[]{"XunjuuArtMeshes","XunjuuEnvironment3D","XunjuuEnvironmentCatalog","XunjuuMeadowDressing","XunjuuTerrainGrounding","ForestGenerator","CloudStatic","CloudSpawnerDome","CloudBillboard"}},
            {"Combat/Enemies", new[]{"EnemyHealt","EnemyFire","FireballProjectile"}},
            {"Collectibles", new[]{"MazahuaWordCollectible","MazahuaWordAnimalSpawner","MazahuaMotifVoxel2_5D"}},
            {"Interaction", new[]{"treeinteractivo"}},
            {"Combat/Weapons", new[]{"SwordPickupItem"}},
            {"Diagnostics", new[]{"XunjuuFrameCapture"}}
        };
        foreach (var group in groups) foreach (string name in group.Value)
            add("Assets/Scripts/"+name+".cs", "Assets/Scripts/"+group.Key+"/"+name+".cs");
        add("Assets/Scripts/Animales", "Assets/Scripts/Animals");
        add("Assets/Scripts/Boss", "Assets/Scripts/Combat/Boss");
        // Move individual weapon scripts into the destination that also holds pickups.
        if (Directory.Exists("Assets/Scripts/Sword")) foreach (string file in Directory.GetFiles("Assets/Scripts/Sword", "*.cs"))
            add(file.Replace('\\','/'), "Assets/Scripts/Combat/Weapons/"+Path.GetFileName(file));
        foreach (string file in Directory.GetFiles("Assets/Editor", "*.cs"))
        {
            string name=Path.GetFileName(file);
            string group=name.Contains("Validation") || name.Contains("Smoke") ? "Validation"
                : name.Contains("Importer") || name.Contains("TextureRepair") ? "Importers"
                : name=="AndroidBuildScript.cs" ? "Build" : "Authoring";
            add(file.Replace('\\','/'), "Assets/Editor/"+group+"/"+name);
        }
        add("Assets/Editor/XunjuuTwoLevelSetup.applied", "Assets/Editor/Authoring/XunjuuTwoLevelSetup.applied");
        add("Assets/Animation", "Assets/Animations/Legacy");
        var prefabGroups = new Dictionary<string,string[]> {
            {"Environment",new[]{"Tree","arbolN","cloudPrefab"}},
            {"Animals",new[]{"Ave","lorro"}},
            {"Combat/Weapons",new[]{"SwordFloating"}},
            {"Combat/Enemies",new[]{"Enemigo_Dyanatro_Nivel2"}},
            {"Combat/Projectiles",new[]{"Fireball"}},
            {"Combat/Boss",new[]{"Ocelotl"}},
            {"Missions",new[]{"MisionSecundaria_Macuahuitl","Recompensa_Nivel2_Pendiente"}},
            {"Collectibles",new[]{"MotivoMazahuaVoxel2_5D"}}
        };
        foreach(var group in prefabGroups) foreach(string name in group.Value)
            add("Assets/Prefabs/"+name+".prefab","Assets/Prefabs/"+group.Key+"/"+name+".prefab");
        add("Assets/HealthBarGlow.mat","Assets/Materials/UI/HealthBarGlow.mat");
        add("Assets/DefaultVolumeProfile.asset","Assets/Settings/Rendering/DefaultVolumeProfile.asset");
        add("Assets/UniversalRenderPipelineGlobalSettings.asset","Assets/Settings/Rendering/UniversalRenderPipelineGlobalSettings.asset");
        add("Assets/InputSystem_Actions.inputactions","Assets/Settings/Input/InputSystem_Actions.inputactions");
        add("Assets/InputSystem.inputsettings.asset","Assets/Settings/Input/InputSystem.inputsettings.asset");
        foreach(string file in Directory.GetFiles("Assets","*.terrainlayer"))
            add(file.Replace('\\','/'),"Assets/Environment/Terrain/Layers/"+Path.GetFileName(file));
        foreach(string file in Directory.GetFiles("Assets","*.asset"))
            if(Path.GetFileName(file).StartsWith("TerrainData_") || Path.GetFileName(file).StartsWith("New Terrain"))
                add(file.Replace('\\','/'),"Assets/Environment/Terrain/Data/"+Path.GetFileName(file));
        foreach (string dir in Directory.GetDirectories("Assets/Sprites", "_backup*"))
            add(dir.Replace('\\','/'), "Assets/Art/Archive/Player/"+Path.GetFileName(dir));
        foreach (string file in Directory.GetFiles("Assets/Sprites"))
        {
            string name=Path.GetFileName(file), ext=Path.GetExtension(file).ToLowerInvariant();
            if(ext==".meta")continue;
            string destination;
            if(ext==".anim" || ext==".controller") destination="Assets/Animations/LegacySprites/";
            else if(ext==".mat") destination="Assets/Materials/LegacySprites/";
            else if(name.StartsWith("personaje_")) destination="Assets/Art/Source/Player/";
            else if(name.Contains("Mazahua")) destination="Assets/Sprites/Collectibles/";
            else if(name.StartsWith("enemigo") || name.StartsWith("Enemy") || name=="bolafuego.png") destination="Assets/Sprites/Enemies/";
            else if(name=="pajaro-Sheet.png") destination="Assets/Sprites/Animals/";
            else if(name.StartsWith("tree") || name.StartsWith("arbol") || name=="clound.png" || name=="hoja.png") destination="Assets/Sprites/Environment/";
            else if(name=="healt.png") destination="Assets/Sprites/UI/";
            else destination="Assets/Sprites/Player/Legacy/";
            add(file.Replace('\\','/'), destination+name);
        }
        if(plan.moves.Count==0){Debug.Log("[ORGANIZE] Already organized.");return;}
        if(plan.moves.Select(m=>m.to).Distinct(StringComparer.OrdinalIgnoreCase).Count()!=plan.moves.Count)throw new Exception("Destinos duplicados");
        Directory.CreateDirectory(Report);
        string run=Report+"/"+DateTime.Now.ToString("yyyyMMdd-HHmmss");
        Directory.CreateDirectory(run);
        File.WriteAllText(run+"/moves.json",JsonUtility.ToJson(plan,true));
        // Snapshot all asset bytes and metadata before moving anything.
        var before=Directory.GetFiles("Assets","*",SearchOption.AllDirectories)
            .ToDictionary(p=>p.Replace('\\','/'),p=>Hash(p));
        var edits=new Dictionary<string,string>();
        var ordered=plan.moves.OrderByDescending(m=>m.from.Length).ToArray();
        foreach(string source in Directory.GetFiles("Assets","*.cs",SearchOption.AllDirectories))
        {
            string path=source.Replace('\\','/');
            if(path.EndsWith("/XunjuuProjectOrganizer.cs"))continue;
            string original=File.ReadAllText(source), updated=original;
            foreach(var m in ordered)updated=ReplaceAssetPath(updated,m);
            if(updated!=original){edits[path]=original;}
        }
        File.WriteAllLines(run+"/hashes-before.tsv",before.Select(p=>p.Key+"\t"+p.Value));
        var completed=new List<Move>();
        // New folders must be imported before batching moves into them.
        foreach(var m in plan.moves)EnsureFolder(Path.GetDirectoryName(m.to).Replace('\\','/'));
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        EditorApplication.LockReloadAssemblies();
        try
        {
            foreach(var m in plan.moves)
            {
                EnsureFolder(Path.GetDirectoryName(m.to).Replace('\\','/'));
                string error=AssetDatabase.MoveAsset(m.from,m.to);
                if(!string.IsNullOrEmpty(error))throw new Exception(error);
                completed.Add(m);
            }
            foreach(var edit in edits)
            {
                string path=Map(edit.Key,ordered), updated=edit.Value;
                foreach(var m in ordered)updated=ReplaceAssetPath(updated,m);
                string backup=run+"/source-before/"+edit.Key;
                Directory.CreateDirectory(Path.GetDirectoryName(backup));File.WriteAllText(backup,edit.Value);
                File.WriteAllText(path,updated,new UTF8Encoding(false));
            }
            foreach(var item in before)
            {
                string path=Map(item.Key,ordered);
                if(!File.Exists(path))throw new Exception("Archivo perdido: "+path);
                if(!edits.ContainsKey(item.Key) && Hash(path)!=item.Value)throw new Exception("Contenido alterado: "+path);
            }
            // Metadata is authoritative even before a subsequent import refresh.
            foreach(var m in completed)if(!File.ReadAllLines(m.to+".meta").Contains("guid: "+m.guid))throw new Exception("GUID cambiado: "+m.to);
            File.WriteAllText(run+"/RESULT.txt",$"PASS: {completed.Count} movimientos; todos los archivos y .meta conservados byte a byte salvo {edits.Count} scripts con rutas actualizadas. Resources intacto.\n");
            Debug.Log($"[ORGANIZE PASS] {completed.Count} moves, {edits.Count} path updates. GUID and content checks passed. Report: {run}");
        }
        catch(Exception e)
        {
            foreach(var edit in edits) {string path=Map(edit.Key,completed.ToArray());if(File.Exists(path))File.WriteAllText(path,edit.Value);}
            foreach(var m in completed.AsEnumerable().Reverse())
            {EnsureFolder(Path.GetDirectoryName(m.from).Replace('\\','/'));string error=AssetDatabase.MoveAsset(m.to,m.from);if(!string.IsNullOrEmpty(error))Debug.LogError("Rollback: "+error);}
            Debug.LogException(e);File.WriteAllText(run+"/RESULT.txt","FAILED; rollback attempted: "+e);
        }
        finally {EditorApplication.UnlockReloadAssemblies();AssetDatabase.Refresh();}
    }
    private static string Map(string path,Move[] moves)
    {
        foreach(var m in moves)if(path==m.from || path==m.from+".meta" || path.StartsWith(m.from+"/",StringComparison.Ordinal))return m.to+path.Substring(m.from.Length);
        return path;
    }
    private static string ReplaceAssetPath(string source,Move move)
    {
        // A folder prefix must end at a path boundary (Animation != Animations).
        return Regex.Replace(source,Regex.Escape(move.from)+"(?=$|[/\"\\s])",_=>move.to);
    }
    private static string Hash(string path){using(var sha=SHA256.Create())using(var stream=File.OpenRead(path))return BitConverter.ToString(sha.ComputeHash(stream));}
    private static void EnsureFolder(string path)
    {
        if(AssetDatabase.IsValidFolder(path))return;
        string parent=Path.GetDirectoryName(path).Replace('\\','/');EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent,Path.GetFileName(path));
    }
}
