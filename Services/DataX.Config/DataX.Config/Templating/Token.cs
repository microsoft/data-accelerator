// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using Newtonsoft.Json;
using DataX.Config.ConfigDataModel;
using System;
using System.Collections.Generic;
using System.Text;

namespace DataX.Config.Templating
{
    public class Token
    {
        public static Token NullToken = new Token(null, TokenType.Null);

        public Token(string str, TokenType ty)
        {
            Value = str;
            Type = ty;
        }

        public static Token FromString(string str)
        {
            return str == null ? NullToken : new Token(str, TokenType.String);
        }

        public static Token FromObject(object obj)
        {
            return obj == null ? NullToken : new Token(JsonConvert.SerializeObject(obj), TokenType.Object);
        }

        public static Token FromJson(JsonConfig json)
        {
            return json == null ? NullToken : new Token(json.ToString(), TokenType.Object);
        }

        public static Token FromJsonString(string json)
        {
            return json == null ? NullToken : new Token(json, TokenType.Object);
        }

        public Token Clone()
        {
            if(Type == TokenType.Null)
            {
                return NullToken;
            }
            else
            {
                return new Token(Value, Type);
            }
        }

        public string Value { get; set; }
        public TokenType Type { get; set; }
    }
}
