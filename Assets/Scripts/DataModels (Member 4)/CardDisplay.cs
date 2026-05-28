using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CardDisplay : MonoBehaviour
{
    [Header("Vizualuri Statice (Local)")]
    // Referinta la ScriptableObject pentru poze si nume
    public CardData card;

    [Header("Elemente UI pe ecran")]
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI attackText;
    public TextMeshProUGUI hpText;
    public TextMeshProUGUI flavorText;
    public TextMeshProUGUI manaText;
    public Image artworkImage;

    // Apelata de HandManager dupa ce are datele de la PlayFab (flux vechi).
    public void SetupCardFromCloud(CloudCardStats cloudStats, string cardId)
    {
        if (card != null)
        {
            nameText.text = card.cardName;
            if (card.artwork != null) artworkImage.sprite = card.artwork;
        }
        else
        {
            nameText.text = cardId;
            Debug.LogWarning("CardData lipseste de pe prefab-ul cartii: " + cardId);
        }

        if (cloudStats != null)
        {
            attackText.text = cloudStats.Attack;
            hpText.text = cloudStats.Health;

            if (!string.IsNullOrEmpty(cloudStats.flavorText))
                flavorText.text = cloudStats.flavorText;
            else if (card != null && card.flavorText != null)
                flavorText.text = card.flavorText;

            if (manaText != null) manaText.text = cloudStats.ManaCost;
        }
        else
        {
            Debug.LogError("Nu s-au putut aplica statisticile din Cloud pentru cartea: " + cardId);
        }
    }

    // NOU (Etapa 2): afiseaza o carte direct dintr-un CardData (deja completat din cloud
    // de catre GameManager). Folosit pentru cartile din mana.
    public void SetupFromCardData(CardData data)
    {
        card = data;
        if (data == null) return;

        if (nameText != null) nameText.text = data.cardName;
        if (attackText != null) attackText.text = data.attack.ToString();
        if (hpText != null) hpText.text = data.hp.ToString();
        if (manaText != null) manaText.text = data.manaCost.ToString();
        if (flavorText != null && !string.IsNullOrEmpty(data.flavorText)) flavorText.text = data.flavorText;
        if (artworkImage != null && data.artwork != null) artworkImage.sprite = data.artwork;
    }

    // NOU (Etapa 2): afiseaza o creatura de pe tabla (cu HP-ul CURENT, nu cel de baza).
    public void SetupFromInstance(RuntimeCardInstance inst)
    {
        if (inst == null) return;
        SetupFromCardData(inst.cardData);
        if (hpText != null) hpText.text = inst.currentHP.ToString();
    }
}