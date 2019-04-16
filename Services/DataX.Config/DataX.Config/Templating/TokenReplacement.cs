// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using DataX.Config.ConfigDataModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace DataX.Config.Templating
{
    public static class TokenReplacement
    {
        public static JsonConfig ReplaceTokens(JsonConfig config, IDictionary<string, Token> tokens)
        {
            if(tokens==null || tokens.Count == 0 || config == null)
            {
                return config;
            }

            var text = config.ToString();
            foreach(var token in ResolveTokens(tokens))
            {
                text = ReplaceStringWithToken(text, token.Key, token.Value);
            }

            return JsonConfig.From(text);
        }

        public static string TokenPlaceHolder(string tokenName)
        {
            return $"${{{tokenName}}}";
        }

        public static string QuoteValue(string value)
        {
            return $"\"{value}\"";
        }

        public static string ReplaceStringWithToken(string origin, string tokenName, Token token)
        {
            var text = origin;
            var valueToReplace = token.Value;
            var tokenPlaceHolder = TokenPlaceHolder(tokenName);
            switch (token.Type)
            {
                case TokenType.String:
                    text = text.Replace(tokenPlaceHolder, valueToReplace);
                    break;
                case TokenType.Null:
                    text = text.Replace(QuoteValue(tokenPlaceHolder), "null");
                    text = text.Replace(tokenPlaceHolder, "");
                    break;
                case TokenType.Object:
                    text = text.Replace(QuoteValue(tokenPlaceHolder), valueToReplace);
                    text = text.Replace(tokenPlaceHolder, valueToReplace);
                    break;
                default:
                    break;
            }

            return text;
        }

        public static Token ReplaceTokenWithToken(Token origin, string tokenName, Token token)
        {
            var text = ReplaceStringWithToken(origin.Value, tokenName, token);
            if (text == token.Value)
            {
                return token;
            }
            else
            {
                return new Token(text, origin.Type);
            }
        }

        private static readonly Regex _TokenMatchRegex = new Regex(@"\$\{(\w+)\}");
        public static HashSet<string> GetReferencingTokenNames(string text)
        {
            if (text == null)
            {
                return new HashSet<string>();
            }
            else
            {
                return _TokenMatchRegex.Matches(text).Select(m => m.Groups[1].Value).ToHashSet();
            }
        }

        public static HashSet<string> FindInnerTokenNames(Token token)
        {
            return GetReferencingTokenNames(token.Value);
        }

        public static Dictionary<string, Token> ResolveTokens(IDictionary<string, Token> tokens)
        {
            var visitedTokens = new Dictionary<string, Token>();
            var paths = new Dictionary<string, HashSet<string>>(
                tokens.Select(t => KeyValuePair.Create(t.Key, FindInnerTokenNames(t.Value))));

            var pendingTokens = new Stack<string>();
            
            foreach (var token in paths)
            {
                var tokenName = token.Key;
                if (visitedTokens.ContainsKey(tokenName))
                {
                    continue;
                }

                pendingTokens.Push(tokenName);
                while (pendingTokens.Count > 0)
                {
                    var currentToken = pendingTokens.Pop();
                    if (visitedTokens.ContainsKey(currentToken))
                    {
                        continue;
                    }

                    var embededPendingTokens = paths[currentToken].Where(dep => !visitedTokens.ContainsKey(dep) && paths.ContainsKey(dep)).ToList();

                    if (embededPendingTokens.Count==0)
                    {
                        var value = tokens[currentToken];
                        foreach (var dep in paths[currentToken])
                        {
                            if (visitedTokens.ContainsKey(dep))
                            {
                                value = ReplaceTokenWithToken(value, dep, visitedTokens[dep]);
                            }
                        }

                        visitedTokens[currentToken] = value;
                    }
                    else
                    {
                        pendingTokens.Push(currentToken);
                        foreach(var dep in embededPendingTokens)
                        {
                            if (pendingTokens.Contains(dep))
                            {
                                throw new ConfigGenerationException($"detected circular reference in token replacement, token name:'{dep}'");
                            }
                            else
                            {
                                pendingTokens.Push(dep);
                            }
                        }
                    }
                }
            }

            return visitedTokens;
        }
    }
}
