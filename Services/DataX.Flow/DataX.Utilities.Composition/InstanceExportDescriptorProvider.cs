// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using System;
using System.Collections.Generic;
using System.Composition.Hosting.Core;

namespace DataX.Utilities.Composition
{
    /// <summary>
    /// Custom export descriptor provider class to add existing instances to MEF container
    /// </summary>
    public class InstanceExportDescriptorProvider<TValue> : ExportDescriptorProvider
    {
        private readonly TValue _exportedInstance;

        public InstanceExportDescriptorProvider(TValue exportedInstance)
        {
            _exportedInstance = exportedInstance;
        }

        public override IEnumerable<ExportDescriptorPromise> GetExportDescriptors(CompositionContract contract, DependencyAccessor descriptorAccessor)
        {
            object[] obj = null;
            Type type = _exportedInstance.GetType();            
            if (!type.IsArray)
            {
                obj = new object[] { _exportedInstance };
            }
            else
            {
                obj = (object[])Convert.ChangeType(_exportedInstance, typeof(object[]));
            }
            foreach (var instance in obj)
            {
                if (contract.ContractType.IsInstanceOfType(instance))
                {
                    yield return new ExportDescriptorPromise(contract, instance.ToString(), true, NoDependencies, _ =>
                        ExportDescriptor.Create((c, o) => instance, new Dictionary<string, object>()));
                }
            }            
        }
    }
}
