using System.Collections.Generic;
using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;
using Unity.VisualScripting;
using static UnityEditor.Progress;

// --- STATS CLASS (Must exactly match Custom Data in PlayFab) ---
[System.Serializable]
public class CloudCardStats
{
    // We leave them as strings because PlayFab sends the data as text
    public string Attack;
    public string Health;
    public string ManaCost;
    public string Description;
}

[System.Serializable]
public struct CardMapping
{
    public string playFabItemId;
    public GameObject cardPrefab;
}

public class PlayFabInventoryManager : MonoBehaviour
{
    public static PlayFabInventoryManager Instance;

    [Header("1. What the player owns")]
    public List<string> ownedCards = new List<string>();

    [Header("2. Visual Database (Assigned by you in the Inspector)")]
    public List<CardMapping> visualDatabase;

    // 3. The invisible dictionary with stats downloaded from the Cloud
    public Dictionary<string, CloudCardStats> cardStatsDatabase = new Dictionary<string, CloudCardStats>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // --- STEP 1: DOWNLOAD THE ENCYCLOPEDIA (CATALOG) ---
    // Call this function from PlayFabAuthManager on OnLoginSuccess!
    public void GetCatalogAndInventory()
    {
        Debug.Log("Step 1: Downloading card stats from the cloud...");
        var request = new GetCatalogItemsRequest
        {
            CatalogVersion = "Test_version" // Make sure this matches your catalog name on the website
        };
        PlayFabClientAPI.GetCatalogItems(request, OnCatalogSuccess, OnError);
    }

    private void OnCatalogSuccess(GetCatalogItemsResult result)
    {
        cardStatsDatabase.Clear(); // Clear old data

        foreach (var item in result.Catalog)
        {
            // If the item is a card and has Custom Data on the website
            if (item.ItemClass == "Card" && !string.IsNullOrEmpty(item.CustomData))
            {
                // Translate the JSON received from the server directly into our Unity class
                CloudCardStats stats = JsonUtility.FromJson<CloudCardStats>(item.CustomData);

                // Save in the dictionary (e.g., the key "card_warrior" now has attack, health, etc.)
                cardStatsDatabase.Add(item.ItemId, stats);
            }
        }

        Debug.Log($"<color=orange>Step 1 Complete: Downloaded stats for {cardStatsDatabase.Count} cards.</color>");

        // Now that we know the global stats, download the player's inventory
        GetPlayerInventory();
    }

    // --- STEP 2: DOWNLOAD THE PLAYER'S INVENTORY ---
    private void GetPlayerInventory()
    {
        Debug.Log("Step 2: Checking what cards the player owns...");
        var request = new GetUserInventoryRequest();
        PlayFabClientAPI.GetUserInventory(request, OnInventorySuccess, OnError);
    }

    private void OnInventorySuccess(GetUserInventoryResult result)
    {
        ownedCards.Clear();

        foreach (var item in result.Inventory)
        {
            // This condition automatically ignores your "test_deck" bundle!
            if (item.ItemClass == "Card")
            {
                ownedCards.Add(item.ItemId);
            }
        }
        Debug.Log($"<color=cyan>Step 2 Complete: The player owns {ownedCards.Count} cards ready for battle!</color>");

        foreach(string itemId in ownedCards)
    {
            CloudCardStats stats = GetCloudStatsByID(itemId);
            if (stats != null)
            {
                Debug.Log($"<color=white>[TEST CLOUD] Card: {itemId} | Attack: {stats.Attack} | HP: {stats.Health} | Mana: {stats.ManaCost}</color>");
            }
            else
            {
                Debug.LogWarning($"<color=yellow>[TEST CLOUD] Card: {itemId} has no stats in the cloud database!</color>");
            }
        }
    }
    
    private void OnError(PlayFabError error)
    {
        Debug.LogError("PlayFab Error: " + error.GenerateErrorReport());
    }

    // --- FUNCTIONS FOR TEAM MEMBERS (HANDOFF) ---

    // Member 4 will use this to get the visual (the Prefab)
    public GameObject GetVisualPrefabByID(string itemId)
    {
        foreach (var mapping in visualDatabase)
        {
            if (mapping.playFabItemId == itemId) return mapping.cardPrefab;
        }
        return null;
    }

    // Member 4 will use this to read the stats from the cloud!
    public CloudCardStats GetCloudStatsByID(string itemId)
    {
        if (cardStatsDatabase.ContainsKey(itemId))
        {
            return cardStatsDatabase[itemId];
        }
        Debug.LogWarning("Could not find cloud stats for: " + itemId);
        return null;
    }


}