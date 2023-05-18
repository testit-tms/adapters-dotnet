using Tms.Adapter.Core.Models;
using Tms.Adapter.Core.Service;

namespace Tms.Adapter.XUnit
{
    public static class Steps
    {
        private static readonly AsyncLocal<ITestResultAccessor> TestResultAccessorAsyncLocal = new();

        internal static ITestResultAccessor TestResultAccessor
        {
            get => TestResultAccessorAsyncLocal.Value;
            set => TestResultAccessorAsyncLocal.Value = value;
        }

        #region Fixtures

        public static string StartBeforeFixture(string name)
        {
            var fixtureResult = new FixtureResult
            {
                DisplayName = name,
                Stage = Stage.Running,
                Start = DateTimeOffset.Now.ToUnixTimeMilliseconds()
            };

            AdapterManager.Instance.StartBeforeFixture(TestResultAccessor.TestResultContainer.Id, fixtureResult,
                out var uuid);
            return uuid;
        }

        public static string StartAfterFixture(string name)
        {
            var fixtureResult = new FixtureResult()
            {
                DisplayName = name,
                Stage = Stage.Running,
                Start = DateTimeOffset.Now.ToUnixTimeMilliseconds()
            };

            AdapterManager.Instance.StartAfterFixture(TestResultAccessor.TestResultContainer.Id, fixtureResult,
                out var uuid);
            return uuid;
        }

        public static void StopFixture(Action<FixtureResult> updateResults = null)
        {
            AdapterManager.Instance.StopFixture(result =>
            {
                result.Stage = Stage.Finished;
                result.Stop = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                updateResults?.Invoke(result);
            });
        }

        public static void StopFixtureSuppressTestCase(Action<FixtureResult> updateResults = null)
        {
            var newTestResult = TestResultAccessor.TestResult;
            StopFixture(updateResults);
            AdapterManager.Instance.StartTestCase(TestResultAccessor.TestResultContainer.Id, newTestResult);
        }

        #endregion

        #region Steps

        public static string StartStep(string name, Action<StepResult> updateResults = null)
        {
            var stepResult = new StepResult
            {
                DisplayName = name,
                Stage = Stage.Running,
                Start = DateTimeOffset.Now.ToUnixTimeMilliseconds()
            };
            updateResults?.Invoke(stepResult);

            AdapterManager.Instance.StartStep(stepResult, out var uuid);
            return uuid;
        }

        public static void PassStep(Action<StepResult> updateResults = null)
        {
            AdapterManager.Instance.StopStep(result =>
            {
                result.Status = Status.Passed;
                updateResults?.Invoke(result);
            });
        }

        public static void PassStep(string uuid, Action<StepResult> updateResults = null)
        {
            AdapterManager.Instance.UpdateStep(uuid, result =>
            {
                result.Status = Status.Passed;
                updateResults?.Invoke(result);
            });
            AdapterManager.Instance.StopStep(uuid);
        }

        public static void FailStep()
        {
            AdapterManager.Instance.StopStep(result => { result.Status = Status.Failed; });
        }

        public static void FailStep(string uuid, Action<StepResult> updateResults = null)
        {
            AdapterManager.Instance.UpdateStep(uuid, result =>
            {
                result.Status = Status.Failed;
                updateResults?.Invoke(result);
            });
            AdapterManager.Instance.StopStep(uuid);
        }

        public static void BrokeStep(Action<StepResult> updateResults = null)
        {
            AdapterManager.Instance.StopStep(result =>
            {
                result.Status = Status.Broken;
                updateResults?.Invoke(result);
            });
        }

        public static void BrokeStep(string uuid, Action<StepResult> updateResults = null)
        {
            AdapterManager.Instance.UpdateStep(uuid, result =>
            {
                result.Status = Status.Broken;
                updateResults?.Invoke(result);
            });
            AdapterManager.Instance.StopStep(uuid);
        }

        #endregion

        public static Task<T> Step<T>(string name, Func<Task<T>> action)
        {
            StartStep(name);
            return Execute(action);
        }

        public static T Step<T>(string name, Func<T> action)
        {
            StartStep(name);
            return Execute(name, action);
        }

        public static void Step(string name, Action action)
        {
            Step(name, (Func<object>)(() =>
            {
                action();
                return null;
            }));
        }

        public static Task Step(string name, Func<Task> action)
        {
            return Step(name, async () =>
            {
                await action();
                return Task.FromResult<object>(null);
            });
        }

        public static void Step(string name)
        {
            Step(name, () => { });
        }

        public static Task<T> Before<T>(string name, Func<Task<T>> action)
        {
            StartBeforeFixture(name);
            return Execute(action);
        }

        public static T Before<T>(string name, Func<T> action)
        {
            StartBeforeFixture(name);
            return Execute(name, action);
        }

        public static void Before(string name, Action action)
        {
            Before(name, (Func<object>)(() =>
            {
                action();
                return null;
            }));
        }

        public static Task Before(string name, Func<Task> action)
        {
            return Before(name, async () =>
            {
                await action();
                return Task.FromResult<object>(null);
            });
        }

        public static Task<T> After<T>(string name, Func<Task<T>> action)
        {
            StartAfterFixture(name);
            return Execute(action);
        }

        public static T After<T>(string name, Func<T> action)
        {
            StartAfterFixture(name);
            return Execute(name, action);
        }

        public static void After(string name, Action action)
        {
            After(name, (Func<object>)(() =>
            {
                action();
                return null;
            }));
        }

        public static Task After(string name, Func<Task> action)
        {
            return After(name, async () =>
            {
                await action();
                return Task.FromResult<object>(null);
            });
        }

        private static async Task<T> Execute<T>(Func<Task<T>> action)
        {
            T result;
            try
            {
                result = await action();
            }
            catch (Exception e)
            {
                FailStep();
                throw;
            }

            PassStep();
            return result;
        }

        private static T Execute<T>(string name, Func<T> action)
        {
            T result;
            try
            {
                result = action();
            }
            catch (Exception e)
            {
                FailStep();
                throw new StepFailedException(name, e);
            }

            PassStep();
            return result;
        }
    }
}