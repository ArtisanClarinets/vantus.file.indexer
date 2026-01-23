# Build and Installation Guide for Vantus File Indexer

This guide explains how to build the application and create an installer package for Windows users.

## Prerequisites

*   **Operating System**: Windows 10 (version 1809 or later) or Windows 11.
*   **Visual Studio**: Visual Studio 2022 (Community, Professional, or Enterprise).
    *   **Workloads required**:
        *   .NET Desktop Development
        *   Windows Application Development (WinUI 3)
*   **Windows App SDK**: Visual Studio should install the necessary build tools automatically.

## Building the Application

1.  Open `Vantus.FileIndexer.sln` in Visual Studio 2022.
2.  Set the startup project to **Vantus.Packaging** (right-click the project in Solution Explorer -> **Set as Startup Project**).
    *   *Note: Do not try to run Vantus.App directly unless you are debugging in unpackaged mode.*
3.  Select the **Release** configuration and your target platform (e.g., **x64**).
4.  Build the solution (`Ctrl+Shift+B` or **Build** -> **Build Solution**).

## Creating the Installer (MSIX)

The standard way to install WinUI 3 applications is via an MSIX package. This provides a clean, safe, and 100% reliable installation experience, ensuring all dependencies (like .NET runtime and Windows App SDK) are correctly handled.

1.  In Visual Studio, right-click the **Vantus.Packaging** project in Solution Explorer.
2.  Select **Publish** -> **Create App Packages...**
3.  Select **Sideloading** (unless you are publishing to the Microsoft Store).
4.  **Signing**: You will need to select or create a signing certificate.
    *   If you don't have one, click **Create...** and follow the prompts to create a self-signed test certificate.
    *   *Important: For distribution to other computers, you should ideally use a code signing certificate from a trusted authority. If using a self-signed certificate, the user must install the certificate to the "Trusted People" store on their machine before the MSIX will install.*
5.  **Select and Configure Packages**:
    *   Choose **Always use this version number** or check **Automatically increment**.
    *   Select **x64** (and ARM64 if needed). Uncheck x86 unless you specifically need it.
    *   Ensure **Release** configuration is selected.
6.  Click **Create**.

### Installation for the User

Visual Studio will generate an `AppPackages` folder. Inside, you will find an `.msixbundle` (or `.msix`) file.

**To Install:**
1.  Copy the `.msixbundle` file to the user's computer.
2.  Double-click the file.
3.  The Windows App Installer window will appear. Click **Install**.

*If the user encounters a "certificate not trusted" error (common with self-signed certs):*
1.  Right-click the `.msixbundle` -> **Properties**.
2.  Go to the **Digital Signatures** tab.
3.  Select the signature and click **Details**.
4.  Click **View Certificate** -> **Install Certificate**.
5.  Select **Local Machine**.
6.  Select **Place all certificates in the following store** -> **Trusted People**.
7.  Finish the wizard. Now the install will proceed.

## Alternative: Standalone .exe (Unpackaged)

If you strictly require a "setup.exe" style installer (like Inno Setup) and want to avoid MSIX:

1.  You must publish the app as "Unpackaged". This requires modifying the project file or using specific publish profiles, which is more complex and requires you to manually ensure the Windows App SDK Runtime is installed on the target machine.
2.  **Recommendation**: Stick to **MSIX (Vantus.Packaging)** for the most reliable Windows 10/11 experience.

## Automated Build Script

To simplify the packaging process, you can use the provided PowerShell script. This script builds the solution in Release mode and moves the generated installer to the repository root.

1.  Open PowerShell as Administrator (or Developer PowerShell for Visual Studio).
2.  Run the script:
    ```powershell
    .\scripts\publish_installer.ps1
    ```
3.  Upon success, you will find the installer at `.\VantusInstaller.msixbundle`.
    *   If a bootstrapper (`setup.exe`) was generated (requires signing/updates configuration), it will be at `.\VantusSetup.exe`.
