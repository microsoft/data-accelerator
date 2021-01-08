// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
import React from 'react';
import * as Utilities from 'office-ui-fabric-react/lib/Utilities';
import VerticalTabItem from './verticalTabItem';

export default class VerticalTabs extends React.Component {
    constructor(props) {
        super(props);

        const links = this.getPivotLinks(props);
        let selectedKey;

        if (props.initialSelectedKey) {
            selectedKey = props.initialSelectedKey;
        } else if (props.initialSelectedIndex) {
            selectedKey = links[props.initialSelectedIndex].itemKey;
        } else if (props.selectedKey) {
            selectedKey = props.selectedKey;
        } else if (links.length) {
            selectedKey = links[0].itemKey;
        }

        this.state = {
            links: this.getPivotLinks(props),
            selectedKey: selectedKey
        };
    }

    componentWillReceiveProps(nextProps) {
        const links = this.getPivotLinks(nextProps);

        this.setState(function (prevState, props) {
            let selectedKey;
            if (this.isKeyValid(nextProps.selectedKey)) {
                selectedKey = nextProps.selectedKey;
            } else if (this.isKeyValid(prevState.selectedKey)) {
                selectedKey = prevState.selectedKey;
            } else if (links.length) {
                selectedKey = links[0].itemKey;
            }

            return {
                links: links,
                selectedKey: selectedKey
            };
        });
    }

    render() {
        return (
            <div className="vertical-tabs">
                {this.renderLinks()}
                {this.renderItem()}
            </div>
        );
    }

    renderLinks() {
        return <div className="vertical-tabs-items">{this.state.links.map(link => this.renderLink(link))}</div>;
    }

    renderLink(link) {
        const itemKey = link.itemKey;
        const linkContent = this.renderLinkContent(link);
        return (
            <div
                tabIndex="0"
                className={`vertical-tabs-item clickable-noselect${link.itemCompleted ? ' complete' : ''}${
                    itemKey == this.state.selectedKey ? ' selected' : ''
                }`}
                key={itemKey}
                onClick={this.onLinkClick.bind(this, itemKey)}
                onKeyPress={this.onKeyPress.bind(this, itemKey)}
                role='tab'
            >
                {linkContent}
            </div>
        );
    }

    renderLinkContent(link) {
        const itemCount = link.itemCount,
            itemIcon = link.itemIcon,
            linkText = link.linkText;
        return (
            <span className="ms-Pivot-link-content">
                {itemIcon !== undefined && (
                    <span>
                        <Icon iconName={itemIcon} />
                    </span>
                )}
                {linkText !== undefined && <span>{linkText}</span>}
                {itemCount !== undefined && <span>({itemCount})</span>}
            </span>
        );
    }

    renderItem() {
        if (this.props.headersOnly) {
            return null;
        }

        const itemKey = this.state.selectedKey;
        const index = this.keyToIndexMapping[itemKey];
        return <div className="vertical-tabs-panel">{React.Children.toArray(this.props.children)[index]}</div>;
    }

    isKeyValid(itemKey) {
        return itemKey !== undefined && this.keyToIndexMapping[itemKey] !== undefined;
    }

    getPivotLinks(props) {
        var links = [];
        this.keyToIndexMapping = {};
        this.keyToTabIds = {};

        React.Children.map(props.children, (child, index) => {
            if (typeof child === 'object' && child.type === VerticalTabItem) {
                var item = child;
                var itemKey = item.props.itemKey || index.toString();
                links.push({
                    linkText: item.props.linkText,
                    ariaLabel: item.props.ariaLabel,
                    itemKey: itemKey,
                    itemCount: item.props.itemCount,
                    itemIcon: item.props.itemIcon,
                    itemCompleted: item.props.itemCompleted,
                    onRenderItemLink: item.props.onRenderItemLink
                });
                this.keyToIndexMapping[itemKey] = index;
                this.keyToTabIds[itemKey] = this.getTabId(itemKey, index);
            }
        });

        return links;
    }

    getTabId(itemKey, index) {
        if (this.props.getTabId) {
            return this.props.getTabId(itemKey, index);
        }

        return this._pivotId + ('-Tab' + index);
    }

    onLinkClick(itemKey, event) {
        event.preventDefault();
        this.onUpdateSelectedItem(itemKey, event);
    }

    onKeyPress(itemKey, event) {
        event.preventDefault();
        if (event.which === Utilities.KeyCodes.enter) {
            this.onUpdateSelectedItem(itemKey);
        }
    }

    onUpdateSelectedItem(itemKey, event) {
        this.setState({
            selectedKey: itemKey
        });
    }
}
