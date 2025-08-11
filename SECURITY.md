# Security Policy

## Supported Versions

We actively maintain and provide security updates for the following versions:

| Version | Supported          |
| ------- | ------------------ |
| Latest  | :white_check_mark: |
| < Latest| :x:                |

## Reporting a Vulnerability

We take security seriously. If you discover a security vulnerability, please report it responsibly:

### How to Report

1. **Email**: Send details to the repository maintainer via GitHub private communication
2. **GitHub Security**: Use GitHub's private vulnerability reporting feature
3. **GitHub Issues**: For non-sensitive security suggestions, create a public issue with the `security` label

### What to Include

Please include the following information in your report:

- **Description**: Clear description of the vulnerability
- **Location**: File/function/line where the issue exists
- **Impact**: What an attacker could achieve
- **Reproduction**: Step-by-step instructions to reproduce
- **Suggested Fix**: If you have ideas for remediation

### Response Timeline

- **Initial Response**: Within 48 hours
- **Status Update**: Within 1 week
- **Fix Timeline**: Depends on severity
  - Critical: Within 1 week
  - High: Within 2 weeks
  - Medium: Within 1 month
  - Low: Next scheduled release

## Security Measures

### Current Security Features

- **Static Analysis**: CodeQL, SonarAnalyzer, SecurityCodeScan
- **Dependency Scanning**: Automated vulnerability detection
- **Secret Scanning**: Pattern-based detection of hardcoded secrets
- **Code Review**: All changes require review
- **Automated Updates**: Dependabot for security patches

### Security Best Practices

- All dependencies are regularly updated
- Security analyzers run on every commit
- Secrets are managed via environment variables
- Network communication uses secure protocols
- Input validation on all external data

## Security Considerations for Deployment

### Server Security

- **Steam API Key**: Store securely, never commit to code
- **Network**: Configure firewall rules appropriately
- **Updates**: Keep server updated with latest security patches
- **Monitoring**: Monitor for unusual connection patterns
- **Logs**: Review logs regularly for security events

### Client Plugin Security

- **Source**: Only install from official releases
- **Verification**: Verify file integrity before installation
- **Updates**: Keep plugin updated to latest version
- **Permissions**: BepInEx runs with game privileges only

## Scope

This security policy applies to:

- SF-Server application and its dependencies
- SF-Lidgren client plugin
- Build and deployment scripts
- GitHub Actions workflows
- Documentation that might affect security

## Contact

For security-related questions or concerns, please contact the maintainer through GitHub.

---

*Last updated: 2025-01-08*