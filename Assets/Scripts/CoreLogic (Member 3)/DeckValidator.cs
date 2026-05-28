using System.Collections.Generic;
using System.Linq;

public static class DeckValidator
{
    // Numarul MAXIM de carti (deck-ul poate avea pana la atatea).
    public const int RequiredDeckSize = 15;
    public const int MaxCopiesPerCard = 2;

    // requiredClasses ramane in semnatura pentru compatibilitate, dar NU mai e folosit:
    // nu mai exista regula "minim o carte din fiecare clasa".
    public static DeckValidationResult ValidateDeck(List<CardData> deck, List<string> requiredClasses)
    {
        DeckValidationResult result = new DeckValidationResult();

        if (deck == null)
        {
            result.AddError("Deck-ul nu exista.");
            return result;
        }

        List<CardData> validCards = deck.Where(card => card != null).ToList();

        ValidateDeckSize(validCards, result);
        ValidateMaxCopies(validCards, result);
        // (Regula de clase obligatorii a fost eliminata intentionat.)

        return result;
    }

    private static void ValidateDeckSize(List<CardData> deck, DeckValidationResult result)
    {
        if (deck.Count == 0)
        {
            result.AddError("Deck-ul trebuie sa contina cel putin o carte.");
        }
        else if (deck.Count > RequiredDeckSize)
        {
            result.AddError("Deck-ul poate avea maxim " + RequiredDeckSize + " carti. In prezent are " + deck.Count + ".");
        }
    }

    private static void ValidateMaxCopies(List<CardData> deck, DeckValidationResult result)
    {
        var groupedCards = deck
            .Where(card => !string.IsNullOrWhiteSpace(card.cardID))
            .GroupBy(card => card.cardID);

        foreach (var group in groupedCards)
        {
            if (group.Count() > MaxCopiesPerCard)
            {
                CardData card = group.First();
                string cardName = string.IsNullOrWhiteSpace(card.cardName) ? card.cardID : card.cardName;
                result.AddError("Cartea \"" + cardName + "\" apare de " + group.Count() + " ori. Maxim permis: " + MaxCopiesPerCard + ".");
            }
        }
    }
}