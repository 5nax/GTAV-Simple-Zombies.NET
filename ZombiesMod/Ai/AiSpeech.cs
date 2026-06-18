using System;
using System.Collections.Concurrent;
using System.Speech.Recognition;
using System.Speech.Synthesis;
using ZombiesMod.Static;

namespace ZombiesMod.Ai;

// Windows SAPI wrapper: text-to-speech so companions talk out loud, and optional
// push-to-talk dictation so you can speak to them. Everything is lazily initialized and
// fully guarded — if speech isn't available (no audio device, non-Windows), it simply
// no-ops and the rest of the mod keeps working. Recognized phrases are queued for the
// game thread to drain.
public static class AiSpeech
{
	private static SpeechSynthesizer _tts;

	private static SpeechRecognitionEngine _stt;

	private static bool _initTried;

	private static readonly ConcurrentQueue<string> Heard = new ConcurrentQueue<string>();

	// When false, recognized speech is ignored (push-to-talk gating).
	public static bool Listening { get; set; }

	public static bool SpeechAvailable { get; private set; }

	public static void EnsureInit()
	{
		if (_initTried)
		{
			return;
		}
		_initTried = true;
		try
		{
			_tts = new SpeechSynthesizer();
			_tts.SetOutputToDefaultAudioDevice();
			_tts.Rate = 1;
			SpeechAvailable = true;
		}
		catch (Exception ex)
		{
			AiLog.Warn("TTS init failed: " + ex.Message);
		}

		if (GameConfig.AiVoiceInputEnabled)
		{
			try
			{
				_stt = new SpeechRecognitionEngine();
				_stt.SetInputToDefaultAudioDevice();
				_stt.LoadGrammar(new DictationGrammar());
				_stt.SpeechRecognized += OnRecognized;
				_stt.RecognizeAsync(RecognizeMode.Multiple);
				AiLog.Info("Voice input ready.");
			}
			catch (Exception ex)
			{
				AiLog.Warn("Speech recognition init failed: " + ex.Message);
				_stt = null;
			}
		}
	}

	public static void Speak(string text)
	{
		if (!GameConfig.AiTtsEnabled || string.IsNullOrWhiteSpace(text) || _tts == null)
		{
			return;
		}
		try
		{
			_tts.SpeakAsyncCancelAll();
			_tts.SpeakAsync(text);
		}
		catch (Exception ex)
		{
			AiLog.Warn("TTS speak failed: " + ex.Message);
		}
	}

	public static bool HasVoiceInput => _stt != null;

	public static bool TryGetHeard(out string text)
	{
		return Heard.TryDequeue(out text);
	}

	public static void ClearHeard()
	{
		while (Heard.TryDequeue(out _))
		{
		}
	}

	private static void OnRecognized(object sender, SpeechRecognizedEventArgs e)
	{
		if (Listening && e.Result != null && e.Result.Confidence >= 0.3f && !string.IsNullOrWhiteSpace(e.Result.Text))
		{
			Heard.Enqueue(e.Result.Text);
		}
	}
}
