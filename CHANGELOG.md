# Release Notes

## [Unreleased]

#### Removed

- **Breaking:** the `CatLib.IO` module (`CombineStream`, `RingBufferStream`,
  `SegmentStream`, `WrapperStream`, `StreamExtensions`) has been deleted.
  These were stream utilities with no dependency on, or from, the container
  and did not belong in a DI package.
- **Breaking:** the `CatLib.Util` grab-bag has been removed entirely.
  `Arr`, `Str`, `SortSet`, `InternalHelper` and finally `Guard` are all
  gone; their few in-tree call sites were replaced with BCL equivalents
  (`List<T>` + `HashSet<T>`, LINQ, inline `Array.Copy`, and inline
  `if (x is null) throw new ArgumentNullException(nameof(x));`).
- **Breaking:** the `CatLib.Exception` namespace is gone. `RuntimeException`,
  `LogicException` and `AssertException` were thin reskins of
  `System.Exception` / `System.InvalidOperationException` and are replaced
  one-for-one by `System.InvalidOperationException` at every throw site
  (and at every `ExpectedException` attribute in the test project).
  `UnresolvableException` is preserved because it carries domain meaning,
  but its base class has changed from the deleted `RuntimeException` to
  `System.InvalidOperationException`.
- The corresponding test files were removed. Expect a drop from 732 to
  158 tests; the deleted ones all exercised the removed modules.

#### Changed

- **Breaking:** lifecycle events used to live in eleven one-type files under
  `CatLib/Events/`, each with its own copyright header and namespace block.
  They are now all defined together in `CatLib/Events/ApplicationEventArgs.cs`.
  Public types, members, and behaviour are unchanged; only the on-disk
  layout moved. `SA1402` (one-type-per-file) is disabled in
  `analysis.ruleset` because grouping tiny, tightly-related types is the
  whole point of this change.
- The old `Util.Guard` helper built exceptions via reflection *and* lost
  the parameter name on every `ArgumentNullException` it raised. Every
  null check is now an explicit `if (x is null) throw new
  ArgumentNullException(nameof(x));` at the call site (about ninety of
  them across nine files). Callers now actually see which parameter was
  null.
- `Container` no longer stores `findType` and `instanceTiming` in
  `SortSet`. `findType` is now a `List<(Func, int)>` kept sorted on
  insert by ascending priority; `instanceTiming` is a `List<string>`
  paired with a `HashSet<string>` so Flush still releases instances
  LIFO with O(1) existence checks.
- `ContainerExtensions.BindMethod(string, object, string)` inlines the
  identifier extraction previously provided by `Str.Method` as a private
  helper. Behavior is unchanged.
- CI and the test project moved to .NET 8 (LTS). The core library continues
  to target `netstandard2.0` and is **not** touched - Unity consumers on any
  supported Mono / IL2CPP runtime are unaffected. GitHub Actions now uses
  `actions/setup-dotnet@v4` with `dotnet-version: 8.0.x` and
  `actions/checkout@v4`. The `ubuntu-latest` leg of the matrix, which had
  been failing on the EOL .NET Core 3.0 SDK because of OpenSSL 3.x, passes
  again.
- Test dependencies bumped to current stable:
  `Microsoft.NET.Test.Sdk` 16.2 → 17.11.1, `MSTest.TestAdapter` /
  `MSTest.TestFramework` 2.0 → 3.6.4, `Moq` 4.13 → 4.20.72,
  `coverlet.msbuild` 2.7 → 6.0.2.
- Analyzers bumped: `Microsoft.CodeAnalysis.FxCopAnalyzers` (deprecated) is
  replaced by `Microsoft.CodeAnalysis.NetAnalyzers` 9.0.0; `StyleCop.Analyzers`
  1.1.118 → 1.2.0-beta.556; `SonarAnalyzer.CSharp` 7.17 → 10.3. The obsolete
  explicit `Microsoft.CodeAnalysis.CSharp` pin (only needed by FxCopAnalyzers)
  is removed. New Sonar rules that had actionable hits were fixed in place
  (`is T typed` pattern matching, `Array.Empty<T>()` instead of `null`
  collection returns); three rules remain intentionally suppressed in
  `analysis.ruleset` where the diagnostic conflicts with the container design
  (`S3011` non-public reflection, `S2955` generic null check, `S2325`
  make-method-static vs `SA1204` ordering).

#### Fixed

- `TestException` in the test project no longer exposes the obsolete
  `Exception(SerializationInfo, StreamingContext)` constructor (SYSLIB0051
  in .NET 8); the type is only thrown, never serialized.

#### Added

- `Application` lifecycle methods now explicitly enforce their
  single-threaded contract. `Bootstrap`, `Init`, `Register`, `Terminate`,
  `SetDispatcher` and the `DebugLevel` setter throw `LogicException` when
  invoked from any thread other than the one that constructed the
  `Application`. Previously this was an implicit assumption; calling these
  APIs from a worker thread silently corrupted internal state. Worker
  threads may still call `Make`, `Instance`, `Bind`, etc. on the
  thread-safe container surface.
- Container is now thread-safe. A single reentrant lock (exposed internally
  via `Container.SyncRoot` and shared with `MethodContainer`, `BindData` and
  `Bindable`) protects every mutable collection in the container graph.
  User-supplied closures (bind factories, `OnResolving` / `OnAfterResolving`
  / `OnRelease`, `Extend` closures, `OnRebound` callbacks) may re-enter the
  container from the same thread safely; they must not block waiting for
  another thread to re-enter the container from inside a closure.
- Informational micro-benchmark `TestBenchmarkMakeSingleton` measures
  `Make(singleton)` throughput (observed ~120 ns/op on .NET Core 3.0 /
  modern x64; the single-threaded overhead of the lock is dwarfed by
  reflection and dictionary work).

#### Fixed

- `Container.BuildStack` and `UserParamsStack` are now per-thread
  (`ThreadLocal<Stack<T>>`). Previously they were shared instance fields,
  which on concurrent `Make()` calls caused false-positive circular-
  dependency exceptions and corrupted user-param lookups.
- `Container.this[string]` setter, `Container.BindIf` (function overload),
  `BindData.Alias` / `BindData.Tag` and `Bindable<T>.Needs` are now atomic
  under the container lock. Previously a concurrent caller could slip
  between the check and the mutation and cause a spurious
  "already exists" `LogicException` or leak a partially-configured bind.
- `Bindable<T>.Needs` no longer caches a single `GivenData<T>` per
  bindable. Each call returns a fresh context so two threads running
  independent `Needs(...).Given(...)` chains can no longer clobber each
  other's pending `needs` field.
- `Container.Dispose(bool)` now runs under `SyncRoot`, ensuring that a
  late `Dispose()` call waits for in-flight `Make`/`Bind` calls on other
  threads to finish before tearing down the per-thread build stacks.

#### Changed

- `Container` implements `IDisposable` to release the per-thread build-
  stack handles. Test fixtures holding a `Container`/`Application` field
  must follow suit (done here for `TestsApplication` and `TestsBindData`).

#### Removed (Breaking)

- Removed the static `CatLib.App` facade class. The global static accessor was a
  Service Locator that hid dependencies, was not thread-safe, and had grown into
  a ~700-line god object. Resolve services via constructor injection of
  `IContainer` / `IApplication` (available as `ServiceProvider.App` inside
  `Register()` / `Init()`).
- Removed the generic `CatLib.Facade<TService>` static facade. Inject the
  service interface directly through the container instead.
- Removed the `Application.New(bool global = true)` static factory. Use
  `new Application()` directly; it no longer installs itself as a global
  singleton.

#### Changed (Breaking)

- Internal `Container` helpers renamed to match the surrounding verb-first
  convention (`Make*`, `Resolve*`, `Build`, `Inject`):
  - `SpeculatedServiceType` -> `SpeculateServiceType` (participle -> verb).
  - `MakeEmptyBindData` -> `CreateEmptyBindData` (`Make` is reserved for the
    public resolution pipeline; factory helpers use `Create*`).
  Note: `BindData.TriggerRelease` was intentionally left unchanged -- it is
  consistent with `TriggerResolving` / `TriggerAfterResolving` and the
  corresponding events (`OnRelease` / `OnResolving` / `OnAfterResolving`).
- Static helper classes renamed to the .NET plural convention:
  `ContainerExtension` -> `ContainerExtensions`,
  `BindDataExtension` -> `BindDataExtensions`,
  `StreamExtension` -> `StreamExtensions` (files renamed accordingly).
- `ServiceProvider.App` (protected property) renamed to `Application` to avoid
  collision with the (removed) static `App` class and to reflect the type
  (`IApplication`).
- Spelling fixes in public API:
  - `StartProcess.Bootstraped` -> `StartProcess.Bootstrapped`.
  - `Container.ResloveClass` / `ResloveAttrClass` / `ResloveFromContextual` ->
    `ResolveClass` / `ResolveAttrClass` / `ResolveFromContextual`.
  - `Container.MakeBuildFaildException` -> `MakeBuildFailedException`, and the
    corresponding error text "Create ... faild" -> "Create ... failed".
  - `MakeFromContextualClosure` parameter `ouput` -> `output`.

#### Fixed

- Internal field `Container.afterResloving` renamed to `afterResolving`; doc
  comments `gloabl`, `speified`, local variable `newGloablPosition`, and test
  names `TestBoostrap*` / `TestMakeAttributeInjectFaild*` /
  `TestPropertyInjectWithProtectedAccessFaild` corrected.

## [v2.0.0-alpha.1 (2019-12-04)](https://github.com/CatLib/Core/releases/tag/v2.0.0) 

#### Added

- `Inject` allowed to be set to optional.(#253 )
- `Arr.Test` can get matched value of the first match(#280 )

#### Changed

- Comments translated from Chinese into English(#133 )
- Defined Container.Build as a virtual function(#210 )
- Optimizes the constructor of `MethodContainer`(#218 )
- The default project uses the .net standard 2.0(#225 )
- Rename Util helper class to Helper class Change access level to internal.(#230 )
- `Application.IsRegisted` changed(rename) to `IsRegistered`(#226 ) 
- Use `VariantAttribute` to mark variable types instead of `IVariant`(#232 )
- `Guard` Will be expandable with `Guard.That`(#233 )
- Fixed the problem of container exception stack loss(#234 )
- Adjusted the internal file structure to make it clearer(#236 ).
- Add code analyzers (#206 )
- Refactoring event system (#177 )
- Refactoring `RingBuffer` make it inherit from `Stream`.(#238 )
- Namespace structure adjustment(optimization).(#241 )
- `App` can be extended by `That` (Handler rename to that) and removed `HasHandler` API (#242 )
- Unnecessary inheritance: WrappedStream(#247 )
- Clean up useless comment(#249 ).
- `Guard.Require` can set error messages and internal exceptions(#250).
- Exception class implementation support: SerializationInfo build(#252 ).
- Refactoring unit test, import moq.(#255 )
- `CodeStandardException` replaces to `LogicException`(#257 )
- Exception move to namespace `CatLib.Exception`(#258 )
- `Facade<>.Instance` changed to `Facade<>.That`(#259 )
- `Application.StartProcess` migrate to `StartProcess`(#260 )
- `Arr` optimization, lifting some unnecessary restrictions (#263)
- `Str` optimization, lifting some unnecessary restrictions (#264)
- Refactoring `SortSet`(#265 )
- Removed global params in application constructor. use Application.New() instead.(#267 )
- Containers are no longer thread-safe by default(#270 )

#### Fixed

- Fixed a bug that caused `Arr.Fill` to not work properly under special circumstances. (#255 )

#### Removed

- Removed `ExcludeFromCodeCoverageAttribute` (#229 )
- Removed unnecessary interface design `ISortSet`(#211 ).
- Removed `Version` classes and `Application.Compare` method.(#212).
- Removed `Template`  supported(#213 ).
- Removed `FilterChain` supported(#214 ).
- Removed `Enum` supported(#215 ).
- Removed `IAwait` interface(#217 ).
- Removed `Container.Flash`  api(#219 ).
- Removed `Arr.Flash` method(#220 ).
- Removed `Dict` helper class(#221 ).
- Removed `ThreadStatic` helper class(#223 ).
- Removed `QuickList` supported(#224 ).
- Removed `Storage` supported(#228 )
- Removed `SystemTime` class(#235 ).
- Removed `ICoroutineInit` feature from core library(#243 ).
- Removed the priority attribute, depending on the loading order(#244 ).
- Removed `Util.Encoding` (#245 ).
- Removed `Str.Encoding`(#246 )
- Removed `IServiceProviderType` feature in core library(#246 ).
- Removed unnecessary extension functions(#247 ).
- Removed `PipelineStream` stream.(#256 )
- Removed all `Obsolete` method and clean code.(#261 )
- Removed `App.Version`.(#266 )
