Scenario Tester is a tool that proactively performs http calls to data-accelerator backend and asserts that the expected output is returned by running a set of user-defined scenario runs. Each scenario is run periodically and results are tracked and stored in a database.

From cloud installation steps [link](https://github.com/Microsoft/data-accelerator/wiki/Cloud-deployment) retrieve the used inputs from common.parameters.txt and admin.parameters.txt and from deployed data-accelerator resources.

Execute deployResources.ps1, for example:

```
deployResources.ps1 -tenantId $tenantId -subscriptionId $subscriptionId -resourceGroupName $rg -applicationId $appId -appSecretKey $appSecret -kvBaseNamePrefix $kvPrefix
```
