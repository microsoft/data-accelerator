// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
import React from 'react';
import PropTypes from 'prop-types';
import * as Models from '../../flowModels';
import { DetailsList, DetailsListLayoutMode, Selection, SelectionMode, CheckboxVisibility, DefaultButton } from 'office-ui-fabric-react';
import { Colors, IconButtonStyles, PanelHeader, PanelHeaderButtons, ScrollableContentPane, StatementBox } from 'datax-common';
import RecurringScheduleSettings from './recurringScheduleSettings';
import OneTimeScheduleSettings from './oneTimeScheduleSettings';
import * as Styles from '../../../../common/styles';

const batchColumns = [
    {
        key: 'columnBatchName',
        name: 'Alias',
        fieldName: 'id',
        isResizable: true
    },
    {
        key: 'columnBatchType',
        name: 'Type',
        fieldName: 'typeDisplay',
        isResizable: true,
        minWidth: 100
    },
    {
        key: 'columnBatchStatus',
        name: 'Status',
        fieldName: 'status',
        isResizable: true,
        minWidth: 100
    }
];

export default class ScheduleSettingsContent extends React.Component {
    constructor(props) {
        super(props);

        this.batchSelection = new Selection({
            selectionMode: SelectionMode.single
        });

        this.batchTypeToRenderFuncMap = {
            [Models.batchTypeEnum.recurring]: batch => this.renderRecurringScheduleSettings(batch),
            [Models.batchTypeEnum.oneTime]: batch => this.renderOneTimeScheduleSettings(batch)
        };

        this.batchTypeToDisplayMap = {};
        Models.batchTypes.forEach(batchType => {
            this.batchTypeToDisplayMap[batchType.key] = batchType.name;
        });
    }

    render() {
        return (
            <div style={rootStyle}>
                <StatementBox
                    icon="SignOut"
                    statement="Schedule recurring and/or one time batch jobs. For the jobs scheduled correctly, the scheduler will create and start jobs. And once a job is created, the schedule associated with it will be disabled and read-only."
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

        const batchList = this.props.batchList;
        batchList.forEach(item => {
            item.typeDisplay = this.batchTypeToDisplayMap[item.type];
            item.status = !item.disabled ? 'Active' : 'Disabled';
        });

        return (
            <div style={leftPaneStyle}>
                <PanelHeader style={Styles.panelHeaderStyle}>Schedule Batch Jobs</PanelHeader>
                <PanelHeaderButtons style={panelHeaderButtonStyle}>{this.renderButtons()}</PanelHeaderButtons>

                <div style={leftPaneContentStyle}>
                    <div style={listContainerStyle}>
                        <DetailsList
                            items={batchList}
                            columns={batchColumns}
                            onActiveItemChanged={(item, index) => this.onSelectBatchItem(item, index)}
                            setKey="batchList"
                            layoutMode={DetailsListLayoutMode.justified}
                            selectionMode={SelectionMode.single}
                            selectionPreservedOnEmptyClick={true}
                            isHeaderVisible={true}
                            checkboxVisibility={CheckboxVisibility.hidden}
                            selection={this.batchSelection}
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
                <ScrollableContentPane backgroundColor={Colors.neutralLighterAlt}>{this.renderBatchSettings()}</ScrollableContentPane>
            </div>
        );
    }

    renderButtons() {
        return [this.renderAddBatchButton(), this.renderDeleteBatchButton()];
    }

    renderAddBatchButton() {
        let recurringExists = this.props.batchList.filter(b => b.type === Models.batchTypeEnum.recurring);

        const menuItems =
            !recurringExists || recurringExists.length < 1
                ? Models.batchTypes.map(batchType => {
                      return Object.assign({}, batchType, {
                          onClick: () => this.props.onNewBatch(batchType.key)
                      });
                  })
                : Models.batchTypes
                      .filter(batchType => batchType.key !== Models.batchTypeEnum.recurring)
                      .map(batchType => {
                          return Object.assign({}, batchType, {
                              onClick: () => this.props.onNewBatch(batchType.key)
                          });
                      });

        return (
            <DefaultButton
                key="newbatch"
                className="content-header-button"
                disabled={!this.props.addBatchButtonEnabled}
                title="Add new batch"
                menuProps={{ items: menuItems }}
            >
                <i
                    style={this.props.addBatchButtonEnabled ? IconButtonStyles.greenStyle : IconButtonStyles.disabledStyle}
                    className="ms-Icon ms-Icon--Add"
                />
                Add
            </DefaultButton>
        );
    }

    renderDeleteBatchButton() {
        const batch = this.getBatch();
        const enableButton = batch !== undefined && this.props.deleteBatchButtonEnabled;

        return (
            <DefaultButton
                key="deletebatch"
                className="content-header-button"
                disabled={!enableButton}
                title="Delete selected batch"
                onClick={() => this.props.onDeleteBatch(this.props.selectedBatchIndex)}
            >
                <i
                    style={enableButton ? IconButtonStyles.neutralStyle : IconButtonStyles.disabledStyle}
                    className="ms-Icon ms-Icon--Delete"
                />
                Delete
            </DefaultButton>
        );
    }

    renderBatchSettings() {
        const batch = this.getBatch();
        if (!batch) {
            return null;
        }

        if (batch.type in this.batchTypeToRenderFuncMap) {
            return this.batchTypeToRenderFuncMap[batch.type](batch);
        } else {
            alert('Not supported batch type');
            return null;
        }
    }

    renderRecurringScheduleSettings(batch) {
        return (
            <RecurringScheduleSettings
                batch={batch}
                batchTypeDisplayName={this.batchTypeToDisplayMap[batch.type]}
                onUpdateBatchName={this.props.onUpdateBatchName}
                onUpdateBatchStartTime={this.props.onUpdateBatchStartTime}
                onUpdateBatchEndTime={this.props.onUpdateBatchEndTime}
                onUpdateBatchIntervalValue={this.props.onUpdateBatchIntervalValue}
                onUpdateBatchIntervalType={this.props.onUpdateBatchIntervalType}
                onUpdateBatchDelayValue={this.props.onUpdateBatchDelayValue}
                onUpdateBatchDelayType={this.props.onUpdateBatchDelayType}
                onUpdateBatchWindowValue={this.props.onUpdateBatchWindowValue}
                onUpdateBatchWindowType={this.props.onUpdateBatchWindowType}
            />
        );
    }

    renderOneTimeScheduleSettings(batch) {
        return (
            <OneTimeScheduleSettings
                batch={batch}
                batchTypeDisplayName={this.batchTypeToDisplayMap[batch.type]}
                onUpdateBatchName={this.props.onUpdateBatchName}
                onUpdateBatchStartTime={this.props.onUpdateBatchStartTime}
                onUpdateBatchEndTime={this.props.onUpdateBatchEndTime}
                onUpdateBatchIntervalValue={this.props.onUpdateBatchIntervalValue}
                onUpdateBatchIntervalType={this.props.onUpdateBatchIntervalType}
                onUpdateBatchWindowValue={this.props.onUpdateBatchWindowValue}
                onUpdateBatchWindowType={this.props.onUpdateBatchWindowType}
            />
        );
    }

    onSelectBatchItem(item, index) {
        if (index !== this.props.selectedBatchIndex) {
            this.props.onUpdateSelectedBatchIndex(index);
        }
    }

    getBatch() {
        const batchList = this.props.batchList;
        if (this.props.selectedBatchIndex !== undefined && this.props.selectedBatchIndex < batchList.length) {
            return batchList[this.props.selectedBatchIndex];
        } else {
            return undefined;
        }
    }

    showSelectionInDetailsList() {
        const batchList = this.props.batchList;
        if (batchList.length > 0) {
            this.batchSelection.setChangeEvents(false, true);
            this.batchSelection.setItems(batchList);
            this.batchSelection.setIndexSelected(this.props.selectedBatchIndex, true, false);
            this.batchSelection.setChangeEvents(true, true);
        }
    }
}

// Props
ScheduleSettingsContent.propTypes = {
    batchList: PropTypes.array.isRequired,
    selectedBatchIndex: PropTypes.number,

    onNewBatch: PropTypes.func.isRequired,
    onDeleteBatch: PropTypes.func.isRequired,
    onUpdateSelectedBatchIndex: PropTypes.func.isRequired,

    addBatchButtonEnabled: PropTypes.bool.isRequired,
    deleteBatchButtonEnabled: PropTypes.bool.isRequired,

    // Schedule Settings
    onUpdateBatchName: PropTypes.func.isRequired,
    onUpdateBatchStartTime: PropTypes.func.isRequired,
    onUpdateBatchEndTime: PropTypes.func.isRequired,
    onUpdateBatchIntervalValue: PropTypes.func.isRequired,
    onUpdateBatchIntervalType: PropTypes.func.isRequired,
    onUpdateBatchDelayValue: PropTypes.func.isRequired,
    onUpdateBatchDelayType: PropTypes.func.isRequired,
    onUpdateBatchWindowValue: PropTypes.func.isRequired,
    onUpdateBatchWindowType: PropTypes.func.isRequired
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
