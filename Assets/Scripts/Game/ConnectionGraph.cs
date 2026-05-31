using System.Collections.Generic;

public class ConnectionGraph
{
    private readonly Dictionary<string, List<RuntimeConnection>> graph = new Dictionary<string, List<RuntimeConnection>>();

    public void Build(List<CardDefinition> cards, List<TagRule> tagRules,
                      List<ConnectionDefinition> overrides)
    {
        graph.Clear();

        // Auto-generate connections from tag rules.
        for (int i = 0; i < cards.Count; i++)
        for (int j = i + 1; j < cards.Count; j++)
        {
            float s = MaxTagStrength(cards[i].tags, cards[j].tags, tagRules);
            if (s > 0f)
            {
                AddEdge(cards[i].id, cards[j].id, s);
                AddEdge(cards[j].id, cards[i].id, s);
            }
        }

        // Explicit overrides replace the tag-generated strength (or add if no tag match).
        foreach (ConnectionDefinition c in overrides)
        {
            SetEdge(c.card1, c.card2, c.strength);
            SetEdge(c.card2, c.card1, c.strength);
        }
    }

    public bool CanPlay(string activeId, string targetId)
    {
        if (string.IsNullOrEmpty(activeId) || string.IsNullOrEmpty(targetId)) return false;
        return graph.TryGetValue(activeId, out var edges)
            && edges.Exists(e => e.targetId == targetId);
    }

    public List<RuntimeConnection> GetConnections(string cardId) =>
        graph.TryGetValue(cardId, out var list) ? list : new List<RuntimeConnection>();

    private float MaxTagStrength(string[] a, string[] b, List<TagRule> rules)
    {
        if (a == null || b == null) return 0f;
        float max = 0f;
        foreach (string t1 in a)
        foreach (string t2 in b)
        {
            TagRule r = rules.Find(r =>
                (r.tag1 == t1 && r.tag2 == t2) || (r.tag1 == t2 && r.tag2 == t1));
            if (r != null && r.strength > max) max = r.strength;
        }
        return max;
    }

    private void AddEdge(string from, string to, float strength)
    {
        if (!graph.ContainsKey(from))
            graph[from] = new List<RuntimeConnection>();
        graph[from].Add(new RuntimeConnection { targetId = to, strength = strength });
    }

    private void SetEdge(string from, string to, float strength)
    {
        if (!graph.ContainsKey(from))
            graph[from] = new List<RuntimeConnection>();
        var existing = graph[from].Find(e => e.targetId == to);
        if (existing != null) existing.strength = strength;
        else graph[from].Add(new RuntimeConnection { targetId = to, strength = strength });
    }
}
