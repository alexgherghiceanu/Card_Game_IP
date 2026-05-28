using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// ETAPA 2: leaga LOGICA (GameManager) de VIZUAL.
///
/// Joaca o carte: TRAGI cartea din mana pe board-ul tau (drag and drop).
///                (merge si pe click simplu pe carte, ca rezerva.)
/// Atac minion : buton "Atac minion" -> click pe minionul tau -> click pe minionul inamic.
/// Atac erou   : buton "Atac erou"   -> click pe minionul cu care ataci -> loveste eroul.
/// Termina tura: buton "End Turn".
///
/// Afiseaza: mana ta (jos), board-ul tau, board-ul si MANA adversarului (sus).
/// </summary>
public class BattleView : MonoBehaviour
{
    [Header("Referinte (obligatorii)")]
    public GameManager gameManager;
    [Tooltip("Prefab-ul de carte (cel cu CardDisplay), ex. Card_Template 1.")]
    public GameObject cardPrefab;

    [Header("Zonele tale (jos)")]
    public Transform playerHandArea;
    public Transform playerBoardArea;

    [Header("Zonele adversarului (sus)")]
    public Transform enemyHandArea;
    public Transform enemyBoardArea;

    [Header("Evidentiere atacator selectat")]
    public Color culoareSelectat = new Color(1f, 0.92f, 0.4f, 1f);

    public enum ModAtac { Nimic, Minion, Erou }
    private ModAtac modCurent = ModAtac.Nimic;
    private RuntimeCardInstance atacatorSelectat;

    private Canvas canvas;

    private void OnEnable()
    {
        if (gameManager == null) gameManager = FindObjectOfType<GameManager>();
        if (gameManager == null) { Debug.LogError("[BattleView] Nu am gasit GameManager."); return; }

        gameManager.onCardsChanged.AddListener(Redeseneaza);
        gameManager.onCombatResolved.AddListener(Redeseneaza);
        gameManager.onResourcesChanged.AddListener(Redeseneaza);
        gameManager.onGameStateChanged.AddListener(Redeseneaza);
        gameManager.onPlayerTurnStarted.AddListener(Redeseneaza);
        gameManager.onGameStarted.AddListener(Redeseneaza);
    }

    private void OnDisable()
    {
        if (gameManager == null) return;
        gameManager.onCardsChanged.RemoveListener(Redeseneaza);
        gameManager.onCombatResolved.RemoveListener(Redeseneaza);
        gameManager.onResourcesChanged.RemoveListener(Redeseneaza);
        gameManager.onGameStateChanged.RemoveListener(Redeseneaza);
        gameManager.onPlayerTurnStarted.RemoveListener(Redeseneaza);
        gameManager.onGameStarted.RemoveListener(Redeseneaza);
    }

    private void Start()
    {
        if (playerHandArea != null) canvas = playerHandArea.GetComponentInParent<Canvas>();
        if (canvas == null) canvas = FindObjectOfType<Canvas>();
        Redeseneaza();
    }

    // =================== BUTOANE (de legat in UI) ===================
    public void EndTurn()
    {
        ReseteazaMod();
        if (gameManager != null) gameManager.EndPlayerTurn();
    }

    public void ModAtacMinion()
    {
        modCurent = ModAtac.Minion;
        atacatorSelectat = null;
        Debug.Log("[BattleView] Mod ATAC MINION: click pe minionul tau, apoi pe cel inamic.");
        Redeseneaza();
    }

    public void ModAtacErou()
    {
        modCurent = ModAtac.Erou;
        atacatorSelectat = null;
        Debug.Log("[BattleView] Mod ATAC EROU: click pe minionul cu care vrei sa ataci eroul.");
        Redeseneaza();
    }

    private void ReseteazaMod()
    {
        modCurent = ModAtac.Nimic;
        atacatorSelectat = null;
    }

    // =================== INTERACTIUNI ===================
    private void ClickCarteMana(CardData carte)
    {
        if (gameManager != null) gameManager.TryPlayCard(carte);
    }

    private void ClickMinionPropriu(RuntimeCardInstance inst)
    {
        if (modCurent == ModAtac.Erou)
        {
            ReseteazaMod();
            gameManager.ProceseazaAtacAsupraEroului(inst, gameManager.enemy);
        }
        else if (modCurent == ModAtac.Minion)
        {
            atacatorSelectat = inst;
            Debug.Log("[BattleView] Atacator selectat: " + inst.GetCardName() + ". Acum click pe un minion inamic.");
            Redeseneaza();
        }
        else
        {
            Debug.Log("[BattleView] Apasa intai un buton de atac (minion sau erou).");
        }
    }

    private void ClickMinionInamic(RuntimeCardInstance inst)
    {
        if (modCurent == ModAtac.Minion && atacatorSelectat != null)
        {
            RuntimeCardInstance atacator = atacatorSelectat;
            ReseteazaMod();
            gameManager.ProceseazaAtac(atacator, inst);
        }
        else
        {
            Debug.Log("[BattleView] Pentru atac: 'Atac minion' -> click pe minionul tau -> click pe cel inamic.");
        }
    }

    // Apelat de BattleDragSource cand dai drumul cartii din mana.
    public void RezolvaDropJucat(BattleDragSource sursa, Vector2 pozitieEcran)
    {
        if (gameManager != null && sursa != null && sursa.carteMana != null)
        {
            if (PesteRect(playerBoardArea, pozitieEcran))
                gameManager.TryPlayCard(sursa.carteMana);
        }

        if (sursa != null) Destroy(sursa.gameObject);
        Redeseneaza();
    }

    // =================== DESENARE ===================
    public void Redeseneaza()
    {
        if (gameManager == null || cardPrefab == null) return;

        GolesteZona(playerHandArea);
        GolesteZona(playerBoardArea);
        GolesteZona(enemyHandArea);
        GolesteZona(enemyBoardArea);

        // --- Mana ta (jos): drag pe board = joaca; click = joaca (rezerva) ---
        if (playerHandArea != null)
        {
            foreach (CardData c in gameManager.player.mana)
            {
                CardData captura = c;
                GameObject go = SpawneazaCarte(playerHandArea, cd => cd.SetupFromCardData(captura));
                AdaugaClick(go, () => ClickCarteMana(captura));
                AdaugaDragJucat(go, captura);
            }
        }

        // --- Board-ul tau: click = selecteaza / ataca ---
        if (playerBoardArea != null)
        {
            foreach (RuntimeCardInstance inst in gameManager.player.board)
            {
                RuntimeCardInstance captura = inst;
                GameObject go = SpawneazaCarte(playerBoardArea, cd => cd.SetupFromInstance(captura));
                if (captura == atacatorSelectat) Evidentiaza(go);
                AdaugaClick(go, () => ClickMinionPropriu(captura));
            }
        }

        // --- Mana adversarului (sus), exact ca la player ---
        if (enemyHandArea != null)
        {
            foreach (CardData c in gameManager.enemy.mana)
            {
                CardData captura = c;
                SpawneazaCarte(enemyHandArea, cd => cd.SetupFromCardData(captura));
            }
        }

        // --- Board-ul adversarului (sus): click = tinta ---
        if (enemyBoardArea != null)
        {
            foreach (RuntimeCardInstance inst in gameManager.enemy.board)
            {
                RuntimeCardInstance captura = inst;
                GameObject go = SpawneazaCarte(enemyBoardArea, cd => cd.SetupFromInstance(captura));
                AdaugaClick(go, () => ClickMinionInamic(captura));
            }
        }
    }

    // =================== AJUTOARE ===================
    private GameObject SpawneazaCarte(Transform parinte, Action<CardDisplay> setup)
    {
        GameObject go = Instantiate(cardPrefab, parinte);
        go.SetActive(true);

        CardDragNDrop oldDrag = go.GetComponent<CardDragNDrop>();
        if (oldDrag != null) oldDrag.enabled = false; // dezactivam drag-ul vechi (vizual)

        CardDisplay disp = go.GetComponent<CardDisplay>();
        if (disp == null) disp = go.GetComponentInChildren<CardDisplay>();
        if (disp != null) setup(disp);
        else Debug.LogWarning("[BattleView] Prefab-ul de carte nu are CardDisplay.");

        return go;
    }

    private void GolesteZona(Transform zona)
    {
        if (zona == null) return;
        for (int i = zona.childCount - 1; i >= 0; i--)
            Destroy(zona.GetChild(i).gameObject);
    }

    private void Evidentiaza(GameObject go)
    {
        Image img = go.GetComponent<Image>();
        if (img != null) img.color = culoareSelectat;
    }

    private void AsiguraRaycast(GameObject go)
    {
        Graphic g = go.GetComponent<Graphic>();
        if (g == null)
        {
            Image img = go.AddComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0f);
            img.raycastTarget = true;
        }
        else g.raycastTarget = true;

        if (go.GetComponent<CanvasGroup>() == null) go.AddComponent<CanvasGroup>();
    }

    private void AdaugaClick(GameObject go, Action actiune)
    {
        AsiguraRaycast(go);
        BattleClickable click = go.GetComponent<BattleClickable>();
        if (click == null) click = go.AddComponent<BattleClickable>();
        click.onClick = actiune;
    }

    private void AdaugaDragJucat(GameObject go, CardData carte)
    {
        AsiguraRaycast(go);
        BattleDragSource src = go.GetComponent<BattleDragSource>();
        if (src == null) src = go.AddComponent<BattleDragSource>();
        src.Init(this, carte);
    }

    public Canvas GetCanvas() { return canvas; }

    private bool PesteRect(Transform zona, Vector2 pozitieEcran)
    {
        if (zona == null) return false;
        RectTransform rt = zona as RectTransform;
        if (rt == null) rt = zona.GetComponent<RectTransform>();
        if (rt == null) return false;
        Camera cam = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay) ? canvas.worldCamera : null;
        return RectTransformUtility.RectangleContainsScreenPoint(rt, pozitieEcran, cam);
    }
}

/// <summary>Declanseaza o actiune la click pe un element de UI.</summary>
public class BattleClickable : MonoBehaviour, IPointerClickHandler
{
    public Action onClick;
    public void OnPointerClick(PointerEventData eventData)
    {
        onClick?.Invoke();
    }
}

/// <summary>Drag pentru o carte din mana: o tragi pe board ca s-o joci.</summary>
public class BattleDragSource : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public CardData carteMana;

    private BattleView view;
    private CanvasGroup cg;

    public void Init(BattleView v, CardData carte)
    {
        view = v;
        carteMana = carte;
        cg = GetComponent<CanvasGroup>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        Canvas c = view != null ? view.GetCanvas() : GetComponentInParent<Canvas>();
        if (c != null) transform.SetParent(c.transform, true);
        transform.SetAsLastSibling();
        if (cg != null) cg.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        transform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (cg != null) cg.blocksRaycasts = true;
        if (view != null) view.RezolvaDropJucat(this, eventData.position);
    }
}