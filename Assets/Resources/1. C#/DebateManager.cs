// using System.Collections;
// using System.Collections.Generic;
// using System.Text;
// using System.Text.RegularExpressions;
// using UnityEngine;
// using UnityEngine.Networking;
// using UnityEngine.UI;
// using TMPro;
// using System.IO;
// using System.Linq;

// [System.Serializable]
// public class DebateTopic{
//     public string header;
//     public string opening;
// }

// [System.Serializable]
// public class OpponentTurn{
//     public string argument;
//     public string fallacy_type;
//     public string insult;
// }

// [System.Serializable]
// public class JudgeScore{
//     public int fallacy_score;
//     public int logic_score;
//     public int insult_score;
//     public int total_score;
// }

// [System.Serializable]
// public class OllamaRequest{
//     public string model;
//     public string prompt;
//     public bool stream;
// }

// [System.Serializable]
// public class OllamaResponse{
//     public string response;
// }

// public class DebateManager : MonoBehaviour
// {
//     [Header("OLLAMA")]
//     public string ollamaURL = "http://127.0.0.1:11434/api/generate";
//     public string model = "mistral";

//     [Header("UI LAYOUT")]
//     public RectTransform chatContent;
//     public GameObject bubbleOpponentPrefab;
//     public GameObject bubblePlayerPrefab;
//     public GameObject bubbleSystemPrefab;
//     public ScrollRect chatScrollRect;
//     [Space(10)]
//     public TMP_Text topicHeaderText;
//     public GameObject formPanel;

//     [Header("UI CONTROLS")]
//     public TMP_InputField playerInput;
//     public Button sendButton;
//     public TMP_Text scorePanelText;

//     [Header("TOPIC CREATION")]
//     public TMP_InputField topicInputField;
//     // public Button topicGenerateButton;

//     private DebateTopic currentTopic;
//     public bool IsDebateActive => currentTopic != null;
//     private OpponentTurn currentOpponentTurn;
//     private JudgeScore lastJudgeScore;
//     string lastPlayerReply;

//     private bool waitingForResponse = false;
//     private bool opponentBusy = false;

//     //String HashMap
//     private HashSet<string> englishWords;
//     private HashSet<string> spectatorWords;
//     Dictionary<string, string[]> fallacyPool = new Dictionary<string, string[]> {
//         {"attack", new[]{ "Ad Hominem", "Strawman", "False Dilemma" }},
//         {"logic_bend", new[]{ "False Equivalence", "Circular Reasoning", "No True Scotsman" }},
//         {"diversion", new[]{ "Red Herring", "Tu Quoque", "Appeal to Hypocrisy" }},
//         {"emotional", new[]{ "Appeal to Emotion", "Bandwagon", "Overgeneralization" }},
//         {"neutral", new[]{ "Slippery Slope", "False Balance", "Ambiguity" }}
//     };
//     private List<string> suitableForOpening = new List<string> { "Ad Hominem", "Strawman", "Appeal to Emotion", "Bandwagon", "False Dilemma", "Slippery Slope" };

//     string[] randomSpectators = new string[]{
//         "Interesting point!",
//         "I hadn't considered that.",
//         "That's a bold claim.",
//         "Can you back that up?",
//         "I'm not convinced.",
//         "Well argued!",
//         "That's a common misconception.",
//         "I see where you're coming from.",
//         "Could you elaborate?",
//         "That's quite the fallacy!"
//     };

//     void Start(){
//         sendButton.onClick.AddListener(OnSendPressed);
//         topicInputField.onSubmit.AddListener(_ => OnTopicGeneratePressed());
//         // StartCoroutine(RequestDebateTopic());

//         StartCoroutine(LoadDictionary());
//     }

//     #region UTILS
//     IEnumerator LoadDictionary(){
//         TextAsset wordList = Resources.Load<TextAsset>("Txt/words_alpha");
//         if(wordList != null){
//             string[] words = wordList.text.Split('\n');
//             englishWords = new HashSet<string>(words.Select(w => w.Trim().ToLower()));
//         }else Debug.LogError("Word list not found in Resources/EnglishWord/");
        
//         yield break;
//     }
//     bool IsEnglishWord(string input) => englishWords.Contains(input.ToLower());
//     string SuggestClosestWord(string input){
//         if(englishWords == null || englishWords.Count == 0) return input;
//         if(string.IsNullOrWhiteSpace(input)) return input;

//         string bestMatch = input;
//         int bestDistance = int.MaxValue;

//         foreach(string word in englishWords){
//             int dist = LevenshteinDistance(input, word);
//             if(dist < bestDistance){
//                 bestDistance = dist;
//                 bestMatch = word;
//             }
//             if(bestDistance == 1) break;
//         }

//         return bestDistance <= 2 ? bestMatch : input;
//     }
//     int LevenshteinDistance(string a, string b){
//         int[,] dp = new int[a.Length + 1, b.Length + 1];

//         for(int i = 0; i <= a.Length; i++) dp[i, 0] = i;
//         for(int j = 0; j <= b.Length; j++) dp[0, j] = j;

//         for(int i = 1; i <= a.Length; i++){
//             for(int j = 1; j <= b.Length; j++){
//                 int cost = (a[i - 1] == b[j - 1]) ? 0 : 1;

//                 dp[i, j] = Mathf.Min(
//                     Mathf.Min(dp[i - 1, j] + 1, dp[i, j - 1] + 1),
//                     dp[i - 1, j - 1] + cost
//                 );
//             }
//         }

//         return dp[a.Length, b.Length];
//     }
//     string TraitToneRule(string trait){
//         switch(trait){
//             case "attack":
//                 return "Tone: aggressive, mocking, and confrontational — you feel insulted and want to strike back while pretending to stay logical.";
//             case "logic_bend":
//                 return "Tone: smug and overly rational — act superior, twist logic to appear right even when you’re not.";
//             case "diversion":
//                 return "Tone: evasive and defensive — change the subject or redirect blame subtly without admitting fault.";
//             case "emotional":
//                 return "Tone: emotional, frustrated, and reactive — lean into feelings, exaggerate, and appeal to sympathy or outrage.";
//             case "neutral":
//                 return "Tone: composed but subtly manipulative — appear reasonable while embedding small logical traps.";
//             default:
//                 return "Tone: conversational and reactive — maintain the debate flow naturally.";
//         }
//     }
//     #endregion

//     void Update(){
//         if(formPanel.activeSelf && Input.GetKeyDown(KeyCode.Return)) OnSendPressed();
//     }

//     #region LAYOUT
//     void AddOpponentBubble(string message){
//         var go = Instantiate(bubbleOpponentPrefab, chatContent);
//         go.GetComponentInChildren<TMP_Text>().text = SanitizeForDisplay(message);
//         StartCoroutine(RefreshLayout());
//     }

//     void AddPlayerBubble(string message){
//         var go = Instantiate(bubblePlayerPrefab, chatContent);
//         go.GetComponentInChildren<TMP_Text>().text = SanitizeForDisplay(message);
//         StartCoroutine(RefreshLayout());
//     }

//     void AddSystemBubble(string message){
//         var go = Instantiate(bubbleSystemPrefab, chatContent);
//         go.GetComponentInChildren<TMP_Text>().text = SanitizeForDisplay(message);
//         StartCoroutine(RefreshLayout());
//     }

//     private IEnumerator RefreshLayout(){
//         yield return null;
//         LayoutRebuilder.ForceRebuildLayoutImmediate(chatContent);
//         Canvas.ForceUpdateCanvases();
//         chatScrollRect.verticalNormalizedPosition = 0f;
//         Canvas.ForceUpdateCanvases();
//     }
//     #endregion

//     #region TURN FLOW
//     IEnumerator RequestDebateTopic(string topic){
//         string chosenFallacy = suitableForOpening[Random.Range(0, suitableForOpening.Count)];

//         string prompt = $@"
// You are simulating a controversial online debate thread centered around the topic: ""{EscapeForPrompt(topic)}"".

// Your task:
// Write the **first post** of the thread — an opinionated, emotional, and confident argument that **must include a clear logical fallacy** : **{chosenFallacy}**.

// Output ONLY valid JSON in this exact structure:
// {{
//   ""header"": ""a short provocative title (1 sentence, human-like phrasing)"",
//   ""opening"": ""the first post written by a user, 2-3 sentences long, containing at least one explicit logical fallacy but still sounding persuasive.""
// }}

// Critical Rules:
// - **The post MUST contain at least one identifiable logical fallacy.**
//   Examples of acceptable fallacies:
//     - Ad hominem
//     - Strawman
//     - Slippery slope
//     - False equivalence
//     - Red herring
//     - Appeal to emotion
//     - Bandwagon
// - Make the fallacy sound natural — don’t label it or mention the name.
// - Do NOT explain what the fallacy is.
// - Tone: informal, emotional, confident — like a heated online opinion.
// - No disclaimers, no commentary, no markdown, and no extra text outside the JSON.
// ";

//         yield return SendToOllama(prompt, result => {
//             string sanitizedResult = SanitizeModelOutput(result);
//             string clean = ExtractJson(sanitizedResult);
//             if(string.IsNullOrEmpty(clean)){
//                 AddSystemBubble("[Error] Could not parse debate topic. Raw: " + SanitizeForDisplay(result));
//                 return;
//             }

//             if(!TryParseJson<DebateTopic>(clean, out currentTopic)){
//                 currentTopic = LooseParseDebateTopic(clean);
//                 if(currentTopic == null){
//                     AddSystemBubble("[Error] Could not parse debate topic.");
//                     return;
//                 }
//             }

//             topicHeaderText.text = currentTopic.header;
//             AddOpponentBubble(currentTopic.opening);

//             currentOpponentTurn = new OpponentTurn{
//                 argument = currentTopic.opening,
//                 fallacy_type = chosenFallacy,
//                 insult = ""
//             };
//         });
//     }

//     void OnSendPressed(){
//         if(waitingForResponse) return;

//         string playerText = playerInput.text?.Trim();
//         if(string.IsNullOrEmpty(playerText)) return;
//         lastPlayerReply = playerText;

//         AddPlayerBubble(playerText);
//         playerInput.text = string.Empty;
//         playerInput.ActivateInputField();

//         waitingForResponse = true;
//         StartCoroutine(RequestJudgeScore(currentOpponentTurn, playerText));
//     }

//     void OnTopicGeneratePressed(){
//         currentTopic = null;
//         currentOpponentTurn = null;
//         lastJudgeScore = null;
//         lastPlayerReply = null;

//         scorePanelText.text = "";
//         topicHeaderText.text = "";
//         playerInput.text = "";

//         string topic = topicInputField.text?.Trim();
//         if(string.IsNullOrEmpty(topic)) return;
//         else if(!IsEnglishWord(topic)){
//             string suggestion = SuggestClosestWord(topic);
//             if(suggestion != topic){
//                 AddSystemBubble($"[Auto-corrected] '{topic}' → '{suggestion}'");
//                 topic = suggestion;
//             }else AddSystemBubble($"[Warning] '{topic}' not recognized as an English word.");
//         }
//         StartCoroutine(RequestDebateTopic(topic));
//     }

//     private IEnumerator RequestJudgeScore(OpponentTurn opponent, string playerReply){
//         string prompt = $@"
// You are a neutral but strict debate judge evaluating replies in an online forum argument.

// Current topic: ""{EscapeForPrompt(currentTopic.header)}""
// Thread opening: ""{EscapeForPrompt(currentTopic.opening)}""

// Opponent's previous argument:
// ""{EscapeForPrompt(opponent.argument)}""
// (Fallacy type: {EscapeForPrompt(opponent.fallacy_type)})

// Player's reply:
// ""{EscapeForPrompt(playerReply)}""

// Your task:
// Evaluate how well the player's reply performs in the debate, given the topic and the opponent's use of a fallacy.

// Scoring criteria:
// - **fallacy_score (0-10):** Did they correctly identify or effectively counter the fallacy used?
// - **logic_score (0-10):** Is the reasoning coherent, persuasive, and relevant to the topic?
// - **insult_score (0-10):** Was their comeback sharp or witty without being purely rude?
// - **total_score:** Sum of the three above.

// Rules:
// - Be consistent with the thread’s context — relevance to the main topic matters.
// - Don't reward vague moralizing or off-topic statements.
// - Keep all scores as integers (no decimals).
// - Ifthe reply shows emotional intelligence or nuanced reasoning, reflect that in the logic score.

// Output format:
// Return ONLY JSON exactly like: {{""fallacy_score"":X,""logic_score"":Y,""insult_score"":Z,""total_score"":SUM}}

// Then, on the next line, add one feedback line starting with:
// FEEDBACK: (short judge comment, 1-2 sentences giving rationale).

// Example:
// FEEDBACK: Strong logical counter but lacked direct fallacy naming.
// ";

//         yield return SendToOllama(prompt, result => {
//             string sanitizedResult = SanitizeModelOutput(result);
//             string json = ExtractJson(sanitizedResult);
//             string feedback = ExtractTrailingFeedback(sanitizedResult);

//             if(string.IsNullOrEmpty(json)){
//                 AddSystemBubble("[Error] Could not parse judge JSON. Raw: " + SanitizeForDisplay(result));
//                 waitingForResponse = false;
//                 return;
//             }

//             if(!TryParseJson<JudgeScore>(json, out lastJudgeScore)){
//                 lastJudgeScore = LooseParseJudgeScore(json);
//                 if(lastJudgeScore == null){
//                     AddSystemBubble("[Error] Judge JSON parse failed after attempts.");
//                     waitingForResponse = false;
//                     return;
//                 }
//             }

//             scorePanelText.text =
//                 $"F: {lastJudgeScore.fallacy_score}  " +
//                 $"L: {lastJudgeScore.logic_score}  " +
//                 $"I: {lastJudgeScore.insult_score}  " +
//                 $"[{lastJudgeScore.total_score}]";

//             if(!string.IsNullOrEmpty(feedback))
//                 AddSystemBubble(feedback);

//             StartCoroutine(RequestOpponentReply(lastJudgeScore, lastPlayerReply));
//             waitingForResponse = false;
//         });
//     }

//     private IEnumerator RequestOpponentReply(JudgeScore score, string playerReply){
//         if(opponentBusy) yield break;
//         opponentBusy = true;
//         string chosenTrait;

//         if(score.insult_score >= 8) chosenTrait = "attack";
//         else if(score.logic_score >= 8) chosenTrait = "logic_bend";
//         else if(score.fallacy_score >= 8) chosenTrait = "diversion";
//         else if(score.total_score <= 12) chosenTrait = "emotional";
//         else chosenTrait = "neutral";

//         string chosenFallacy = fallacyPool[chosenTrait][Random.Range(0, fallacyPool[chosenTrait].Length)];

//         string prompt = $@"
// You are continuing a heated online debate thread as the opponent.
// React to your score ({score.total_score}) AND reply directly to the player's last comment.

// Topic: ""{EscapeForPrompt(currentTopic.header)}""
// Your previous argument: ""{EscapeForPrompt(currentOpponentTurn.argument)}""
// Player said: ""{EscapeForPrompt(playerReply)}""

// Write one natural post that combines both emotional reaction to the score AND a direct counter-argument.

// Formatting & Output Rules:
// - Output ONLY valid JSON (no markdown, no commentary, no extra text).
// - JSON fields must use lowercase keys and values must be plain strings without line breaks.
// - Keep the style human and conversational.

// JSON structure:
// {{
//   ""argument"": ""2-3 full sentences forming your reply."",
//   ""fallacy_type"": ""name of the logical fallacy used (e.g., ad hominem, strawman, slippery slope, red herring, appeal to emotion, false equivalence)"",
//   ""insult"": ""short sarcastic jab, under 10 words, or null if not used.""
// }}

// Tone and Emotion Rules:
// - {TraitToneRule(chosenTrait)}
// - The fallacy must be obvious but natural — do not mention its name.
// - If using an insult, make it witty but not overly harsh.

// Examples of valid outputs:
// {{
//   ""argument"": ""Oh sure, because quoting Wikipedia makes you an expert now. Maybe think for yourself next time."",
//   ""fallacy_type"": ""ad hominem"",
//   ""insult"": ""try harder next time""
// }}

// {{
//   ""argument"": ""You're missing the point completely — this is about personal responsibility, not statistics."",
//   ""fallacy_type"": ""strawman"",
//   ""insult"": null
// }}
// ";
//         Debug.Log(TraitToneRule(chosenTrait) + " | Fallacy: " + chosenFallacy);

//         yield return SendToOllama(prompt, result => {
//             string sanitizedResult = SanitizeModelOutput(result);
//             string clean = ExtractJson(sanitizedResult);
//             if(string.IsNullOrEmpty(clean)){
//                 AddSystemBubble("[Error] Could not parse opponent JSON. Raw: " + SanitizeForDisplay(result));
//                 opponentBusy = false;
//                 return;
//             }

//             if(!TryParseJson<OpponentTurn>(clean, out currentOpponentTurn)){
//                 currentOpponentTurn = LooseParseOpponentTurn(clean);
//                 if(currentOpponentTurn == null){
//                     AddSystemBubble("[Error] Opponent JSON parse failed after attempts. Raw: " + SanitizeForDisplay(result));
//                     opponentBusy = false;
//                     return;
//                 }
//             }
//             string spoken = BuildNaturalOpponentSpeech(currentOpponentTurn);
//             AddOpponentBubble(spoken);
//         });

//         opponentBusy = false;
//     }
//     #endregion

//     #region NETWORK
//     private IEnumerator SendToOllama(string prompt, System.Action<string> onComplete){
//         var reqData = new OllamaRequest{
//             model = model,
//             prompt = prompt,
//             stream = false
//         };

//         string bodyJson = JsonUtility.ToJson(reqData);

//         UnityWebRequest req = new UnityWebRequest(ollamaURL, "POST");
//         req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(bodyJson));
//         req.downloadHandler = new DownloadHandlerBuffer();
//         req.SetRequestHeader("Content-Type", "application/json");

//         yield return req.SendWebRequest();

//         if(req.result == UnityWebRequest.Result.Success){
//             try{
//                 OllamaResponse wrapper = JsonUtility.FromJson<OllamaResponse>(req.downloadHandler.text);
//                 if(!string.IsNullOrEmpty(wrapper.response)){
//                     onComplete(wrapper.response);
//                     yield break;
//                 }
//             }catch { }

//             onComplete(req.downloadHandler.text);
//         }else{
//             Debug.LogError("Ollama Error: " + req.error);
//             AddSystemBubble("[Network] Ollama Error: " + req.error);
//         }
//     }

//     string ExtractJson(string raw) {
//         if(string.IsNullOrEmpty(raw)) return null;
        
//         raw = raw.Replace("```json", "").Replace("```", "").Trim();
//         MatchCollection matches = Regex.Matches(raw, "\\{[\\s\\S]*?\\}");
//         string json = null;

//         if(matches.Count > 0) json = matches.OrderByDescending(m => m.Value.Length).First().Value.Trim();
//         else{
//             if(Regex.IsMatch(raw, "\"[a-zA-Z0-9_]+\"\\s*:\\s*\"")) json = "{" + raw.Trim().Trim(',') + "}";
//             else return null;
//         }

//         return SanitizeJson(json);
//     }
//     string SanitizeJson(string json){
//         if(string.IsNullOrEmpty(json)) return json;

//         json = json.Replace("“", "\"").Replace("”", "\"").Replace("„", "\"").Replace("‟", "\"");
//         json = Regex.Replace(json, @"[\uD800-\uDBFF][\uDC00-\uDFFF]", "");
//         json = Regex.Replace(json, ",\\s*([}\\]])", "$1");

//         int last = json.LastIndexOf('}');
//         if(last >= 0 && last < json.Length - 1) json = json.Substring(0, last + 1);

//         return json;
//     }

//     string ExtractTrailingFeedback(string raw){
//         if(string.IsNullOrEmpty(raw)) return null;

//         Match m = Regex.Match(raw, "FEEDBACK:\\s*(.+)", RegexOptions.IgnoreCase);
//         if(m.Success) return m.Groups[1].Value.Trim();
//         return null;
//     }

//     string SanitizeModelOutput(string s){
//         if(string.IsNullOrEmpty(s)) return s;
//         s = s.Replace("\r\n", "\n");
//         s = s.Replace("```json", "").Replace("```", "");
//         s = Regex.Replace(s, "[\x00-\x1F\x7F]+", " ");
//         s = Regex.Replace(s, @"[\uD83C-\uDBFF\uDC00-\uDFFF]+", "", RegexOptions.Compiled);
//         s = Regex.Replace(s, "[\u2600-\u26FF\u2700-\u27BF]", "", RegexOptions.Compiled);
//         return s.Trim();
//     }

//    string SanitizeForDisplay(string s){
//         if(string.IsNullOrEmpty(s)) return s ?? "";
//         string outStr = s;

//         outStr = outStr.Replace("```", "");
//         outStr = Regex.Replace(outStr, @"[\uD800-\uDBFF][\uDC00-\uDFFF]", "");
//         outStr = Regex.Replace(outStr, @"[\u2600-\u26FF\u2700-\u27BF]", "");

//         outStr = outStr.Replace(":)", "").Replace(":D", "").Replace(";)", "").Replace(":(", "");
//         outStr = Regex.Replace(outStr, @"\s+", " ").Trim();

//         return outStr;
//     }

//     private bool TryParseJson<T>(string json, out T obj) where T : class{
//         obj = null;
//         if(string.IsNullOrEmpty(json)) return false;
//         string cleaned = SanitizeJson(json);
//         try{
//             obj = JsonUtility.FromJson<T>(cleaned);
//             return obj != null;
//         }catch (System.Exception ex){
//             Debug.LogWarning("TryParseJson failed: " + ex.Message);
//             string repaired = QuickRepairJson(cleaned);
//             try{
//                 obj = JsonUtility.FromJson<T>(repaired);
//                 return obj != null;
//             }catch { return false; }
//         }
//     }

//     string QuickRepairJson(string s){
//         if(string.IsNullOrEmpty(s)) return s;
//         if(!s.StartsWith("{")) s = "{" + s;
//         if(!s.EndsWith("}")) s = s + "}";

//         int quotes = 0;
//         foreach (char c in s) if(c == '"') quotes++;
//         if((quotes % 2) == 1) s += '"';
//         return s;
//     }

//     private OpponentTurn LooseParseOpponentTurn(string raw){
//         if(string.IsNullOrEmpty(raw)) return null;

//         var ot = new OpponentTurn();
//         Match mArg = Regex.Match(raw, @"\""argument\""\s*:\s*\""([\s\S]*?)\""", RegexOptions.IgnoreCase);
//         Match mFall = Regex.Match(raw, @"\""fallacy_type\""\s*:\s*\""([^\""]*)\""", RegexOptions.IgnoreCase);
//         Match mIns = Regex.Match(raw, @"\""insult\""\s*:\s*\""([\s\S]*?)\""", RegexOptions.IgnoreCase);

//         ot.argument = mArg.Success ? mArg.Groups[1].Value : raw;
//         ot.fallacy_type = mFall.Success ? mFall.Groups[1].Value : "unknown";
//         ot.insult = mIns.Success ? mIns.Groups[1].Value : "";
//         return ot;
//     }

//     private JudgeScore LooseParseJudgeScore(string raw){
//         if(string.IsNullOrEmpty(raw)) return null;
//         var js = new JudgeScore();
//         int v;

//         Match mF = Regex.Match(raw, @"\""fallacy_score\""\s*:\s*(\d+)", RegexOptions.IgnoreCase);
//         Match mL = Regex.Match(raw, @"\""logic_score\""\s*:\s*(\d+)", RegexOptions.IgnoreCase);
//         Match mI = Regex.Match(raw, @"\""insult_score\""\s*:\s*(\d+)", RegexOptions.IgnoreCase);

//         if(mF.Success && int.TryParse(mF.Groups[1].Value, out v)) js.fallacy_score = v; else js.fallacy_score = 0;
//         if(mL.Success && int.TryParse(mL.Groups[1].Value, out v)) js.logic_score = v; else js.logic_score = 0;
//         if(mI.Success && int.TryParse(mI.Groups[1].Value, out v)) js.insult_score = v; else js.insult_score = 0;

//         js.total_score = js.fallacy_score + js.logic_score + js.insult_score;
//         return js;
//     }

//     private DebateTopic LooseParseDebateTopic(string raw){
//         if(string.IsNullOrEmpty(raw)) return null;

//         var dt = new DebateTopic();
//         Match mH = Regex.Match(raw, @"\""header\""\s*:\s*\""([\s\S]*?)\""", RegexOptions.IgnoreCase);
//         Match mO = Regex.Match(raw, @"\""opening\""\s*:\s*\""([\s\S]*?)\""", RegexOptions.IgnoreCase);

//         dt.header = mH.Success ? mH.Groups[1].Value : "(Untitled Topic)";
//         dt.opening = mO.Success ? mO.Groups[1].Value : "(No opening provided)";
//         return dt;
//     }


//     string EscapeForPrompt(string s){
//         if(string.IsNullOrEmpty(s)) return "";
//         return s.Replace("\"", "'");
//     }
//     #endregion

//     #region FORMAT
//     string BuildNaturalOpponentSpeech(OpponentTurn t){
//         if(t == null) return "[Error: null opponent]";
//         string arg = t.argument.Trim();
//         string insult = t.insult?.Trim();

//         if(!string.IsNullOrEmpty(insult)){
//             float pick = Random.value;
//             if(pick < 0.5f) return $"{arg} {Capitalize(insult)}";
//             else return $"{arg}\n\n\"{Capitalize(insult)}\"";
//         }
//         return arg;
//     }

//     string Capitalize(string s){
//         if(string.IsNullOrEmpty(s)) return s;
//         s = s.Trim();
//         return char.ToUpper(s[0]) + s.Substring(1);
//     }
//     #endregion
// }
