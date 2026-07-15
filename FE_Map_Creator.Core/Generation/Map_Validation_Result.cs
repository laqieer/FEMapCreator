using System;
using System.Collections.Generic;

#nullable disable
namespace FE_Map_Creator.Generation;

public sealed class Map_Validation_Result
{
  public bool Is_Valid => this.Errors.Count == 0;
  public int Checked_Adjacency_Count { get; }
  public int Skipped_Zero_Cell_Count { get; }
  public IReadOnlyList<string> Errors { get; }

  public Map_Validation_Result(
    int checked_adjacency_count,
    int skipped_zero_cell_count,
    IReadOnlyList<string> errors)
  {
    this.Checked_Adjacency_Count = checked_adjacency_count;
    this.Skipped_Zero_Cell_Count = skipped_zero_cell_count;
    this.Errors = errors == null
      ? Array.Empty<string>()
      : new List<string>(errors).AsReadOnly();
  }
}
