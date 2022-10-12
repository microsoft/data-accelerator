// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
const webpack = require('webpack');
const path = require('path');
const TerserPlugin = require('terser-webpack-plugin');
const MiniCssExtractPlugin = require('mini-css-extract-plugin');
const dist = require('./util/dist');

function configure(env, argv) {
    var distEnv = dist.withEnv(env);
    console.log('Building to ' + distEnv.dist);

    const isDebug = distEnv.name == 'dev';
    const browserslist = ['>1%', 'last 4 versions', 'Firefox ESR', 'not ie < 9'];

    var config = {
        // https://webpack.js.org/configuration/entry-context/#entry
        entry: {
            main: './client/js/main.js',
            vendor: [
                'q',
                'react',
                'react-dom',
                'react-router',
                'react-router-dom',
                'react-redux',
                'redux',
                'redux-thunk',
                'radium',
                'prop-types',
                'promise-polyfill',
                'whatwg-fetch',
                'reselect',
                'office-ui-fabric-react'
            ]
        },
        // https://webpack.js.org/configuration/output/
        output: {
            path: path.resolve(`./${distEnv.dist}/`),
            filename: 'js/[name].js'
        },
        // https://webpack.js.org/configuration/resolve/
        resolve: {
            extensions: ['.js', '.jsx'],
            // include sources from the following folders
            modules: [path.resolve('./components'), path.resolve('./data'), 'node_modules']
        },
        // https://webpack.js.org/configuration/module/
        module: {
            rules: [
                {
                    test: /\.(js|jsx)$/,
                    exclude: /(node_modules|bower_components)/,
                    include: [
                        path.resolve(__dirname, './client/js/'),
                        path.resolve(__dirname, './components/'),
                        path.resolve(__dirname, './data/')
                    ],
                    use: {
                        // https://webpack.js.org/loaders/babel-loader/
                        loader: 'babel-loader',
                        options: {
                            //cacheDirectory: isDebug,
                            presets: [
                                // https://babeljs.io/docs/en/babel-preset-env
                                [
                                    '@babel/preset-env',
                                    {
                                        targets: {
                                            browsers: browserslist
                                        },
                                        modules: false,
                                        useBuiltIns: false,
                                        debug: false
                                    }
                                ],
                                // https://babeljs.io/docs/en/babel-preset-react
                                [
                                    '@babel/preset-react',
                                    {
                                        development: isDebug
                                    }
                                ]
                            ],
                            plugins: [
                                //https://babeljs.io/docs/en/babel-plugin-syntax-dynamic-import
                                '@babel/plugin-syntax-dynamic-import'
                            ]
                        }
                    }
                },
                {
                    // https://github.com/webpack-contrib/mini-css-extract-plugin
                    test: /\.css$/,
                    use: [
                        {
                            loader: MiniCssExtractPlugin.loader,
                            options: {
                                publicPath: '../'
                            }
                        },
                        'css-loader'
                    ]
                },
                {
                    test: /\.svg(\?v=\d+\.\d+\.\d+)?$/,
                    loader: 'url-loader?name=img/[name].[ext]&limit=10000&mimetype=image/svg+xml'
                },
                {
                    test: /\.(svg|png|jpg|jpeg|gif)$/,
                    loader: 'file-loader?name=img/[name].[ext]'
                },
                {
                    test: /\.(html|ico)$/,
                    include: path.resolve(__dirname, 'client'),
                    loader: 'file-loader?name=[name].[ext]'
                },
                {
                    test: /\.eot(\?v=\d+\.\d+\.\d+)?$/,
                    loader: 'file-loader?name=fonts/[name].[ext]'
                },
                {
                    test: /\.(woff|woff2)$/,
                    loader: 'url-loader?name=fonts/[name].[ext]&prefix=font&limit=5000'
                },
                {
                    test: /\.ttf(\?v=\d+\.\d+\.\d+)?$/,
                    loader: 'url-loader?name=fonts/[name].[ext]&limit=10000&mimetype=application/octet-stream'
                }
            ]
        },
        // https://webpack.js.org/configuration/plugins/
        plugins: [
            // https://webpack.js.org/plugins/define-plugin/
            new webpack.DefinePlugin({
                'process.env': {
                    NODE_ENV: isDebug ? '"dev"' : '"production"'
                }
            }),
            // https://github.com/webpack-contrib/mini-css-extract-plugin
            new MiniCssExtractPlugin({
                filename: 'css/[name].css'
            })
        ],
    };

    if (!isDebug) {
        // https://github.com/webpack-contrib/terser-webpack-plugin
        // This TerserPlugin replaces UglifyJsPlugin for webpack 4
        config.plugins.push(
            new TerserPlugin({
                parallel: true,
                terserOptions: {
                    mangle: true /* uglify */
                }
            })
        );
    }

    return config;
}

module.exports = configure;
