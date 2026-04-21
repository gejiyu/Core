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
using CatLib.EventDispatcher;

namespace CatLib
{
    /// <summary>
    /// Base class for every event raised by an <see cref="IApplication"/>.
    /// </summary>
    /// <remarks>
    /// Historically every lifecycle event had its own file. They were all
    /// thin wrappers over the same two or three fields, so they now live
    /// together here. <see cref="ApplicationEvents"/> still holds the
    /// string keys used with <see cref="IEventDispatcher"/>.
    /// </remarks>
    public class ApplicationEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ApplicationEventArgs"/> class.
        /// </summary>
        /// <param name="application">The application instance.</param>
        public ApplicationEventArgs(IApplication application)
        {
            Application = application;
        }

        /// <summary>
        /// Gets the application instance.
        /// </summary>
        public IApplication Application { get; private set; }
    }

    /// <summary>
    /// Raised before the <see cref="Application.Bootstrap"/> pipeline runs.
    /// Handlers may replace the bootstrap list via <see cref="SetBootstraps"/>.
    /// </summary>
    public class BeforeBootEventArgs : ApplicationEventArgs
    {
        private IBootstrap[] bootstraps;

        /// <summary>
        /// Initializes a new instance of the <see cref="BeforeBootEventArgs"/> class.
        /// </summary>
        /// <param name="bootstraps">An array of the bootstrap list.</param>
        /// <param name="application">The application instance.</param>
        public BeforeBootEventArgs(IBootstrap[] bootstraps, IApplication application)
            : base(application)
        {
            this.bootstraps = bootstraps;
        }

        /// <summary>
        /// Gets an array of bootstrap will be bootstrapped.
        /// </summary>
        /// <returns>Returns an array of bootstraps.</returns>
        public IBootstrap[] GetBootstraps()
        {
            return bootstraps;
        }

        /// <summary>
        /// Sets the bootstrap will replace the old boot list.
        /// </summary>
        /// <param name="bootstraps">New bootstrap list.</param>
        public void SetBootstraps(IBootstrap[] bootstraps)
        {
            this.bootstraps = bootstraps;
        }
    }

    /// <summary>
    /// Raised while a specific <see cref="IBootstrap"/> is being booted.
    /// Call <see cref="Skip"/> to skip the remaining bootstrap work.
    /// </summary>
    public class BootingEventArgs : ApplicationEventArgs, IStoppableEvent
    {
        private readonly IBootstrap bootstrap;

        /// <summary>
        /// Initializes a new instance of the <see cref="BootingEventArgs"/> class.
        /// </summary>
        /// <param name="bootstrap">The boot class that is booting.</param>
        /// <param name="application">The application instance.</param>
        public BootingEventArgs(IBootstrap bootstrap, IApplication application)
            : base(application)
        {
            IsSkip = false;
            this.bootstrap = bootstrap;
        }

        /// <summary>
        /// Gets a value indicating whether the boot class is skip booting.
        /// </summary>
        public bool IsSkip { get; private set; }

        /// <inheritdoc />
        public bool IsPropagationStopped => IsSkip;

        /// <summary>
        /// Gets the a boot class that is booting.
        /// </summary>
        /// <returns>Return the boot class.</returns>
        public IBootstrap GetBootstrap()
        {
            return bootstrap;
        }

        /// <summary>
        /// Disable the boot class.
        /// </summary>
        public void Skip()
        {
            IsSkip = true;
        }
    }

    /// <summary>
    /// Raised after the <see cref="Application.Bootstrap"/> pipeline completes.
    /// </summary>
    public class AfterBootEventArgs : ApplicationEventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AfterBootEventArgs"/> class.
        /// </summary>
        /// <param name="application">The application instance.</param>
        public AfterBootEventArgs(IApplication application)
            : base(application)
        {
        }
    }

    /// <summary>
    /// Raised when a service provider is about to be registered.
    /// Call <see cref="Skip"/> to cancel registration.
    /// </summary>
    public class RegisterProviderEventArgs : ApplicationEventArgs, IStoppableEvent
    {
        private readonly IServiceProvider provider;

        /// <summary>
        /// Initializes a new instance of the <see cref="RegisterProviderEventArgs"/> class.
        /// </summary>
        /// <param name="provider">The service provider class that will register.</param>
        /// <param name="application">The application instance.</param>
        public RegisterProviderEventArgs(IServiceProvider provider, IApplication application)
            : base(application)
        {
            IsSkip = false;
            this.provider = provider;
        }

        /// <summary>
        /// Gets a value indicating whether the service provider is skip register.
        /// </summary>
        public bool IsSkip { get; private set; }

        /// <inheritdoc />
        public bool IsPropagationStopped => IsSkip;

        /// <summary>
        /// Gets the a service provider class that will register.
        /// </summary>
        /// <returns>Return the service provider class.</returns>
        public IServiceProvider GetServiceProvider()
        {
            return provider;
        }

        /// <summary>
        /// Skip the register service provider.
        /// </summary>
        public void Skip()
        {
            IsSkip = true;
        }
    }

    /// <summary>
    /// Raised before the <see cref="Application.Init"/> pipeline runs.
    /// </summary>
    public class BeforeInitEventArgs : ApplicationEventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BeforeInitEventArgs"/> class.
        /// </summary>
        /// <param name="application">The application instance.</param>
        public BeforeInitEventArgs(IApplication application)
            : base(application)
        {
        }
    }

    /// <summary>
    /// Raised immediately before a specific service provider's <see cref="IServiceProvider.Init"/>.
    /// </summary>
    public class InitProviderEventArgs : ApplicationEventArgs
    {
        private readonly IServiceProvider provider;

        /// <summary>
        /// Initializes a new instance of the <see cref="InitProviderEventArgs"/> class.
        /// </summary>
        /// <param name="provider">The service provider class that will inited.</param>
        /// <param name="application">The application instance.</param>
        public InitProviderEventArgs(IServiceProvider provider, IApplication application)
            : base(application)
        {
            this.provider = provider;
        }

        /// <summary>
        /// Gets the a service provider class that will inited.
        /// </summary>
        /// <returns>Return the service provider class.</returns>
        public IServiceProvider GetServiceProvider()
        {
            return provider;
        }
    }

    /// <summary>
    /// Raised after all service providers have been initialised.
    /// </summary>
    public class AfterInitEventArgs : ApplicationEventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AfterInitEventArgs"/> class.
        /// </summary>
        /// <param name="application">The application instance.</param>
        public AfterInitEventArgs(IApplication application)
            : base(application)
        {
        }
    }

    /// <summary>
    /// Raised once the application has finished its start sequence and is ready.
    /// </summary>
    public class StartCompletedEventArgs : ApplicationEventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StartCompletedEventArgs"/> class.
        /// </summary>
        /// <param name="application">The application instance.</param>
        public StartCompletedEventArgs(IApplication application)
            : base(application)
        {
        }
    }

    /// <summary>
    /// Raised before the application starts tearing down.
    /// </summary>
    public class BeforeTerminateEventArgs : ApplicationEventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BeforeTerminateEventArgs"/> class.
        /// </summary>
        /// <param name="application">The terminate application instance.</param>
        public BeforeTerminateEventArgs(IApplication application)
            : base(application)
        {
        }
    }

    /// <summary>
    /// Raised after the application has finished terminating.
    /// </summary>
    public class AfterTerminateEventArgs : ApplicationEventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AfterTerminateEventArgs"/> class.
        /// </summary>
        /// <param name="application">The terminate application instance.</param>
        public AfterTerminateEventArgs(IApplication application)
            : base(application)
        {
        }
    }
}
