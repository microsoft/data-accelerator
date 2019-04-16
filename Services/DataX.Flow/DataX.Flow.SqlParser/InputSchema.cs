// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using System.Collections.Generic;

/// <summary>
/// Diable warning for the naming rule for this file: # Public members must be capitalized
/// </summary>
#pragma warning disable IDE1006
namespace DataX.Flow.SqlParser
{
    public class Metadata
    {
    }

    public class Field
    {
        public string name { get; set; }
        public object type { get; set; }
        public bool nullable { get; set; }
        public Metadata metadata { get; set; }
    }

    public class InputSchema
    {
        public string type { get; set; }
        public List<Field> fields { get; set; }
    }
}
