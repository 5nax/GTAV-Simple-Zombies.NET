using System;
using System.Collections.Concurrent;
using System.IO;
using System.Media;
using System.Threading;
using ZombiesMod.Static;

namespace ZombiesMod.Ai;

// Voice output for companions. Lines are queued and spoken one at a time on a dedicated
// background thread so playback never blocks the game and lines don't overlap. Uses
// Gemini neural TTS by default (rich, natural voices) and automatically falls back to
// Windows SAPI if synthesis/playback fails or the provider is set to "sapi".
public static class AiTts
{
	private struct Job
	{
		public string Text;
		public string Voice;
	}

	private static readonly BlockingCollection<Job> Queue = new BlockingCollection<Job>(new ConcurrentQueue<Job>());

	private static Thread _worker;

	private static readonly object StartLock = new object();

	public static void Speak(string text, string voice = null)
	{
		if (!GameConfig.AiTtsEnabled || string.IsNullOrWhiteSpace(text))
		{
			return;
		}
		EnsureWorker();
		// Cap the backlog so a flurry of barks can't pile up.
		if (Queue.Count < 4)
		{
			Queue.Add(new Job { Text = text, Voice = voice });
		}
	}

	private static void EnsureWorker()
	{
		if (_worker != null)
		{
			return;
		}
		lock (StartLock)
		{
			if (_worker == null)
			{
				_worker = new Thread(Loop) { IsBackground = true, Name = "ZombiesAiTts" };
				_worker.Start();
			}
		}
	}

	private static void Loop()
	{
		foreach (Job job in Queue.GetConsumingEnumerable())
		{
			try
			{
				bool useSapi = string.Equals(GameConfig.AiTtsProvider, "sapi", StringComparison.OrdinalIgnoreCase)
					|| !GeminiClient.IsConfigured;

				if (!useSapi)
				{
					byte[] wav = GeminiTts.SynthesizeWavAsync(job.Text, job.Voice).GetAwaiter().GetResult();
					if (wav != null && wav.Length > 44)
					{
						PlayWav(wav);
						continue;
					}
				}
				// Fallback: Windows SAPI (also handles provider == "sapi").
				AiSpeech.Speak(job.Text);
			}
			catch (Exception ex)
			{
				AiLog.Warn("TTS job failed: " + ex.Message);
				try
				{
					AiSpeech.Speak(job.Text);
				}
				catch
				{
				}
			}
		}
	}

	private static void PlayWav(byte[] wav)
	{
		using (var ms = new MemoryStream(wav))
		using (var player = new SoundPlayer(ms))
		{
			player.PlaySync(); // blocks this worker thread only — serializes lines
		}
	}
}
