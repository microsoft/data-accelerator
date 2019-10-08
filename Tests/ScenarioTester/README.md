## Scenario Tester

Scenario Tester is designed to run through a number of actions in a sequence with N number of parallel executions of the scenario. 

To use it: 

1. Create a set of "ScenarioSteps" identified by the [StepAttribute] that takes the ScenarioContext and returns a StepResult. Each Scenario Step is an autonomous function
that gets all its inputs from the scenario context, executes the action for the step, validates the action results, and returns the result of the step in StepResult. Any outputs 
can be added into the ScenarioContext to be passed down to the next step in the Scenario chain.

```
public class Host
{
    [Step("createResource")]
    public static StepResult CreateResource(ScenarioContext context)
    {
        // get step input from the context
        var resourceName = context["resource_name"]

        // run the step
        string jsonResult = Request.Get(serverAddress + "?resource=" + resourceName);

        // add the output of the result to the context
        dynamic result = JsonConvert.DeserializeObject(jsonResult);
        context["resource_id"] = result.resource_id;

        // validate the step output
        return new StepResult(result.resource_id != null, "resource was created");
    }
}
```

2. A scenario is a collection of steps.

```
// using the steps directly
var scenario = new ScenarioDescription("resourceAddDelete", 
                Host.CreateResource, 
                Host.DeleteResource);

// or from JSON 
var scenario = ScenarioDescription.FromJson("resourceAddDelete", @"
[
    {'action':'createResource'}, 
    {'action':'deleteResource'}
]", typeof(Host));
```

3. The scenario can then be run. 

```
var context = new ScenarioContext();
context["resource_name"] = "ResourceA";

var results = await ScenarioResult.RunAsync(scenario, context, 100);
```


