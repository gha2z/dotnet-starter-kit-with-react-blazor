using FSH.Dashboard.Wasm.E2E.Tests.Infrastructure;
using Xunit;

namespace FSH.Dashboard.Wasm.E2E.Tests;

[CollectionDefinition("e2e", DisableParallelization = true)]
public sealed class E2ECollection : ICollectionFixture<BlazorAppServerFixture>;
