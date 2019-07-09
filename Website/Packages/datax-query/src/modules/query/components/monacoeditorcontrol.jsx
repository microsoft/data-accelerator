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
import MonacoEditor from 'react-monaco-editor';

const MonacoEditorControl = Radium(
  class MonacoEditorControl extends React.Component {
        constructor(props) {
            super(props);

        }
        render() {
            return (
                <MonacoEditor 
                    name={this.props.name}
                    height={this.props.height} 
                    width={this.props.width} 
                    fontSize={this.props.fontSize} 
                    language={this.props.language}
                    theme={this.props.theme}
                    value={this.props.value} 
                    options={this.props.options}
                    onChange={this.props.onChange}
                    editorWillMount={this.props.editorWillMount }
                    editorDidMount={this.props.editorDidMount}
                /> 
            );
        }
    }
);


// Props
MonacoEditorControl.propTypes = {
    name: PropTypes.string.isRequired,
    height: PropTypes.string,
    width: PropTypes.string,
    fontSize: PropTypes.string,
    language: PropTypes.string,
    theme: PropTypes.string,
    value: PropTypes.string,
    options: PropTypes.object,
    onChange: PropTypes.func,
    editorWillMount: PropTypes.func,
    editorDidMount: PropTypes.func
};

export default MonacoEditorControl;
