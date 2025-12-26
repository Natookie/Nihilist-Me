using System;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using System.Collections.Generic;

public static class DebateJsonParser
{
    public static string ExtractJson(string raw){
        if(string.IsNullOrEmpty(raw)){
            Debug.LogWarning("ExtractJson: Raw input is null or empty");
            return null;
        }


        raw = SanitizeModelOutput(raw);
        raw = raw.Replace("```json", "").Replace("```", "").Trim();
        
        if(raw.Trim().StartsWith("{") && raw.Trim().EndsWith("}")){

            return SanitizeJson(raw);
        }
        
        if(ContainsAllJudgeFields(raw)){

            string wrappedJson = "{" + raw + "}";
            return SanitizeJson(wrappedJson);
        }
        
        try{
            MatchCollection matches = Regex.Matches(raw, @"\{[^{}]*(?:\{[^{}]*\}[^{}]*)*\}");
            if(matches.Count > 0){
                string json = matches.OrderByDescending(m => m.Value.Length).First().Value.Trim();
    
                return SanitizeJson(json);
            }
        }
        catch(System.Exception e){
            Debug.LogWarning($"ExtractJson complex regex failed: {e.Message}");
        }
        
        try{
            MatchCollection matches = Regex.Matches(raw, @"\{[\s\S]*?\}");
            if(matches.Count > 0){
                string json = matches.OrderByDescending(m => m.Value.Length).First().Value.Trim();
    
                return SanitizeJson(json);
            }
        }
        catch(System.Exception e){
            Debug.LogWarning($"ExtractJson simple regex failed: {e.Message}");
        }
        
        try{
            if(raw.Contains("\"response\"") && raw.Contains("\"model\"")){
                var responseMatch = Regex.Match(raw, @"""response""\s*:\s*""([\s\S]*?)""\s*[,}]");
                if(responseMatch.Success){
                    string innerJson = responseMatch.Groups[1].Value;
                    innerJson = innerJson.Replace("\\n", "\n").Replace("\\\"", "\"").Replace("\\\\", "\\");
        
                    
                    if(innerJson.Trim().StartsWith("{") && innerJson.Trim().EndsWith("}")){
                        return SanitizeJson(innerJson);
                    }
                }
            }
        }
        catch(System.Exception e){
            Debug.LogWarning($"ExtractJson OLLAMA response parsing failed: {e.Message}");
        }
        
        string constructed = ConstructJsonFromKeyValuePairs(raw);
        if(!string.IsNullOrEmpty(constructed)){
            return constructed;
        }
        
        Debug.LogWarning($"ExtractJson: Failed to extract any JSON from: {raw.Substring(0, Math.Min(200, raw.Length))}");
        return null;
    }

    private static bool ContainsAllJudgeFields(string text){
        if(string.IsNullOrEmpty(text)) return false;
        
        string[] requiredFields = { "fallacy_score", "logic_score", "insult_score", "total_score", "feedback" };
        
        foreach(string field in requiredFields){
            bool hasField = text.Contains($"\"{field}\"") || text.Contains($"{field}:");
            if(!hasField){
                return false;
            }
        }
        
        return true;
    }

    private static string ConstructJsonFromKeyValuePairs(string raw){
        
        try{
            raw = raw.Replace("\\\"", "\"");
            
            string pattern = @"""([^""]+)""\s*:\s*(""(?:(?:[^""\\]|\\.)*)""|[^,}\s]+)";
            
            var matches = Regex.Matches(raw, pattern, RegexOptions.Singleline);
            if(matches.Count == 0){
    
                return null;
            }
            
            List<string> jsonPairs = new List<string>();
            
            foreach(Match match in matches){
                string key = match.Groups[1].Value;
                string value = match.Groups[2].Value;
                
                if(value.StartsWith("\"") && value.EndsWith("\"")){
                    string innerValue = value.Substring(1, value.Length - 2);
                    value = $"\"{innerValue.Replace("\"", "\\\"").Replace("\\", "\\\\")}\"";
                }
                
                jsonPairs.Add($"\"{key}\": {value}");
                if(jsonPairs.Count >= 5) break;
            }
            
            if(jsonPairs.Count > 0){
                string json = "{\n    " + string.Join(",\n    ", jsonPairs) + "\n}";
                return SanitizeJson(json);
            }
        }
        catch{ }
        
        return null;
    }

    public static string SanitizeJson(string json){
        if(string.IsNullOrEmpty(json)) return json;

        json = json.Replace("“", "\"").Replace("”", "\"").Replace("„", "\"").Replace("‟", "\"");
        json = Regex.Replace(json, @"[\uD800-\uDBFF][\uDC00-\uDFFF]", "");

        json = Regex.Replace(json, ",\\s*([}\\]])", "$1");
        int last = json.LastIndexOf('}');
        if(last >= 0 && last < json.Length - 1) json = json.Substring(0, last + 1);

        json = FixUnescapedQuotes(json);

        return json;
    }

    public static string ExtractFeedback(string raw){
        if(string.IsNullOrEmpty(raw)) return null;

        Match m = Regex.Match(raw, "FEEDBACK:\\s*(.+)", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        if(m.Success) return m.Groups[1].Value.Trim();
        
        m = Regex.Match(raw, @"""feedback""\s*:\s*""([^""]+)""", RegexOptions.IgnoreCase);
        if(m.Success) return m.Groups[1].Value.Trim();
        
        m = Regex.Match(raw, @"""feedback""\s*:\s*""((?:[^""\\]|\\.)*)""", RegexOptions.IgnoreCase);
        if(m.Success) return m.Groups[1].Value.Replace("\\\"", "\"").Trim();
        
        return null;
    }

    public static string SanitizeModelOutput(string s){
        if(string.IsNullOrEmpty(s)) return s;

        s = s.Replace("\r\n", "\n");
        s = s.Replace("```json", "").Replace("```", "");
        
        s = s.Replace("\\,", ",");
        
        s = Regex.Replace(s, "[\x00-\x1F\x7F]+", " ");
        
        s = Regex.Replace(s, @"[\uD83C-\uDBFF\uDC00-\uDFFF]+", "");
        s = Regex.Replace(s, "[\u2600-\u26FF\u2700-\u27BF]", "");

        return s.Trim();
    }

    public static bool TryParseJson<T>(string json, out T obj) where T : class{
        obj = null;
        if(string.IsNullOrEmpty(json)) return false;

        string cleaned = SanitizeJson(json);

        try{
            obj = JsonUtility.FromJson<T>(cleaned);
            if(obj != null) return true;
        }
        catch { }

        try{
            obj = JsonUtility.FromJson<T>(QuickRepairJson(cleaned));
            return obj != null;
        }
        catch{
            return false;
        }
    }

    public static string QuickRepairJson(string s){
        if(string.IsNullOrEmpty(s)) return s;

        s = Regex.Replace(s, @",\s*}", "}");
        s = Regex.Replace(s, @",\s*\]", "]");
        
        if(!s.Trim().StartsWith("{")) s = "{" + s;
        if(!s.Trim().EndsWith("}")) s += "}";
        
        s = FixUnescapedQuotes(s);
        
        s = s.Replace("\\,", ",");
        
        return s;
    }

    private static string FixUnescapedQuotes(string json){
        var feedbackMatch = Regex.Match(json, @"""feedback""\s*:\s*""([^""]+)""", RegexOptions.IgnoreCase);
        if(feedbackMatch.Success){
            string feedback = feedbackMatch.Groups[1].Value;
            if(feedback.Contains("\"") && !feedback.Contains("\\\"")){
                string escapedFeedback = feedback.Replace("\"", "\\\"");
                json = json.Replace(feedback, escapedFeedback);
            }
        }
        
        return json;
    }
}