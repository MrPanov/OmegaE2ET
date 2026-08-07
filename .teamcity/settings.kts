import jetbrains.buildServer.configs.kotlin.*
import jetbrains.buildServer.configs.kotlin.buildSteps.dotnetRestore
import jetbrains.buildServer.configs.kotlin.buildSteps.dotnetTest
import jetbrains.buildServer.configs.kotlin.buildSteps.script
import jetbrains.buildServer.configs.kotlin.triggers.schedule
import jetbrains.buildServer.configs.kotlin.triggers.vcs

version = "2025.11"

project {
    buildType(UiSmokeTests)
    buildType(UiP0ReleaseTests)
    buildType(UiNightlyTests)
    buildType(UiProductionReadOnlyTests)
}

object UiSmokeTests : BuildType({
    name = "UI Smoke - Test"
    configureUiTests(testFilter = "TestCategory=Smoke")

    triggers {
        vcs {
            branchFilter = "+:*"
        }
    }
})

object UiP0ReleaseTests : BuildType({
    name = "UI P0 Release - Test"
    description = "Manual release-gate run for critical P0 scenarios."
    configureUiTests(testFilter = "TestCategory=P0")
})

object UiNightlyTests : BuildType({
    name = "UI P0 + P1 Nightly - Test"
    configureUiTests(testFilter = "(TestCategory=P0|TestCategory=P1)")

    triggers {
        schedule {
            schedulingPolicy = daily {
                hour = 2
            }
            branchFilter = "+:<default>"
            triggerBuild = always()
            withPendingChangesOnly = false
        }
    }
})

object UiProductionReadOnlyTests : BuildType({
    name = "UI Read Only - Production"
    description = "Manual-only production run. Mutating tests are blocked in code and by filter."
    configureUiTests(
        testFilter = "TestCategory=ProductionSafe&TestCategory!=MutatesUserState",
        environmentName = "Production",
        allowProduction = true)
})

fun BuildType.configureUiTests(
    testFilter: String,
    environmentName: String = "Test",
    allowProduction: Boolean = false)
{
    params {
        param("env.OMEGA_ENVIRONMENT", environmentName)
        param("env.ALLOW_PRODUCTION_TESTS", allowProduction.toString())
        param("env.REQUIRE_AUTHENTICATION", "true")
        param(
            "env.OMEGA_EMAIL",
            if (environmentName == "Production")
                "<production technical login>"
            else
                "web@omega-auto.biz")
        select("env.BROWSER", "chrome", options = listOf("chrome", "edge", "firefox"))
        checkbox("env.HEADLESS", "true", checked = "true", unchecked = "false")
        param("env.EXPLICIT_WAIT_SECONDS", "10")
    }

    vcs {
        root(DslContext.settingsRoot)
    }

    steps {
        script {
            name = "Show .NET SDK"
            scriptContent = "dotnet --info"
        }
        dotnetRestore {
            name = "Restore NuGet packages"
            projects = "UiAutomation.sln"
        }
        dotnetTest {
            name = "Run configuration unit tests"
            projects = "UiAutomation.sln"
            configuration = "Release"
            args = "--no-restore --filter \"TestCategory=Unit\" " +
                "--logger \"trx;LogFileName=configuration-tests.trx\" " +
                "--results-directory artifacts/TestResults"
        }
        dotnetTest {
            name = "Run UI tests"
            projects = "UiAutomation.sln"
            configuration = "Release"
            args = "--no-restore --filter \"$testFilter\" " +
                "--logger \"trx;LogFileName=ui-tests.trx\" " +
                "--results-directory artifacts/TestResults"
        }
    }

    artifactRules = """
        artifacts/TestResults => TestResults
        **/screenshots/*.png => Screenshots
    """.trimIndent()

    failureConditions {
        executionTimeoutMin = 60
    }
}
