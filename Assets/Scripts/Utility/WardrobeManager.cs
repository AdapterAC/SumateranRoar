using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Linq;

public class WardrobeManager : NetworkBehaviour
{
    // A helper class to organize parts in the inspector
    [System.Serializable]
    public class BodyPartGroup
    {
        public string name;
        // We will now reference the renderers directly for better performance
        public List<SkinnedMeshRenderer> parts;
    }

    [Header("Modular Parts References")]
    public List<BodyPartGroup> armorGroups;
    public List<BodyPartGroup> bodyGroups;

    // --- Network Variables for Synchronization ---
    private NetworkVariable<int> armId = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private NetworkVariable<int> beltId = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private NetworkVariable<int> chestId = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private NetworkVariable<int> feetId = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private NetworkVariable<int> headId = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private NetworkVariable<int> legsId = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private NetworkVariable<int> earsId = new NetworkVariable<int>(1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private NetworkVariable<int> eyebrowId = new NetworkVariable<int>(1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private NetworkVariable<int> eyesId = new NetworkVariable<int>(1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private NetworkVariable<int> faceHairId = new NetworkVariable<int>(1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private NetworkVariable<int> hairId = new NetworkVariable<int>(1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private NetworkVariable<int> noseId = new NetworkVariable<int>(1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    private Dictionary<string, (NetworkVariable<int> netVar, List<SkinnedMeshRenderer> renderers)> partMap;
    private bool isInitialized = false;

    public override void OnNetworkSpawn()
    {
        // All clients need to subscribe to network variable changes to see others' customizations
        EnsureInitialized();
        foreach (var entry in partMap)
        {
            entry.Value.netVar.OnValueChanged += (prev, current) => OnPartChanged(entry.Key, current);
        }

        // The owner is responsible for loading their own saved appearance
        if (IsOwner)
        {
            InitializeVisuals();
        }
        
        // Immediately update visuals for all clients based on the current network state
        // This ensures late-joining clients see the correct appearance
        foreach (var entry in partMap)
        {
            OnPartChanged(entry.Key, entry.Value.netVar.Value);
        }
    }

    /// <summary>
    /// Ensures the part map is initialized. Can be called multiple times safely.
    /// </summary>
    private void EnsureInitialized()
    {
        if (isInitialized) return;
        InitializePartMap();
        isInitialized = true;
    }

    /// <summary>
    /// Initializes the player's appearance based on saved data, or sets defaults.
    /// This should be called by the owner on spawn, or by local scenes like the profile editor.
    /// </summary>
    public void InitializeVisuals()
    {
        EnsureInitialized();

        // Check if profile has been set up before. If not, set defaults and randomize.
        if (PlayerPrefs.GetInt("ProfileInitialized_v2", 0) == 0)
        {
            Debug.Log("First-time initialization detected.");
            SetDefaultAndRandomizeAppearance();
            PlayerPrefs.SetInt("ProfileInitialized_v2", 1);
            PlayerPrefs.Save();
        }
        else
        {
            Debug.Log("Loading existing profile.");
            LoadAppearance();
        }

        // Force visual update after a short delay to ensure all renderers are ready
        // This is especially important for offline scenes
        StartCoroutine(ForceVisualUpdateCoroutine());
    }

    private System.Collections.IEnumerator ForceVisualUpdateCoroutine()
    {
        yield return null; // Wait one frame
        
        Debug.Log("Forcing visual update after delay.");
        foreach (var entry in partMap)
        {
            // Re-read from PlayerPrefs to ensure we have the correct value
            string saveKey = entry.Key.ToUpper();
            bool isArmor = armorGroups.Any(g => g.name.ToUpper() == entry.Key.ToUpper());
            int defaultValue = isArmor ? 0 : 1;
            int savedId = PlayerPrefs.GetInt($"Wardrobe_{saveKey}", defaultValue);
            
            Debug.Log($"Force updating {entry.Key} to ID {savedId}");
            OnPartChanged(entry.Key, savedId);
        }
    }

    private void InitializePartMap()
    {
        partMap = new Dictionary<string, (NetworkVariable<int> netVar, List<SkinnedMeshRenderer> renderers)>
        {
            { "ARM", (armId, GetGroupByName("ARM")?.parts) },
            { "BELT", (beltId, GetGroupByName("BELT")?.parts) },
            { "CHEST", (chestId, GetGroupByName("CHEST")?.parts) },
            { "FEET", (feetId, GetGroupByName("FEET")?.parts) },
            { "HEAD", (headId, GetGroupByName("HEAD")?.parts) },
            { "LEGS", (legsId, GetGroupByName("LEGS")?.parts) },
            { "Ears", (earsId, GetGroupByName("Ears")?.parts) },
            { "Eyebrow", (eyebrowId, GetGroupByName("Eyebrow")?.parts) },
            { "Eyes", (eyesId, GetGroupByName("Eyes")?.parts) },
            { "FaceHair", (faceHairId, GetGroupByName("FaceHair")?.parts) },
            { "Hair", (hairId, GetGroupByName("Hair")?.parts) },
            { "Nose", (noseId, GetGroupByName("Nose")?.parts) }
        };
    }

    public BodyPartGroup GetGroupByName(string name)
    {
        var group = armorGroups.FirstOrDefault(g => g.name.ToUpper() == name.ToUpper());
        if (group == null)
        {
            group = bodyGroups.FirstOrDefault(g => g.name.ToUpper() == name.ToUpper());
        }
        return group;
    }

    private void OnPartChanged(string partKey, int newId)
    {
        if (partMap == null || !partMap.TryGetValue(partKey, out var entry) || entry.renderers == null)
        {
            return; // Not initialized or group not found
        }

        for (int i = 0; i < entry.renderers.Count; i++)
        {
            var renderer = entry.renderers[i];
            if (renderer != null) // Null check to prevent errors
            {
                // ID is 1-based, index is 0-based. ID 0 means unequipped.
                bool isActive = (i == newId - 1);
                renderer.enabled = isActive;
            }
        }
    }

    public void ChangeAndSavePart(string partKey, int newId)
    {
        // Fix: Check for null Singleton to avoid NRE in offline scenes
        // If NetworkManager.Singleton is null, we are definitely offline.
        bool isOffline = NetworkManager.Singleton == null || !NetworkManager.Singleton.IsClient;

        if (IsOwner || isOffline)
        {
            EnsureInitialized();
            if (partMap.TryGetValue(partKey, out var entry))
            {
                // Only update NetworkVariable if we are online and owner
                if(IsOwner && NetworkManager.Singleton != null) 
                {
                    entry.netVar.Value = newId;
                }
                
                // Use consistent key format (uppercase)
                PlayerPrefs.SetInt($"Wardrobe_{partKey.ToUpper()}", newId);
                PlayerPrefs.Save();
                
                OnPartChanged(partKey, newId);
            }
        }
    }

    private void SetDefaultAndRandomizeAppearance()
    {
        Debug.Log("First time setup: Setting default and randomized appearance.");
        foreach (var armorGroup in armorGroups)
        {
            string key = armorGroup.name.ToUpper();
            if (partMap.TryGetValue(key, out var entry))
            {
                if(IsOwner && NetworkManager.Singleton != null) entry.netVar.Value = 0;
                // Use consistent key format (uppercase)
                PlayerPrefs.SetInt($"Wardrobe_{key}", 0);
            }
        }

        foreach (var bodyGroup in bodyGroups)
        {
            string key = bodyGroup.name.ToUpper();
            if (partMap.TryGetValue(key, out var entry))
            {
                if (entry.renderers != null && entry.renderers.Count > 0)
                {
                    int randomId = Random.Range(1, entry.renderers.Count + 1);
                    if(IsOwner && NetworkManager.Singleton != null) entry.netVar.Value = randomId;
                    // Use consistent key format (uppercase)
                    PlayerPrefs.SetInt($"Wardrobe_{key}", randomId);
                }
            }
        }
    }

    private void LoadAppearance()
    {
        Debug.Log("Loading saved appearance.");
        foreach (var entry in partMap)
        {
            // Use consistent key format (uppercase from partMap)
            string saveKey = entry.Key.ToUpper();
            
            // Determine default value: 0 for armor, 1 for body parts
            bool isArmor = armorGroups.Any(g => g.name.ToUpper() == entry.Key.ToUpper());
            int defaultValue = isArmor ? 0 : 1;

            int loadedId = PlayerPrefs.GetInt($"Wardrobe_{saveKey}", defaultValue);
            
            Debug.Log($"Loading {entry.Key}: ID = {loadedId} (default: {defaultValue})");
            
            // Only update NetworkVariable if we are online and owner
            if(IsOwner && NetworkManager.Singleton != null) 
            {
                entry.Value.netVar.Value = loadedId;
            }
            
            OnPartChanged(entry.Key, loadedId);
        }
    }

    [ContextMenu("Validate All Part References")]
    private void ValidateReferences()
    {
        Debug.Log("--- Starting Wardrobe Validation ---");
        bool allGood = true;

        foreach (var group in armorGroups.Concat(bodyGroups))
        {
            if (group == null || string.IsNullOrEmpty(group.name))
            {
                Debug.LogError("A group is null or has no name!");
                allGood = false;
                continue;
            }

            if (group.parts == null)
            {
                 Debug.LogError($"Group '{group.name}' has a null parts list!");
                 allGood = false;
                 continue;
            }

            for (int i = 0; i < group.parts.Count; i++)
            {
                if (group.parts[i] == null)
                {
                    Debug.LogError($"<b>MISSING REFERENCE:</b> Group '<b>{group.name}</b>' has a null entry at <b>Element {i}</b>.", this);
                    allGood = false;
                }
            }
        }

        if (allGood)
        {
            Debug.Log("<color=green><b>Validation Complete:</b> All references look good!</color>", this);
        }
        else
        {
             Debug.LogWarning("Validation finished with errors. Check the logs above.", this);
        }
        Debug.Log("--- End of Wardrobe Validation ---");
    }
}
