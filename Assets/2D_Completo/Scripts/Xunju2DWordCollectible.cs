using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Xunju2DWordCollectible : MonoBehaviour
{
    [SerializeField] private string mazahuaWord;
    [SerializeField] private string spanishMeaning;
    [SerializeField] private float bobHeight = 0.12f;
    [SerializeField] private float bobSpeed = 2.1f;

    private Vector3 startPosition;
    private bool collected;

    public void Configure(string word, string meaning)
    {
        mazahuaWord = word;
        spanishMeaning = meaning;
        gameObject.name = "Palabra_2D_" + word;
    }

    void Start()
    {
        startPosition = transform.position;
        Collider2D collider = GetComponent<Collider2D>();
        collider.isTrigger = true;
    }

    void Update()
    {
        if (collected)
            return;

        transform.position = startPosition + Vector3.up * (Mathf.Sin(Time.time * bobSpeed) * bobHeight);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (collected)
            return;

        if (other.GetComponentInParent<Xunju2DPlayerController>() == null)
            return;

        collected = true;
        Xunju2DGameManager manager = FindFirstObjectByType<Xunju2DGameManager>();
        if (manager != null)
            manager.CollectMazahuaWord(mazahuaWord, spanishMeaning);

        Destroy(gameObject);
    }
}
