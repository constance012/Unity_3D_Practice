using System.Collections.Generic;

public static class DictionaryExtensions
{
	public static bool ContainsKey<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, out TValue value)
	{
		bool contained = dictionary.ContainsKey(key);

		if (contained)
		{
			value = dictionary[key];
			return true;
		}

		value = default;
		return false;
	}
}
