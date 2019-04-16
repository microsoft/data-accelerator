// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
import React from 'react';
import { withRouter } from 'react-router';
import { Panel, Colors } from 'datax-common';
import Card from './card';
import FooterItem from './footerItem';
import Section from './section';
import WelcomeCard from './welcomeCard';
import WideCard from './wideCard';
import { composition } from '../composition';
import { targetTypeEnum } from '../models';

const isLocalHosted = window.location.hostname === 'localhost';

class HomePage extends React.Component {
    constructor(props) {
        super(props);

        this.state = {
            selectedTargetType: isLocalHosted ? targetTypeEnum.local : targetTypeEnum.cloud
        };
    }

    render() {
        return (
            <Panel>
                <div style={pageStyle}>
                    <div style={contentStyle}>
                        {this.renderWelcomeSection()}
                        {this.renderTutorialsSection()}
                        {this.renderSamplesSection()}
                        {this.renderReferencesSection()}
                    </div>
                </div>
            </Panel>
        );
    }

    renderWelcomeSection() {
        return (
            <div style={welcomeSectionStyle}>
                <div>
                    <div style={centeredContentStyle}>
                        <WelcomeCard
                            src={composition.welcome.image}
                            title={composition.welcome.title}
                            descriptions={composition.welcome.descriptions}
                            buttonText={composition.welcome.buttonText}
                            onButtonClick={() => this.onNavigate(composition.welcome.url)}
                        />
                    </div>
                </div>
            </div>
        );
    }

    renderTutorialsSection() {
        const cards = composition.tutorials.items
            .filter(tutorial => tutorial.target === this.state.selectedTargetType)
            .map((tutorial, index) => {
                return (
                    <Card
                        key={`tutorial_card_${index}`}
                        src={tutorial.image}
                        title={tutorial.title}
                        description={tutorial.description}
                        url={tutorial.url}
                        linkText={composition.tutorials.linkText}
                    />
                );
            });

        return (
            <div>
                <div style={centeredContentStyle}>
                    <Section
                        title={composition.tutorials.title}
                        supportViewMore={true}
                        viewMoreText={composition.tutorials.viewMoreText}
                        maxBeforeViewMore={composition.tutorials.maxBeforeViewMore}
                        supportTargetTypeChange={true}
                        selectedTargetType={this.state.selectedTargetType}
                        onTargetTypeChange={type => this.onTargetTypeChange(type)}
                    >
                        {cards}
                    </Section>
                </div>
            </div>
        );
    }

    renderSamplesSection() {
        const target = isLocalHosted ? targetTypeEnum.local : targetTypeEnum.cloud;
        const cards = composition.samples.items
            .filter(sample => sample.target === target)
            .map((sample, index) => {
                return (
                    <WideCard
                        key={`sample_card_${index}`}
                        src={sample.image}
                        title={sample.title}
                        description={sample.description}
                        url={sample.url}
                        onClick={() => this.onNavigate(sample.url)}
                    />
                );
            });

        return (
            <div>
                <div style={centeredContentStyle}>
                    <Section
                        title={composition.samples.title}
                        supportViewMore={true}
                        viewMoreText={composition.samples.viewMoreText}
                        maxBeforeViewMore={composition.samples.maxBeforeViewMore}
                        supportTargetTypeChange={false}
                    >
                        {cards}
                    </Section>
                </div>
            </div>
        );
    }

    renderReferencesSection() {
        const items = composition.references.items.map((reference, index) => {
            return (
                <FooterItem
                    key={`reference_card_${index}`}
                    src={reference.image}
                    largeImage={reference.largeImage}
                    description={reference.description}
                    urlText={reference.urlText}
                    url={reference.url}
                />
            );
        });

        return (
            <div style={referencesSectionStyle}>
                <div>
                    <div style={centeredContentStyle}>
                        <div style={footerStyle}>
                            <Section title={composition.references.title} supportViewMore={false} supportTargetTypeChange={false}>
                                <div style={columnContainerStyle}>{items}</div>
                            </Section>
                        </div>
                    </div>
                </div>
            </div>
        );
    }

    onNavigate(url) {
        this.props.history.push(url);
    }

    onTargetTypeChange(type) {
        this.setState({
            selectedTargetType: type
        });
    }
}

export default withRouter(HomePage);

const pageStyle = {
    overflowY: 'auto'
};

const columnContainerStyle = {
    display: 'flex',
    flexDirection: 'column',
    marginTop: 12
};

const contentStyle = {
    display: 'flex',
    flexDirection: 'column'
};

const welcomeSectionStyle = {
    display: 'flex',
    flexDirection: 'column',
    backgroundColor: Colors.neutralLight,
    marginBottom: 30
};

const referencesSectionStyle = {
    display: 'flex',
    flexDirection: 'column',
    backgroundColor: Colors.white,
    borderTop: `1px solid ${Colors.neutralLight}`,
    paddingTop: 30,
    marginTop: 14
};

const footerStyle = {
    height: 350
};

const centeredContentStyle = {
    minWidth: 1024,
    maxWidth: 1326,
    margin: '0 auto'
};
