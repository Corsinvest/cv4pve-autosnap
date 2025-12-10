# WinGet First Publication Guide

This folder contains **template manifests** for the **first manual submission** to WinGet.

After the first version is approved in microsoft/winget-pkgs, all future updates will be handled automatically by the WinGet Releaser workflow in `.github/workflows/publish.yml`.

## Prerequisites

1. ✅ Fork microsoft/winget-pkgs under **Corsinvest** organization
2. ✅ Create a GitHub release (the workflow will do this automatically when you push a tag)
3. ✅ Have `wingetcreate` installed (recommended) or prepare for manual PR

## Steps for First Publication

### Option A: Using wingetcreate (Recommended)

1. **Create a release** (or use existing):
   ```bash
   git tag v1.17.0
   git push origin v1.17.0
   # Wait for publish workflow to complete
   ```

2. **Install wingetcreate**:
   ```bash
   winget install wingetcreate
   ```

3. **Update the manifests**:
   - Replace `<VERSION>` with your version (e.g., `1.17.0` - without 'v')
   - Replace `<RELEASE_DATE>` with release date (e.g., `2025-01-15`)
   - Calculate SHA256 hashes:
     ```powershell
     # Download the release files first
     Invoke-WebRequest -Uri "https://github.com/Corsinvest/cv4pve-autosnap/releases/download/v1.17.0/cv4pve-autosnap.exe-win-x64.zip" -OutFile "cv4pve-autosnap.exe-win-x64.zip"
     Invoke-WebRequest -Uri "https://github.com/Corsinvest/cv4pve-autosnap/releases/download/v1.17.0/cv4pve-autosnap.exe-win-x86.zip" -OutFile "cv4pve-autosnap.exe-win-x86.zip"
     Invoke-WebRequest -Uri "https://github.com/Corsinvest/cv4pve-autosnap/releases/download/v1.17.0/cv4pve-autosnap.exe-win-arm64.zip" -OutFile "cv4pve-autosnap.exe-win-arm64.zip"

     # Calculate hashes
     Get-FileHash -Algorithm SHA256 cv4pve-autosnap.exe-win-x64.zip
     Get-FileHash -Algorithm SHA256 cv4pve-autosnap.exe-win-x86.zip
     Get-FileHash -Algorithm SHA256 cv4pve-autosnap.exe-win-arm64.zip
     ```
   - Replace `<SHA256_X64>`, `<SHA256_X86>`, `<SHA256_ARM64>` with the calculated hashes

4. **Validate the manifests**:
   ```bash
   wingetcreate validate .\deploy\winget\
   ```

5. **Submit to WinGet**:
   ```bash
   wingetcreate submit .\deploy\winget\ --token YOUR_GITHUB_PAT
   ```

### Option B: Manual PR

1. Follow steps 1 and 3 from Option A

2. **Fork and clone microsoft/winget-pkgs**:
   ```bash
   # Fork on GitHub: https://github.com/microsoft/winget-pkgs
   git clone https://github.com/Corsinvest/winget-pkgs.git
   cd winget-pkgs
   ```

3. **Create branch and add manifests**:
   ```bash
   git checkout -b corsinvest-cv4pve-autosnap-1.17.0

   # Create directory structure
   mkdir -p manifests/c/Corsinvest/cv4pve.autosnap/1.17.0

   # Copy the 3 manifest files
   cp ../cv4pve-autosnap/deploy/winget/*.yaml manifests/c/Corsinvest/cv4pve.autosnap/1.17.0/

   # Commit and push
   git add .
   git commit -m "New package: Corsinvest.cv4pve.autosnap version 1.17.0"
   git push origin corsinvest-cv4pve-autosnap-1.17.0
   ```

4. **Create Pull Request**:
   - Go to https://github.com/microsoft/winget-pkgs
   - Create PR from your branch
   - Wait for review and approval

## After First Publication

Once your first PR is merged:

1. ✅ **Setup automation** (if not already done):
   - Create GitHub PAT (classic) with `public_repo` scope
   - Add `WINGET_TOKEN` secret to repository or organization

2. ✅ **Future releases are automatic**:
   - Just create a new tag: `git tag v1.17.1 && git push origin v1.17.1`
   - The workflow will automatically create a PR to winget-pkgs
   - No manual intervention needed!

## File Structure

```
deploy/winget/
├── Corsinvest.cv4pve.autosnap.yaml                # Version manifest
├── Corsinvest.cv4pve.autosnap.installer.yaml      # Installer info (URLs, SHA256)
├── Corsinvest.cv4pve.autosnap.locale.en-US.yaml   # Package metadata
└── README.md                                       # This file
```

## References

- [WinGet Contributing Guide](https://github.com/microsoft/winget-pkgs/blob/master/CONTRIBUTING.md)
- [WinGet Manifest Schema](https://github.com/microsoft/winget-cli/tree/master/schemas)
- [wingetcreate Documentation](https://github.com/microsoft/winget-create)
