using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;

public class CutsceneUIController : MonoBehaviour
{
    void Start()
    {
        // Unlock dan tampilkan cursor saat di cutscene
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void BackToHome()
    {
        // Shutdown NetworkManager jika masih aktif
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.Shutdown();
        }

        // Kembali ke StartMenu
        SceneManager.LoadScene("StartMenu");
    }
}
