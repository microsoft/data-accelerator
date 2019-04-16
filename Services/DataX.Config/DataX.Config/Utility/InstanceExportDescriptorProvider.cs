// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using System.Collections.Generic;
using System.Composition.Hosting.Core;

namespace DataX.Config.Utility
{
    /// <summary>
    /// Custom export descriptor provider class to add existing instances to MEF container
    /// </summary>
    public class InstanceExportDescriptorProvider : ExportDescriptorProvider
    {
        private readonly object[] _instances;

        public InstanceExportDescriptorProvider(object[] instances)
        {
            _instances = instances;
        }

        public override IEnumerable<ExportDescriptorPromise> GetExportDescriptors(CompositionContract contract, DependencyAccessor descriptorAccessor)
        {
            foreach (var instance in _instances)
            {
                if (contract.ContractType.IsInstanceOfType(instance))
                {
                    yield return new ExportDescriptorPromise(contract, contract.ContractType.FullName, true, NoDependencies, dependencies => ExportDescriptor.Create((context, operation) => instance, NoMetadata));
                }
            }
        }
    }
}
