// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
import React from 'react';
import { Colors } from 'datax-common';

const headerStyles = {
    textAlign: 'left',
    borderWidth: 1,
    borderColor: Colors.customDarkGray,
    borderStyle: 'solid'
};

const cellStyles = {
    borderWidth: 1,
    borderColor: Colors.customDarkGray,
    borderStyle: 'solid'
};

class MetricWidgetSimpleTable extends React.Component {
    constructor() {
        super();
    }

    renderHeaders(headers) {
        return headers.map((h, i) => (
            <th key={i} style={headerStyles}>
                {h}
            </th>
        ));
    }

    renderRow(row) {
        return row.map((d, i) => (
            <td key={i} style={cellStyles}>
                {String(d)}
            </td>
        ));
    }

    renderBody(body) {
        return body.map((d, i) => <tr key={i}>{this.renderRow(d)}</tr>);
    }

    renderTable(headers, body) {
        return (
            <table className="table">
                <thead>
                    <tr>{this.renderHeaders(headers)}</tr>
                </thead>
                <tbody>{this.renderBody(body)}</tbody>
            </table>
        );
    }

    render() {
        let data = this.props.value;
        let headers = [];
        let body = [];
        if (data && data.length > 0) {
            let series = data[0];
            if (series.length > 0) {
                headers = Object.keys(series[0]);
                body = series.map(r => headers.map(h => r[h]));
            }
        }

        return (
            <div>
                <h3 style={{ fontWeight: 'normal', color: `${Colors.themeDarker}` }} tabindex="0">{this.props.displayName}</h3>
                <div style={{ width: 'auto', height: 300 }}>{this.renderTable(headers, body)}</div>
            </div>
        );
    }
}

export default MetricWidgetSimpleTable;
