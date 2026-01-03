using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Networking;
using Nova;
using NovaSamples.UIControls;
using System.Linq;
using System.IO;

public class OnlineDebateManager : MonoBehaviour
{
    public static OnlineDebateManager Instance { get; private set; }

    [Header("OLLAMA")]
    public string ollamaURL = "http://127.0.0.1:11434/api/generate";
    public string model = "mistral";
    public float requestTimeoutSeconds = 20f;
    public DebateNetwork network;

    [Header("DEBATE CONFIGURATION")]
    public int maxTurns = 10;
    public float minInputInterval = 2f;
    public int maxPoorResponses = 3;
    [Space(10)]
    public bool generateLog = true;

    [Header("TOPIC")]
    public TextBlock topicTextField;
    public TextBlock titleHeader;
    public TextBlock titleOpening;
    public TextBlock likeCount;
    public TextBlock commentCount;

    [Header("CONTENT")]
    public TextBlock replyTextField;
    public TextBlock playerScoring;
    public TextBlock completedRoundCounterText;
    public TextBlock performanceText;

    [Header("PREFAB")]
    public GameObject playerCommentPrefab;
    public GameObject enemyCommentPrefab;
    public GameObject systemCommentPrefab;
    public Transform chatContainer;
    public Scroller scroller;

    [Header("REFERENCES")]
    public EngagementLogic engagementLogic;
    public DictionaryManager dictionaryManager;
    [Space(10)]
    public IResponseQualityEvaluator qualityEvaluator;
    public IDebateLogger debateLogger;

    [Header("AUTO COMPLETE")]
    public bool autoComplete = false;
    public string autoTopic = "hotdog";
    public string[] autoReplies;

    private int currentAutoReplyIndex = 0;
    private bool isFirstAutoTurn = true;

    [Header("PROMPT TEMPLATES")]
    public System.Action<string> OnPromptGenerated;

    //Debate State
    private DebateTopic currentTopic;
    private OpponentTurn currentOpponentTurn;
    private JudgeScore lastJudgeScore;
    private string lastPlayerReply;
    private string currentJudgeFeedback;

    //State Management
    private DebateState currentState = DebateState.Idle;
    private enum DebateState { Idle, WaitingForTopic, WaitingForJudge, WaitingForOpponent, DebateEnded }

    //Conversation History
    private List<ConversationEntry> conversationHistory = new List<ConversationEntry>();
    private int completedRoundCount = 0;
    private float lastInputTime = 0f;

    //Performance Monitoring & Quality Control
    private List<int> recentScores = new List<int>();
    private float averageScore = 0f;
    private int totalFallaciesIdentified = 0;
    private int highQualityTurns = 0;
    private int consecutivePoorResponses = 0;
    private int aiResponseAppropriateness = 0;

    public string currentOpponentName;
    private const int MAX_MESSAGES = 50;

    public bool isDebateActive;
    static public bool isDebateEnded => Instance != null && Instance.currentState == DebateState.DebateEnded;

    // Win lose count
    [HideInInspector] static public int winCount = 0;
    [HideInInspector] static public int loseCount = 0;

    [System.Serializable]
    public class ConversationEntry{
        public string speaker;
        public string message;
        public string fallacyUsed;
        public int score;

        public ConversationEntry(string speaker, string message, string fallacyUsed = "", int score = 0){
            this.speaker = speaker;
            this.message = message;
            this.fallacyUsed = fallacyUsed;
            this.score = score;
        }
    }

    void Awake(){
        if(Instance != null && Instance != this){
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if(qualityEvaluator == null) qualityEvaluator = new ResponseQualityEvaluator();
        if(debateLogger == null) debateLogger = new FileDebateLogger(generateLog: generateLog, logDirectory: "");
    }

    void Start(){
        if(autoComplete) StartCoroutine(StartAutoDebate());
    }

    void Update(){
        if(!Input.GetKeyDown(KeyCode.Return) && !Input.GetKeyDown(KeyCode.KeypadEnter)) return;
 
        if(!isDebateActive) SendTopic();
        else SendReply();
    }

    public void SendTopic(){
        string topic = topicTextField.Text.Trim();
        if(topic.Length <= 2) return;
            
        HandleTopicSubmission(topic);
    }
    public void SendReply(){
        if(autoComplete && isDebateActive){
            UseAutoReply();
            return;
        }

        string reply = replyTextField.Text.Trim();
        if(reply.Length == 0) return;

        replyTextField.Text = "";
        OnSendReply_Internal(reply);
    }

    #region PERFORMANCE MONITORING
    void UpdatePerformanceMetrics(JudgeScore score, string playerReply){
        int quality = qualityEvaluator.CalculateQuality(playerReply);
        ResponseTier tier = qualityEvaluator.DetermineTier(quality, lastJudgeScore);
        bool isQualityTurn = score.total_score >= 15 && quality >= 7;

        recentScores.Add(score.total_score);
        if(recentScores.Count > 5) recentScores.RemoveAt(0);
        averageScore = recentScores.Count > 0 ? (float)recentScores.Average() : 0f;

        if(isQualityTurn) highQualityTurns++;
        if(score.fallacy_score >= 7) totalFallaciesIdentified++;

        UpdatePerformanceUI();
    }

    void UpdatePerformanceUI(){
        if(performanceText == null) return;

        string performance = $"Avg: {averageScore:F1} | Fallacies: {totalFallaciesIdentified} | Quality: {highQualityTurns}/{completedRoundCount}";
        if(consecutivePoorResponses > 0) performance += $" | Warnings: {consecutivePoorResponses}/{maxPoorResponses}";
        performanceText.Text = performance;

        if(completedRoundCounterText != null) completedRoundCounterText.Text = $"Turn: {completedRoundCount}/{maxTurns}";
    }

    string GetPerformanceSummary(){
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
    public void ShowPerformanceSummary(){
        if(cachedPerformanceSummary == null) return;

        string fullMessage = $"{cachedPerformanceSummary}";
        InstantiateCommentPrefab(systemCommentPrefab, fullMessage, Role.System);
        
        if(scroller != null) StartCoroutine(ScrollToBottom());
        cachedPerformanceSummary = null;
    }
    #endregion

    #region CONVERSATION HISTORY
    void AddToHistory(string speaker, string message, string fallacyUsed = "", int score = 0){
        conversationHistory.Add(new ConversationEntry(speaker, message, fallacyUsed, score));
        if(conversationHistory.Count > 8) conversationHistory.RemoveAt(0);
    }

    string BuildHistoryContext(){
        if(conversationHistory.Count == 0) return "No previous exchanges.";

        var recentHistory = conversationHistory.TakeLast(4);
        return string.Join("\n", recentHistory.Select(entry => 
            $"{entry.speaker}: {entry.message}" + 
            (entry.score > 0 ? $" [Score: {entry.score}]" : "") +
            (!string.IsNullOrEmpty(entry.fallacyUsed) ? $" [Fallacy: {entry.fallacyUsed}]" : "")
        ));
    }
    #endregion

    #region TURN MANAGEMENT
    bool CanProceedToNextTurn(){
        if(completedRoundCount >= maxTurns){
            EndDebate("Maximum turns reached. Debate concluded.");
            engagementLogic.PopNotification(EngagementLogic.NotificationType.End, EngagementLogic.EndReason.MaxTurn);
            return false;
        }
        if(currentState == DebateState.DebateEnded) return false;

        if(Time.time - lastInputTime < minInputInterval) return false;
        return true;
    }

    private string cachedPerformanceSummary = null;
    private string cachedEndReason = null;
    public void EndDebate(string reason){
        currentState = DebateState.DebateEnded;
        StopAllCoroutines();
        UpdateWinLoseCount();

        if(generateLog) debateLogger.EndDebate(reason, cachedPerformanceSummary);
    }
    void UpdateWinLoseCount(){
        if(averageScore >= 10f && consecutivePoorResponses < maxPoorResponses) winCount++;
        else loseCount++;
    }
    static public void ResetWinloseCount(){
        winCount = 0;
        loseCount = 0;
    }
    #endregion

    #region IMPROVED SCORE-BASED LOGIC
    string ChooseTraitByScore(JudgeScore score){
        if(score == null) return "neutral";
        if(consecutivePoorResponses >= 2) return "attack";

        Dictionary<string, float> traitWeights = new Dictionary<string, float>{
            { "attack", Mathf.Clamp(score.insult_score / 10f, 0.1f, 0.8f) },
            { "logic_bend", Mathf.Clamp(score.logic_score / 12f, 0.1f, 0.7f) },
            { "diversion", Mathf.Clamp((30 - score.total_score) / 30f, 0.1f, 0.6f) },
            { "emotional", Mathf.Clamp((15 - score.total_score) / 15f, 0.1f, 0.7f) },
            { "neutral", 0.3f }
        };

        float turnFactor = completedRoundCount / (float)maxTurns;
        if(turnFactor > 0.7f){
            traitWeights["attack"] *= 1.5f;
            traitWeights["emotional"] *= 1.3f;
        }

        if(consecutivePoorResponses >= 2){
            traitWeights["diversion"] *= 1.4f;
            traitWeights["attack"] *= 1.2f;
        }

        foreach(var trait in traitWeights.Keys.ToList()) traitWeights[trait] *= Random.Range(0.8f, 1.2f);

        return traitWeights.OrderByDescending(x => x.Value).First().Key;
    }

    string GetAdaptiveToneRule(string chosenTrait, JudgeScore score){
        string baseTone = chosenTrait switch{
            "attack" => "Tone: aggressive, disgusted, and contemptuous — you're intellectually superior and they're wasting your time",
            "logic_bend" => "Tone: smug, intellectually superior, and condescending",
            "diversion" => "Tone: evasive and mocking their lack of intelligence",
            "emotional" => "Tone: emotionally charged, frustrated, and intellectually disgusted",
            _ => "Tone: composed but intellectually superior and subtly mocking"
        };

        if(score.total_score >= 25) baseTone += " — you're feeling intellectually challenged but determined to prove your superiority";
        else if(score.total_score <= 12) baseTone += " — you're confident in your intellectual superiority and mocking their incompetence";

        if(consecutivePoorResponses >= 3) baseTone += " — completely disgusted by their intellectual incapacity and ending this farce";
        else if(consecutivePoorResponses == 2) baseTone += " — intellectually disgusted and giving one final contemptuous warning";
        else if(consecutivePoorResponses == 1) baseTone += " — annoyed by their intellectual inferiority and losing patience";

        return baseTone;
    }

    string GetEmotionalState(ResponseTier tier){
        if(tier == ResponseTier.Gibberish) return "GENUINELY CONFUSED - Concerned about their mental state, questioning if they're having a stroke";
        
        return consecutivePoorResponses switch{
            >= 3 => "INTELLECTUALLY DISGUSTED - Genuinely contemptuous of their mental incapacity, ready to end this farce",
            2 => "CONTEMPTUOUS - Mocking their intellectual inferiority, one chance left",
            1 => "ANNOYED - Frustrated by their poor reasoning skills but willing to continue",
            _ => "PASSIONATELY ENGAGED - Emotionally invested in the debate, ready for intellectual combat"
        };
    }
    #endregion

    #region SOCIAL STATS
    void GenerateSocialStats(){
        if(likeCount == null && commentCount == null) return;

        int baseLikes = Random.Range(500, 5000);
        int engagementBoost = Random.Range(0, 15000);
        int likes = baseLikes + engagementBoost;
        
        float commentRatio = Random.Range(0.01f, 0.1f);
        int comments = Mathf.RoundToInt(likes * commentRatio) + Random.Range(0, 500);

        engagementLogic?.RecordEngagement(likes, comments);

        if(likeCount != null) likeCount.Text = FormatNumber(likes);
        if(commentCount != null) commentCount.Text = FormatNumber(comments);
    }
    #endregion

    #region UI LOGIC
    enum Role { Player, Enemy, System }

    GameObject InstantiateCommentPrefab(GameObject prefab, string message, Role role){
        if(prefab == null || chatContainer == null) return null;

        if(chatContainer.childCount >= MAX_MESSAGES){
            var oldestMessage = chatContainer.GetChild(0);
            Destroy(oldestMessage.gameObject);
        }

        GameObject go = Instantiate(prefab, chatContainer);
        var tbs = go.GetComponentsInChildren<TextBlock>(true);
        var fill = FindTextBlockByName(tbs, "Fill");
        SetTextOrWarn(tbs, fill, message, prefab.name);

        if(role == Role.Enemy) SetConsistentAiName(tbs);
        if(fill != null){
            int fillIndex = fill.transform.GetSiblingIndex();
            Transform parent = fill.transform.parent;
            
            if(parent.childCount > fillIndex + 1){
                Transform specialChild = parent.GetChild(fillIndex + 1);
                var specialTb = specialChild.GetChild(1).GetComponent<TextBlock>();
                
                if(specialTb != null){
                    ResponseTier? playerTier = null;
                    int quality = 0;
                    if(role == Role.Player){
                        quality = qualityEvaluator.CalculateQuality(lastPlayerReply);
                        playerTier = qualityEvaluator.DetermineTier(quality, lastJudgeScore);
                    }
                    
                    string specialText = "";
                    switch(role){
                        case Role.System:
                            specialText = $"Score: {lastJudgeScore?.total_score ?? 67} / 30";
                            break;
                        case Role.Player:
                            specialText = $"Tier: {playerTier?.ToString() ?? "N/A"} [{quality}]";
                            go.GetComponent<CommentData>().SetData(quality);
                            break;
                        case Role.Enemy:
                            specialText = $"Fallacy: {currentOpponentTurn?.fallacy_type ?? "none"}";
                            go.GetComponent<CommentData>().SetData((int)averageScore, "AI");
                            engagementLogic.PopNotification(EngagementLogic.NotificationType.Reply);
                            break;
                    }

                    // string specialText = role switch{
                    //     Role.System => $"Score: {lastJudgeScore?.total_score ?? 67} / 30",
                    //     Role.Player => $"Tier: {playerTier?.ToString() ?? "N/A"} [{quality}]",
                    //     Role.Enemy => $"Fallacy: {currentOpponentTurn?.fallacy_type ?? "none"}",
                    //     _ => ""
                    // };
                    
                    specialTb.Text = specialText;
                }
            }
        }

        if(scroller != null) StartCoroutine(ScrollToBottom());
        return go;
    }

    IEnumerator ScrollToBottom(){
        yield return new WaitForEndOfFrame();
        scroller.ScrollToIndex(scroller.ScrollableChildCount-1, true);
    }

    void SetTextOrWarn(TextBlock[] tbs, TextBlock fill, string message, string prefabName){
        string clean = SanitizeForDisplay(message);
        if(fill != null){ fill.Text = clean; return; }

        if(tbs.Length > 0){
            foreach(var tb in tbs){
                if(!string.Equals(tb.name, "Name", System.StringComparison.OrdinalIgnoreCase)){
                    tb.Text = clean;
                    return;
                }
            }
        }
    }

    void SetConsistentAiName(TextBlock[] tbs){
        var nameTB = FindTextBlockByName(tbs, "Name");
        if(nameTB != null){ 
            nameTB.Text = currentOpponentName; 
            return; 
        }
        
        foreach(var tb in tbs){
            if(tb.name.ToLower().Contains("name")){ 
                tb.Text = currentOpponentName; 
                break; 
            }
        }
    }

    void SetRandomOpponentName() => currentOpponentName = DebateData.AiNames[Random.Range(0, DebateData.AiNames.Length)];

    TextBlock FindTextBlockByName(TextBlock[] all, string target){
        if(all == null || all.Length == 0 || string.IsNullOrEmpty(target)) return null;

        foreach(var tb in all) if(string.Equals(tb.name, target, System.StringComparison.OrdinalIgnoreCase)) return tb;
        foreach(var tb in all) if(tb.gameObject.name.ToLower().Contains(target.ToLower())) return tb;
        return null;
    }

    public string FormatNumber(int n){
        if(n >= 1_000_000) return (n / 1_000_000f).ToString("0.#") + "M";
        if(n >= 1000) return (n / 1000f).ToString("0.#") + "K";
        return n.ToString();
    }
    #endregion

    #region ENTRYPOINTS
    void HandleTopicSubmission(string topic){
        if(currentState != DebateState.Idle) return;

        if(!autoComplete){
            bool isValidWord = true;
            if(dictionaryManager != null){
                string[] words = topic.Split(' ', System.StringSplitOptions.RemoveEmptyEntries);
                
                if(words.Length == 1){
                    if(!dictionaryManager.IsEnglishWord(topic)){
                        string suggestion = dictionaryManager.SuggestClosestWord(topic);
                        if(suggestion != topic && suggestion.Length > 0){
                            //Auto-correct
                            topicTextField.Text = suggestion;
                            topic = suggestion;
                            var textField = topicTextField.gameObject.GetComponent<TextField>();
                            if(textField != null) textField.MoveCursor(textField.CursorPosition.MoveToEnd(), false);
                        }else{
                            //Show warning then clear
                            topicTextField.Text = "Not a valid word";
                            topicTextField.Color = new Color32(255, 100, 100, 255);
                            isValidWord = false;

                            var textField = topicTextField.gameObject.GetComponent<TextField>();
                            if(textField != null) textField.MoveCursor(textField.CursorPosition.MoveToEnd(), false);
                            StartCoroutine(ClearTextFieldAfterDelay(1f));
                        }
                    }
                }
            }
            if(!isValidWord) return;
        }

        if(engagementLogic) engagementLogic.Init(topic);
        topicTextField.Color = new Color32(255, 255, 255, 255);
        isDebateActive = true;
        ResetConversationState();
        StartCoroutine(RequestDebateTopic(topic));
    }

    IEnumerator ClearTextFieldAfterDelay(float delay){
        yield return new WaitForSeconds(delay);
        topicTextField.Text = "";
        topicTextField.Color = new Color32(255, 255, 255, 255);
    }

    void ResetConversationState(){
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

        if(playerScoring != null) playerScoring.Text = "";
        if(titleHeader != null) titleHeader.Text = "";
        if(titleOpening != null) titleOpening.Text = "";
        if(likeCount != null) likeCount.Text = "0";
        if(commentCount != null) commentCount.Text = "0";
        UpdatePerformanceUI();

        if(generateLog) debateLogger.InitializeLogs(currentOpponentName, maxTurns, maxPoorResponses, model, ollamaURL);
    }

    public void OnSendReply(){
        if(replyTextField == null) return;
        string txt = replyTextField.Text?.Trim();
        if(string.IsNullOrEmpty(txt)) return;
        replyTextField.Text = "";
        OnSendReply_Internal(txt);
    }

    void OnSendReply_Internal(string playerText){
        if(!CanProceedToNextTurn()) return;
        if(currentState != DebateState.Idle) return;
        if(currentTopic == null) return; 

        lastInputTime = Time.time;
        lastPlayerReply = playerText;
        InstantiateCommentPrefab(playerCommentPrefab, playerText, Role.Player);
        currentState = DebateState.WaitingForJudge;
        
        StartCoroutine(RequestJudgeScore(currentOpponentTurn, playerText));
        if(autoComplete) StartCoroutine(ScheduleNextAutoReply());
    }
    #endregion

    #region TOPIC FLOW
    IEnumerator RequestDebateTopic(string topic){
        currentState = DebateState.WaitingForTopic;
        string chosenFallacy = DebateData.SuitableForOpening[Random.Range(0, DebateData.SuitableForOpening.Count)];
        engagementLogic.RecordFallacy(chosenFallacy);

        string prompt = string.Format(PromptTemplates.TopicPrompt, 
            EscapeForPrompt(topic), 
            chosenFallacy);

        debateLogger.LogPrompt("TOPIC_GENERATION", prompt, ResponseTier.Normal, 0, null, consecutivePoorResponses);

        yield return SendToOllama(prompt, result => {
            string clean = DebateJsonParser.ExtractJson(result);
            if(string.IsNullOrEmpty(clean)){ 
                StartCoroutine(RetryDebateTopic(prompt, topic, chosenFallacy));
                return;
            }

            if(!DebateJsonParser.TryParseJson<DebateTopic>(clean, out currentTopic)) currentTopic = LooseParseDebateTopic(clean);
            
            if(currentTopic == null){ 
                StartCoroutine(RetryDebateTopic(prompt, topic, chosenFallacy));
                return;
            }

            if(titleHeader != null) titleHeader.Text = currentTopic.header;
            if(titleOpening != null) titleOpening.Text = currentTopic.opening;
            engagementLogic.PopNotification(EngagementLogic.NotificationType.Topic);

            currentOpponentTurn = new OpponentTurn{ 
                argument = currentTopic.opening, 
                fallacy_type = chosenFallacy
            };

            AddToHistory(currentOpponentName, currentTopic.opening, chosenFallacy);
            GenerateSocialStats();
            if(generateLog) debateLogger.LogTopic(currentTopic.header, currentTopic.opening, currentOpponentName);
            
            currentState = DebateState.Idle;
        });
    }

    IEnumerator RetryDebateTopic(string prompt, string topic, string chosenFallacy){
        yield return SendToOllama(prompt, result => {
            string clean = DebateJsonParser.ExtractJson(result);
            if(string.IsNullOrEmpty(clean)){ 
                InstantiateCommentPrefab(systemCommentPrefab, "[Error] Could not parse debate topic.", Role.System); 
                currentState = DebateState.Idle;
                return; 
            }

            if(!DebateJsonParser.TryParseJson<DebateTopic>(clean, out currentTopic)) 
                currentTopic = LooseParseDebateTopic(clean);
            
            if(currentTopic == null){ 
                InstantiateCommentPrefab(systemCommentPrefab, "[Error] Could not parse debate topic.", Role.System); 
                currentState = DebateState.Idle;
                return; 
            }

            if(titleHeader != null) titleHeader.Text = currentTopic.header;
            if(titleOpening != null) titleOpening.Text = currentTopic.opening;

            currentOpponentTurn = new OpponentTurn{ 
                argument = currentTopic.opening, 
                fallacy_type = chosenFallacy
            };

            AddToHistory(currentOpponentName, currentTopic.opening, chosenFallacy);
            GenerateSocialStats();
            if(generateLog) debateLogger.LogTopic(currentTopic.header, currentTopic.opening, currentOpponentName);
            
            currentState = DebateState.Idle;
        });
    }
    #endregion

    #region SCORING FLOW
    IEnumerator RequestJudgeScore(OpponentTurn opponent, string playerReply){
        string prompt = string.Format(PromptTemplates.JudgePrompt,
            EscapeForPrompt(currentTopic.header),
            EscapeForPrompt(currentTopic.opening),
            EscapeForPrompt(opponent.fallacy_type),
            EscapeForPrompt(opponent.argument),
            EscapeForPrompt(playerReply),
            EscapeForPrompt(BuildHistoryContext()));

        int playerQuality = qualityEvaluator.CalculateQuality(playerReply);
        ResponseTier playerTier = qualityEvaluator.DetermineTier(playerQuality, null);
        debateLogger.LogPrompt("JUDGE_SCORING", prompt, playerTier, playerQuality, null, consecutivePoorResponses);

        yield return SendToOllama(prompt, result =>{
            string sanitized = DebateJsonParser.SanitizeModelOutput(result);
            string json = DebateJsonParser.ExtractJson(sanitized);
                
            if(string.IsNullOrEmpty(json)){
                StartCoroutine(RetryJudgeScore(prompt, opponent, playerReply));
                return;
            }

            if(!DebateJsonParser.TryParseJson<JudgeScore>(json, out lastJudgeScore)) 
                lastJudgeScore = LooseParseJudgeScore(json);
            
            if(lastJudgeScore == null){ 
                StartCoroutine(RetryJudgeScore(prompt, opponent, playerReply));
                return;
            }

            currentJudgeFeedback = lastJudgeScore.feedback ?? "No feedback available.";
            
            string displayFeedback = MakeFeedbackEducational(currentJudgeFeedback, lastJudgeScore);
            
            UpdatePerformanceMetrics(lastJudgeScore, playerReply);

            if(playerScoring != null) playerScoring.Text = $"F: {lastJudgeScore.fallacy_score}  L: {lastJudgeScore.logic_score}  I: {lastJudgeScore.insult_score}  [{lastJudgeScore.total_score}]";
            InstantiateCommentPrefab(systemCommentPrefab, displayFeedback, Role.System);
            AddToHistory("Player", playerReply, "", lastJudgeScore.total_score);

            if(generateLog) debateLogger.LogPlayerTurn(completedRoundCount, playerReply, lastJudgeScore, currentJudgeFeedback, consecutivePoorResponses);

            currentState = DebateState.WaitingForOpponent;
            StartCoroutine(RequestOpponentReply(lastJudgeScore, lastPlayerReply));
        });
    }

    IEnumerator RetryJudgeScore(string prompt, OpponentTurn opponent, string playerReply){
        yield return SendToOllama(prompt, result =>{
            string sanitized = DebateJsonParser.SanitizeModelOutput(result);
            string json = DebateJsonParser.ExtractJson(sanitized);
            
            if(string.IsNullOrEmpty(json)){
                InstantiateCommentPrefab(systemCommentPrefab, "[Error] Could not parse judge response.", Role.System);
                currentState = DebateState.Idle;
                return;
            }

            if(!DebateJsonParser.TryParseJson<JudgeScore>(json, out lastJudgeScore)) 
                lastJudgeScore = LooseParseJudgeScore(json);
            
            if(lastJudgeScore == null){
                InstantiateCommentPrefab(systemCommentPrefab, "[Error] Judge response parse failed.", Role.System); 
                currentState = DebateState.Idle;
                return; 
            }

            currentJudgeFeedback = lastJudgeScore.feedback ?? "No feedback available.";
            string displayFeedback = MakeFeedbackEducational(currentJudgeFeedback, lastJudgeScore);
            
            UpdatePerformanceMetrics(lastJudgeScore, playerReply);

            if(playerScoring != null) 
                playerScoring.Text = $"F: {lastJudgeScore.fallacy_score}  L: {lastJudgeScore.logic_score}  I: {lastJudgeScore.insult_score}  [{lastJudgeScore.total_score}]";
            
            InstantiateCommentPrefab(systemCommentPrefab, displayFeedback, Role.System);
            AddToHistory("Player", playerReply, "", lastJudgeScore.total_score);

            if(generateLog) debateLogger.LogPlayerTurn(completedRoundCount, playerReply, lastJudgeScore, currentJudgeFeedback, consecutivePoorResponses);

            currentState = DebateState.WaitingForOpponent;
            StartCoroutine(RequestOpponentReply(lastJudgeScore, lastPlayerReply));
        });
    }
    #endregion

    #region OPPONENT RESPONSE
    IEnumerator RequestOpponentReply(JudgeScore score, string playerReply){
        int responseQuality = qualityEvaluator.CalculateQuality(playerReply);
        ResponseTier responseTier = qualityEvaluator.DetermineTier(responseQuality, lastJudgeScore);
        
        bool isResponseAppropriateForTier = 
            (responseTier == ResponseTier.Gibberish && responseQuality <= 2) ||
            (responseTier == ResponseTier.Warning && responseQuality <= 5) ||
            (responseTier == ResponseTier.Normal && responseQuality > 5);
            
        if(isResponseAppropriateForTier) aiResponseAppropriateness++;
        if(responseTier != ResponseTier.Normal) consecutivePoorResponses++;
        else consecutivePoorResponses = 0;

        if(consecutivePoorResponses >= maxPoorResponses){
            currentOpponentTurn = GenerateFinalDisengagement(consecutivePoorResponses, playerReply);
            string finalResponse = BuildNaturalOpponentSpeech(currentOpponentTurn);
            InstantiateCommentPrefab(enemyCommentPrefab, finalResponse, Role.Enemy);
            AddToHistory(currentOpponentName, currentOpponentTurn.argument, "none");
            
            if(generateLog) debateLogger.LogOpponentTurn(completedRoundCount, currentOpponentName, currentOpponentTurn, responseTier);
            
            EndDebate($"{currentOpponentName} ended debate after {consecutivePoorResponses} poor responses.");
            engagementLogic.PopNotification(EngagementLogic.NotificationType.End, EngagementLogic.EndReason.PoorResponse);
            yield break;
        }

        string prompt;
        string promptType;
        string chosenFallacy = "";
        string chosenTrait = "";
        string normalFallacy = "";

        switch(responseTier){
            case ResponseTier.Gibberish:
                prompt = string.Format(PromptTemplates.OpponentGibberishPrompt,
                    EscapeForPrompt(playerReply),
                    EscapeForPrompt(currentOpponentTurn.argument),
                    BuildHistoryContext(),
                    consecutivePoorResponses);
                promptType = "GIBBERISH_RESPONSE";
                break;

            case ResponseTier.Warning:
                chosenFallacy = DebateData.FallacyPool["attack"][Random.Range(0, DebateData.FallacyPool["attack"].Length)];
                prompt = string.Format(PromptTemplates.OpponentWarningPrompt,
                    EscapeForPrompt(playerReply),
                    BuildHistoryContext(),
                    consecutivePoorResponses,
                    maxPoorResponses,
                    chosenFallacy);
                promptType = "WARNING_RESPONSE";
                break;

            default:
                chosenTrait = ChooseTraitByScore(score);
                normalFallacy = DebateData.FallacyPool[chosenTrait][Random.Range(0, DebateData.FallacyPool[chosenTrait].Length)];
                prompt = string.Format(PromptTemplates.OpponentNormalPrompt,
                    EscapeForPrompt(playerReply),
                    BuildHistoryContext(),
                    GetEmotionalState(responseTier),
                    normalFallacy,
                    GetAdaptiveToneRule(chosenTrait, score));
                promptType = "NORMAL_RESPONSE";
                break;
        }

        debateLogger.LogPrompt(promptType, prompt, responseTier, responseQuality, score, consecutivePoorResponses);

        //for retry
        ResponseTier localResponseTier = responseTier;
        JudgeScore localScore = score;
        string localPlayerReply = playerReply;
        int localResponseQuality = responseQuality;
        int localConsecutivePoorResponses = consecutivePoorResponses;
        string localChosenTrait = chosenTrait;
        string localFallacy = responseTier == ResponseTier.Warning ? chosenFallacy : normalFallacy;

        yield return SendToOllama(prompt, result =>{
            string clean = DebateJsonParser.ExtractJson(result);
            if(string.IsNullOrEmpty(clean)){ 
                StartCoroutine(RetryOpponentReply(prompt, localResponseTier, localScore, localPlayerReply, 
                    localResponseQuality, localConsecutivePoorResponses, localChosenTrait, localFallacy));
                return;
            }

            if(!DebateJsonParser.TryParseJson<OpponentTurn>(clean, out currentOpponentTurn)) 
                currentOpponentTurn = LooseParseOpponentTurn(clean);
            
            if(currentOpponentTurn == null){ 
                StartCoroutine(RetryOpponentReply(prompt, localResponseTier, localScore, localPlayerReply, 
                    localResponseQuality, localConsecutivePoorResponses, localChosenTrait, localFallacy));
                return;
            }

            string spoken = BuildNaturalOpponentSpeech(currentOpponentTurn);
            InstantiateCommentPrefab(enemyCommentPrefab, spoken, Role.Enemy);

            AddToHistory(currentOpponentName, currentOpponentTurn.argument, currentOpponentTurn.fallacy_type);

            if(generateLog) debateLogger.LogOpponentTurn(completedRoundCount, currentOpponentName, currentOpponentTurn, responseTier);
            completedRoundCount++;
            currentState = DebateState.Idle;
            
            if(completedRoundCount >= maxTurns) EndDebate("Maximum turns reached. Debate concluded.");
        });
    }

    IEnumerator RetryOpponentReply(string prompt, ResponseTier responseTier, JudgeScore score, 
        string playerReply, int responseQuality, int consecutivePoorResponses, string chosenTrait, string fallacy){
        
        yield return SendToOllama(prompt, result =>{
            string clean = DebateJsonParser.ExtractJson(result);
            if(string.IsNullOrEmpty(clean)){ 
                InstantiateCommentPrefab(systemCommentPrefab, "[Error] Could not parse opponent response.", Role.System); 
                currentState = DebateState.Idle;
                return; 
            }

            if(!DebateJsonParser.TryParseJson<OpponentTurn>(clean, out currentOpponentTurn)) 
                currentOpponentTurn = LooseParseOpponentTurn(clean);
            
            if(currentOpponentTurn == null){ 
                InstantiateCommentPrefab(systemCommentPrefab, "[Error] Opponent response parse failed.", Role.System); 
                currentState = DebateState.Idle;
                return; 
            }

            string spoken = BuildNaturalOpponentSpeech(currentOpponentTurn);
            InstantiateCommentPrefab(enemyCommentPrefab, spoken, Role.Enemy);

            AddToHistory(currentOpponentName, currentOpponentTurn.argument, currentOpponentTurn.fallacy_type);

            if(generateLog) debateLogger.LogOpponentTurn(completedRoundCount, currentOpponentName, currentOpponentTurn, responseTier);
            completedRoundCount++;
            if(completedRoundCount >= maxTurns) EndDebate("Maximum turns reached. Debate concluded.");
        });
    }

    OpponentTurn GenerateFinalDisengagement(int poorResponseCount, string lastPlayerReply){
        string[] finalResponses = {
            "I'm out. This conversation has devolved into pure nonsense. You've demonstrated that rational debate is impossible with someone at your intellectual level. Don't bother responding - I'm moving on to something actually worthwhile.",
            "Look, I've given you multiple chances to engage with actual arguments, but you keep posting nonsense. I'm done. This isn't worth my time.",
            "You know what? I'm not doing this anymore. You're either trolling or genuinely unable to form a coherent thought. Either way, I'm out.",
            "This has gone nowhere. I tried to have a real discussion, but you're clearly not capable of it. I'm moving on.",
            "I'm leaving this conversation. You've proven you can't engage honestly or intelligently. Goodbye.",
            "At this point it's obvious you're not interested in actual debate. Good luck with whatever this is supposed to be.",
            "That's it. I'm done wasting my energy here. You've had your chances.",
            "I can't believe I spent this much time on someone who can't even string together a coherent argument. I'm out."
        };

        string response = finalResponses[Random.Range(0, finalResponses.Length)];
        
        return new OpponentTurn{
            argument = response,
            fallacy_type = "none"
        };
    }
    #endregion

    #region PARSING & SANITIZE
    string SanitizeForDisplay(string s){
        if(string.IsNullOrEmpty(s)) return s ?? "";
        string outStr = s.Replace("```", "");
        outStr = Regex.Replace(outStr, @"[\uD800-\uDBFF][\uDC00-\uDFFF]", "");
        outStr = Regex.Replace(outStr, @"[\u2600-\u26FF\u2700-\u27BF]", "");
        outStr = outStr.Replace(":)", "").Replace(":D", "").Replace(";)", "").Replace(":(", "");
        outStr = Regex.Replace(outStr, @"\s+", " ").Trim();
        return outStr;
    }

    private OpponentTurn LooseParseOpponentTurn(string raw){
        if(string.IsNullOrEmpty(raw)) return null;
        var ot = new OpponentTurn();
        var mArg = Regex.Match(raw, @"\""argument\""\s*:\s*\""([\s\S]*?)\""", RegexOptions.IgnoreCase);
        var mFall = Regex.Match(raw, @"\""fallacy_type\""\s*:\s*\""([^\""]*)\""", RegexOptions.IgnoreCase);
        ot.argument = mArg.Success ? mArg.Groups[1].Value : raw;
        ot.fallacy_type = mFall.Success ? mFall.Groups[1].Value : "unknown";
        return ot;
    }

    private JudgeScore LooseParseJudgeScore(string raw){
        if(string.IsNullOrEmpty(raw)) return null;
        var js = new JudgeScore(); 
        int v;
        
        var mF = Regex.Match(raw, @"\""fallacy_score\""\s*:\s*(\d+)", RegexOptions.IgnoreCase);
        var mL = Regex.Match(raw, @"\""logic_score\""\s*:\s*(\d+)", RegexOptions.IgnoreCase);
        var mI = Regex.Match(raw, @"\""insult_score\""\s*:\s*(\d+)", RegexOptions.IgnoreCase);
        var mT = Regex.Match(raw, @"\""total_score\""\s*:\s*(\d+)", RegexOptions.IgnoreCase);
        var mFeedback = Regex.Match(raw, @"\""feedback\""\s*:\s*\""([\s\S]*?)\""", RegexOptions.IgnoreCase);
        
        js.fallacy_score = (mF.Success && int.TryParse(mF.Groups[1].Value, out v)) ? v : 0;
        js.logic_score = (mL.Success && int.TryParse(mL.Groups[1].Value, out v)) ? v : 0;
        js.insult_score = (mI.Success && int.TryParse(mI.Groups[1].Value, out v)) ? v : 0;
        js.total_score = (mT.Success && int.TryParse(mT.Groups[1].Value, out v)) ? v : (js.fallacy_score + js.logic_score + js.insult_score);
        js.feedback = mFeedback.Success ? mFeedback.Groups[1].Value.Trim() : "No feedback provided.";
        
        return js;
    }

    private DebateTopic LooseParseDebateTopic(string raw){
        if(string.IsNullOrEmpty(raw)) return null;
        var dt = new DebateTopic();
        var mH = Regex.Match(raw, @"\""header\""\s*:\s*\""([\s\S]*?)\""", RegexOptions.IgnoreCase);
        var mO = Regex.Match(raw, @"\""opening\""\s*:\s*\""([\s\S]*?)\""", RegexOptions.IgnoreCase);
        dt.header = mH.Success ? mH.Groups[1].Value : "(Untitled Topic)";
        dt.opening = mO.Success ? mO.Groups[1].Value : "(No opening provided)";
        return dt;
    }

    string EscapeForPrompt(string s){ return string.IsNullOrEmpty(s) ? "" : s.Replace("\"", "'"); }
    #endregion

    #region FORMAT
    string BuildNaturalOpponentSpeech(OpponentTurn t){
        if(t == null) return "[Error: null opponent]";
        return t.argument.Trim();
    }

    string Capitalize(string s) { if(string.IsNullOrEmpty(s)) return s; s = s.Trim(); return char.ToUpper(s[0]) + s.Substring(1); }

    string MakeFeedbackEducational(string feedback, JudgeScore score){
        if(string.IsNullOrEmpty(feedback) || feedback.Contains("No feedback")){
            StringBuilder sb = new StringBuilder();
            sb.Append($"Debate Analysis - Score: {score.total_score}/30. ");
            
            if(score.fallacy_score <= 3){
                sb.Append($"You didn't address the opponent's logical fallacy ({currentOpponentTurn?.fallacy_type ?? "unknown fallacy"}). ");
                sb.Append("Tip: Identify the fallacy type and explain why it's flawed. ");
            }else if(score.fallacy_score <= 6){
                sb.Append("You partially engaged with the fallacy. ");
                sb.Append("Tip: Be more specific about how the fallacy breaks logic. ");
            }else sb.Append("Good job addressing the logical flaw. ");
            
            if(score.logic_score <= 3){
                sb.Append("Your argument lacked logical structure. ");
                sb.Append("Tip: Use clear reasoning: Premise → Evidence → Conclusion. ");
            }else if(score.logic_score <= 6){
                sb.Append("Some logical coherence but could be stronger. ");
                sb.Append("Tip: Connect your points more clearly to the topic. ");
            }else sb.Append("Strong logical reasoning. ");
            
            if(score.insult_score <= 3){
                sb.Append("Too much personal attack, not enough argument. ");
                sb.Append("Tip: Critique the idea, not the person. ");
            }else if(score.insult_score <= 6){
                sb.Append("Some constructive criticism mixed with personal remarks. ");
                sb.Append("Tip: Focus on dismantling the argument, not the arguer. ");
            }else sb.Append("Effective criticism without ad hominem. ");
            
            sb.Append("For your next turn: ");
            if(score.total_score < 10){
                sb.Append("Identify the fallacy, explain why it's flawed, and provide a counter-example.");
            }else if(score.total_score < 20) sb.Append("Build on your current approach, anticipate their next fallacy, and strengthen your evidence.");
            else sb.Append("Maintain your logical consistency, watch for new fallacies, and consider conceding minor points.");
            
            return sb.ToString();
        }
        
        return feedback;
    }
    #endregion

    #region MISC
    IEnumerator SendToOllama(string prompt, System.Action<string> onComplete){
        yield return network.SendToOllama(ollamaURL, model, requestTimeoutSeconds, prompt, onComplete);
    }

    void UseAutoReply(){
        if(!isDebateActive || currentState != DebateState.Idle) return;
        
        string reply;
        if(isFirstAutoTurn){
            reply = autoReplies[0];
            currentAutoReplyIndex = 1;
            isFirstAutoTurn = false;
        }else{
            reply = autoReplies[currentAutoReplyIndex % autoReplies.Length];
            currentAutoReplyIndex++;
        }
        
        replyTextField.Text = "";
        OnSendReply_Internal(reply);
    }

    IEnumerator StartAutoDebate(){
        yield return null;
        if(!autoComplete) yield break;
        
        if(topicTextField != null){
            topicTextField.Text = autoTopic;
            var parent = topicTextField.Parent;
            var sibling = parent?.GetChild(0) as TextBlock;
            if(sibling != null) sibling.Text = "";

            yield return new WaitForSeconds(.5f);
            SendTopic();
        }
    }

    IEnumerator ScheduleNextAutoReply(){
        while(currentState != DebateState.Idle) yield return null;
        yield return new WaitForSeconds(1f);
        if(isDebateActive && currentState == DebateState.Idle) UseAutoReply();
    }

    public void ResetDebate(){
        ResetConversationState();

        var toDestroy = new List<GameObject>();
        for(int i = 2; i < chatContainer.childCount; i++) toDestroy.Add(chatContainer.GetChild(i).gameObject);
        foreach(var go in toDestroy) Destroy(go);

        topicTextField.Text = "";
        replyTextField.Text = "";
        isDebateActive = false;
        StartCoroutine(ScrollToBottom());
    } 
    #endregion
}