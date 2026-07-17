using System;

#nullable disable
namespace FE_Map_Creator.Editing;

public sealed class Map_Edit_Operation
{
  public string Action { get; set; }

  public string Shape { get; set; }

  public int? X { get; set; }

  public int? Y { get; set; }

  public int? EndX { get; set; }

  public int? EndY { get; set; }

  public int? Tile { get; set; }

  public int? Terrain { get; set; }

  public int? Width { get; set; }

  public int? Height { get; set; }

  public void validate(string context = "Map edit operation")
  {
    Map_Edit_Action action = this.action(context);
    if (action == Map_Edit_Action.Resize)
    {
      if (!this.Width.HasValue || this.Width.Value <= 0 ||
          !this.Height.HasValue || this.Height.Value <= 0)
      {
        throw new InvalidOperationException($"{context} resize requires positive width and height.");
      }
      return;
    }
    if (action == Map_Edit_Action.Clear_Locks || action == Map_Edit_Action.Clear_Terrain)
      return;

    if (!this.X.HasValue || !this.Y.HasValue)
      throw new InvalidOperationException($"{context} requires x and y coordinates.");
    Map_Edit_Shape shape = this.shape(context);
    if (shape == Map_Edit_Shape.Rectangle && (!this.EndX.HasValue || !this.EndY.HasValue))
      throw new InvalidOperationException($"{context} rectangle requires endX and endY coordinates.");
    if (action == Map_Edit_Action.Set_Tile && (!this.Tile.HasValue || this.Tile.Value < 0))
      throw new InvalidOperationException($"{context} set-tile requires a non-negative tile index.");
    if ((action == Map_Edit_Action.Require_Terrain || action == Map_Edit_Action.Forbid_Terrain) &&
        (!this.Terrain.HasValue || this.Terrain.Value <= 0))
    {
      throw new InvalidOperationException($"{context} terrain action requires a positive terrain id.");
    }
  }

  public Map_Edit_Action action(string context = "Map edit operation")
  {
    string value = normalize(this.Action);
    switch (value)
    {
      case "set-tile":
        return Map_Edit_Action.Set_Tile;
      case "erase":
        return Map_Edit_Action.Erase;
      case "lock":
        return Map_Edit_Action.Lock;
      case "unlock":
        return Map_Edit_Action.Unlock;
      case "require-terrain":
        return Map_Edit_Action.Require_Terrain;
      case "forbid-terrain":
        return Map_Edit_Action.Forbid_Terrain;
      case "resize":
        return Map_Edit_Action.Resize;
      case "clear-locks":
        return Map_Edit_Action.Clear_Locks;
      case "clear-terrain":
        return Map_Edit_Action.Clear_Terrain;
      default:
        throw new InvalidOperationException(
          $"{context} action \"{this.Action}\" is invalid; expected set-tile, erase, lock, unlock, " +
          "require-terrain, forbid-terrain, resize, clear-locks, or clear-terrain.");
    }
  }

  public Map_Edit_Shape shape(string context = "Map edit operation")
  {
    string value = normalize(this.Shape);
    if (value.Length == 0 || value == "cell")
      return Map_Edit_Shape.Cell;
    switch (value)
    {
      case "rectangle":
        return Map_Edit_Shape.Rectangle;
      case "flood-fill":
        return Map_Edit_Shape.Flood_Fill;
      default:
        throw new InvalidOperationException(
          $"{context} shape \"{this.Shape}\" is invalid; expected cell, rectangle, or flood-fill.");
    }
  }

  private static string normalize(string value)
  {
    return string.IsNullOrWhiteSpace(value)
      ? ""
      : value.Trim().Replace('_', '-').ToLowerInvariant();
  }
}
