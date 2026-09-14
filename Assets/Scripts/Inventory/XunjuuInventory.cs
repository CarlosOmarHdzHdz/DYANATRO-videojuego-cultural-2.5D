using System;
using System.Collections.Generic;
using UnityEngine;

// ============================================================================
// Xunjuú v0.1 - Inventario del protagonista
// Acción: almacenar y exponer recursos recogidos en una lista editable.
// ============================================================================
[DisallowMultipleComponent]
public sealed class XunjuuInventory : MonoBehaviour
{
    [Serializable]
    public sealed class Slot
    {
        [SerializeField] private string itemId;
        [SerializeField] private string displayName;
        [SerializeField] private int amount;
        [SerializeField] private Sprite icon;

        public string ItemId => itemId;
        public string DisplayName => displayName;
        public int Amount => amount;
        public Sprite Icon => icon;

        public Slot(string id, string name, int initialAmount, Sprite itemIcon)
        {
            itemId = id;
            displayName = name;
            amount = Mathf.Max(0, initialAmount);
            icon = itemIcon;
        }

        public void Add(int value)
        {
            amount = Mathf.Max(0, amount + value);
        }

        public void SetIcon(Sprite itemIcon)
        {
            if (icon == null && itemIcon != null)
                icon = itemIcon;
        }
    }

    [Header("Xunjuú v0.1 - Capacidad")]
    [SerializeField, Min(1)] private int maxDifferentItems = 32;
    [SerializeField] private List<Slot> items = new List<Slot>();
    [SerializeField, Min(-1)] private int selectedIndex = -1;

    public event Action Changed;
    public IReadOnlyList<Slot> Items => items;
    public int SelectedIndex => selectedIndex;
    public int Capacity => maxDifferentItems;
    public Slot SelectedItem => selectedIndex >= 0 && selectedIndex < items.Count ? items[selectedIndex] : null;

    // ACCIÓN: agregar una cantidad y apilarla si el recurso ya existe.
    public bool AddItem(string itemId, string displayName, int amount = 1, Sprite icon = null)
    {
        if (string.IsNullOrWhiteSpace(itemId) || amount <= 0)
            return false;

        Slot existing = items.Find(slot => string.Equals(slot.ItemId, itemId, StringComparison.OrdinalIgnoreCase));
        if (existing != null)
        {
            existing.Add(amount);
            existing.SetIcon(icon);
            Changed?.Invoke();
            return true;
        }

        if (items.Count >= maxDifferentItems)
            return false;

        string safeName = string.IsNullOrWhiteSpace(displayName) ? itemId : displayName;
        items.Add(new Slot(itemId.Trim(), safeName.Trim(), amount, icon));
        if (selectedIndex < 0)
            selectedIndex = 0;
        Changed?.Invoke();
        return true;
    }

    // ACCIÓN: retirar recursos sin permitir cantidades negativas.
    public bool RemoveItem(string itemId, int amount = 1)
    {
        if (string.IsNullOrWhiteSpace(itemId) || amount <= 0)
            return false;

        Slot existing = items.Find(slot => string.Equals(slot.ItemId, itemId, StringComparison.OrdinalIgnoreCase));
        if (existing == null || existing.Amount < amount)
            return false;

        existing.Add(-amount);
        if (existing.Amount == 0)
            items.Remove(existing);

        ClampSelection();

        Changed?.Invoke();
        return true;
    }

    // Xunjuu v0.1 - ACCION: recorrer el inventario como una secuencia de eleccion.
    public void SelectNext()
    {
        if (items.Count == 0)
            return;

        selectedIndex = (selectedIndex + 1 + items.Count) % items.Count;
        Changed?.Invoke();
    }

    public void SelectPrevious()
    {
        if (items.Count == 0)
            return;

        selectedIndex = (selectedIndex - 1 + items.Count) % items.Count;
        Changed?.Invoke();
    }

    public void SelectIndex(int index)
    {
        if (index < 0 || index >= items.Count)
            return;

        selectedIndex = index;
        Changed?.Invoke();
    }

    // ACCIÓN: consultar una cantidad desde misiones u otros sistemas.
    public int GetAmount(string itemId)
    {
        Slot existing = items.Find(slot => string.Equals(slot.ItemId, itemId, StringComparison.OrdinalIgnoreCase));
        return existing != null ? existing.Amount : 0;
    }

    // ACCIÓN: limpiar el inventario durante pruebas desde el Inspector.
    [ContextMenu("Xunjuú v0.1/Limpiar inventario")]
    public void ClearInventory()
    {
        items.Clear();
        selectedIndex = -1;
        Changed?.Invoke();
    }

    private void ClampSelection()
    {
        selectedIndex = items.Count == 0 ? -1 : Mathf.Clamp(selectedIndex, 0, items.Count - 1);
    }

    private void OnValidate()
    {
        maxDifferentItems = Mathf.Max(1, maxDifferentItems);
        items.RemoveAll(slot => slot == null || string.IsNullOrWhiteSpace(slot.ItemId) || slot.Amount <= 0);
        ClampSelection();
    }
}
