// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************

import 'jsoneditor-react/es/editor.min.css';
import 'monaco-editor/min/vs/editor/editor.main.css';
import './styles/styles.css';
/**
 * Export all modules to expose out of this package library
 */
export * from './modules';
export { JsonEditor } from 'jsoneditor-react';
export { default as MonacoEditorControl} from './modules/query/components/monacoeditorcontrol'