using System;
using FE_Map_Creator.Generation;

#nullable disable
namespace FE_Map_Creator.Cli.Execution;

internal static class Algorithm_Selection
{
  internal static Map_Generation_Algorithm resolve(string cli_value, string spec_value)
  {
    string value = Job_Merge.merge_string(cli_value, spec_value);
    if (string.IsNullOrWhiteSpace(value)
      || string.Equals(value.Trim(), "legacy", StringComparison.OrdinalIgnoreCase))
      return Map_Generation_Algorithm.Legacy;
    if (string.Equals(value.Trim(), "experimental", StringComparison.OrdinalIgnoreCase))
      return Map_Generation_Algorithm.Experimental_Constraint;
    throw new InvalidOperationException(
      $"Algorithm \"{value}\" is invalid; expected \"legacy\" or \"experimental\".");
  }

  internal static string suffix(Map_Generation_Algorithm algorithm)
  {
    return algorithm == Map_Generation_Algorithm.Experimental_Constraint
      ? " using experimental algorithm"
      : "";
  }
}
