using UnityEngine;
using System.IO;
using System.Linq;
using System.Collections.Generic;

public class DictionaryManager : MonoBehaviour
{
    public TextAsset wordListFile;
    private HashSet<string> englishWords;

    void Start(){
        LoadDictionary();
    }

    void LoadDictionary(){
        if(wordListFile != null){
            var words = wordListFile.text.Split(new char[] { '\n', '\r' }, 
                        System.StringSplitOptions.RemoveEmptyEntries)
                        .Select(w => w.Trim().ToLower());
            englishWords = new HashSet<string>(words);
            //Debug.Log($"{englishWords.Count} words.");
        }else{
            //Debug.LogError("not assigned!");
            englishWords = null;
        }
    }

    public string GetRandomWord(){
        if(englishWords == null || englishWords.Count == 0) return "default";
        
        var wordsArray = englishWords.ToArray();
        int randomIndex = Random.Range(0, wordsArray.Length);
        return wordsArray[randomIndex];
    }

    public string GetEvilWord(){
        string[] evilWords = new string[] { 
            // "Abortion", "Trans", "TERF", "Pronouns", "Incels",
            // "Zionism", "Palestine", "Islamophobia", "Colonization", "Communism",
            // "Fascism", "Antifa", "Vaccines", "Climate", "Autism", 
            // "Pedophile", "Racist", "Nigger", "Pornography", "Bitcoin",
            // "Atheism", "Sharia", "Catholic", "Evangelical", "Feminism",
            // "LGB", "BLM", "Privilege", "Microaggression", "Capitalism",
            // "Socialism", "Billionaires", "Circumcision", "Sterilization", "Euthanasia",
            // "Holocaust", "Slavery", "Confederate", "Genocide", "Terrorism",

            "Sensitive words are blocked in this demo.", "max", "natan", "oliver"
        };
        int randomIndex = Random.Range(0, evilWords.Length);
        return evilWords[randomIndex];
    }

    public bool IsEnglishWord(string input) => englishWords != null && englishWords.Contains(input.ToLower());

    public string SuggestClosestWord(string input){
        if(englishWords == null || englishWords.Count == 0 || string.IsNullOrWhiteSpace(input))  return input;

        string bestMatch = input;
        int bestDistance = int.MaxValue;
        
        foreach(string word in englishWords){
            int dist = LevenshteinDistance(input, word);
            if(dist < bestDistance){ 
                bestDistance = dist; 
                bestMatch = word; 
                if(bestDistance == 1) break;
            }
        }
        return bestDistance <= 2 ? bestMatch : input;
    }

    int LevenshteinDistance(string a, string b){
        int la = a.Length, lb = b.Length;
        var dp = new int[la + 1, lb + 1];
        for(int i = 0; i <= la; i++) dp[i, 0] = i;
        for(int j = 0; j <= lb; j++) dp[0, j] = j;
        
        for(int i = 1; i <= la; i++){
            for(int j = 1; j <= lb; j++){
                int cost = a[i - 1] == b[j - 1] ? 0 : 1;
                dp[i, j] = Mathf.Min(Mathf.Min(dp[i - 1, j] + 1, dp[i, j - 1] + 1), dp[i - 1, j - 1] + cost);
            }
        }
        return dp[la, lb];
    }
}