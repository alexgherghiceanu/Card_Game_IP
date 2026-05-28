using System;
using System.Collections.Generic;

/// <summary>
/// Reprezinta starea unui jucator IN INTERIORUL unui meci.
/// Corespunde clasei "JucatorMeci" din diagrama de clase (doc. de arhitectura).
///
/// Este o clasa simpla [Serializable], NU un MonoBehaviour, ca sa nu fie nevoie
/// de niciun GameObject sau wiring suplimentar in Unity. Tot ce tine de un
/// jucator dintr-un meci (erou, mana, deck, mana, board) sta aici, iar
/// GameManager (clasa "Meci") detine doi astfel de jucatori: tu si adversarul.
/// </summary>
[Serializable]
public class JucatorMeci
{
    public string numeJucator = "Jucator";

    // --- Eroul (Hero) ---
    public int hpMax = 30;
    public int hpCurent = 30;

    // --- Mana (resursa de joc) ---
    public int manaMaxima = 0;   // creste cu 1 la inceputul fiecarei ture (pana la o limita)
    public int manaCurenta = 0;  // se reface la valoarea maxima la inceputul turei

    // --- Cartile (zonele de joc) ---
    public List<CardData> deck = new List<CardData>();              // pachetul din care tragi
    public List<CardData> mana = new List<CardData>();              // cartile din mana (in hand)
    public List<RuntimeCardInstance> board = new List<RuntimeCardInstance>(); // creaturile pe tabla

    public bool esteControlatDeAI = false; // true pentru adversarul AI (Etapa 1)

    public JucatorMeci() { }

    public JucatorMeci(string nume, int hpInitial)
    {
        numeJucator = nume;
        hpMax = hpInitial;
        hpCurent = hpInitial;
    }

    /// <summary>Scade HP-ul eroului. Corespunde "primesteDamage(valoare)" din diagrama.</summary>
    public void PrimesteDamage(int valoare)
    {
        if (valoare <= 0) return;

        hpCurent -= valoare;
        if (hpCurent < 0) hpCurent = 0;
    }

    /// <summary>
    /// Incearca sa consume mana. Corespunde "consumaMana(valoare)" din diagrama.
    /// Returneaza true daca a reusit (avea destula mana), false altfel.
    /// </summary>
    public bool ConsumaMana(int valoare)
    {
        if (valoare < 0) valoare = 0;

        if (manaCurenta < valoare)
            return false;

        manaCurenta -= valoare;
        return true;
    }

    public bool AreDestulaMana(int cost)
    {
        return manaCurenta >= cost;
    }

    public bool EsteInViata()
    {
        return hpCurent > 0;
    }
}