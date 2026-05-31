using System.Collections.Generic;
using UnityEngine;

public static class LevelLoader
{
    public static List<CardDefinition> Cards;
    public static List<TagRule> TagRules;
    public static List<ConnectionDefinition> Connections;
    public static List<LevelDefinition> Levels;

    public static void LoadAll()
    {
        Cards = LoadArray<CardDefinitionList, CardDefinition>("Data/cards");
        TagRules = LoadArray<TagRuleList, TagRule>("Data/tag-rules");
        Connections = LoadArray<ConnectionDefinitionList, ConnectionDefinition>("Data/connections");
        Levels = LoadLevels("Data/Levels");
    }

    public static LevelDefinition GetLevel(int id) =>
        Levels?.Find(l => l.id == id);

    public static CardDefinition GetCard(string id) =>
        Cards?.Find(c => c.id == id);

    private static List<LevelDefinition> LoadLevels(string folder)
    {
        var levels = new List<LevelDefinition>();
        int id = 1;
        while (true)
        {
            TextAsset asset = Resources.Load<TextAsset>($"{folder}/level_{id}");
            if (asset == null) break;
            string wrapped = "{\"items\":" + asset.text + "}";
            var wrapper = JsonUtility.FromJson<LevelVariantList>(wrapped);
            var variants = (List<LevelDefinition>)typeof(LevelVariantList).GetField("items").GetValue(wrapper);
            if (variants != null && variants.Count > 0)
            {
                var picked = variants[Random.Range(0, variants.Count)];
                picked.id = id;
                levels.Add(picked);
            }
            id++;
        }
        return levels;
    }

    private static List<TItem> LoadArray<TWrapper, TItem>(string resourcePath)
        where TWrapper : class
    {
        TextAsset asset = Resources.Load<TextAsset>(resourcePath);
        if (asset == null)
        {
            Debug.LogError($"LevelLoader: missing resource at {resourcePath}");
            return new List<TItem>();
        }
        // Wrap the bare JSON array so JsonUtility can parse it.
        string wrapped = "{\"items\":" + asset.text + "}";
        var wrapper = JsonUtility.FromJson<TWrapper>(wrapped);

        // Use reflection to pull the items list — avoids duplicating logic per type.
        var field = typeof(TWrapper).GetField("items");
        return (List<TItem>)field.GetValue(wrapper);
    }
}
