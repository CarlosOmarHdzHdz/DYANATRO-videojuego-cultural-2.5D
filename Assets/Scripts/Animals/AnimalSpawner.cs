using UnityEngine;
using System.Collections.Generic;

// ============================================
// SPAWNER DE ANIMALES - ALREDEDOR DEL JUGADOR
// ============================================

public class AnimalSpawner : MonoBehaviour
{
    [Header("=== CONFIGURACIÓN ===")]
    [SerializeField] private GameObject[] animalPrefabs;     // Diferentes tipos de animales
    [SerializeField] private bool spawnOnStart;              // Beta v0.1: la mision activa sus propios animales
    [SerializeField] private float spawnRadius = 20f;        // Radio alrededor del jugador
    [SerializeField] private int minAnimals = 5;             // Mínimo de animales
    [SerializeField] private int maxAnimals = 8;             // Máximo de animales

    [Header("=== RESPAWN ===")]
    [SerializeField] private float respawnTime = 30f;        // Tiempo para respawnear animales
    [SerializeField] private float minDistanceFromPlayer = 10f; // Distancia mínima para spawnear

    private List<GameObject> spawnedAnimals = new List<GameObject>();
    private Transform player;
    private float nextRespawnCheck = 0f;
    private bool spawnerActive;

    void Start()
    {
        player = transform; // El spawner está en el jugador
        spawnerActive = spawnOnStart;
        if (spawnerActive)
            SpawnInitialAnimals();
    }

    void Update()
    {
        if (!spawnerActive)
            return;

        // Revisar cada cierto tiempo si hay que respawnear
        if (Time.time >= nextRespawnCheck)
        {
            nextRespawnCheck = Time.time + respawnTime;
            CheckAndRespawn();
        }
    }

    // Xunjuu v0.1 - ACCION: habilitar este spawner solo desde una mision futura.
    public void ActivateSpawner()
    {
        if (spawnerActive)
            return;

        spawnerActive = true;
        SpawnInitialAnimals();
    }

    // Xunjuu v0.1 - ACCION: detener reapariciones y retirar animales ambientales.
    public void DeactivateSpawner()
    {
        spawnerActive = false;
        ClearAllAnimals();
    }

    void SpawnInitialAnimals()
    {
        int animalCount = Random.Range(minAnimals, maxAnimals + 1);
        Debug.Log($"🦌 Generando {animalCount} animales alrededor del jugador");

        for (int i = 0; i < animalCount; i++)
        {
            SpawnSingleAnimal();
        }
    }

    void SpawnSingleAnimal()
    {
        if (animalPrefabs.Length == 0)
        {
            Debug.LogError("❌ No hay prefabs de animales asignados");
            return;
        }

        // Elegir animal aleatorio
        GameObject animalPrefab = animalPrefabs[Random.Range(0, animalPrefabs.Length)];

        // Posición aleatoria alrededor del jugador
        Vector3 randomPosition = GetRandomPositionAroundPlayer();

        // Crear el animal
        GameObject animal = Instantiate(animalPrefab, randomPosition, Quaternion.identity);
        spawnedAnimals.Add(animal);
    }

    Vector3 GetRandomPositionAroundPlayer()
    {
        // Posición aleatoria en círculo alrededor del jugador
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        float radius = Random.Range(minDistanceFromPlayer, spawnRadius);

        float x = player.position.x + Mathf.Cos(angle) * radius;
        float z = player.position.z + Mathf.Sin(angle) * radius;

        // Obtener altura del terreno
        float y = GetGroundHeight(x, z);

        return new Vector3(x, y, z);
    }

    float GetGroundHeight(float x, float z)
    {
        // Raycast para obtener altura del terreno
        RaycastHit hit;
        if (Physics.Raycast(new Vector3(x, 100f, z), Vector3.down, out hit, 200f))
        {
            return hit.point.y;
        }
        return 0f;
    }

    void CheckAndRespawn()
    {
        // Limpiar animales muertos (destruidos)
        spawnedAnimals.RemoveAll(animal => animal == null);

        // Calcular cuántos animales faltan
        int currentCount = spawnedAnimals.Count;
        int targetCount = Random.Range(minAnimals, maxAnimals + 1);

        if (currentCount < targetCount)
        {
            int toSpawn = targetCount - currentCount;
            Debug.Log($"🦌 Respawn: aparecen {toSpawn} nuevos animales");

            for (int i = 0; i < toSpawn; i++)
            {
                SpawnSingleAnimal();
            }
        }
    }

    // Método para limpiar todos los animales
    public void ClearAllAnimals()
    {
        foreach (GameObject animal in spawnedAnimals)
        {
            if (animal != null)
                Destroy(animal);
        }
        spawnedAnimals.Clear();
    }

    void OnDrawGizmosSelected()
    {
        if (player != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(player.position, spawnRadius);

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(player.position, minDistanceFromPlayer);
        }
    }
}
