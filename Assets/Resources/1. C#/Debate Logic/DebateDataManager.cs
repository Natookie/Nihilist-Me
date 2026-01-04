using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DebateDataManager : MonoBehaviour
{
    public static DebateDataManager Instance { get; private set; }
    
    public string ollamaURL = "http://127.0.0.1:11434/api/generate";
    public string model = "mistral";
    public float requestTimeoutSeconds = 20f;
    
    public int maxTurns = 10;
    public float minInputInterval = 2f;
    public int maxPoorResponses = 3;
    public bool generateLog = true;
    
    public DebateTopic currentTopic;
    public OpponentTurn currentOpponentTurn;
    public JudgeScore lastJudgeScore;
    public string lastPlayerReply;
    public string currentJudgeFeedback;
    
    public List<ConversationEntry> conversationHistory = new List<ConversationEntry>();
    public int completedRoundCount = 0;
    public float lastInputTime = 0f;
    
    public List<int> recentScores = new List<int>();
    public float averageScore = 0f;
    public int totalFallaciesIdentified = 0;
    public int highQualityTurns = 0;
    public int consecutivePoorResponses = 0;
    public int aiResponseAppropriateness = 0;
    
    public string currentOpponentName;
    public bool isDebateActive = false;
    public DebateState currentState = DebateState.Idle;
    
    public int winCount = 0;
    public int loseCount = 0;
    
    public string cachedPerformanceSummary;
    public string cachedEndReason;
    
    public int currentAutoReplyIndex = 0;
    public bool isFirstAutoTurn = true;
    
    public Dictionary<string, string[]> fallacyPool;
    public List<string> suitableForOpening;
    
    public enum DebateState { Idle, WaitingForTopic, WaitingForJudge, WaitingForOpponent, DebateEnded }
    
    void Awake(){
        if(Instance != null && Instance != this){
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        InitializeAIData();
    }
    
    void InitializeAIData(){
        fallacyPool = new Dictionary<string, string[]>();
        foreach(var kvp in DebateData.FallacyPool){
            fallacyPool[kvp.Key] = (string[])kvp.Value.Clone();
        }
        
        suitableForOpening = new List<string>(DebateData.SuitableForOpening);
    }
    
    public void ResetConversationState(){
        currentTopic = null;
        currentOpponentTurn = null;
        lastJudgeScore = null;
        lastPlayerReply = null;
        currentJudgeFeedback = null;
        completedRoundCount = 0;
        consecutivePoorResponses = 0;
        aiResponseAppropriateness = 0;
        currentState = DebateState.Idle;
        
        SetRandomOpponentName();
        
        conversationHistory.Clear();
        recentScores.Clear();
        averageScore = 0f;
        totalFallaciesIdentified = 0;
        highQualityTurns = 0;
        isDebateActive = false;
        
        currentAutoReplyIndex = 0;
        isFirstAutoTurn = true;
        cachedPerformanceSummary = null;
        cachedEndReason = null;
    }
    
    public void SetRandomOpponentName(){
        if(DebateData.AiNames != null && DebateData.AiNames.Length > 0) currentOpponentName = DebateData.AiNames[Random.Range(0, DebateData.AiNames.Length)];
        else currentOpponentName = "DebateAI";
    }
    
    public void AddToHistory(string speaker, string message, string fallacyUsed = "", int score = 0){
        conversationHistory.Add(new ConversationEntry(speaker, message, fallacyUsed, score));
        if(conversationHistory.Count > 8) conversationHistory.RemoveAt(0);
    }
    
    public string BuildHistoryContext(){
        if(conversationHistory.Count == 0) return "No previous exchanges.";
        
        var recentHistory = conversationHistory.TakeLast(4);
        return string.Join("\n", recentHistory.Select(entry => 
            $"{entry.speaker}: {entry.message}" + 
            (entry.score > 0 ? $" [Score: {entry.score}]" : "") +
            (!string.IsNullOrEmpty(entry.fallacyUsed) ? $" [Fallacy: {entry.fallacyUsed}]" : "")
        ));
    }
    
    public void UpdatePerformanceMetrics(JudgeScore score, string playerReply, IResponseQualityEvaluator qualityEvaluator){
        if(score == null || qualityEvaluator == null) return;
            
        int quality = qualityEvaluator.CalculateQuality(playerReply);
        ResponseTier tier = qualityEvaluator.DetermineTier(quality, lastJudgeScore);
        bool isQualityTurn = score.total_score >= 15 && quality >= 7;
        
        recentScores.Add(score.total_score);
        if(recentScores.Count > 5) recentScores.RemoveAt(0);
        
        averageScore = recentScores.Count > 0 ? (float)recentScores.Average() : 0f;
        
        if(isQualityTurn) highQualityTurns++;
        if(score.fallacy_score >= 7) totalFallaciesIdentified++;
    }
    
    public string GetPerformanceSummary(){
        float successRate = completedRoundCount > 0 ? (float)highQualityTurns / completedRoundCount * 100 : 0;
        float appropriatenessRate = completedRoundCount > 0 ? (float)aiResponseAppropriateness / completedRoundCount * 100 : 0;
        
        return $@"
        DEBATE PERFORMANCE SUMMARY:
        \n- Total Turns: {completedRoundCount}
        \n- Average Score: {averageScore:F1}
        \n- Fallacies Identified: {totalFallaciesIdentified}
        \n- High Quality Turns: {highQualityTurns}
        \n- Success Rate: {successRate:F1}%
        \n- AI Response Appropriateness: {appropriatenessRate:F1}%
        \n- Engagement Issues: {consecutivePoorResponses} poor responses
        ";
    }
    
    public void UpdateWinLoseCount(){
        if(averageScore >= 10f && consecutivePoorResponses < maxPoorResponses) winCount++;
        else loseCount++;
    }
    
    public static void ResetWinLoseCount(){
        if(Instance != null){
            Instance.winCount = 0;
            Instance.loseCount = 0;
        }
    }
    
    [System.Serializable]
    public class DebateSaveData{
        public DebateTopic currentTopic;
        public OpponentTurn currentOpponentTurn;
        public JudgeScore lastJudgeScore;
        public string lastPlayerReply;
        public List<ConversationEntry> conversationHistory;
        public int completedRoundCount;
        public List<int> recentScores;
        public float averageScore;
        public int totalFallaciesIdentified;
        public int highQualityTurns;
        public int consecutivePoorResponses;
        public string currentOpponentName;
        public bool isDebateActive;
        public DebateState currentState;
        public int winCount;
        public int loseCount;
    }
    
    public DebateSaveData GetSaveData(){
        return new DebateSaveData{
            currentTopic = currentTopic,
            currentOpponentTurn = currentOpponentTurn,
            lastJudgeScore = lastJudgeScore,
            lastPlayerReply = lastPlayerReply,
            conversationHistory = conversationHistory,
            completedRoundCount = completedRoundCount,
            recentScores = recentScores,
            averageScore = averageScore,
            totalFallaciesIdentified = totalFallaciesIdentified,
            highQualityTurns = highQualityTurns,
            consecutivePoorResponses = consecutivePoorResponses,
            currentOpponentName = currentOpponentName,
            isDebateActive = isDebateActive,
            currentState = currentState,
            winCount = winCount,
            loseCount = loseCount
        };
    }
    
    public void LoadSaveData(DebateSaveData data){
        if(data == null) return;
            
        currentTopic = data.currentTopic;
        currentOpponentTurn = data.currentOpponentTurn;
        lastJudgeScore = data.lastJudgeScore;
        lastPlayerReply = data.lastPlayerReply;
        conversationHistory = data.conversationHistory ?? new List<ConversationEntry>();
        completedRoundCount = data.completedRoundCount;
        recentScores = data.recentScores ?? new List<int>();
        averageScore = data.averageScore;
        totalFallaciesIdentified = data.totalFallaciesIdentified;
        highQualityTurns = data.highQualityTurns;
        consecutivePoorResponses = data.consecutivePoorResponses;
        currentOpponentName = data.currentOpponentName;
        isDebateActive = data.isDebateActive;
        currentState = data.currentState;
        winCount = data.winCount;
        loseCount = data.loseCount;
    }
}