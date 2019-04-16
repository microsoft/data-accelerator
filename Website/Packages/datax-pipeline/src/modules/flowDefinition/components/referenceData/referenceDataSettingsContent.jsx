// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
import React from 'react';
import PropTypes from 'prop-types';
import * as Models from '../../flowModels';
import { Colors, IconButtonStyles, PanelHeader, PanelHeaderButtons, ScrollableContentPane, StatementBox } from 'datax-common';
import { DetailsList, DetailsListLayoutMode, Selection, SelectionMode, CheckboxVisibility, DefaultButton } from 'office-ui-fabric-react';
import CsvReferenceDataSettings from './csvReferenceDataSettings';
import * as Styles from '../../../../common/styles';

const referenceDataColumns = [
    {
        key: 'columnReferenceDataName',
        name: 'Alias',
        fieldName: 'id',
        isResizable: true
    },
    {
        key: 'columnReferenceDataType',
        name: 'Type',
        fieldName: 'typeDisplay',
        isResizable: true
    }
];

export default class ReferenceDataSettingsContent extends React.Component {
    constructor(props) {
        super(props);

        this.referenceDataSelection = new Selection({
            selectionMode: SelectionMode.single
        });

        this.referenceDataTypeToRenderFuncMap = {
            [Models.referenceDataTypeEnum.csv]: referenceData => this.renderCsvSettings(referenceData)
        };

        this.referenceDataTypeToDisplayMap = {};
        Models.referenceDataTypes.forEach(referenceDataType => {
            this.referenceDataTypeToDisplayMap[referenceDataType.key] = referenceDataType.name;
        });
    }

    render() {
        return (
            <div style={rootStyle}>
                <StatementBox icon="Table" statement="Register all reference data that your processing query script may use." />
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

        const referenceDatas = this.props.referenceDataItems;
        referenceDatas.forEach(item => {
            item.typeDisplay = this.referenceDataTypeToDisplayMap[item.type];
        });

        return (
            <div style={leftPaneStyle}>
                <PanelHeader style={Styles.panelHeaderStyle}>Reference Data</PanelHeader>
                <PanelHeaderButtons style={panelHeaderButtonStyle}>{this.renderButtons()}</PanelHeaderButtons>

                <div style={leftPaneContentStyle}>
                    <div style={listContainerStyle}>
                        <DetailsList
                            items={referenceDatas}
                            columns={referenceDataColumns}
                            onActiveItemChanged={(item, index) => this.onSelectReferenceDataItem(item, index)}
                            setKey="referencedatalist"
                            layoutMode={DetailsListLayoutMode.justified}
                            selectionMode={SelectionMode.single}
                            selectionPreservedOnEmptyClick={true}
                            isHeaderVisible={true}
                            checkboxVisibility={CheckboxVisibility.hidden}
                            selection={this.referenceDataSelection}
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
                <ScrollableContentPane backgroundColor={Colors.neutralLighterAlt}>
                    {this.renderReferenceDataSettings()}
                </ScrollableContentPane>
            </div>
        );
    }

    renderButtons() {
        return [this.renderAddReferenceDataButton(), this.renderDeleteReferenceDataButton()];
    }

    renderAddReferenceDataButton() {
        const menuItems = Models.referenceDataTypes.map(referenceDataType => {
            return Object.assign({}, referenceDataType, {
                onClick: () => this.props.onNewReferenceData(referenceDataType.key)
            });
        });

        return (
            <DefaultButton
                key="newreferencedata"
                className="content-header-button"
                disabled={!this.props.addReferenceDataButtonEnabled}
                title="Add new reference data"
                menuProps={{ items: menuItems }}
            >
                <i
                    style={this.props.addReferenceDataButtonEnabled ? IconButtonStyles.greenStyle : IconButtonStyles.disabledStyle}
                    className="ms-Icon ms-Icon--Add"
                />
                Add
            </DefaultButton>
        );
    }

    renderDeleteReferenceDataButton() {
        const enableButton = this.props.selectedReferenceDataIndex !== undefined && this.props.deleteReferenceDataButtonEnabled;

        return (
            <DefaultButton
                key="deletereferencedata"
                className="content-header-button"
                disabled={!enableButton}
                title="Delete selected reference data"
                onClick={() => this.props.onDeleteReferenceData(this.props.selectedReferenceDataIndex)}
            >
                <i
                    style={enableButton ? IconButtonStyles.neutralStyle : IconButtonStyles.disabledStyle}
                    className="ms-Icon ms-Icon--Delete"
                />
                Delete
            </DefaultButton>
        );
    }

    renderReferenceDataSettings() {
        const referenceData = this.getReferenceData();
        if (!referenceData) {
            return null;
        }

        if (referenceData.type in this.referenceDataTypeToRenderFuncMap) {
            return this.referenceDataTypeToRenderFuncMap[referenceData.type](referenceData);
        } else {
            alert('Not supported reference data type');
            return null;
        }
    }

    renderCsvSettings(referenceData) {
        return (
            <CsvReferenceDataSettings
                referenceData={referenceData}
                referenceDataDisplayName={this.referenceDataTypeToDisplayMap[referenceData.type]}
                enableLocalOneBox={this.props.enableLocalOneBox}
                onUpdateReferenceDataName={this.props.onUpdateReferenceDataName}
                onUpdateCsvPath={this.props.onUpdateCsvPath}
                onUpdateCsvDelimiter={this.props.onUpdateCsvDelimiter}
                onUpdateCsvContainsHeader={this.props.onUpdateCsvContainsHeader}
            />
        );
    }

    onSelectReferenceDataItem(item, index) {
        if (index !== this.props.selectedReferenceDataIndex) {
            this.props.onUpdateSelectedReferenceDataIndex(index);
        }
    }

    getReferenceData() {
        const referenceDatas = this.props.referenceDataItems;
        if (this.props.selectedReferenceDataIndex !== undefined && this.props.selectedReferenceDataIndex < referenceDatas.length) {
            return referenceDatas[this.props.selectedReferenceDataIndex];
        } else {
            return undefined;
        }
    }

    showSelectionInDetailsList() {
        const referenceDatas = this.props.referenceDataItems;
        if (referenceDatas.length > 0) {
            this.referenceDataSelection.setChangeEvents(false, true);
            this.referenceDataSelection.setItems(referenceDatas);
            this.referenceDataSelection.setIndexSelected(this.props.selectedReferenceDataIndex, true, false);
            this.referenceDataSelection.setChangeEvents(true, true);
        }
    }
}

// Props
ReferenceDataSettingsContent.propTypes = {
    referenceDataItems: PropTypes.array.isRequired,
    selectedReferenceDataIndex: PropTypes.number,

    onNewReferenceData: PropTypes.func.isRequired,
    onDeleteReferenceData: PropTypes.func.isRequired,
    onUpdateSelectedReferenceDataIndex: PropTypes.func.isRequired,
    onUpdateReferenceDataName: PropTypes.func.isRequired,

    // CSV
    onUpdateCsvPath: PropTypes.func.isRequired,
    onUpdateCsvDelimiter: PropTypes.func.isRequired,
    onUpdateCsvContainsHeader: PropTypes.func.isRequired,

    addReferenceDataButtonEnabled: PropTypes.bool.isRequired,
    deleteReferenceDataButtonEnabled: PropTypes.bool.isRequired
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
