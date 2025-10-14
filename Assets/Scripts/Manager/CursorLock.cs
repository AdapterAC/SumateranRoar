using UnityEngine;

public class CursorLock : MonoBehaviour
{
    void Start()
    {
        // Kunci dan sembunyikan kursor saat game dijalankan
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        // Tekan Esc untuk melepas kursor (keluar dari lock)
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        // Tekan klik kiri untuk mengunci kembali kursor ke game
        if (Input.GetMouseButtonDown(0))
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}
