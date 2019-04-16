// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using System;
using System.Collections.Generic;
using System.Text;

namespace DataX.Flow.CodegenRules
{
    public class Formatter
    {
        public const string WhiteSpace = " \n\r\f\t";

        protected const string IndentString = "    ";
        protected const string Initial = "\n    ";
        protected static readonly HashSet<string> BeginClauses = new HashSet<string>();
        protected static readonly HashSet<string> Dml = new HashSet<string>();
        protected static readonly HashSet<string> EndClauses = new HashSet<string>();
        protected static readonly HashSet<string> Logical = new HashSet<string>();
        protected static readonly HashSet<string> Misc = new HashSet<string>();
        protected static readonly HashSet<string> Quantifiers = new HashSet<string>();

        static Formatter()
        {
            BeginClauses.Add("left");
            BeginClauses.Add("right");
            BeginClauses.Add("inner");
            BeginClauses.Add("outer");
            BeginClauses.Add("group");
            BeginClauses.Add("order");

            EndClauses.Add("where");
            EndClauses.Add("set");
            EndClauses.Add("having");
            EndClauses.Add("join");
            EndClauses.Add("from");
            EndClauses.Add("by");
            EndClauses.Add("join");
            EndClauses.Add("into");
            EndClauses.Add("union");

            Logical.Add("and");
            Logical.Add("or");
            Logical.Add("when");
            Logical.Add("else");
            Logical.Add("end");

            Quantifiers.Add("in");
            Quantifiers.Add("all");
            Quantifiers.Add("exists");
            Quantifiers.Add("some");
            Quantifiers.Add("any");

            Dml.Add("insert");
            Dml.Add("update");
            Dml.Add("delete");

            Misc.Add("select");
            Misc.Add("on");
            Misc.Add("struct");
            Misc.Add("map");
            Misc.Add("if");
            Misc.Add("array");
            Misc.Add("filternull");
        }

        public static string Format(string source)
        {
            return new FormatProcess(source).Perform();
        }

        #region Nested type: FormatProcess

        private class FormatProcess
        {
            private readonly List<bool> _afterByOrFromOrSelects = new List<bool>();
            private readonly List<int> _parenCounts = new List<int>();
            private readonly StringBuilder _result = new StringBuilder();
            private readonly IEnumerator<string> _tokens;
            private bool _afterBeginBeforeEnd;
            private bool _afterBetween;
            private bool _afterByOrSetOrFromOrSelect;
            private bool _afterInsert;
            private bool _afterOn;
            private bool _beginLine = true;
            private bool _endCommandFound;
            private bool _insideStructOrMapOrIf = false;

            private int _indent = 1;
            private int _inFunction;

            private string _lastToken;
            private string _lcToken;
            private int _parensSinceSelect;
            private string _token;

            public FormatProcess(string sql)
            {
                // TODO : some delimiter may depend from a specific Dialect/Drive (as ';' to separate multi query)
                _tokens = new StringTokenizer(sql, "()+*/-=<>'`\"[],;" + WhiteSpace, true).GetEnumerator();
            }

            public string Perform()
            {
                _result.Append(Initial);

                while (_tokens.MoveNext())
                {
                    _token = _tokens.Current;
                    _lcToken = _token.ToLowerInvariant();

                    if ("'".Equals(_token))
                    {
                        ExtractStringEnclosedBy("'");
                    }
                    else if ("\"".Equals(_token))
                    {
                        ExtractStringEnclosedBy("\"");
                    }

                    if (IsMultiQueryDelimiter(_token))
                    {
                        StartingNewQuery();
                    }
                    else if (_afterByOrSetOrFromOrSelect && ",".Equals(_token))
                    {
                        CommaAfterByOrFromOrSelect();
                    }
                    else if (_afterOn && ",".Equals(_token))
                    {
                        CommaAfterOn();
                    }
                    else if ("(".Equals(_token))
                    {
                        OpenParen();
                    }
                    else if (")".Equals(_token))
                    {
                        CloseParen();
                    }
                    else if (BeginClauses.Contains(_lcToken))
                    {
                        BeginNewClause();
                    }
                    else if (EndClauses.Contains(_lcToken))
                    {
                        EndNewClause();
                    }
                    else if ("select".Equals(_lcToken))
                    {
                        Select();
                    }
                    else if (Dml.Contains(_lcToken))
                    {
                        UpdateOrInsertOrDelete();
                    }
                    else if ("values".Equals(_lcToken))
                    {
                        Values();
                    }
                    else if ("on".Equals(_lcToken))
                    {
                        On();
                    }
                    else if (_afterBetween && _lcToken.Equals("and"))
                    {
                        Misc();
                        _afterBetween = false;
                    }
                    else if (Formatter.Logical.Contains(_lcToken))
                    {
                        Logical();
                    }
                    else if (IsWhitespace(_token))
                    {
                        White();
                    }
                    else
                    {
                        Misc();
                    }

                    if (!IsWhitespace(_token))
                    {
                        _lastToken = _lcToken;
                    }
                }
                return _result.ToString();
            }

            private void StartingNewQuery()
            {
                Out();
                _indent = 1;
                _endCommandFound = true;
                Newline();
            }

            private bool IsMultiQueryDelimiter(string delimiter)
            {
                return ";".Equals(delimiter);
            }

            private void ExtractStringEnclosedBy(string stringDelimiter)
            {
                while (_tokens.MoveNext())
                {
                    string t = _tokens.Current;
                    _token += t;
                    if (stringDelimiter.Equals(t))
                    {
                        break;
                    }
                }
            }

            private void CommaAfterOn()
            {
                Out();
                _indent--;
                Newline();
                _afterOn = false;
                _afterByOrSetOrFromOrSelect = true;
            }

            private void CommaAfterByOrFromOrSelect()
            {
                Out();
                Newline();
            }

            private void Logical()
            {
                if ("end".Equals(_lcToken))
                {
                    _indent--;
                }
                Newline();
                Out();
                _beginLine = false;
            }

            private void On()
            {
                _indent++;
                _afterOn = true;
                Newline();
                Out();
                _beginLine = false;
            }

            private void Misc()
            {
                Out();
                if ("between".Equals(_lcToken))
                {
                    _afterBetween = true;
                }
                if (_afterInsert)
                {
                    Newline();
                    _afterInsert = false;
                }
                else
                {
                    _beginLine = false;
                    if ("case".Equals(_lcToken))
                    {
                        _indent++;
                    }
                }
            }

            private void White()
            {
                if (!_beginLine)
                {
                    _result.Append(" ");
                }
            }

            private void UpdateOrInsertOrDelete()
            {
                Out();
                _indent++;
                _beginLine = false;
                if ("update".Equals(_lcToken))
                {
                    Newline();
                }
                if ("insert".Equals(_lcToken))
                {
                    _afterInsert = true;
                }
                _endCommandFound = false;
            }

            private void Select()
            {
                Out();
                _indent++;
                Newline();
                _parenCounts.Insert(_parenCounts.Count, _parensSinceSelect);
                _afterByOrFromOrSelects.Insert(_afterByOrFromOrSelects.Count, _afterByOrSetOrFromOrSelect);
                _parensSinceSelect = 0;
                _afterByOrSetOrFromOrSelect = true;
                _endCommandFound = false;
            }

            private void Out()
            {
                _result.Append(_token);
            }

            private void EndNewClause()
            {
                if (!_afterBeginBeforeEnd)
                {
                    _indent--;
                    if (_afterOn)
                    {
                        _indent--;
                        _afterOn = false;
                    }
                    Newline();
                }
                Out();
                if (!"union".Equals(_lcToken))
                {
                    _indent++;
                }
                Newline();
                _afterBeginBeforeEnd = false;
                _afterByOrSetOrFromOrSelect = "by".Equals(_lcToken) || "set".Equals(_lcToken) || "from".Equals(_lcToken);
            }

            private void BeginNewClause()
            {
                if (!_afterBeginBeforeEnd)
                {
                    if (_afterOn)
                    {
                        _indent--;
                        _afterOn = false;
                    }
                    _indent--;
                    Newline();
                }
                Out();
                _beginLine = false;
                _afterBeginBeforeEnd = true;
            }

            private void Values()
            {
                _indent--;
                Newline();
                Out();
                _indent++;
                Newline();
            }

            private void CloseParen()
            {
                if (_endCommandFound)
                {
                    Out();
                    return;
                }
                _parensSinceSelect--;
                if (_parensSinceSelect < 0)
                {
                    _indent--;
                    int tempObject = _parenCounts[_parenCounts.Count - 1];
                    _parenCounts.RemoveAt(_parenCounts.Count - 1);
                    _parensSinceSelect = tempObject;

                    bool tempObject2 = _afterByOrFromOrSelects[_afterByOrFromOrSelects.Count - 1];
                    _afterByOrFromOrSelects.RemoveAt(_afterByOrFromOrSelects.Count - 1);
                    _afterByOrSetOrFromOrSelect = tempObject2;
                }
                if (_inFunction > 0)
                {
                    _inFunction--;
                    Out();
                }
                else
                {
                    if (!_afterByOrSetOrFromOrSelect || _insideStructOrMapOrIf)
                    {
                        _indent--;
                        Newline();
                    }
                    Out();
                }
                _beginLine = false;
            }

            private void OpenParen()
            {
                if (_endCommandFound)
                {
                    Out();
                    return;
                }
                if (IsFunctionName(_lastToken) || _inFunction > 0)
                {
                    _inFunction++;
                }
                _beginLine = false;
                if (_inFunction > 0)
                {
                    Out();
                }
                else
                {
                    Out();
                    if (_lastToken == "struct" || _lastToken == "map" || _lastToken == "if" || _lastToken == "array" || _lastToken == "filternull")
                    {
                        _insideStructOrMapOrIf = true;
                    }

                    if (!_afterByOrSetOrFromOrSelect || _insideStructOrMapOrIf)
                    {
                        _indent++;
                        Newline();
                        _beginLine = true;
                    }
                }
                _parensSinceSelect++;
            }

            private static bool IsFunctionName(string tok)
            {
                char begin = tok[0];
                bool isIdentifier = char.IsLetter(begin) || begin.CompareTo('$') == 0 || begin.CompareTo('_') == 0 || '"' == begin;
                return isIdentifier && !Formatter.Logical.Contains(tok) && !EndClauses.Contains(tok) && !Quantifiers.Contains(tok)
                       && !Dml.Contains(tok) && !Formatter.Misc.Contains(tok);
            }

            private static bool IsWhitespace(string token)
            {
                return WhiteSpace.IndexOf(token) >= 0;
            }

            private void Newline()
            {
                _result.Append("\n");
                for (int i = 0; i < _indent; i++)
                {
                    _result.Append(IndentString);
                }
                _beginLine = true;
            }
        }

        #endregion
    }

    /// <summary>
    /// A StringTokenizer java like object 
    /// </summary>
    public class StringTokenizer : IEnumerable<string>
    {
        private const string _DefaultDelim = " \t\n\r\f";
        private string _origin;
        private string _delim;
        private bool _returnDelim;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="str"></param>
        public StringTokenizer(string str)
        {
            _origin = str;
            _delim = _DefaultDelim;
            _returnDelim = false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="str"></param>
        /// <param name="delim"></param>
        public StringTokenizer(string str, string delim)
        {
            _origin = str;
            _delim = delim;
            _returnDelim = true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="str"></param>
        /// <param name="delim"></param>
        /// <param name="returnDelims"></param>
        public StringTokenizer(string str, string delim, bool returnDelims)
        {
            _origin = str;
            _delim = delim;
            _returnDelim = returnDelims;
        }

        #region IEnumerable<string> Members

        public IEnumerator<string> GetEnumerator()
        {
            return new StringTokenizerEnumerator(this);
        }

        #endregion

        #region IEnumerable Members

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return new StringTokenizerEnumerator(this);
        }

        #endregion


        private class StringTokenizerEnumerator : IEnumerator<string>
        {
            private StringTokenizer _stokenizer;
            private int _cursor = 0;

            public StringTokenizerEnumerator(StringTokenizer stok)
            {
                _stokenizer = stok;
            }

            #region IEnumerator<string> Members

            public string Current { get; private set; } = null;

            #endregion

            #region IDisposable Members

            public void Dispose()
            {
            }

            #endregion

            #region IEnumerator Members

            object System.Collections.IEnumerator.Current => Current;

            public bool MoveNext()
            {
                Current = GetNext();
                return Current != null;
            }

            public void Reset()
            {
                _cursor = 0;
            }

            #endregion

            private string GetNext()
            {
                char c;
                bool isDelim;

                if (_cursor >= _stokenizer._origin.Length)
                {
                    return null;
                }

                c = _stokenizer._origin[_cursor];
                isDelim = _stokenizer._delim.IndexOf(c) != -1;

                if (isDelim)
                {
                    _cursor++;
                    if (_stokenizer._returnDelim)
                    {
                        return c.ToString();
                    }
                    return GetNext();
                }

                int nextDelimPos = _stokenizer._origin.IndexOfAny(_stokenizer._delim.ToCharArray(), _cursor);
                if (nextDelimPos == -1)
                {
                    nextDelimPos = _stokenizer._origin.Length;
                }

                string nextToken = _stokenizer._origin.Substring(_cursor, nextDelimPos - _cursor);
                _cursor = nextDelimPos;
                return nextToken;
            }
        }
    }

}
