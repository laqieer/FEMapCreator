using System;
using FE_Map_Creator.Generation;

#nullable disable
namespace FE_Map_Creator.Cli.Execution;

/// <summary>
/// Builds the <see cref="Map_State"/> passed to <see cref="Map_Generation_Engine"/> from a
/// (possibly template-backed) tile grid plus an optional <see cref="Map_Job_Spec"/>'s
/// locked/drawn/terrain matrices, applying the CLI's own drawn/locked defaulting rules
/// (see plan.md's generate/repair semantics) on top of what <see cref="Map_Job_Spec"/>
/// itself already validates (rectangularity, cross-matrix dimensions).
/// </summary>
internal static class Map_State_Builder
{
  /// <summary>
  /// Generate: without a template every cell starts open (locking a cell would leave it
  /// permanently blank with no template to source a value from, so a lock without a
  /// template is treated as a conflict, same as any other locked-but-undrawn cell).
  /// With a template, an explicit spec <c>drawn</c> matrix wins; otherwise drawn defaults
  /// to the locked mask so template values are fixed only where explicitly locked.
  /// </summary>
  internal static Map_State build_for_generate(
    int[,] tiles,
    bool has_template,
    Map_Job_Spec spec,
    int width,
    int height)
  {
    bool[,] locked = spec?.locked_array(width, height) ?? new bool[width, height];
    bool[,] drawn;
    if (has_template && spec?.Drawn != null)
      drawn = spec.drawn_array(width, height, false);
    else
      drawn = (bool[,]) locked.Clone();
    int[,] terrain = spec?.terrain_array(width, height) ?? new int[width, height];

    spec?.validate_constraints(width, height);
    validate_locked_cells_are_drawn(locked, drawn, width, height);
    return new Map_State(tiles, drawn, locked, terrain);
  }

  /// <summary>
  /// Repair: every input cell starts drawn (tile 0 cells are repair holes the engine
  /// itself reopens); the spec's <c>drawn</c> matrix, if any, plays no role here.
  /// </summary>
  internal static Map_State build_for_repair(
    int[,] tiles,
    Map_Job_Spec spec,
    int width,
    int height)
  {
    bool[,] locked = spec?.locked_array(width, height) ?? new bool[width, height];
    bool[,] drawn = new bool[width, height];
    for (int y = 0; y < height; ++y)
    {
      for (int x = 0; x < width; ++x)
        drawn[x, y] = true;
    }
    int[,] terrain = spec?.terrain_array(width, height) ?? new int[width, height];

    spec?.validate_constraints(width, height);
    validate_locked_cells_are_drawn(locked, drawn, width, height);
    return new Map_State(tiles, drawn, locked, terrain);
  }

  private static void validate_locked_cells_are_drawn(bool[,] locked, bool[,] drawn, int width, int height)
  {
    for (int y = 0; y < height; ++y)
    {
      for (int x = 0; x < width; ++x)
      {
        if (locked[x, y] && !drawn[x, y])
          throw new InvalidOperationException($"Cell ({x},{y}) is locked but not drawn; locked cells must be drawn.");
      }
    }
  }
}
