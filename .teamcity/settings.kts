import jetbrains.buildServer.configs.kotlin.*
import jetbrains.buildServer.configs.kotlin.buildSteps.dotnetRestore
import jetbrains.buildServer.configs.kotlin.buildSteps.dotnetTest

version = "2025.11"

project {
    buildType(UiTests)
}

object UiTests : BuildType({
    name = "Selenium UI Tests"

    params {
        select(
            "env.OMEGA_ENVIRONMENT",
            "Production",
            label = "Target environment",
            description = "Production: my.omega.page; Test: test.omega.page",
            options = listOf("Production", "Test")
        )
        param("env.OMEGA_EMAIL", "web@omega-auto.biz")
        select("env.BROWSER", "chrome", options = listOf("chrome", "edge", "firefox"))
        checkbox("env.HEADLESS", "true", checked = "true", unchecked = "false")
        param("env.EXPLICIT_WAIT_SECONDS", "10")
    }

    vcs {
        root(DslContext.settingsRoot)
    }

    steps {
        dotnetRestore {
            name = "Restore NuGet packages"
            projects = "UiAutomation.sln"
        }
        dotnetTest {
            name = "Run NUnit UI tests"
            projects = "UiAutomation.sln"
            configuration = "Release"
            args = "--no-restore --filter \"TestCategory=Smoke\" --logger \"trx;LogFileName=ui-tests.trx\" --results-directory artifacts/TestResults"
        }
    }

    artifactRules = """
        artifacts/TestResults => TestResults
        **/screenshots/*.png => Screenshots
    """.trimIndent()

    failureConditions {
        executionTimeoutMin = 30
    }
})
