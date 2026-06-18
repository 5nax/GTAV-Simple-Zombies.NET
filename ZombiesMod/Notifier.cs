// Global-namespace notification shim. SHVDN v2 had a static GTA.UI.Notify(...) helper;
// SHVDN v3 replaced it with GTA.UI.Notification.PostTicker(...). Routing every call site
// through here keeps them terse and gives one place to tweak notification behavior.
public static class Notifier
{
	public static void Show(string message)
	{
		GTA.UI.Notification.PostTicker(message, false, true);
	}

	public static void Show(string message, bool blinking)
	{
		GTA.UI.Notification.PostTicker(message, blinking, true);
	}
}
