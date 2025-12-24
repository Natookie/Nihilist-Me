using System;
using System.IO;
using UnityEngine;

public class FileDebateLogger : IDebateLogger
{
    private bool generateLog;
    private string logDirectory;
    private string logFileName = "debate_log.txt";
    private string promptLogFileName = "prompt_log.txt";

    public FileDebateLogger(bool generateLog, string logDirectory = ""){
        this.generateLog = generateLog;
        this.logDirectory = string.IsNullOrEmpty(logDirectory) ? Application.dataPath : logDirectory;
        if(!Directory.Exists(this.logDirectory)) Directory.CreateDirectory(this.logDirectory);
    }

    public void InitializeLogs(string opponentName, int maxTurns, int maxPoorResponses, string model, string url){
        if(!generateLog) return;

        string debateLogPath = GetLogPath("debate");
        string debateHeader = $"=== DEBATE LOG - {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===\n" +
                              $"Max Turns: {maxTurns}\n" +
                              $"Max Poor Responses: {maxPoorResponses}\n" +
                              $"Opponent: {opponentName}\n";
        File.WriteAllText(debateLogPath, debateHeader);

        string promptLogPath = GetLogPath("prompt");
        string promptHeader = $"=== PROMPT LOG - {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===\n" +
                              $"Model: {model}\n" +
                              $"URL: {url}\n";
        File.WriteAllText(promptLogPath, promptHeader);
    }

    public void LogTopic(string header, string opening, string opponentName){
        if(!generateLog) return;
        string logEntry = $"[TOPIC] {DateTime.Now:HH:mm:ss}\n" +
                          $"Header: {header}\n" +
                          $"Opening: {opening}\n" +
                          $"Opponent: {opponentName}\n" +
                          new string('-', 50) + "\n";
        AppendToLog(logEntry, "debate");
    }

    public void LogPlayerTurn(int turnCount, string reply, JudgeScore score, string judgeFeedback, int consecutivePoorResponses){
        if(!generateLog) return;
        string qualityNote = consecutivePoorResponses > 0 ? $"[POOR RESPONSE #{consecutivePoorResponses}] " : "";
        string logEntry = $"[TURN {turnCount} - PLAYER] {DateTime.Now:HH:mm:ss}\n" +
                          $"{qualityNote}Reply: {reply}\n" +
                          $"Score: F{score.fallacy_score} L{score.logic_score} I{score.insult_score} (Total: {score.total_score})\n" +
                          $"Judge: {judgeFeedback}\n" +
                          new string('-', 30) + "\n";
        AppendToLog(logEntry, "debate");
    }

    public void LogOpponentTurn(int turnCount, string opponentName, OpponentTurn turn, ResponseTier tier){
        if(!generateLog) return;
        string tierNote = tier switch{
            ResponseTier.Gibberish => "[GIBBERISH RESPONSE] ",
            ResponseTier.Warning => "[WARNING RESPONSE] ",
            _ => ""
        };
        string logEntry = $"[TURN {turnCount} - {opponentName}] {DateTime.Now:HH:mm:ss}\n" +
                          $"{tierNote}Argument: {turn.argument}\n" +
                          $"Fallacy: {turn.fallacy_type}\n" +
                          new string('-', 50) + "\n";
        AppendToLog(logEntry, "debate");
    }

    public void LogPrompt(string promptType, string prompt, ResponseTier tier, int qualityScore, JudgeScore judgeScore, int consecutivePoorResponses){
        if(!generateLog) return;
        string context = $"\n[CONTEXT] Tier: {tier}, Quality: {qualityScore}, Judge Score: {judgeScore?.total_score ?? 0}, Poor Responses: {consecutivePoorResponses}";
        string logEntry = $"[PROMPT] {DateTime.Now:HH:mm:ss}\n" +
                          $"Type: {promptType}{context}\n" +
                          $"{prompt}\n" +
                          new string('=', 80) + "\n";
        AppendToLog(logEntry, "prompt");
    }

    public void EndDebate(string reason, string performanceSummary){
        if(!generateLog) return;
        string logEntry = $"\n[DEBATE ENDED]\nReason: {reason}\n{performanceSummary}\n{new string('=', 60)}\n";
        AppendToLog(logEntry, "debate");
    }

    public string GetFullLog(string logType = "debate"){
        if(!generateLog) return "Logging disabled";
        string path = GetLogPath(logType);
        return File.Exists(path) ? File.ReadAllText(path) : $"No {logType} log file found";
    }

    string GetLogPath(string logType){
        string fileName = logType == "prompt" ? promptLogFileName : logFileName;
        return Path.Combine(logDirectory, fileName);
    }

    void AppendToLog(string content, string logType){
        if(!generateLog) return;
        try{
            File.AppendAllText(GetLogPath(logType), content);
        }
        catch (Exception e){
            Debug.LogError($"[DebateLog] Failed to write {logType} log: {e.Message}");
        }
    }
}