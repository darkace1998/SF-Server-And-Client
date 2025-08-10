# GitHub Actions CI/CD Pipeline

This repository includes comprehensive GitHub Actions workflows for continuous integration, security analysis, and code quality checking.

## Workflows

### 🔄 Continuous Integration (`ci.yml`)
**Triggers:** Push and pull requests to `main` and `develop` branches
- **Lint and Security Analysis**: Builds with static analysis enabled
- **Dependency Vulnerability Scan**: Checks for known vulnerabilities in NuGet packages  
- **Code Quality Analysis**: Runs comprehensive code analysis and generates reports
- **Build Validation**: Tests builds across Ubuntu, Windows, and macOS

### 🔒 Security Analysis (`security.yml`)
**Triggers:** Push, pull requests, daily schedule (2 AM UTC), manual
- **Security Vulnerability Scan**: Deep security analysis with detailed reporting
- **Static Code Analysis**: Comprehensive analyzer output and categorization
- **Hardcoded Secrets Detection**: Basic pattern matching for potential secrets
- **Dependency Analysis**: Complete package dependency review

### ✨ Code Quality (`quality.yml`)
**Triggers:** Push and pull requests to `main` and `develop` branches
- **Linting and Code Style**: Code formatting validation with dotnet-format
- **Code Complexity Analysis**: Method complexity and duplication detection
- **Documentation Check**: API documentation coverage analysis

## Security Features

The workflows include:
- **Static Security Analysis**: Microsoft.CodeAnalysis.NetAnalyzers, SecurityCodeScan, SonarAnalyzer
- **Dependency Vulnerability Scanning**: Automatic detection of vulnerable packages
- **Code Quality Metrics**: Comprehensive warning categorization and reporting
- **Multi-platform Build Validation**: Ensures compatibility across operating systems

## Configuration

### Security Rules
Security analysis is configured through:
- `Directory.Build.props` - Global MSBuild security settings
- `security.globalconfig` - Security analyzer rules and severity levels
- Project files include security-focused analyzer packages

### Analyzer Packages
- `Microsoft.CodeAnalysis.NetAnalyzers` - Microsoft security rules
- `SecurityCodeScan.VS2019` - OWASP security scanning
- `SonarAnalyzer.CSharp` - SonarQube quality and security rules

## Artifacts

Each workflow run generates downloadable artifacts:
- **Code Analysis Results**: Build logs and analysis summaries
- **Security Reports**: Detailed security analysis findings
- **Lint Reports**: Code style and formatting analysis
- **Documentation Reports**: API documentation coverage

## Usage

### Running Locally
To run similar checks locally:

```bash
# Security analysis
cd SF-Server
dotnet build --configuration Release --verbosity normal

# Vulnerability check
dotnet list package --vulnerable

# Code formatting
dotnet tool install -g dotnet-format
dotnet format --verify-no-changes

# Dependency check
dotnet tool install -g dotnet-outdated-tool
dotnet outdated
```

### Workflow Status
Check the **Actions** tab in GitHub to view:
- ✅ Successful builds and security scans
- ⚠️ Warnings and code quality issues  
- ❌ Failed builds or security vulnerabilities
- 📊 Downloadable analysis reports

## Maintenance

### Daily Security Scan
The security workflow runs automatically every day at 2 AM UTC to check for:
- New vulnerability disclosures affecting dependencies
- Updated security rules and analysis patterns
- Dependency updates and compatibility issues

### Manual Triggers
All workflows can be manually triggered via the GitHub Actions interface using the "workflow_dispatch" trigger.