using System;
using System.IO;

#nullable disable
namespace FE_Map_Creator.Cli;

/// <summary>
/// Injectable stdout/stderr sink so commands and tests do not depend on <see cref="Console"/> directly.
/// </summary>
internal sealed class Cli_Output
{
  internal TextWriter Out { get; }

  internal TextWriter Error { get; }

  internal Cli_Output(TextWriter output, TextWriter error)
  {
    this.Out = output ?? throw new ArgumentNullException(nameof (output));
    this.Error = error ?? throw new ArgumentNullException(nameof (error));
  }

  internal static Cli_Output console()
  {
    return new Cli_Output(Console.Out, Console.Error);
  }
}
