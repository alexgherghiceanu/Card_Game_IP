using System;

/// <summary>
/// O instanta "vie" a unei carti, asa cum exista ea pe tabla de joc in timpul unui meci.
/// Spre deosebire de CardData (care e doar sablonul/definitia, neschimbabil), aceasta
/// retine starea care se modifica in meci: HP-ul curent, daca a atacat deja in tura asta,
/// si daca tocmai a fost invocata (nu poate ataca in prima tura - "summoning sickness").
/// </summary>
[Serializable]
public class RuntimeCardInstance
{
    public CardData cardData;
    public int currentHP;
    public bool hasAttackedThisTurn;

    // NOU (Etapa 1): o creatura tocmai invocata nu poate ataca in tura in care a fost jucata.
    public bool justSummoned;

    public RuntimeCardInstance(CardData sourceCard)
    {
        cardData = sourceCard;
        currentHP = (sourceCard != null) ? sourceCard.hp : 0;
        hasAttackedThisTurn = false;
        justSummoned = true; // devine false la inceputul urmatoarei ture a proprietarului
    }

    public int Attack => (cardData != null) ? cardData.attack : 0;

    /// <summary>Creatura are statusul "Imobil"? Atunci nu poate ataca niciodata.</summary>
    public bool EsteImobil()
    {
        return cardData != null
            && !string.IsNullOrWhiteSpace(cardData.status)
            && cardData.status.Trim().Equals("Imobil", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Poate aceasta creatura sa atace acum?</summary>
    public bool PoateAtaca()
    {
        return currentHP > 0
            && !hasAttackedThisTurn
            && !justSummoned
            && !EsteImobil();
    }

    public bool EsteMoarta()
    {
        return currentHP <= 0;
    }

    public string GetCardName()
    {
        if (cardData == null) return "Unknown Card";
        if (!string.IsNullOrWhiteSpace(cardData.cardName)) return cardData.cardName;
        return cardData.cardID;
    }
}