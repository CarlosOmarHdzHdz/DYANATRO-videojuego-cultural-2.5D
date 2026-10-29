using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Foco limitado al menú o diálogo superior; no modifica la navegación de minijuegos.
[DefaultExecutionOrder(-300)]
public sealed class XunjuuMenuNavigation : MonoBehaviour
{
    private static readonly List<XunjuuMenuNavigation> scopes = new List<XunjuuMenuNavigation>();
    private DyanatroGameDirector owner;
    private Selectable[] controls;
    private GameObject previous;
    private int priority;
    private bool needsFocus;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetScopes() { scopes.Clear(); }

    public static void Configure(GameObject root, DyanatroGameDirector director, int order, params Selectable[] buttons)
    {
        var scope = root.AddComponent<XunjuuMenuNavigation>();
        scope.owner = director; scope.priority = order; scope.controls = buttons;
        for (int i = 0; i < buttons.Length; i++)
        {
            var navigation = new Navigation { mode = Navigation.Mode.Explicit };
            navigation.selectOnUp = buttons[(i + buttons.Length - 1) % buttons.Length];
            navigation.selectOnDown = buttons[(i + 1) % buttons.Length];
            if (!(buttons[i] is Slider))
            {
                navigation.selectOnLeft = navigation.selectOnUp;
                navigation.selectOnRight = navigation.selectOnDown;
            }
            buttons[i].navigation = navigation;
        }
        scope.needsFocus = true;
    }

    private void OnEnable()
    {
        scopes.Add(this);
        previous = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        needsFocus = true;
    }

    private bool IsTop()
    {
        if (owner == null || controls == null || owner.IsLudotecaOpen) return false;
        foreach (var scope in scopes)
            if (scope != null && scope.isActiveAndEnabled && scope.owner == owner && scope.priority > priority) return false;
        return true;
    }

    private bool Contains(GameObject target)
    {
        if (target == null || controls == null) return false;
        foreach (var control in controls)
            if (control != null && control.gameObject == target) return true;
        return false;
    }

    private void Update()
    {
        var events = EventSystem.current;
        if (events == null) return;
        if (!IsTop())
        {
            if (Contains(events.currentSelectedGameObject)) events.SetSelectedGameObject(null);
            return;
        }
        if (needsFocus || !Contains(events.currentSelectedGameObject))
        {
            foreach (var control in controls)
                if (control != null && control.IsActive() && control.IsInteractable())
                { events.SetSelectedGameObject(control.gameObject); break; }
            needsFocus = false;
        }
        if (!Input.GetKeyDown(KeyCode.Tab)) return;
        int index = 0;
        for (int i = 0; i < controls.Length; i++) if (controls[i].gameObject == events.currentSelectedGameObject) index = i;
        int direction = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) ? -1 : 1;
        for (int step = 1; step <= controls.Length; step++)
        {
            var next = controls[(index + direction * step + controls.Length) % controls.Length];
            if (next != null && next.IsActive() && next.IsInteractable()) { next.Select(); break; }
        }
    }

    private void OnDisable()
    {
        scopes.Remove(this);
        var events = EventSystem.current;
        if (events == null) return;
        if (Contains(events.currentSelectedGameObject)) events.SetSelectedGameObject(null);
        // Recuperar el botón que abrió el diálogo, sin robar foco a la ludoteca.
        foreach (var scope in scopes)
            if (scope != null && scope.IsTop() && scope.Contains(previous) && previous.activeInHierarchy)
            { events.SetSelectedGameObject(previous); break; }
    }
}
