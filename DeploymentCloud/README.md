Unleash the full power of [Data Accelerator](Data-accelerator) by deploying it into your subscription in Azure by following the instructions below and get started setting up end to end data pipelines! 

# Prerequisites
 - Install [Azure CLI](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli?view=azure-cli-latest)
 - Download the scripts and templates via [this link](https://github.com/Microsoft/data-accelerator/tree/stable/DeploymentCloud)

# Deployment
The deployment scripts require a minimum of configurations before launching the steps to set up the environment.  You can do so by following these steps:
 - Open common.parameters.txt under Deployment.DataX and provide TenantId and SubscriptionId.  See [Configuring the ARM template](https://github.com/Microsoft/data-accelerator/wiki/Configuring-the-Arm-template) for more options.
 - On Windows OS, open a command prompt as an admin and run the following under Deployment.DataX
```
Deploy.bat
```
 - If you are not an admin of the subscription, you’ll need to ask the admin to run the following along with the parameter file adminsteps.parameters.txt from under Deployment.DataX to finish setting up.
```
adminSteps.ps1
```
Once the deployment finishes, the Data Accelerator portal should open (or you can navigate to http://name.azurewebsites.net where the name will be displayed at the end of the script or can be obtained from the deployed App Service on the http://portal.azure.com)
   
# Troubleshooting and FAQ
 - Please refer to the [FAQ](https://github.com/Microsoft/data-accelerator/wiki/FAQ) for troubleshooting and more information.
	
