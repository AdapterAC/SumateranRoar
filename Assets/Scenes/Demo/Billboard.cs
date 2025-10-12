using UnityEngine;

public class BillboardText : MonoBehaviour {
    void LateUpdate() {
        if (Camera.main == null) return;

        // Selalu hadap ke kamera
        transform.LookAt(Camera.main.transform);

        // Balik arah supaya tidak terbalik
        transform.Rotate(0, 180, 0);
    }
}
