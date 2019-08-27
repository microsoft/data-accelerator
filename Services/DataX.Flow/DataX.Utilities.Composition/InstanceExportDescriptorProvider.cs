// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
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
            if (contract.ContractType == typeof(TValue))
            {
                yield return new ExportDescriptorPromise(contract, _exportedInstance.ToString(), true, NoDependencies, _ =>
                    ExportDescriptor.Create((c, o) => _exportedInstance, new Dictionary<string, object>()));
            }
        }
    }
}
