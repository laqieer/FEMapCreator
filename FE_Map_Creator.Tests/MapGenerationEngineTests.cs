using FEXNA_Library;
using FE_Map_Creator.Generation;

namespace FE_Map_Creator.Tests;

[TestClass]
public sealed class MapGenerationEngineTests
{
  [TestMethod]
  public void GenerateFillsBlankMapAndReportsSeed()
  {
    Map_Generation_Engine engine = new Map_Generation_Engine(create_uniform_data(1));
    Map_State state = blank_state(4, 3);

    Map_Generation_Result result = engine.generate(state, new Map_Generation_Options()
    {
      Depth = 1,
      Seed = 1234
    });

    Assert.AreEqual(0, result.Unresolved_Tile_Count);
    Assert.AreEqual(1234, result.Seed);
    Assert.AreEqual(Map_Generation_Algorithm.Experimental_Constraint, result.Algorithm);
    assert_all_tiles(state, 1);
  }

  [TestMethod]
  public void GenerateProgressCountsSeedAndEveryNewCell()
  {
    Map_State state = blank_state(2, 2);
    Recording_Progress progress = new Recording_Progress();

    new Map_Generation_Engine(create_uniform_data(1)).generate(
      state,
      new Map_Generation_Options()
      {
        Algorithm = Map_Generation_Algorithm.Legacy,
        Seed = 1234
      },
      progress: progress);

    CollectionAssert.AreEqual(new int[] { 1, 2, 3, 4 }, progress.Values);
  }

  [TestMethod]
  public void GenerateWithSameSeedIsDeterministic()
  {
    Tileset_Generation_Data data = create_connected_data(1, 2);
    Map_State first = blank_state(6, 5);
    Map_State second = blank_state(6, 5);

    new Map_Generation_Engine(data).generate(first, new Map_Generation_Options()
    {
      Depth = 1,
      Seed = 42
    });
    new Map_Generation_Engine(data).generate(second, new Map_Generation_Options()
    {
      Depth = 1,
      Seed = 42
    });

    CollectionAssert.AreEqual(first.Tiles.Cast<int>().ToArray(), second.Tiles.Cast<int>().ToArray());
  }

  [TestMethod]
  public void GenerateWithFixedSeedPreservesStableOutput()
  {
    Map_State state = blank_state(6, 5);

    new Map_Generation_Engine(create_connected_data(1, 2)).generate(state, new Map_Generation_Options()
    {
      Algorithm = Map_Generation_Algorithm.Legacy,
      Depth = 1,
      Seed = 42
    });

    CollectionAssert.AreEqual(
      new int[]
      {
        1, 1, 1, 2, 1, 2,
        2, 1, 2, 1, 1, 1,
        2, 1, 2, 1, 2, 1,
        2, 1, 1, 1, 1, 2,
        1, 1, 2, 2, 1, 2
      },
      tiles_row_major(state));
  }

  [TestMethod]
  public void IdenticalAliasIndexPreservesOrderingAndSeededOutput()
  {
    Map_State state = blank_state(8, 1);

    new Map_Generation_Engine(create_alias_data()).generate(state, new Map_Generation_Options()
    {
      Algorithm = Map_Generation_Algorithm.Legacy,
      Depth = 1,
      Seed = 77
    });

    CollectionAssert.AreEqual(new int[] { 1, 1, 3, 3, 2, 3, 3, 1 }, tiles_row_major(state));
  }

  [TestMethod]
  public void GenerateWithAllCellsDrawnAndLockedIsNoOp()
  {
    Map_State state = new Map_State(
      new int[2, 2]
      {
        { 1, 3 },
        { 2, 4 }
      },
      filled_bool_array(2, 2, true),
      filled_bool_array(2, 2, true),
      new int[2, 2]);
    int[] expected_tiles = state.Tiles.Cast<int>().ToArray();
    bool[] expected_drawn = state.Drawn.Cast<bool>().ToArray();
    bool[] expected_locked = state.Locked.Cast<bool>().ToArray();

    Map_Generation_Result result = new Map_Generation_Engine(create_uniform_data(1)).generate(
      state,
      new Map_Generation_Options()
      {
        Seed = 5
      });

    Assert.AreEqual(0, result.Unresolved_Tile_Count);
    CollectionAssert.AreEqual(expected_tiles, state.Tiles.Cast<int>().ToArray());
    CollectionAssert.AreEqual(expected_drawn, state.Drawn.Cast<bool>().ToArray());
    CollectionAssert.AreEqual(expected_locked, state.Locked.Cast<bool>().ToArray());
  }

  [TestMethod]
  public void GeneratePreservesLockedTemplateTile()
  {
    Map_State state = blank_state(3, 3);
    state.Tiles[1, 1] = 1;
    state.Drawn[1, 1] = true;
    state.Locked[1, 1] = true;

    new Map_Generation_Engine(create_uniform_data(1)).generate(state, new Map_Generation_Options()
    {
      Seed = 7
    });

    Assert.AreEqual(1, state.Tiles[1, 1]);
    Assert.IsTrue(state.Locked[1, 1]);
    assert_all_tiles(state, 1);
  }

  [TestMethod]
  public void GenerateFillsDisconnectedRegionsSeparatedByLockedCell()
  {
    Map_State state = blank_state(5, 1);
    state.Tiles[0, 0] = 1;
    state.Drawn[0, 0] = true;
    state.Tiles[2, 0] = 1;
    state.Drawn[2, 0] = true;
    state.Locked[2, 0] = true;

    Map_Generation_Result result = new Map_Generation_Engine(create_uniform_data(1)).generate(
      state,
      new Map_Generation_Options()
      {
        Seed = 6
      });

    Assert.AreEqual(0, result.Unresolved_Tile_Count);
    Assert.AreEqual(1, state.Tiles[2, 0]);
    Assert.IsTrue(state.Locked[2, 0]);
    Assert.AreEqual(1, state.Tiles[1, 0]);
    Assert.AreEqual(1, state.Tiles[3, 0]);
    Assert.AreEqual(1, state.Tiles[4, 0]);
    Assert.IsTrue(state.Drawn[4, 0]);
  }

  [TestMethod]
  public void GenerateDisconnectedRegionsPreservesSeededSelectionOrder()
  {
    Map_State state = isolated_checkerboard_state(5, 5);

    Map_Generation_Result result = new Map_Generation_Engine(create_connected_data(1, 2)).generate(
      state,
      new Map_Generation_Options()
      {
        Algorithm = Map_Generation_Algorithm.Legacy,
        Seed = 42
      });

    Assert.AreEqual(0, result.Unresolved_Tile_Count);
    CollectionAssert.AreEqual(
      new int[]
      {
        2, 1, 1, 1, 1,
        1, 1, 1, 1, 1,
        1, 1, 1, 1, 1,
        1, 2, 1, 1, 1,
        2, 1, 1, 1, 1
      },
      tiles_row_major(state));
  }

  [TestMethod]
  public void GenerateHandlesManyIsolatedRegions()
  {
    Map_State state = isolated_checkerboard_state(40, 40);

    Map_Generation_Result result = new Map_Generation_Engine(create_uniform_data(1)).generate(
      state,
      new Map_Generation_Options()
      {
        Seed = 43
      });

    Assert.AreEqual(0, result.Unresolved_Tile_Count);
    assert_all_tiles(state, 1);
  }

  [TestMethod]
  public void TerrainConstraintFiltersCandidates()
  {
    Tileset_Generation_Data data = create_connected_data(1, 2);
    Data_Tileset metadata = new Data_Tileset()
    {
      Terrain_Tags = new List<int>() { 0, 1, 2 }
    };
    Map_State state = blank_state(3, 3);
    for (int y = 0; y < state.Height; ++y)
    {
      for (int x = 0; x < state.Width; ++x)
        state.Terrain[x, y] = 2;
    }

    new Map_Generation_Engine(data, metadata).generate(state, new Map_Generation_Options()
    {
      Seed = 8
    });

    assert_all_tiles(state, 2);
  }

  [TestMethod]
  public void NegativeTerrainConstraintExcludesForbiddenCandidates()
  {
    Tileset_Generation_Data data = create_connected_data(1, 2);
    Data_Tileset metadata = new Data_Tileset()
    {
      Terrain_Tags = new List<int>() { 0, 1, 2 }
    };
    Map_State state = blank_state(3, 3);
    for (int y = 0; y < state.Height; ++y)
    {
      for (int x = 0; x < state.Width; ++x)
        state.Terrain[x, y] = -1;
    }

    new Map_Generation_Engine(data, metadata).generate(state, new Map_Generation_Options()
    {
      Seed = 8
    });

    assert_all_tiles(state, 2);
  }

  [TestMethod]
  public void RepairPreservesLockedCellsAndFillsHole()
  {
    int[,] tiles = new int[3, 1]
    {
      { 1 },
      { 0 },
      { 1 }
    };
    bool[,] drawn = new bool[3, 1]
    {
      { true },
      { true },
      { true }
    };
    bool[,] locked = new bool[3, 1]
    {
      { true },
      { false },
      { false }
    };
    Map_State state = new Map_State(tiles, drawn, locked, new int[3, 1]);

    Map_Generation_Result result = new Map_Generation_Engine(create_uniform_data(1)).repair(
      state,
      new Map_Repair_Options()
      {
        Algorithm = Map_Generation_Algorithm.Legacy,
        Radius = 1,
        Depth = 1,
        Seed = 9
      });

    Assert.AreEqual(0, result.Unresolved_Tile_Count);
    Assert.AreEqual(1, state.Tiles[0, 0]);
    Assert.IsTrue(state.Locked[0, 0]);
    assert_all_tiles(state, 1);
  }

  [TestMethod]
  public void RepairPreservesLockedTerrainIncompatibleCell()
  {
    Data_Tileset metadata = new Data_Tileset()
    {
      Terrain_Tags = new List<int>() { 0, 1 }
    };
    Map_State state = new Map_State(
      new int[2, 1]
      {
        { 1 },
        { 1 }
      },
      filled_bool_array(2, 1, true),
      new bool[2, 1]
      {
        { true },
        { false }
      },
      new int[2, 1]
      {
        { 2 },
        { 2 }
      });

    Map_Generation_Result result = new Map_Generation_Engine(create_uniform_data(1), metadata).repair(
      state,
      new Map_Repair_Options()
      {
        Algorithm = Map_Generation_Algorithm.Legacy,
        Radius = 0,
        Depth = 1,
        Seed = 15
      });

    Assert.AreEqual(1, result.Unresolved_Tile_Count);
    Assert.AreEqual(1, state.Tiles[0, 0]);
    Assert.IsTrue(state.Drawn[0, 0]);
    Assert.IsTrue(state.Locked[0, 0]);
    Assert.AreEqual(0, state.Tiles[1, 0]);
    Assert.IsTrue(state.Drawn[1, 0]);
    Assert.IsFalse(state.Locked[1, 0]);
  }

  [TestMethod]
  public void RepairUsesLockedZeroAsHoleOriginWithoutChangingIt()
  {
    Map_State state = new Map_State(
      new int[2, 1]
      {
        { 0 },
        { 1 }
      },
      filled_bool_array(2, 1, true),
      new bool[2, 1]
      {
        { true },
        { false }
      },
      new int[2, 1]);
    int redrawn = 0;

    Map_Generation_Result result = new Map_Generation_Engine(create_uniform_data(1)).repair(
      state,
      new Map_Repair_Options()
      {
        Algorithm = Map_Generation_Algorithm.Legacy,
        Radius = 1,
        Depth = 1,
        Seed = 16
      },
      tile_drawn: (x, y, tile) => ++redrawn);

    Assert.AreEqual(1, result.Unresolved_Tile_Count);
    Assert.AreEqual(0, state.Tiles[0, 0]);
    Assert.IsTrue(state.Drawn[0, 0]);
    Assert.IsTrue(state.Locked[0, 0]);
    Assert.AreEqual(0, state.Tiles[1, 0]);
    Assert.IsTrue(state.Drawn[1, 0]);
    Assert.IsFalse(state.Locked[1, 0]);
    Assert.AreEqual(1, redrawn);
  }

  [TestMethod]
  public void GenerateWithEmptyConfigMarksEveryCellUnresolved()
  {
    Map_State state = blank_state(2, 2);
    Recording_Progress progress = new Recording_Progress();

    Map_Generation_Result result = new Map_Generation_Engine(create_generation_data()).generate(
      state,
      new Map_Generation_Options()
      {
        Algorithm = Map_Generation_Algorithm.Legacy,
        Seed = 12
      },
      progress: progress);

    Assert.AreEqual(4, result.Unresolved_Tile_Count);
    assert_all_tiles(state, 0);
    CollectionAssert.AreEqual(new int[] { 1, 2, 3, 4 }, progress.Values);
  }

  [TestMethod]
  public void GenerateWithImpossibleTerrainMarksEveryCellUnresolved()
  {
    Data_Tileset metadata = new Data_Tileset()
    {
      Terrain_Tags = new List<int>() { 0, 1, 2 }
    };
    Map_State state = blank_state(2, 2);
    for (int y = 0; y < state.Height; ++y)
    {
      for (int x = 0; x < state.Width; ++x)
        state.Terrain[x, y] = 999;
    }

    Map_Generation_Result result = new Map_Generation_Engine(create_connected_data(1, 2), metadata).generate(
      state,
      new Map_Generation_Options()
      {
        Algorithm = Map_Generation_Algorithm.Legacy,
        Seed = 13
      });

    Assert.AreEqual(4, result.Unresolved_Tile_Count);
    assert_all_tiles(state, 0);
  }

  [TestMethod]
  public void GenerateMarksOnlyImpossibleSeedCellUnresolved()
  {
    Data_Tileset metadata = new Data_Tileset()
    {
      Terrain_Tags = new List<int>() { 0, 1 }
    };
    Map_State state = blank_state(2, 1);
    state.Terrain[0, 0] = 999;

    Map_Generation_Result result = new Map_Generation_Engine(create_uniform_data(1), metadata).generate(
      state,
      new Map_Generation_Options()
      {
        Algorithm = Map_Generation_Algorithm.Legacy,
        Seed = 1
      });

    Assert.AreEqual(1, result.Unresolved_Tile_Count);
    Assert.AreEqual(0, state.Tiles[0, 0]);
    Assert.IsTrue(state.Drawn[0, 0]);
    Assert.AreEqual(1, state.Tiles[1, 0]);
    Assert.IsTrue(state.Drawn[1, 0]);
  }

  [TestMethod]
  public void GenerateReportsUnresolvedCells()
  {
    Tileset_Generation_Data data = create_generation_data();
    data.generation_data[1] = new Tile_Data();
    Map_State state = blank_state(2, 1);

    Map_Generation_Result result = new Map_Generation_Engine(data).generate(
      state,
      new Map_Generation_Options()
      {
        Algorithm = Map_Generation_Algorithm.Legacy,
        Seed = 10
      });

    Assert.AreEqual(1, result.Unresolved_Tile_Count);
    Assert.IsTrue(state.Drawn[0, 0]);
    Assert.IsTrue(state.Drawn[1, 0]);
    Assert.IsTrue(state.Tiles[0, 0] == 0 || state.Tiles[1, 0] == 0);
    Assert.HasCount(1, result.Unresolved_Cells);
  }

  [TestMethod]
  public void DepthTwoLookaheadAvoidsDepthOneDeadEnd()
  {
    Map_State depth_one = depth_fixture_state();
    Map_State depth_two = depth_fixture_state();

    Map_Generation_Result depth_one_result = new Map_Generation_Engine(create_depth_fixture_data()).generate(
      depth_one,
      new Map_Generation_Options()
      {
        Algorithm = Map_Generation_Algorithm.Legacy,
        Depth = 1,
        Seed = 0
      });
    Map_Generation_Result depth_two_result = new Map_Generation_Engine(create_depth_fixture_data()).generate(
      depth_two,
      new Map_Generation_Options()
      {
        Algorithm = Map_Generation_Algorithm.Legacy,
        Depth = 2,
        Seed = 0
      });

    Assert.AreEqual(2, depth_one_result.Unresolved_Tile_Count);
    CollectionAssert.AreEqual(new int[] { 1, 2, 0, 0 }, tiles_row_major(depth_one));
    Assert.AreEqual(0, depth_two_result.Unresolved_Tile_Count);
    CollectionAssert.AreEqual(new int[] { 1, 3, 5, 6 }, tiles_row_major(depth_two));
    assert_horizontal_adjacency(create_depth_fixture_data(), depth_two);
  }

  [TestMethod]
  public void ExperimentalDepthOneBacktracksAcrossLegacyDeadEnd()
  {
    Tileset_Generation_Data data = create_depth_fixture_data();
    Map_State state = depth_fixture_state();

    Map_Generation_Result result = new Map_Generation_Engine(data).generate(
      state,
      new Map_Generation_Options()
      {
        Algorithm = Map_Generation_Algorithm.Experimental_Constraint,
        Depth = 1,
        Seed = 0
      });

    Assert.AreEqual(Map_Generation_Algorithm.Experimental_Constraint, result.Algorithm);
    Assert.IsTrue(result.Is_Complete);
    Assert.AreEqual(0, result.Unresolved_Tile_Count);
    assert_horizontal_adjacency(data, state);
  }

  [TestMethod]
  public void ExperimentalReportsExhaustedSearchBudget()
  {
    Tileset_Generation_Data data = create_generation_data();
    data.generation_data[1] = new Tile_Data();
    data.generation_data[2] = new Tile_Data();
    Map_State state = blank_state(2, 1);

    Map_Generation_Result result = new Map_Generation_Engine(data).generate(
      state,
      new Map_Generation_Options()
      {
        Algorithm = Map_Generation_Algorithm.Experimental_Constraint,
        Depth = 1,
        Experimental_Search_Node_Limit = 1,
        Seed = 0
      });

    Assert.IsTrue(result.Search_Budget_Exhausted);
    Assert.AreEqual(1, result.Search_Node_Count);
  }

  [TestMethod]
  public void ExperimentalRejectsNonPositiveSearchBudget()
  {
    Assert.Throws<ArgumentOutOfRangeException>(() =>
      new Map_Generation_Engine(create_uniform_data(1)).generate(
        blank_state(1, 1),
        new Map_Generation_Options()
        {
          Algorithm = Map_Generation_Algorithm.Experimental_Constraint,
          Experimental_Search_Node_Limit = 0
        }));
  }

  [TestMethod]
  public void ExperimentalRejectsInvalidRestartAndNogoodSettings()
  {
    Map_Generation_Engine engine = new Map_Generation_Engine(create_uniform_data(1));

    Assert.Throws<ArgumentOutOfRangeException>(() =>
      engine.generate(
        blank_state(1, 1),
        new Map_Generation_Options()
        {
          Algorithm = Map_Generation_Algorithm.Experimental_Constraint,
          Experimental_Restart_Count = 0
        }));
    Assert.Throws<ArgumentOutOfRangeException>(() =>
      engine.generate(
        blank_state(1, 1),
        new Map_Generation_Options()
        {
          Algorithm = Map_Generation_Algorithm.Experimental_Constraint,
          Experimental_Nogood_Limit = -1
        }));
  }

  [TestMethod]
  public void ExperimentalImpossibleTerrainReturnsMinimumOpenCells()
  {
    Data_Tileset metadata = new Data_Tileset()
    {
      Terrain_Tags = new List<int>() { 0, 1, 2 }
    };
    Map_State state = blank_state(2, 2);
    for (int y = 0; y < state.Height; ++y)
    {
      for (int x = 0; x < state.Width; ++x)
        state.Terrain[x, y] = 999;
    }

    Map_Generation_Result result = new Map_Generation_Engine(create_connected_data(1, 2), metadata).generate(
      state,
      new Map_Generation_Options()
      {
        Algorithm = Map_Generation_Algorithm.Experimental_Constraint,
        Seed = 13
      });

    Assert.AreEqual(4, result.Unresolved_Tile_Count);
    Assert.HasCount(4, result.Unresolved_Cells);
    for (int y = 0; y < state.Height; ++y)
    {
      for (int x = 0; x < state.Width; ++x)
      {
        Assert.AreEqual(0, state.Tiles[x, y]);
        Assert.IsFalse(state.Drawn[x, y]);
      }
    }
  }

  [TestMethod]
  public void ExperimentalTreatsTileZeroAsValidCandidate()
  {
    Map_State state = blank_state(2, 2);

    Map_Generation_Result result = new Map_Generation_Engine(create_uniform_data(0)).generate(
      state,
      new Map_Generation_Options()
      {
        Algorithm = Map_Generation_Algorithm.Experimental_Constraint,
        Seed = 17
      });

    Assert.AreEqual(0, result.Unresolved_Tile_Count);
    assert_all_tiles(state, 0);
  }

  [TestMethod]
  public void ExperimentalDeduplicatesRedundantGenerationKeys()
  {
    Tileset_Generation_Data data = create_alias_data();
    data.generation_data[2] = new Tile_Data(data.generation_data[1]);
    Map_State state = blank_state(4, 1);

    Map_Generation_Result result = new Map_Generation_Engine(data).generate(
      state,
      new Map_Generation_Options()
      {
        Algorithm = Map_Generation_Algorithm.Experimental_Constraint,
        Seed = 22
      });

    Assert.AreEqual(0, result.Unresolved_Tile_Count);
    Assert.IsTrue(state.Drawn.Cast<bool>().All(drawn => drawn));
  }

  [TestMethod]
  public void ExperimentalMinimizesStructuralDeadEndToOneCell()
  {
    Tileset_Generation_Data data = create_generation_data();
    data.generation_data[1] = create_tile_data((6, 2, 1));
    data.generation_data[2] = create_tile_data((4, 1, 1));
    Map_State state = blank_state(3, 1);
    state.Tiles[0, 0] = 1;
    state.Drawn[0, 0] = true;
    state.Locked[0, 0] = true;

    Map_Generation_Result result = new Map_Generation_Engine(data).generate(
      state,
      new Map_Generation_Options()
      {
        Algorithm = Map_Generation_Algorithm.Experimental_Constraint,
        Seed = 21
      });

    Assert.AreEqual(1, result.Unresolved_Tile_Count);
    Assert.HasCount(1, result.Unresolved_Cells);
    Assert.AreEqual(1, state.Drawn.Cast<bool>().Count(drawn => !drawn));
  }

  [TestMethod]
  public void ExperimentalReportsAndSolvesDisconnectedComponents()
  {
    Map_State state = isolated_checkerboard_state(5, 5);

    Map_Generation_Result result = new Map_Generation_Engine(create_uniform_data(1)).generate(
      state,
      new Map_Generation_Options()
      {
        Algorithm = Map_Generation_Algorithm.Experimental_Constraint,
        Experimental_Search_Node_Limit = 13,
        Seed = 24
      });

    Assert.AreEqual(13, result.Search_Component_Count);
    Assert.HasCount(13, result.Components);
    Assert.AreEqual(result.Search_Node_Count, result.Components.Sum(component => component.Search_Node_Count));
    Assert.AreEqual(
      result.Unresolved_Tile_Count,
      result.Components.Sum(component => component.Unresolved_Tile_Count));
    Assert.AreEqual(0, result.Unresolved_Tile_Count);
    Assert.IsLessThanOrEqualTo(13, result.Search_Node_Count);
    assert_all_tiles(state, 1);
  }

  [TestMethod]
  public void ExperimentalComponentBudgetsNeverExceedTotalLimit()
  {
    Map_State state = isolated_checkerboard_state(5, 5);

    Map_Generation_Result result = new Map_Generation_Engine(create_generation_data()).generate(
      state,
      new Map_Generation_Options()
      {
        Algorithm = Map_Generation_Algorithm.Experimental_Constraint,
        Experimental_Search_Node_Limit = 5,
        Seed = 25
      });

    Assert.AreEqual(13, result.Search_Component_Count);
    Assert.AreEqual(13, result.Unresolved_Tile_Count);
    Assert.IsTrue(result.Search_Budget_Exhausted);
    Assert.IsLessThanOrEqualTo(5, result.Search_Node_Count);
  }

  [TestMethod]
  public void ExperimentalCarriesUnusedBudgetToLaterComponent()
  {
    Map_State state = blank_state(6, 1);
    state.Tiles[1, 0] = 99;
    state.Drawn[1, 0] = true;
    state.Locked[1, 0] = true;
    state.Tiles[2, 0] = 1;
    state.Drawn[2, 0] = true;
    state.Locked[2, 0] = true;

    Map_Generation_Result result = new Map_Generation_Engine(create_depth_fixture_data()).generate(
      state,
      new Map_Generation_Options()
      {
        Algorithm = Map_Generation_Algorithm.Experimental_Constraint,
        Experimental_Search_Node_Limit = 100,
        Seed = 28
      });

    Assert.AreEqual(2, result.Search_Component_Count);
    Assert.AreEqual(0, result.Components[0].Search_Node_Count);
    Assert.AreEqual(100, result.Components[1].Search_Node_Limit);
    Assert.IsLessThanOrEqualTo(100, result.Search_Node_Count);
    Assert.AreEqual(
      0,
      result.Unresolved_Tile_Count,
      string.Join(
        "; ",
        result.Components.Select(component =>
          $"{component.Origin.X},{component.Origin.Y}: nodes={component.Search_Node_Count}, unresolved={component.Unresolved_Tile_Count}, exhausted={component.Search_Budget_Exhausted}, propagation={component.Propagation_Removal_Count}")));
  }

  [TestMethod]
  public void ExperimentalPropagationPrunesUnsupportedDomains()
  {
    Tileset_Generation_Data data = create_generation_data();
    data.generation_data[1] = new Tile_Data();
    data.generation_data[2] = new Tile_Data();
    Map_State state = blank_state(2, 1);

    Map_Generation_Result result = new Map_Generation_Engine(data).generate(
      state,
      new Map_Generation_Options()
      {
        Algorithm = Map_Generation_Algorithm.Experimental_Constraint,
        Experimental_Search_Node_Limit = 2,
        Seed = 26
      });

    Assert.IsGreaterThan(0, result.Propagation_Removal_Count);
    Assert.AreEqual(1, result.Search_Component_Count);
    Assert.IsLessThanOrEqualTo(2, result.Search_Node_Count);
  }

  [TestMethod]
  public void ExperimentalBranchArcConsistencyDefaultsToExistingDeterministicBaseline()
  {
    Assert.IsFalse(
      new Map_Generation_Options().Experimental_Enable_Branch_Arc_Consistency);
    Assert.IsFalse(
      new Map_Repair_Options().Experimental_Enable_Branch_Arc_Consistency);
    (Map_State omitted_state, Map_Generation_Result omitted) =
      run_branch_arc_fixture(null, 20, 4);
    (Map_State false_state, Map_Generation_Result explicit_false) =
      run_branch_arc_fixture(false, 20, 4);
    int[] expected_tiles =
    {
      67, 999, 999,
      1, 68, 71,
      999, 76, 0
    };
    bool[] expected_drawn =
    {
      true, true, true,
      true, true, true,
      true, true, false
    };

    CollectionAssert.AreEqual(expected_tiles, tiles_row_major(omitted_state));
    CollectionAssert.AreEqual(expected_tiles, tiles_row_major(false_state));
    CollectionAssert.AreEqual(expected_drawn, drawn_row_major(omitted_state));
    CollectionAssert.AreEqual(expected_drawn, drawn_row_major(false_state));
    Assert.AreEqual(1, omitted.Unresolved_Tile_Count);
    Assert.AreEqual(20, omitted.Search_Node_Count);
    Assert.AreEqual(9, omitted.Propagation_Removal_Count);
    Assert.AreEqual(2, omitted.Search_Restart_Count);
    Assert.AreEqual(7, omitted.Nogood_Learned_Count);
    Assert.AreEqual(7, omitted.Nogood_Retained_Count);
    Assert.AreEqual(0, omitted.Nogood_Hit_Count);
    Assert.AreEqual(0, omitted.Backjump_Count);
    Assert.IsTrue(omitted.Search_Budget_Exhausted);
    Assert.HasCount(1, omitted.Unresolved_Cells);
    Assert.AreEqual(new Cell(2, 2), omitted.Unresolved_Cells[0]);
    Assert.HasCount(1, omitted.Components);
    Assert.AreEqual(0, omitted.Components[0].Best_Restart);

    Assert.AreEqual(omitted.Unresolved_Tile_Count, explicit_false.Unresolved_Tile_Count);
    Assert.AreEqual(omitted.Search_Node_Count, explicit_false.Search_Node_Count);
    Assert.AreEqual(omitted.Propagation_Removal_Count, explicit_false.Propagation_Removal_Count);
    Assert.AreEqual(omitted.Search_Restart_Count, explicit_false.Search_Restart_Count);
    Assert.AreEqual(omitted.Nogood_Learned_Count, explicit_false.Nogood_Learned_Count);
    Assert.AreEqual(omitted.Nogood_Retained_Count, explicit_false.Nogood_Retained_Count);
    Assert.AreEqual(omitted.Nogood_Hit_Count, explicit_false.Nogood_Hit_Count);
    Assert.AreEqual(omitted.Backjump_Count, explicit_false.Backjump_Count);
    Assert.AreEqual(omitted.Search_Budget_Exhausted, explicit_false.Search_Budget_Exhausted);
  }

  [TestMethod]
  public void ExperimentalBranchArcConsistencyIsDeterministic()
  {
    (Map_State first_state, Map_Generation_Result first) =
      run_branch_arc_fixture(true, 20, 4);
    (Map_State second_state, Map_Generation_Result second) =
      run_branch_arc_fixture(true, 20, 4);
    int[] expected_tiles =
    {
      67, 999, 999,
      66, 70, 71,
      999, 75, 73
    };

    CollectionAssert.AreEqual(expected_tiles, tiles_row_major(first_state));
    CollectionAssert.AreEqual(expected_tiles, tiles_row_major(second_state));
    CollectionAssert.AreEqual(drawn_row_major(first_state), drawn_row_major(second_state));
    Assert.IsTrue(first_state.Drawn.Cast<bool>().All(drawn => drawn));
    Assert.AreEqual(0, first.Unresolved_Tile_Count);
    Assert.AreEqual(19, first.Search_Node_Count);
    Assert.AreEqual(23, first.Propagation_Removal_Count);
    Assert.AreEqual(2, first.Search_Restart_Count);
    Assert.AreEqual(3, first.Nogood_Learned_Count);
    Assert.AreEqual(3, first.Nogood_Retained_Count);
    Assert.AreEqual(0, first.Nogood_Hit_Count);
    Assert.AreEqual(0, first.Backjump_Count);
    Assert.IsFalse(first.Search_Budget_Exhausted);
    Assert.AreEqual(1, first.Components[0].Best_Restart);

    Assert.AreEqual(first.Unresolved_Tile_Count, second.Unresolved_Tile_Count);
    Assert.AreEqual(first.Search_Node_Count, second.Search_Node_Count);
    Assert.AreEqual(first.Propagation_Removal_Count, second.Propagation_Removal_Count);
    Assert.AreEqual(first.Search_Restart_Count, second.Search_Restart_Count);
    Assert.AreEqual(first.Nogood_Learned_Count, second.Nogood_Learned_Count);
    Assert.AreEqual(first.Nogood_Retained_Count, second.Nogood_Retained_Count);
    Assert.AreEqual(first.Nogood_Hit_Count, second.Nogood_Hit_Count);
    Assert.AreEqual(first.Backjump_Count, second.Backjump_Count);
    CollectionAssert.AreEqual(
      first.Unresolved_Cells.Select(cell => (cell.X, cell.Y)).ToArray(),
      second.Unresolved_Cells.Select(cell => (cell.X, cell.Y)).ToArray());
  }

  [TestMethod]
  public void ExperimentalBranchArcConsistencyRestoresMultiwordDomainsAfterMultiHopContradiction()
  {
    Tileset_Generation_Data data = ExperimentalBranchArcConsistencyTestFixture.create_data();
    Map_State state = ExperimentalBranchArcConsistencyTestFixture.create_state();

    Map_Generation_Result result = new Map_Generation_Engine(
      data,
      ExperimentalBranchArcConsistencyTestFixture.create_metadata()).generate(
        state,
        new Map_Generation_Options()
        {
          Algorithm = Map_Generation_Algorithm.Experimental_Constraint,
          Experimental_Search_Node_Limit = 20,
          Experimental_Restart_Count = 2,
          Experimental_Nogood_Limit = 64,
          Experimental_Enable_Branch_Arc_Consistency = true,
          Seed = 4
        });

    Assert.HasCount(76, data.generation_data);
    Assert.AreEqual(
      ExperimentalBranchArcConsistencyTestFixture.Good_Root,
      state.Tiles[0, 1],
      "The candidate at zero-based index 65 was not restored after the failed branch.");
    Assert.AreEqual(0, result.Unresolved_Tile_Count);
    Assert.AreEqual(19, result.Search_Node_Count);
    Assert.AreEqual(23, result.Propagation_Removal_Count);
    assert_branch_arc_fixture_adjacency(data, state);
  }

  [TestMethod]
  public void ExperimentalBranchArcConsistencyHonorsSmallNodeLimits()
  {
    foreach ((int limit, int propagation_removals, int restarts) in
      new[] { (1, 0, 1), (2, 9, 2), (3, 10, 2) })
    {
      (Map_State first_state, Map_Generation_Result first) =
        run_branch_arc_fixture(true, limit, 4);
      (Map_State second_state, Map_Generation_Result second) =
        run_branch_arc_fixture(true, limit, 4);

      CollectionAssert.AreEqual(
        new int[]
        {
          67, 999, 999,
          1, 68, 71,
          999, 76, 0
        },
        tiles_row_major(first_state));
      CollectionAssert.AreEqual(tiles_row_major(first_state), tiles_row_major(second_state));
      CollectionAssert.AreEqual(drawn_row_major(first_state), drawn_row_major(second_state));
      Assert.AreEqual(1, first.Unresolved_Tile_Count);
      Assert.AreEqual(limit, first.Search_Node_Count);
      Assert.AreEqual(propagation_removals, first.Propagation_Removal_Count);
      Assert.AreEqual(restarts, first.Search_Restart_Count);
      Assert.AreEqual(0, first.Nogood_Learned_Count);
      Assert.IsTrue(first.Search_Budget_Exhausted);
      Assert.AreEqual(first.Unresolved_Tile_Count, second.Unresolved_Tile_Count);
      Assert.AreEqual(first.Search_Node_Count, second.Search_Node_Count);
      Assert.AreEqual(first.Propagation_Removal_Count, second.Propagation_Removal_Count);
      Assert.AreEqual(first.Search_Restart_Count, second.Search_Restart_Count);
      Assert.AreEqual(first.Search_Budget_Exhausted, second.Search_Budget_Exhausted);
    }
  }

  [TestMethod]
  public void ExperimentalRepairPassesBranchArcConsistencyAndPreservesOutsideRadius()
  {
    Map_State state = new Map_State(
      new int[5, 1]
      {
        { 1 },
        { 1 },
        { 0 },
        { 1 },
        { 0 }
      },
      new bool[5, 1]
      {
        { true },
        { true },
        { false },
        { true },
        { true }
      },
      new bool[5, 1]
      {
        { true },
        { false },
        { false },
        { false },
        { false }
      },
      new int[5, 1]);

    Map_Generation_Result result = new Map_Generation_Engine(create_uniform_data(1)).repair(
      state,
      new Map_Repair_Options()
      {
        Algorithm = Map_Generation_Algorithm.Experimental_Constraint,
        Experimental_Enable_Branch_Arc_Consistency = true,
        Radius = 1,
        Seed = 32
      });

    Assert.AreEqual(0, result.Unresolved_Tile_Count);
    Assert.AreEqual(1, state.Tiles[0, 0]);
    Assert.IsTrue(state.Drawn[0, 0]);
    Assert.IsTrue(state.Locked[0, 0]);
    for (int x = 1; x <= 3; ++x)
    {
      Assert.AreEqual(1, state.Tiles[x, 0]);
      Assert.IsTrue(state.Drawn[x, 0]);
    }
    Assert.AreEqual(0, state.Tiles[4, 0]);
    Assert.IsTrue(state.Drawn[4, 0]);
  }

  [TestMethod]
  public void HybridPassesBranchArcConsistencyToRegionalSolves()
  {
    Tileset_Generation_Data data = ExperimentalBranchArcConsistencyTestFixture.create_data();
    Data_Tileset metadata = ExperimentalBranchArcConsistencyTestFixture.create_metadata();
    Map_State disabled_state = ExperimentalBranchArcConsistencyTestFixture.create_state();
    Map_State enabled_state = ExperimentalBranchArcConsistencyTestFixture.create_state();

    Map_Generation_Result disabled = new Map_Generation_Engine(data, metadata).generate(
      disabled_state,
      new Map_Generation_Options()
      {
        Algorithm = Map_Generation_Algorithm.Experimental_Hybrid,
        Experimental_Search_Node_Limit = 20,
        Experimental_Restart_Count = 2,
        Experimental_Nogood_Limit = 64,
        Experimental_Enable_Branch_Arc_Consistency = false,
        Hybrid_Initial_Halo = 3,
        Hybrid_Max_Halo = 3,
        Seed = 4
      });
    Map_Generation_Result enabled = new Map_Generation_Engine(data, metadata).generate(
      enabled_state,
      new Map_Generation_Options()
      {
        Algorithm = Map_Generation_Algorithm.Experimental_Hybrid,
        Experimental_Search_Node_Limit = 20,
        Experimental_Restart_Count = 2,
        Experimental_Nogood_Limit = 64,
        Experimental_Enable_Branch_Arc_Consistency = true,
        Hybrid_Initial_Halo = 3,
        Hybrid_Max_Halo = 3,
        Seed = 4
      });

    Assert.AreEqual(1, disabled.Hybrid_Legacy_Unresolved_Tile_Count);
    Assert.AreEqual(1, disabled.Unresolved_Tile_Count);
    Assert.AreEqual(20, disabled.Search_Node_Count);
    Assert.AreEqual(9, disabled.Propagation_Removal_Count);
    Assert.AreEqual(1, disabled.Hybrid_Attempt_Count);
    Assert.IsFalse(disabled.Hybrid_Improved);
    Assert.IsTrue(disabled.Search_Budget_Exhausted);

    Assert.AreEqual(1, enabled.Hybrid_Legacy_Unresolved_Tile_Count);
    Assert.AreEqual(0, enabled.Unresolved_Tile_Count);
    Assert.AreEqual(19, enabled.Search_Node_Count);
    Assert.AreEqual(23, enabled.Propagation_Removal_Count);
    Assert.AreEqual(1, enabled.Hybrid_Attempt_Count);
    Assert.AreEqual(3, enabled.Hybrid_Halo);
    Assert.IsTrue(enabled.Hybrid_Improved);
    Assert.IsFalse(enabled.Search_Budget_Exhausted);
    Assert.AreEqual(
      ExperimentalBranchArcConsistencyTestFixture.Good_Root,
      enabled_state.Tiles[0, 1]);
    assert_branch_arc_fixture_adjacency(data, enabled_state);
  }

  [TestMethod]
  public void ExperimentalComponentProgressIsMonotonic()
  {
    Map_State state = isolated_checkerboard_state(5, 5);
    Recording_Progress progress = new Recording_Progress();

    new Map_Generation_Engine(create_uniform_data(1)).generate(
      state,
      new Map_Generation_Options()
      {
        Algorithm = Map_Generation_Algorithm.Experimental_Constraint,
        Seed = 27
      },
      progress: progress);

    Assert.IsNotEmpty(progress.Values);
    for (int index = 1; index < progress.Values.Count; ++index)
      Assert.IsGreaterThanOrEqualTo(progress.Values[index - 1], progress.Values[index]);
    Assert.AreEqual(13, progress.Values[^1]);
  }

  [TestMethod]
  public void ExperimentalConflictLearningReusesExactNogoods()
  {
    Tileset_Generation_Data data = create_parity_cycle_data(include_tail: false);
    Data_Tileset metadata = create_parity_cycle_metadata(include_tail: false);
    Map_State learned_state = create_parity_cycle_state(include_tail: false);
    Map_State chronological_state = create_parity_cycle_state(include_tail: false);

    Map_Generation_Result learned = new Map_Generation_Engine(data, metadata).generate(
      learned_state,
      new Map_Generation_Options()
      {
        Algorithm = Map_Generation_Algorithm.Experimental_Constraint,
        Experimental_Search_Node_Limit = 200,
        Experimental_Restart_Count = 4,
        Experimental_Nogood_Limit = 8,
        Experimental_Enable_Conflict_Learning = true,
        Seed = 29
      });
    Map_Generation_Result chronological = new Map_Generation_Engine(data, metadata).generate(
      chronological_state,
      new Map_Generation_Options()
      {
        Algorithm = Map_Generation_Algorithm.Experimental_Constraint,
        Experimental_Search_Node_Limit = 200,
        Experimental_Restart_Count = 4,
        Experimental_Nogood_Limit = 0,
        Experimental_Enable_Conflict_Learning = false,
        Seed = 29
      });

    Assert.AreEqual(learned.Unresolved_Tile_Count, chronological.Unresolved_Tile_Count);
    Assert.IsGreaterThan(0, learned.Nogood_Learned_Count);
    Assert.IsGreaterThan(0, learned.Nogood_Hit_Count);
    Assert.IsLessThanOrEqualTo(8, learned.Nogood_Retained_Count);
    Assert.IsLessThan(chronological.Search_Node_Count, learned.Search_Node_Count);
  }

  [TestMethod]
  public void ExperimentalConflictSearchBackjumpsPastIrrelevantAssignments()
  {
    Tileset_Generation_Data data = create_parity_cycle_data(include_tail: true);
    Data_Tileset metadata = create_parity_cycle_metadata(include_tail: true);
    bool observed_backjump = false;

    for (int seed = 0; seed < 64 && !observed_backjump; ++seed)
    {
      Map_Generation_Result result = new Map_Generation_Engine(data, metadata).generate(
        create_parity_cycle_state(include_tail: true),
        new Map_Generation_Options()
        {
          Algorithm = Map_Generation_Algorithm.Experimental_Constraint,
          Experimental_Search_Node_Limit = 200,
          Experimental_Restart_Count = 3,
          Experimental_Nogood_Limit = 32,
          Experimental_Enable_Conflict_Learning = true,
          Seed = seed
        });
      observed_backjump = result.Backjump_Count > 0;
    }

    Assert.IsTrue(observed_backjump, "No deterministic seed exercised a conflict-directed backjump.");
  }

  [TestMethod]
  public void ExperimentalLaterRestartCanCompleteAfterShortPartialRestart()
  {
    Tileset_Generation_Data data = create_depth_fixture_data();
    bool observed_later_success = false;
    for (int seed = 0; seed < 128 && !observed_later_success; ++seed)
    {
      Map_State state = depth_fixture_state();
      Map_Generation_Result result = new Map_Generation_Engine(data).generate(
        state,
        new Map_Generation_Options()
        {
          Algorithm = Map_Generation_Algorithm.Experimental_Constraint,
          Experimental_Search_Node_Limit = 10,
          Experimental_Restart_Count = 2,
          Seed = seed
        });
      observed_later_success = result.Unresolved_Tile_Count == 0
        && result.Search_Restart_Count == 2
        && result.Components[0].Best_Restart == 1;
      if (observed_later_success)
      {
        Assert.IsLessThanOrEqualTo(10, result.Search_Node_Count);
        assert_horizontal_adjacency(data, state);
      }
    }
    Assert.IsTrue(observed_later_success, "No deterministic seed required the later restart.");
  }

  [TestMethod]
  public void ExperimentalRestartConfigurationIsDeterministic()
  {
    Tileset_Generation_Data data = create_connected_data(1, 2);
    Map_State first = blank_state(6, 5);
    Map_State second = blank_state(6, 5);
    Map_Generation_Options options = new Map_Generation_Options()
    {
      Algorithm = Map_Generation_Algorithm.Experimental_Constraint,
      Experimental_Search_Node_Limit = 100,
      Experimental_Restart_Count = 5,
      Experimental_Nogood_Limit = 16,
      Seed = 31
    };

    Map_Generation_Result first_result = new Map_Generation_Engine(data).generate(first, options);
    Map_Generation_Result second_result = new Map_Generation_Engine(data).generate(second, options);

    CollectionAssert.AreEqual(first.Tiles.Cast<int>().ToArray(), second.Tiles.Cast<int>().ToArray());
    Assert.AreEqual(first_result.Search_Node_Count, second_result.Search_Node_Count);
    Assert.AreEqual(first_result.Search_Restart_Count, second_result.Search_Restart_Count);
    Assert.AreEqual(first_result.Nogood_Hit_Count, second_result.Nogood_Hit_Count);
  }

  [TestMethod]
  public void HybridExpandsHaloAndResolvesLegacyDeadEnd()
  {
    Tileset_Generation_Data data = create_depth_fixture_data();
    Map_State legacy_state = depth_fixture_state();
    Map_State hybrid_state = depth_fixture_state();

    Map_Generation_Result legacy = new Map_Generation_Engine(data).generate(
      legacy_state,
      new Map_Generation_Options()
      {
        Algorithm = Map_Generation_Algorithm.Legacy,
        Depth = 1,
        Seed = 0
      });
    Map_Generation_Result hybrid = new Map_Generation_Engine(data).generate(
      hybrid_state,
      new Map_Generation_Options()
      {
        Algorithm = Map_Generation_Algorithm.Experimental_Hybrid,
        Depth = 1,
        Hybrid_Initial_Halo = 0,
        Hybrid_Max_Halo = 3,
        Seed = 0
      });

    Assert.AreEqual(2, legacy.Unresolved_Tile_Count);
    Assert.AreEqual(Map_Generation_Algorithm.Experimental_Hybrid, hybrid.Algorithm);
    Assert.AreEqual(2, hybrid.Hybrid_Legacy_Unresolved_Tile_Count);
    Assert.AreEqual(1, hybrid.Hybrid_Halo);
    Assert.AreEqual(2, hybrid.Hybrid_Attempt_Count);
    Assert.AreEqual(0, hybrid.Unresolved_Tile_Count);
    Assert.IsFalse(hybrid.Hybrid_Attempts
      .Any(attempt => attempt.Region_Index == 0 && attempt.Halo > 1));
    Assert.AreEqual(
      hybrid.Search_Node_Count,
      hybrid.Hybrid_Attempts.Sum(attempt => attempt.Search_Node_Count));
    Assert.AreEqual(
      hybrid.Search_Node_Count,
      hybrid.Components.Sum(component => component.Search_Node_Count));
    assert_horizontal_adjacency(data, hybrid_state);
  }

  [TestMethod]
  public void HybridNeverWorsensLegacyResult()
  {
    Tileset_Generation_Data data = create_depth_fixture_data();
    Map_State legacy_state = depth_fixture_state();
    Map_State hybrid_state = depth_fixture_state();

    Map_Generation_Result legacy = new Map_Generation_Engine(data).generate(
      legacy_state,
      new Map_Generation_Options()
      {
        Algorithm = Map_Generation_Algorithm.Legacy,
        Depth = 1,
        Seed = 2
      });
    Map_Generation_Result hybrid = new Map_Generation_Engine(data).generate(
      hybrid_state,
      new Map_Generation_Options()
      {
        Algorithm = Map_Generation_Algorithm.Experimental_Hybrid,
        Depth = 1,
        Experimental_Search_Node_Limit = 1,
        Hybrid_Initial_Halo = 0,
        Hybrid_Max_Halo = 0,
        Seed = 2
      });

    Assert.IsLessThanOrEqualTo(legacy.Unresolved_Tile_Count, hybrid.Unresolved_Tile_Count);
  }

  [TestMethod]
  public void HybridPreservesCellsOutsideFinalHalo()
  {
    Tileset_Generation_Data data = create_depth_fixture_data();
    Map_State baseline = blank_state(6, 1);
    baseline.Tiles[0, 0] = 1;
    baseline.Drawn[0, 0] = true;
    baseline.Locked[0, 0] = true;
    baseline.Tiles[4, 0] = 99;
    baseline.Drawn[4, 0] = true;
    baseline.Locked[4, 0] = true;
    baseline.Tiles[5, 0] = 5;
    baseline.Drawn[5, 0] = true;
    Map_State legacy_state = clone_state(baseline);
    Map_State hybrid_state = clone_state(baseline);

    Map_Generation_Result legacy = new Map_Generation_Engine(data).generate(
      legacy_state,
      new Map_Generation_Options()
      {
        Algorithm = Map_Generation_Algorithm.Legacy,
        Depth = 1,
        Seed = 3
      });
    Map_Generation_Result hybrid = new Map_Generation_Engine(data).generate(
      hybrid_state,
      new Map_Generation_Options()
      {
        Algorithm = Map_Generation_Algorithm.Experimental_Hybrid,
        Depth = 1,
        Hybrid_Initial_Halo = 1,
        Hybrid_Max_Halo = 1,
        Seed = 3
      });

    Assert.IsGreaterThan(0, legacy.Unresolved_Tile_Count);
    Assert.AreEqual(legacy_state.Tiles[5, 0], hybrid_state.Tiles[5, 0]);
    Assert.AreEqual(legacy_state.Drawn[5, 0], hybrid_state.Drawn[5, 0]);
    Assert.IsLessThanOrEqualTo(legacy.Unresolved_Tile_Count, hybrid.Unresolved_Tile_Count);
  }

  [TestMethod]
  public void HybridPartialRegionsRemainIsolated()
  {
    Tileset_Generation_Data data = create_depth_fixture_data();
    Map_State state = blank_state(9, 1);
    state.Tiles[0, 0] = 1;
    state.Drawn[0, 0] = true;
    state.Locked[0, 0] = true;
    state.Tiles[4, 0] = 99;
    state.Drawn[4, 0] = true;
    state.Locked[4, 0] = true;
    state.Tiles[5, 0] = 1;
    state.Drawn[5, 0] = true;
    state.Locked[5, 0] = true;

    Map_Generation_Result result = new Map_Generation_Engine(data).generate(
      state,
      new Map_Generation_Options()
      {
        Algorithm = Map_Generation_Algorithm.Experimental_Hybrid,
        Depth = 1,
        Hybrid_Initial_Halo = 0,
        Hybrid_Max_Halo = 0,
        Seed = 7
      });

    Assert.AreEqual(4, result.Hybrid_Legacy_Unresolved_Tile_Count);
    Assert.AreEqual(2, result.Unresolved_Tile_Count);
    Assert.HasCount(2, result.Hybrid_Attempts);
    Assert.IsTrue(result.Hybrid_Attempts.All(attempt => attempt.Components.Count == 1));
    Assert.IsTrue(result.Hybrid_Attempts.All(attempt => attempt.Unresolved_Tile_Count == 1));
  }

  [TestMethod]
  public void HybridWithSameSeedIsDeterministic()
  {
    Tileset_Generation_Data data = create_depth_fixture_data();
    Map_State first = depth_fixture_state();
    Map_State second = depth_fixture_state();
    Map_Generation_Options options = new Map_Generation_Options()
    {
      Algorithm = Map_Generation_Algorithm.Experimental_Hybrid,
      Hybrid_Initial_Halo = 0,
      Hybrid_Max_Halo = 1,
      Seed = 4
    };

    Map_Generation_Result first_result = new Map_Generation_Engine(data).generate(first, options);
    Map_Generation_Result second_result = new Map_Generation_Engine(data).generate(second, options);

    CollectionAssert.AreEqual(first.Tiles.Cast<int>().ToArray(), second.Tiles.Cast<int>().ToArray());
    Assert.AreEqual(first_result.Hybrid_Halo, second_result.Hybrid_Halo);
    Assert.AreEqual(first_result.Search_Node_Count, second_result.Search_Node_Count);
  }

  [TestMethod]
  public void HybridFallbackKeepsLegacyResultAndReportsAttempts()
  {
    Map_State state = blank_state(2, 2);

    Map_Generation_Result result = new Map_Generation_Engine(create_generation_data()).generate(
      state,
      new Map_Generation_Options()
      {
        Algorithm = Map_Generation_Algorithm.Experimental_Hybrid,
        Hybrid_Initial_Halo = 0,
        Hybrid_Max_Halo = 1,
        Seed = 5
      });

    Assert.IsFalse(result.Hybrid_Improved);
    Assert.AreEqual(-1, result.Hybrid_Halo);
    Assert.AreEqual(4, result.Hybrid_Legacy_Unresolved_Tile_Count);
    Assert.IsGreaterThan(0, result.Hybrid_Attempt_Count);
    Assert.AreEqual(4, result.Unresolved_Tile_Count);
    Assert.IsGreaterThan(0, result.Components.Count);
  }

  [TestMethod]
  public void HybridRepairPreservesLockedZeroLegacySemantics()
  {
    Map_State baseline = new Map_State(
      new int[2, 1]
      {
        { 0 },
        { 1 }
      },
      filled_bool_array(2, 1, true),
      new bool[2, 1]
      {
        { true },
        { false }
      },
      new int[2, 1]);
    Map_State legacy_state = clone_state(baseline);
    Map_State hybrid_state = clone_state(baseline);

    Map_Generation_Result legacy = new Map_Generation_Engine(create_uniform_data(1)).repair(
      legacy_state,
      new Map_Repair_Options()
      {
        Algorithm = Map_Generation_Algorithm.Legacy,
        Radius = 1,
        Seed = 6
      });
    Map_Generation_Result hybrid = new Map_Generation_Engine(create_uniform_data(1)).repair(
      hybrid_state,
      new Map_Repair_Options()
      {
        Algorithm = Map_Generation_Algorithm.Experimental_Hybrid,
        Radius = 1,
        Seed = 6
      });

    Assert.AreEqual(legacy.Unresolved_Tile_Count, hybrid.Hybrid_Legacy_Unresolved_Tile_Count);
    Assert.AreEqual(0, hybrid_state.Tiles[0, 0]);
    Assert.IsTrue(hybrid_state.Drawn[0, 0]);
    Assert.IsTrue(hybrid_state.Locked[0, 0]);
  }

  [TestMethod]
  public void HybridRejectsInvalidHaloRange()
  {
    Assert.Throws<ArgumentOutOfRangeException>(() =>
      new Map_Generation_Engine(create_uniform_data(1)).generate(
        blank_state(1, 1),
        new Map_Generation_Options()
        {
          Algorithm = Map_Generation_Algorithm.Experimental_Hybrid,
          Hybrid_Initial_Halo = 2,
          Hybrid_Max_Halo = 1
        }));
  }

  [TestMethod]
  public void ExperimentalWithSameSeedIsDeterministic()
  {
    Tileset_Generation_Data data = create_connected_data(1, 2);
    Map_State first = blank_state(6, 5);
    Map_State second = blank_state(6, 5);
    Map_Generation_Options options = new Map_Generation_Options()
    {
      Algorithm = Map_Generation_Algorithm.Experimental_Constraint,
      Depth = 2,
      Seed = 42
    };

    new Map_Generation_Engine(data).generate(first, options);
    new Map_Generation_Engine(data).generate(second, options);

    CollectionAssert.AreEqual(first.Tiles.Cast<int>().ToArray(), second.Tiles.Cast<int>().ToArray());
    CollectionAssert.AreEqual(first.Drawn.Cast<bool>().ToArray(), second.Drawn.Cast<bool>().ToArray());
  }

  [TestMethod]
  public void ExperimentalRepairFillsHole()
  {
    Map_State state = new Map_State(
      new int[3, 1]
      {
        { 1 },
        { 0 },
        { 1 }
      },
      new bool[3, 1]
      {
        { true },
        { false },
        { true }
      },
      new bool[3, 1],
      new int[3, 1]);

    Map_Generation_Result result = new Map_Generation_Engine(create_uniform_data(1)).repair(
      state,
      new Map_Repair_Options()
      {
        Algorithm = Map_Generation_Algorithm.Experimental_Constraint,
        Radius = 1,
        Depth = 1,
        Seed = 18
      });

    Assert.AreEqual(0, result.Unresolved_Tile_Count);
    assert_all_tiles(state, 1);
  }

  [TestMethod]
  public void ExperimentalRepairPreservesDrawnTileZero()
  {
    Map_State state = new Map_State(
      new int[3, 1]
      {
        { 0 },
        { 0 },
        { 0 }
      },
      filled_bool_array(3, 1, true),
      new bool[3, 1],
      new int[3, 1]);

    Map_Generation_Result result = new Map_Generation_Engine(create_uniform_data(0)).repair(
      state,
      new Map_Repair_Options()
      {
        Algorithm = Map_Generation_Algorithm.Experimental_Constraint,
        Radius = 1,
        Seed = 20
      });

    Assert.AreEqual(0, result.Unresolved_Tile_Count);
    assert_all_tiles(state, 0);
  }

  [TestMethod]
  public void ExperimentalRepairUsesLockedOpenZeroAsHoleOrigin()
  {
    Map_State state = new Map_State(
      new int[2, 1]
      {
        { 0 },
        { 1 }
      },
      new bool[2, 1]
      {
        { false },
        { true }
      },
      new bool[2, 1]
      {
        { true },
        { false }
      },
      new int[2, 1]);

    Map_Generation_Result result = new Map_Generation_Engine(create_uniform_data(1)).repair(
      state,
      new Map_Repair_Options()
      {
        Algorithm = Map_Generation_Algorithm.Experimental_Constraint,
        Radius = 1,
        Seed = 23
      });

    Assert.AreEqual(0, result.Unresolved_Tile_Count);
    Assert.AreEqual(0, state.Tiles[0, 0]);
    Assert.IsTrue(state.Drawn[0, 0]);
    Assert.IsTrue(state.Locked[0, 0]);
    Assert.AreEqual(1, state.Tiles[1, 0]);
    Assert.IsTrue(state.Drawn[1, 0]);
  }

  [TestMethod]
  public void ExperimentalCancellationDoesNotMutateState()
  {
    using CancellationTokenSource cancellation = new CancellationTokenSource();
    cancellation.Cancel();
    Map_State state = blank_state(3, 3);
    int[] expected_tiles = state.Tiles.Cast<int>().ToArray();
    bool[] expected_drawn = state.Drawn.Cast<bool>().ToArray();

    Assert.Throws<OperationCanceledException>(() =>
      new Map_Generation_Engine(create_uniform_data(1)).generate(
        state,
        new Map_Generation_Options()
        {
          Algorithm = Map_Generation_Algorithm.Experimental_Constraint,
          Seed = 19
        },
        cancellation.Token));

    CollectionAssert.AreEqual(expected_tiles, state.Tiles.Cast<int>().ToArray());
    CollectionAssert.AreEqual(expected_drawn, state.Drawn.Cast<bool>().ToArray());
  }

  [TestMethod]
  public void GenerateHonorsCancellation()
  {
    using CancellationTokenSource cancellation = new CancellationTokenSource();
    cancellation.Cancel();
    Map_State state = blank_state(3, 3);
    int[] expected_tiles = state.Tiles.Cast<int>().ToArray();
    bool[] expected_drawn = state.Drawn.Cast<bool>().ToArray();

    Assert.Throws<OperationCanceledException>(() =>
      new Map_Generation_Engine(create_uniform_data(1)).generate(
        state,
        new Map_Generation_Options()
        {
          Seed = 11
        },
        cancellation.Token));

    CollectionAssert.AreEqual(expected_tiles, state.Tiles.Cast<int>().ToArray());
    CollectionAssert.AreEqual(expected_drawn, state.Drawn.Cast<bool>().ToArray());
  }

  [TestMethod]
  public void RepairHonorsCancellationWithoutMutatingState()
  {
    using CancellationTokenSource cancellation = new CancellationTokenSource();
    cancellation.Cancel();
    Map_State state = new Map_State(
      new int[3, 1]
      {
        { 1 },
        { 0 },
        { 1 }
      },
      filled_bool_array(3, 1, true),
      new bool[3, 1],
      new int[3, 1]);
    int[] expected_tiles = state.Tiles.Cast<int>().ToArray();
    bool[] expected_drawn = state.Drawn.Cast<bool>().ToArray();

    Assert.Throws<OperationCanceledException>(() =>
      new Map_Generation_Engine(create_uniform_data(1)).repair(
        state,
        new Map_Repair_Options()
        {
          Radius = 1,
          Depth = 1,
          Seed = 14
        },
        cancellation.Token));

    CollectionAssert.AreEqual(expected_tiles, state.Tiles.Cast<int>().ToArray());
    CollectionAssert.AreEqual(expected_drawn, state.Drawn.Cast<bool>().ToArray());
  }

  private static Map_State blank_state(int width, int height)
  {
    return new Map_State(
      new int[width, height],
      new bool[width, height],
      new bool[width, height],
      new int[width, height]);
  }

  private static Map_State clone_state(Map_State state)
  {
    return new Map_State(
      (int[,]) state.Tiles.Clone(),
      (bool[,]) state.Drawn.Clone(),
      (bool[,]) state.Locked.Clone(),
      (int[,]) state.Terrain.Clone());
  }

  private static Tileset_Generation_Data create_uniform_data(short tile)
  {
    Tileset_Generation_Data data = create_generation_data();
    Tile_Data tile_data = new Tile_Data();
    foreach (byte direction in new byte[] { 2, 4, 6, 8 })
      tile_data.Valid_Tile_Priority[direction][tile] = 1;
    data.generation_data[tile] = tile_data;
    return data;
  }

  private static Tileset_Generation_Data create_alias_data()
  {
    Tile_Matching_Data matching = new Tile_Matching_Data(new HashSet<Tile_Directions>()
    {
      Tile_Directions.Center
    });
    matching.add(Tile_Directions.Center, new List<short>() { 1, 2, 3 });
    matching.refresh_identical(4);
    Tileset_Generation_Data data = new Tileset_Generation_Data(4, matching);
    Tile_Data tile_data = new Tile_Data();
    foreach (byte direction in new byte[] { 2, 4, 6, 8 })
      tile_data.Valid_Tile_Priority[direction][1] = 1;
    data.generation_data[1] = tile_data;
    return data;
  }

  private static Map_State depth_fixture_state()
  {
    Map_State state = blank_state(4, 1);
    state.Tiles[0, 0] = 1;
    state.Drawn[0, 0] = true;
    state.Locked[0, 0] = true;
    return state;
  }

  private static Map_State isolated_checkerboard_state(int width, int height)
  {
    Map_State state = blank_state(width, height);
    for (int y = 0; y < height; ++y)
    {
      for (int x = 0; x < width; ++x)
      {
        if ((x + y) % 2 == 1)
        {
          state.Tiles[x, y] = 1;
          state.Drawn[x, y] = true;
          state.Locked[x, y] = true;
        }
      }
    }
    return state;
  }

  private static Tileset_Generation_Data create_depth_fixture_data()
  {
    Tileset_Generation_Data data = create_generation_data();
    data.generation_data[1] = create_tile_data((6, 2, 100), (6, 3, 1));
    data.generation_data[2] = create_tile_data((4, 1, 1), (6, 4, 1));
    data.generation_data[3] = create_tile_data((4, 1, 1), (6, 5, 1));
    data.generation_data[4] = create_tile_data((4, 2, 1));
    data.generation_data[5] = create_tile_data((4, 3, 1), (6, 6, 1));
    data.generation_data[6] = create_tile_data((4, 5, 1));
    return data;
  }

  private static Tileset_Generation_Data create_parity_cycle_data(bool include_tail)
  {
    Tileset_Generation_Data data = create_generation_data();
    int tile_count = include_tail ? 10 : 8;
    for (int tile = 1; tile <= tile_count; ++tile)
      data.generation_data[tile] = new Tile_Data();

    add_edge(data, 1, 6, 3);
    add_edge(data, 2, 6, 4);
    add_edge(data, 3, 2, 5);
    add_edge(data, 4, 2, 6);
    add_edge(data, 7, 6, 5);
    add_edge(data, 8, 6, 6);
    add_edge(data, 1, 2, 8);
    add_edge(data, 2, 2, 7);
    if (include_tail)
    {
      add_edge(data, 3, 6, 9);
      add_edge(data, 4, 6, 10);
    }
    return data;
  }

  private static void add_edge(Tileset_Generation_Data data, short source, byte direction, short target)
  {
    data.generation_data[source].Valid_Tile_Priority[direction][target] = 1;
    data.generation_data[target].Valid_Tile_Priority[(byte) (10 - direction)][source] = 1;
  }

  private static Data_Tileset create_parity_cycle_metadata(bool include_tail)
  {
    List<int> tags = new List<int>() { 0, 1, 1, 2, 2, 3, 3, 4, 4 };
    if (include_tail)
      tags.AddRange(new int[] { 5, 5 });
    return new Data_Tileset() { Terrain_Tags = tags };
  }

  private static Map_State create_parity_cycle_state(bool include_tail)
  {
    int width = include_tail ? 3 : 2;
    Map_State state = blank_state(width, 2);
    state.Terrain[0, 0] = 1;
    state.Terrain[1, 0] = 2;
    state.Terrain[1, 1] = 3;
    state.Terrain[0, 1] = 4;
    if (include_tail)
    {
      state.Terrain[2, 0] = 5;
      state.Tiles[2, 1] = 99;
      state.Drawn[2, 1] = true;
      state.Locked[2, 1] = true;
    }
    return state;
  }

  private static Tile_Data create_tile_data(params (byte Direction, short Tile, short Weight)[] edges)
  {
    Tile_Data data = new Tile_Data();
    foreach ((byte direction, short tile, short weight) in edges)
      data.Valid_Tile_Priority[direction][tile] = weight;
    return data;
  }

  private static Tileset_Generation_Data create_connected_data(params short[] tiles)
  {
    Tileset_Generation_Data data = create_generation_data();
    foreach (short tile in tiles)
    {
      Tile_Data tile_data = new Tile_Data();
      foreach (byte direction in new byte[] { 2, 4, 6, 8 })
      {
        foreach (short neighbor in tiles)
          tile_data.Valid_Tile_Priority[direction][neighbor] = 1;
      }
      data.generation_data[tile] = tile_data;
    }
    return data;
  }

  private static Tileset_Generation_Data create_generation_data()
  {
    return new Tileset_Generation_Data(
      0,
      new Tile_Matching_Data(new HashSet<Tile_Directions>()));
  }

  private static bool[,] filled_bool_array(int width, int height, bool value)
  {
    bool[,] result = new bool[width, height];
    for (int y = 0; y < height; ++y)
    {
      for (int x = 0; x < width; ++x)
        result[x, y] = value;
    }
    return result;
  }

  private static int[] tiles_row_major(Map_State state)
  {
    List<int> result = new List<int>();
    for (int y = 0; y < state.Height; ++y)
    {
      for (int x = 0; x < state.Width; ++x)
        result.Add(state.Tiles[x, y]);
    }
    return result.ToArray();
  }

  private static bool[] drawn_row_major(Map_State state)
  {
    List<bool> result = new List<bool>();
    for (int y = 0; y < state.Height; ++y)
    {
      for (int x = 0; x < state.Width; ++x)
        result.Add(state.Drawn[x, y]);
    }
    return result.ToArray();
  }

  private static (
    Map_State State,
    Map_Generation_Result Result) run_branch_arc_fixture(
      bool? enable_branch_arc_consistency,
      int search_node_limit,
      int seed)
  {
    Map_State state = ExperimentalBranchArcConsistencyTestFixture.create_state();
    Map_Generation_Options options = enable_branch_arc_consistency.HasValue
      ? new Map_Generation_Options()
      {
        Algorithm = Map_Generation_Algorithm.Experimental_Constraint,
        Experimental_Search_Node_Limit = search_node_limit,
        Experimental_Restart_Count = 2,
        Experimental_Nogood_Limit = 64,
        Experimental_Enable_Branch_Arc_Consistency =
          enable_branch_arc_consistency.Value,
        Seed = seed
      }
      : new Map_Generation_Options()
      {
        Algorithm = Map_Generation_Algorithm.Experimental_Constraint,
        Experimental_Search_Node_Limit = search_node_limit,
        Experimental_Restart_Count = 2,
        Experimental_Nogood_Limit = 64,
        Seed = seed
      };
    Map_Generation_Result result = new Map_Generation_Engine(
      ExperimentalBranchArcConsistencyTestFixture.create_data(),
      ExperimentalBranchArcConsistencyTestFixture.create_metadata()).generate(
        state,
        options);
    return (state, result);
  }

  private static void assert_branch_arc_fixture_adjacency(
    Tileset_Generation_Data data,
    Map_State state)
  {
    assert_edge(data, (short) state.Tiles[0, 0], 2, (short) state.Tiles[0, 1]);
    assert_edge(data, (short) state.Tiles[0, 1], 6, (short) state.Tiles[1, 1]);
    assert_edge(data, (short) state.Tiles[1, 1], 6, (short) state.Tiles[2, 1]);
    assert_edge(data, (short) state.Tiles[1, 1], 2, (short) state.Tiles[1, 2]);
    assert_edge(data, (short) state.Tiles[2, 1], 2, (short) state.Tiles[2, 2]);
    assert_edge(data, (short) state.Tiles[1, 2], 6, (short) state.Tiles[2, 2]);
  }

  private static void assert_edge(
    Tileset_Generation_Data data,
    short source,
    byte direction,
    short target)
  {
    Assert.IsTrue(data.generation_data[source].Valid_Tile_Priority[direction].ContainsKey(target));
    Assert.IsTrue(data.generation_data[target].Valid_Tile_Priority[(byte) (10 - direction)].ContainsKey(source));
  }

  private static void assert_horizontal_adjacency(Tileset_Generation_Data data, Map_State state)
  {
    for (int x = 0; x + 1 < state.Width; ++x)
    {
      short left = (short) state.Tiles[x, 0];
      short right = (short) state.Tiles[x + 1, 0];
      Assert.IsTrue(data.generation_data[left].Valid_Tile_Priority[(byte) 6].ContainsKey(right));
      Assert.IsTrue(data.generation_data[right].Valid_Tile_Priority[(byte) 4].ContainsKey(left));
    }
  }

  private static void assert_all_tiles(Map_State state, int expected)
  {
    for (int y = 0; y < state.Height; ++y)
    {
      for (int x = 0; x < state.Width; ++x)
      {
        Assert.IsTrue(state.Drawn[x, y], $"Cell ({x},{y}) was not drawn.");
        Assert.AreEqual(expected, state.Tiles[x, y], $"Unexpected tile at ({x},{y}).");
      }
    }
  }

  private sealed class Recording_Progress : IProgress<int>
  {
    public List<int> Values { get; } = new List<int>();

    public void Report(int value)
    {
      this.Values.Add(value);
    }
  }
}
