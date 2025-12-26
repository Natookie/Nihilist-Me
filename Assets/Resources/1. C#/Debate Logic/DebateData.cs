using UnityEngine;
using System.Collections.Generic;

public static class DebateData{
    public static readonly Dictionary<string, string[]> FallacyPool = new Dictionary<string, string[]>{
        {"attack", new[]{ "Ad Hominem", "Strawman", "False Dilemma" }},
        {"logic_bend", new[]{ "False Equivalence", "Circular Reasoning", "No True Scotsman" }},
        {"diversion", new[]{ "Red Herring", "Tu Quoque" }},
        {"emotional", new[]{ "Appeal to Emotion", "Bandwagon", "Overgeneralization" }},
        {"neutral", new[]{ "Slippery Slope", "False Balance", "Ambiguity" }}
    };

    public static readonly List<string> SuitableForOpening = new List<string>{ 
        "Ad Hominem", 
        "Strawman", 
        "Appeal to Emotion", 
        "Bandwagon", 
        "False Dilemma", 
        "Slippery Slope" 
    };

    public static readonly string[] RandomSpectators = new string[]{
        "Interesting point!", "I hadn't considered that.", "That's a bold claim.",
        "Can you back that up?", "I'm not convinced.", "Well argued!",
        "That's a common misconception.", "I see where you're coming from.",
        "Could you elaborate?", "That's quite the fallacy!"
    };

    public static readonly string[] AiNames = new string[]{
        "Alex", "Riley", "Jordan", "Taylor", "Casey", "Morgan", "Sam", 
        "Quinn", "Avery", "Dakota"
    };
}
