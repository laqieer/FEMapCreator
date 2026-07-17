using FE_Map_Creator.Gui.Models;
using System;

#nullable disable
namespace FE_Map_Creator.Gui.Controls;

public sealed class Map_Cell_Event_Args : EventArgs
{
  public Map_Cell Cell { get; }

  public Map_Cell_Event_Args(Map_Cell cell)
  {
    this.Cell = cell;
  }
}
