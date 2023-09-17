using System;
using System.Linq;

namespace CSTGames.Utility
{
	/// <summary>
	/// Provides some useful methods for manipulating Strings.
	/// </summary>
	public static class StringManipulator
	{
		public static string AddWhitespaceBeforeCapital(string str)
		{
			return String.Concat(str.Select(x => Char.IsUpper(x) ? " " + x : x.ToString()))
									.TrimStart(' ');
		}

		public static string AddHyphenBeforeNumber(string str)
		{
			return String.Concat(str.Select(x => Char.IsDigit(x) ? "-" + x : x.ToString()))
									.TrimStart('-');
		}

		public static string ClearWhitespaces(string str)
		{
			return new string(str.ToCharArray()
				.Where(c => !Char.IsWhiteSpace(c))
				.ToArray());
		}
	}

	/// <summary>
	/// Provides some useful methods for manipulating Numbers.
	/// </summary>
	public static class NumberManipulator
	{
		/// <summary>
		/// Linearly converts a floating point number from one range to another, maintains ratio.
		/// <para />
		/// You can swap the min and max of a range to achieve inverse result, respectively.
		/// </summary>
		/// <param name="targetValue"> The value to convert. </param>
		/// <param name="oldMin"> The min of the old range. </param>
		/// <param name="oldMax"> The max of the old range. </param>
		/// <param name="newMin"> The min of the new range. </param>
		/// <param name="newMax"> The max of the new range. </param>
		/// <returns> <c>newValue</c> A new converted value within the new range. </returns>
		public static float RangeConvert(float targetValue, float oldMin, float oldMax, float newMin, float newMax)
		{
			float oldRange = oldMax - oldMin;
			float newRange = newMax - newMin;

			// If the oldMax == oldMin, then just clamps the value directly within the new range.
			if (oldRange == 0f)
				return Math.Clamp(targetValue, newMin, newMax);
			else
				return ((targetValue - oldMin) * newRange / oldRange) + newMin;
		}

		/// <summary>
		/// Linearly converts an integer from one range to another, maintains ratio.
		/// <para />
		/// You can swap the min and max of a range to achieve inverse result, respectively.
		/// </summary>
		/// <param name="targetValue"> The value to convert. </param>
		/// <param name="oldMin"> The min of the old range. </param>
		/// <param name="oldMax"> The max of the old range. </param>
		/// <param name="newMin"> The min of the new range. </param>
		/// <param name="newMax"> The max of the new range. </param>
		/// <returns> <c>newValue</c> A new converted value within the new range. </returns>
		public static int RangeConvert(int targetValue, int oldMin, int oldMax, int newMin, int newMax)
		{
			int oldRange = oldMax - oldMin;
			int newRange = newMax - newMin;

			if (oldRange == 0)
				return Math.Clamp(targetValue, newMin, newMax);
			else
				return ((targetValue - oldMin) * newRange / oldRange) + newMin;
		}
	}
}