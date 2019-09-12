// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
import React from 'react';
import PropTypes from 'prop-types';
import * as Models from '../../flowModels';
import { DetailsList, DetailsListLayoutMode, Selection, SelectionMode, CheckboxVisibility, DefaultButton } from 'office-ui-fabric-react';
import { Colors, IconButtonStyles, PanelHeader, PanelHeaderButtons, ScrollableContentPane, StatementBox } from 'datax-common';
import UdfSettings from './udfSettings';
import AzureFunctionSettings from './azureFunctionSettings';
import * as Styles from '../../../../common/styles';

const functionColumns = [
    {
        key: 'columnFunctionName',
        name: 'Alias',
        fieldName: 'id',
        isResizable: true
    },
    {
        key: 'columnFunctionType',
        name: 'Type',
        fieldName: 'typeDisplay',
        isResizable: true
    }
];

export default class FunctionSettingsContent extends React.Component {
    constructor(props) {
        super(props);

        this.functionSelection = new Selection({
            selectionMode: SelectionMode.single
        });

        this.functionTypeToRenderFuncMap = {
            [Models.functionTypeEnum.udf]: functionItem => this.renderUdfSettings(functionItem),
            [Models.functionTypeEnum.udaf]: functionItem => this.renderUdfSettings(functionItem),
            [Models.functionTypeEnum.azureFunction]: functionItem => this.renderAzureFunctionSettings(functionItem)
        };

        this.functionTypeToDisplayMap = {};
        Models.functionTypes.forEach(functionType => {
            this.functionTypeToDisplayMap[functionType.key] = functionType.name;
        });
    }

    render() {
        return (
            <div style={rootStyle}>
                <StatementBox icon="Embed" statement="Register all functions that your processing query script may later use." />
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

        const functions = this.props.functionItems;
        functions.forEach(item => {
            item.typeDisplay = this.functionTypeToDisplayMap[item.type];
        });

        return (
            <div style={leftPaneStyle}>
                <PanelHeader style={Styles.panelHeaderStyle}>Functions</PanelHeader>
                <PanelHeaderButtons style={panelHeaderButtonStyle}>{this.renderButtons()}</PanelHeaderButtons>

                <div style={leftPaneContentStyle}>
                    <div style={listContainerStyle}>
                        <DetailsList
                            items={functions}
                            columns={functionColumns}
                            onActiveItemChanged={(item, index) => this.onSelectFunctionItem(item, index)}
                            setKey="functions"
                            layoutMode={DetailsListLayoutMode.justified}
                            selectionMode={SelectionMode.single}
                            selectionPreservedOnEmptyClick={true}
                            isHeaderVisible={true}
                            checkboxVisibility={CheckboxVisibility.hidden}
                            selection={this.functionSelection}
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
                <ScrollableContentPane backgroundColor={Colors.neutralLighterAlt}>{this.renderFunctionSettings()}</ScrollableContentPane>
            </div>
        );
    }

    renderButtons() {
        return [this.renderAddFunctionButton(), this.renderDeleteFunctionButton()];
    }

    renderAddFunctionButton() {
        const menuItems = this.props.enableLocalOneBox
            ? Models.functionTypes
                  .filter(functionType => functionType.name !== 'Azure Function') // disable Azure function in OneBox mode
                  .map(functionType => {
                      return Object.assign({}, functionType, {
                          onClick: () => this.props.onNewFunction(functionType.key)
                      });
                  })
            : Models.functionTypes.map(functionType => {
                  return Object.assign({}, functionType, {
                      onClick: () => this.props.onNewFunction(functionType.key)
                  });
              });

        return (
            <DefaultButton
                key="newfunction"
                className="content-header-button"
                disabled={!this.props.addFunctionButtonEnabled}
                title="Add new function"
                menuProps={{ items: menuItems }}
            >
                <i
                    style={this.props.addFunctionButtonEnabled ? IconButtonStyles.greenStyle : IconButtonStyles.disabledStyle}
                    className="ms-Icon ms-Icon--Add"
                />
                Add
            </DefaultButton>
        );
    }

    renderDeleteFunctionButton() {
        const enableButton = this.props.selectedFunctionIndex !== undefined && this.props.deleteFunctionButtonEnabled;

        return (
            <DefaultButton
                key="deletefunction"
                className="content-header-button"
                disabled={!enableButton}
                title="Delete selected function"
                onClick={() => this.props.onDeleteFunction(this.props.selectedFunctionIndex)}
            >
                <i
                    style={enableButton ? IconButtonStyles.neutralStyle : IconButtonStyles.disabledStyle}
                    className="ms-Icon ms-Icon--Delete"
                />
                Delete
            </DefaultButton>
        );
    }

    renderFunctionSettings() {
        const functionItem = this.getFunction();
        if (!functionItem) {
            return null;
        }

        if (functionItem.type in this.functionTypeToRenderFuncMap) {
            return this.functionTypeToRenderFuncMap[functionItem.type](functionItem);
        } else {
            alert('Not supported function type');
            return null;
        }
    }

    renderUdfSettings(functionItem) {
        return (
            <UdfSettings
                functionItem={functionItem}
                functionDisplayName={this.functionTypeToDisplayMap[functionItem.type]}
                enableLocalOneBox={this.props.enableLocalOneBox}
                onUpdateFunctionName={this.props.onUpdateFunctionName}
                onUpdateUdfPath={this.props.onUpdateUdfPath}
                onUpdateUdfClass={this.props.onUpdateUdfClass}
                onUpdateUdfDependencyLibs={this.props.onUpdateUdfDependencyLibs}
            />
        );
    }

    renderAzureFunctionSettings(functionItem) {
        return (
            <AzureFunctionSettings
                functionItem={functionItem}
                functionDisplayName={this.functionTypeToDisplayMap[functionItem.type]}
                onUpdateFunctionName={this.props.onUpdateFunctionName}
                onUpdateAzureFunctionServiceEndpoint={this.props.onUpdateAzureFunctionServiceEndpoint}
                onUpdateAzureFunctionApi={this.props.onUpdateAzureFunctionApi}
                onUpdateAzureFunctionCode={this.props.onUpdateAzureFunctionCode}
                onUpdateAzureFunctionMethodType={this.props.onUpdateAzureFunctionMethodType}
                onUpdateAzureFunctionParams={this.props.onUpdateAzureFunctionParams}
            />
        );
    }

    onSelectFunctionItem(item, index) {
        if (index !== this.props.selectedFunctionIndex) {
            this.props.onUpdateSelectedFunctionIndex(index);
        }
    }

    getFunction() {
        const functions = this.props.functionItems;
        if (this.props.selectedFunctionIndex !== undefined && this.props.selectedFunctionIndex < functions.length) {
            return functions[this.props.selectedFunctionIndex];
        } else {
            return undefined;
        }
    }

    showSelectionInDetailsList() {
        const functions = this.props.functionItems;
        if (functions.length > 0) {
            this.functionSelection.setChangeEvents(false, true);
            this.functionSelection.setItems(functions);
            this.functionSelection.setIndexSelected(this.props.selectedFunctionIndex, true, false);
            this.functionSelection.setChangeEvents(true, true);
        }
    }
}

// Props
FunctionSettingsContent.propTypes = {
    functionItems: PropTypes.array.isRequired,
    selectedFunctionIndex: PropTypes.number,

    onNewFunction: PropTypes.func.isRequired,
    onDeleteFunction: PropTypes.func.isRequired,
    onUpdateSelectedFunctionIndex: PropTypes.func.isRequired,
    onUpdateFunctionName: PropTypes.func.isRequired,

    // UDF/UDAF
    onUpdateUdfPath: PropTypes.func.isRequired,
    onUpdateUdfClass: PropTypes.func.isRequired,
    onUpdateUdfDependencyLibs: PropTypes.func.isRequired,

    // Azure Functions
    onUpdateAzureFunctionServiceEndpoint: PropTypes.func.isRequired,
    onUpdateAzureFunctionApi: PropTypes.func.isRequired,
    onUpdateAzureFunctionCode: PropTypes.func.isRequired,
    onUpdateAzureFunctionMethodType: PropTypes.func.isRequired,
    onUpdateAzureFunctionParams: PropTypes.func.isRequired,

    addFunctionButtonEnabled: PropTypes.bool.isRequired,
    deleteFunctionButtonEnabled: PropTypes.bool.isRequired
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
