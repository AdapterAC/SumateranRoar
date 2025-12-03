using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class SpawnArea : MonoBehaviour
{
    [Tooltip("Radius area bermain (meter). Titik pusat = posisi GameObject ini.")]
    public float radius = 100f;

    [Tooltip("Gunakan tinggi (Y) tetap sesuai posisi GameObject ini. Jika aktif, tidak memakai Terrain/Raycast.")]
    public bool useFixedHeight = true;

    [Tooltip("Jika tidak ada Terrain, coba cari ketinggian tanah dengan Physics.Raycast ke bawah.")]
    public bool fallbackPhysicsRaycast = true;

    [Tooltip("LayerMask untuk raycast ground (berlaku jika fallbackPhysicsRaycast = true).")]
    public LayerMask groundMask = ~0;

    [Tooltip("Offset Y setelah penempatan (opsional), misal 0.1 agar tidak clip.")]
    public float yOffset = 1.0f;
    
    [Tooltip("Jangan destroy SpawnArea ini saat scene berubah")]
    public bool persistAcrossScenes = false;

    private void Awake()
    {
        // Pastikan SpawnArea tidak di-destroy jika diperlukan
        if (persistAcrossScenes)
        {
            DontDestroyOnLoad(gameObject);
        }
    }

    /// <summary>
    /// Mendapatkan titik random dalam area spawn yang valid di atas ground/terrain.
    /// </summary>
    public bool TryGetRandomPoint(out Vector3 point)
    {
        // Ambil titik acak dalam lingkaran (sumbu XZ)
        Vector2 r = Random.insideUnitCircle * radius;
        Vector3 center = transform.position;
        Vector3 pos = new Vector3(center.x + r.x, center.y, center.z + r.y);

        // Jika tinggi tetap diminta, langsung pakai Y pusat (horizontal only)
        if (useFixedHeight)
        {
            pos.y = center.y + yOffset;
            point = pos;
            return true;
        }

        // Coba pakai Terrain jika ada
        Terrain terrain = Terrain.activeTerrain;
        if (terrain != null)
        {
            float y = terrain.SampleHeight(pos) + terrain.transform.position.y + yOffset;
            pos.y = y;
            point = pos;
            return true;
        }

        // Fallback: raycast dari atas ke bawah
        if (fallbackPhysicsRaycast)
        {
            Vector3 rayOrigin = pos + Vector3.up * 2000f;
            if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, 4000f, groundMask, QueryTriggerInteraction.Ignore))
            {
                pos.y = hit.point.y + yOffset;
                point = pos;
                return true;
            }
        }

        // Jika tidak ada info ground, gunakan Y pusat area + offset
        pos.y = center.y + yOffset;
        point = pos;
        return true;
    }
    
    /// <summary>
    /// Mendapatkan titik random dengan validasi tambahan untuk memastikan posisi aman.
    /// </summary>
    public bool TryGetValidatedRandomPoint(out Vector3 point, int maxAttempts = 10)
    {
        for (int i = 0; i < maxAttempts; i++)
        {
            if (TryGetRandomPoint(out Vector3 candidatePoint))
            {
                // Validasi: pastikan ada ground di bawah
                if (IsPositionValid(candidatePoint))
                {
                    point = candidatePoint;
                    return true;
                }
            }
        }
        
        // Fallback ke center
        point = transform.position + Vector3.up * yOffset;
        return true;
    }
    
    /// <summary>
    /// Cek apakah posisi valid (ada ground di bawah, tidak di void)
    /// </summary>
    private bool IsPositionValid(Vector3 position)
    {
        // Cek dengan raycast ke bawah
        Vector3 rayOrigin = position + Vector3.up * 10f;
        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, 50f, groundMask, QueryTriggerInteraction.Ignore))
        {
            // Posisi valid jika hit point tidak terlalu jauh dari posisi yang diminta
            float heightDiff = Mathf.Abs(hit.point.y - (position.y - yOffset));
            return heightDiff < 20f; // Toleransi 20 meter
        }
        
        // Cek dengan Terrain
        Terrain terrain = Terrain.activeTerrain;
        if (terrain != null)
        {
            float terrainHeight = terrain.SampleHeight(position) + terrain.transform.position.y;
            // Pastikan posisi berada di atas terrain
            return position.y >= terrainHeight - 1f;
        }
        
        return false;
    }

    private void OnDrawGizmosSelected()
    {
        // Tampilkan radius sebagai lingkaran datar (2D) di bidang XZ
    #if UNITY_EDITOR
        Handles.color = new Color(0f, 1f, 1f, 0.9f);
        Handles.DrawWireDisc(transform.position, Vector3.up, radius);
        
        // Tampilkan area dengan warna berbeda
        Handles.color = new Color(0f, 1f, 0f, 0.2f);
        Handles.DrawSolidDisc(transform.position, Vector3.up, radius);
    #else
        // Fallback saat bukan di Editor (jarang diperlukan untuk gizmo)
        Gizmos.color = new Color(0f, 1f, 1f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, radius);
    #endif
    }
}
