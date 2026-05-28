using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Construieste obiecte CardData (runtime) pornind de la datele din PlayFab.
/// Folosit atat de Deck Builder (colectia ta) cat si, conceptual, de meci.
/// Statisticile vin 100% din cloud; numele tot din cloud (DisplayName);
/// poza/clasa/status din asset-ul local daca exista (poza nu poate veni din PlayFab).
/// </summary>
public static class CloudCardFactory
{
    public static CardData BuildFromCloud(string itemId)
    {
        var inv = PlayFabInventoryManager.Instance;
        if (inv == null || string.IsNullOrEmpty(itemId)) return null;

        CloudCardStats stats;
        if (!inv.cardStatsDatabase.TryGetValue(itemId, out stats) || stats == null)
            return null;

        CardData rt = ScriptableObject.CreateInstance<CardData>();
        rt.cardID = itemId;
        rt.attack = ParseInt(stats.Attack);
        rt.hp = ParseInt(stats.Health);
        rt.manaCost = ParseInt(stats.ManaCost);
        rt.flavorText = stats.flavorText;

        CardData local = (DatabaseManager.Instance != null) ? DatabaseManager.Instance.GetCardByID(itemId) : null;
        if (local != null)
        {
            rt.cardName = !string.IsNullOrWhiteSpace(local.cardName) ? local.cardName : itemId;
            rt.artwork = local.artwork;
            rt.cardClass = local.cardClass;
            rt.status = local.status;
        }
        else
        {
            rt.cardName = itemId;
        }

        if (!string.IsNullOrWhiteSpace(stats.Class)) rt.cardClass = stats.Class;
        if (!string.IsNullOrWhiteSpace(stats.Status)) rt.status = stats.Status;
        if (!string.IsNullOrWhiteSpace(stats.DisplayName)) rt.cardName = stats.DisplayName;

        return rt;
    }

    // Colectia jucatorului (cartile detinute), fiecare carte unica o singura data.
    public static List<CardData> BuildOwnedCollection()
    {
        List<CardData> list = new List<CardData>();
        var inv = PlayFabInventoryManager.Instance;
        if (inv == null || inv.ownedCards == null) return list;

        foreach (string id in inv.ownedCards.Distinct())
        {
            CardData c = BuildFromCloud(id);
            if (c != null) list.Add(c);
        }
        return list;
    }

    private static int ParseInt(string s)
    {
        return int.TryParse(s, out int v) ? v : 0;
    }
}