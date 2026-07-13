#nullable disable
namespace FE_Map_Creator.Generation;

/// <summary>
/// Invoked whenever the engine commits a tile to a cell, so a WinForms caller (or any
/// other host) can invalidate just that region instead of the whole map.
/// </summary>
public delegate void Tile_Drawn_Callback(int x, int y, int tile_index);
