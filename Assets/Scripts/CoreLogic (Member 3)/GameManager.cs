using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// MOTORUL DE JOC (clasa "Meci" din diagrama de clase).
///
/// Detine doi jucatori (tu + adversarul) sub forma de JucatorMeci si conduce
/// intregul flux al unui meci: start, ture, tras carti, jucat carti, COMBAT
/// (minion vs minion / minion vs erou) si CONDITIA DE VICTORIE.
///
/// Etapa 1: adversarul este controlat de un AI simplu (esteControlatDeAI = true),
/// ca sa fie un joc complet jucabil de unul singur, imediat. In Etapa 3, AI-ul
/// poate fi inlocuit cu input venit prin retea (PlayFab), fara a schimba modelul.
///
/// Compatibilitate: am pastrat toate campurile/metodele publice vechi
/// (playerCurrentMana, StartGame, EndPlayerTurn, TryPlayCard, DrawCardFromButton...)
/// ca wiring-ul existent din scena sa nu se strice.
/// </summary>
public class GameManager : MonoBehaviour
{
    [Header("Game State")]
    public GameState currentState = GameState.NotStarted;

    [Header("Reguli")]
    public int heroStartingHP = 30;
    public int maxManaLimit = 10;
    public int startingHandSize = 3;
    public int cardsDrawnPerTurn = 1;
    public int maxHandSize = 10;
    public int maxBoardSize = 7;

    [Header("Adversar AI (Etapa 1)")]
    public bool enableEnemyAI = true;
    [Tooltip("Pauza (secunde) intre actiunile AI-ului, ca sa se vada ce face.")]
    public float enemyActionDelay = 0.8f;

    [Header("Debug")]
    [Tooltip("Printeaza in Console ce statistici a citit din PlayFab pentru fiecare carte.")]
    public bool debugLogCloudCards = true;

    [Header("Deck de test (fallback daca nu exista deck din PlayFab)")]
    public List<CardData> startingDeck = new List<CardData>();

    // --- Cei doi jucatori ai meciului (sursa de adevar) ---
    public JucatorMeci player = new JucatorMeci("Tu", 30);
    public JucatorMeci enemy = new JucatorMeci("Adversar", 30);

    // --- Oglinzi pentru compatibilitate cu codul/wiring-ul vechi ---
    // Acestea pointeaza catre listele jucatorului uman.
    [HideInInspector] public List<CardData> playerDeck = new List<CardData>();
    [HideInInspector] public List<CardData> playerHand = new List<CardData>();
    [HideInInspector] public List<RuntimeCardInstance> playerBoard = new List<RuntimeCardInstance>();
    [HideInInspector] public int playerMaxMana = 0;
    [HideInInspector] public int playerCurrentMana = 0;

    [Header("UI - Jucator (optional)")]
    public TextMeshProUGUI manaText;
    public TextMeshProUGUI deckCountText;
    public TextMeshProUGUI handCountText;
    public TextMeshProUGUI stateText;
    public TextMeshProUGUI messageText;
    public TextMeshProUGUI playerHpText;

    [Header("UI - Adversar (optional)")]
    public TextMeshProUGUI enemyHpText;
    public TextMeshProUGUI enemyManaText;
    public TextMeshProUGUI enemyDeckCountText;
    public TextMeshProUGUI enemyHandCountText;

    [Header("Evenimente pentru integrare UI")]
    public UnityEvent onGameStarted;
    public UnityEvent onPlayerTurnStarted;
    public UnityEvent onEnemyTurnStarted;
    public UnityEvent onGameStateChanged;
    public UnityEvent onResourcesChanged;
    public UnityEvent onCardsChanged;
    public UnityEvent onCombatResolved;   // NOU: dupa fiecare atac (pentru animatii/SFX)
    public UnityEvent onGameEnded;         // NOU: cand un erou ajunge la 0 HP

    private bool astepDatePlayFab = false;

    private void Start()
    {
        var inv = PlayFabInventoryManager.Instance;

        if (inv != null)
        {
            // Suntem intr-un flux cu PlayFab. Daca datele din cloud sunt deja
            // descarcate, pornim direct. Altfel asteptam semnalul OnInventoryReady
            // (exact ca HandManager-ul), ca sa nu pornim cu deck gol.
            bool dateGata = (inv.cardStatsDatabase != null && inv.cardStatsDatabase.Count > 0);

            if (dateGata)
            {
                StartGame();
            }
            else
            {
                astepDatePlayFab = true;
                PlayFabInventoryManager.OnInventoryReady += OnPlayFabReady;
                RefreshDebugUI();
                ShowMessage("Astept datele cartilor din PlayFab...");
            }
        }
        else if (startingDeck.Count > 0)
        {
            // Scena de test fara PlayFab: folosim deck-ul local.
            StartGame();
        }
        else
        {
            RefreshDebugUI();
            ShowMessage("GameManager pregatit. Conecteaza PlayFab sau adauga carti in Starting Deck pentru test.");
        }
    }

    private void OnPlayFabReady()
    {
        PlayFabInventoryManager.OnInventoryReady -= OnPlayFabReady;
        astepDatePlayFab = false;
        StartGame();
    }

    private void OnDisable()
    {
        if (astepDatePlayFab)
        {
            PlayFabInventoryManager.OnInventoryReady -= OnPlayFabReady;
            astepDatePlayFab = false;
        }
    }

    // ============================================================
    //  START MECI  (= startMeci(j1, j2) din diagrama)
    // ============================================================
    [ContextMenu("0. Start / Restart meci")]
    public void StartGame()
    {
        currentState = GameState.NotStarted;

        // Initializare jucatori
        player = new JucatorMeci("Tu", heroStartingHP) { esteControlatDeAI = false };
        enemy = new JucatorMeci("Adversar", heroStartingHP) { esteControlatDeAI = enableEnemyAI };

        // Incarcam deck-urile (PlayFab daca exista, altfel deck-ul de test)
        IncarcaDeckuri();

        ShuffleDeck(player.deck);
        ShuffleDeck(enemy.deck);

        // Mana initiala in mana ambilor jucatori
        for (int i = 0; i < startingHandSize; i++)
        {
            DrawCardFor(player, anuntaUI: false);
            DrawCardFor(enemy, anuntaUI: false);
        }

        SyncMirrors();

        currentState = GameState.PlayerTurn;
        onGameStarted?.Invoke();
        StartPlayerTurn();

        ShowMessage("Meci inceput. Mult succes!");
    }

    private void IncarcaDeckuri()
    {
        player.deck.Clear();
        enemy.deck.Clear();

        var inv = PlayFabInventoryManager.Instance;
        bool cloudGata = inv != null && inv.cardStatsDatabase != null && inv.cardStatsDatabase.Count > 0;

        if (cloudGata)
        {
            // Sursa de adevar = PlayFab. Construim cartile din statisticile din cloud.
            player.deck = ConstruiesteDeckDinCloud(inv.playerActiveDeck);
            enemy.deck = ConstruiesteDeckDinCloud(inv.enemyActiveDeck);

            // Vs AI (Etapa 1): daca adversarul nu are un deck setat in PlayFab,
            // ii dam o copie a deck-ului tau, ca AI-ul sa aiba cu ce juca.
            if (enemy.deck.Count == 0 && inv.playerActiveDeck != null && inv.playerActiveDeck.Count > 0)
                enemy.deck = ConstruiesteDeckDinCloud(inv.playerActiveDeck);

            if (player.deck.Count == 0)
                ShowMessage("ATENTIE: playerActiveDeck e gol. Deck-ul nu a fost incarcat din Deck Builder inainte de meci.");
        }
        else
        {
            // Fallback LOCAL doar pentru scena de test (fara PlayFab).
            player.deck = new List<CardData>(startingDeck);
            enemy.deck = new List<CardData>(startingDeck);
        }
    }

    // Construieste un deck (lista de CardData) pornind de la o lista de itemId-uri din PlayFab.
    private List<CardData> ConstruiesteDeckDinCloud(List<string> itemIds)
    {
        List<CardData> rezultat = new List<CardData>();
        if (itemIds == null) return rezultat;

        foreach (string id in itemIds)
        {
            CardData c = ConstruiesteCarteDinCloud(id);
            if (c != null) rezultat.Add(c);
        }
        return rezultat;
    }

    // Construieste o instanta runtime de CardData din statisticile PlayFab (atac/HP/cost),
    // imprumutand numai numele/poza/clasa din asset-ul local daca acesta exista.
    // Nu modifica niciun asset salvat (foloseste o copie creata in memorie).
    private CardData ConstruiesteCarteDinCloud(string itemId)
    {
        var inv = PlayFabInventoryManager.Instance;
        if (inv == null || string.IsNullOrEmpty(itemId)) return null;

        CloudCardStats stats;
        if (!inv.cardStatsDatabase.TryGetValue(itemId, out stats) || stats == null)
        {
            Debug.LogWarning("[Meci] Nu am gasit statistici cloud pentru cartea: " + itemId);
            return null;
        }

        // Copie runtime (NU asset salvat) - stats vin 100% din PlayFab
        CardData rt = ScriptableObject.CreateInstance<CardData>();
        rt.cardID = itemId;
        rt.attack = ParseInt(stats.Attack);
        rt.hp = ParseInt(stats.Health);
        rt.manaCost = ParseInt(stats.ManaCost);
        rt.flavorText = stats.flavorText;

        // Numele, poza, clasa si statusul: din asset-ul local daca il gasim
        // (poza nu poate veni din PlayFab - sprite-urile traiesc local in Unity).
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

        // Daca ai adaugat campurile Class / Status in Custom Data din PlayFab,
        // ele au prioritate (sursa de adevar ramane cloud-ul).
        if (!string.IsNullOrWhiteSpace(stats.Class)) rt.cardClass = stats.Class;
        if (!string.IsNullOrWhiteSpace(stats.Status)) rt.status = stats.Status;

        // Numele real vine din DisplayName-ul cartii din catalogul PlayFab.
        if (!string.IsNullOrWhiteSpace(stats.DisplayName)) rt.cardName = stats.DisplayName;

        if (debugLogCloudCards)
        {
            Debug.Log("[Meci] Construit din cloud: " + rt.cardName + " (" + itemId + ")" +
                      " ATK=" + rt.attack + " HP=" + rt.hp + " MANA=" + rt.manaCost +
                      " Class=" + rt.cardClass + " Status=" + rt.status);
        }

        return rt;
    }

    private int ParseInt(string s)
    {
        return int.TryParse(s, out int v) ? v : 0;
    }

    // ============================================================
    //  TURE
    // ============================================================
    public void StartPlayerTurn()
    {
        currentState = GameState.PlayerTurn;

        CresteManaPentruTuraNoua(player);
        ReseteazaBoardPentruTuraNoua(player);
        for (int i = 0; i < cardsDrawnPerTurn; i++) DrawCardFor(player);

        SyncMirrors();
        onPlayerTurnStarted?.Invoke();
        onGameStateChanged?.Invoke();
        ShowMessage("A inceput tura ta.");
        RefreshDebugUI();
    }

    [ContextMenu("3. Termina tura (apoi joaca AI-ul)")]
    public void EndPlayerTurn()
    {
        if (currentState != GameState.PlayerTurn)
        {
            ShowMessage("Nu este tura ta.");
            return;
        }

        currentState = GameState.EnemyTurn;
        onEnemyTurnStarted?.Invoke();
        onGameStateChanged?.Invoke();
        ShowMessage("Tura adversarului...");
        RefreshDebugUI();

        if (enemy.esteControlatDeAI)
            StartCoroutine(RunEnemyTurnRoutine());
        // Daca NU e AI (ex. multiplayer in Etapa 3), tura adversarului
        // va fi condusa din afara prin apeluri echivalente.
    }

    private void ReseteazaBoardPentruTuraNoua(JucatorMeci j)
    {
        foreach (var inst in j.board)
        {
            if (inst == null) continue;
            inst.hasAttackedThisTurn = false;
            inst.justSummoned = false; // creaturile invocate tura trecuta pot ataca acum
        }
    }

    private void CresteManaPentruTuraNoua(JucatorMeci j)
    {
        if (j.manaMaxima < maxManaLimit) j.manaMaxima++;
        j.manaCurenta = j.manaMaxima;
        onResourcesChanged?.Invoke();
    }

    // ============================================================
    //  TRAS CARTI
    // ============================================================
    public bool DrawCardFor(JucatorMeci j, bool anuntaUI = true)
    {
        if (j.deck.Count <= 0)
        {
            if (anuntaUI) ShowMessage(j.numeJucator + ": deck gol, nu mai poti trage.");
            return false;
        }
        if (j.mana.Count >= maxHandSize)
        {
            if (anuntaUI) ShowMessage(j.numeJucator + ": mana plina, cartea a fost arsa.");
            j.deck.RemoveAt(0); // overdraw: cartea se pierde
            return false;
        }

        CardData drawn = j.deck[0];
        j.deck.RemoveAt(0);
        j.mana.Add(drawn);

        if (anuntaUI)
        {
            if (!j.esteControlatDeAI) ShowMessage("Ai tras: " + GetCardName(drawn));
            onCardsChanged?.Invoke();
        }
        SyncMirrors();
        RefreshDebugUI();
        return true;
    }

    // --- Compatibilitate: vechiul DrawCard() trage pentru jucatorul uman ---
    public bool DrawCard() => DrawCardFor(player);
    public void DrawCardFromButton() => DrawCardFor(player);

    // ============================================================
    //  JUCAT CARTI
    // ============================================================
    public bool TryPlayCard(CardData card)
    {
        return PlayCardFor(player, card, esteTuraValida: currentState == GameState.PlayerTurn);
    }

    private bool PlayCardFor(JucatorMeci j, CardData card, bool esteTuraValida)
    {
        if (card == null) { ShowMessage("Carte invalida."); return false; }
        if (!esteTuraValida) { ShowMessage("Nu este tura corecta pentru a juca o carte."); return false; }
        if (!j.mana.Contains(card)) { ShowMessage("Cartea nu e in mana."); return false; }
        if (j.board.Count >= maxBoardSize) { ShowMessage("Board plin."); return false; }
        if (!j.AreDestulaMana(card.manaCost))
        {
            if (!j.esteControlatDeAI)
                ShowMessage("Mana insuficienta pentru " + GetCardName(card) +
                            " (cost " + card.manaCost + ", ai " + j.manaCurenta + ").");
            return false;
        }

        j.ConsumaMana(card.manaCost);
        j.mana.Remove(card);
        j.board.Add(new RuntimeCardInstance(card));

        if (!j.esteControlatDeAI) ShowMessage("Ai jucat: " + GetCardName(card));
        else ShowMessage("Adversarul a jucat: " + GetCardName(card));

        SyncMirrors();
        onCardsChanged?.Invoke();
        onResourcesChanged?.Invoke();
        RefreshDebugUI();
        return true;
    }

    public void TryPlayFirstCardFromHand()
    {
        if (player.mana.Count <= 0) { ShowMessage("Nu ai carti in mana."); return; }
        TryPlayCard(player.mana[0]);
    }

    // ============================================================
    //  COMBAT  (= proceseazaAtac(...) din diagrama)
    // ============================================================

    /// <summary>Atac minion vs minion. Returneaza true daca atacul a fost legal si rezolvat.</summary>
    public bool ProceseazaAtac(RuntimeCardInstance atacator, RuntimeCardInstance tinta)
    {
        if (!ValideazaAtacator(atacator)) return false;
        if (tinta == null || tinta.EsteMoarta()) { ShowMessage("Tinta invalida."); return false; }

        // Schimb de damage (ambele entitati primesc atacul celeilalte)
        int damageCatreTinta = atacator.Attack;
        int damageCatreAtacator = tinta.Attack;

        tinta.currentHP -= damageCatreTinta;
        atacator.currentHP -= damageCatreAtacator;
        atacator.hasAttackedThisTurn = true;

        ShowMessage(atacator.GetCardName() + " ataca " + tinta.GetCardName() + ".");

        EliminaCartiDistruse();
        onCombatResolved?.Invoke();
        SyncMirrors();
        RefreshDebugUI();
        VerificaConditieVictorie();
        return true;
    }

    /// <summary>Atac minion vs EROU advers.</summary>
    public bool ProceseazaAtacAsupraEroului(RuntimeCardInstance atacator, JucatorMeci eroultinta)
    {
        if (!ValideazaAtacator(atacator)) return false;
        if (eroultinta == null) return false;

        eroultinta.PrimesteDamage(atacator.Attack);
        atacator.hasAttackedThisTurn = true;

        ShowMessage(atacator.GetCardName() + " loveste eroul " + eroultinta.numeJucator +
                    " pentru " + atacator.Attack + " damage.");

        onCombatResolved?.Invoke();
        SyncMirrors();
        RefreshDebugUI();
        VerificaConditieVictorie();
        return true;
    }

    private bool ValideazaAtacator(RuntimeCardInstance atacator)
    {
        if (atacator == null) { ShowMessage("Atacator invalid."); return false; }
        if (atacator.EsteImobil()) { ShowMessage(atacator.GetCardName() + " este Imobil si nu poate ataca."); return false; }
        if (atacator.justSummoned) { ShowMessage(atacator.GetCardName() + " tocmai a fost invocat (nu poate ataca tura asta)."); return false; }
        if (atacator.hasAttackedThisTurn) { ShowMessage(atacator.GetCardName() + " a atacat deja in tura asta."); return false; }
        if (atacator.EsteMoarta()) return false;
        return true;
    }

    /// <summary>Scoate de pe ambele table creaturile cu 0 HP.</summary>
    public void EliminaCartiDistruse()
    {
        player.board.RemoveAll(c => c == null || c.EsteMoarta());
        enemy.board.RemoveAll(c => c == null || c.EsteMoarta());
        SyncMirrors();
    }

    /// <summary>Verifica daca un erou a ajuns la 0 HP si incheie meciul.</summary>
    public bool VerificaConditieVictorie()
    {
        if (currentState == GameState.EndGame) return true;

        if (!enemy.EsteInViata())
        {
            IncheieMeci(player);
            return true;
        }
        if (!player.EsteInViata())
        {
            IncheieMeci(enemy);
            return true;
        }
        return false;
    }

    private void IncheieMeci(JucatorMeci castigator)
    {
        currentState = GameState.EndGame;
        StopAllCoroutines();
        ShowMessage(castigator == player ? "AI CASTIGAT!" : "Ai pierdut. Mai incearca!");
        onGameStateChanged?.Invoke();
        onGameEnded?.Invoke();
        RefreshDebugUI();
    }

    // ============================================================
    //  AI ADVERSAR (Etapa 1)
    // ============================================================
    private IEnumerator RunEnemyTurnRoutine()
    {
        yield return new WaitForSeconds(enemyActionDelay);

        CresteManaPentruTuraNoua(enemy);
        ReseteazaBoardPentruTuraNoua(enemy);
        DrawCardFor(enemy);
        yield return new WaitForSeconds(enemyActionDelay);

        // --- Faza de jucat carti: cele mai ieftine intai, cat permite mana/board ---
        bool aJucatCeva = true;
        while (aJucatCeva && currentState == GameState.EnemyTurn)
        {
            aJucatCeva = false;
            CardData deJucat = enemy.mana
                .Where(c => c != null && c.manaCost <= enemy.manaCurenta)
                .OrderBy(c => c.manaCost)
                .FirstOrDefault();

            if (deJucat != null && enemy.board.Count < maxBoardSize)
            {
                PlayCardFor(enemy, deJucat, esteTuraValida: true);
                aJucatCeva = true;
                yield return new WaitForSeconds(enemyActionDelay);
            }
        }

        // --- Faza de atac ---
        var atacatori = enemy.board.Where(m => m != null && m.PoateAtaca()).ToList();
        foreach (var m in atacatori)
        {
            if (currentState == GameState.EndGame) yield break;
            if (!m.PoateAtaca()) continue;

            // Lethal pe erou?
            if (m.Attack >= player.hpCurent)
            {
                ProceseazaAtacAsupraEroului(m, player);
            }
            else
            {
                // Schimb favorabil cu un minion advers (il omor si supravietuiesc)?
                RuntimeCardInstance tinta = player.board
                    .Where(pm => pm != null && m.Attack >= pm.currentHP && pm.Attack < m.currentHP)
                    .OrderByDescending(pm => pm.Attack)
                    .FirstOrDefault();

                if (tinta != null) ProceseazaAtac(m, tinta);
                else ProceseazaAtacAsupraEroului(m, player); // altfel, lovesc eroul
            }

            yield return new WaitForSeconds(enemyActionDelay);
        }

        if (currentState == GameState.EnemyTurn)
        {
            ShowMessage("Adversarul a terminat tura.");
            StartPlayerTurn();
        }
    }

    // ============================================================
    //  METODE DE TEST (ca sa poti verifica motorul cu butoane,
    //  inainte sa legam drag & drop-ul real in Etapa 2)
    // ============================================================
    [ContextMenu("1. Joaca cea mai ieftina carte din mana")]
    public void DebugPlayCheapestCard()
    {
        var c = player.mana.Where(x => x != null && x.manaCost <= player.manaCurenta)
                           .OrderBy(x => x.manaCost).FirstOrDefault();
        if (c != null) TryPlayCard(c);
        else ShowMessage("Nu poti juca nicio carte (mana?).");
    }

    [ContextMenu("2a. Primul minion al tau loveste EROUL advers")]
    public void DebugPlayerFirstMinionAttacksEnemyHero()
    {
        var m = player.board.FirstOrDefault(x => x != null && x.PoateAtaca());
        if (m != null) ProceseazaAtacAsupraEroului(m, enemy);
        else ShowMessage("Niciun minion al tau nu poate ataca.");
    }

    [ContextMenu("2b. Primul minion al tau loveste primul MINION advers")]
    public void DebugPlayerFirstMinionAttacksEnemyFirstMinion()
    {
        var m = player.board.FirstOrDefault(x => x != null && x.PoateAtaca());
        var t = enemy.board.FirstOrDefault(x => x != null && !x.EsteMoarta());
        if (m == null) { ShowMessage("Niciun minion al tau nu poate ataca."); return; }
        if (t == null) { ShowMessage("Adversarul nu are minioni."); return; }
        ProceseazaAtac(m, t);
    }

    // ============================================================
    //  UTILITARE
    // ============================================================
    private void ShuffleDeck(List<CardData> deck)
    {
        for (int i = 0; i < deck.Count; i++)
        {
            int r = Random.Range(i, deck.Count);
            (deck[i], deck[r]) = (deck[r], deck[i]);
        }
    }

    private string GetCardName(CardData card)
    {
        if (card == null) return "Unknown Card";
        return !string.IsNullOrWhiteSpace(card.cardName) ? card.cardName : card.cardID;
    }

    // Tine sincronizate oglinzile vechi (playerDeck/Hand/Board, manele) cu JucatorMeci-ul.
    private void SyncMirrors()
    {
        playerDeck = player.deck;
        playerHand = player.mana;
        playerBoard = player.board;
        playerMaxMana = player.manaMaxima;
        playerCurrentMana = player.manaCurenta;
    }

    private void RefreshDebugUI()
    {
        if (manaText != null) manaText.text = "Mana: " + player.manaCurenta + "/" + player.manaMaxima;
        if (deckCountText != null) deckCountText.text = "Deck: " + player.deck.Count;
        if (handCountText != null) handCountText.text = "Hand: " + player.mana.Count;
        if (stateText != null) stateText.text = "State: " + currentState;
        if (playerHpText != null) playerHpText.text = "HP: " + player.hpCurent + "/" + player.hpMax;

        if (enemyHpText != null) enemyHpText.text = "HP: " + enemy.hpCurent + "/" + enemy.hpMax;
        if (enemyManaText != null) enemyManaText.text = "Mana: " + enemy.manaCurenta + "/" + enemy.manaMaxima;
        if (enemyDeckCountText != null) enemyDeckCountText.text = "Deck: " + enemy.deck.Count;
        if (enemyHandCountText != null) enemyHandCountText.text = "Hand: " + enemy.mana.Count;
    }

    private void ShowMessage(string message)
    {
        if (messageText != null) messageText.text = message;
        Debug.Log("[Meci] " + message);
    }
}