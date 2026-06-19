using System.Collections.Generic;

[System.Serializable]
public class CardDefinition
{
    public string id;
    public string text;
    public string image;
    public string category;
    public string[] tags;
}

[System.Serializable]
public class ConnectionDefinition
{
    public string card1;
    public string card2;
    public float strength;
}

[System.Serializable]
public class ConsumableFreeUses
{
    public string id;
    public int freeUses;
}

[System.Serializable]
public class LevelDefinition
{
    public int id;
    public string activeCard;
    public string[] hand;
    public string[] deck;
    public int maxMoves;
    public int optimalMoves;
    public ConsumableFreeUses[] consumableFreeUses;

    public int GetFreeUses(string consumableId)
    {
        if (consumableFreeUses == null) return 0;
        foreach (ConsumableFreeUses c in consumableFreeUses)
            if (c.id == consumableId) return c.freeUses;
        return 0;
    }
}

[System.Serializable]
public class TagRule
{
    public string tag1;
    public string tag2;
    public float strength;
}

[System.Serializable]
public class RuntimeConnection
{
    public string targetId;
    public float strength;
}

// JsonUtility cannot deserialize bare JSON arrays — wrap with these helpers at load time.
[System.Serializable]
public class CardDefinitionList { public List<CardDefinition> items; }
[System.Serializable]
public class ConnectionDefinitionList { public List<ConnectionDefinition> items; }
[System.Serializable]
public class TagRuleList { public List<TagRule> items; }
[System.Serializable]
public class LevelVariantList { public List<LevelDefinition> items; }
