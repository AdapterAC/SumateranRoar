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
    public float yOffset = 0.0f;

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

    private void OnDrawGizmosSelected()
    {
        // Tampilkan radius sebagai lingkaran datar (2D) di bidang XZ
    #if UNITY_EDITOR
        Handles.color = new Color(0f, 1f, 1f, 0.9f);
        Handles.DrawWireDisc(transform.position, Vector3.up, radius);
    #else
        // Fallback saat bukan di Editor (jarang diperlukan untuk gizmo)
        Gizmos.color = new Color(0f, 1f, 1f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, radius);
    #endif
    }
}
