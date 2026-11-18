using Microsoft.Extensions.Logging;
using UICatalog;

namespace IntegrationTests;

public class ScenarioLogCaptureTests
{
    [Fact]
    public void LogBuffer_IsTrimmed_WhenItGrowsTooLarge ()
    {
        ScenarioLogCapture capture = new ();
        ILogger logger = capture.CreateLogger ("test");
        string message = new ('x', 4096);

        for (var i = 0; i < 120; i++)
        {
            logger.LogInformation ("{Message}", message);
        }

        Assert.True (capture.GetAllLogs ().Length <= 256_000);
    }

    [Fact]
    public void GetScenarioLogs_RespectsScenarioStart_AfterTrim ()
    {
        ScenarioLogCapture capture = new ();
        ILogger logger = capture.CreateLogger ("test");
        string message = new ('y', 4096);

        // Fill close to the cap so the next few entries will trigger a trim.
        while (capture.GetAllLogs ().Length < 250_000)
        {
            logger.LogInformation ("{Message}", message);
        }

        logger.LogInformation ("before-start");

        capture.MarkScenarioStart ();

        int lengthBefore = capture.GetAllLogs ().Length;
        bool trimmed = false;

        for (int i = 0; i < 50; i++)
        {
            logger.LogInformation ("{Message}", message);

            int lengthAfter = capture.GetAllLogs ().Length;

            if (lengthAfter < lengthBefore)
            {
                trimmed = true;
                break;
            }

            lengthBefore = lengthAfter;
        }

        Assert.True (trimmed);

        logger.LogInformation ("after-start");

        string logs = capture.GetScenarioLogs ();
        Assert.DoesNotContain ("before-start", logs);
        Assert.Contains ("after-start", logs);
    }
}
