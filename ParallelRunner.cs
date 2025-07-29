using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProfitWin
{
    public class ParallelRunner
    {
        public async Task<List<(List<double> parameters, List<double> result)>> RunRandomAsync(
            Func<List<double>, List<double>> function,
            List<(double min, double max)> paramRanges,
            int maxConcurrent,
            int iterations)
        {
            var results = new ConcurrentBag<(List<double>, List<double>)>();
            var tasks = new List<Task>();
            var semaphore = new SemaphoreSlim(maxConcurrent);
            var rnd = new Random();

            for (int i = 0; i < iterations; i++)
            {
                await semaphore.WaitAsync();

                tasks.Add(Task.Run(() =>
                {
                    try
                    {
                        var parameters = paramRanges
                            .Select(range => rnd.NextDouble() * (range.max - range.min) + range.min)
                            .ToList();

                        var result = function(parameters);

                        results.Add((parameters, result));
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }));
            }

            await Task.WhenAll(tasks);

            return results.ToList();
        }

        public static List<List<T>> CartesianProduct<T>(List<List<T>> sequences)
        {
            IEnumerable<List<T>> product = new List<List<T>> { new List<T>() };

            foreach (var sequence in sequences)
            {
                product = from acc in product
                          from item in sequence
                          select new List<T>(acc) { item };
            }

            return product.ToList();
        }


        public async Task<List<(List<double> parameters, List<double> result)>> RunSampleGridAsync(
            Func<List<double>, List<double>> function,
            List<List<double>> perDimensionOptions,
            int maxConcurrent = 1)
        {
            var parameterCombinations = CartesianProduct(perDimensionOptions);

            var results = new ConcurrentBag<(List<double>, List<double>)>();
            var semaphore = new SemaphoreSlim(maxConcurrent);
            var tasks = new List<Task>();

            foreach (var sample in parameterCombinations)
            {
                await semaphore.WaitAsync();

                tasks.Add(Task.Run(() =>
                {
                    try
                    {
                        var result = function(sample);
                        results.Add((sample, result));
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }));
            }

            await Task.WhenAll(tasks);
            return results.ToList();
        }
    }

}
