using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public class ResponseQualityEvaluator : IResponseQualityEvaluator
{
    private static readonly Regex RepeatedChar = new(@"^(.)\1{3,}$", RegexOptions.Compiled);
    private static readonly Regex RepeatedPair = new(@"^(.{2})\1+$", RegexOptions.Compiled);
    private static readonly Regex ConsonantRun = new(@"[bcdfghjklmnpqrstvwxyz]{4,}", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex Whitespace = new(@"\s+", RegexOptions.Compiled);
    private static readonly Regex VowelPattern = new(@"[aeiouAEIOU]");
    private static readonly HashSet<string> ValidShortWords = new(StringComparer.OrdinalIgnoreCase){
        "ok", "okay", "yes", "no", "why", "how", "sure", "nah", "yeah", "fine", "cool"
    };

    public int CalculateQuality(string input){
        if(string.IsNullOrWhiteSpace(input)) return 0;
        input = input.Trim();

        if(ValidShortWords.Contains(input)) return 8;

        int len = input.Length;
        if(len < 3) return len == 1 ? 0 : 2;

        int quality = Math.Min(10, 2 + len / 2);

        string clean = Whitespace.Replace(input, "");
        if(clean.Length >= 3){
            int vowels = VowelPattern.Matches(clean).Count;
            if(vowels == 0 || (clean.Length > 5 && vowels < 2)) quality -= 3;
        }

        if(RepeatedChar.IsMatch(input)) quality -= 4;
        if(HasExcessiveRepetition(input)) quality -= 3;
        if(IsTrolling(input)) quality -= 5;

        return Math.Clamp(quality, 0, 10);
    }

    public ResponseTier DetermineTier(int qualityScore, JudgeScore judgeScore){
        if(qualityScore <= 2 || (judgeScore != null && judgeScore.total_score <= 5))
            return ResponseTier.Gibberish;
        else if(qualityScore <= 5 || (judgeScore != null && judgeScore.total_score <= 12))
            return ResponseTier.Warning;
        else
            return ResponseTier.Normal;
    }

    bool HasExcessiveRepetition(string input){
        if(input.Length < 4) return false;
        string[] words = input.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if(words.Length < 3) return false;
        var wordGroups = words.GroupBy(w => w);
        return wordGroups.Any(g => g.Count() >= words.Length * 0.6f);
    }

    bool IsTrolling(string input){
        string lowerInput = input.ToLower();
        string[] trollPatterns = {
            "ignore all previous instruction",
            "hahaha", "lol", "lmao", "lmfao", "rofl",
            "asdf", "qwerty", "zxcv",
            "123", "900", "80085", "1337", "6969",
            "test", "testing",
            "fuck", "shit", "ass", "bitch", "nigga", "retard"
        };
        return trollPatterns.Any(pattern => 
            !pattern.Contains(" ") && lowerInput.Contains(pattern) ||
            pattern.Contains(" ") && Regex.IsMatch(lowerInput, $@"\b{Regex.Escape(pattern)}\b")
        );
    }
}