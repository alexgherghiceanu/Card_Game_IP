using System.Collections.Generic;
using System.Linq;

public static class DeckValidator
{
    public const int RequiredDeckSize = 15;
    public const int MaxCopiesPerCard = 2;

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
        ValidateRequiredClasses(validCards, requiredClasses, result);

        return result;
    }

    private static void ValidateDeckSize(List<CardData> deck, DeckValidationResult result)
    {
        if (deck.Count != RequiredDeckSize)
        {
            result.AddError("Deck-ul trebuie sa contina exact " + RequiredDeckSize + " carti. In prezent are " + deck.Count + ".");
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

    private static void ValidateRequiredClasses(List<CardData> deck, List<string> requiredClasses, DeckValidationResult result)
    {
        if (requiredClasses == null || requiredClasses.Count == 0)
        {
            result.AddError("Lista de clase obligatorii nu este setata.");
            return;
        }

        HashSet<string> deckClasses = new HashSet<string>();

        foreach (CardData card in deck)
        {
            if (card != null && !string.IsNullOrWhiteSpace(card.cardClass))
            {
                deckClasses.Add(card.cardClass.Trim());
            }
        }

        foreach (string requiredClass in requiredClasses)
        {
            if (string.IsNullOrWhiteSpace(requiredClass))
            {
                continue;
            }

            string normalizedClass = requiredClass.Trim();

            if (!deckClasses.Contains(normalizedClass))
            {
                result.AddError("Deck-ul trebuie sa contina cel putin o carte din clasa \"" + normalizedClass + "\".");
            }
        }
    }
}
