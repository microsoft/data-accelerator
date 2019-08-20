// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************

using Microsoft.Azure.Cosmos.Table;
using System;
using System.Collections.Generic;

namespace DataX.Utilities
{
    /// <summary>
    /// CosmosTable base class
    /// </summary>
    public class BaseTableEntity : TableEntity
    {
        // azure storage limits
        private const long _MAX_FIELD_LENGTH = 65536; // 64KB
        private const long _MAX_TOTAL_FIELD_LENGTH = 1048576; // 1MB

        public override IDictionary<string, EntityProperty> WriteEntity(OperationContext operationContext)
        {

            var properties = base.WriteEntity(operationContext);

            var totalLength = 0;
            foreach (var property in properties)
            {
                if (property.Value.PropertyType == EdmType.String && property.Value.StringValue != null)
                {
                    var value = property.Value.StringValue;
                    totalLength += value.Length;
                    if (value.Length > _MAX_FIELD_LENGTH)
                    {
                        var e = new ArgumentException("The maximum length of a single field (64KB) has been exceeded", property.Key);
                        throw e;
                    }
                    else if (totalLength > _MAX_TOTAL_FIELD_LENGTH)
                    {
                        throw new ArgumentException("The maximum length of all fields (1MB) has been exceeded");
                    }
                }
            }

            return properties;
        }
    }
}
