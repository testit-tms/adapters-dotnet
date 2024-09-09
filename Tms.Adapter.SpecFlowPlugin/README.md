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
dotnet add package TestIT.Adapter.SpecFlowPlugin
dotnet add package TestIT.Adapter.Core
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

| Description                                                                                                                                                                                                                                                                                                         | File property                     | Environment variable                       |
|---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|-----------------------------------|--------------------------------------------|
| Location of the TMS instance                                                                                                                                                                                                                                                                                        | url                               | TMS_URL                                    |
| API secret key [How to getting API secret key?](https://github.com/testit-tms/.github/tree/main/configuration#privatetoken)                                                                                                                                                                                         | privateToken                      | TMS_PRIVATE_TOKEN                          | 
| ID of project in TMS instance [How to getting project ID?](https://github.com/testit-tms/.github/tree/main/configuration#projectid)                                                                                                                                                                                 | projectId                         | TMS_PROJECT_ID                             | 
| ID of configuration in TMS instance [How to getting configuration ID?](https://github.com/testit-tms/.github/tree/main/configuration#configurationid)                                                                                                                                                               | configurationId                   | TMS_CONFIGURATION_ID                       | 
| ID of the created test run in TMS instance. If you don't provide a test run ID, the adapter will automatically create one.                                                                                                                                                                                          | testRunId                         | TMS_TEST_RUN_ID                            | 
| Parameter for specifying the name of test run in TMS instance (**It's optional**). If it is not provided, it is created automatically                                                                                                                                                                               | testRunName                       | TMS_TEST_RUN_NAME                          |
| It enables/disables certificate validation (**It's optional**). Default value - true                                                                                                                                                                                                                                | certValidation                    | TMS_CERT_VALIDATION                        | 
| Mode of automatic creation test cases (**It's optional**). Default value - false. The adapter supports following modes:<br/>true - in this mode, the adapter will create a test case linked to the created autotest (not to the updated autotest)<br/>false - in this mode, the adapter will not create a test case | automaticCreationTestCases        | TMS_AUTOMATIC_CREATION_TEST_CASES          |
| Mode of automatic updation links to test cases (**It's optional**). Default value - false. The adapter supports following modes:<br/>true - in this mode, the adapter will update links to test cases<br/>false - in this mode, the adapter will not update link to test cases                                      | automaticUpdationLinksToTestCases | TMS_AUTOMATIC_UPDATION_LINKS_TO_TEST_CASES |
| Enable debug logs (**It's optional**). Default value - false                                                                                                                                                                                                                                                        | isDebug                           | -                                          |

#### File

Create **Tms.config.json** file in the project directory:

```json
{
  "url": "URL",
  "privateToken": "USER_PRIVATE_TOKEN",
  "projectId": "PROJECT_ID",
  "configurationId": "CONFIGURATION_ID",
  "testRunId": "TEST_RUN_ID",
  "testRunName": "TEST_RUN_NAME",
  "automaticCreationTestCases": false,
  "automaticUpdationLinksToTestCases": false,
  "certValidation": true,
  "isDebug": true
}
```

### How to run

Just run the command:

```bash
dotnet test
```

### Attributes

Use attributes to specify information about autotest.

Description of attributes:

* `WorkItemIds` -  a method that links autotests with manual tests. Receives the array of manual tests' IDs
* `DisplayName` - internal autotest name (used in Test IT)
* `ExternalId` - unique internal autotest ID (used in Test IT)
* `Title` - autotest name specified in the autotest card. If not specified, the name from the displayName method is used
* `Description` - autotest description specified in the autotest card
* `Labels` - tags listed in the autotest card
* `Links` - links listed in the autotest card

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
