# Release Notes

## [Unreleased]

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

- `CombineStream.Seek` now passes the correct `paramName` and `actualValue` to
  `ArgumentOutOfRangeException` (was a literal description string).
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
