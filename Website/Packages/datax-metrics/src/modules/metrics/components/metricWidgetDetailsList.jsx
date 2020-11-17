// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
import React from 'react';
import * as moment from 'moment';
import { DetailsList, DetailsListLayoutMode, SelectionMode } from 'office-ui-fabric-react';
import { Colors } from 'datax-common';

const renderCell = (item, k) => {
    let d = item[k];
    if (moment.isDate(d)) {
        return new Date(d).toLocaleString();
    } else {
        return String(d);
    }
};

export default class MetricWidgetDetailsList extends React.Component {
    constructor() {
        super();
    }

    render() {
        let columns = [];
        let items = [];
        let data = this.props.value;
        if (data && data.length > 0) {
            let series = data[0];
            if (series.length > 0) {
                let firstRecord = series[0];
                if (Array.isArray(firstRecord)) {
                    columns = firstRecord.map((n, i) => ({
                        key: 'col' + i,
                        name: 'Column ' + String(i + 1),
                        fieldName: i,
                        minWidth: 100,
                        maxWidth: 150,
                        isResizable: true,
                        onRender: item => renderCell(item, i)
                    }));
                } else {
                    columns = Object.keys(firstRecord).map(n => ({
                        key: 'col' + n,
                        name: n,
                        fieldName: n,
                        minWidth: 100,
                        maxWidth: 150,
                        isResizable: true,
                        onRender: item => renderCell(item, n)
                    }));
                }

                items = series;
            }
        }

        return (
            <div>
                <h3 style={{ fontWeight: 'normal' }} tabindex="0">{this.props.displayName}</h3>
                <div style={{ width: 'auto', border: `1px solid ${Colors.customGray}` }}>
                    <DetailsList
                        items={items}
                        columns={columns}
                        setKey="set"
                        layoutMode={DetailsListLayoutMode.justified}
                        selectionMode={SelectionMode.none}
                    />
                </div>
            </div>
        );
    }
}
