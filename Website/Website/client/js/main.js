// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
import React from 'react';
import ReactDOM from 'react-dom';
import App from 'app';
import '../index.html';
import '../favicon.ico';
import '../css/global.css';

window.onload = () => {
    ReactDOM.render(<App />, document.getElementById('main'));
};
