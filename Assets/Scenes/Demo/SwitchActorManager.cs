using UnityEngine;

public class SwitchActorManager : MonoBehaviour {
    [Header("Scene Objects")]
    public GameObject player;   // drag Player di scene
    public GameObject tiger;    // drag Tiger di scene

    [Header("Swap")]
    public float switchDistance = 2.5f;
    public KeyCode switchKey = KeyCode.E;

    public GameObject Current { get; private set; }

    void Start() {
        SetCurrent(player); // default: Player
    }

    void Update() {
        if (!player || !tiger || !Current) return;

        GameObject other = (Current == player) ? tiger : player;
        if (Vector3.Distance(Current.transform.position, other.transform.position) <= switchDistance
            && Input.GetKeyDown(switchKey)) {
            SetCurrent(other);
        }
    }

    public void SetCurrent(GameObject obj) {
        if (!obj || obj == Current) return;

        // nonaktifkan semua kontrol + kamera pada keduanya
        ToggleCharacter(player, false);
        ToggleCharacter(tiger,  false);

        // aktifkan kontrol + kamera hanya pada yang dipilih
        ToggleCharacter(obj, true);

        Current = obj;
    }

    void ToggleCharacter(GameObject root, bool on) {
        if (!root) return;

        // 1) hidup/matikan semua script kontrol (MonoBehaviour)
        var monos = root.GetComponentsInChildren<MonoBehaviour>(true);
        foreach (var m in monos) m.enabled = on;

        // 2) pastikan hanya kamera & audio listener milik karakter aktif yang hidup
        var cams = root.GetComponentsInChildren<Camera>(true);
        foreach (var c in cams) c.enabled = on;

        var listeners = root.GetComponentsInChildren<AudioListener>(true);
        foreach (var al in listeners) al.enabled = on;
    }
}
