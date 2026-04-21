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
using System.Diagnostics.CodeAnalysis;

namespace CatLib.Container
{
    /// <summary>
    /// Thrown when the container cannot resolve a service.
    /// </summary>
    /// <remarks>
    /// Inherits from <see cref="InvalidOperationException"/> (previously
    /// inherited from a custom <c>RuntimeException</c> base class). The
    /// container rejects a resolve request whenever its own state does
    /// not support the operation -- that is precisely the semantics
    /// <see cref="InvalidOperationException"/> is meant for.
    /// </remarks>
    [ExcludeFromCodeCoverage]
    public class UnresolvableException : InvalidOperationException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UnresolvableException"/> class.
        /// </summary>
        public UnresolvableException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UnresolvableException"/> class.
        /// </summary>
        /// <param name="message">The exception message.</param>
        public UnresolvableException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UnresolvableException"/> class.
        /// </summary>
        /// <param name="message">The exception message.</param>
        /// <param name="innerException">The inner exception.</param>
        public UnresolvableException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
