using BenchmarkDotNet.Running;

namespace Cntryl.Keys.Benchmarks;

static class Program
{
    static void Main(string[] args) => BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
}
