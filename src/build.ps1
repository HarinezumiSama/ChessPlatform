#Requires -Version 5

using namespace System
using namespace System.Management.Automation

[CmdletBinding(PositionalBinding = $false)]
param
(
    [Parameter()]
    [ValidateSet('Debug', 'Release')]
    [string] $BuildConfiguration = 'Debug',

    [Parameter()]
    [ValidateSet('Any CPU')]
    [string] $BuildPlatform = 'Any CPU',

    [Parameter()]
    [switch] $AppveyorBuild,

    [Parameter()]
    [AllowNull()]
    [AllowEmptyString()]
    [string] $AppveyorSourceCodeRevisionId = $null,

    [Parameter()]
    [AllowNull()]
    [AllowEmptyString()]
    [string] $AppveyorSourceControlBranchName = $null,

    [Parameter()]
    [AllowNull()]
    [AllowEmptyString()]
    [string] $AppveyorBuildNumber = $null
)
begin
{
    $Script:ErrorActionPreference = [System.Management.Automation.ActionPreference]::Stop
    Microsoft.PowerShell.Core\Set-StrictMode -Version 1

    [ValidateNotNullOrEmpty()] [string] $workspaceRootDirectoryPath = $PSScriptRoot
    [string] $solutionFilePattern = '*.sln'

    function Get-ErrorDetails([ValidateNotNull()] [System.Management.Automation.ErrorRecord] $errorRecord = $_)
    {
        [ValidateNotNull()] [System.Exception] $exception = $errorRecord.Exception
        while ($exception -is [System.Management.Automation.RuntimeException] -and $exception.InnerException -ne $null)
        {
            $exception = $exception.InnerException
        }

        [string[]] $lines = `
        @(
            $exception.Message,
            '',
            '<<<',
            "Exception: '$($exception.GetType().FullName)'",
            "FullyQualifiedErrorId: '$($errorRecord.FullyQualifiedErrorId)'"
        )

        if (![string]::IsNullOrWhiteSpace($errorRecord.ScriptStackTrace))
        {
            $lines += `
            @(
                '',
                'Script stack trace:',
                '-------------------',
                $($errorRecord.ScriptStackTrace)
            )
        }

        if (![string]::IsNullOrWhiteSpace($exception.StackTrace))
        {
            $lines += `
            @(
                '',
                'Exception stack trace:',
                '----------------------',
                $($exception.StackTrace)
            )
        }

        $lines += '>>>'

        return ($lines -join ([System.Environment]::NewLine))
    }

    function Write-MajorSeparator
    {
        [CmdletBinding(PositionalBinding = $false)]
        param ()
        process
        {
            Write-Host ''
            Write-Host -ForegroundColor Magenta ('=' * 100)
            Write-Host ''
        }
    }

    function Write-ActionTitle
    {
        [CmdletBinding(PositionalBinding = $false)]
        param
        (
            [Parameter(Position = 0)]
            [ValidateNotNullOrEmpty()]
            [string] $Title = $(throw [ArgumentNullException]::new('Title'))
        )
        process
        {
            Write-Host -ForegroundColor Green $Title
        }
    }
}
process
{
    [Console]::ResetColor()
    Write-MajorSeparator

    try
    {
        Write-Host -ForegroundColor Green "BuildConfiguration: ""$BuildConfiguration"""
        Write-Host -ForegroundColor Green "BuildPlatform: ""$BuildPlatform"""
        Write-Host ''
        Write-Host -ForegroundColor Green "AppveyorBuild: $AppveyorBuild"
        if ($AppveyorBuild)
        {
            Write-Host -ForegroundColor Green "AppveyorSourceCodeRevisionId: ""$AppveyorSourceCodeRevisionId"""
            Write-Host -ForegroundColor Green "AppveyorSourceControlBranchName: ""$AppveyorSourceControlBranchName"""
            Write-Host -ForegroundColor Green "AppveyorBuildNumber: ""$AppveyorBuildNumber"""
        }

        Write-MajorSeparator

        if ([string]::IsNullOrWhiteSpace($BuildConfiguration))
        {
            throw [ArgumentException]::new('The build configuration cannot be blank.', 'BuildConfiguration')
        }
        if ([string]::IsNullOrWhiteSpace($BuildPlatform))
        {
            throw [ArgumentException]::new('The build platform cannot be blank.', 'BuildPlatform')
        }

        if ($AppveyorBuild)
        {
            if ([string]::IsNullOrWhiteSpace($AppveyorSourceCodeRevisionId))
            {
                throw [ArgumentException]::new(
                    'The source code revision ID cannot be blank when the AppveyorBuild switch is ON.',
                    'AppveyorSourceCodeRevisionId')
            }
            if ([string]::IsNullOrWhiteSpace($AppveyorSourceControlBranchName))
            {
                throw [ArgumentException]::new(
                    'The source code branch name cannot be blank when the AppveyorBuild switch is ON.',
                    'AppveyorSourceControlBranchName')
            }
            if ([string]::IsNullOrWhiteSpace($AppveyorBuildNumber))
            {
                throw [ArgumentException]::new(
                    'The Appveyor build number cannot be blank when the AppveyorBuild switch is ON.',
                    'AppveyorBuildNumber')
            }
        }

        [string[]] $foundSolutionFilePaths = `
            Get-ChildItem -LiteralPath $PSScriptRoot -Recurse -Force -File -Filter $solutionFilePattern `
                | Select-Object -ExpandProperty FullName

        if ($foundSolutionFilePaths.Count -ne 1)
        {
            throw "There must be exactly one solution file found within ""$PSScriptRoot"". Found: $($foundSolutionFilePaths.Count)."
        }

        [ValidateNotNullOrEmpty()] [string] $solutionFilePath = $foundSolutionFilePaths[0]

        [string[]] $commonBuildPropertyArguments = @()

        [string[]] $testExecutionCliOptions = `
            @(
                '--no-build'
                '--logger:trx'
                '--logger:html'
                '--logger:console;verbosity=normal'
            )

        if ($AppveyorBuild)
        {
            [string] $shortRevisionId = $AppveyorSourceCodeRevisionId.Substring(0, [Math]::Min(12, $AppveyorSourceCodeRevisionId.Length))

            $commonBuildPropertyArguments += `
                @(
                    "-p:IsAppveyorBuild=""true"""
                    "-p:CI_BuildNumber=""$AppveyorBuildNumber"""
                    "-p:VersionBuildMetadataPrefix=""$shortRevisionId.$AppveyorSourceControlBranchName."""
                )

            $testExecutionCliOptions += @('--test-adapter-path:.', '--logger:Appveyor')
        }

        Write-MajorSeparator
        Write-ActionTitle "Cleaning build output for the solution ""$solutionFilePath""."
        dotnet clean --verbosity:normal """$solutionFilePath""" 2>&1

        Write-MajorSeparator
        Write-ActionTitle "Restoring packages for the solution ""$solutionFilePath""."
        dotnet restore --verbosity:normal """$solutionFilePath""" @commonBuildPropertyArguments 2>&1

        Write-MajorSeparator
        Write-ActionTitle "Building the solution ""$solutionFilePath""."

        dotnet build `
            --verbosity:normal `
            """$solutionFilePath""" `
            --no-incremental `
            --no-restore `
            "-p:Platform=""$BuildPlatform""" `
            "--configuration:""$BuildConfiguration""" `
            @commonBuildPropertyArguments `
            2>&1

        Write-MajorSeparator
        Write-ActionTitle "Running tests for the solution ""$solutionFilePath""."

        dotnet test `
            """$solutionFilePath""" `
            @testExecutionCliOptions `
            "-p:Platform=""$BuildPlatform""" `
            "--configuration:""$BuildConfiguration""" `
            -- `
            RunConfiguration.TargetPlatform=x64 `
            2>&1

        Write-MajorSeparator
    }
    catch
    {
        [string] $errorDetails = Get-ErrorDetails
        [Console]::ResetColor()
        Write-MajorSeparator
        Write-Host -ForegroundColor Red $errorDetails
        Write-MajorSeparator

        throw
    }
}