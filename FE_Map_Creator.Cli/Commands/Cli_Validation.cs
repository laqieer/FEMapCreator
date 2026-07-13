using System.CommandLine;
using System.CommandLine.Parsing;
using FE_Map_Creator.Cli.Requests;

#nullable disable
namespace FE_Map_Creator.Cli.Commands;

/// <summary>
/// Shared cross-option validation helpers used by command <c>Validators</c> callbacks.
/// Each helper adds a <see cref="SymbolResult.AddError"/> entry rather than throwing, so
/// System.CommandLine can report every violation in a single parse pass.
/// </summary>
internal static class Cli_Validation
{
  /// <summary>
  /// Reads an option's value the way command <c>Validators</c> must: as of System.CommandLine
  /// 2.0.0, <see cref="CommandResult.GetValue{T}(Option{T})"/> returns <c>default(T)</c> (not the
  /// option's configured default) inside a Validators callback when the option was not supplied on
  /// the command line, because default-value factories are only applied later during binding. This
  /// falls back to the option's own configured default in that case.
  /// </summary>
  internal static T bound_value<T>(CommandResult result, Option<T> option)
  {
    OptionResult option_result = result.GetResult(option);
    if (option_result != null)
      return option_result.GetValueOrDefault<T>();
    return option.HasDefaultValue ? (T)option.GetDefaultValue() : default;
  }

  internal static void positive(CommandResult result, int? value, string option_name)
  {
    if (value.HasValue && value.Value <= 0)
      result.AddError($"{option_name} must be a positive number.");
  }

  internal static void non_negative(CommandResult result, int value, string option_name)
  {
    if (value < 0)
      result.AddError($"{option_name} must be zero or greater.");
  }

  internal static void valid_depth(CommandResult result, int depth)
  {
    if (depth != 1 && depth != 2)
      result.AddError("--depth must be 1 or 2.");
  }

  internal static void mutually_exclusive_flags(
    CommandResult result,
    bool first_value,
    string first_name,
    bool second_value,
    string second_name)
  {
    if (first_value && second_value)
      result.AddError($"{first_name} and {second_name} cannot both be specified.");
  }

  internal static void mutually_exclusive_values(
    CommandResult result,
    string first_value,
    string first_name,
    string second_value,
    string second_name)
  {
    if (!string.IsNullOrWhiteSpace(first_value) && !string.IsNullOrWhiteSpace(second_value))
      result.AddError($"{first_name} and {second_name} cannot both be specified.");
  }

  internal static Cli_Map_Format? parse_format(string value)
  {
    if (string.IsNullOrWhiteSpace(value))
      return null;
    switch (value.Trim().ToLowerInvariant())
    {
      case "map":
        return Cli_Map_Format.Map;
      case "mar":
        return Cli_Map_Format.Mar;
      case "tmx":
        return Cli_Map_Format.Tmx;
      default:
        return null;
    }
  }
}
