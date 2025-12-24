using UnityEngine;

public interface IDebateLogger
{
    void InitializeLogs(string opponentName, int maxTurns, int maxPoorResponses, string model, string url);
    void LogTopic(string header, string opening, string opponentName);
    void LogPlayerTurn(int turnCount, string reply, JudgeScore score, string judgeFeedback, int consecutivePoorResponses);
    void LogOpponentTurn(int turnCount, string opponentName, OpponentTurn turn, ResponseTier tier);
    void LogPrompt(string promptType, string prompt, ResponseTier tier, int qualityScore, JudgeScore judgeScore, int consecutivePoorResponses);
    void EndDebate(string reason, string performanceSummary);
    string GetFullLog(string logType = "debate");
}