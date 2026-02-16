# Windows Widget Tutorial (Cat Fact)

This tutorial builds a single Windows 11 widget provider end-to-end with a focus on clarity and practical correctness.

## Audience

- C# developers new to Windows widgets
- people who want a minimal, working provider before adding advanced patterns

## Learning Path

1. [Step 01 - Architecture and Prerequisites](steps/01-architecture-and-prereqs.md)
2. [Step 02 - Create and Configure the Base Project](steps/02-create-project.md)
3. [Step 03 - Add Adaptive Card Templates](steps/03-adaptive-card-template.md)
4. [Step 04 - Build the Cat Fact Service](steps/04-catfact-service.md)
5. [Step 05 - Add the Widget State Model](steps/05-widget-state.md)
6. [Step 06 - Implement the Widget Provider Lifecycle](steps/06-widget-provider.md)
7. [Step 07 - Add COM Class Factory Boilerplate](steps/07-com-factory.md)
8. [Step 08 - Wire the Program Entry Point](steps/08-entry-point.md)
9. [Step 09 - Add Packaging and Manifest Declarations](steps/09-packaging-and-manifest.md)
10. [Step 10 - Build, Deploy, and Test the Widget](steps/10-build-deploy-test.md)
11. [Step 11 - Troubleshooting](steps/11-troubleshooting.md)
12. [Step 12 - Production Extras (Lightweight)](steps/12-production-extras.md)
13. [Step 13 - Final Wrap-Up and Next Steps](steps/13-next-steps.md)

## What This Series Prioritizes

- one widget definition, one provider, one clean flow
- minimal but correct COM + MSIX wiring
- practical troubleshooting for real failures
- no unnecessary framework abstractions

## Glossary

- **Widget Host**: Windows component that renders widget cards and invokes provider callbacks.
- **Widget Provider**: your COM-activated app process implementing `IWidgetProvider`.
- **Adaptive Card**: JSON template rendered by the host with runtime data bindings.
- **CustomState**: host-persisted string used to store per-widget state.
- **MSIX**: packaging format required for widget discovery/activation.
- **CLSID**: GUID used for COM activation and provider class identity.
- **MsixPackage profile**: Visual Studio launch profile used for single-project MSIX deploy/debug.

## Repo Pointers

- Source code: `src/Xakpc.Widgets.Playground`
- Tutorial steps: `tutorial/windows-widget/steps`
- Placeholder assets index: `tutorial/windows-widget/assets/placeholders/README.md`
