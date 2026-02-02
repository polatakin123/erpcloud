using Xunit;

// Disable test parallelization to prevent InMemory DB conflicts
// This ensures deterministic test execution across the entire assembly
[assembly: CollectionBehavior(DisableTestParallelization = true)]
