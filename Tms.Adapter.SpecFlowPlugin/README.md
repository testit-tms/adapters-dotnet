# Test IT TMS Adapter for SpecFlow

## Getting Started

### Installation

#### NuGet CLI

```bash
Install-Package TestIT.Adapter.SpecFlowPlugin
Install-Package TestIT.Adapter.Core
```

#### .NET CLI

```bash
dotnet package add TestIT.Adapter.SpecFlowPlugin
dotnet package add TestIT.Adapter.Core
```

#### Add runtime plugin

Also you need to add following lines to `specflow.json`:

```json
{
  "assembly": "Tms.Adapter.SpecFlowPlugin"
}
```

## Usage

### Configuration

| Description                                                                                                                                                                                                                                                                                                         | Property                   | Environment variable              |
|---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|----------------------------|-----------------------------------|
| Location of the TMS instance                                                                                                                                                                                                                                                                                        | url                        | TMS_URL                           |
| API secret key [How to getting API secret key?](https://github.com/testit-tms/.github/tree/main/configuration#privatetoken)                                                                                                                                                                                         | privateToken               | TMS_PRIVATE_TOKEN                 | 
| ID of project in TMS instance [How to getting project ID?](https://github.com/testit-tms/.github/tree/main/configuration#projectid)                                                                                                                                                                                 | projectId                  | TMS_PROJECT_ID                    | 
| ID of configuration in TMS instance [How to getting configuration ID?](https://github.com/testit-tms/.github/tree/main/configuration#configurationid)                                                                                                                                                               | configurationId            | TMS_CONFIGURATION_ID              | 
| ID of the created test run in TMS instance.                                                                                                                                                                                                                                                                         | testRunId                  | TMS_TEST_RUN_ID                   | 
| It enables/disables certificate validation (**It's optional**). Default value - true                                                                                                                                                                                                                                | certValidation             | TMS_CERT_VALIDATION               | 
| Mode of automatic creation test cases (**It's optional**). Default value - false. The adapter supports following modes:<br/>true - in this mode, the adapter will create a test case linked to the created autotest (not to the updated autotest)<br/>false - in this mode, the adapter will not create a test case | automaticCreationTestCases | TMS_AUTOMATIC_CREATION_TEST_CASES |
| Enable debug logs (**It's optional**). Default value - false                                                                                                                                                                                                                                                        | isDebug                    | -                                 |

#### File

Create **Tms.config.json** file in the project directory:

```json
{
    "url": "URL",
    "privateToken": "USER_PRIVATE_TOKEN",
    "projectId": "PROJECT_ID",
    "configurationId": "CONFIGURATION_ID",
    "testRunId": "TEST_RUN_ID",
    "automaticCreationTestCases": false,
    "certValidation": true,
    "isDebug": true
}
```

### How to run

If you specified TestRunId, then just run the command: 

```bash
dotnet test
```

To create and complete TestRun you can use the [Test IT CLI](https://docs.testit.software/user-guide/integrations/cli.html):

```bash
$ export TMS_TOKEN=<YOUR_TOKEN>
$ testit \
  --mode create
  --url https://tms.testit.software \
  --project-id 5236eb3f-7c05-46f9-a609-dc0278896464 \
  --testrun-name "New test run" \
  --output tmp/output.txt

$ export TMS_TEST_RUN_ID=$(cat output.txt)  

$ dotnet test

$ testit \
  --mode finish
  --url https://tms.testit.software \
  --testrun-id $(cat tmp/output.txt) 
```

### Attributes

Use attributes to specify information about autotest.

Description of attributes:

* `WorkItemIds` - linking an autotest to a test case.
* `DisplayName` - name of the autotest in Test IT.
* `ExternalId` - ID of the autotest within the project in Test IT.
* `Title` - title in the autotest card and the step.
* `Description` - description in the autotest card and the step.
* `Labels` - tags in the autotest card.
* `Links` - links in the autotest card.

Description of methods:

* `Adapter.AddLinks` - add links to the autotest result.
* `Adapter.AddAttachments` - add attachments to the autotest result.
* `Adapter.AddMessage` - add message to the autotest result.

### Examples

#### Simple test

```gherkin
Feature: Simple
  
  @ExternalId=With_all_annotations_success
  @DisplayName=With_all_annotations_success_display_name
  @Title=With_all_annotations_success_title
  @Description=With_all_annotations_success
  @Labels=Label1,Label2
  @Links={"url":"https://test01.example","title":"Example01","description":"Example01_description","type":"Issue"}
  @Links={"url":"https://test02.example","title":"Example02","description":"Example02_description","type":"Issue"}
  @WorkItemIds=123,321
  Scenario: With all annotations
    Then return true
```

#### Parameterized test

```gherkin
Feature: Parameterized

  @ExternalId=parametrized_test_{number}_{value}_success
  @DisplayName=parametrized_test_{number}_{value}_success_display_name
  @Title=parametrized_test_{number}_{value}_success_title
  @Description=parametrized_test_{number}_{value}_success
  Scenario Outline: Parametrized test
    When get parameters <number> <value>
    Then return true

    Examples:
      | number | value    |
      | 1      | string01 |
      | 2      | string02 |
      | 3      | string03 |
```

# Contributing

You can help to develop the project. Any contributions are **greatly appreciated**.

* If you have suggestions for adding or removing projects, feel free
  to [open an issue](https://github.com/testit-tms/adapters-dotnet/issues/new) to discuss it, or create a direct pull
  request after you edit the *README.md* file with necessary changes.
* Make sure to check your spelling and grammar.
* Create individual PR for each suggestion.
* Read the [Code Of Conduct](https://github.com/testit-tms/adapters-dotnet/blob/main/CODE_OF_CONDUCT.md) before posting
  your first idea as well.

# License

Distributed under the Apache-2.0 License.
See [LICENSE](https://github.com/testit-tms/adapters-dotnet/blob/main/LICENSE.md) for more information.
