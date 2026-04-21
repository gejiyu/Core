/*
 * This file is part of the CatLib package.
 *
 * (c) CatLib <support@catlib.io>
 *
 * For the full copyright and license information, please view the LICENSE
 * file that was distributed with this source code.
 *
 * Document: https://catlib.io/
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using CatLib.Container;
using CatLib.EventDispatcher;
using CatLib.Exception;

namespace CatLib
{
    /// <summary>
    /// The CatLib <see cref="Application"/> instance.
    /// </summary>
    public class Application : Container.Container, IApplication
    {
        private static string version;
        private readonly IList<IServiceProvider> loadedProviders;
        private readonly int mainThreadId;
        private readonly IDictionary<Type, string> dispatchMapping;
        private bool bootstrapped;
        private bool inited;
        private bool registering;
        private long incrementId;
        private DebugLevel debugLevel;
        private IEventDispatcher dispatcher;

        /// <summary>
        /// Initializes a new instance of the <see cref="Application"/> class.
        /// </summary>
        public Application()
        {
            loadedProviders = new List<IServiceProvider>();

            mainThreadId = Thread.CurrentThread.ManagedThreadId;
            RegisterBaseBindings();

            dispatchMapping = new Dictionary<Type, string>()
            {
                { typeof(AfterBootEventArgs), ApplicationEvents.OnAfterBoot },
                { typeof(AfterInitEventArgs), ApplicationEvents.OnAfterInit },
                { typeof(AfterTerminateEventArgs), ApplicationEvents.OnAfterTerminate },
                { typeof(BeforeBootEventArgs), ApplicationEvents.OnBeforeBoot },
                { typeof(BeforeInitEventArgs), ApplicationEvents.OnBeforeInit },
                { typeof(BeforeTerminateEventArgs), ApplicationEvents.OnBeforeTerminate },
                { typeof(BootingEventArgs), ApplicationEvents.OnBooting },
                { typeof(InitProviderEventArgs), ApplicationEvents.OnInitProvider },
                { typeof(RegisterProviderEventArgs), ApplicationEvents.OnRegisterProvider },
                { typeof(StartCompletedEventArgs), ApplicationEvents.OnStartCompleted },
            };

            // We use closures to save the current context state
            // Do not change to: OnFindType(Type.GetType) This
            // causes the active assembly to be not the expected scope.
            OnFindType(finder => { return Type.GetType(finder); });

            DebugLevel = DebugLevel.Production;
            Process = StartProcess.Construct;
        }

        /// <summary>
        /// Gets the CatLib <see cref="Application"/> version.
        /// </summary>
        public static string Version => version ?? (version = FileVersionInfo
                       .GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileVersion);

        /// <summary>
        /// Gets indicates the application startup process.
        /// </summary>
        public StartProcess Process { get; private set; }

        /// <inheritdoc />
        public bool IsMainThread => mainThreadId == Thread.CurrentThread.ManagedThreadId;

        /// <inheritdoc />
        public DebugLevel DebugLevel
        {
            get => debugLevel;
            set
            {
                GuardMainThread();
                debugLevel = value;
                this.Instance<DebugLevel>(debugLevel);
            }
        }

        /// <summary>
        /// Sets the event dispatcher.
        /// </summary>
        /// <param name="dispatcher">The event dispatcher instance.</param>
        public void SetDispatcher(IEventDispatcher dispatcher)
        {
            GuardMainThread();
            this.dispatcher = dispatcher;
            this.Instance<IEventDispatcher>(dispatcher);
        }

        /// <inheritdoc />
        public IEventDispatcher GetDispatcher()
        {
            return dispatcher;
        }

        /// <inheritdoc />
        public virtual void Terminate()
        {
            GuardMainThread();
            Process = StartProcess.Terminate;
            Raise(new BeforeTerminateEventArgs(this));
            Process = StartProcess.Terminating;
            Flush();
            Process = StartProcess.Terminated;
            Raise(new AfterTerminateEventArgs(this));
        }

        /// <summary>
        /// Bootstrap the given array of bootstrap classes.
        /// </summary>
        /// <param name="bootstraps">The given bootstrap classes.</param>
        public virtual void Bootstrap(params IBootstrap[] bootstraps)
        {
            GuardMainThread();
            if (bootstraps is null)
            {
                throw new ArgumentNullException(nameof(bootstraps));
            }

            if (bootstrapped || Process != StartProcess.Construct)
            {
                throw new LogicException($"Cannot repeatedly trigger the {nameof(Bootstrap)}()");
            }

            Process = StartProcess.Bootstrap;
            bootstraps = Raise(new BeforeBootEventArgs(bootstraps, this))
                            .GetBootstraps();
            Process = StartProcess.Bootstrapping;

            var existed = new HashSet<IBootstrap>();

            foreach (var bootstrap in bootstraps)
            {
                if (bootstrap == null)
                {
                    continue;
                }

                if (existed.Contains(bootstrap))
                {
                    throw new LogicException($"The bootstrap already exists : {bootstrap}");
                }

                existed.Add(bootstrap);

                var skipped = Raise(new BootingEventArgs(bootstrap, this))
                                .IsSkip;
                if (!skipped)
                {
                    bootstrap.Bootstrap();
                }
            }

            Process = StartProcess.Bootstrapped;
            bootstrapped = true;
            Raise(new AfterBootEventArgs(this));
        }

        /// <summary>
        /// Init all of the registered service provider.
        /// </summary>
        public virtual void Init()
        {
            GuardMainThread();
            if (!bootstrapped)
            {
                throw new LogicException($"You must call {nameof(Bootstrap)}() first.");
            }

            if (inited || Process != StartProcess.Bootstrapped)
            {
                throw new LogicException($"Cannot repeatedly trigger the {nameof(Init)}()");
            }

            Process = StartProcess.Init;
            Raise(new BeforeInitEventArgs(this));
            Process = StartProcess.Initing;

            foreach (var provider in loadedProviders)
            {
                InitProvider(provider);
            }

            inited = true;
            Process = StartProcess.Inited;
            Raise(new AfterInitEventArgs(this));

            Process = StartProcess.Running;
            Raise(new StartCompletedEventArgs(this));
        }

        /// <inheritdoc />
        public virtual void Register(IServiceProvider provider, bool force = false)
        {
            GuardMainThread();
            if (provider is null)
            {
                throw new ArgumentNullException(nameof(provider));
            }

            if (IsRegistered(provider))
            {
                if (!force)
                {
                    throw new LogicException($"Provider [{provider.GetType()}] is already register.");
                }

                loadedProviders.Remove(provider);
            }

            if (Process == StartProcess.Initing)
            {
                throw new LogicException($"Unable to add service provider during {nameof(StartProcess.Initing)}");
            }

            if (Process > StartProcess.Running)
            {
                throw new LogicException($"Unable to {nameof(Terminate)} in-process registration service provider");
            }

            if (provider is ServiceProvider baseProvider)
            {
                baseProvider.SetApplication(this);
            }

            var skipped = Raise(new RegisterProviderEventArgs(provider, this))
                            .IsSkip;
            if (skipped)
            {
                return;
            }

            try
            {
                registering = true;
                provider.Register();
            }
            finally
            {
                registering = false;
            }

            loadedProviders.Add(provider);

            if (inited)
            {
                InitProvider(provider);
            }
        }

        /// <inheritdoc />
        public bool IsRegistered(IServiceProvider provider)
        {
            if (provider is null)
            {
                throw new ArgumentNullException(nameof(provider));
            }

            return loadedProviders.Contains(provider);
        }

        /// <inheritdoc />
        public long GetRuntimeId()
        {
            return Interlocked.Increment(ref incrementId);
        }

        /// <summary>
        /// Initialize the specified service provider.
        /// </summary>
        /// <param name="provider">The specified service provider.</param>
        protected virtual void InitProvider(IServiceProvider provider)
        {
            Raise(new InitProviderEventArgs(provider, this));
            provider.Init();
        }

        /// <inheritdoc />
        protected override void GuardConstruct(string method)
        {
            if (registering)
            {
                throw new LogicException(
                    $"It is not allowed to make services or dependency injection in the {nameof(Register)} process, method:{method}");
            }

            base.GuardConstruct(method);
        }

        /// <summary>
        /// Ensures the current call is on the main thread (the thread that constructed
        /// this <see cref="Application"/>). Lifecycle operations (Bootstrap/Init/Register/
        /// Terminate/SetDispatcher/DebugLevel) are single-threaded by design; invoking
        /// them from a worker thread indicates a programming error.
        /// </summary>
        /// <param name="method">The calling member name, auto-filled by the compiler.</param>
        /// <exception cref="LogicException">
        /// Thrown when invoked from a thread other than the construction thread.
        /// </exception>
        protected void GuardMainThread([CallerMemberName] string method = null)
        {
            if (IsMainThread)
            {
                return;
            }

            throw new LogicException(
                $"{nameof(Application)}.{method} must be called from the main thread " +
                $"(id {mainThreadId}); current thread id is {Thread.CurrentThread.ManagedThreadId}.");
        }

        private void RegisterBaseBindings()
        {
            this.Singleton<IApplication>(() => this).Alias<Application>().Alias<IContainer>();
            SetDispatcher(new EventDispatcher.EventDispatcher());
        }

        private T Raise<T>(T args)
            where T : EventArgs
        {
            if (!dispatchMapping.TryGetValue(args.GetType(), out string eventName))
            {
                throw new AssertException($"Assertion error: Undefined event {args}");
            }

            if (dispatcher == null)
            {
                return args;
            }

            dispatcher.Raise(eventName, this, args);
            return args;
        }
    }
}
