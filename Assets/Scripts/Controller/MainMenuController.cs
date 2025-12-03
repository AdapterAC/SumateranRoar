using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    [SerializeField] private Button hostButton;
    [SerializeField] private Button joinButton;
    [SerializeField] private TMP_InputField joinCodeInput;
    [SerializeField] private Button backButton;

    // Start is called before the first frame update
    void Start()
    {
        ScreenLogger.Log("Start - MainMenu berhasil ditampilkan", ScreenLogger.LogType.Success);
        hostButton.onClick.AddListener(async () => {
            string joinCode = await RelayManager.Instance.StartHostWithRelay(4, "dtls");
            Debug.Log($"Host started with join code: {joinCode}");
            SceneManager.LoadScene("Lobby");
        });

        joinButton.onClick.AddListener(async () => {
            string joinCode = joinCodeInput.text;
            bool success = await RelayManager.Instance.StartClientWithRelay(joinCode, "dtls");
            Debug.Log($"Join button clicked. Success: {success}");
            SceneManager.LoadScene("Lobby");
        });
        backButton.onClick.AddListener(() => {
            SceneManager.LoadScene("StartMenu");
        });
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
