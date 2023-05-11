# Test IT TMS Adapter for MsTest/NUnit

## Getting Started

### Installation

#### Requirements

* MethodBoundaryAspect.Fody 2.0.148+
* Microsoft.NET.Test.Sdk 17.5.0+
* MSTest.TestAdapter 3.0.2+ (only for MSTest)
* MSTest.TestFramework 3.0.2+ (only for MSTest)

#### NuGet CLI

``` bash
Install-Package TestIt.Adapter
```

#### .NET CLI

``` bash
dotnet package add TestIt.Adapter
```

## Usage

### Configuration

#### File

1. Create **Tms.config.json** file in the project directory:

    ``` json
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

2. Fill parameters with your configuration, where:
   * `URL` - location of the TMS instance.
   * `USER_PRIVATE_TOKEN` - API secret key. To do that:
      1. Go to the `https://{DOMAIN}/user-profile` profile.
      2. Copy the API secret key.

   * `PROJECT_ID` - ID of a project in TMS instance.
      1. Create a project.
      2. Open DevTools > Network.
      3. Go to the project `https://{DOMAIN}/projects/{PROJECT_GLOBAL_ID}/tests`.
      4. GET-request project, Preview tab, copy iID field.
   * `CONFIGURATION_ID` - ID of a configuration in TMS instance.
      1. Create a project.
      2. Open DevTools > Network.
      3. Go to the project `https://{DOMAIN}/projects/{PROJECT_GLOBAL_ID}/tests`.
      4. GET-request configurations, Preview tab, copy id field.

   * `TEST_RUN_ID` - ID of the created test-run in TMS instance. `TEST_RUN_ID` is optional. If it is not provided, it is created automatically.

   * `TEST_RUN_NAME` - name of the new test-run.`TEST_RUN_NAME` is optional. If it is not provided, it is created automatically.

   * `ADAPTER_MODE` - adapter mode. Default value - 0. The adapter supports following modes:
      * 0 - in this mode, the adapter filters tests by test run ID and configuration ID, and sends the results to the test run.
      * 1 - in this mode, the adapter sends all results to the test run without filtering.
      * 2 - in this mode, the adapter creates a new test run and sends results to the new test run.

   * `AUTOMATIC_CREATION_TEST_CASES` - mode of automatic creation test cases. Default value - false. The adapter supports following modes:
       * true - in this mode, the adapter will create a test case linked to the created autotest (not to the updated autotest).
       * false - in this mode, the adapter will not create a test case.

   * `CERT_VALIDATION` - mode of API-client certificate validation. Default value - true.

#### ENV

You can use environment variables (environment variables take precedence over file variables):

* `TMS_URL` - location of the TMS instance.
  
* `TMS_PRIVATE_TOKEN` - API secret key.
  
* `TMS_PROJECT_ID` - ID of a project in TMS instance.
  
* `TMS_CONFIGURATION_ID` - ID of a configuration in TMS instance.

* `TMS_ADAPTER_MODE` - adapter mode. Default value - 0.
  
* `TMS_TEST_RUN_ID` - ID of the created test-run in TMS instance. `TMS_TEST_RUN_ID` is optional. If it is not provided, it is created automatically.
  
* `TMS_TEST_RUN_NAME` - name of the new test-run.`TMS_TEST_RUN_NAME` is optional. If it is not provided, it is created automatically.

* `TMS_AUTOMATIC_CREATION_TEST_CASES` - mode of automatic creation test cases. Default value - false.

* `TMS_CERT_VALIDATION` - mode of API-client certificate validation. Default value - true.

* `TMS_CONFIG_FILE` - name of the configuration file. `TMS_CONFIG_FILE` is optional. If it is not provided, it is used default file name.
  
#### Command line

You also can CLI variables (CLI variables take precedence over environment variables):

* `tmsUrl` - location of the TMS instance.
  
* `tmsPrivateToken` - API secret key.
  
* `tmsProjectId` - ID of a project in TMS instance.
  
* `tmsConfigurationId` - ID of a configuration in TMS instance.

* `tmsAdapterMode` - adapter mode. Default value - 0.

* `tmsTestRunId` - ID of the created test-run in TMS instance. `tmsTestRunId` is optional. If it is not provided, it is created automatically.
  
* `tmsTestRunName` - name of the new test-run.`tmsTestRunName` is optional. If it is not provided, it is created automatically.

* `tmsAutomaticCreationTestCases` - mode of automatic creation test cases. Default value - false.

* `tmsCertValidation` - mode of API-client certificate validation. Default value - true.

* `tmsConfigFile` - name of the configuration file. `tmsConfigFile` is optional. If it is not provided, it is used default file name.

#### Examples

> **_NOTE:_** To run tests, you must use the TmsRunner utility.

```
TmsRunner --runner "/usr/local/share/dotnet/sdk/6.0.302/vstest.console.dll" --testassembly "/tests/MsTest.dll" -tmsUrl=http://localhost:8080 -tmsPrivateToken=Token -tmsProjectId=f5da5bab-380a-4382-b36f-600083fdd795 -tmsConfigurationId=3a14fa45-b54e-4859-9998-cc502d4cc8c6
-tmsAdapterMode=0 -DtmsTestRunId=a17269da-bc65-4671-90dd-d3e3da92af80 -tmsTestRunName=Regress -tmsAutomaticCreationTestCases=true -tmsCertValidation=true --debug
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

``` c#
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

``` c#
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

* If you have suggestions for adding or removing projects, feel free to [open an issue](https://github.com/testit-tms/adapters-dotnet/issues/new) to discuss it, or create a direct pull request after you edit the *README.md* file with necessary changes.
* Make sure to check your spelling and grammar.
* Create individual PR for each suggestion.
* Read the [Code Of Conduct](https://github.com/testit-tms/adapters-dotnet/blob/main/CODE_OF_CONDUCT.md) before posting your first idea as well.

# License

Distributed under the Apache-2.0 License. See [LICENSE](https://github.com/testit-tms/adapters-dotnet/blob/main/LICENSE.md) for more information.
