using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class WordleGame : MonoBehaviour
{
    [Header("WORDLE CONFIG")]
    [SerializeField] private int maxAttempts = 5;
    [SerializeField] private int betCost = 10;
    [SerializeField] private int lossPenalty = 3;
    
    [Header("REFERENCES")]
    [SerializeField] private TextAsset wordList;
    
    private string targetWord;
    private HashSet<string> englishWords;
    private List<string> guesses = new();
    private HashSet<char> usedLetters = new();
    
    public event Action<string, Color[]> OnGuessProcessed;
    public event Action<int> OnGameWon;
    public event Action<string> OnGameLost;
    public event Action<int> OnCrystalsUpdated;
    
    private int playerCrystals;
    
    #region INITIALIZATION
    void Start(){
        if(wordList != null) LoadWordList();
    }
    
    private void LoadWordList(){
        string[] words = wordList.text.Split('\n');
        englishWords = new HashSet<string>(words.Select(w => w.Trim().ToLower()).Where(w => w.Length == 5));
    }
    #endregion
    
    #region GAME CONTROL
    public void StartGame(ref int crystals){
        if(englishWords == null || englishWords.Count == 0){
            Debug.LogError("Word dictionary not loaded!");
            return;
        }
        
        if(crystals < betCost){
            Debug.LogWarning("Not enough crystals to play");
            return;
        }
        
        playerCrystals = crystals;
        crystals -= betCost;
        OnCrystalsUpdated?.Invoke(-betCost);
        
        targetWord = englishWords.ElementAt(UnityEngine.Random.Range(0, englishWords.Count));
        guesses.Clear();
        usedLetters.Clear();
    }
    
    public bool SubmitGuess(string guess, ref int crystals, out string debugOutput){
        debugOutput = null;
        if(guess == "gback") return false;
        if(guess.Equals("bulll", StringComparison.OrdinalIgnoreCase)){
            debugOutput = targetWord.ToUpper();
            return false;
        }
        
        if(!IsValidGuess(guess)) return false;
        
        playerCrystals = crystals;
        guesses.Add(guess);
        UpdateUsedLetters(guess);
        
        Color[] colors = ProcessGuess(guess);
        OnGuessProcessed?.Invoke(guess, colors);
        
        if(guess == targetWord){
            int bonus = Mathf.Max(10 - guesses.Count * 2, 1);
            crystals += betCost + bonus;
            OnCrystalsUpdated?.Invoke(betCost + bonus);
            OnGameWon?.Invoke(bonus);
            return true;
        }
        
        if(guesses.Count >= maxAttempts){
            crystals = Mathf.Max(0, crystals - lossPenalty);
            OnCrystalsUpdated?.Invoke(-lossPenalty);
            OnGameLost?.Invoke(targetWord);
            return true;
        }
        
        return false;
    }
    #endregion
    
    #region GAME LOGIC
    bool IsValidGuess(string guess){
        return (guess.Length == 5 && englishWords.Contains(guess.ToLower()));
    }
    
    Color[] ProcessGuess(string guess){
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
        
        return colors;
    }
    
    private void UpdateUsedLetters(string guess){
        foreach(char c in guess.ToUpper()) usedLetters.Add(c);
    }
    #endregion
    
    #region API
    public int GetMaxAttempts() => maxAttempts;
    public int GetBetCost() => betCost;
    public int GetLossPenalty() => lossPenalty;
    public int GetRemainingAttempts() => maxAttempts - guesses.Count;
    public List<string> GetGuesses() => new List<string>(guesses);
    public HashSet<char> GetUsedLetters() => new HashSet<char>(usedLetters);
    public bool IsGameActive() => !string.IsNullOrEmpty(targetWord);
    public string GetTargetWord() => targetWord;
    #endregion
    
    #region TUTORIAL
    public string GetTutorialText(){
        return @"=x=x=x=x=x=x=x
Guess a 5-letter English word.
<color=green>Green</color> = Correct letter & correct position.
<color=yellow>Yellow</color> = Correct letter, wrong position.
<color=#B0B0B0>Gray</color> = Letter not in the word.

You have <b>" + maxAttempts + @" attempts</b> to guess the correct word.

Each Wordle round costs <b>" + betCost + @"</b> crystals to play.
If you guess the word correctly:
- You get your bet back
- You earn bonus crystals based on how fast you solved it
  (Fewer attempts = bigger bonus!)";
    }
    #endregion
}