// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
import React from 'react';
import PropTypes from 'prop-types';
import * as Helpers from '../../flowHelpers';
import * as Models from '../../flowModels';
import { Label } from 'office-ui-fabric-react';
import { Colors } from 'datax-common';

export default class ConditionsPreview extends React.Component {
    constructor(props) {
        super(props);
    }

    render() {
        const labels = this.renderGroup(this.props.query, 0, this.props.supportAggregate, true);
        return <div style={rootStyle}>{labels.map((element, index) => React.cloneElement(element, { key: `Label_${index}` }))}</div>;
    }

    renderGroup(group, groupIndex, supportAggregate, isRoot) {
        let labels = [];
        if (groupIndex > 0) {
            labels.push(this.renderSpace());
            labels.push(this.renderConjunction(group.conjunction));
            labels.push(this.renderSpace());
        }

        if (!isRoot) {
            labels.push(this.renderParens(true));
        }

        group.conditions.forEach((condition, index) => {
            if (condition.type === Models.conditionTypeEnum.group) {
                labels = labels.concat(this.renderGroup(condition, index, supportAggregate, false));
            } else {
                labels = labels.concat(this.renderCondition(condition, index, supportAggregate));
            }
        });

        if (!isRoot) {
            labels.push(this.renderParens(false));
        }
        return labels;
    }

    renderCondition(condition, conditionIndex, supportAggregate) {
        let labels = [];
        if (conditionIndex > 0) {
            labels.push(this.renderSpace());
            labels.push(this.renderConjunction(condition.conjunction));
            labels.push(this.renderSpace());
        }

        if (supportAggregate && condition.aggregate !== Models.aggregateTypeEnum.none) {
            labels.push(this.renderAggregate(condition.aggregate));
            labels.push(this.renderParens(true));

            if (this.isDistinctCountAggregate(condition.aggregate)) {
                labels.push(this.renderDistinctKeyword());
                labels.push(this.renderSpace());
                labels.push(this.renderField(condition.field));
            } else {
                labels.push(this.renderField(condition.field));
            }
            labels.push(this.renderParens(false));
        } else {
            labels.push(this.renderField(condition.field));
        }

        labels.push(this.renderSpace());
        labels.push(this.renderOperator(condition.operator));
        labels.push(this.renderSpace());
        labels.push(this.renderValue(condition.operator, condition.value));
        return labels;
    }

    renderValue(operator, value) {
        const isNumber = Helpers.isNumberOperator(operator);
        const style = {
            color: isNumber ? Colors.themeDarkAlt : Colors.redDark
        };

        return (
            <Label className="ms-font-m ms-fontWeight-semibold" style={style}>
                {Helpers.formatValue(operator, value)}
            </Label>
        );
    }

    renderDistinctKeyword() {
        return (
            <Label className="ms-font-m ms-fontWeight-semibold" style={distinctKeywordStyle}>
                {Models.aggregateDistinctKeyword}
            </Label>
        );
    }

    renderSpace() {
        return <Label className="ms-font-m" style={spaceStyle} />;
    }

    renderOperator(operator) {
        return <Label className="ms-font-m">{Helpers.formatOperator(operator)}</Label>;
    }

    renderField(field) {
        return <Label className="ms-font-m ms-fontWeight-semibold">{field}</Label>;
    }

    renderAggregate(aggregate) {
        const text = this.isDistinctCountAggregate(aggregate) ? Models.aggregateTypeEnum.COUNT : aggregate;

        return (
            <Label className="ms-font-m ms-fontWeight-semibold" style={aggregateStyle}>
                {text}
            </Label>
        );
    }

    renderConjunction(conjunction) {
        return (
            <Label className="ms-font-m" style={conjunctionStyle}>
                {Helpers.formatConjunction(conjunction).trim()}
            </Label>
        );
    }

    renderParens(isLeftParen) {
        return <Label className="ms-font-m">{isLeftParen ? '(' : ')'}</Label>;
    }

    isDistinctCountAggregate(aggregate) {
        return aggregate === Models.aggregateTypeEnum.DCOUNT;
    }
}

// Props
ConditionsPreview.propTypes = {
    query: PropTypes.object.isRequired,
    supportAggregate: PropTypes.bool.isRequired
};

// Styles
const rootStyle = {
    display: 'flex',
    flexDirection: 'row',
    flexWrap: 'wrap',
    paddingLeft: 15,
    paddingRight: 15,
    paddingTop: 15,
    paddingBottom: 15,
    borderBottom: `1px solid ${Colors.neutralQuaternaryAlt}`
};

const spaceStyle = {
    width: 5
};

const distinctKeywordStyle = {
    color: Colors.themeDarkAlt
};

const aggregateStyle = {
    color: Colors.magenta
};

const conjunctionStyle = {
    color: '#333333'
};
