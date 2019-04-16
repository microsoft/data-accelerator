// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
import { targetTypeEnum } from './models';

export const composition = {
    welcome: {
        image: 'img/prop-race-car-data.svg',
        title: 'Welcome to Data Accelerator!',
        descriptions: [
            'Easily onboard to Streaming Big Data on Spark. Get started in minutes on a no-code experience to help with creation, editing and management of streaming jobs.'
        ],
        buttonText: 'Get started',
        url: '/config'
    },
    //To be synced with https://github.com/Microsoft/data-accelerator/wiki/Tutorials
    tutorials: {
        title: 'Tutorials',
        linkText: 'View tutorial',
        viewMoreText: 'tutorials',
        maxBeforeViewMore: 6,
        items: [
            {
                target: targetTypeEnum.cloud,
                image: 'img/prop-firstflow.svg',
                title: 'Setting up a cloud-based pipeline in 5 minutes',
                description: 'Build your first pipeline using Data Accelerator in Microsoft Azure.',
                url: 'https://aka.ms/data-accelerator-tutorial-cloud-firstflow'
            },
            {
                target: targetTypeEnum.cloud,
                image: 'img/prop-livequery.svg',
                title: 'Accelerate SQL editing with Live query',
                description:
                    'Save hours by executing queries in seconds against incoming data to view, and test queries as you build them.',
                url: 'https://aka.ms/data-accelerator-tutorial-cloud-livequery'
            },
            {
                target: targetTypeEnum.cloud,
                image: 'img/prop-alert.svg',
                title: 'Setting up an Alert without code',
                description: 'Build alerts based on data using the rules UI and engine.',
                url: 'https://aka.ms/data-accelerator-tutorial-cloud-alert'
            },
            {
                target: targetTypeEnum.cloud,
                image: 'img/prop-aggalert.svg',
                title: 'Setting up an Aggregate Alert without code',
                description: 'Build aggregated alerts based on data using the rules UI and engine.',
                url: 'https://aka.ms/data-accelerator-tutorial-cloud-aggalert'
            },
            {
                target: targetTypeEnum.cloud,
                image: 'img/prop-output-cloud.svg',
                title: 'Setup new Output',
                description: 'Set up an output to send processed data to an Event Hub, Cosmos DB or Azure Blob.',
                url: 'https://aka.ms/data-accelerator-tutorial-cloud-output'
            },
            {
                target: targetTypeEnum.cloud,
                image: 'img/prop-tag.svg',
                title: 'Adding a Tag to your data using rules',
                description: 'Add metadata to your streamed data with rules.',
                url: 'https://aka.ms/data-accelerator-tutorial-cloud-tag'
            },
            {
                target: targetTypeEnum.cloud,
                image: 'img/prop-aggtag.svg',
                title: 'Add a Tag to your data using aggregated rules',
                description: 'Add metadata to your streamed data with aggregated rules.',
                url: 'https://aka.ms/data-accelerator-tutorial-cloud-aggtag'
            },
            {
                target: targetTypeEnum.cloud,
                image: 'img/prop-output-cosmosdb.svg',
                title: 'Output rules data to Cosmos DB',
                description: 'Send tagged data to Cosmos DB to connect to other Microsoft Azure supported assets.',
                url: 'https://aka.ms/data-accelerator-tutorial-cloud-outputrules'
            },
            {
                target: targetTypeEnum.cloud,
                image: 'img/prop-sql.svg',
                title: 'Adding SQL to your flow',
                description: 'Build processing queries against streaming data with SQL-like queries.',
                url: 'https://aka.ms/data-accelerator-tutorial-cloud-sql'
            },
            {
                target: targetTypeEnum.cloud,
                image: 'img/prop-metrics.svg',
                title: 'Creating a metrics on the dashboard',
                description: 'Visualize the results of your queries on the Metrics dashboard.',
                url: 'https://aka.ms/data-accelerator-tutorial-cloud-sqlmetrics'
            },
            {
                target: targetTypeEnum.cloud,
                image: 'img/prop-window.svg',
                title: 'Use Time Window',
                description: 'Apply Time Window to data to create logic across batches.',
                url: 'https://aka.ms/data-accelerator-tutorial-cloud-window'
            },
            {
                target: targetTypeEnum.cloud,
                image: 'img/prop-reference.svg',
                title: 'Reference data',
                description: 'Add Reference data to join with your streaming data.',
                url: 'https://aka.ms/data-accelerator-tutorial-cloud-reference'
            },
            {
                target: targetTypeEnum.cloud,
                image: 'img/prop-functions.svg',
                title: 'Extending with custom code',
                description: 'Add compiled Scala code via Azure Functions, UDF and UDAF.',
                url: 'https://aka.ms/data-accelerator-tutorial-cloud-functions'
            },
            {
                target: targetTypeEnum.cloud,
                image: 'img/prop-accumulator.svg',
                title: 'Use Accumulator',
                description: 'Store data in an accumulator to monitor data beyond a time window.',
                url: 'https://aka.ms/data-accelerator-tutorial-cloud-accumulator'
            },
            {
                target: targetTypeEnum.cloud,
                image: 'img/prop-scale.svg',
                title: 'Scale the deployment',
                description: 'Increase the capacity of the deployment to enable larger queries.',
                url: 'https://aka.ms/data-accelerator-tutorial-cloud-scale'
            },
            {
                target: targetTypeEnum.cloud,
                image: 'img/prop-role.svg',
                title: 'Add users and Roles',
                description: 'Add users to collaborate on development and understand Role-based access.',
                url: 'https://aka.ms/data-accelerator-tutorial-cloud-role'
            },
            {
                target: targetTypeEnum.cloud,
                image: 'img/prop-armcustom.svg',
                title: 'Customize a Cloud deployment',
                description: 'Customize all Cloud settings via the ARM template.',
                url: 'https://aka.ms/data-accelerator-tutorial-cloud-armcustom'
            },
            {
                target: targetTypeEnum.cloud,
                image: 'img/prop-debug.svg',
                title: 'Debug your job with logs',
                description: 'Review Spark logs to diagnose issues.',
                url: 'https://aka.ms/data-accelerator-tutorial-cloud-debuglog'
            },
            {
                target: targetTypeEnum.cloud,
                image: 'img/prop-debugtelemetry.svg',
                title: 'Debug deployment with telemetry',
                description: 'Review AppInsight telemetry to diagnose deployment issues.',
                url: 'https://aka.ms/data-accelerator-tutorial-cloud-debugtelemetry'
            },
            {
                target: targetTypeEnum.cloud,
                image: 'img/prop-schema.svg',
                title: 'Customize the simulated data schema',
                description: 'Simulate customized data to test your queries and rules.',
                url: 'https://aka.ms/data-accelerator-tutorial-cloud-simulator'
            },
            {
                target: targetTypeEnum.local,
                image: 'img/prop-firstflow.svg',
                title: 'Setting up a local pipeline without code',
                description: 'Build your first pipeline using Data Accelerator.',
                url: 'https://aka.ms/data-accelerator-tutorial-local-firstflow'
            },
            {
                target: targetTypeEnum.local,
                image: 'img/prop-alert.svg',
                title: 'Setting up an Alert without code',
                description: 'Build alerts based on data using the rules UI and engine.',
                url: 'https://aka.ms/data-accelerator-tutorial-local-alert'
            },
            {
                target: targetTypeEnum.local,
                image: 'img/prop-aggalert.svg',
                title: 'Setting up an Aggregate Alert without code',
                description: 'Build aggregated alerts based on data using the rules UI and engine.',
                url: 'https://aka.ms/data-accelerator-tutorial-local-aggalert'
            },
            {
                target: targetTypeEnum.local,
                image: 'img/prop-output-local.svg',
                title: 'Output to disk',
                description: 'Set up an output to send processed data to disk.',
                url: 'https://aka.ms/data-accelerator-tutorial-local-output'
            },
            {
                target: targetTypeEnum.local,
                image: 'img/prop-sql.svg',
                title: 'Adding SQL to your flow',
                description: 'Build processing queries against streaming data with SQL-like queries.',
                url: 'https://aka.ms/data-accelerator-tutorial-local-sql'
            },
            {
                target: targetTypeEnum.local,
                image: 'img/prop-debug.svg',
                title: 'Debug your job with logs',
                description: 'Review Spark logs to diagnose issues.',
                url: 'https://aka.ms/data-accelerator-tutorial-local-debug'
            },
            {
                target: targetTypeEnum.local,
                image: 'img/prop-tag.svg',
                title: 'Adding a Tag to your data using rules',
                description: 'Add metadata to your streamed data with rules.',
                url: 'https://aka.ms/data-accelerator-tutorial-local-tag'
            },
            {
                target: targetTypeEnum.local,
                image: 'img/prop-aggtag.svg',
                title: 'Add a Tag to your data using aggregated rules',
                description: 'Add metadata to your streamed data with aggregated rules.',
                url: 'https://aka.ms/data-accelerator-tutorial-local-aggtag'
            },
            {
                target: targetTypeEnum.local,
                image: 'img/prop-reference.svg',
                title: 'Reference data',
                description: 'Add Reference data to join with your streaming data.',
                url: 'https://aka.ms/data-accelerator-tutorial-local-reference'
            },
            {
                target: targetTypeEnum.local,
                image: 'img/prop-window.svg',
                title: 'Use Time Window',
                description: 'Apply Time Window to data to create logic across batches.',
                url: 'https://aka.ms/data-accelerator-tutorial-local-window'
            },
            {
                target: targetTypeEnum.local,
                image: 'img/prop-accumulator.svg',
                title: 'Use Accumulator',
                description: 'Store data in an accumulator to monitor data beyond a time window.',
                url: 'https://aka.ms/data-accelerator-tutorial-local-accumulator'
            },
            {
                target: targetTypeEnum.local,
                image: 'img/prop-functions.svg',
                title: 'Extending with custom code',
                description: 'Add compiled Scala code via UDF and UDAF.',
                url: 'https://aka.ms/data-accelerator-tutorial-local-extend'
            },
            {
                target: targetTypeEnum.local,
                image: 'img/prop-schema.svg',
                title: 'Customize the simulated data schema',
                description: 'Infer the schema of your data using Get Schema to get intellisense throughout the experience.',
                url: 'https://aka.ms/data-accelerator-tutorial-local-simul'
            },
            {
                target: targetTypeEnum.local,
                image: 'img/prop-scale.svg',
                title: 'Scale the docker host',
                description: 'Increase the capacity of the Docker host to enable larger queries.',
                url: 'https://aka.ms/data-accelerator-tutorial-local-scale'
            }
        ]
    },
    //see /DeploymentCloud/Deployment.Data Accelerator/Samples
    samples: {
        title: 'Samples',
        viewMoreText: 'samples',
        maxBeforeViewMore: 3,
        items: [
            {
                target: targetTypeEnum.cloud,
                image: 'img/prop-sample-home-automation.svg',
                title: 'Home automation',
                description: 'Get started with home automation data and alerts with this sample.',
                url: '/config/edit/iotsample'
            },
            {
                target: targetTypeEnum.local,
                image: 'img/prop-sample-home-automation.svg',
                title: 'Home automation',
                description: 'Get started with home automation data and alerts with this sample.',
                url: '/config/edit/homeautomationlocal'
            },
            {
                target: targetTypeEnum.local,
                image: 'img/prop-sample-telemetry.svg',
                title: 'Metrics',
                description: 'Get started with sending data to the metrics dashboard.',
                url: '/config/edit/basiclocal'
            }
        ]
    },
    references: {
        title: 'References',
        items: [
            {
                image: 'img/prop-wiki.svg',
                largeImage: false,
                description: 'View full documentation on',
                urlText: 'Data Accelerator Wiki',
                url: 'https://aka.ms/data-accelerator-wiki'
            },
            {
                image: 'img/prop-github.svg',
                largeImage: false,
                description: 'Ask questions, submit code changes or request features on GitHub',
                urlText: 'http://aka.ms/data-accelerator',
                url: 'http://aka.ms/data-accelerator'
            },
            {
                image: 'img/prop-spark.svg',
                largeImage: true,
                description: 'Learn how to query your streaming data with',
                urlText: 'Apache Spark SQL',
                url: 'https://spark.apache.org/sql/'
            }
        ]
    }
};
