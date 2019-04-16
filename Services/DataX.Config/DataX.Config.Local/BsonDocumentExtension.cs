// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using LiteDB;
using System;
using System.Linq;

namespace DataX.Config.Local
{

    /* 
    * This function is a modified version of source code located at https://github.com/mbdavid/LiteDB/blob/master/LiteDB/Document/BsonDocument.cs
    * Copy of the MIT license covering LiteDB sources can be obtained from https://opensource.org/licenses/MIT
    * */
    /// <summary>
    ///  Extension method for LiteDb.BsonDocument to update the field.
    ///  If field doesn't exist, a new field is added.
    /// </summary>
    public static class BsonDocumentExtension
    {
        public static bool SetField(this BsonDocument doc, string path, BsonValue value)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException(nameof(value));
            }
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            var field = path.StartsWith("$") ? path : "$." + path;
            var parent = field.Substring(0, field.LastIndexOf('.'));
            var key = field.Substring(field.LastIndexOf('.') + 1);
            var expr = new BsonExpression(parent);
            var changed = false;

            foreach (var item in expr.Execute(doc, false).Where(x => x.IsDocument))
            {
                var idoc = item.AsDocument;
                var cur = idoc[key];
                idoc[key] = value;
                changed = true;
            }

            return changed;
        }
    }
}
