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
using UnityEngine.SceneManagement;

public class OnlineDebateManager : MonoBehaviour
{
    [Header("UI REFERENCES")]
    public TextBlock topicTextField;
    public TextBlock titleHeader;
    public TextBlock titleOpening;
    public TextBlock likeCount;
    public TextBlock commentCount;
    public TextBlock replyTextField;
    public TextBlock playerScoring;
    public TextBlock completedRoundCounterText;
    public TextBlock performanceText;
    public GameObject playerCommentPrefab;
    public GameObject enemyCommentPrefab;
    public GameObject systemCommentPrefab;
    public Transform chatContainer;
    public Scroller scroller;

    [Header("REFERENCES")]
    public EngagementLogic engagementLogic;
    public DictionaryManager dictionaryManager;
    public DebateNetwork network;

    [Header("AUTO COMPLETE")]
    public bool autoComplete = false;
    public string autoTopic = "hotdog";
    public string[] autoReplies;
    private bool isFirstAutoTurn = true;
    private int currentAutoReplyIndex = 0;

    [Header("LOCAL CONFIG OVERRIDES")]
    public string localOllamaURL = "";
    public string localModel = "";
    public int localMaxTurns = -1;
    public int localMaxPoorResponses = -1;

    private IResponseQualityEvaluator qualityEvaluator;
    private IDebateLogger debateLogger;
    private string lastDisplayedPerformance = "";
    private const int MAX_MESSAGES = 50;

    private enum Role { Player, Enemy, System }
    private const string DEFAULT_HEADER_TEXT = "Welcome to our AI demo.";
    private const string DEFAULT_OPENING_TEXT = 
        "1. Type a > 2 letter single-word topic (e.g., \"privacy\", \"communist\", \"hotdog\") in the top bar and press Enter. The AI will open the debate with a fallacious argument.\n" +
        "2. Reply in the bottom box, stay logical, smart insults are encouraged, and rebute the fallacy.\n" +
        "3. After each reply, an AI judge scores you.\n" +
        "The AI opponent adapts: smart replies earn serious debate, weak ones trigger mockery or early termination after 3 consecutive poor responses.\n" +
        "4. The debate lasts up to 10 turns, but can end sooner.";

    void Awake(){
        if(qualityEvaluator == null) qualityEvaluator = new ResponseQualityEvaluator();
        if(debateLogger == null) debateLogger = new FileDebateLogger(
            generateLog: false, 
            logDirectory: ""
        );
            
        ApplyLocalOverrides();
    }

    void Start(){
        if(DebateDataManager.Instance != null){
            debateLogger = new FileDebateLogger(
                generateLog: DebateDataManager.Instance.generateLog, 
                logDirectory: ""
            );
        }

        if(titleHeader != null && string.IsNullOrEmpty(titleHeader.Text)) titleHeader.Text = DEFAULT_HEADER_TEXT;
        if(titleOpening != null && string.IsNullOrEmpty(titleOpening.Text)) titleOpening.Text = DEFAULT_OPENING_TEXT;

        LoadDataIntoUI();
        SceneManager.sceneLoaded += OnSceneLoaded;
        if(autoComplete) StartCoroutine(StartAutoDebate());
    }

    void OnDestroy(){
        SaveUIToData();
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void Update(){
        if(!Input.GetKeyDown(KeyCode.Return) && !Input.GetKeyDown(KeyCode.KeypadEnter)) return;
        if(!DebateDataManager.Instance.isDebateActive) SendTopic();
        else SendReply();
    }

    #region SCENE MANAGEMENT
    void OnSceneLoaded(Scene scene, LoadSceneMode mode){
        StartCoroutine(RefreshUIReferences());
    }

    IEnumerator RefreshUIReferences(){
        yield return new WaitForEndOfFrame();
        LoadDataIntoUI();
    }
    #endregion

    void ApplyLocalOverrides(){
        if(DebateDataManager.Instance == null) return;
        if(!string.IsNullOrEmpty(localOllamaURL)) DebateDataManager.Instance.ollamaURL = localOllamaURL;
        if(!string.IsNullOrEmpty(localModel)) DebateDataManager.Instance.model = localModel;
        if(localMaxTurns > 0) DebateDataManager.Instance.maxTurns = localMaxTurns;
        if(localMaxPoorResponses > 0) DebateDataManager.Instance.maxPoorResponses = localMaxPoorResponses;
    }

    #region UI DATA MANAGEMENT
    void LoadDataIntoUI(){
        if(DebateDataManager.Instance == null) return;
        
        //Topic data
        if(titleHeader != null){
            if(DebateDataManager.Instance.currentTopic != null && 
               !string.IsNullOrEmpty(DebateDataManager.Instance.currentTopic.header) &&
               DebateDataManager.Instance.currentTopic.header != DEFAULT_HEADER_TEXT){
                titleHeader.Text = DebateDataManager.Instance.currentTopic.header;
            }else if(string.IsNullOrEmpty(titleHeader.Text)){
                titleHeader.Text = DEFAULT_HEADER_TEXT;
            }
        }
            
        if(titleOpening != null){
            if(DebateDataManager.Instance.currentTopic != null && 
               !string.IsNullOrEmpty(DebateDataManager.Instance.currentTopic.opening) &&
               DebateDataManager.Instance.currentTopic.opening != DEFAULT_OPENING_TEXT){
                titleOpening.Text = DebateDataManager.Instance.currentTopic.opening;
            }else if(string.IsNullOrEmpty(titleOpening.Text)){
                titleOpening.Text = DEFAULT_OPENING_TEXT;
            }
        }

        //Conversation history
        if(chatContainer != null){
            int childrenToKeep = 2;
            for(int i = chatContainer.childCount - 1; i >= childrenToKeep; i--){
                Destroy(chatContainer.GetChild(i).gameObject);
            }
            
            foreach(var entry in DebateDataManager.Instance.conversationHistory){
                GameObject prefab = GetPrefabForSpeaker(entry.speaker);
                if(prefab != null){
                    InstantiateCommentPrefab(prefab, entry.message, 
                        GetRoleForSpeaker(entry.speaker), 
                        entry.fallacyUsed, 
                        entry.score);
                }
            }
        }
        
        UpdatePerformanceUI();
        if(completedRoundCounterText != null)
            completedRoundCounterText.Text = $"Turn: {DebateDataManager.Instance.completedRoundCount}/{DebateDataManager.Instance.maxTurns}";
        if(likeCount != null && commentCount != null) GenerateSocialStats();
    }

    void SaveUIToData(){
        if(DebateDataManager.Instance == null) return;
        if(topicTextField != null && titleHeader != null && titleOpening != null){
            DebateDataManager.Instance.currentTopic = new DebateTopic{ 
                header = titleHeader.Text, 
                opening = titleOpening.Text 
            };
        }
    }
    #endregion

    #region UI HELPERS
    GameObject GetPrefabForSpeaker(string speaker){
        if(speaker == "Player" || speaker == "You") return playerCommentPrefab;
        else if(speaker == DebateDataManager.Instance.currentOpponentName) return enemyCommentPrefab;
        else if(speaker == "System" || speaker.Contains("Judge")) return systemCommentPrefab;
        return null;
    }

    Role GetRoleForSpeaker(string speaker){
        if(speaker == "Player" || speaker == "You") return Role.Player;
        else if(speaker == DebateDataManager.Instance.currentOpponentName) return Role.Enemy;
        else return Role.System;
    }
    #endregion

    #region USER INPUT
    public void SendTopic(){
        string topic = topicTextField.Text.Trim();
        if(topic.Length <= 2) return;
        HandleTopicSubmission(topic);
    }

    public void SendReply(){
        if(autoComplete && DebateDataManager.Instance.isDebateActive){
            UseAutoReply();
            return;
        }

        string reply = replyTextField.Text.Trim();
        if(reply.Length == 0) return;
        replyTextField.Text = "";
        OnSendReply_Internal(reply);
    }

    void HandleTopicSubmission(string topic){
        if(DebateDataManager.Instance.currentState != DebateDataManager.DebateState.Idle) return;

        if(!autoComplete && dictionaryManager != null){
            bool isValidWord = true;
            string[] words = topic.Split(' ', System.StringSplitOptions.RemoveEmptyEntries);
            
            if(words.Length == 1){
                if(!dictionaryManager.IsEnglishWord(topic)){
                    string suggestion = dictionaryManager.SuggestClosestWord(topic);
                    if(suggestion != topic && suggestion.Length > 0){
                        topicTextField.Text = suggestion;
                        topic = suggestion;
                        var textField = topicTextField.gameObject.GetComponent<TextField>();
                        if(textField != null) textField.MoveCursor(textField.CursorPosition.MoveToEnd(), false);
                    }else{
                        topicTextField.Text = "Not a valid word";
                        topicTextField.Color = new Color32(255, 100, 100, 255);
                        isValidWord = false;

                        var textField = topicTextField.gameObject.GetComponent<TextField>();
                        if(textField != null) textField.MoveCursor(textField.CursorPosition.MoveToEnd(), false);
                        StartCoroutine(ClearTextFieldAfterDelay(1f));
                    }
                }
            }
            if(!isValidWord) return;
        }

        var visual = GetComponent<OnlineForumVisual>();
        if(visual != null) visual.StartLoadingAnimation();

        if(engagementLogic != null) engagementLogic.Init(topic);
        DebateDataManager.Instance.ResetConversationState();
        topicTextField.Color = new Color32(255, 255, 255, 255);
        StartCoroutine(RequestDebateTopic(topic));
    }

    IEnumerator ClearTextFieldAfterDelay(float delay){
        yield return new WaitForSeconds(delay);
        topicTextField.Text = "";
        topicTextField.Color = new Color32(255, 255, 255, 255);
    }
    #endregion

    #region TOPIC GENERATION
    IEnumerator RequestDebateTopic(string topic){
        DebateDataManager.Instance.currentState = DebateDataManager.DebateState.WaitingForTopic;
        string chosenFallacy = DebateDataManager.Instance.suitableForOpening[Random.Range(0, DebateDataManager.Instance.suitableForOpening.Count)];
        
        if(engagementLogic != null) engagementLogic.RecordFallacy(chosenFallacy);

        string prompt = string.Format(PromptTemplates.TopicPrompt, 
            EscapeForPrompt(topic), 
            chosenFallacy);

        debateLogger.LogPrompt("TOPIC_GENERATION", prompt, ResponseTier.Normal, 0, null, 
            DebateDataManager.Instance.consecutivePoorResponses);

        yield return SendToOllama(prompt, result => {
            string clean = DebateJsonParser.ExtractJson(result);
            if(string.IsNullOrEmpty(clean)){ 
                StartCoroutine(RetryDebateTopic(prompt, topic, chosenFallacy));
                return;
            }

            DebateTopic topicObj;
            if(!DebateJsonParser.TryParseJson<DebateTopic>(clean, out topicObj))
                topicObj = LooseParseDebateTopic(clean);
            
            if(topicObj == null){ 
                StartCoroutine(RetryDebateTopic(prompt, topic, chosenFallacy));
                return;
            }

            DebateDataManager.Instance.currentTopic = topicObj;
            if(titleHeader != null) titleHeader.Text = topicObj.header;
            if(titleOpening != null) titleOpening.Text = topicObj.opening;
            if(engagementLogic != null) engagementLogic.PopNotification(EngagementLogic.NotificationType.Topic);
            
            DebateDataManager.Instance.isDebateActive = true;
            DebateDataManager.Instance.currentOpponentTurn = new OpponentTurn{ 
                argument = topicObj.opening, 
                fallacy_type = chosenFallacy
            };

            DebateDataManager.Instance.AddToHistory(DebateDataManager.Instance.currentOpponentName, 
                topicObj.opening, chosenFallacy);
            GenerateSocialStats();
            
            if(DebateDataManager.Instance.generateLog)
                debateLogger.LogTopic(topicObj.header, topicObj.opening, 
                    DebateDataManager.Instance.currentOpponentName);
            
            DebateDataManager.Instance.currentState = DebateDataManager.DebateState.Idle;
        });
    }

    IEnumerator RetryDebateTopic(string prompt, string topic, string chosenFallacy){
        yield return SendToOllama(prompt, result => {
            string clean = DebateJsonParser.ExtractJson(result);
            if(string.IsNullOrEmpty(clean)){ 
                InstantiateCommentPrefab(systemCommentPrefab, "[Error] Could not parse debate topic.", Role.System); 
                DebateDataManager.Instance.currentState = DebateDataManager.DebateState.Idle;
                return; 
            }

            DebateTopic topicObj;
            if(!DebateJsonParser.TryParseJson<DebateTopic>(clean, out topicObj)) 
                topicObj = LooseParseDebateTopic(clean);
            
            if(topicObj == null){ 
                InstantiateCommentPrefab(systemCommentPrefab, "[Error] Could not parse debate topic.", Role.System); 
                DebateDataManager.Instance.currentState = DebateDataManager.DebateState.Idle;
                return; 
            }

            DebateDataManager.Instance.currentTopic = topicObj;
            if(titleHeader != null) titleHeader.Text = topicObj.header;
            if(titleOpening != null) titleOpening.Text = topicObj.opening;

            DebateDataManager.Instance.currentOpponentTurn = new OpponentTurn{ 
                argument = topicObj.opening, 
                fallacy_type = chosenFallacy
            };

            DebateDataManager.Instance.AddToHistory(DebateDataManager.Instance.currentOpponentName, topicObj.opening, chosenFallacy);
            GenerateSocialStats();
            
            if(DebateDataManager.Instance.generateLog)
                debateLogger.LogTopic(topicObj.header, topicObj.opening, DebateDataManager.Instance.currentOpponentName);
            
            DebateDataManager.Instance.currentState = DebateDataManager.DebateState.Idle;
        });
    }
    #endregion

    #region PLAYER REPLY
    void OnSendReply_Internal(string playerText){
        if(!CanProceedToNextTurn()) return;
        if(DebateDataManager.Instance.currentState != DebateDataManager.DebateState.Idle) return;
        if(DebateDataManager.Instance.currentTopic == null) return;

        DebateDataManager.Instance.lastInputTime = Time.time;
        DebateDataManager.Instance.lastPlayerReply = playerText;
        InstantiateCommentPrefab(playerCommentPrefab, playerText, Role.Player);
        DebateDataManager.Instance.currentState = DebateDataManager.DebateState.WaitingForJudge;
        
        StartCoroutine(RequestJudgeScore(DebateDataManager.Instance.currentOpponentTurn, playerText));
        if(autoComplete) StartCoroutine(ScheduleNextAutoReply());
    }

    bool CanProceedToNextTurn(){
        if(DebateDataManager.Instance.completedRoundCount >= DebateDataManager.Instance.maxTurns){
            EndDebate("Maximum turns reached. Debate concluded.");
            if(engagementLogic != null)
                engagementLogic.PopNotification(EngagementLogic.NotificationType.End, 
                    EngagementLogic.EndReason.MaxTurn);
            return false;
        }
        if(DebateDataManager.Instance.currentState == DebateDataManager.DebateState.DebateEnded) return false;
        if(Time.time - DebateDataManager.Instance.lastInputTime < DebateDataManager.Instance.minInputInterval) return false;
        return true;
    }
    #endregion

    #region JUDGE SCORING
    IEnumerator RequestJudgeScore(OpponentTurn opponent, string playerReply){
        string prompt = string.Format(PromptTemplates.JudgePrompt,
            EscapeForPrompt(DebateDataManager.Instance.currentTopic.header),
            EscapeForPrompt(DebateDataManager.Instance.currentTopic.opening),
            EscapeForPrompt(opponent.fallacy_type),
            EscapeForPrompt(opponent.argument),
            EscapeForPrompt(playerReply),
            EscapeForPrompt(DebateDataManager.Instance.BuildHistoryContext()));

        int playerQuality = qualityEvaluator.CalculateQuality(playerReply);
        ResponseTier playerTier = qualityEvaluator.DetermineTier(playerQuality, null);
        debateLogger.LogPrompt("JUDGE_SCORING", prompt, playerTier, playerQuality, null, 
            DebateDataManager.Instance.consecutivePoorResponses);

        yield return SendToOllama(prompt, result =>{
            string sanitized = DebateJsonParser.SanitizeModelOutput(result);
            string json = DebateJsonParser.ExtractJson(sanitized);
                
            if(string.IsNullOrEmpty(json)){
                StartCoroutine(RetryJudgeScore(prompt, opponent, playerReply));
                return;
            }

            JudgeScore judgeScore;
            if(!DebateJsonParser.TryParseJson<JudgeScore>(json, out judgeScore)) 
                judgeScore = LooseParseJudgeScore(json);
            
            if(judgeScore == null){ 
                StartCoroutine(RetryJudgeScore(prompt, opponent, playerReply));
                return;
            }

            DebateDataManager.Instance.lastJudgeScore = judgeScore;
            DebateDataManager.Instance.currentJudgeFeedback = judgeScore.feedback ?? "No feedback available.";
            
            string displayFeedback = MakeFeedbackEducational(DebateDataManager.Instance.currentJudgeFeedback, judgeScore);
            DebateDataManager.Instance.UpdatePerformanceMetrics(judgeScore, playerReply, qualityEvaluator);

            if(playerScoring != null) 
                playerScoring.Text = $"F: {judgeScore.fallacy_score}  L: {judgeScore.logic_score}  I: {judgeScore.insult_score}  [{judgeScore.total_score}]";
            
            InstantiateCommentPrefab(systemCommentPrefab, displayFeedback, Role.System);
            DebateDataManager.Instance.AddToHistory("Player", playerReply, "", judgeScore.total_score);

            if(DebateDataManager.Instance.generateLog) 
                debateLogger.LogPlayerTurn(DebateDataManager.Instance.completedRoundCount, playerReply, judgeScore, 
                    DebateDataManager.Instance.currentJudgeFeedback, DebateDataManager.Instance.consecutivePoorResponses);

            DebateDataManager.Instance.currentState = DebateDataManager.DebateState.WaitingForOpponent;
            StartCoroutine(RequestOpponentReply(judgeScore, DebateDataManager.Instance.lastPlayerReply));
        });
    }

    IEnumerator RetryJudgeScore(string prompt, OpponentTurn opponent, string playerReply){
        yield return SendToOllama(prompt, result =>{
            string sanitized = DebateJsonParser.SanitizeModelOutput(result);
            string json = DebateJsonParser.ExtractJson(sanitized);
            
            if(string.IsNullOrEmpty(json)){
                InstantiateCommentPrefab(systemCommentPrefab, "[Error] Could not parse judge response.", Role.System);
                DebateDataManager.Instance.currentState = DebateDataManager.DebateState.Idle;
                return;
            }

            JudgeScore judgeScore;
            if(!DebateJsonParser.TryParseJson<JudgeScore>(json, out judgeScore)) 
                judgeScore = LooseParseJudgeScore(json);
            
            if(judgeScore == null){
                InstantiateCommentPrefab(systemCommentPrefab, "[Error] Judge response parse failed.", Role.System); 
                DebateDataManager.Instance.currentState = DebateDataManager.DebateState.Idle;
                return; 
            }

            DebateDataManager.Instance.lastJudgeScore = judgeScore;
            DebateDataManager.Instance.currentJudgeFeedback = judgeScore.feedback ?? "No feedback available.";
            string displayFeedback = MakeFeedbackEducational(DebateDataManager.Instance.currentJudgeFeedback, judgeScore);
            DebateDataManager.Instance.UpdatePerformanceMetrics(judgeScore, playerReply, qualityEvaluator);

            if(playerScoring != null) 
                playerScoring.Text = $"F: {judgeScore.fallacy_score}  L: {judgeScore.logic_score}  I: {judgeScore.insult_score}  [{judgeScore.total_score}]";
            
            InstantiateCommentPrefab(systemCommentPrefab, displayFeedback, Role.System);
            DebateDataManager.Instance.AddToHistory("Player", playerReply, "", judgeScore.total_score);

            if(DebateDataManager.Instance.generateLog) 
                debateLogger.LogPlayerTurn(DebateDataManager.Instance.completedRoundCount, playerReply, judgeScore, 
                    DebateDataManager.Instance.currentJudgeFeedback, DebateDataManager.Instance.consecutivePoorResponses);

            DebateDataManager.Instance.currentState = DebateDataManager.DebateState.WaitingForOpponent;
            StartCoroutine(RequestOpponentReply(judgeScore, DebateDataManager.Instance.lastPlayerReply));
        });
    }
    #endregion

    #region OPPONENT RESPONSE
    IEnumerator RequestOpponentReply(JudgeScore score, string playerReply){
        int responseQuality = qualityEvaluator.CalculateQuality(playerReply);
        ResponseTier responseTier = qualityEvaluator.DetermineTier(responseQuality, DebateDataManager.Instance.lastJudgeScore);
        
        bool isResponseAppropriateForTier = 
            (responseTier == ResponseTier.Gibberish && responseQuality <= 2) ||
            (responseTier == ResponseTier.Warning && responseQuality <= 5) ||
            (responseTier == ResponseTier.Normal && responseQuality > 5);
            
        if(isResponseAppropriateForTier) DebateDataManager.Instance.aiResponseAppropriateness++;
        if(responseTier != ResponseTier.Normal) DebateDataManager.Instance.consecutivePoorResponses++;
        else DebateDataManager.Instance.consecutivePoorResponses = 0;

        if(DebateDataManager.Instance.consecutivePoorResponses >= DebateDataManager.Instance.maxPoorResponses){
            DebateDataManager.Instance.currentOpponentTurn = GenerateFinalDisengagement(
                DebateDataManager.Instance.consecutivePoorResponses, playerReply);
            
            string finalResponse = BuildNaturalOpponentSpeech(DebateDataManager.Instance.currentOpponentTurn);
            InstantiateCommentPrefab(enemyCommentPrefab, finalResponse, Role.Enemy);
            DebateDataManager.Instance.AddToHistory(DebateDataManager.Instance.currentOpponentName, 
                DebateDataManager.Instance.currentOpponentTurn.argument, "none");
            
            if(DebateDataManager.Instance.generateLog) 
                debateLogger.LogOpponentTurn(DebateDataManager.Instance.completedRoundCount, 
                    DebateDataManager.Instance.currentOpponentName, 
                    DebateDataManager.Instance.currentOpponentTurn, responseTier);
            
            EndDebate($"{DebateDataManager.Instance.currentOpponentName} ended debate after {DebateDataManager.Instance.consecutivePoorResponses} poor responses.");
            if(engagementLogic != null)
                engagementLogic.PopNotification(EngagementLogic.NotificationType.End, 
                    EngagementLogic.EndReason.PoorResponse);
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
                    EscapeForPrompt(DebateDataManager.Instance.currentOpponentTurn.argument),
                    DebateDataManager.Instance.BuildHistoryContext(),
                    DebateDataManager.Instance.consecutivePoorResponses);
                promptType = "GIBBERISH_RESPONSE";
                break;

            case ResponseTier.Warning:
                chosenFallacy = DebateDataManager.Instance.fallacyPool["attack"][Random.Range(0, 
                    DebateDataManager.Instance.fallacyPool["attack"].Length)];
                prompt = string.Format(PromptTemplates.OpponentWarningPrompt,
                    EscapeForPrompt(playerReply),
                    DebateDataManager.Instance.BuildHistoryContext(),
                    DebateDataManager.Instance.consecutivePoorResponses,
                    DebateDataManager.Instance.maxPoorResponses,
                    chosenFallacy);
                promptType = "WARNING_RESPONSE";
                break;

            default:
                chosenTrait = ChooseTraitByScore(score);
                normalFallacy = DebateDataManager.Instance.fallacyPool[chosenTrait][Random.Range(0, 
                    DebateDataManager.Instance.fallacyPool[chosenTrait].Length)];
                prompt = string.Format(PromptTemplates.OpponentNormalPrompt,
                    EscapeForPrompt(playerReply),
                    DebateDataManager.Instance.BuildHistoryContext(),
                    GetEmotionalState(responseTier),
                    normalFallacy,
                    GetAdaptiveToneRule(chosenTrait, score));
                promptType = "NORMAL_RESPONSE";
                break;
        }

        debateLogger.LogPrompt(promptType, prompt, responseTier, responseQuality, score, 
            DebateDataManager.Instance.consecutivePoorResponses);

        //for retry
        ResponseTier localResponseTier = responseTier;
        JudgeScore localScore = score;
        string localPlayerReply = playerReply;
        int localResponseQuality = responseQuality;
        int localConsecutivePoorResponses = DebateDataManager.Instance.consecutivePoorResponses;
        string localChosenTrait = chosenTrait;
        string localFallacy = responseTier == ResponseTier.Warning ? chosenFallacy : normalFallacy;

        yield return SendToOllama(prompt, result =>{
            string clean = DebateJsonParser.ExtractJson(result);
            if(string.IsNullOrEmpty(clean)){ 
                StartCoroutine(RetryOpponentReply(prompt, localResponseTier, localScore, localPlayerReply, 
                    localResponseQuality, localConsecutivePoorResponses, localChosenTrait, localFallacy));
                return;
            }

            OpponentTurn opponentTurn;
            if(!DebateJsonParser.TryParseJson<OpponentTurn>(clean, out opponentTurn)) 
                opponentTurn = LooseParseOpponentTurn(clean);
            
            if(opponentTurn == null){ 
                StartCoroutine(RetryOpponentReply(prompt, localResponseTier, localScore, localPlayerReply, 
                    localResponseQuality, localConsecutivePoorResponses, localChosenTrait, localFallacy));
                return;
            }

            DebateDataManager.Instance.currentOpponentTurn = opponentTurn;
            string spoken = BuildNaturalOpponentSpeech(opponentTurn);
            InstantiateCommentPrefab(enemyCommentPrefab, spoken, Role.Enemy);

            DebateDataManager.Instance.AddToHistory(DebateDataManager.Instance.currentOpponentName, 
                opponentTurn.argument, opponentTurn.fallacy_type);

            if(DebateDataManager.Instance.generateLog) 
                debateLogger.LogOpponentTurn(DebateDataManager.Instance.completedRoundCount, 
                    DebateDataManager.Instance.currentOpponentName, opponentTurn, responseTier);
            
            DebateDataManager.Instance.completedRoundCount++;
            DebateDataManager.Instance.currentState = DebateDataManager.DebateState.Idle;
            if(DebateDataManager.Instance.completedRoundCount >= DebateDataManager.Instance.maxTurns) 
                EndDebate("Maximum turns reached. Debate concluded.");
        });
    }

    IEnumerator RetryOpponentReply(string prompt, ResponseTier responseTier, JudgeScore score, 
        string playerReply, int responseQuality, int consecutivePoorResponses, string chosenTrait, string fallacy){
        
        yield return SendToOllama(prompt, result =>{
            string clean = DebateJsonParser.ExtractJson(result);
            if(string.IsNullOrEmpty(clean)){ 
                InstantiateCommentPrefab(systemCommentPrefab, "[Error] Could not parse opponent response.", Role.System); 
                DebateDataManager.Instance.currentState = DebateDataManager.DebateState.Idle;
                return; 
            }

            OpponentTurn opponentTurn;
            if(!DebateJsonParser.TryParseJson<OpponentTurn>(clean, out opponentTurn)) 
                opponentTurn = LooseParseOpponentTurn(clean);
            
            if(opponentTurn == null){ 
                InstantiateCommentPrefab(systemCommentPrefab, "[Error] Opponent response parse failed.", Role.System); 
                DebateDataManager.Instance.currentState = DebateDataManager.DebateState.Idle;
                return; 
            }

            DebateDataManager.Instance.currentOpponentTurn = opponentTurn;
            string spoken = BuildNaturalOpponentSpeech(opponentTurn);
            InstantiateCommentPrefab(enemyCommentPrefab, spoken, Role.Enemy);

            DebateDataManager.Instance.AddToHistory(DebateDataManager.Instance.currentOpponentName, 
                opponentTurn.argument, opponentTurn.fallacy_type);

            if(DebateDataManager.Instance.generateLog) 
                debateLogger.LogOpponentTurn(DebateDataManager.Instance.completedRoundCount, 
                    DebateDataManager.Instance.currentOpponentName, opponentTurn, responseTier);
            
            DebateDataManager.Instance.completedRoundCount++;
            DebateDataManager.Instance.currentState = DebateDataManager.DebateState.Idle;
            if(DebateDataManager.Instance.completedRoundCount >= DebateDataManager.Instance.maxTurns) 
                EndDebate("Maximum turns reached. Debate concluded.");
        });
    }
    #endregion

    #region DEBATE ENDING
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
        return new OpponentTurn{ argument = response, fallacy_type = "none" };
    }

    void EndDebate(string reason){
        DebateDataManager.Instance.currentState = DebateDataManager.DebateState.DebateEnded;
        StopAllCoroutines();
        
        if(DebateDataManager.Instance != null){
            DebateDataManager.Instance.cachedPerformanceSummary = DebateDataManager.Instance.GetPerformanceSummary();
            DebateDataManager.Instance.cachedEndReason = reason;
            DebateDataManager.Instance.UpdateWinLoseCount();
        }

        if(DebateDataManager.Instance.generateLog)
            debateLogger.EndDebate(reason, DebateDataManager.Instance.cachedPerformanceSummary);
    }
    #endregion

    #region AI BEHAVIOR LOGIC
    string ChooseTraitByScore(JudgeScore score){
        if(score == null) return "neutral";
        if(DebateDataManager.Instance.consecutivePoorResponses >= 2) return "attack";

        Dictionary<string, float> traitWeights = new Dictionary<string, float>{
            { "attack", Mathf.Clamp(score.insult_score / 10f, 0.1f, 0.8f) },
            { "logic_bend", Mathf.Clamp(score.logic_score / 12f, 0.1f, 0.7f) },
            { "diversion", Mathf.Clamp((30 - score.total_score) / 30f, 0.1f, 0.6f) },
            { "emotional", Mathf.Clamp((15 - score.total_score) / 15f, 0.1f, 0.7f) },
            { "neutral", 0.3f }
        };

        float turnFactor = DebateDataManager.Instance.completedRoundCount / (float)DebateDataManager.Instance.maxTurns;
        if(turnFactor > 0.7f){
            traitWeights["attack"] *= 1.5f;
            traitWeights["emotional"] *= 1.3f;
        }

        if(DebateDataManager.Instance.consecutivePoorResponses >= 2){
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

        if(DebateDataManager.Instance.consecutivePoorResponses >= 3) baseTone += " — completely disgusted by their intellectual incapacity and ending this farce";
        else if(DebateDataManager.Instance.consecutivePoorResponses == 2) baseTone += " — intellectually disgusted and giving one final contemptuous warning";
        else if(DebateDataManager.Instance.consecutivePoorResponses == 1) baseTone += " — annoyed by their intellectual inferiority and losing patience";

        return baseTone;
    }

    string GetEmotionalState(ResponseTier tier){
        if(tier == ResponseTier.Gibberish) 
            return "GENUINELY CONFUSED - Concerned about their mental state, questioning if they're having a stroke";
        
        return DebateDataManager.Instance.consecutivePoorResponses switch{
            >= 3 => "INTELLECTUALLY DISGUSTED - Genuinely contemptuous of their mental incapacity, ready to end this farce",
            2 => "CONTEMPTUOUS - Mocking their intellectual inferiority, one chance left",
            1 => "ANNOYED - Frustrated by their poor reasoning skills but willing to continue",
            _ => "PASSIONATELY ENGAGED - Emotionally invested in the debate, ready for intellectual combat"
        };
    }
    #endregion

    #region PERFORMANCE UI
    void UpdatePerformanceUI(){
        if(performanceText == null || DebateDataManager.Instance == null) return;

        string performance = $"Avg: {DebateDataManager.Instance.averageScore:F1} | " +
                           $"Fallacies: {DebateDataManager.Instance.totalFallaciesIdentified} | " +
                           $"Quality: {DebateDataManager.Instance.highQualityTurns}/{DebateDataManager.Instance.completedRoundCount}";
                           
        if(DebateDataManager.Instance.consecutivePoorResponses > 0)
            performance += $" | Warnings: {DebateDataManager.Instance.consecutivePoorResponses}/{DebateDataManager.Instance.maxPoorResponses}";
            
        if(performance != lastDisplayedPerformance){
            performanceText.Text = performance;
            lastDisplayedPerformance = performance;
        }

        if(completedRoundCounterText != null)
            completedRoundCounterText.Text = $"Turn: {DebateDataManager.Instance.completedRoundCount}/{DebateDataManager.Instance.maxTurns}";
    }

    void GenerateSocialStats(){
        if(likeCount == null && commentCount == null) return;

        int baseLikes = Random.Range(500, 5000);
        int engagementBoost = Random.Range(0, 15000);
        int likes = baseLikes + engagementBoost;
        
        float commentRatio = Random.Range(0.01f, 0.1f);
        int comments = Mathf.RoundToInt(likes * commentRatio) + Random.Range(0, 500);

        if(engagementLogic != null) engagementLogic.RecordEngagement(likes, comments);
        if(likeCount != null) likeCount.Text = FormatNumber(likes);
        if(commentCount != null) commentCount.Text = FormatNumber(comments);
    }
    #endregion

    #region CHAT UI
    GameObject InstantiateCommentPrefab(GameObject prefab, string message, Role role, 
        string fallacyUsed = "", int score = 0){
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
                        quality = qualityEvaluator.CalculateQuality(DebateDataManager.Instance.lastPlayerReply);
                        playerTier = qualityEvaluator.DetermineTier(quality, DebateDataManager.Instance.lastJudgeScore);
                    }
                    
                    string specialText = "";
                    switch(role){
                        case Role.System:
                            specialText = $"Score: {DebateDataManager.Instance.lastJudgeScore?.total_score ?? 67} / 30";
                            break;
                        case Role.Player:
                            specialText = $"Tier: {playerTier?.ToString() ?? "N/A"} [{quality}]";
                            if(go.GetComponent<CommentData>() != null) go.GetComponent<CommentData>().SetData(quality);
                            break;
                        case Role.Enemy:
                            specialText = $"Fallacy: {DebateDataManager.Instance.currentOpponentTurn?.fallacy_type ?? "none"}";
                            if(go.GetComponent<CommentData>() != null) go.GetComponent<CommentData>().SetData((int)DebateDataManager.Instance.averageScore, "AI");
                            if(engagementLogic != null) engagementLogic.PopNotification(EngagementLogic.NotificationType.Reply);
                            break;
                    }
                    
                    specialTb.Text = specialText;
                }
            }
        }

        if(scroller != null) StartCoroutine(ScrollToBottom());
        return go;
    }

    IEnumerator ScrollToBottom(){
        yield return new WaitForEndOfFrame();
        if(scroller != null) scroller.ScrollToIndex(scroller.ScrollableChildCount-1, true);
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
        if(nameTB != null){ nameTB.Text = DebateDataManager.Instance.currentOpponentName; return; }
        
        foreach(var tb in tbs){
            if(tb.name.ToLower().Contains("name")){ 
                tb.Text = DebateDataManager.Instance.currentOpponentName; 
                break; 
            }
        }
    }

    TextBlock FindTextBlockByName(TextBlock[] all, string target){
        if(all == null || all.Length == 0 || string.IsNullOrEmpty(target)) return null;
        foreach(var tb in all) if(string.Equals(tb.name, target, System.StringComparison.OrdinalIgnoreCase)) return tb;
        foreach(var tb in all) if(tb.gameObject.name.ToLower().Contains(target.ToLower())) return tb;
        return null;
    }
    #endregion

    #region STRING UTILITIES
    public string FormatNumber(int n){
        if(n >= 1_000_000) return (n / 1_000_000f).ToString("0.#") + "M";
        if(n >= 1000) return (n / 1000f).ToString("0.#") + "K";
        return n.ToString();
    }

    string SanitizeForDisplay(string s){
        if(string.IsNullOrEmpty(s)) return s ?? "";
        string outStr = s.Replace("```", "");
        outStr = Regex.Replace(outStr, @"[\uD800-\uDBFF][\uDC00-\uDFFF]", "");
        outStr = Regex.Replace(outStr, @"[\u2600-\u26FF\u2700-\u27BF]", "");
        outStr = outStr.Replace(":)", "").Replace(":D", "").Replace(";)", "").Replace(":(", "");
        outStr = Regex.Replace(outStr, @"\s+", " ").Trim();
        return outStr;
    }
    #endregion

    #region PARSING
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
    #endregion

    #region PROMPT FORMATTING
    string EscapeForPrompt(string s){ 
        return string.IsNullOrEmpty(s) ? "" : s.Replace("\"", "'"); 
    }

    string BuildNaturalOpponentSpeech(OpponentTurn t){
        if(t == null) return "[Error: null opponent]";
        return t.argument.Trim();
    }

    string MakeFeedbackEducational(string feedback, JudgeScore score){
        if(string.IsNullOrEmpty(feedback) || feedback.Contains("No feedback")){
            StringBuilder sb = new StringBuilder();
            sb.Append($"Debate Analysis - Score: {score.total_score}/30. ");
            
            if(score.fallacy_score <= 3){
                sb.Append($"You didn't address the opponent's logical fallacy ({DebateDataManager.Instance.currentOpponentTurn?.fallacy_type ?? "unknown fallacy"}). ");
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

    #region NETWORK
    IEnumerator SendToOllama(string prompt, System.Action<string> onComplete){
        yield return network.SendToOllama(
            DebateDataManager.Instance.ollamaURL, 
            DebateDataManager.Instance.model, 
            DebateDataManager.Instance.requestTimeoutSeconds, 
            prompt, 
            onComplete
        );
    }
    #endregion

    #region AUTO COMPLETE
    void UseAutoReply(){
        if(!DebateDataManager.Instance.isDebateActive || 
           DebateDataManager.Instance.currentState != DebateDataManager.DebateState.Idle) return;
        
        string reply;
        if(isFirstAutoTurn){
            reply = autoReplies[0];
            DebateDataManager.Instance.currentAutoReplyIndex = 1;
            isFirstAutoTurn = false;
        }else{
            reply = autoReplies[DebateDataManager.Instance.currentAutoReplyIndex % autoReplies.Length];
            DebateDataManager.Instance.currentAutoReplyIndex++;
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
        while(DebateDataManager.Instance.currentState != DebateDataManager.DebateState.Idle) yield return null;
        yield return new WaitForSeconds(1f);
        if(DebateDataManager.Instance.isDebateActive && DebateDataManager.Instance.currentState == DebateDataManager.DebateState.Idle) UseAutoReply();
    }
    #endregion

    #region PUBLIC API
    public void ResetDebate(){
        DebateDataManager.Instance.ResetConversationState();

        int childrenToKeep = 2;
        var toDestroy = new List<GameObject>();
        for(int i = chatContainer.childCount - 1; i >= childrenToKeep; i--)
            toDestroy.Add(chatContainer.GetChild(i).gameObject);
        foreach(var go in toDestroy) Destroy(go);

        topicTextField.Text = "";
        replyTextField.Text = "";
        titleHeader.Text = DEFAULT_HEADER_TEXT;
        titleOpening.Text = DEFAULT_OPENING_TEXT;
            
        DebateDataManager.Instance.isDebateActive = false;
        DebateDataManager.Instance.cachedPerformanceSummary = null;
        DebateDataManager.Instance.cachedEndReason = null;
        
        StartCoroutine(ScrollToBottom());
    }

    public void ShowPerformanceSummary(){
        if(DebateDataManager.Instance.cachedPerformanceSummary == null) return;
        string fullMessage = $"{DebateDataManager.Instance.cachedPerformanceSummary}";
        InstantiateCommentPrefab(systemCommentPrefab, fullMessage, Role.System);
        if(scroller != null) StartCoroutine(ScrollToBottom());
        DebateDataManager.Instance.cachedPerformanceSummary = null;
    }

    public void OnSendReply(){
        if(replyTextField == null) return;
        string txt = replyTextField.Text?.Trim();
        if(string.IsNullOrEmpty(txt)) return;
        replyTextField.Text = "";
        OnSendReply_Internal(txt);
    }
    #endregion
}