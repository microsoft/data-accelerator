// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
import React from 'react';
import PropTypes from 'prop-types';
import { connect } from 'react-redux';
import { withRouter } from 'react-router';
import * as Actions from '../actions';
import * as Selectors from '../selectors';
import {
    DefaultButton,
    SearchBox,
    DetailsList,
    DetailsListLayoutMode,
    SelectionMode,
    CheckboxVisibility,
    MessageBar,
    MessageBarType
} from 'office-ui-fabric-react';
import { Colors, PageHeader, Panel, PanelHeaderButtons, LoadingPanel, IconButtonStyles } from 'datax-common';
import { functionEnabled } from '../../../common/api';

const columns = [
    {
        key: 'columnDisplayName',
        name: 'Name',
        fieldName: 'displayName',
        minWidth: 100,
        maxWidth: 300,
        isResizable: true
    },
    {
        key: 'ownerName',
        name: 'Creator',
        fieldName: 'owner',
        minWidth: 100,
        maxWidth: 300,
        isResizable: true
    }
];

class FlowListPanel extends React.Component {
    constructor(props) {
        super(props);

        this.state = {
            filter: null,
            showMessageBar: true,
            newFlowButtonEnabled: false
        };
    }

    componentDidMount() {
        this.props.onGetFlowsList();

        functionEnabled().then(response => {
            this.setState({ newFlowButtonEnabled: response.newFlowButtonEnabled ? true : false });
        });
    }

    render() {
        return (
            <Panel>
                <div role='banner'>
                    <PageHeader>Flows</PageHeader>
                </div>
                <PanelHeaderButtons>{this.renderNewButton()}</PanelHeaderButtons>
                {this.renderMessageBar()}
                {this.renderContent()}
            </Panel>
        );
    }

    renderContent() {
        if (!this.props.errorMessage) {
            if (this.props.flowslist) {
                const filter = this.state.filter;
                const items = filter ? this.props.flowslist.filter(i => i.name.toLowerCase().indexOf(filter) > -1) : this.props.flowslist;

                return (
                    <div style={contentStyle} role="main">
                        <div style={filterContainerStyle}>
                            <SearchBox
                                className="filter-box"
                                placeholder="Filter"
                                onChanged={filterValue => this.onUpdateFilter(filterValue)}
                            />
                        </div>

                        <div style={listContainerStyle}>
                            <DetailsList
                                items={items}
                                columns={columns}
                                onItemInvoked={item => this.onEditItem(item)}
                                setKey="flowslist"
                                layoutMode={DetailsListLayoutMode.justified}
                                selectionMode={SelectionMode.single}
                                selectionPreservedOnEmptyClick={true}
                                isHeaderVisible={true}
                                checkboxVisibility={CheckboxVisibility.hidden}
                            />
                        </div>
                    </div>
                );
            } else {
                return <LoadingPanel showImmediately={true} />;
            }
        }
    }

    renderMessageBar() {
        if (this.props.errorMessage && this.state.showMessageBar) {
            return (
                <MessageBar messageBarType={MessageBarType.error} onDismiss={() => this.onDismissMessageBar()}>
                    Error - {`${this.props.errorMessage}`}
                </MessageBar>
            );
        }
    }

    onDismissMessageBar() {
        this.setState({
            showMessageBar: false
        });
    }

    renderNewButton() {
        if (!this.props.errorMessage) {
            return (
                <DefaultButton
                    role="navigation"
                    key="new"
                    className="header-button"
                    title="Add new DataX Flow"
                    disabled={!this.state.newFlowButtonEnabled}
                    onClick={() => this.onNewItem()}
                >
                    <i
                        style={this.state.newFlowButtonEnabled ? IconButtonStyles.greenStyle : IconButtonStyles.disabledStyle}
                        className="ms-Icon ms-Icon--Add"
                    />
                    New
                </DefaultButton>
            );
        }
    }

    onUpdateFilter(text) {
        this.setState({ filter: text });
    }

    onNewItem() {
        if (this.props.newItemPath) {
            this.props.history.push(this.props.newItemPath);
        }
    }

    onEditItem(item) {
        if (this.props.editItemPath) {
            this.props.history.push(`${this.props.editItemPath}/${item.name}`);
        }
    }
}

// Props
FlowListPanel.propTypes = {
    newItemPath: PropTypes.string.isRequired,
    editItemPath: PropTypes.string.isRequired
};

// State Props
const mapStateToProps = state => ({
    flowslist: Selectors.getFlowItems(state),
    errorMessage: Selectors.getErrorMessage(state)
});

// Dispatch Props
const mapDispatchToProps = dispatch => ({
    onGetFlowsList: () => dispatch(Actions.getFlowsList())
});

// Styles
const contentStyle = {
    paddingLeft: 30,
    paddingRight: 30,
    paddingTop: 30,
    paddingBottom: 30,
    flex: 1,
    display: 'flex',
    flexDirection: 'column',
    overflowY: 'auto'
};

const filterContainerStyle = {
    paddingBottom: 15
};

const listContainerStyle = {
    backgroundColor: Colors.white,
    border: `1px solid ${Colors.neutralTertiaryAlt}`,
    flex: 1,
    overflowY: 'auto',
    minHeight: '100px'
};

export default withRouter(
    connect(
        mapStateToProps,
        mapDispatchToProps
    )(FlowListPanel)
);
