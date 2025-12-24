using UnityEngine;

public interface IResponseQualityEvaluator
{
    int CalculateQuality(string input);
    ResponseTier DetermineTier(int qualityScore, JudgeScore judgeScore);
}

public enum ResponseTier
{
    Gibberish,
    Warning,
    Normal
}
