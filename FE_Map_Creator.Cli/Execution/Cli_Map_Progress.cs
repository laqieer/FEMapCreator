using System;
using System.IO;

#nullable disable
namespace FE_Map_Creator.Cli.Execution;

internal sealed class Cli_Map_Progress : IProgress<int>
{
  private readonly TextWriter Writer;
  private readonly string Operation;
  private readonly string Map_Label;
  private readonly int Total_Cells;
  private readonly int Report_Step;
  private int Next_Report;
  private int Last_Value;

  internal Cli_Map_Progress(TextWriter writer, string operation, string map_path, int total_cells)
  {
    this.Writer = writer ?? throw new ArgumentNullException(nameof (writer));
    this.Operation = operation;
    this.Map_Label = Path.GetFileName(map_path);
    this.Total_Cells = Math.Max(1, total_cells);
    this.Report_Step = Math.Max(1, this.Total_Cells / 10);
    this.Next_Report = 1;
  }

  public void Report(int value)
  {
    if (value <= this.Last_Value)
      return;
    this.Last_Value = value;
    if (value < this.Next_Report && value < this.Total_Cells)
      return;
    this.Next_Report = value + this.Report_Step;
    lock (this.Writer)
      this.Writer.WriteLine($"{this.Operation} progress: {this.Map_Label}: {value} cell(s) processed.");
  }

  internal void complete()
  {
    lock (this.Writer)
      this.Writer.WriteLine($"{this.Operation} progress: {this.Map_Label}: complete ({this.Last_Value} cell(s) processed).");
  }
}
