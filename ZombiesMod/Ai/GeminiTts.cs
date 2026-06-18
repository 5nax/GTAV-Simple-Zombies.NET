using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using ZombiesMod.Static;

namespace ZombiesMod.Ai;

// Neural text-to-speech via Gemini's TTS model. Returns a ready-to-play WAV (the API
// returns raw 16-bit PCM, which we wrap with a RIFF/WAVE header). Runs off the game
// thread; returns null on any failure so the caller can fall back to SAPI.
public static class GeminiTts
{
	private static readonly HttpClient Http = new HttpClient { Timeout = TimeSpan.FromSeconds(30.0) };

	private static readonly JavaScriptSerializer Json = new JavaScriptSerializer();

	public static async Task<byte[]> SynthesizeWavAsync(string text, string voiceName)
	{
		if (!GeminiClient.IsConfigured || string.IsNullOrWhiteSpace(text))
		{
			return null;
		}
		try
		{
			string url = $"https://generativelanguage.googleapis.com/v1beta/models/{GameConfig.GeminiTtsModel}:generateContent?key={GameConfig.GeminiApiKey}";
			string voice = string.IsNullOrWhiteSpace(voiceName) ? GameConfig.GeminiTtsVoice : voiceName;

			var body = new Dictionary<string, object>
			{
				["contents"] = new[]
				{
					new Dictionary<string, object>
					{
						["parts"] = new[] { new Dictionary<string, object> { ["text"] = text } }
					}
				},
				["generationConfig"] = new Dictionary<string, object>
				{
					["responseModalities"] = new[] { "AUDIO" },
					["speechConfig"] = new Dictionary<string, object>
					{
						["voiceConfig"] = new Dictionary<string, object>
						{
							["prebuiltVoiceConfig"] = new Dictionary<string, object> { ["voiceName"] = voice }
						}
					}
				}
			};

			using (var content = new StringContent(Json.Serialize(body), Encoding.UTF8, "application/json"))
			using (HttpResponseMessage resp = await Http.PostAsync(url, content).ConfigureAwait(false))
			{
				string payload = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
				if (!resp.IsSuccessStatusCode)
				{
					AiLog.Warn($"Gemini TTS HTTP {(int)resp.StatusCode}");
					return null;
				}
				return ToWav(payload);
			}
		}
		catch (Exception ex)
		{
			AiLog.Warn("Gemini TTS failed: " + ex.Message);
			return null;
		}
	}

	private static byte[] ToWav(string responseJson)
	{
		try
		{
			var root = Json.DeserializeObject(responseJson) as Dictionary<string, object>;
			if (root != null
				&& root["candidates"] is object[] candidates && candidates.Length > 0
				&& candidates[0] is Dictionary<string, object> cand
				&& cand["content"] is Dictionary<string, object> contentObj
				&& contentObj["parts"] is object[] parts && parts.Length > 0
				&& parts[0] is Dictionary<string, object> part
				&& part["inlineData"] is Dictionary<string, object> inline)
			{
				string mime = inline.TryGetValue("mimeType", out object m) ? m?.ToString() : "";
				byte[] pcm = Convert.FromBase64String(inline["data"].ToString());
				int rate = ParseRate(mime);
				return BuildWav(pcm, rate, channels: 1, bitsPerSample: 16);
			}
		}
		catch (Exception ex)
		{
			AiLog.Warn("Gemini TTS parse failed: " + ex.Message);
		}
		return null;
	}

	private static int ParseRate(string mime)
	{
		// e.g. "audio/L16;codec=pcm;rate=24000"
		Match match = Regex.Match(mime ?? "", "rate=(\\d+)");
		return match.Success ? int.Parse(match.Groups[1].Value) : 24000;
	}

	private static byte[] BuildWav(byte[] pcm, int sampleRate, short channels, short bitsPerSample)
	{
		int byteRate = sampleRate * channels * bitsPerSample / 8;
		short blockAlign = (short)(channels * bitsPerSample / 8);
		using (var ms = new MemoryStream())
		using (var w = new BinaryWriter(ms))
		{
			w.Write(Encoding.ASCII.GetBytes("RIFF"));
			w.Write(36 + pcm.Length);
			w.Write(Encoding.ASCII.GetBytes("WAVE"));
			w.Write(Encoding.ASCII.GetBytes("fmt "));
			w.Write(16);                 // PCM fmt chunk size
			w.Write((short)1);           // PCM format
			w.Write(channels);
			w.Write(sampleRate);
			w.Write(byteRate);
			w.Write(blockAlign);
			w.Write(bitsPerSample);
			w.Write(Encoding.ASCII.GetBytes("data"));
			w.Write(pcm.Length);
			w.Write(pcm);
			w.Flush();
			return ms.ToArray();
		}
	}
}
