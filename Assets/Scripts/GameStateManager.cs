using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(900)]
public sealed class GameStateManager : MonoBehaviour
{
    private DyanatroGameDirector director;
    private readonly List<CanvasGroup> hud = new List<CanvasGroup>();
    public bool HudVisible => director != null && director.IsGameplayHudVisible;

    public static void RegisterHud(GameObject target)
    {
        var owner = FindFirstObjectByType<DyanatroGameDirector>();
        if (owner == null || target == null) return;
        var manager = owner.GetComponent<GameStateManager>() ?? owner.gameObject.AddComponent<GameStateManager>();
        manager.director = owner;
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
        group.alpha = HudVisible ? 1f : 0f;
        group.interactable = HudVisible;
        group.blocksRaycasts = HudVisible;
    }
}
