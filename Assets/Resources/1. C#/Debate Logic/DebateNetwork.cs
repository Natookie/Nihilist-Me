using System;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Text.RegularExpressions;

public class DebateNetwork : MonoBehaviour
{
    [Serializable]
    public class OllamaRequest
    {
        public string model;
        public string prompt;
        public bool stream;
    }

    [Serializable]
    private class OllamaResponseWrapper
    {
        public string model;
        public string created_at;
        public string response;
        public bool done;
        public object[] context;
        public long total_duration;
        public long load_duration;
        public int prompt_eval_count;
        public long prompt_eval_duration;
        public int eval_count;
        public long eval_duration;
    }

    public IEnumerator SendToOllama(
        string url,
        string model,
        float timeoutSeconds,
        string prompt,
        Action<string> onComplete)
    {
        var reqData = new OllamaRequest
        {
            model = model,
            prompt = prompt,
            stream = false
        };

        string jsonBody = JsonUtility.ToJson(reqData);

        using (UnityWebRequest req = new UnityWebRequest(url, "POST")){
            req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonBody));
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");

            var operation = req.SendWebRequest();
            float startTime = Time.time;

            while(!operation.isDone){
                if(Time.time - startTime > timeoutSeconds){
                    req.Abort();
                    onComplete?.Invoke("[Network Timeout]");
                    yield break;
                }
                yield return null;
            }

            #if UNITY_2020_1_OR_NEWER
            bool success = req.result == UnityWebRequest.Result.Success;
            #else
            bool success = !req.isNetworkError && !req.isHttpError;
            #endif

            if(success){
                string rawText = req.downloadHandler.text;
                string extractedResponse = ExtractOllamaResponse(rawText);
                onComplete?.Invoke(extractedResponse);
            }else onComplete?.Invoke("[Network Error] " + req.error);
        }
    }

    private string ExtractOllamaResponse(string rawJson){
        if(string.IsNullOrEmpty(rawJson))  return rawJson;

        try{
            var wrapper = JsonUtility.FromJson<OllamaResponseWrapper>(rawJson);
            if(!string.IsNullOrEmpty(wrapper.response)){
                //Debug.Log($"[Ollama] Successfully extracted response: {wrapper.response.Substring(0, Math.Min(50, wrapper.response.Length))}...");
                return wrapper.response;
            }
        }
        catch(Exception e){
            Debug.LogWarning($"[Ollama] JSON parsing failed: {e.Message}");
        }

        try{
            var match = Regex.Match(rawJson, @"""response""\s*:\s*""([\s\S]*?)""\s*[,}]");
            if(match.Success && match.Groups.Count > 1){
                string response = match.Groups[1].Value;
                response = response.Replace("\\n", "\n").Replace("\\\"", "\"").Replace("\\\\", "\\");
                //Debug.Log($"[Ollama] Regex extracted response: {response.Substring(0, Math.Min(50, response.Length))}...");
                return response;
            }
        }
        catch(Exception e){
            Debug.LogError($"[Ollama] Regex extraction failed: {e.Message}");
        }

        Debug.LogWarning($"[Ollama] Failed to extract response, returning raw: {rawJson}");
        return rawJson;
    }
}