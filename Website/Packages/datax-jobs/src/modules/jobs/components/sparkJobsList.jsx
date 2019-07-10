// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
import React from 'react';
import { withRouter } from 'react-router';
import { DefaultButton, PrimaryButton, DetailsList, DetailsListLayoutMode, SelectionMode } from 'office-ui-fabric-react';
import { Dialog, DialogType, DialogFooter } from 'office-ui-fabric-react/lib/Dialog';
import { TooltipHost, TooltipDelay, DirectionalHint } from 'office-ui-fabric-react/lib/Tooltip';
import { Link } from 'office-ui-fabric-react/lib/Link';
import { JobState } from '../constants';
import * as Api from '../api';

function renderLinks(links) {
    if (links == null) {
        return [];
    }

    return Object.keys(links)
        .map(name => (
            <Link href={links[name]} key={name} target="_blank" rel="noopener noreferrer">
                {name}
            </Link>
        ))
        .reduce((accu, elem) => (accu.length === 0 ? [elem] : [...accu, ' | ', elem]), []);
}

const columnDefinition = [
    {
        key: 'colName',
        name: 'Name',
        fieldName: 'name',
        minWidth: 100,
        maxWidth: 300,
        isResizable: true
    },
    {
        key: 'colCluster',
        name: 'Cluster',
        fieldName: 'cluster',
        minWidth: 50,
        maxWidth: 100,
        isResizable: true
    },
    {
        key: 'colState',
        name: 'State',
        fieldName: 'state',
        minWidth: 20,
        maxWidth: 60,
        isResizable: true,
        onRender: item => getJobStateIcon(item)
    },
    {
        key: 'colLink',
        name: 'Link',
        minWidth: 50,
        maxWidth: 200,
        isResizable: true,
        onRender: item => <div>{renderLinks(item.links)}</div>
    },
    {
        key: 'colNote',
        name: 'Note',
        fieldName: 'note',
        minWidth: 100,
        maxWidth: 700,
        isMultiline: true,
        isResizable: true,
        onRender: (item, index) => {
            var contentList = item.note.split('\n');
            if (contentList.length == 0) return <div />;

            var id = 'noteTooltip' + index;
            return (
                <TooltipHost
                    calloutProps={{ gapSpace: 20 }}
                    tooltipProps={{
                        onRenderContent: () => (
                            <div>
                                <ul style={{ margin: 0, padding: 0 }}>
                                    {contentList.map((d, i) => (
                                        <li key={i}>{d}</li>
                                    ))}
                                </ul>
                            </div>
                        ),
                        maxWidth: 800
                    }}
                    delay={TooltipDelay.zero}
                    id={id}
                    directionalHint={DirectionalHint.bottomCenter}
                >
                    <span aria-describedby={id}>{contentList[contentList.length - 1]}</span>
                </TooltipHost>
            );
        }
    }
];

function getJobStateIcon(item) {
    const iconComponent = (icon, label, fontSize, rotate) => (
        <div style={{ display: 'flex', flexDirection: 'row', marginLeft: -5 }}>
            <div style={{ paddingRight: 5 }}>
                <i style={{ fontSize: fontSize }} className={`ms-Icon ms-Icon--${icon} ${rotate ? 'rotate-icon' : ''}`} />
            </div>
            {label}
        </div>
    );

    switch (item.state) {
        case JobState.Running:
            return iconComponent('Settings', 'running', 18, true);

        case JobState.Idle:
            return iconComponent('Hotel', 'idle', 15, false);

        default:
            return <div>{item.state}</div>;
    }
}

const StateOrder = { JobState };
StateOrder[JobState.Error] = 1;
StateOrder[JobState.Starting] = 2;
StateOrder[JobState.Running] = 3;
StateOrder[JobState.Idle] = 4;

function jobStateOrder(state) {
    if (!state || !(state in StateOrder)) return 0;
    return StateOrder[state];
}

function compareJobOrder(j1, j2) {
    if (j1.state == j2.state) {
        if (j1.name == j2.name) return 0;
        else return j1.name < j2.name ? -1 : 1;
    } else {
        return jobStateOrder(j1.state) - jobStateOrder(j2.state);
    }
}

class SparkJobsList extends React.Component {
    constructor() {
        super();

        this.state = {
            showConfirmationDialog: false,
            confirmationDisabled: false,
            jobActionsEnabled: false
        };
    }

    componentDidMount() {
        Api.functionEnabled().then(response => {
            this.setState({ jobActionsEnabled: response.jobActionsEnabled ? true : false });
        });
    }

    render() {
        return (
            <div>
                <DetailsList
                    items={(this.props.jobs || []).sort(compareJobOrder)}
                    columns={this.getColumns()}
                    setKey="set"
                    layoutMode={DetailsListLayoutMode.justified}
                    selectionMode={SelectionMode.none}
                    selectionPreservedOnEmptyClick={true}
                />

                <Dialog
                    hidden={!this.state.showConfirmationDialog}
                    dialogContentProps={{
                        type: DialogType.normal,
                        subText: this.state.confirmationMessage
                    }}
                    modalProps={{
                        isBlocking: true,
                        containerClassName: 'ms-dialogMainOverride'
                    }}
                    onDismiss={() => this.setState({ showConfirmationDialog: false })}
                    title="Confirmation"
                >
                    <div />
                    <DialogFooter>
                        <PrimaryButton onClick={this.state.confirmationHandler} text="Yes" disabled={this.state.confirmationDisabled} />
                        <DefaultButton
                            onClick={() => this.setState({ showConfirmationDialog: false })}
                            disabled={this.state.confirmationDisabled}
                            text="No"
                        />
                    </DialogFooter>
                </Dialog>
            </div>
        );
    }

    refreshItems() {
        this.props.refresh();
    }

    getFunctionHandler(action, message) {
        return (item, index) => {
            this.setState({
                showConfirmationDialog: true,
                confirmationHandler: () => {
                    this.setState({ confirmationDisabled: true });
                    action(item, index)
                        .then(() => {
                            this.setState({ showConfirmationDialog: false, confirmationDisabled: false });
                            this.refreshItems();
                        })
                        .catch(err => {
                            this.setState({ showConfirmationDialog: false, confirmationDisabled: false });
                            alert(err && err.message);
                        });
                },
                confirmationMessage: message(item, index)
            });
        };
    }

    getFunctionColumn(functionName, handler, available, iconName) {
        iconName = iconName || functionName;
        return {
            key: functionName,
            fieldName: functionName,
            name: '',
            minWidth: 20,
            maxWidth: 50,
            onRender: (item, index) => {
                if (available && !available(item, index)) {
                    return <div />;
                } else {
                    return (
                        <div>
                            <i
                                className={`clickable-noselect ms-Icon ms-Icon--${iconName}`}
                                aria-hidden="true"
                                onClick={handler.bind(this, item, index)}
                            />
                        </div>
                    );
                }
            }
        };
    }

    getColumns() {
        let columns = [].concat(columnDefinition);
        if (this.state.jobActionsEnabled) {
            columns.push(
                this.getFunctionColumn(
                    'Start',
                    this.getFunctionHandler(item => Api.startSparkJob(item.name), item => `Do you want to start job '${item.name}'?`),
                    item => item.state == JobState.Idle,
                    'TriangleSolidRight12'
                )
            );
            columns.push(
                this.getFunctionColumn(
                    'Stop',
                    this.getFunctionHandler(item => Api.stopSparkJob(item.name), item => `Do you want to stop job '${item.name}'?`),
                    item => item.state == JobState.Running,
                    'StopSolid'
                )
            );
            columns.push(
                this.getFunctionColumn(
                    'Restart',
                    this.getFunctionHandler(item => Api.restartSparkJob(item.name), item => `Do you want to restart job '${item.name}'?`),
                    item => item.state == JobState.Running,
                    'Refresh'
                )
            );
        }

        return columns;
    }
}

export default withRouter(SparkJobsList);
