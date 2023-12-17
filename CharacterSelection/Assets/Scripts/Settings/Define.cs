using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class CharacterData {
    public string Name;
    public Sprite SmallPortrait;
    public Sprite FullBodyPortrait;
}

public class CharacterSelectionData {
    public string Name;
    public Sprite SmallPortrait;
    public Sprite FullBodyPortrait;
    public int HP;
    public int MP;
    public int Attack;
    public int Defense;
}


