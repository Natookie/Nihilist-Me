using System;
using UnityEngine;

[Serializable]
public class DebateTopic
{
    public string header;
    public string opening;
}

[Serializable]
public class OpponentTurn
{
    public string argument;
    public string fallacy_type;
}

[Serializable]
public class JudgeScore
{
    public int fallacy_score;
    public int logic_score;
    public int insult_score;
    public int total_score;
    public string feedback;
}

[Serializable]
public class ConversationEntry
{
    public string speaker;
    public string message;
    public string fallacyUsed;
    public int score;

    public ConversationEntry(string speaker, string message, string fallacyUsed = "", int score = 0)
    {
        this.speaker = speaker;
        this.message = message;
        this.fallacyUsed = fallacyUsed;
        this.score = score;
    }
}
