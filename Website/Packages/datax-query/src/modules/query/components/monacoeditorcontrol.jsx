// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************

/**
 * Adding control MonacoEditorControl - This can be consumed by the consumers of datax-query without taking any dependency on monaco-editor packages
 */
import React from 'react';
import Radium from 'radium';
import PropTypes from 'prop-types';
import {MonacoEditor} from 'react-monaco-editor';

const MonacoEditorControl = Radium(
    class MonacoEditorControl extends React.Component {
        render() {
            if (!this.state.showNormalizationSnippet) {
                return null;
            }        
            return (
                    <div style={editorContainerStyle}>
                        <MonacoEditor
                            name="monacoeditor"
                            height="100%"
                            width="100%"
                            fontSize="13px"
                            language="sql"
                            value=""                            
                            options={{}}                                
                        />
                    </div>                
                    );
                }
           })
// Props
MonacoEditorControl.propTypes = {
    style: PropTypes.object
};

export default MonacoEditorControl;

// Styles
const editorContainerStyle = {
    display: 'flex',
    flexDirection: 'column',
    flex: 1,
    border: `1px solid ${Colors.neutralTertiaryAlt}`
};