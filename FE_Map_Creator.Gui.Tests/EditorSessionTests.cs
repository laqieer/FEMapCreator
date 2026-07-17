using FE_Map_Creator.Gui.Models;

namespace FE_Map_Creator.Gui.Tests;

[TestClass]
public sealed class EditorSessionTests
{
  [TestMethod]
  public void PaintedTileZeroRemainsDistinctFromAnOpenCellAcrossUndo()
  {
    Editor_Session session = new Editor_Session(3, 2);

    session.begin_edit();
    session.apply_cell(1, 1, Editor_Mode.Tile, 0, 0);
    session.end_edit();

    Assert.AreEqual(0, session.Tiles[1, 1]);
    Assert.IsTrue(session.Drawn[1, 1]);
    Assert.IsTrue(session.undo());
    Assert.IsFalse(session.Drawn[1, 1]);
    Assert.IsTrue(session.redo());
    Assert.IsTrue(session.Drawn[1, 1]);
  }

  [TestMethod]
  public void LockAndTerrainEditsRemainMutuallyExclusive()
  {
    Editor_Session session = new Editor_Session(2, 2);
    session.begin_edit();
    session.apply_cell(0, 0, Editor_Mode.Tile, 4, 0);
    session.apply_cell(0, 0, Editor_Mode.Lock, 0, 0, true);
    session.end_edit();

    Assert.IsTrue(session.Locked[0, 0]);
    Assert.AreEqual(0, session.Terrain[0, 0]);

    session.begin_edit();
    session.apply_cell(0, 0, Editor_Mode.Terrain_Required, 0, 12);
    session.end_edit();

    Assert.IsFalse(session.Locked[0, 0]);
    Assert.AreEqual(12, session.Terrain[0, 0]);

    session.begin_edit();
    session.apply_cell(0, 0, Editor_Mode.Lock, 0, 0, true);
    session.end_edit();

    Assert.IsTrue(session.Locked[0, 0]);
    Assert.AreEqual(0, session.Terrain[0, 0]);
  }

  [TestMethod]
  public void RectangleAndFloodFillCreateSingleUndoableTransactions()
  {
    Editor_Session session = new Editor_Session(4, 3);
    session.begin_edit();
    session.apply_rectangle(
      new Map_Cell(0, 0),
      new Map_Cell(1, 2),
      Editor_Mode.Tile,
      7,
      0);
    session.end_edit();

    Assert.IsTrue(session.Drawn[1, 2]);
    Assert.IsFalse(session.Drawn[2, 2]);

    session.begin_edit();
    session.flood_fill(3, 1, Editor_Mode.Tile, 9, 0);
    session.end_edit();

    Assert.AreEqual(7, session.Tiles[0, 1]);
    Assert.AreEqual(9, session.Tiles[3, 1]);
    Assert.IsTrue(session.Drawn[2, 0]);

    Assert.IsTrue(session.undo());
    Assert.IsFalse(session.Drawn[3, 1]);
    Assert.IsTrue(session.Drawn[0, 1]);
    Assert.IsTrue(session.undo());
    Assert.IsFalse(session.Drawn[0, 1]);
  }

  [TestMethod]
  public void ResizePreservesOverlappingTileMetadata()
  {
    Editor_Session session = new Editor_Session(2, 2);
    session.begin_edit();
    session.apply_cell(1, 1, Editor_Mode.Tile, 15, 0);
    session.apply_cell(1, 1, Editor_Mode.Lock, 0, 0, true);
    session.end_edit();

    session.resize(4, 3);

    Assert.AreEqual(4, session.Width);
    Assert.AreEqual(3, session.Height);
    Assert.AreEqual(15, session.Tiles[1, 1]);
    Assert.IsTrue(session.Drawn[1, 1]);
    Assert.IsTrue(session.Locked[1, 1]);
    Assert.IsFalse(session.Drawn[3, 2]);
  }
}
