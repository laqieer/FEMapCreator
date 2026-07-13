// Ported from FE_Map_Creator\Tile_Directions.cs into FE_Map_Creator.Core so it can be
// shared between the WinForms GUI and other callers (CLI, tests). Made public (was
// internal) since it now lives in a separate assembly. Values and ordering are unchanged.

#nullable disable
namespace FE_Map_Creator;

public enum Tile_Directions
{
  None,
  SW,
  Down,
  SE,
  Left,
  Center,
  Right,
  NW,
  Up,
  NE,
}
