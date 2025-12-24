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