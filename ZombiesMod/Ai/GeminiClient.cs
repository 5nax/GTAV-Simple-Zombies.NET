using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using ZombiesMod.Static;

namespace ZombiesMod.Ai;

// Minimal async client for Google Gemini's generateContent REST endpoint. Runs entirely
// off the game thread (callers await it on a background Task) so the game never blocks on
// the network. JSON is built/parsed with the framework's JavaScriptSerializer — no extra
// dependency. The API key + model come from the INI ([ai] section).
public static class GeminiClient
{
	private static readonly HttpClient Http = new HttpClient { Timeout = TimeSpan.FromSeconds(20.0) };

	private static readonly JavaScriptSerializer Json = new JavaScriptSerializer();

	public static bool IsConfigured =>
		GameConfig.AiEnabled && !string.IsNullOrWhiteSpace(GameConfig.GeminiApiKey);

	// Returns the model's text response (the model is asked to reply as JSON), or null on
	// any failure. Never throws to the caller.
	public static async Task<string> GenerateAsync(string systemPrompt, string userPrompt)
	{
		if (!IsConfigured)
		{
			return null;
		}
		try
		{
			string url = $"https://generativelanguage.googleapis.com/v1beta/models/{GameConfig.GeminiModel}:generateContent?key={GameConfig.GeminiApiKey}";

			var body = new Dictionary<string, object>
			{
				["systemInstruction"] = new Dictionary<string, object>
				{
					["parts"] = new[] { new Dictionary<string, object> { ["text"] = systemPrompt } }
				},
				["contents"] = new[]
				{
					new Dictionary<string, object>
					{
						["role"] = "user",
						["parts"] = new[] { new Dictionary<string, object> { ["text"] = userPrompt } }
					}
				},
				["generationConfig"] = new Dictionary<string, object>
				{
					["responseMimeType"] = "application/json",
					["temperature"] = 0.85,
					["maxOutputTokens"] = 400
				}
			};

			using (var content = new StringContent(Json.Serialize(body), Encoding.UTF8, "application/json"))
			using (HttpResponseMessage resp = await Http.PostAsync(url, content).ConfigureAwait(false))
			{
				string text = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
				if (!resp.IsSuccessStatusCode)
				{
					AiLog.Warn($"Gemini HTTP {(int)resp.StatusCode}: {Trim(text)}");
					return null;
				}
				return ExtractText(text);
			}
		}
		catch (Exception ex)
		{
			AiLog.Warn("Gemini request failed: " + ex.Message);
			return null;
		}
	}

	// Pull candidates[0].content.parts[0].text out of the response payload.
	private static string ExtractText(string responseJson)
	{
		try
		{
			var root = Json.DeserializeObject(responseJson) as Dictionary<string, object>;
			if (root == null)
			{
				return null;
			}
			if (root["candidates"] is object[] candidates && candidates.Length > 0
				&& candidates[0] is Dictionary<string, object> cand
				&& cand["content"] is Dictionary<string, object> contentObj
				&& contentObj["parts"] is object[] parts && parts.Length > 0
				&& parts[0] is Dictionary<string, object> part
				&& part.TryGetValue("text", out object t))
			{
				return t?.ToString();
			}
		}
		catch (Exception ex)
		{
			AiLog.Warn("Gemini parse failed: " + ex.Message);
		}
		return null;
	}

	// Parse the model's JSON reply into a small {say, action, target} record.
	public static AiDecision ParseDecision(string modelJson)
	{
		var decision = new AiDecision();
		if (string.IsNullOrWhiteSpace(modelJson))
		{
			return decision;
		}
		try
		{
			var obj = Json.DeserializeObject(modelJson) as Dictionary<string, object>;
			if (obj != null)
			{
				if (obj.TryGetValue("say", out object say))
				{
					decision.Say = say?.ToString();
				}
				if (obj.TryGetValue("action", out object action))
				{
					decision.Action = (action?.ToString() ?? "none").ToLowerInvariant();
				}
				if (obj.TryGetValue("target", out object target))
				{
					decision.Target = target?.ToString();
				}
			}
		}
		catch
		{
			// If the model didn't return clean JSON, treat the whole thing as a spoken line.
			decision.Say = modelJson;
		}
		return decision;
	}

	private static string Trim(string s) => string.IsNullOrEmpty(s) || s.Length <= 300 ? s : s.Substring(0, 300);
}

public class AiDecision
{
	public string Say { get; set; }

	public string Action { get; set; } = "none";

	public string Target { get; set; }
}
