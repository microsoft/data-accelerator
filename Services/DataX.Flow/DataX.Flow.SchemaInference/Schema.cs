// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using System.Collections.Generic;

/// <summary>
/// Disable warning for the naming rule for this file: # Public members must be capitalized
/// </summary>
#pragma warning disable IDE1006
namespace DataX.Flow.SchemaInference
{
    public class Metadata
    {
    }

    public class Type
    {
        public string type { get; set; }
        public object elementType { get; set; }
        public bool containsNull { get; set; }

        public Type()
        {
            type = "array";
            containsNull = true;
        }
    }


    public class Field
    {
        public string name { get; set; }
        public object type { get; set; }
        public bool nullable { get; set; }
        public Metadata metadata { get; set; }

        public Field(string name, string type)
        {
            this.name = name;
            this.type = type;
            nullable = true;
            metadata = new Metadata();
        }

        public Field(string name, StructObject type)
        {
            this.name = name;
            this.type = type;
            nullable = true;
            metadata = new Metadata();
        }

        public Field(string name, Type type)
        {
            this.name = name;
            this.type = type;
            nullable = true;
            metadata = new Metadata();
        }
    }

    public class StructObject
    {
        public string type { get; set; }
        public List<Field> fields { get; set; }

        public StructObject()
        {
            type = "struct";
            fields = new List<Field>();
        }
    }

    public class SchemaResult
    {
        public string Schema { get; set; }
        public List<string> Errors { get; set; }
    }
}
