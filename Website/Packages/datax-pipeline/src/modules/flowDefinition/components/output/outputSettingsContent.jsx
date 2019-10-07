// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
import React from 'react';
import PropTypes from 'prop-types';
import * as Helpers from '../../flowHelpers';
import * as Models from '../../flowModels';
import { DetailsList, DetailsListLayoutMode, Selection, SelectionMode, CheckboxVisibility, DefaultButton } from 'office-ui-fabric-react';
import { Colors, IconButtonStyles, PanelHeader, PanelHeaderButtons, ScrollableContentPane, StatementBox } from 'datax-common';
import CosmosDbSinkerSettings from './cosmosdbSinkerSettings';
import EventHubSinkerSettings from './eventHubSinkerSettings';
import BlobSinkerSettings from './blobSinkerSettings';
import SqlSinkerSettings from './sqlSinkerSettings';
import MetricSinkerSettings from './metricSinkerSettings';
import LocalSinkerSettings from './localSinkerSettings';
import * as Styles from '../../../../common/styles';

const sinkerColumns = [
    {
        key: 'columnSinkerName',
        name: 'Alias',
        fieldName: 'id',
        isResizable: true
    },
    {
        key: 'columnSinkerType',
        name: 'Type',
        fieldName: 'typeDisplay',
        isResizable: true
    }
];

export default class OutputSettingsContent extends React.Component {
    constructor(props) {
        super(props);

        this.sinkerSelection = new Selection({
            selectionMode: SelectionMode.single
        });

        this.sinkerTypeToRenderFuncMap = {
            [Models.sinkerTypeEnum.cosmosdb]: sinker => this.renderCosmosDbSettings(sinker),
            [Models.sinkerTypeEnum.eventHub]: sinker => this.renderEventHubSettings(sinker),
            [Models.sinkerTypeEnum.blob]: sinker => this.renderBlobSettings(sinker),
            [Models.sinkerTypeEnum.sql]: sinker => this.renderSqlSettings(sinker),
            [Models.sinkerTypeEnum.metric]: sinker => this.renderMetricSettings(sinker),
            [Models.sinkerTypeEnum.local]: sinker => this.renderLocalSettings(sinker)
        };

        this.sinkerTypeToDisplayMap = {};
        Models.outputSinkerTypes.forEach(sinkerType => {
            this.sinkerTypeToDisplayMap[sinkerType.key] = sinkerType.name;
        });
        this.sinkerTypeToDisplayMap[Models.sinkerTypeEnum.metric] = Models.metricSinkerTypeName;
    }

    render() {
        return (
            <div style={rootStyle}>
                <StatementBox
                    icon="SignOut"
                    statement="Register all output sinks that your processing query script or rules you define may use."
                />
                {this.renderContent()}
            </div>
        );
    }

    renderContent() {
        return (
            <div style={contentStyle}>
                {this.renderLeftPane()}
                {this.renderRightPane()}
            </div>
        );
    }

    renderLeftPane() {
        this.showSelectionInDetailsList();

        const sinkers = this.props.sinkerItems;
        sinkers.forEach(item => {
            item.typeDisplay = this.sinkerTypeToDisplayMap[item.type];
        });

        return (
            <div style={leftPaneStyle}>
                <PanelHeader style={Styles.panelHeaderStyle}>Output Sinks</PanelHeader>
                <PanelHeaderButtons style={panelHeaderButtonStyle}>{this.renderButtons()}</PanelHeaderButtons>

                <div style={leftPaneContentStyle}>
                    <div style={listContainerStyle}>
                        <DetailsList
                            items={sinkers}
                            columns={sinkerColumns}
                            onActiveItemChanged={(item, index) => this.onSelectSinkerItem(item, index)}
                            setKey="sinkers"
                            layoutMode={DetailsListLayoutMode.justified}
                            selectionMode={SelectionMode.single}
                            selectionPreservedOnEmptyClick={true}
                            isHeaderVisible={true}
                            checkboxVisibility={CheckboxVisibility.hidden}
                            selection={this.sinkerSelection}
                        />
                    </div>
                </div>
            </div>
        );
    }

    renderRightPane() {
        return (
            <div style={rightPaneStyle}>
                <PanelHeader style={Styles.panelHeaderStyle}>Settings</PanelHeader>
                <ScrollableContentPane backgroundColor={Colors.neutralLighterAlt}>{this.renderSinkSettings()}</ScrollableContentPane>
            </div>
        );
    }

    renderButtons() {
        return [this.renderAddSinkerButton(), this.renderDeleteSinkerButton()];
    }

    renderAddSinkerButton() {
        const menuItems = this.props.enableLocalOneBox
            ? Models.outputSinkerTypes
                  .filter(sinkerType => sinkerType.name === 'Local')
                  .map(sinkerType => {
                      return Object.assign({}, sinkerType, {
                          onClick: () => this.props.onNewSinker(sinkerType.key)
                      });
                  })
            : Models.outputSinkerTypes
                  .filter(sinkerType => sinkerType.name !== 'Local')
                  .map(sinkerType => {
                      return Object.assign({}, sinkerType, {
                          onClick: () => this.props.onNewSinker(sinkerType.key)
                      });
                  });

        return (
            <DefaultButton
                key="newsink"
                className="content-header-button"
                disabled={!this.props.addOutputSinkButtonEnabled}
                title="Add new output sink"
                menuProps={{ items: menuItems }}
            >
                <i
                    style={this.props.addOutputSinkButtonEnabled ? IconButtonStyles.greenStyle : IconButtonStyles.disabledStyle}
                    className="ms-Icon ms-Icon--Add"
                />
                Add
            </DefaultButton>
        );
    }

    renderDeleteSinkerButton() {
        // enable only if there is a sinker selected and its not a metric sinker.
        // we do not allow the user to delete our provided metric sinker.
        const sinker = this.getSinker();
        const enableButton = sinker !== undefined && !Helpers.isMetricSinker(sinker) && this.props.deleteOutputSinkButtonEnabled;

        return (
            <DefaultButton
                key="deletesink"
                className="content-header-button"
                disabled={!enableButton}
                title="Delete selected output sink"
                onClick={() => this.props.onDeleteSinker(this.props.selectedSinkerIndex)}
            >
                <i
                    style={enableButton ? IconButtonStyles.neutralStyle : IconButtonStyles.disabledStyle}
                    className="ms-Icon ms-Icon--Delete"
                />
                Delete
            </DefaultButton>
        );
    }

    renderSinkSettings() {
        const sinker = this.getSinker();
        if (!sinker) {
            return null;
        }

        if (sinker.type in this.sinkerTypeToRenderFuncMap) {
            return this.sinkerTypeToRenderFuncMap[sinker.type](sinker);
        } else {
            alert('Not supported sinker type');
            return null;
        }
    }

    renderCosmosDbSettings(sinker) {
        return (
            <CosmosDbSinkerSettings
                sinker={sinker}
                sinkerDisplayName={this.sinkerTypeToDisplayMap[sinker.type]}
                onUpdateSinkerName={this.props.onUpdateSinkerName}
                onUpdateCosmosDbConnection={this.props.onUpdateCosmosDbConnection}
                onUpdateCosmosDbDatabase={this.props.onUpdateCosmosDbDatabase}
                onUpdateCosmosDbCollection={this.props.onUpdateCosmosDbCollection}
            />
        );
    }

    renderEventHubSettings(sinker) {
        return (
            <EventHubSinkerSettings
                sinker={sinker}
                sinkerDisplayName={this.sinkerTypeToDisplayMap[sinker.type]}
                onUpdateSinkerName={this.props.onUpdateSinkerName}
                onUpdateEventHubConnection={this.props.onUpdateEventHubConnection}
                onUpdateFormatType={this.props.onUpdateFormatType}
                onUpdateCompressionType={this.props.onUpdateCompressionType}
            />
        );
    }

    renderBlobSettings(sinker) {
        return (
            <BlobSinkerSettings
                sinker={sinker}
                sinkerDisplayName={this.sinkerTypeToDisplayMap[sinker.type]}
                onUpdateSinkerName={this.props.onUpdateSinkerName}
                onUpdateBlobConnection={this.props.onUpdateBlobConnection}
                onUpdateBlobContainerName={this.props.onUpdateBlobContainerName}
                onUpdateBlobPrefix={this.props.onUpdateBlobPrefix}
                onUpdateBlobPartitionFormat={this.props.onUpdateBlobPartitionFormat}
                onUpdateFormatType={this.props.onUpdateFormatType}
                onUpdateCompressionType={this.props.onUpdateCompressionType}
            />
        );
    }

    renderSqlSettings(sinker) {
        return (
            <SqlSinkerSettings
                sinker={sinker}
                sinkerDisplayName={this.sinkerTypeToDisplayMap[sinker.type]}
                onUpdateSinkerName={this.props.onUpdateSinkerName}
                onUpdateSqlConnection={this.props.onUpdateSqlConnection}
                onUpdateSqlTableName={this.props.onUpdateSqlTableName}
                onUpdateSqlWriteMode={this.props.onUpdateSqlWriteMode}
                onUpdateSqlUseBulkInsert={this.props.onUpdateSqlUseBulkInsert}
            />
        );
    }

    renderMetricSettings(sinker) {
        return (
            <MetricSinkerSettings flowId={this.props.flowId} sinker={sinker} sinkerDisplayName={this.sinkerTypeToDisplayMap[sinker.type]} />
        );
    }

    renderLocalSettings(sinker) {
        return (
            <LocalSinkerSettings
                sinker={sinker}
                sinkerDisplayName={this.sinkerTypeToDisplayMap[sinker.type]}
                onUpdateSinkerName={this.props.onUpdateSinkerName}
                onUpdateBlobConnection={this.props.onUpdateBlobConnection}
                onUpdateBlobPartitionFormat={this.props.onUpdateBlobPartitionFormat}
                onUpdateFormatType={this.props.onUpdateFormatType}
                onUpdateCompressionType={this.props.onUpdateCompressionType}
            />
        );
    }

    onSelectSinkerItem(item, index) {
        if (index !== this.props.selectedSinkerIndex) {
            this.props.onUpdateSelectedSinkerIndex(index);
        }
    }

    getSinker() {
        const sinkers = this.props.sinkerItems;
        if (this.props.selectedSinkerIndex !== undefined && this.props.selectedSinkerIndex < sinkers.length) {
            return sinkers[this.props.selectedSinkerIndex];
        } else {
            return undefined;
        }
    }

    showSelectionInDetailsList() {
        const sinkers = this.props.sinkerItems;
        if (sinkers.length > 0) {
            this.sinkerSelection.setChangeEvents(false, true);
            this.sinkerSelection.setItems(sinkers);
            this.sinkerSelection.setIndexSelected(this.props.selectedSinkerIndex, true, false);
            this.sinkerSelection.setChangeEvents(true, true);
        }
    }
}

// Props
OutputSettingsContent.propTypes = {
    flowId: PropTypes.string,
    sinkerItems: PropTypes.array.isRequired,
    selectedSinkerIndex: PropTypes.number,

    onNewSinker: PropTypes.func.isRequired,
    onDeleteSinker: PropTypes.func.isRequired,
    onUpdateSelectedSinkerIndex: PropTypes.func.isRequired,
    onUpdateSinkerName: PropTypes.func.isRequired,

    // Cosmos DB
    onUpdateCosmosDbConnection: PropTypes.func.isRequired,
    onUpdateCosmosDbDatabase: PropTypes.func.isRequired,
    onUpdateCosmosDbCollection: PropTypes.func.isRequired,

    // Event Hub and Azure Blob
    onUpdateFormatType: PropTypes.func.isRequired,
    onUpdateCompressionType: PropTypes.func.isRequired,

    // Event Hub
    onUpdateEventHubConnection: PropTypes.func.isRequired,

    // Azure Blob
    onUpdateBlobConnection: PropTypes.func.isRequired,
    onUpdateBlobContainerName: PropTypes.func.isRequired,
    onUpdateBlobPrefix: PropTypes.func.isRequired,
    onUpdateBlobPartitionFormat: PropTypes.func.isRequired,

    // Sql
    onUpdateSqlConnection: PropTypes.func.isRequired,
    onUpdateSqlTableName: PropTypes.func.isRequired,
    onUpdateSqlWriteMode: PropTypes.func.isRequired,
    onUpdateSqlUseBulkInsert: PropTypes.func.isRequired,

    addOutputSinkButtonEnabled: PropTypes.bool.isRequired,
    deleteOutputSinkButtonEnabled: PropTypes.bool.isRequired
};

// Styles
const rootStyle = {
    display: 'flex',
    flexDirection: 'column',
    overflowY: 'hidden',
    flex: 1
};

const contentStyle = {
    display: 'flex',
    flexDirection: 'row',
    overflowY: 'hidden',
    flex: 1
};

const panelHeaderButtonStyle = {
    backgroundColor: Colors.neutralLighter
};

const leftPaneStyle = {
    flex: 1,
    display: 'flex',
    flexDirection: 'column',
    borderRight: `1px solid ${Colors.neutralTertiaryAlt}`
};

const leftPaneContentStyle = {
    paddingLeft: 30,
    paddingRight: 30,
    paddingTop: 30,
    paddingBottom: 30,
    flex: 1,
    display: 'flex',
    flexDirection: 'column',
    overflowY: 'hidden'
};

const rightPaneStyle = {
    flex: 3,
    display: 'flex',
    flexDirection: 'column'
};

const listContainerStyle = {
    backgroundColor: Colors.white,
    border: `1px solid ${Colors.neutralTertiaryAlt}`,
    flex: 1,
    overflowY: 'auto'
};
