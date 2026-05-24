// Copyright © 2026 Neil Colvin.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
namespace System.Runtime.CompilerServices;

internal static class RuntimeHelpers
	{
	public static T[] GetSubArray<T> (T[] array, Range range)
		{
		(int offset, int length) = range.GetOffsetAndLength (array.Length);
		var result = new T[length];
		Array.Copy (array, offset, result, 0, length);
		return result;
		}
	}