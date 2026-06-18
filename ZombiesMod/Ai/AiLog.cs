using System;
using System.IO;

namespace ZombiesMod.Ai;

// Thread-safe logging for the AI subsystem (calls happen on background threads).
public static class AiLog
{
	private static readonly object Lock = new object();

	private const string Path = "./scripts/ZombiesAi.log";

	public static void Info(string message) => Write("INFO", message);

	public static void Warn(string message) => Write("WARN", message);

	private static void Write(string level, string message)
	{
		try
		{
			lock (Lock)
			{
				File.AppendAllText(Path, $"[{DateTime.UtcNow:u}] {level} {message}{Environment.NewLine}");
			}
		}
		catch
		{
			// Never let logging throw on a worker thread.
		}
	}
}
