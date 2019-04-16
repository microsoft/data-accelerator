// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace DataX.Config.ConfigDataModel
{
    /// <summary>
    /// Represents the properties-format configuration
    /// </summary>
    public class PropertiesDictionary: Dictionary<string, string>
    {
        private static Regex _CommentRegex = new Regex(@"^\s*#");
        public const string Separater = "=";

        /// <summary>
        /// Get a <see cref="PropertiesDictionary"/> instance from string
        /// </summary>
        /// <param name="content">content of the property-format string</param>
        /// <returns>An instance of <see cref="PropertiesDictionary"/></returns>
        public static PropertiesDictionary From(string content)
        {
            if (content == null)
            {
                return null;
            }

            return From(ReadPropertiesFromString(content));
        }

        /// <summary>
        /// Get a list of property name-value pairs from a string
        /// </summary>
        /// <param name="content">content of the property-format string</param>
        /// <returns>A list of property name-value pairs</returns>
        public static IEnumerable<Tuple<string, string>> ReadPropertiesFromString(string content)
        {
            using (var sr = new StringReader(content))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    if (string.IsNullOrWhiteSpace(line) || _CommentRegex.IsMatch(line))
                    {
                        continue;
                    }

                    var pos = line.IndexOf(Separater);
                    if (pos < 0)
                    {
                        yield return Tuple.Create(line, (string)null);
                    }
                    else
                    {
                        yield return Tuple.Create(line.Substring(0, pos), line.Substring(pos + 1));
                    }
                }
            }
        }

        /// <summary>
        /// Get a <see cref="PropertiesDictionary"/> instance from a list of key-value pairs
        /// Note if there are duplicated keys in the list, the last one wins.
        /// </summary>
        /// <param name="keyvaluePairs">the list of key-value pairs as input</param>
        /// <returns>An instance of <see cref="PropertiesDictionary"/></returns>
        public static PropertiesDictionary From(IEnumerable<Tuple<string, string>> keyvaluePairs)
        {
            if (keyvaluePairs == null)
            {
                return null;
            }

            var result = new PropertiesDictionary();

            foreach(var kv in keyvaluePairs)
            {
                result[kv.Item1] = kv.Item2;
            }

            return result;
        }

        /// <summary>
        /// Comparing two <see cref="Dictionary{string, string}"/> by full-outer-joining them on the key field
        /// </summary>
        /// <param name="dict1">first dictionay</param>
        /// <param name="dict2">second dictionay</param>
        /// <returns>
        /// List of tuples with first item being the key,
        /// second item the value from first input dictionary,
        /// third item the value from the second input dictionary.
        ///
        /// if one dictionary doesn't have value at the key, the corresponding value item will be null
        /// if both input dictionaries are null, this return an empty list.
        /// </returns>
        public static IEnumerable<Tuple<string, string, string>> Match(Dictionary<string, string> dict1, Dictionary<string, string> dict2)
        {
            if(dict1 == null && dict2 == null)
            {
                return Enumerable.Empty<Tuple<string, string, string>>();
            }
            else if(dict1 == null)
            {
                return dict2.Select(d => Tuple.Create(d.Key, (string)null, d.Value));
            }
            else if (dict2 == null)
            {
                return dict1.Select(d => Tuple.Create(d.Key, d.Value, (string)null));
            }
            else
            {
                return MatchNotNulls(dict1, dict2);
            }
        }

        /// <summary>
        /// Comparing two <see cref="Dictionary{string, string}"/> by full-outer-joining them on the key field
        /// </summary>
        /// <param name="dict1">first dictionay, assuming not null</param>
        /// <param name="dict2">second dictionay, assuming not null</param>
        /// <returns>
        /// List of tuples with first item being the key,
        /// second item the value from first input dictionary,
        /// third item the value from the second input dictionary.
        ///
        /// if one dictionary doesn't have value at the key, the corresponding value item will be null
        /// </returns>
        private static IEnumerable<Tuple<string, string, string>> MatchNotNulls(Dictionary<string, string> dict1, Dictionary<string, string> dict2)
        {
            var props1 = dict1.Keys;
            var props2 = dict2.Keys;

            var exceptions1 = props1.ToHashSet();
            exceptions1.ExceptWith(props2);
            foreach (var name in exceptions1)
            {
                var value = dict1[name];
                yield return Tuple.Create(name, value, (string)null);
            }

            var exceptions2 = props2.ToHashSet();
            exceptions2.ExceptWith(props1);
            foreach (var name in exceptions2)
            {
                var value = dict2[name];
                yield return Tuple.Create(name, (string)null, value);
            }

            var commons = props1.ToHashSet();
            commons.ExceptWith(exceptions1);
            foreach (var name in commons)
            {
                var value1 = dict1[name];
                var value2 = dict2[name];
                yield return Tuple.Create(name, value1, value2);
            }
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            foreach (var kv in this.Where(kv => kv.Value != null))
            {
                sb.AppendLine(kv.Key + Separater + kv.Value);
            }

            return sb.ToString();
        }
    }
}
