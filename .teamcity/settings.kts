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
    buildType(UiProductionTestClientTests)
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
    description = "Manual-only production run without test-client state changes."
    configureUiTests(
        testFilter = "TestCategory=ProductionSafe&TestCategory!=MutatesUserState&TestCategory!=ProductionBlocked",
        environmentName = "Production",
        allowProduction = true)
})

object UiProductionTestClientTests : BuildType({
    name = "UI Test Client - Production"
    description = "Manual full run for the isolated test client in the production database."
    configureUiTests(
        testFilter = "(TestCategory=ProductionSafe|TestCategory=ProductionTestClient)&TestCategory!=ProductionBlocked",
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
                "<production test-client login>"
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
