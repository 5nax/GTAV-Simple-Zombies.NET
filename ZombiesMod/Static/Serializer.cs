using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace ZombiesMod.Static;

// Binary save/load for the mod's [Serializable] save types. BinaryFormatter is fully
// supported on .NET Framework 4.8 (the runtime ScriptHookVDotNet requires); the data is
// local and mod-authored, so the deserialization-of-untrusted-data concern does not apply.
// Hardened over the original: streams are always disposed, failures are logged with a full
// timestamp + stack trace (appended, not overwritten), and a corrupt save is backed up
// rather than silently destroyed on the next write.
public static class Serializer
{
	private static readonly string CrashLogPath = Config.ScriptFilePath + "ZombiesModCrashLog.txt";

	public static T Deserialize<T>(string path)
	{
		if (!File.Exists(path))
		{
			return default(T);
		}
		try
		{
			using (FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read))
			{
				BinaryFormatter binaryFormatter = new BinaryFormatter();
				return (T)binaryFormatter.Deserialize(fileStream);
			}
		}
		catch (Exception ex)
		{
			Log($"Failed to load '{path}': {ex}");
			BackupCorruptFile(path);
			return default(T);
		}
	}

	public static void Serialize<T>(string path, T obj)
	{
		try
		{
			using (FileStream fileStream = new FileStream(path, FileMode.Create, FileAccess.Write))
			{
				BinaryFormatter binaryFormatter = new BinaryFormatter();
				binaryFormatter.Serialize(fileStream, obj);
			}
		}
		catch (Exception ex)
		{
			Log($"Failed to save '{path}': {ex}");
		}
	}

	private static void BackupCorruptFile(string path)
	{
		try
		{
			string backup = path + ".corrupt";
			if (File.Exists(backup))
			{
				File.Delete(backup);
			}
			File.Move(path, backup);
			Log($"Backed up unreadable save '{path}' to '{backup}'.");
		}
		catch (Exception ex)
		{
			Log($"Could not back up corrupt save '{path}': {ex.Message}");
		}
	}

	private static void Log(string message)
	{
		try
		{
			File.AppendAllText(CrashLogPath, $"[{DateTime.UtcNow:u}] {message}{Environment.NewLine}");
		}
		catch
		{
			// Logging must never throw on the script thread.
		}
	}
}
