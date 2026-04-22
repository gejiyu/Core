# CatLib.Core

[![License](https://img.shields.io/badge/license-MIT-blue.svg)](./LICENSE)
[![Build](https://github.com/gejiyu/Core/actions/workflows/catlib.yml/badge.svg?branch=master)](https://github.com/gejiyu/Core/actions/workflows/catlib.yml)

`CatLib.Core` is a lightweight dependency-injection container for .NET.
It targets `netstandard2.0`, which keeps it compatible with modern
.NET and with older Unity runtimes.

This repository is a trimmed fork of the original
[CatLib/Core](https://github.com/CatLib/Core). It focuses purely on the
IoC container, the `Application` lifecycle and the event dispatcher.
Everything else has been removed — see [Scope](#scope).

## Highlights

- **Single-file container.** `Container` / `IContainer` covers
  binding, resolving, aliases, tags, instance decoration, rebinding,
  release/resolving/after-resolving callbacks, and method binding.
- **Thread-safe.** Shared state is protected by a single reentrant
  monitor; per-thread build stacks live in `ThreadLocal<T>`. Stress
  tests cover parallel bind/make/release.
- **Main-thread-only lifecycle.** `Application.Bootstrap` / `Init` /
  `Register` / `Terminate` / `SetDispatcher` / `DebugLevel` throw
  `InvalidOperationException` if called off the thread that
  constructed the `Application`.
- **BCL-only exception surface.** `ArgumentNullException`,
  `ArgumentException`, `InvalidOperationException`. The only custom
  type left is `UnresolvableException`, which inherits from
  `InvalidOperationException`.
- **No hidden helpers.** No `Guard`, `Arr`, `Str`, `SortSet`,
  `RuntimeException`, `LogicException`, `AssertException` — null
  checks and invariant checks are inline at the call site.

## Scope

### What's in

| Module | What it does |
|--------|--------------|
| `CatLib.Container` | `Container`, `IContainer`, `BindData`, `MethodContainer`, extension methods, `UnresolvableException`. |
| `CatLib.CatLib` (Application) | `Application`, `IApplication`, `StartProcess`, `DebugLevel`, service providers, lifecycle events. |
| `CatLib.EventDispatcher` | `IEventDispatcher`, `EventDispatcher`, `EventArgs`-based dispatch. |

### What's out

The following modules used to ship with the package and have been
removed from this fork. If you need them, pull them from the upstream
CatLib repository or replace them with the BCL.

- **`CatLib.IO`** — `CombineStream`, `RingBufferStream`,
  `SegmentStream`, `WrapperStream`, `StreamExtensions`.
- **`CatLib.Util`** — `Arr`, `Str`, `SortSet`, `Guard`,
  `InternalHelper`. Null checks are now inline
  `ArgumentNullException` throws.
- **`CatLib.Exception`** — `RuntimeException`, `LogicException`,
  `AssertException`. The container raises `InvalidOperationException`
  directly.

## Requirements

- .NET SDK **8.0** to build and run tests.
- Produced assembly targets **`netstandard2.0`**; usable from any
  runtime that supports it (including legacy Unity).

## Build & Test

```shell
dotnet build CatLib.Core.sln -c Release
dotnet test  CatLib.Core.sln -c Release
```

CI runs on Windows, macOS and Ubuntu via
[GitHub Actions](https://github.com/gejiyu/Core/actions/workflows/catlib.yml).

## Pack

```shell
dotnet pack src/CatLib.Core -c Release
```

The resulting `.nupkg` lands in `src/CatLib.Core/bin/Release/`.

## Quick start

Resolve services against a bare container:

```csharp
using CatLib.Container;

var container = new Container();
container.Singleton<IMyService, MyService>();

var service = container.Make<IMyService>();
```

If you want the `Application` lifecycle (bootstraps, service providers,
lifecycle events), use it exactly like a container with two extra
phases up front:

```csharp
using CatLib;
using CatLib.Container;

var app = new Application();
app.Bootstrap();
app.Init();

app.Singleton<IMyService, MyService>();

var service = app.Make<IMyService>();
```

`Bootstrap`, `Init`, `Register`, `Terminate`, `SetDispatcher` and the
`DebugLevel` setter must be called from the thread that constructed
the `Application`; any other thread gets an
`InvalidOperationException`.

## Changelog

See [CHANGELOG.md](./CHANGELOG.md) for the list of changes since the
last upstream release.

## License

[MIT](./LICENSE)
