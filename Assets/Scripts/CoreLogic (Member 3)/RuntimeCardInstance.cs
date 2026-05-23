using System;

[Serializable]
public class RuntimeCardInstance
{
    public CardData cardData;
    public int currentHP;
    public bool hasAttackedThisTurn;

    public RuntimeCardInstance(CardData sourceCard)
    {
        cardData = sourceCard;

        if (sourceCard != null)
        {
            currentHP = sourceCard.hp;
        }
        else
        {
            currentHP = 0;
        }

        hasAttackedThisTurn = false;
    }

    public string GetCardName()
    {
        if (cardData == null)
        {
            return "Unknown Card";
        }

        if (!string.IsNullOrWhiteSpace(cardData.cardName))
        {
            return cardData.cardName;
        }

        return cardData.cardID;
    }
}
