using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

public static class XunjuuWorldScaleValidation
{
    public static void Run(PlayerController player)
    {
        float height = XunjuuWorldScale.PlayerHeight(player.transform);
        var meshes = UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None);
        var houses = meshes.Where(m => m.sharedMesh != null && m.sharedMesh.name.StartsWith("Casa") && m.GetComponentInParent<XunjuuMeadowDressing>() != null).ToArray();
        var trees = meshes.Where(m => m.sharedMesh != null && m.sharedMesh.name.StartsWith("Arbol") && (m.name == "Visual_3D" || m.name == "Arboleda_Lejana")).ToArray();
        if (houses.Length < 3 || trees.Length < 3) throw new Exception("Scale QA: missing village or forest.");
        var report = new StringBuilder();
        report.AppendLine($"Player visible reference height: {height:F3}");
        foreach (var house in houses)
        {
            Vector3 scale = house.transform.lossyScale;
            float door = XunjuuWorldScale.HouseDoorHeight * Mathf.Abs(scale.y);
            if (Mathf.Abs(door / height - XunjuuWorldScale.DoorToPlayer) > .01f) throw new Exception("Scale QA: door too small/large: " + house.name);
            var collider = house.GetComponent<BoxCollider>();
            if (collider == null || collider.isTrigger || !collider.enabled) throw new Exception("Scale QA: house lost solid collider.");
            Vector3 size = XunjuuArtMeshes.HouseSize(house.sharedMesh.name);
            if ((collider.size - size).sqrMagnitude > .001f || Mathf.Abs(collider.center.y - size.y * .5f) > .001f) throw new Exception("Scale QA: collider scaled twice.");
            if (Mathf.Abs(scale.x - scale.y) > .01f || Mathf.Abs(scale.z - scale.y) > .01f) throw new Exception("Scale QA: distorted house.");
            report.AppendLine($"{house.name}: door={door:F3}, roof={house.GetComponent<Renderer>().bounds.size.y:F3}, wall footprint={size.x*scale.x:F3}x{size.z*scale.z:F3}");
        }
        float min = float.PositiveInfinity, max = 0;
        foreach (var tree in trees)
        {
            float treeHeight = tree.GetComponent<Renderer>().bounds.size.y;
            float ratio = treeHeight / height;
            if (ratio < XunjuuWorldScale.MinimumTreeToPlayer - .02f || ratio > XunjuuWorldScale.MaximumTreeToPlayer + .02f) throw new Exception("Scale QA: tree out of range: " + tree.name + " ratio=" + ratio);
            Vector3 scale = tree.transform.lossyScale;
            if (Mathf.Abs(Mathf.Abs(scale.x) - Mathf.Abs(scale.y)) > .02f || Mathf.Abs(Mathf.Abs(scale.z) - Mathf.Abs(scale.y)) > .02f) throw new Exception("Scale QA: legacy parent distorts tree.");
            if (tree.name == "Visual_3D")
            {
                var trunk = tree.GetComponent<CapsuleCollider>();
                // The existing distance optimizer intentionally disables far
                // colliders. Preserve that policy; only nearby trunks must be live.
                if (trunk == null || trunk.isTrigger) throw new Exception("Scale QA: missing solid trunk: " + tree.transform.parent.name + "/" + tree.sharedMesh.name + " collider=" + (trunk == null ? "null" : "trigger=" + trunk.isTrigger) + " soft=" + XunjuuSceneVisualPolicy.IsSoftDecoration(tree.transform));
                Vector3 delta=tree.transform.position-player.transform.position;delta.y=0;
                if(delta.sqrMagnitude<15f*15f && !trunk.enabled)throw new Exception("Scale QA: nearby trunk collision disabled.");
            }
            min = Mathf.Min(min, treeHeight); max = Mathf.Max(max, treeHeight);
        }
        report.AppendLine($"Trees: {trees.Length}; height range={min:F3}..{max:F3}");
        report.AppendLine("PASS: proportions, uniform scale, solid wall/trunk colliders.");
        Directory.CreateDirectory("output/world-scale");
        File.WriteAllText("output/world-scale/validation.txt", report.ToString());
        Debug.Log($"[WORLD SCALE PASS] player={height:F3}, houses={houses.Length}, trees={trees.Length}, doors=1.25x, trees=3.2..4.8x.");
    }
}
