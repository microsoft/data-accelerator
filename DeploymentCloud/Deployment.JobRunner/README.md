# Scenario Tester

Scenario Tester is a tool that proactively performs http calls to data-accelerator backend and asserts that the expected output is returned by running a set of user-defined scenario runs. Each scenario is run periodically and results are tracked and stored in a database.

# Install

## Prerequisites
From cloud installation steps [link](https://github.com/Microsoft/data-accelerator/wiki/Cloud-deployment) retrieve the used inputs from common.parameters.txt and admin.parameters.txt and from deployed data-accelerator resources. Specifically we require the following:

- SubscriptionId: Subscription guid where data-accelerator was previously deployed
- TenantId: Tenant where the subscription belongs to
- ResourceGroup: Resource group where data-accelerator was previously deployed
- Application id and secret: Service principal id and secret used in the admin steps
- Keyvault base name prefix: Prefix chose by the data-accelerator deployment script. You can look for the prefix manually by looking at the data-accelerator current keyvault secret names

## Steps
Execute deployResources.ps1, for example:

```
deployResources.ps1 -tenantId $tenantId -subscriptionId $subscriptionId -resourceGroupName $rg -applicationId $appId -appSecretKey $appSecret -kvBaseNamePrefix $kvPrefix
```

# Development

Add your scenario creating a new class inheriting IJob and place it at [Jobs](https://github.com/microsoft/data-accelerator/tree/stable/Services/JobRunner/Jobs) directory. Then register your scenario in [JobRunner class](https://github.com/microsoft/data-accelerator/blob/master/Services/JobRunner/JobRunner.cs#L274).


