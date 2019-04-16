// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using DataX.Config.ConfigDataModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataX.Config.Templating
{
    /// <summary>
    /// A container holds a map of key - token pairs
    /// </summary>
    public class TokenDictionary
    {
        /// <summary>
        /// the internal dictionary of key - token
        /// </summary>
        private readonly Dictionary<string, Token> _tokens;

        /// <summary>
        /// hashmap of missing token name and its dependents
        /// </summary>
        private readonly Dictionary<string, HashSet<string>> _missingTokenNames;

        /// <summary>
        /// hashmap of token name and its unresolved inner token names 
        /// </summary>
        private readonly Dictionary<string, HashSet<string>> _tokensUnresolved;

        /// <summary>
        /// Constructor of the class <see cref="TokenDictionary"/>
        /// </summary>
        public TokenDictionary()
            :this(new Dictionary<string, Token>(),
                 new Dictionary<string, HashSet<string>>(),
                 new Dictionary<string, HashSet<string>>())
        {
        }

        private TokenDictionary(Dictionary<string, Token> tokens,
                            Dictionary<string, HashSet<string>> missings,
                            Dictionary<string, HashSet<string>> tokensUnresolved)
        {
            _tokens = tokens;
            _missingTokenNames = missings;
            _tokensUnresolved = tokensUnresolved;
        }

        /// <summary>
        /// Add a batch of "token name" and token pairs
        /// </summary>
        /// <param name="append">the batch of pairs to add</param>
        public void AddBatch(IEnumerable<KeyValuePair<string, Token>> append)
        {
            if (append == null)
            {
                return;
            }

            foreach(var t in append)
            {
                Set(t.Key, t.Value);
            }
        }

        /// <summary>
        /// Add tokens from another <see cref="TokenDictionary"/> into this
        /// </summary>
        /// <param name="dict">the dictionary to merge</param>
        public void AddDictionary(TokenDictionary dict)
        {
            if (dict == null)
            {
                return;
            }

            AddBatch(dict._tokens);
        }

        /// <summary>
        /// return a deep cloned instance of <see cref="TokenDictionary"/> with each <see cref="Token"/> cloned too.
        /// </summary>
        /// <returns></returns>
        public TokenDictionary Clone()
        {
            var tokens = _tokens.ToDictionary(kv => kv.Key, kv => kv.Value.Clone());
            var missings = _missingTokenNames.ToDictionary(kv => kv.Key, kv => new HashSet<string>(kv.Value));
            var unresolveds = _tokensUnresolved.ToDictionary(kv => kv.Key, kv => new HashSet<string>(kv.Value));
            return new TokenDictionary(tokens, missings, unresolveds);
        }

        /// <summary>
        /// Add token for name
        /// </summary>
        /// <param name="key">token name</param>
        /// <param name="token"></param>
        public void Set(string key, Token token)
        {
            var refers = _missingTokenNames.ContainsKey(key) ? _missingTokenNames[key] : new HashSet<string>();
            var missingRefees = new HashSet<string>();

            foreach (var innerTokenName in TokenReplacement.FindInnerTokenNames(token))
            {
                if (innerTokenName == key || refers.Contains(innerTokenName))
                {
                    throw new ConfigGenerationException($"detected circular reference in token replacement, token name:'{key}'");
                }

                if (_tokens.ContainsKey(innerTokenName))
                {
                    token = TokenReplacement.ReplaceTokenWithToken(token, innerTokenName, _tokens[innerTokenName]);
                    if (_tokensUnresolved.ContainsKey(innerTokenName))
                    {
                        missingRefees.UnionWith(_tokensUnresolved[innerTokenName]);
                    }
                }
                else
                {
                    missingRefees.Add(innerTokenName);
                }
            }

            foreach(var refer in refers)
            {
                _tokens[refer] = TokenReplacement.ReplaceTokenWithToken(_tokens[refer], key, token);
                if (_tokensUnresolved.ContainsKey(refer))
                {
                    _tokensUnresolved[refer].Remove(key);
                    _tokensUnresolved[refer].UnionWith(missingRefees);
                }
                else
                {
                    if (missingRefees.Count > 0)
                    {
                        _tokensUnresolved.Add(refer, missingRefees);
                    }
                }
            }

            if (missingRefees.Count > 0)
            {
                refers.Add(key);
                foreach (var refee in missingRefees)
                {
                    _missingTokenNames.MergeKeyList(refee, refers);
                }

                _tokensUnresolved[key] = missingRefees;
            }

            _missingTokenNames.Remove(key);
            _tokens[key] = token;
        }

        /// <summary>
        /// Get string value of the token by given name
        /// </summary>
        /// <param name="key">name of the token</param>
        /// <returns></returns>
        public string GetString(string key)
        {
            if (_tokens.ContainsKey(key))
            {
                return _tokens[key].Value;
            }
            else
            {
                return null;
                //throw new ArgumentException($"token name '{key}' is not found in this dictionary");
            }
        }

        /// <summary>
        /// Replace tokenized placeholders in the given text with the tokens' value from this dictionary
        /// Note: the token name not found in this dictionary will not be replaced
        /// </summary>
        /// <param name="text">the given text to perform token replacement</param>
        /// <returns></returns>
        public string Resolve(string text)
        {
            var refees = TokenReplacement.GetReferencingTokenNames(text);
            refees.IntersectWith(_tokens.Keys);

            var result = text;
            foreach(var refee in refees)
            {
                var token = _tokens[refee];
                result = TokenReplacement.ReplaceStringWithToken(result, refee, token);
            }

            return result;
        }

        /// <summary>
        /// Replace the tokenized placeholders in the serialized json with the tokens' value from this dictionary,
        /// then deserialize and return another instance of <see cref="JsonConfig"/>
        /// </summary>
        /// <param name="json">input json object to perform the token replacement</param>
        /// <returns></returns>
        public JsonConfig Resolve(JsonConfig json)
        {
            if (json == null)
            {
                return json;
            }

            return JsonConfig.From(Resolve(json.ToString()));
        }
    }
}
