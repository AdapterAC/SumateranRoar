using UnityEngine;
using UnityEngine.SceneManagement;

public class StartMenuController : MonoBehaviour
{
    public void PlayButton_OnClick()
    {
        // Loads the existing MainMenu scene
        SceneManager.LoadScene("MainMenu");
    }

    public void ProfileButton_OnClick()
    {
        // TODO: Implement logic for the Profile menu
        Debug.Log("Profile button clicked. Not implemented yet.");
    }

    public void SettingsButton_OnClick()
    {
        // TODO: Implement logic for the Settings menu
        Debug.Log("Settings button clicked. Not implemented yet.");
    }

    public void QuitButton_OnClick()
    {
        // Quit the application
        Debug.Log("Quitting application...");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
