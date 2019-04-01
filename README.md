### Data Accelerator for Apache Spark - Open Source
Data Accelerator for Apache Spark simplifies streaming of Big Data using Spark.  It offers a no-code experience to build Rules and Alerts, as well as and numerous productivity improvements to develop and manage Spark SQL jobs on Azure HDInsights.  Data Accelerator is used in production at Microsoft to process terabytes of streamed data every day. 
<p align="center"><img style="float: center;" src="https://github.com/Microsoft/data-accelerator/wiki/tutorials/images/readme2.png"></p>

Data Accelerator offers three level of experiences:
 - The first requires no code at all, using rules to create alerts on data content.  
 - The second allows to quickly write a Spark SQL query with additions like LiveQuery, time windowing, in-memory accumulator and more.
 - The third enables integrating custom code written in Scala or via Azure functions.

You can get started locally for Windows, macOs and Linux on the [[Local-mode-with-Docker]].  <br/>
To deploy to Azure, you can use the ARM template; see instructions here [[Cloud-deployment]].<br/>

The [`data-accelerator`](https://github.com/Microsoft/data-accelerator/) repository contains everything needed to set up an end-to-end data pipeline.  There are many ways you can participate in the project:
 - [Submit bugs and requests](https://github.com/Microsoft/data-accelerator/issues)
 - [Review code changes](https://github.com/microsoft/data-accelerator/pulls)
 - [Review documentation](wiki) and make updates ranging from typos to new content.

# Getting Started

You can get started with Data Accelerator for Spark in 5 minutes by using a Docker image.  Please see the [[Local-mode-with-Docker]] to obtain the image then, follow the following steps to create your first data pipeline or Flow.<br/>

Once you are ready to deploy to Azure, you can use the deployment scripts and ARM template to instantiate the infrastructure and apply the right settings.  See the [[Cloud-deployment]].  Data Accelerator for Spark runs on the following:
 - HDInsights with Spark 2.3
 - Service Fabric (v6.4.637.9590) with
   - .NET Core 2.1
   - ASP.NET
 - App Service with Node 10.6

See the [wiki](https://github.com/Microsoft/data-accelerator/wiki) pages for further information on how to build, diagnose and maintain your data pipelines built using Data Accelerator for Spark.

# Engage
Some of the best ways to contribute are to try things out, file issues, join in design conversations, and make pull-requests.

* [Download the latest stable or in-development releases](https://github.com/Microsoft/data-accelerator/wiki)
* Request new features on [GitHub](https://github.com/Microsoft/data-accelerator/blob/master/CONTRIBUTING.md)
* Open a new issue on [GitHub](https://github.com/Microsoft/data-accelerator/issues)
* Ask a question on [Stack Overflow](https://stackoverflow.com/questions/tagged/data-accelerator)
* [Build Data Accelerator source code](CONTRIBUTING.md#build-and-run)
* Check out the [contributing page](CONTRIBUTING.md) to see the best places to log issues and start discussions.

Please also see our [Code of Conduct](CODE_OF_CONDUCT.md).

# Security issues
Security issues and bugs should be reported privately, via email, to the Microsoft Security Response Center (MSRC) secure@microsoft.com. You should receive a response within 24 hours. If for some reason you do not, please follow up via email to ensure we received your original message. Further information, including the MSRC PGP key, can be found in the Security TechCenter.

# License
This repository is licensed with the [MIT](LICENSE) license.