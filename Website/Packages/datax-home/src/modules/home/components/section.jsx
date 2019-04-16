// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
import React from 'react';
import { Dropdown } from 'office-ui-fabric-react';
import { Colors } from 'datax-common';
import { targetTypeOptions } from '../models';

export default class Section extends React.Component {
    constructor(props) {
        super(props);

        this.state = {
            viewMore: false
        };
    }

    render() {
        if (this.props.supportViewMore) {
            const lessThanMax = React.Children.count(this.props.children) <= this.props.maxBeforeViewMore;

            let items;
            if (this.state.viewMore || lessThanMax) {
                items = this.props.children;
            } else {
                items = React.Children.map(this.props.children, (item, index) => {
                    return index < this.props.maxBeforeViewMore ? item : null;
                });
            }

            return (
                <div style={rootStyle}>
                    {this.renderTitleContainer(viewMoreTitleStyle)}
                    <div style={itemContainerStyle}>{items}</div>
                    {this.renderViewToggle(lessThanMax)}
                </div>
            );
        } else {
            return (
                <div style={rootStyle}>
                    {this.renderTitleContainer(titleStyle)}
                    <div style={itemContainerStyle}>{this.props.children}</div>
                </div>
            );
        }
    }

    renderViewToggle(lessThanMax) {
        const viewText = `Show ${this.state.viewMore ? 'less' : 'more'} ${this.props.viewMoreText}`;

        return lessThanMax ? null : (
            <div style={viewMoreToggleStyle} onClick={() => this.onViewToggleClick()}>
                {viewText}
            </div>
        );
    }

    renderTitleContainer(style) {
        return (
            <div style={titleContainerStyle}>
                {this.renderTitle(style)}
                {this.renderTargetTypeDropdown()}
            </div>
        );
    }

    renderTitle(style) {
        return (
            <div className="ms-fontSize-xl" style={style}>
                {this.props.title}
            </div>
        );
    }

    renderTargetTypeDropdown() {
        if (this.props.supportTargetTypeChange) {
            return (
                <div style={fillStyle}>
                    <div style={targetDropdownStyle}>
                        <Dropdown
                            className="ms-font-m"
                            options={targetTypeOptions}
                            selectedKey={this.props.selectedTargetType}
                            onChange={(event, selection) => this.props.onTargetTypeChange(selection.key)}
                        />
                    </div>
                </div>
            );
        } else {
            return null;
        }
    }

    onViewToggleClick() {
        this.setState({
            viewMore: !this.state.viewMore
        });
    }
}

const rootStyle = {
    paddingLeft: 30,
    marginBottom: 16
};

const titleContainerStyle = {
    display: 'flex',
    flexDirection: 'row',
    height: 32,
    paddingRight: 30
};

const titleStyle = {
    color: Colors.neutralPrimaryAlt,
    marginBottom: 16
};

const fillStyle = {
    flex: 1
};

const targetDropdownStyle = {
    width: 150,
    float: 'right'
};

const viewMoreTitleStyle = {
    color: Colors.neutralPrimaryAlt
};

const itemContainerStyle = {
    display: 'flex',
    flexDirection: 'row',
    flexWrap: 'wrap'
};

const viewMoreToggleStyle = {
    color: Colors.customBlue,
    cursor: 'pointer',
    textAlign: 'right',
    paddingRight: 30
};
