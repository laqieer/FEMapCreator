#nullable disable
namespace FE_Map_Creator.Cli.Execution;

/// <summary>
/// Derives stable, collision-resistant per-job seeds for homogeneous <c>generate --count</c>
/// batches from a single base seed, so a whole batch is reproducible from one number while
/// still giving each job an independent-looking seed (not a trivially adjacent
/// <c>base_seed + index</c> sequence).
/// </summary>
internal static class Seed_Derivation
{
  /// <summary>
  /// Combines <paramref name="base_seed"/> and the 1-based <paramref name="job_index"/>
  /// with the public-domain splitmix64 finalizer (avalanche mixer): every input bit
  /// affects every output bit, so consecutive indices do not produce correlated or
  /// adjacent seeds, while the result is fully deterministic and reproducible from the
  /// same (base_seed, job_index) pair. Collisions between two indices in the same batch
  /// are only possible if the finalizer itself collides, which is exponentially
  /// unlikely for any realistic --count.
  /// </summary>
  internal static int derive(int base_seed, int job_index)
  {
    ulong x = unchecked((ulong)(uint)base_seed * 0x9E3779B97F4A7C15UL + (ulong)(uint)job_index);
    x ^= x >> 30;
    x *= 0xBF58476D1CE4E5B9UL;
    x ^= x >> 27;
    x *= 0x94D049BB133111EBUL;
    x ^= x >> 31;
    return unchecked((int)(uint)x);
  }
}
