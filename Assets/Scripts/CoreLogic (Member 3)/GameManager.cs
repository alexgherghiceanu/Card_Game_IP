using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class GameManager : MonoBehaviour
{
    [Header("Game State")]
    public GameState currentState = GameState.NotStarted;

    [Header("Player Resources")]
    public int playerMaxMana = 0;
    public int playerCurrentMana = 0;
    public int maxManaLimit = 10;

    [Header("Cards")]
    public List<CardData> startingDeck = new List<CardData>();
    public List<CardData> playerDeck = new List<CardData>();
    public List<CardData> playerHand = new List<CardData>();
    public List<RuntimeCardInstance> playerBoard = new List<RuntimeCardInstance>();

    [Header("Rules")]
    public int startingHandSize = 3;
    public int cardsDrawnPerTurn = 1;
    public int maxHandSize = 10;
    public int maxBoardSize = 7;

    [Header("Optional UI References")]
    public TextMeshProUGUI manaText;
    public TextMeshProUGUI deckCountText;
    public TextMeshProUGUI handCountText;
    public TextMeshProUGUI stateText;
    public TextMeshProUGUI messageText;

    [Header("Events For UI Integration")]
    public UnityEvent onGameStarted;
    public UnityEvent onPlayerTurnStarted;
    public UnityEvent onEnemyTurnStarted;
    public UnityEvent onGameStateChanged;
    public UnityEvent onResourcesChanged;
    public UnityEvent onCardsChanged;

    private void Start()
    {
        if (startingDeck.Count > 0)
        {
            StartGame();
        }
        else
        {
            RefreshDebugUI();
            ShowMessage("GameManager pregatit. Adauga carti in Starting Deck pentru test.");
        }
    }

    public void StartGame()
    {
        currentState = GameState.NotStarted;

        playerDeck = new List<CardData>(startingDeck);
        playerHand.Clear();
        playerBoard.Clear();

        playerMaxMana = 0;
        playerCurrentMana = 0;

        ShuffleDeck(playerDeck);

        for (int i = 0; i < startingHandSize; i++)
        {
            DrawCard();
        }

        currentState = GameState.PlayerTurn;
        StartPlayerTurn();

        onGameStarted?.Invoke();
        onGameStateChanged?.Invoke();

        ShowMessage("Meci inceput.");
        RefreshDebugUI();
    }

    public void StartPlayerTurn()
    {
        currentState = GameState.PlayerTurn;

        IncreaseManaForNewTurn();
        DrawCardsForTurn();

        foreach (RuntimeCardInstance cardInstance in playerBoard)
        {
            if (cardInstance != null)
            {
                cardInstance.hasAttackedThisTurn = false;
            }
        }

        onPlayerTurnStarted?.Invoke();
        onGameStateChanged?.Invoke();

        ShowMessage("A inceput tura jucatorului.");
        RefreshDebugUI();
    }

    public void EndPlayerTurn()
    {
        if (currentState != GameState.PlayerTurn)
        {
            ShowMessage("Nu poti termina tura deoarece nu este tura jucatorului.");
            return;
        }

        currentState = GameState.EnemyTurn;

        onEnemyTurnStarted?.Invoke();
        onGameStateChanged?.Invoke();

        ShowMessage("Tura jucatorului s-a terminat. Urmeaza tura inamicului.");
        RefreshDebugUI();
    }

    public void EndEnemyTurnAndStartPlayerTurn()
    {
        if (currentState != GameState.EnemyTurn)
        {
            ShowMessage("Nu poti porni tura jucatorului deoarece nu esti in EnemyTurn.");
            return;
        }

        StartPlayerTurn();
    }

    public bool DrawCard()
    {
        if (playerDeck.Count <= 0)
        {
            ShowMessage("Deck-ul este gol. Nu mai poti trage carti.");
            RefreshDebugUI();
            return false;
        }

        if (playerHand.Count >= maxHandSize)
        {
            ShowMessage("Mana este plina. Cartea nu a fost trasa.");
            RefreshDebugUI();
            return false;
        }

        CardData drawnCard = playerDeck[0];
        playerDeck.RemoveAt(0);
        playerHand.Add(drawnCard);

        string cardName = GetCardName(drawnCard);
        ShowMessage("Ai tras cartea: " + cardName);

        onCardsChanged?.Invoke();
        RefreshDebugUI();

        return true;
    }

    public bool TryPlayCard(CardData card)
    {
        if (card == null)
        {
            ShowMessage("Nu poti juca o carte nula.");
            return false;
        }

        if (currentState != GameState.PlayerTurn)
        {
            ShowMessage("Nu poti juca o carte acum. Nu este tura jucatorului.");
            return false;
        }

        if (!playerHand.Contains(card))
        {
            ShowMessage("Cartea nu se afla in mana jucatorului.");
            return false;
        }

        if (playerBoard.Count >= maxBoardSize)
        {
            ShowMessage("Board-ul este plin. Nu mai poti plasa carti.");
            return false;
        }

        if (!HasEnoughMana(card))
        {
            ShowMessage("Mana insuficienta pentru " + GetCardName(card) + ". Cost: " + card.manaCost + ", mana disponibila: " + playerCurrentMana + ".");
            return false;
        }

        SpendMana(card.manaCost);

        playerHand.Remove(card);
        RuntimeCardInstance playedCard = new RuntimeCardInstance(card);
        playerBoard.Add(playedCard);

        ShowMessage("Ai jucat cartea: " + GetCardName(card));

        onCardsChanged?.Invoke();
        RefreshDebugUI();

        return true;
    }

    public void DrawCardFromButton()
    {
        DrawCard();
    }

    public void TryPlayFirstCardFromHand()
    {
        if (playerHand.Count <= 0)
        {
            ShowMessage("Nu exista carti in mana.");
            return;
        }

        TryPlayCard(playerHand[0]);
    }

    public bool HasEnoughMana(CardData card)
    {
        if (card == null)
        {
            return false;
        }

        return playerCurrentMana >= card.manaCost;
    }

    public void SpendMana(int amount)
    {
        playerCurrentMana -= amount;

        if (playerCurrentMana < 0)
        {
            playerCurrentMana = 0;
        }

        onResourcesChanged?.Invoke();
        RefreshDebugUI();
    }

    private void IncreaseManaForNewTurn()
    {
        if (playerMaxMana < maxManaLimit)
        {
            playerMaxMana++;
        }

        playerCurrentMana = playerMaxMana;

        onResourcesChanged?.Invoke();
    }

    private void DrawCardsForTurn()
    {
        for (int i = 0; i < cardsDrawnPerTurn; i++)
        {
            DrawCard();
        }
    }

    private void ShuffleDeck(List<CardData> deck)
    {
        for (int i = 0; i < deck.Count; i++)
        {
            int randomIndex = Random.Range(i, deck.Count);

            CardData temporary = deck[i];
            deck[i] = deck[randomIndex];
            deck[randomIndex] = temporary;
        }
    }

    private string GetCardName(CardData card)
    {
        if (card == null)
        {
            return "Unknown Card";
        }

        if (!string.IsNullOrWhiteSpace(card.cardName))
        {
            return card.cardName;
        }

        return card.cardID;
    }

    private void RefreshDebugUI()
    {
        if (manaText != null)
        {
            manaText.text = "Mana: " + playerCurrentMana + "/" + playerMaxMana;
        }

        if (deckCountText != null)
        {
            deckCountText.text = "Deck: " + playerDeck.Count;
        }

        if (handCountText != null)
        {
            handCountText.text = "Hand: " + playerHand.Count;
        }

        if (stateText != null)
        {
            stateText.text = "State: " + currentState;
        }
    }

    private void ShowMessage(string message)
    {
        if (messageText != null)
        {
            messageText.text = message;
        }

        Debug.Log("[GameManager] " + message);
    }
}
