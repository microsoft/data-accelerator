#Building Services

#Requirements
In order to build the Data Accelerator Services, you will need the following
 - Managed Developement with dotnet core
 - Service Fabric
 
#How to build
 - Open the datax.sln in Visual Studio.  
 - Build

#How to deploy
 - In the datax.sln, right click on each service (Flow, Config, Gateway, Metrics, Simulator)
 - Deploy each service from Visual Studio targetting your Service Fabric cluster

#How to create a PR
 - Ensure all tests are passing
 - Create a pull request aainst the master branch