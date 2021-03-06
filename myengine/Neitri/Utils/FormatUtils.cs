﻿using System.Collections;
using System.Collections.Generic;

namespace Neitri
{
	public static class FormatUtils
	{
		/// <summary>
		/// If it sees the default Object.ToString() was used, it tries to enumerate elements.
		/// If element is KeyValuePair it shows both key and value.
		/// </summary>
		/// <param name="msg"></param>
		/// <returns></returns>
		public static string BetterToString(object msg)
		{
			if (msg == null) return "(null)";
			if (msg is string) return (string)msg;
			var ret = msg.ToString();
			if (ret == msg.GetType().ToString()) // if its default to string, attempt our better to string
			{
				var enumerable = msg as IEnumerable;
				if (enumerable != null)
				{
					var msgs = new List<string>();
					int nullsFound = 0;
					int countEntries = 0;
					foreach (var value in enumerable)
					{
						if (value != null)
						{
							// http://stackoverflow.com/questions/2729614/c-sharp-reflection-how-can-i-tell-if-object-o-is-of-type-keyvaluepair-and-then
							var valueType = value.GetType();
							if (valueType.IsGenericType)
							{
								var baseType = valueType.GetGenericTypeDefinition();
								if (baseType == typeof(KeyValuePair<,>))
								{
									//var argTypes = baseType.GetGenericArguments();
									object kvpKey = valueType.GetProperty("Key").GetValue(value, null);
									object kvpValue = valueType.GetProperty("Value").GetValue(value, null);
									msgs.Add(BetterToString(kvpKey) + ": " + BetterToString(kvpValue));
									continue;
								}
							}
							msgs.Add(BetterToString(value));
						}
						else
						{
							nullsFound++;
						}
						countEntries++;
					}
					ret = ret.Substring(0, ret.Length - 2) + "[" + countEntries + "]";
					if (msgs.Count > 0) ret += " = \"" + string.Join("\", \"", msgs.ToArray()) + "\"";
					else if (nullsFound > 0) ret += " (IEnumerable contains only " + nullsFound + " null entries)";
					else ret += " (IEnumerable is empty)";
				}
			}
			return ret;
		}

		public static string BetterNicifyVariableName(string name)
		{
			//return ObjectNames.NicifyVariableName(name);
			string ret = "";
			for (int i = 0; i < name.Length; i++)
			{
				char thisChar = name[i];
				char nextChar = (i + 1) >= name.Length ? ' ' : name[(i + 1)];
				bool nextIsUpper = char.IsUpper(nextChar);
				bool thisIsUpper = char.IsUpper(thisChar);

				// first always upper
				if (i == 0 && nextChar != '_') thisChar = char.ToUpper(thisChar);
				ret += thisChar;

				if (!thisIsUpper && nextIsUpper) ret += " ";
			}
			return ret;
		}

		public static string SecondsToString(float seconds)
		{
			return SecondsToString((double)seconds);
		}
		public static string SecondsToString(double seconds)
		{
			if (seconds > 1) return SecondsToStringHelper(seconds) + "s";
			if (seconds > 1e-3) return SecondsToStringHelper(seconds * 1e3) + "ms";
			if (seconds > 1e-6) return SecondsToStringHelper(seconds * 1e6) + "μs";
			if (seconds > 1e-9) return SecondsToStringHelper(seconds * 1e9) + "ns";
			if (seconds > 1e-12) return SecondsToStringHelper(seconds * 1e12) + "ps";
			return seconds + "s";
		}
		private static string SecondsToStringHelper(double v)
		{
			if (v > 1000) return ((int)v).ToString();
			if (v > 100) return string.Format("{0:N1}", v);
			if (v > 10) return string.Format("{0:N2}", v);
			return string.Format("{0:N3}", v);
		}
	}
}