using UnityEngine;

[System.Serializable]
public class UnitStats
{
    public string Name;
    public int MaxHP, HP, Attack, Defense;
    public bool IsDead => HP <= 0;

    public static UnitStats Hero(string name, int hp, int atk, int def)
        => new UnitStats { Name = name, MaxHP = hp, HP = hp, Attack = atk, Defense = def };

    public static UnitStats Enemy(string name, int hp, int atk, int def)
        => new UnitStats { Name = name, MaxHP = hp, HP = hp, Attack = atk, Defense = def };
}