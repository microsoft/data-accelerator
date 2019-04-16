// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using Newtonsoft.Json;
using DataX.Config.ConfigDataModel;
using DataX.Config.Templating;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace DataX.Config
{
    /// <summary>
    /// Represents a session for deployment
    /// </summary>
    public class DeploymentSession
    {
        public DeploymentSession(string name, TokenDictionary tokens)
        {
            this.Name = name;
            this.Tokens = (tokens == null) ? new TokenDictionary() : tokens.Clone();
        }

        public string Name { get; }
        public TokenDictionary Tokens { get; }
        
        public string GetTokenString(string key)
        {
            return Tokens.GetString(key);
        }

        public void SetToken(string key, Token value)
        {
            Tokens.Set(key, value);
        }

        public void SetStringToken(string key, string value)
        {
            SetToken(key, Token.FromString(value));
        }

        public void SetNullToken(string key)
        {
            SetToken(key, Token.NullToken);
        }

        public void SetObjectToken(string key, object value)
        {
            SetToken(key, Token.FromObject(value));
        }

        public void SetJsonToken(string key, JsonConfig json)
        {
            SetToken(key, Token.FromJson(json));
        }

        public void SetObjectTokenWithJsonString(string key, string value)
        {
            SetToken(key, Token.FromJsonString(value));
        }
    }
}
