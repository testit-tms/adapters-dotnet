# Test IT TMS Adapter for MsTest/NUnit

## Getting Started

### Installation

#### Requirements

* MethodBoundaryAspect.Fody 2.0.148+
* Microsoft.NET.Test.Sdk 17.5.0+
* MSTest.TestAdapter 3.0.2 (only for MSTest)
* MSTest.TestFramework 3.0.2 (only for MSTest)

#### NuGet CLI

```bash
Install-Package TestIt.Adapter
```

#### .NET CLI

```bash
dotnet package add TestIt.Adapter
```

## Usage

### Configuration

| Description                                                                                                                                                                                                                                                                                                                                                                            | Property                   | Environment variable              | CLI argument                  |
|----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|----------------------------|-----------------------------------|-------------------------------|
| Location of the TMS instance                                                                                                                                                                                                                                                                                                                                                           | url                        | TMS_URL                           | tmsUrl                        |
| API secret key [How to getting API secret key?](https://github.com/testit-tms/.github/tree/main/configuration#privatetoken)                                                                                                                                                                                                                                                            | privateToken               | TMS_PRIVATE_TOKEN                 | tmsPrivateToken               |
| ID of project in TMS instance [How to getting project ID?](https://github.com/testit-tms/.github/tree/main/configuration#projectid)                                                                                                                                                                                                                                                    | projectId                  | TMS_PROJECT_ID                    | tmsProjectId                  |
| ID of configuration in TMS instance [How to getting configuration ID?](https://github.com/testit-tms/.github/tree/main/configuration#configurationid)                                                                                                                                                                                                                                  | configurationId            | TMS_CONFIGURATION_ID              | tmsConfigurationId            |
| ID of the created test run in TMS instance.<br/>It's necessary for **adapterMode** 0 or 1                                                                                                                                                                                                                                                                                              | testRunId                  | TMS_TEST_RUN_ID                   | tmsTestRunId                  |
| Parameter for specifying the name of test run in TMS instance (**It's optional**). If it is not provided, it is created automatically                                                                                                                                                                                                                                                  | testRunName                | TMS_TEST_RUN_NAME                 | tmsTestRunName                |
| Adapter mode. Default value - 0. The adapter supports following modes:<br/>0 - in this mode, the adapter filters tests by test run ID and configuration ID, and sends the results to the test run<br/>1 - in this mode, the adapter sends all results to the test run without filtering<br/>2 - in this mode, the adapter creates a new test run and sends results to the new test run | adapterMode                | TMS_ADAPTER_MODE                  | tmsAdapterMode                |
| It enables/disables certificate validation (**It's optional**). Default value - true                                                                                                                                                                                                                                                                                                   | certValidation             | TMS_CERT_VALIDATION               | tmsCertValidation             |
| Mode of automatic creation test cases (**It's optional**). Default value - false. The adapter supports following modes:<br/>true - in this mode, the adapter will create a test case linked to the created autotest (not to the updated autotest)<br/>false - in this mode, the adapter will not create a test case                                                                    | automaticCreationTestCases | TMS_AUTOMATIC_CREATION_TEST_CASES | tmsAutomaticCreationTestCases |
| List of labels for filtering tests (**Optional**). It will only work with adapter mode 2.                                                                                                                                                                                                                                                                                              | -                          | -                                 | tmsLabelsOfTestsToRun         |

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
  "adapterMode": ADAPTER_MODE,
  "automaticCreationTestCases": AUTOMATIC_CREATION_TEST_CASES,
  "certValidation": CERT_VALIDATION
}
```

#### Examples

> **_NOTE:_** To run tests, you must use the TmsRunner utility.

```
TmsRunner --runner "/usr/local/share/dotnet/sdk/6.0.302/vstest.console.dll" --testassembly "/tests/MsTest.dll" -tmsUrl=http://localhost:8080 -tmsPrivateToken=Token -tmsProjectId=f5da5bab-380a-4382-b36f-600083fdd795 -tmsConfigurationId=3a14fa45-b54e-4859-9998-cc502d4cc8c6
-tmsAdapterMode=0 -DtmsTestRunId=a17269da-bc65-4671-90dd-d3e3da92af80 -tmsTestRunName=Regress -tmsAutomaticCreationTestCases=true -tmsCertValidation=true -tmsLabelsOfTestsToRun smoke,regress --debug
```

* `runner` - path to vstest.console.dll or vstest.console.exe
* `testassembly` - path to dll with tests
* `debug` - enable debug logs

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
* `Step` - the designation of the step.

Description of methods:

* `Adapter.AddLinks` - add links to the autotest result.
* `Adapter.AddAttachments` - add attachments to the autotest result.
* `Adapter.AddMessage` - add message to the autotest result.

### Examples

#### Simple test

```c#
using Tms.Adapter;
using Tms.Adapter.Attributes;
using Tms.Adapter.Models;
using Atr = Tms.Adapter.Attributes;

public class SampleTests {

    [Step]
    public void CreateProject()
    {
        Assert.IsTrue(true);
    }

    [Step]
    [Title("Enter project title")]
    [Atr.Description("Enter project description")]
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

    [TestMethod]
    [WorkItemIds("1523344")]
    [ExternalId("all_annotations_test")]
    [Title("All Annotations Test Title")]
    [DisplayName("All Annotations Test Display Name")]
    [Labels("tag01", "tag02")]
    [Atr.Description("All Annotations Test Description")]
    [Links(url: "https://testit.ru/", LinkType.Related, "TestTitle", "TestDescr")]
    [Links(url: "https://testit.ru/", LinkType.Defect)]
    public void AllAnnotationsTest()
    {
        Adapter.AddLinks("https://testit.ru/", "Test 4", "Desc 4", LinkType.Related);
        CreateProject();
        EnterProject();
        CreateSection();
    }
}
```

#### Parameterized test

```c#
using Tms.Adapter.Attributes;

public class ParameterizedTests {

    [Parameterized]
    [DataRow(MsTest.TestType.Parameter)]
    [DataRow(MsTest.TestType.Simple)]
    [ExternalId("TestType_{testType}")]
    [Title("Title {testType}")]
    [DisplayName("Display Name {testType}")]
    [TestMethod]
    public async Task TestType(TestType testType)
    {
        Assert.AreEqual(testType, testType);
    }

    [Parameterized]
    [DataRow(1,1,2)]
    [DataRow(2,2,4)]
    [ExternalId("Sum_{a}_{b}_{c}")]
    [Title("Title {a} {b} {c}")]
    [DisplayName("Display Name {a} {b} {c}")]
    [TestMethod]
    public async Task Sum(int a, int b, int c)
    {
        Assert.AreEqual(c, a + b);
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
