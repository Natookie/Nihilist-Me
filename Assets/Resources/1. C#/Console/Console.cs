using Nova;
using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Console : MonoBehaviour
{
    [Header("SYSTEM REFERENCES")]
    [SerializeField] private WordleGame wordleGame;
    [SerializeField] private CosmeticManager cosmeticManager;
    [SerializeField] private ConsoleUIHelper uiHelper;

    [Header("UI REFERENCES")]
    [SerializeField] private UIBlock2D root;
    [SerializeField] private Transform textRoot;

    private int crystals = 200;
    private string currentInput = "";
    private bool isOnFocus;
    public bool IsOnFocus() => isOnFocus;
    private bool returningFromTutorial = false;
    private ConsoleState state = ConsoleState.MainMenu;

    bool isOnHover;

    enum ConsoleState { MainMenu, GameMenu, WaitingForEnter, WordleGame, Gacha, Inventory, Shop, WordleEnd }

    void Start(){        
        InitializeWordle();
        InitializeCosmeticManager();
        InitializeUI();
        
        root ??= GetComponentInParent<UIBlock2D>();
        //root.AddGestureHandler<Gesture.OnPress>(OnClick);
        root.AddGestureHandler<Gesture.OnHover>(OnHover);
        root.AddGestureHandler<Gesture.OnUnhover>(OnUnhover);

        DisplayMain();
        uiHelper.CreateInputLine();
    }

    void OnDestroy(){
        if(wordleGame != null){
            wordleGame.OnGuessProcessed -= HandleWordleGuess;
            wordleGame.OnGameWon -= HandleWordleWin;
            wordleGame.OnGameLost -= HandleWordleLoss;
            wordleGame.OnCrystalsUpdated -= UpdateCrystals;
        }
    }

    void Update(){
        if(Input.GetMouseButtonDown(0) && root.gameObject.activeSelf){
            if(isOnHover){isOnFocus = true; root.Border.Enabled = true;}
            else {isOnFocus = false; root.Border.Enabled = false;}
        }
        if(!isOnFocus) return;

        if(state == ConsoleState.WaitingForEnter && !uiHelper.HasActiveInput()){
            if(Input.GetKeyDown(KeyCode.Return)){
                uiHelper.ClearConsole();
                if(returningFromTutorial){
                    returningFromTutorial = false;
                    ShowGameMenu();
                }else{
                    DisplayMain();
                    uiHelper.CreateInputLine();
                    state = ConsoleState.MainMenu;
                }
            }
            return;
        }

        if(!uiHelper.HasActiveInput()) return;
        
        foreach(char c in Input.inputString){
            if(c == '\b'){
                if(currentInput.Length > 0) currentInput = currentInput[..^1];
            }else if(c == '\n' || c == '\r'){
                SubmitCommand(currentInput.Trim().ToLower());
                return;
            }else if(state == ConsoleState.WordleGame || state == ConsoleState.WordleEnd){
                if(char.IsLetter(c) && currentInput.Length < 5) currentInput += char.ToLower(c);
            }else currentInput += c;
        }

        if(state == ConsoleState.WordleGame) UpdateWordleInputDisplay();
        else uiHelper.UpdateInputText(currentInput);
    }

    #region INITIALIZATION
    void InitializeWordle(){
        wordleGame = GetComponent<WordleGame>();
        if(wordleGame == null) wordleGame = gameObject.AddComponent<WordleGame>();

        wordleGame.OnGuessProcessed += HandleWordleGuess;
        wordleGame.OnGameWon += HandleWordleWin;
        wordleGame.OnGameLost += HandleWordleLoss;
        wordleGame.OnCrystalsUpdated += UpdateCrystals;
    }

    void InitializeCosmeticManager(){
        cosmeticManager = GetComponent<CosmeticManager>();
        if(cosmeticManager == null) cosmeticManager = gameObject.AddComponent<CosmeticManager>();
    }

    void InitializeUI(){
        if(uiHelper == null) uiHelper = GetComponent<ConsoleUIHelper>();
        if(uiHelper == null) uiHelper = gameObject.AddComponent<ConsoleUIHelper>();
        uiHelper.Initialize(textRoot, root);
    }
    #endregion

    #region WORDLE LOGIC
    void HandleWordleGuess(string guess, Color[] colors){
        string colored = "";
        for(int i = 0; i < guess.Length; i++){
            string hex = ColorUtility.ToHtmlStringRGB(colors[i]);
            colored += $"<color=#{hex}>{char.ToUpper(guess[i])}</color>";
        }
        
        uiHelper.AddText($"[{wordleGame.GetGuesses().Count}.] {colored}");
        uiHelper.AddText(" ");
    }

    void HandleWordleWin(int bonus){
        StartCoroutine(FlashResult(Color.green, $"Correct! Returned {wordleGame.GetBetCost()} + bonus {bonus} = +{(wordleGame.GetBetCost() + bonus)} Crystals."));
        state = ConsoleState.WordleEnd;
    }

    void HandleWordleLoss(string targetWord){
        StartCoroutine(FlashResult(Color.red, $"Failed! The word was: {targetWord}\nLost {wordleGame.GetLossPenalty()} crystals."));
        state = ConsoleState.WordleEnd;
    }

    void UpdateCrystals(int amount) => crystals += amount;
    #endregion

    #region INPUT HANDLING
    void OnHover(Gesture.OnHover evt) => isOnHover = true;
    void OnUnhover(Gesture.OnUnhover evt) => isOnHover = false;

    void SubmitCommand(string cmd){
        if(string.IsNullOrEmpty(cmd)) return;

        string inputCopy = cmd;
        currentInput = "";

        switch(state){
            case ConsoleState.MainMenu:
                uiHelper.DestroyInputBox();
                HandleMainCommand(inputCopy);
                break;

            case ConsoleState.GameMenu:
                uiHelper.DestroyInputBox();
                HandleGameMenuInput(inputCopy);
                break;

            case ConsoleState.Gacha:
                uiHelper.DestroyInputBox();
                HandleGachaInput(inputCopy);
                break;

            case ConsoleState.Inventory:
                uiHelper.DestroyInputBox();
                HandleInventoryInput(inputCopy);
                break;

            case ConsoleState.Shop:
                uiHelper.DestroyInputBox();
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
    #endregion

    #region MAIN MENU
    void DisplayMain(){
        uiHelper.AddText("=========================================================================================", Color.cyan, true);
        uiHelper.AddText("░█▀█░█▀▀░█░█░█▀█░█▀█░░░█░█░█▀▀░█▀▄░█▀█░█▀▀░█░░░", Color.cyan, true);
        uiHelper.AddText("░█░█░█▀▀░▄▀▄░█░█░█░█░░░█▀▄░█▀▀░█▀▄░█░█░█▀▀░█░░░", Color.cyan, true);
        uiHelper.AddText("░▀░▀░▀▀▀░▀░▀░▀▀▀░▀░▀░░░▀░▀░▀▀▀░▀░▀░▀░▀░▀▀▀░▀▀▀░", Color.cyan, true);
        uiHelper.AddText("=========================================================================================", Color.cyan, true);
        uiHelper.AddText("Welcome back, Beeze Cooda!", Color.green);
        uiHelper.AddText("Crystals: " + crystals, Color.yellow);
        uiHelper.AddText(" ");
        uiHelper.AddText("1. Play Game");
        uiHelper.AddText("2. Gacha");
        uiHelper.AddText("3. See Inventory");
        uiHelper.AddText("4. Visit Shop");
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
                uiHelper.AddText("Unknown command.", Color.red);
                WaitForEnter();
                break;
        }
    }
    #endregion

    #region GAME MENU
    void ShowGameMenu(){
        uiHelper.ClearConsole();
        state = ConsoleState.GameMenu;

        uiHelper.AddText("WORDLE MENU", Color.yellow);
        uiHelper.AddText("1. Play");
        uiHelper.AddText("2. Tutorial");
        uiHelper.AddText("3. Go Back");

        uiHelper.CreateInputLine();
    }

    void HandleGameMenuInput(string cmd){
        switch(cmd){
            case "1":
            case "play":
                if(crystals < wordleGame.GetBetCost())
                {
                    uiHelper.AddText("Not enough crystals to play Wordle.", Color.red);
                    uiHelper.AddText($"You need {wordleGame.GetBetCost()} crystals to start a round.", Color.yellow);
                    uiHelper.CreateInputLine();
                    return;
                }
                uiHelper.DestroyInputBox();
                StartWordle();
                break;

            case "2":
            case "tutorial":
                ShowWordleTutorial();
                break;

            case "3":
            case "0":
            case "back":
                uiHelper.ClearConsole();
                DisplayMain();
                uiHelper.CreateInputLine();
                state = ConsoleState.MainMenu;
                break;

            default:
                uiHelper.AddText("Unknown command.", Color.red);
                uiHelper.CreateInputLine();
                break;
        }
    }

    void ShowWordleTutorial(){
        uiHelper.ClearConsole();
        uiHelper.AddText("WORDLE TUTORIAL", Color.cyan);
        uiHelper.AddText(wordleGame.GetTutorialText());
        uiHelper.AddText(" ");
        uiHelper.AddText("Press Enter to return.", Color.white);

        returningFromTutorial = true;
        state = ConsoleState.WaitingForEnter;
    }
    #endregion

    #region WORDLE GAMEPLAY
    void StartWordle(){
        uiHelper.ClearConsole();
        state = ConsoleState.WordleGame;
        wordleGame.StartGame(ref crystals);

        uiHelper.AddText("Guess the 5-letter word", Color.yellow);
        uiHelper.AddText("<color=green>Correct</color> | <color=yellow>Misplaced</color> | <color=#B0B0B0>Wrong</color>");
        uiHelper.AddText(" ");
        uiHelper.AddText($"Crystals: <color=yellow>{crystals}</color> | <color=white>'gback' to go back</color>");
        uiHelper.AddText("====================================");
        uiHelper.AddText(" ");

        CreateWordleInput();
    }

    void HandleWordleInput(string input){
        if(input == "gback"){
            uiHelper.ClearConsole();
            ShowGameMenu();
            return;
        }

        if(input.Equals("bulll", StringComparison.OrdinalIgnoreCase)){
            string targetWord = wordleGame.GetTargetWord();
            if(!string.IsNullOrEmpty(targetWord)) uiHelper.AddText($"DEBUG: Target word = {targetWord.ToUpper()}", Color.magenta);
            else uiHelper.AddText("DEBUG: No active Wordle game", Color.red);

            uiHelper.AddText(" ");
            CreateWordleInput();
            return;
        }

        string debugOutput;
        bool gameEnded = wordleGame.SubmitGuess(input, ref crystals, out debugOutput);
        
        if(!string.IsNullOrEmpty(debugOutput)){
            uiHelper.AddText(debugOutput, Color.magenta);
        
            if(input == "bulll"){
                uiHelper.AddText(" ");
                CreateWordleInput();
                return;
            }
        }

        if(gameEnded){
            uiHelper.AddText("Play again? (Y/N)", Color.gray);
            uiHelper.CreateInputLine();
        }else CreateWordleInput();
    }

    void HandleWordleEndInput(string cmd){
        if(cmd == "y" || cmd == "yes"){
            uiHelper.DestroyInputBox();
            if(crystals < wordleGame.GetBetCost()){
                uiHelper.AddText("Not enough crystals to play another round.", Color.red);
                WaitForEnter();
                return;
            }

            crystals -= wordleGame.GetBetCost();
            StartWordle();
        }else if(cmd == "n" || cmd == "no" || cmd == "0"){
            uiHelper.DestroyInputBox();
            uiHelper.ClearConsole();
            DisplayMain();
            uiHelper.CreateInputLine();
            state = ConsoleState.MainMenu;
        }else{
            uiHelper.AddText("Please type Y or N.", Color.red);
            uiHelper.CreateInputLine();
        }
    }

    void CreateWordleInput(){
        uiHelper.CreateInputLine();
        currentInput = "";
        UpdateWordleInputDisplay();
    }

    void UpdateWordleInputDisplay(){
        if(!uiHelper.HasActiveInput()) return;

        string display = ">> ";
        var usedLetters = wordleGame.GetUsedLetters();
        foreach(char c in currentInput.ToUpper()){
            if(usedLetters.Contains(c)) display += $"<color=#B0B0B0>{c}</color> ";
            else display += $"<color=white>{c}</color> ";
        }

        for(int i = currentInput.Length; i < 5; i++) display += "_ ";

        uiHelper.UpdateInputText(display.TrimEnd());
    }
    #endregion

    #region GACHA SYSTEM
    void StartGacha(){
        uiHelper.ClearConsole();
        state = ConsoleState.Gacha;
        uiHelper.AddText("GACHA SIMULATOR", Color.yellow);
        uiHelper.AddText($"Rolling costs {cosmeticManager.GetGachaCost()} crystals. Type 'roll' to draw or '0' to go back.");
        uiHelper.AddText(" ");
        uiHelper.CreateInputLine();
    }

    void HandleGachaInput(string cmd){
        if(cmd == "0"){
            uiHelper.ClearConsole();
            DisplayMain();
            uiHelper.CreateInputLine();
            state = ConsoleState.MainMenu;
            return;
        }

        if(cmd == "roll"){
            if(crystals < cosmeticManager.GetGachaCost()){
                uiHelper.AddText("Not enough crystals!", Color.red);
                uiHelper.CreateInputLine();
                return;
            }
            
            crystals -= cosmeticManager.GetGachaCost();
            CosmeticManager.Cosmetic reward = cosmeticManager.RollGacha();
            cosmeticManager.UnlockCosmetic(reward);

            uiHelper.AddText($"You got: {reward.displayName} [{reward.rarity}]", reward.color);
            uiHelper.AddText(" ");
            WaitForEnter();
        }else{
            uiHelper.AddText("Unknown input.", Color.red);
            uiHelper.CreateInputLine();
        }
    }
    #endregion

    #region INVENTORY & SHOP
    void ShowInventory(){
        uiHelper.ClearConsole();
        state = ConsoleState.Inventory;
        uiHelper.AddText("INVENTORY:", Color.cyan);
        uiHelper.AddText(" ");

        List<CosmeticManager.Cosmetic> inventory = cosmeticManager.GetInventory();
        if(inventory.Count == 0) uiHelper.AddText("You own nothing yet.", Color.gray);
        else foreach(var c in inventory) uiHelper.AddText($"- {c.displayName} [{c.rarity}]", c.color);

        uiHelper.AddText(" ");
        uiHelper.AddText("0. Return");
        uiHelper.CreateInputLine();
    }

    void HandleInventoryInput(string cmd){
        if(cmd == "0"){
            uiHelper.ClearConsole(); 
            DisplayMain(); 
            uiHelper.CreateInputLine(); 
            state = ConsoleState.MainMenu;
        }else{
            uiHelper.AddText("Invalid command.", Color.red); 
            uiHelper.CreateInputLine(); 
        }
    }

    void ShowShop(){
        uiHelper.ClearConsole();
        state = ConsoleState.Shop;
        uiHelper.AddText("SHOP (Coming Soon...)", Color.magenta);
        uiHelper.AddText(" ");
        uiHelper.AddText("0. Return");
        uiHelper.CreateInputLine();
    }

    void HandleShopInput(string cmd){
        if(cmd == "0"){
            uiHelper.ClearConsole(); 
            DisplayMain(); 
            uiHelper.CreateInputLine(); 
            state = ConsoleState.MainMenu;
        }
    }
    #endregion

    #region UTILITIES
    void WaitForEnter(){
        uiHelper.AddText("Press Enter to continue...", Color.gray);
        state = ConsoleState.WaitingForEnter;
    }

    IEnumerator FlashResult(Color color, string message){
        uiHelper.AddText(message, color);
        yield return new WaitForSeconds(0.5f);
    }
    #endregion
}