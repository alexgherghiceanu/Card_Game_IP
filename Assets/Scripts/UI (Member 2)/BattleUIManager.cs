using UnityEngine;
using TMPro;

public class BattleUIManager : MonoBehaviour
{
    [Header("Zone carti (pentru membrul 4)")]
    public Transform playerHandArea;
    public Transform playerBoardArea;

    [Header("HUD Stats (pentru membrul 3)")]
    public TextMeshProUGUI playerHPText;
    public TextMeshProUGUI playerManaText;

    public void UpdatePlayerHP(int newHP)
    {
        if (playerHPText != null)
        {
            playerHPText.text = "HP: " + newHP;
        }
    }

    public void UpdatePlayerMana(int currentMana, int maxMana)
    {
        if (playerManaText != null)
        {
            playerManaText.text = "Mana: " + currentMana + "/" + maxMana;
        }
    }
}