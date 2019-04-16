// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
import React from 'react';
import { BrowserRouter, Route, Redirect, Switch } from 'react-router-dom';
import { Provider } from 'react-redux';
import storeManager from '../data/storeManager';
import Loadable from 'react-loadable';
import loadPackage from '../package.loader';
import * as Api from '../data/api';
import { ErrorMessagePanel, LoadingPanel as BlankLoadingPage } from 'datax-common';
import { initializeIcons } from 'office-ui-fabric-react/lib/Icons';
import Layout from './layout';
import LoadingPage from './loadingPage';

export default class App extends React.Component {
    constructor(props) {
        super(props);

        storeManager.createStore();

        // Initialize all Office UI Fabric icons
        initializeIcons();

        this.state = {
            webComposition: undefined
        };
    }

    componentDidMount() {
        Api.getWebComposition().then(webComposition => {
            this.setState({ webComposition: webComposition });
        });
    }

    render() {
        const { pages, routes } = this.getPageAndRoutes();
        const displayName = (this.state.webComposition && this.state.webComposition.displayName) || '';
        const emailUrl = (this.state.webComposition && this.state.webComposition.emailUrl) || '';
        const bugUrl = (this.state.webComposition && this.state.webComposition.bugUrl) || '';

        return (
            <Provider store={storeManager.store}>
                <BrowserRouter>
                    <Layout displayName={displayName} pages={pages} emailUrl={emailUrl} bugUrl={bugUrl}>
                        <Switch>{routes}</Switch>
                    </Layout>
                </BrowserRouter>
            </Provider>
        );
    }

    getPageAndRoutes() {
        let pages = [];
        let routes = [];

        if (this.state.webComposition && this.state.webComposition.pages) {
            const activePages = this.state.webComposition.pages.filter(page => page.enable);

            // Unique pages
            activePages
                .map(page => {
                    const isExternal = !!page.externalUrl;
                    return {
                        title: page.title,
                        url: isExternal ? page.externalUrl : page.routePath,
                        external: isExternal,
                        blankLoadingPage: page.blankLoadingPage
                    };
                })
                .filter((page, index, array) => {
                    return array.map(item => item.title).indexOf(page.title) === index;
                })
                .forEach(p => pages.push(p));

            // Routes
            activePages
                .filter(page => !page.externalUrl)
                .map(page => <Route key={page.key} exact path={page.routePath} component={() => this.getDynamicComponent(page)} />)
                .forEach(r => routes.push(r));

            // Redirects to first active page
            if (activePages.length > 0) {
                routes.push(<Redirect key="redirect" to={activePages[0].routePath} />);
            }
        }

        return { pages, routes };
    }

    getDynamicComponent(page) {
        // Load component on demand
        const LoadedComponent = Loadable({
            loader: loadPackage(page.packageName),
            loading: page.blankLoadingPage ? BlankLoadingPage : LoadingPage,
            delay: 300,
            render: loaded => {
                if (page.componentName in loaded) {
                    const reducersContractName = 'reducers';
                    if (reducersContractName in loaded) {
                        // Register reducers
                        const reducers = loaded[reducersContractName];
                        storeManager.registerReducers(reducers);

                        // Load component
                        const Component = loaded[page.componentName];
                        return <Component {...page.componentProps} />;
                    } else {
                        return <ErrorMessagePanel message={`Failed to find ${reducersContractName} in ${page.packageName} package`} />;
                    }
                } else {
                    return (
                        <ErrorMessagePanel message={`Failed to load ${page.componentName} component from ${page.packageName} package`} />
                    );
                }
            }
        });

        return <LoadedComponent />;
    }
}
