using UnityEngine;

[CreateAssetMenu(fileName = "New Card", menuName = "Card Game/Card Data")]
public class CardData : ScriptableObject
{
    public string cardID;
    public string cardName;
    public int manaCost;
    public int attack;
    public int hp;
    public string cardClass;

    // NOU (Etapa 1): camp de status, cerut in specificatii ("status : varchar(50)").
    // Daca este "Imobil", creatura NU poate ataca. Gol = creatura normala.
    public string status;

    [TextArea(3, 10)]
    public string flavorText;
    public Sprite artwork;
}