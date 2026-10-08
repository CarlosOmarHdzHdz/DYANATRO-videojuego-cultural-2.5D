using System;
using System.Collections;
using UnityEngine;

// Interaccion no violenta: el ejemplar se registra en la Ludoteca y se retira
// de la escena. La mision escucha el evento sin depender del prefab concreto.
[DisallowMultipleComponent]
public sealed class XunjuuAnimalCapture : MonoBehaviour
{
    [SerializeField] private string faunaId;
    [SerializeField, Range(1.5f, 5f)] private float captureDistance = 2.8f;
    private bool captured;

    public string FaunaId => faunaId;
    public float CaptureDistance => captureDistance;
    public bool IsAvailable => !captured && isActiveAndEnabled && gameObject.activeInHierarchy;
    public XunjuuFaunaCatalog.Entry Entry => XunjuuFaunaCatalog.Find(faunaId);

    public void Configure(string newFaunaId)
    {
        faunaId = newFaunaId;
    }

    private void Awake()
    {
        if (XunjuuFaunaCatalog.Find(faunaId) == null)
            faunaId = XunjuuFaunaCatalog.FindForName(name).Id;
    }

    public void Capture()
    {
        if (!IsAvailable)
            return;

        captured = true;
        XunjuuFaunaCatalog.Entry entry = Entry;
        bool newCard = XunjuuFaunaCatalog.RegisterCapture(faunaId);
        StartCoroutine(CaptureRoutine());
        XunjuuAnimalCaptureEvents.Report(gameObject, faunaId);
        XunjuuFaunaCaptureSystem.NotifyCapture(entry != null ? entry.DisplayName : name, newCard);
    }

    private IEnumerator CaptureRoutine()
    {
        Animal animal = GetComponent<Animal>();
        if (animal != null) animal.enabled = false;
        Animator animator = GetComponent<Animator>();
        if (animator != null) animator.enabled = false;
        foreach (Collider shape in GetComponentsInChildren<Collider>(true))
            shape.enabled = false;

        Rigidbody body = GetComponent<Rigidbody>();
        if (body != null)
        {
            body.linearVelocity = Vector3.zero;
            body.isKinematic = true;
            body.useGravity = false;
        }

        Vector3 initialScale = transform.localScale;
        Vector3 initialPosition = transform.position;
        const float duration = .32f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            transform.localScale = initialScale * Mathf.Lerp(1f, .08f, progress * progress);
            transform.position = initialPosition + Vector3.up * Mathf.Sin(progress * Mathf.PI) * .65f;
            yield return null;
        }
        Destroy(gameObject);
    }
}

public static class XunjuuAnimalCaptureEvents
{
    public static event Action<GameObject, string> AnimalCaptured;

    public static void Report(GameObject animal, string faunaId)
    {
        if (animal != null)
            AnimalCaptured?.Invoke(animal, faunaId);
    }
}
