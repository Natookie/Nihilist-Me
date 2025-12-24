using Nova;
using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class Console : MonoBehaviour
{
    [Header("REFERENCES")]
    public UIBlock Root;
    public Transform textRoot;

    [Header("CONSOLE STYLE")]
    public GameObject headerPrefab;
    public GameObject messagePrefab;
    public GameObject inputPrefab;
    public Color defaultColor = Color.white;

    private bool isOnFocus;
    private string currentInput = "";
    private TextBlock activeInputText;

    private int crystals = 200;

    private const int maxWordleAttempts = 5;
    private const int wordleBetCost = 10;

    private const int gachaCost = 20;

    private enum ConsoleState { MainMenu, GameMenu, WaitingForEnter, WordleGame, Gacha, Inventory, Shop, WordleEnd }
    private ConsoleState state = ConsoleState.MainMenu;

    private string targetWord;
    private HashSet<string> englishWords;
    private List<string> guesses = new();

    private HashSet<char> usedLetters = new();

    private List<Cosmetic> cosmetics = new();
    private List<Cosmetic> inventory = new();

    private HashSet<string> unlockedCosmetics = new HashSet<string>();

    bool returningFromTutorial = false;

    void Start(){
        Root ??= GetComponentInParent<UIBlock>();
        Root.AddGestureHandler<Gesture.OnPress>(OnClick);

        InitializeCosmeticsMasterList();
        LoadUnlockedCosmetics();

        StartCoroutine(LoadDictionary());
        DisplayMain();
        CreateInputLine();
    }

    void OnClick(Gesture.OnPress press) => isOnFocus = true;

    void Update(){
        if(!isOnFocus) return;

        if(state == ConsoleState.WaitingForEnter && activeInputText == null){
            if(Input.GetKeyDown(KeyCode.Return)){
                ClearConsole();
                if(returningFromTutorial){
                    returningFromTutorial = false;
                    ShowGameMenu();
                }else{
                    DisplayMain();
                    CreateInputLine();
                    state = ConsoleState.MainMenu;
                }
            }
            return;
        }

        if(activeInputText == null) return;
        foreach(char c in Input.inputString){
            if(c == '\b'){
                if(currentInput.Length > 0) currentInput = currentInput[..^1];
            }
            else if(c == '\n' || c == '\r'){
                SubmitCommand(currentInput.Trim().ToLower());
                return;
            }
            else if(state == ConsoleState.WordleGame || state == ConsoleState.WordleEnd){
                if(char.IsLetter(c) && currentInput.Length < 5) currentInput += char.ToLower(c);
            }
            else currentInput += c;
        }

        if(state == ConsoleState.WordleGame)
            UpdateWordleInputDisplay();
        else
            activeInputText.Text = currentInput;
    }

    #region UI
    void DisplayMain(){
        AddText("=========================================================================================", Color.cyan, true);
        AddText("░█▀█░█▀▀░█░█░█▀█░█▀█░░░█░█░█▀▀░█▀▄░█▀█░█▀▀░█░░░", Color.cyan, true);
        AddText("░█░█░█▀▀░▄▀▄░█░█░█░█░░░█▀▄░█▀▀░█▀▄░█░█░█▀▀░█░░░", Color.cyan, true);
        AddText("░▀░▀░▀▀▀░▀░▀░▀▀▀░▀░▀░░░▀░▀░▀▀▀░▀░▀░▀░▀░▀▀▀░▀▀▀░", Color.cyan, true);
        AddText("=========================================================================================", Color.cyan, true);
        AddText("Welcome back, Beeze Cooda!", Color.green);
        AddText("Crystals: " + crystals, Color.yellow);
        AddText(" ");
        AddText("1. Play Game");
        AddText("2. Gacha");
        AddText("3. See Inventory");
        AddText("4. Visit Shop");
    }

    void CreateInputLine(){
        if(inputPrefab == null) return;

        GameObject instance = Instantiate(inputPrefab, textRoot);
        TextBlock[] texts = instance.GetComponentsInChildren<TextBlock>();
        foreach(var t in texts) if(t.name.Contains("Input")) activeInputText = t;

        currentInput = "";
        ScrollToTop();
    }
    #endregion

    IEnumerator LoadDictionary(){
        TextAsset wordList = Resources.Load<TextAsset>("Txt/WordleWord");
        if(wordList != null){
            string[] words = wordList.text.Split('\n');
            englishWords = new HashSet<string>(words.Select(w => w.Trim().ToLower()).Where(w => w.Length == 5));
        }else{
            Debug.LogError("Word list not found");
            englishWords = new();
        }
        yield break;
    }

    #region INPUT LOGIC
    void SubmitCommand(string cmd){
        if(string.IsNullOrEmpty(cmd)) return;

        string inputCopy = cmd;
        currentInput = "";

        switch(state){
            case ConsoleState.MainMenu:
                DestroyInputBox();
                HandleMainCommand(inputCopy);
                break;

            case ConsoleState.GameMenu:
                DestroyInputBox();
                HandleGameMenuInput(inputCopy);
                break;

            case ConsoleState.Gacha:
                DestroyInputBox();
                HandleGachaInput(inputCopy);
                break;

            case ConsoleState.Inventory:
                DestroyInputBox();
                HandleInventoryInput(inputCopy);
                break;

            case ConsoleState.Shop:
                DestroyInputBox();
                HandleShopInput(inputCopy);
                break;

            case ConsoleState.WordleGame:
                HandleWordleInput(inputCopy);
                break;

            case ConsoleState.WordleEnd:
                HandleWordleEndInput(inputCopy);
                break;
        }
    }

    void HandleMainCommand(string cmd){
        switch(cmd){
            case "1":
            case "play":
                ShowGameMenu();
                break;

            case "2":
            case "gacha":
                StartGacha();
                break;

            case "3":
            case "inventory":
                ShowInventory();
                break;

            case "4":
            case "shop":
                ShowShop();
                break;

            default:
                AddText("Unknown command.", Color.red);
                WaitForEnter();
                break;
        }
    }
    #endregion

    #region MENU
    void ShowGameMenu(){
        ClearConsole();
        state = ConsoleState.GameMenu;

        AddText("WORDLE MENU", Color.yellow);
        AddText("1. Play");
        AddText("2. Tutorial");
        AddText("3. Go Back");

        CreateInputLine();
    }

    void HandleGameMenuInput(string cmd){
        switch(cmd){
            case "1":
            case "play":
                if(crystals < wordleBetCost){
                    AddText("Not enough crystals to play Wordle.", Color.red);
                    AddText("You need " + wordleBetCost + " crystals to start a round.", Color.yellow);
                    CreateInputLine();
                    return;
                }
                crystals -= wordleBetCost;
                DestroyInputBox();
                StartWordle();
                break;

            case "2":
            case "tutorial":
                ShowWordleTutorial();
                break;

            case "3":
            case "0":
            case "back":
                ClearConsole();
                DisplayMain();
                CreateInputLine();
                state = ConsoleState.MainMenu;
                break;

            default:
                AddText("Unknown command.", Color.red);
                CreateInputLine();
                break;
        }
    }

    void ShowWordleTutorial(){
        ClearConsole();

        AddText("WORDLE TUTORIAL", Color.cyan);
        AddText("Guess a 5-letter English word.");
        AddText("<color=green>Green</color> = Correct letter & correct position.");
        AddText("<color=yellow>Yellow</color> = Correct letter, wrong position.");
        AddText("<color=#B0B0B0>Gray</color> = Letter not in the word.");
        AddText(" ");

        AddText("You have <b>" + maxWordleAttempts + " attempts</b> to guess the correct word.");
        AddText(" ");
        AddText("Each Wordle round costs <b>" + wordleBetCost + "</b> crystals to play.");
        AddText("If you guess the word correctly:");
        AddText("- You get your bet back");
        AddText("- You earn bonus crystals based on how fast you solved it");
        AddText("  (Fewer attempts = bigger bonus!)");
        AddText(" ");

        AddText("Press Enter to return.", Color.white);

        returningFromTutorial = true;
        state = ConsoleState.WaitingForEnter;
    }
    #endregion

    #region WORDLE
    void StartWordle(){
        if(englishWords == null || englishWords.Count == 0){
            AddText("Word dictionary not loaded!", Color.red);
            WaitForEnter();
            return;
        }

        ClearConsole();
        state = ConsoleState.WordleGame;
        guesses.Clear();
        usedLetters.Clear();
        targetWord = englishWords.ElementAt(UnityEngine.Random.Range(0, englishWords.Count));

        AddText("Guess the 5-letter word", Color.yellow);
        AddText("<color=green>Correct</color> | <color=yellow>Misplaced</color> | <color=#B0B0B0>Wrong</color>");
        AddText(" ");
        AddText($"Crystals: <color=yellow>{crystals}</color> | <color=white>'gback' to go back</color>");
        AddText("====================================");
        AddText(" ");

        CreateWordleInput();
    }

    void HandleWordleInput(string input){
        if(input == "gback"){
            ClearConsole();
            ShowGameMenu();
            return;
        }

        if(input.Equals("bulll", StringComparison.OrdinalIgnoreCase)){
            AddText("<color=red>🧠 DEBUG:</color> Target word = <color=cyan>" + targetWord.ToUpper() + "</color>");
            CreateWordleInput();
            return;
        }

        if(input.Length != 5 || !englishWords.Contains(input)){
            StartCoroutine(FlashInvalidInput(input));
            return;
        }

        guesses.Add(input);
        UpdateUsedLetters(input);
        DisplayWordleResult(input);

        if(input == targetWord){
            int bonus = Mathf.Max(10 - guesses.Count * 2, 1);
            crystals += wordleBetCost + bonus;

            StartCoroutine(FlashResult(Color.green, $"Correct! Returned {wordleBetCost} + bonus {bonus} = +{(wordleBetCost + bonus)} Crystals."));
            state = ConsoleState.WordleEnd;
        }else if(guesses.Count >= maxWordleAttempts){
            int loss = 3;
            crystals = Mathf.Max(0, crystals - loss);
            StartCoroutine(FlashResult(Color.red, $"Failed! The word was: {targetWord}\nLost {loss} crystals."));
            state = ConsoleState.WordleEnd;
        }
        else CreateWordleInput();
    }

    IEnumerator FlashResult(Color color, string message){
        AddText(message, color);
        yield return new WaitForSeconds(0.5f);
        AddText("Play again? (Y/N)", Color.gray);
        CreateInputLine();
    }

    void HandleWordleEndInput(string cmd){
        if(cmd == "y" || cmd == "yes"){
            DestroyInputBox();
            if(crystals < wordleBetCost){
                AddText("Not enough crystals to play another round.", Color.red);
                WaitForEnter();
                return;
            }

            crystals -= wordleBetCost;
            StartWordle();
        }else if(cmd == "n" || cmd == "no" || cmd == "0"){
            DestroyInputBox();
            ClearConsole();
            DisplayMain();
            CreateInputLine();
            state = ConsoleState.MainMenu;
        }else{
            AddText("Please type Y or N.", Color.red);
            CreateInputLine();
        }
    }

    void CreateWordleInput(){
        if(inputPrefab == null) return;

        GameObject instance = Instantiate(inputPrefab, textRoot);
        activeInputText = instance.GetComponentInChildren<TextBlock>();
        currentInput = "";
        UpdateWordleInputDisplay();
    }

    void UpdateWordleInputDisplay(){
        if(activeInputText == null) return;

        string display = ">> ";
        foreach(char c in currentInput.ToUpper()){
            if(usedLetters.Contains(c)) display += $"<color=#B0B0B0>{c}</color> ";
            else display += $"<color=white>{c}</color> ";
        }

        for(int i = currentInput.Length; i < 5; i++) display += "_ ";

        activeInputText.Text = display.TrimEnd();
    }

    IEnumerator FlashInvalidInput(string badInput){
        if(activeInputText == null) yield break;

        TextBlock txt = activeInputText;
        txt.Text = ">> " + badInput.ToUpper();
        txt.Color = Color.red;
        yield return new WaitForSeconds(0.4f);
        txt.Color = defaultColor;
        currentInput = "";
        UpdateWordleInputDisplay();
    }
    #endregion

    #region WORDLE RESULT
    void DisplayWordleResult(string guess){
        Dictionary<char, int> freq = new();
        foreach(char c in targetWord){
            if(!freq.ContainsKey(c)) freq[c] = 0;
            freq[c]++;
        }

        Color[] colors = new Color[guess.Length];

        for(int i = 0; i < guess.Length; i++){
            if(targetWord[i] == guess[i]){
                colors[i] = Color.green;
                freq[guess[i]]--;
            }
        }

        for(int i = 0; i < guess.Length; i++){
            if(colors[i] == Color.green) continue;
            char c = guess[i];
            if(freq.ContainsKey(c) && freq[c] > 0){
                colors[i] = Color.yellow;
                freq[c]--;
            }
            else colors[i] = Color.gray;
        }

        string colored = "";
        for(int i = 0; i < guess.Length; i++){
            string hex = ColorUtility.ToHtmlStringRGB(colors[i]);
            colored += $"<color=#{hex}>{char.ToUpper(guess[i])}</color>";
        }

        AddText($"[{guesses.Count}.] {colored}");
        AddText(" ");
    }

    void UpdateUsedLetters(string guess){
        foreach(char c in guess.ToUpper()) usedLetters.Add(c);
    }
    #endregion

    #region GACHA LOGIC
    void StartGacha(){
        ClearConsole();
        state = ConsoleState.Gacha;
        AddText("GACHA SIMULATOR", Color.yellow);
        AddText($"Rolling costs {gachaCost} crystals. Type 'roll' to draw or '0' to go back.");
        AddText(" ");
        CreateInputLine();
    }

    void HandleGachaInput(string cmd){
        if(cmd == "0"){
            ClearConsole();
            DisplayMain();
            CreateInputLine();
            state = ConsoleState.MainMenu;
            return;
        }

        if(cmd == "roll"){
            if(crystals < gachaCost){
                AddText("Not enough crystals!", Color.red);
                CreateInputLine();
                return;
            }
            crystals -= gachaCost;

            Cosmetic reward = GetRandomCosmeticWeighted();
            inventory.Add(reward);
            unlockedCosmetics.Add(reward.id);
            SaveUnlockedCosmetics();

            AddText($"You got: {reward.displayName} [{reward.rarity}]", reward.color);
            AddText(" ");
            WaitForEnter();
        }
        else{
            AddText("Unknown input.", Color.red);
            CreateInputLine();
        }
    }

    Cosmetic GetRandomCosmeticWeighted(){
        int total = cosmetics.Sum(c => c.weight);
        if(total <= 0) return cosmetics[0];

        int roll = UnityEngine.Random.Range(0, total);
        int running = 0;
        foreach(var c in cosmetics){
            running += c.weight;
            if(roll < running) return c;
        }

        return cosmetics.Last();
    }
    #endregion

    #region INVENTORY LOGIC
    void ShowInventory(){
        ClearConsole();
        state = ConsoleState.Inventory;
        AddText("INVENTORY:", Color.cyan);
        AddText(" ");

        if(inventory.Count == 0) AddText("You own nothing yet.", Color.gray);
        else foreach(var c in inventory) AddText($"- {c.displayName} [{c.rarity}]", c.color);

        AddText(" ");
        AddText("0. Return");
        CreateInputLine();
    }

    void HandleInventoryInput(string cmd){
        if(cmd == "0"){
            ClearConsole(); 
            DisplayMain(); 
            CreateInputLine(); 
            state = ConsoleState.MainMenu;
        }else{ 
            AddText("Invalid command.", Color.red); 
            CreateInputLine(); 
        }
    }

    void ShowShop(){
        ClearConsole();
        state = ConsoleState.Shop;
        AddText("SHOP (Coming Soon...)", Color.magenta);
        AddText(" ");
        AddText("0. Return");
        CreateInputLine();
    }

    void HandleShopInput(string cmd){
        if(cmd == "0"){
            ClearConsole(); 
            DisplayMain(); 
            CreateInputLine(); 
            state = ConsoleState.MainMenu;
        }
    }
    #endregion

    #region MISC LOGIC
    void WaitForEnter(){
        AddText("Press Enter to continue...", Color.gray);
        state = ConsoleState.WaitingForEnter;
    }

    void ClearConsole(){
        foreach(Transform child in textRoot) Destroy(child.gameObject);
        currentInput = "";
        activeInputText = null;
    }
    void ScrollToTop() => StartCoroutine(ScrollToTopDelay());
    
    IEnumerator ScrollToTopDelay(){
        yield return new WaitForEndOfFrame();
        var scroller = Root.GetComponentInChildren<Scroller>();
        if(scroller == null) yield break;

        scroller.ScrollToIndex(0, true);
    }

    void DestroyInputBox(){
        if(activeInputText == null) return;

        Destroy(activeInputText.transform.parent.gameObject);
        activeInputText = null;
    }

    public void AddText(string message, Color? color = null, bool isHeader = false){
        if(string.IsNullOrEmpty(message) || textRoot == null) return;

        GameObject prefab = isHeader ? headerPrefab : messagePrefab;
        GameObject instance = Instantiate(prefab, textRoot);
        TextBlock textBlock = instance.GetComponentInChildren<TextBlock>();
        textBlock.Text = message;
        textBlock.Color = color ?? defaultColor;
    }
    #endregion

    #region COSMETIC
    void InitializeCosmeticsMasterList(){
        cosmetics = new List<Cosmetic>(){
            new Cosmetic { id = "hat_red", displayName = "Red Hat", weight = 60, rarity = "Common", color = Color.white },
            new Cosmetic { id = "hat_blue", displayName = "Blue Hat", weight = 30, rarity = "Rare", color = Color.cyan },
            new Cosmetic { id = "halo_gold", displayName = "Golden Halo", weight = 10, rarity = "Legendary", color = new Color(1f, 0.84f, 0f) }
        };

        LoadUnlockedCosmetics();
        inventory.Clear();
        foreach(var c in cosmetics){
            if(unlockedCosmetics.Contains(c.id)) inventory.Add(c);
        }
    }

    void SaveUnlockedCosmetics(){
        PlayerPrefs.SetString("cosmetics_unlocked", string.Join(",", unlockedCosmetics));
        PlayerPrefs.Save();
    }

    void LoadUnlockedCosmetics(){
        unlockedCosmetics.Clear();
        string raw = PlayerPrefs.GetString("cosmetics_unlocked", "");
        if(string.IsNullOrEmpty(raw)) return;

        string[] split = raw.Split(',');
        foreach(var id in split) if(!string.IsNullOrEmpty(id)) unlockedCosmetics.Add(id);
    }

    private class Cosmetic  {
        public string id;
        public string displayName;
        public int weight;
        public string rarity;
        public Color color;
    }
    #endregion
}
