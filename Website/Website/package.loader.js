// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
/**
 * Compile time package registration.
 *
 * NOTE:
 * We have to use string literals in import parameter for static binding; it cannot be an expression/computed.
 * This is a limitation of dynamic import support.
 * Compilers use this information to determine how to break up the packages to smaller "chunk" files and how
 * to build a mapping table so that during runtime, it knows how to resolve packages we demand to be loaded based
 * on our applications triggering mechanisms.
 *
 */

// Register all CSS styles from packages we depend on
import 'datax-common/dist/css/index.css';
import 'datax-pipeline/dist/css/index.css';
import 'datax-jobs/dist/css/index.css';
import 'datax-metrics/dist/css/index.css';
import 'datax-home/dist/css/index.css';

// Image dependencies
require.context('datax-home/dist/img', false, /\.(svg)$/);

export default function loadPackage(packageName) {
    switch (packageName) {
        // Public packages
        case 'datax-pipeline':
            return () => import('datax-pipeline');
        case 'datax-jobs':
            return () => import('datax-jobs');
        case 'datax-metrics':
            return () => import('datax-metrics');
        case 'datax-home':
            return () => import('datax-home');

        // Add future package registration here...

        default:
            return undefined;
    }
}
