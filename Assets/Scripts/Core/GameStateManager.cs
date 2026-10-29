using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(900)]
public sealed class GameStateManager : MonoBehaviour
{
    private DyanatroGameDirector director;
    private readonly List<CanvasGroup> hud = new List<CanvasGroup>();
    private readonly HashSet<CanvasGroup> keepDuringInventory = new HashSet<CanvasGroup>();
    public bool HudVisible => director != null && director.IsGameplayHudVisible;

    public static void RegisterHud(GameObject target, bool hideDuringInventory = true)
    {
        var owner = FindFirstObjectByType<DyanatroGameDirector>();
        if (owner == null || target == null) return;
        var manager = owner.GetComponent<GameStateManager>() ?? owner.gameObject.AddComponent<GameStateManager>();
        manager.director = owner;
        owner.GetComponent<XunjuuHudLayout>()?.Invalidate();
        Canvas canvas = target.GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>() ?? canvas.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
        }
        var group = target.GetComponent<CanvasGroup>();
        if (group == null) group = target.AddComponent<CanvasGroup>();
        if (!manager.hud.Contains(group)) manager.hud.Add(group);
        if(!hideDuringInventory)manager.keepDuringInventory.Add(group);
        manager.Apply(group);
    }

    private void LateUpdate()
    {
        for (int i = hud.Count - 1; i >= 0; i--)
            if (hud[i] == null) hud.RemoveAt(i); else Apply(hud[i]);
    }

    private void Apply(CanvasGroup group)
    {
        if (group == null) return;
        bool visible = HudVisible && (keepDuringInventory.Contains(group) || !director.IsInventoryOpen);
        float alpha = visible ? 1f : 0f;
        if (group.alpha != alpha) group.alpha = alpha;
        if (group.interactable != visible) group.interactable = visible;
        if (group.blocksRaycasts != visible) group.blocksRaycasts = visible;
    }
}
