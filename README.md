### Data Accelerator for Apache Spark

|Flow| [![Build status](https://dev.azure.com/ms/data-accelerator/_apis/build/status/DataX.Flow?branchName=master)](https://dev.azure.com/ms/data-accelerator/_build/latest?definitionId=112&branchName=master) |Gateway| [![Build status](https://dev.azure.com/ms/data-accelerator/_apis/build/status/DataX.Gateway?branchName=master)](https://dev.azure.com/ms/data-accelerator/_build/latest?definitionId=113&branchName=master) |DataProcessing| [![Build status](https://dev.azure.com/ms/data-accelerator/_apis/build/status/DataX.Spark?branchName=master)](https://dev.azure.com/ms/data-accelerator/_build/latest?definitionId=116&branchName=master) |
|:---:|:-----:|:-----:|:-----:|:-----:|:-----:|
|**Metrics**| [![Build status](https://dev.azure.com/ms/data-accelerator/_apis/build/status/DataX.Metrics?branchName=master)](https://dev.azure.com/ms/data-accelerator/_build/latest?definitionId=114&branchName=master) |**SimulatedData**| [![Build status](https://dev.azure.com/ms/data-accelerator/_apis/build/status/DataX.SimulatedData?branchName=master)](https://dev.azure.com/ms/data-accelerator/_build/latest?definitionId=115&branchName=master) |**Website**| [![Build status](https://dev.azure.com/ms/data-accelerator/_apis/build/status/DataX.Web?branchName=master)](https://dev.azure.com/ms/data-accelerator/_build/latest?definitionId=117&branchName=master) |

[Data Accelerator](https://github.com/Microsoft/data-accelerator) for Apache Spark democratizes streaming big data using Spark by offering several key features such as a no-code experience to set up a data pipeline as well as fast dev-test loop for creating complex logic.  Our team has been using the project for two years within Microsoft for processing streamed data across many internal deployments handling data volumes at Microsoft scale. It offers an easy to use platform to learn and evaluate streaming needs and requirements.  We are thrilled to share this project with the wider community as open source!

**Azure Friday:** We are now featured on Azure Fridays!  See the video [here](https://azure.microsoft.com/en-us/resources/videos/azure-friday-how-to-stream-big-data-with-data-accelerator-for-apache-spark/).

<p align="center"><img style="float: center;" width="90%" src="https://github.com/Microsoft/data-accelerator/wiki/tutorials/images/readme3.PNG"></p>

[Data Accelerator](https://github.com/Microsoft/data-accelerator) offers three level of experiences:
 - The first requires no code at all, using rules to create alerts on data content.  
 - The second allows to quickly write a Spark SQL query with additions like LiveQuery, time windowing, in-memory accumulator and more.
 - The third enables integrating custom code written in Scala or via Azure functions.

You can get started locally for Windows, macOs and Linux following [these instructions](https://github.com/Microsoft/data-accelerator/wiki/Local-mode-with-Docker)  <br/>
To deploy to Azure, you can use the ARM template; see instructions [deploy to Azure](https://github.com/Microsoft/data-accelerator/wiki/Cloud-deployment).<br/>

The [`data-accelerator`](https://github.com/Microsoft/data-accelerator/) repository contains everything needed to set up an end-to-end data pipeline.  There are many ways you can participate in the project:
 - [Submit bugs and requests](https://github.com/Microsoft/data-accelerator/issues)
 - [Review code changes](https://github.com/microsoft/data-accelerator/pulls)
 - [Review documentation](https://github.com/Microsoft/data-accelerator/wiki) and make updates ranging from typos to new content.

# Getting Started
To unleash the full power Data Accelerator, [deploy to Azure](https://github.com/Microsoft/data-accelerator/wiki/Cloud-deployment) and check [cloud mode tutorials](https://github.com/Microsoft/data-accelerator/wiki/Tutorials#cloud-mode). 


We have also enabled a "hello world" experience that you try out locally by running docker container. When running locally there are no dependencies on Azure, however the functionality is very limited and only there to give you a very cursory overview of Data Accelerator. 
To run Data Accelerator locally, [deploy locally](https://github.com/Microsoft/data-accelerator/wiki/Local-mode-with-Docker) and then check out the [local mode tutorials](https://github.com/Microsoft/data-accelerator/wiki/Tutorials#local-mode).<br/>

Data Accelerator for Spark runs on the following:
 - Azure HDInsight with Spark 2.4 (2.3 also supported)
 - Azure Databricks with Spark 2.4
 - Service Fabric (v6.4.637.9590) with
   - .NET Core 2.2
   - ASP.NET
 - App Service with Node 10.6

See the [wiki](https://github.com/Microsoft/data-accelerator/wiki) pages for further information on how to build, diagnose and maintain your data pipelines built using Data Accelerator for Spark.

# Contributing
If you are interested in fixing issues and contributing to the code base, we would love to partner with you. Try things out, join in the design conversations and make pull requests. 
* [Download the latest stable or in-development releases](https://github.com/Microsoft/data-accelerator/wiki)
* [Build and Debug Data Accelerator source code](CONTRIBUTING.md#build-and-run)

# Feedback
* Request new features on [GitHub](https://github.com/Microsoft/data-accelerator/blob/master/CONTRIBUTING.md)
* Open a new issue on [GitHub](https://github.com/Microsoft/data-accelerator/issues)
* Ask a question on [Stack Overflow](https://stackoverflow.com/questions/tagged/data-accelerator)
* Contact us: data-accelerator@microsoft.com
* Check out the [contributing page](CONTRIBUTING.md) to see the best places to log issues and start discussions.

Please also see our [Code of Conduct](CODE_OF_CONDUCT.md).

# Security issues
Security issues and bugs should be reported privately, via email, to the Microsoft Security Response Center (MSRC) secure@microsoft.com. You should receive a response within 24 hours. If for some reason you do not, please follow up via email to ensure we received your original message. Further information, including the MSRC PGP key, can be found in the Security TechCenter.

# License
This repository is licensed with the [MIT](LICENSE) license.
