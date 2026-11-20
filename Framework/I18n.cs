using StardewModdingAPI;

namespace LockChests.Framework;

/// <summary>Gets translated text, with an English fallback if the translation is missing.</summary>
internal static class I18n
{
	private static ITranslationHelper? translations;

	/// <summary>Set the translation source. Called once from <see cref="ModEntry.Entry"/>.</summary>
	public static void Init(ITranslationHelper translationHelper)
	{
		translations = translationHelper;
	}

	/// <summary>Get a translation.</summary>
	/// <param name="key">The key in <c>i18n/default.json</c>.</param>
	/// <param name="fallback">The text to use if the key is missing.</param>
	/// <param name="tokens">Values for <c>{{token}}</c> placeholders.</param>
	public static string Get(string key, string fallback, object? tokens = null)
	{
		Translation? translation = translations?.Get(key, tokens);
		return translation is not null && translation.HasValue()
			? translation.ToString()
			: fallback;
	}
}
