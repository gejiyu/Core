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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using CContainer = CatLib.Container.Container;
using SException = System.Exception;

namespace CatLib.Tests.Container
{
    /// <summary>
    /// Smoke-level concurrency tests for <see cref="CContainer"/>.
    /// These do not prove absence of races but regress the specific cases
    /// the reentrant SyncRoot and per-thread build stacks are meant to cover.
    /// </summary>
    [TestClass]
    public sealed class TestsContainerConcurrency
    {
        /// <summary>
        /// Gets or sets the <see cref="TestContext"/> used to surface
        /// benchmark output in the test runner.
        /// </summary>
        public TestContext TestContext { get; set; }

        [TestMethod]
        public void TestSingletonMakeIsConsistentAcrossThreads()
        {
            using var container = new CContainer();
            const string service = "shared";
            container.Bind(service, (c, args) => new object(), true);

            var seen = new ConcurrentBag<object>();
            Parallel.For(0, 10_000, _ => seen.Add(container.Make(service)));

            var first = container.Make(service);
            foreach (var item in seen)
            {
                Assert.AreSame(first, item, "All threads must receive the same singleton instance.");
            }
        }

        [TestMethod]
        [SuppressMessage("Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Concurrency stress test must capture any failure.")]
        public void TestIndependentMakesDoNotFalseTriggerCircularDependency()
        {
            using var container = new CContainer();
            for (var i = 0; i < 16; i++)
            {
                var index = i;
                container.Bind($"svc.{index}", (c, args) => index, false);
            }

            SException captured = null;
            Parallel.For(0, 50_000, i =>
            {
                try
                {
                    container.Make($"svc.{i % 16}");
                }
                catch (SException ex)
                {
                    Interlocked.CompareExchange(ref captured, ex, null);
                }
            });

            Assert.IsNull(captured, $"Concurrent Make() raised: {captured}");
        }

        [TestMethod]
        [SuppressMessage("Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Concurrency stress test must capture any failure.")]
        public void TestConcurrentBindAndMakeDoesNotCorruptState()
        {
            using var container = new CContainer();
            container.Bind("stable", (c, args) => 42, true);

            using var cts = new CancellationTokenSource();
            var makeErrors = 0;
            var makeLoop = Task.Run(() =>
            {
                while (!cts.IsCancellationRequested)
                {
                    try
                    {
                        var value = (int)container.Make("stable");
                        if (value != 42)
                        {
                            Interlocked.Increment(ref makeErrors);
                        }
                    }
                    catch (SException)
                    {
                        Interlocked.Increment(ref makeErrors);
                    }
                }
            });

            var bindErrors = 0;
            var bindLoop = Task.Run(() =>
            {
                for (var i = 0; i < 2_000; i++)
                {
                    var name = $"transient.{i}";
                    try
                    {
                        container.Bind(name, (c, args) => i, false);
                        container.Make(name);
                        container.Unbind(name);
                    }
                    catch (SException)
                    {
                        Interlocked.Increment(ref bindErrors);
                    }
                }
            });

            bindLoop.Wait();
            cts.Cancel();
            makeLoop.Wait();

            Assert.AreEqual(0, bindErrors, "Bind/Unbind loop raised errors.");
            Assert.AreEqual(0, makeErrors, "Make loop raised errors or saw torn values.");
        }

        [TestMethod]
        public void TestConcurrentExtendAppliesAllExtendersExactlyOnce()
        {
            using var container = new CContainer();
            container.Bind("counter", (c, args) => new List<int>(), false);

            const int extenderCount = 128;
            Parallel.For(0, extenderCount, i =>
            {
                container.Extend("counter", (instance, _) =>
                {
                    ((List<int>)instance).Add(i);
                    return instance;
                });
            });

            var list = (List<int>)container.Make("counter");
            Assert.AreEqual(extenderCount, list.Count);
        }

        /// <summary>
        /// Informational micro-benchmark: resolve a pre-bound singleton 1M
        /// times on a single thread and log the per-op cost. Fails only if
        /// each Make exceeds a generous ceiling (1 us) -- guards against
        /// catastrophic regressions, not noise.
        /// </summary>
        [TestMethod]
        [TestCategory("Benchmark")]
        public void TestBenchmarkMakeSingleton()
        {
            using var container = new CContainer();
            container.Bind("benchmark", (c, args) => new object(), true);

            _ = container.Make("benchmark");

            const int iterations = 1_000_000;
            var sw = Stopwatch.StartNew();
            for (var i = 0; i < iterations; i++)
            {
                _ = container.Make("benchmark");
            }

            sw.Stop();

            var nsPerOp = sw.Elapsed.TotalMilliseconds * 1_000_000d / iterations;
            TestContext.WriteLine(
                $"Make(singleton) x{iterations:N0} in {sw.ElapsedMilliseconds} ms => {nsPerOp:F1} ns/op");

            Assert.IsTrue(nsPerOp < 1000, $"Make is catastrophically slow ({nsPerOp:F1} ns/op).");
        }
    }
}
