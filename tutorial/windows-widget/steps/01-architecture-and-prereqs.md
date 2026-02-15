# Step 01 - Architecture and Prerequisites

This step sets the baseline for the tutorial: correct local tooling and a clear mental model of how Windows widgets run.

## Goal

After this step, you should have:

- a machine ready to build and package a widget provider
- a clear understanding of Host vs Provider vs Adaptive Card responsibilities
- confidence about why COM, packaging, and Developer Mode are required

## Scope For This Tutorial

This series builds one widget only: a cat fact widget.

We intentionally keep the architecture simple:

- one provider class
- one widget definition
- one state model using `CustomState`
- one deploy path through packaging/manifest wiring

## What Is Explicitly Out Of Scope

The following advanced patterns are excluded on purpose:

- multi-widget frameworks and registries
- customization mode and analytics callback handling
- polling/subscription infrastructure and background workers
- DI container setup and abstraction-heavy design

These can be explored after the core tutorial is complete.

## Prerequisites (And Why)

- **Windows 11**: widgets run on the Widget Board (`Win+W`).
- **Visual Studio 2022 (17.8+)**: needed for packaging workflow and manifest editing.
- **.NET SDK**: required for modern C# SDK-style builds.
- **Windows SDK (10.0.19041.0+)**: required for Windows-targeted build and packaging tooling.
- **MSIX packaging support in Visual Studio**: required because widget providers are discovered from packaged app metadata.
- **Developer Mode enabled**: required for local sideloading and debugging.

## Setup Checklist

### 1. Visual Studio components

In Visual Studio Installer, verify:

- Workload: **.NET desktop development**
- Component: **Windows 10 SDK (10.0.19041.0+)** or newer
- Component: **MSIX Packaging Tools** (or equivalent)

### 2. Verify .NET SDK

Run:

```powershell
dotnet --info
```

Confirm at least one SDK is listed and command resolution works from PATH.

### 3. Enable Developer Mode

Open Windows Settings -> **For developers** and enable **Developer Mode**.

## Why Developer Mode Matters

During local development, your widget app is deployed as an MSIX package, not through Store distribution.
Without Developer Mode, Windows can block local deployment and package activation.

For widgets specifically, this affects:

- extension discovery from manifest metadata
- COM activation of your provider class
- local build/deploy/test loops

## Runtime Architecture (Simple View)

- **Widget Host (Windows)**: owns rendering surface, size, and lifecycle calls.
- **Widget Provider (your process)**: handles lifecycle callbacks, data loading, and state.
- **Adaptive Card**: JSON template plus data payload rendered by the host.

### Activation flow

1. User pins your widget in the Widget Board.
2. Host reads provider + widget definition from the package manifest.
3. Host activates your COM provider class by CLSID.
4. Host calls provider lifecycle methods such as `CreateWidget` and `OnActionInvoked`.
5. Provider sends template/data/state updates back to host.
6. Host renders the card and persists `CustomState`.

## What We Will Build In This Series

- a cat fact widget backed by a live API
- state persistence via `CustomState`
- minimal COM activation wiring
- minimal packaging + manifest setup
- build validation at each step

## Image Placeholders

- `[IMAGE: widget-board-overview.png]` - Widget Board with Add Widgets panel.
- `[IMAGE: visual-studio-installer-components.png]` - Required Visual Studio components.
- `[IMAGE: windows-developer-mode-toggle.png]` - Developer Mode enabled.
- `[IMAGE: architecture-sequence.png]` - Pin -> COM activation -> provider update -> render flow.

## Verify Step 01

1. Confirm prerequisites/components are installed.
2. Run `dotnet --info` successfully.
3. Confirm Developer Mode is enabled.
4. Run:

```powershell
dotnet build Xakpc.Widgets.Playground.sln
```

## Common Mistakes

- Missing Windows SDK or MSIX tooling in Visual Studio.
- Skipping Developer Mode and hitting false deployment/activation failures.
- Assuming the app draws widget UI directly (the host renders Adaptive Cards).

## Next Step

Step 02 configures the project for Windows widget development.
