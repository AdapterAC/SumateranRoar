using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using TMPro;

public class ProfileUIController : MonoBehaviour
{
    // This needs to be assigned to the player character in the scene
    public WardrobeManager wardrobeManager;

    [System.Serializable]
    public class UIGroup
    {
        public string partKey; // e.g., "CHEST", "HAIR". MUST MATCH WardrobeManager
        public TextMeshProUGUI currentItemText;
        public Button nextButton;
        public Button prevButton;
        [HideInInspector] public int currentId;
        [HideInInspector] public int maxId;
    }

    public List<UIGroup> uiGroups;
    public Button backButton;

    private void Start()
    {
        // Find the local player's WardrobeManager
        // This is a simple approach; a more robust system might use a static reference
        // or pass data between scenes.
        if (wardrobeManager == null)
        {
            wardrobeManager = FindObjectOfType<WardrobeManager>();
        }

        if (wardrobeManager == null)
        {
            Debug.LogError("WardrobeManager not found in the scene! Cannot initialize UI.");
            return;
        }

        // Manually initialize the visuals based on saved PlayerPrefs
        // This is crucial for offline scenes like this one.
        wardrobeManager.InitializeVisuals();

        InitializeUI();

        backButton.onClick.AddListener(GoToStartMenu);
    }

    private void InitializeUI()
    {
        foreach (var group in uiGroups)
        {
            // Get the initial ID from PlayerPrefs, which should be what the player is wearing
            group.currentId = PlayerPrefs.GetInt($"Wardrobe_{group.partKey}", 0);

            // Determine the max ID from the WardrobeManager's configuration
            var partData = wardrobeManager.GetGroupByName(group.partKey);
            group.maxId = (partData != null) ? partData.parts.Count : 0;

            // Add listeners to the buttons, passing the group itself as a parameter
            group.nextButton.onClick.AddListener(() => ChangePart(group, 1));
            group.prevButton.onClick.AddListener(() => ChangePart(group, -1));

            UpdateItemText(group);
        }
    }

    private void ChangePart(UIGroup group, int direction)
    {
        if (group.maxId == 0) return; // No items in this group

        group.currentId += direction;

        // Check if the part is armor (can be unequipped, ID 0) or body (cannot, ID > 0)
        bool isArmor = wardrobeManager.armorGroups.Exists(g => g.name.ToUpper() == group.partKey.ToUpper());
        int minId = isArmor ? 0 : 1;

        // Loop around
        if (group.currentId > group.maxId)
        {
            group.currentId = minId;
        }
        if (group.currentId < minId)
        {
            group.currentId = group.maxId;
        }

        // Update the text
        UpdateItemText(group);

        // Tell the WardrobeManager on the player to update the appearance
        wardrobeManager.ChangeAndSavePart(group.partKey, group.currentId);
    }

    private void UpdateItemText(UIGroup group)
    {
        if (group.currentId == 0)
        {
            group.currentItemText.text = "Unequipped";
        }
        else
        {
            group.currentItemText.text = $"{group.currentId} / {group.maxId}";
        }
    }

    public void GoToStartMenu()
    {
        // Here you can add a "Saving..." screen if you want
        SceneManager.LoadScene("StartMenu");
    }
}
