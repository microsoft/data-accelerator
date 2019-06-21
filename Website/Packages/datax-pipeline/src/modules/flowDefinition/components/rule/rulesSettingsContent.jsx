// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
import React from 'react';
import PropTypes from 'prop-types';
import * as Helpers from '../../flowHelpers';
import * as Models from '../../flowModels';
import { DetailsList, DetailsListLayoutMode, Selection, SelectionMode, CheckboxVisibility, DefaultButton } from 'office-ui-fabric-react';
import { MessageBar, MessageBarType } from 'office-ui-fabric-react/lib/MessageBar';
import { Colors, IconButtonStyles, PanelHeader, PanelHeaderButtons, ScrollableContentPane, StatementBox } from 'datax-common';
import TagRuleSettings from './tagRuleSettings';
import * as Styles from '../../../../common/styles';

const ruleColumns = [
    {
        key: 'columnRuleName',
        name: 'Description',
        fieldName: 'id',
        isResizable: true
    },
    {
        key: 'columnRuleType',
        name: 'Type',
        fieldName: 'typeDisplay',
        isResizable: true
    }
];

export default class RulesSettingsContent extends React.Component {
    constructor(props) {
        super(props);

        this.ruleSelection = new Selection({
            selectionMode: SelectionMode.single
        });

        this.ruleTypeToRenderFuncMap = {
            [Models.ruleTypeEnum.tag]: rule => this.renderTagRuleSettings(rule)
        };

        this.ruleTypeToDisplayMap = {};
        Models.ruleTypes.forEach(ruleType => {
            this.ruleTypeToDisplayMap[ruleType.key] = ruleType.name;
        });

        this.state = {
            tableToSchemaMap: {}
        };
    }

    componentDidMount() {
        this.props.onGetTableSchemas().then(tableToSchemaMap => {
            this.setState({ tableToSchemaMap: tableToSchemaMap });
        });
    }

    render() {
        return (
            <div style={rootStyle}>
                <StatementBox
                    icon="NumberedList"
                    statement="Register all rules to apply to your processing. They are applied when calling built-in functions in your query (e.g. ProcessRules and ProcessAggregateRules)"
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

        const rules = this.props.ruleItems;
        rules.forEach(item => {
            item.typeDisplay = this.ruleTypeToDisplayMap[item.type];
        });

        return (
            <div style={leftPaneStyle}>
                <PanelHeader style={Styles.panelHeaderStyle}>Rules</PanelHeader>
                <PanelHeaderButtons style={panelHeaderButtonStyle}>{this.renderButtons()}</PanelHeaderButtons>

                <div style={leftPaneContentStyle}>
                    <div style={listContainerStyle}>
                        <DetailsList
                            items={rules}
                            columns={ruleColumns}
                            onActiveItemChanged={(item, index) => this.onSelectRuleItem(item, index)}
                            setKey="rules"
                            layoutMode={DetailsListLayoutMode.justified}
                            selectionMode={SelectionMode.single}
                            selectionPreservedOnEmptyClick={true}
                            isHeaderVisible={true}
                            checkboxVisibility={CheckboxVisibility.hidden}
                            selection={this.ruleSelection}
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
                {this.renderWarningBar()}
                <ScrollableContentPane backgroundColor={Colors.neutralLighterAlt}>{this.renderRuleSettings()}</ScrollableContentPane>
            </div>
        );
    }

    renderButtons() {
        return [this.renderAddRuleButton(), this.renderDeleteRuleButton()];
    }

    renderAddRuleButton() {
        const menuItems = Models.ruleTypes.map(ruleType => {
            return Object.assign({}, ruleType, {
                onClick: () => this.props.onNewRule(ruleType.key)
            });
        });

        return (
            <DefaultButton
                key="newrule"
                className="content-header-button"
                disabled={!this.props.addRuleButtonEnabled}
                title="Add new rule"
                menuProps={{ items: menuItems }}
            >
                <i
                    style={this.props.addRuleButtonEnabled ? IconButtonStyles.greenStyle : IconButtonStyles.disabledStyle}
                    className="ms-Icon ms-Icon--Add"
                />
                Add
            </DefaultButton>
        );
    }

    renderDeleteRuleButton() {
        const enableButton = this.props.selectedRuleIndex !== undefined && this.props.deleteRuleButtonEnabled;

        return (
            <DefaultButton
                key="deleterule"
                className="content-header-button"
                disabled={!enableButton}
                title="Delete selected rule"
                onClick={() => this.props.onDeleteRule(this.props.selectedRuleIndex)}
            >
                <i
                    style={enableButton ? IconButtonStyles.neutralStyle : IconButtonStyles.disabledStyle}
                    className="ms-Icon ms-Icon--Delete"
                />
                Delete
            </DefaultButton>
        );
    }

    renderRuleSettings() {
        const rule = this.getRule();
        if (!rule) {
            return null;
        }

        if (rule.type in this.ruleTypeToRenderFuncMap) {
            return this.ruleTypeToRenderFuncMap[rule.type](rule);
        } else {
            alert('Not supported rule type');
            return null;
        }
    }

    renderTagRuleSettings(rule) {
        return (
            <TagRuleSettings
                rule={rule}
                sinkers={this.props.sinkers}
                tableToSchemaMap={this.state.tableToSchemaMap}
                onUpdateTagRuleSubType={this.props.onUpdateTagRuleSubType}
                onUpdateTagRuleDescription={this.props.onUpdateTagRuleDescription}
                onUpdateTagTag={this.props.onUpdateTagTag}
                onUpdateTagIsAlert={this.props.onUpdateTagIsAlert}
                onUpdateTagSinks={this.props.onUpdateTagSinks}
                onUpdateTagSeverity={this.props.onUpdateTagSeverity}
                onUpdateTagConditions={this.props.onUpdateTagConditions}
                onUpdateTagAggregates={this.props.onUpdateTagAggregates}
                onUpdateTagPivots={this.props.onUpdateTagPivots}
                onUpdateSchemaTableName={this.props.onUpdateSchemaTableName}
            />
        );
    }

    renderWarningBar() {
        const rule = this.getRule();
        if (rule && rule.type === Models.ruleTypeEnum.tag) {
            const warningMessage = Helpers.validateConditions(rule.properties.conditions, rule.properties.ruleType, true, true);
            if (warningMessage !== undefined) {
                return (
                    <div style={messageBarStyle}>
                        <MessageBar messageBarType={MessageBarType.warning}>{warningMessage}</MessageBar>
                    </div>
                );
            } else {
                return null;
            }
        } else {
            return null;
        }
    }

    onSelectRuleItem(item, index) {
        if (index !== this.props.selectedRuleIndex) {
            this.props.onUpdateSelectedRuleIndex(index);
        }
    }

    getRule() {
        const rules = this.props.ruleItems;
        if (this.props.selectedRuleIndex !== undefined && this.props.selectedRuleIndex < rules.length) {
            return rules[this.props.selectedRuleIndex];
        } else {
            return undefined;
        }
    }

    showSelectionInDetailsList() {
        const rules = this.props.ruleItems;
        if (rules.length > 0) {
            this.ruleSelection.setChangeEvents(false, true);
            this.ruleSelection.setItems(rules);
            this.ruleSelection.setIndexSelected(this.props.selectedRuleIndex, true, false);
            this.ruleSelection.setChangeEvents(true, true);
        }
    }
}

// Props
RulesSettingsContent.propTypes = {
    ruleItems: PropTypes.array.isRequired,
    selectedRuleIndex: PropTypes.number,
    sinkers: PropTypes.array.isRequired,

    onNewRule: PropTypes.func.isRequired,
    onDeleteRule: PropTypes.func.isRequired,
    onUpdateSelectedRuleIndex: PropTypes.func.isRequired,
    onUpdateRuleName: PropTypes.func.isRequired,
    onGetTableSchemas: PropTypes.func.isRequired,

    // Tag
    onUpdateTagRuleSubType: PropTypes.func.isRequired,
    onUpdateTagRuleDescription: PropTypes.func.isRequired,
    onUpdateTagTag: PropTypes.func.isRequired,
    onUpdateTagIsAlert: PropTypes.func.isRequired,
    onUpdateTagSinks: PropTypes.func.isRequired,
    onUpdateTagSeverity: PropTypes.func.isRequired,
    onUpdateTagConditions: PropTypes.func.isRequired,
    onUpdateTagAggregates: PropTypes.func.isRequired,
    onUpdateTagPivots: PropTypes.func.isRequired,
    onUpdateSchemaTableName: PropTypes.func.isRequired,

    addRuleButtonEnabled: PropTypes.bool.isRequired,
    deleteRuleButtonEnabled: PropTypes.bool.isRequired
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
    flex: 1
};

const messageBarStyle = {
    display: 'block'
};
