// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
const webpack = require('webpack');
const path = require('path');
const pkg = require('./package.json');
const MiniCssExtractPlugin = require('mini-css-extract-plugin');
const TerserPlugin = require('terser-webpack-plugin');

const libraryName = pkg.name;

function configure(env, argv) {
    const isDebug = argv.mode !== 'production';

    var config = {
        // https://webpack.js.org/configuration/entry-context/#entry
        entry: {
            index: './src/index.js'
        },
        // https://webpack.js.org/configuration/output/
        output: {
            path: path.resolve(__dirname, 'dist'),
            filename: '[name].js',
            library: libraryName,
            libraryTarget: 'umd',
            umdNamedDefine: true
        },
        // https://webpack.js.org/configuration/resolve/
        resolve: {
            extensions: ['.js', '.jsx'],
            modules: [path.resolve(__dirname, 'src'), 'node_modules'],
            alias: { 'react': path.resolve(__dirname, './node_modules/', 'react'),
            'react-dom': path.resolve('./node_modules/react-dom')
            }
        },        
        // https://webpack.js.org/configuration/externals/
        externals: {
            react: 'umd react',
            'react-dom': 'umd react-dom'
        },
        // https://webpack.js.org/configuration/module/
        module: {
            rules: [
                {
                    test: /\.(js|jsx)$/,
                    exclude: /(node_modules|bower_components)/,
                    use: {
                        // https://webpack.js.org/loaders/babel-loader/
                        loader: 'babel-loader',
                        options: {
                            cacheDirectory: isDebug,
                            presets: [
                                // https://babeljs.io/docs/en/babel-preset-env
                                '@babel/preset-env',
                                // https://babeljs.io/docs/en/babel-preset-react
                                '@babel/preset-react'
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
        //https://webpack.js.org/configuration/plugins/
        plugins: [
            // https://github.com/webpack-contrib/mini-css-extract-plugin
            new MiniCssExtractPlugin({
                filename: 'css/[name].css'
            })
        ]
    };

    if (!isDebug) {
        // https://github.com/webpack-contrib/terser-webpack-plugin
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
