using System.CommandLine;
using System.CommandLine.Parsing;

#nullable disable
namespace FE_Map_Creator.Cli.Commands;

/// <summary>
/// Binding helpers for options where the CLI's own default value must be distinguished
/// from an explicitly-typed value, so per-job JSON specs can still supply a value when
/// the option was left at its default (see <see cref="Cli_Validation.bound_value{T}"/>
/// for the equivalent problem inside Validators callbacks).
/// </summary>
internal static class Cli_Binding
{
  /// <summary>
  /// Returns the option's value only if the user actually typed it on the command line;
  /// otherwise null, so a later CLI-value-or-spec-value-or-default merge in the executor
  /// treats an untouched default the same as an omitted option.
  /// </summary>
  internal static T? explicit_value_or_null<T>(ParseResult parse_result, Option<T> option) where T : struct
  {
    OptionResult option_result = parse_result.GetResult(option);
    return option_result != null && !option_result.Implicit ? option_result.GetValueOrDefault<T>() : (T?) null;
  }

  internal static string explicit_value_or_null(ParseResult parse_result, Option<string> option)
  {
    OptionResult option_result = parse_result.GetResult(option);
    return option_result != null && !option_result.Implicit ? option_result.GetValueOrDefault<string>() : null;
  }
}
