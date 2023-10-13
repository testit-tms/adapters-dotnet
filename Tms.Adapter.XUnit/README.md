# Test IT TMS Adapter for XUnit

## Getting Started

### Installation

#### NuGet CLI

```bash
Install-Package TestIT.Adapter.XUnit
Install-Package TestIT.Adapter.Core
```

#### .NET CLI

```bash
dotnet package add TestIT.Adapter.XUnit
dotnet package add TestIT.Adapter.Core
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

* `WorkItemIds` -  a method that links autotests with manual tests. Receives the array of manual tests' IDs
* `DisplayName` - internal autotest name (used in Test IT)
* `ExternalId` - unique internal autotest ID (used in Test IT)
* `Title` - autotest name specified in the autotest card. If not specified, the name from the displayName method is used
* `Description` - autotest description specified in the autotest card
* `Labels` - tags listed in the autotest card
* `Links` - links listed in the autotest card
* `Step` - the designation of the step

Description of methods:

* `Adapter.AddLinks` - add links to the autotest result.
* `Adapter.AddAttachments` - add attachments to the autotest result.
* `Adapter.AddMessage` - add message to the autotest result.

### Examples

#### Simple test

```c#
using Tms.Adapter.Core.Attributes;
using Tms.Adapter.XUnit.Attributes;
using Tms.Adapter.Core.Models;

public class SampleTests : IDisposable {

    [Before]
    public StepsTests()
    {
    }
    
    [Step]
    public void CreateProject()
    {
        Assert.IsTrue(true);
    }

    [Step]
    [Title("Enter project title")]
    [Description("Enter project description")]
    public void EnterProject()
    {
        Assert.IsTrue(true);
    }

    [Step]
    public void CreateSection() 
    {
        Assert.IsTrue(true);
        Adapter.AddAttachment("/Users/user/screen.json");
    }

    [TmsFact]
    [WorkItemIds("1523344", "54321")]
    [ExternalId("all_annotations_test")]
    [Title("All Annotations Test Title")]
    [DisplayName("All Annotations Test Display Name")]
    [Labels("tag01", "tag02")]
    [Description("All Annotations Test Description")]
    [Links("https://test01.example", "Example01", "Example01 description", LinkType.Issue)]
    [Links("https://test02.example")]
    public void AllAnnotationsTest()
    {
        Adapter.AddLinks("https://test01.example", "Example01", "Example01 description", LinkType.Issue);
        Adapter.AddLinks("https://test02.example");
        CreateProject();
        EnterProject();
        CreateSection();
    }
    
    [After]
    public void Dispose()
    {
    }
}
```

#### Parameterized test

```c#
using Tms.Adapter.Core.Attributes;
using Tms.Adapter.XUnit.Attributes;
using Tms.Adapter.Core.Models;

public class ParameterizedTests {

    [InlineData(1, "string1")]
    [InlineData(2, "string2")]
    [InlineData(3, "string3")]
    [ExternalId("ParametrizedTest_Success_{number}_{str}")]
    [Title("ParametrizedTest_Success Title {number} {str}")]
    [DisplayName("ParametrizedTest_Success DisplayName {number} {str}")]
    [TmsTheory]
    public void ParametrizedTest_Success(int number, string str)
    {
        Assert.True(true);
    }
}
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
