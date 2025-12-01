using System.Collections.Generic;
using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    [Header("Spawn Areas")]
    [Tooltip("Area spawn untuk Human. Pilih salah satu secara acak.")]
    public SpawnArea[] humanSpawnAreas;
    
    [Tooltip("Area spawn untuk Tiger. Pilih salah satu secara acak.")]
    public SpawnArea[] tigerSpawnAreas;

    [Header("Prefabs")]
    public GameObject humanPrefab;
    public GameObject tigerPrefab;

    [Header("Jumlah Spawn")]
    public int humanCount = 5;
    public int tigerCount = 2;

    [Header("Aturan Jarak")] 
    [Tooltip("Jarak minimal antara harimau dan manusia.")]
    public float minTigerToHumanDistance = 20f;

    [Header("Pengaturan")]
    [Tooltip("Spawn otomatis saat Start.")]
    public bool spawnOnStart = false;

    [Tooltip("Bersihkan spawn lama sebelum spawn baru.")]
    public bool clearBeforeSpawn = true;

    [Tooltip("Percobaan maksimal mencari titik valid per entity.")]
    public int maxAttemptsPerSpawn = 100;

    [Header("Parent (Opsional)")]
    public Transform humansParent;
    public Transform tigersParent;

    private readonly List<Transform> spawnedHumans = new List<Transform>();
    private readonly List<Transform> spawnedTigers = new List<Transform>();

    private void Reset()
    {
        // Auto-assign jika ada SpawnArea di GameObject yang sama
        var localArea = GetComponent<SpawnArea>();
        if (localArea != null)
        {
            if (humanSpawnAreas == null || humanSpawnAreas.Length == 0)
                humanSpawnAreas = new SpawnArea[] { localArea };
            if (tigerSpawnAreas == null || tigerSpawnAreas.Length == 0)
                tigerSpawnAreas = new SpawnArea[] { localArea };
        }
    }

    private void Start()
    {
        if (spawnOnStart)
        {
            SpawnNow();
        }
    }

    [ContextMenu("Spawn Now")]
    public void SpawnNow()
    {
        if ((humanSpawnAreas == null || humanSpawnAreas.Length == 0) && humanCount > 0)
        {
            Debug.LogError("SpawnManager: 'humanSpawnAreas' kosong tapi humanCount > 0. Assign minimal 1 SpawnArea untuk human.");
            return;
        }
        if ((tigerSpawnAreas == null || tigerSpawnAreas.Length == 0) && tigerCount > 0)
        {
            Debug.LogError("SpawnManager: 'tigerSpawnAreas' kosong tapi tigerCount > 0. Assign minimal 1 SpawnArea untuk tiger.");
            return;
        }

        if (clearBeforeSpawn)
            ClearSpawned();

        EnsureParents();

        // 1) Spawn semua Human (boleh berdekatan)
        for (int i = 0; i < humanCount; i++)
        {
            if (TrySpawnAtRandom(humanPrefab, humansParent, out Transform human, humanSpawnAreas))
            {
                spawnedHumans.Add(human);
            }
        }

        // 2) Spawn Harimau, jagakan jarak terhadap semua Human
        for (int i = 0; i < tigerCount; i++)
        {
            if (TrySpawnTigerWithSeparation(out Transform tiger))
            {
                spawnedTigers.Add(tiger);
            }
        }
    }

    [ContextMenu("Clear Spawned")]
    public void ClearSpawned()
    {
        // Hapus semua instance yang pernah di-spawn
        for (int i = spawnedHumans.Count - 1; i >= 0; i--)
        {
            if (spawnedHumans[i] != null)
                DestroyImmediateSafe(spawnedHumans[i].gameObject);
        }
        spawnedHumans.Clear();

        for (int i = spawnedTigers.Count - 1; i >= 0; i--)
        {
            if (spawnedTigers[i] != null)
                DestroyImmediateSafe(spawnedTigers[i].gameObject);
        }
        spawnedTigers.Clear();

        // Opsional: juga kosongkan child pada parent
        if (humansParent != null)
        {
            var toDestroy = new List<GameObject>();
            foreach (Transform t in humansParent)
                toDestroy.Add(t.gameObject);
            foreach (var go in toDestroy)
                DestroyImmediateSafe(go);
        }
        if (tigersParent != null)
        {
            var toDestroy = new List<GameObject>();
            foreach (Transform t in tigersParent)
                toDestroy.Add(t.gameObject);
            foreach (var go in toDestroy)
                DestroyImmediateSafe(go);
        }
    }

    private bool TrySpawnTigerWithSeparation(out Transform tigerOut)
    {
        tigerOut = null;

        if (tigerPrefab == null)
        {
            Debug.LogWarning("SpawnManager: tigerPrefab belum di-assign.");
            return false;
        }

        // Pilih random area untuk tiger
        SpawnArea selectedTigerArea = tigerSpawnAreas[Random.Range(0, tigerSpawnAreas.Length)];

        for (int attempt = 0; attempt < maxAttemptsPerSpawn; attempt++)
        {
            if (!selectedTigerArea.TryGetRandomPoint(out var point))
                continue;

            // Cek jarak ke semua Human
            bool farFromHumans = true;
            for (int h = 0; h < spawnedHumans.Count; h++)
            {
                var human = spawnedHumans[h];
                if (human == null) continue;
                float d = Vector3.Distance(human.position, point);
                if (d < minTigerToHumanDistance)
                {
                    farFromHumans = false;
                    break;
                }
            }

            if (!farFromHumans)
                continue;

            var go = Instantiate(tigerPrefab, point, Quaternion.identity, tigersParent);
            tigerOut = go.transform;
            return true;
        }

        Debug.LogWarning("SpawnManager: gagal menemukan lokasi harimau yang cukup jauh dari human. Pertimbangkan memperbesar radius area atau mengurangi minTigerToHumanDistance.");
        return false;
    }

    private bool TrySpawnAtRandom(GameObject prefab, Transform parent, out Transform spawned, SpawnArea[] areas)
    {
        spawned = null;
        if (prefab == null || areas == null || areas.Length == 0) return false;

        // Pilih random area
        SpawnArea selectedArea = areas[Random.Range(0, areas.Length)];

        for (int attempt = 0; attempt < maxAttemptsPerSpawn; attempt++)
        {
            if (!selectedArea.TryGetRandomPoint(out var point))
                continue;

            var go = Instantiate(prefab, point, Quaternion.identity, parent);
            spawned = go.transform;
            return true;
        }

        return false;
    }

    private void EnsureParents()
    {
        if (humansParent == null)
        {
            var humansGO = GameObject.Find("Humans") ?? new GameObject("Humans");
            humansParent = humansGO.transform;
        }
        if (tigersParent == null)
        {
            var tigersGO = GameObject.Find("Tigers") ?? new GameObject("Tigers");
            tigersParent = tigersGO.transform;
        }
    }

    private void DestroyImmediateSafe(GameObject go)
    {
        if (go == null) return;
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            DestroyImmediate(go);
            return;
        }
#endif
        Destroy(go);
    }
}
