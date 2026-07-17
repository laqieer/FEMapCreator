namespace FE_Map_Creator.Tests;

[TestClass]
public sealed class MapJobSpecTests
{
  [TestMethod]
  public void ConvertsJsonRowsToXYArrays()
  {
    Map_Job_Spec spec = new Map_Job_Spec()
    {
      Drawn = new bool[][]
      {
        new bool[] { true, false },
        new bool[] { false, true }
      },
      Locked = new bool[][]
      {
        new bool[] { false, true },
        new bool[] { false, false }
      },
      Terrain = new int[][]
      {
        new int[] { 1, 0 },
        new int[] { -16, 2 }
      }
    };

    bool[,] drawn = spec.drawn_array(2, 2, false);
    bool[,] locked = spec.locked_array(2, 2);
    int[,] terrain = spec.terrain_array(2, 2);

    Assert.IsTrue(drawn[0, 0]);
    Assert.IsFalse(drawn[1, 0]);
    Assert.IsTrue(locked[1, 0]);
    Assert.AreEqual(-16, terrain[0, 1]);
    Assert.AreEqual(2, terrain[1, 1]);
  }

  [TestMethod]
  public void MissingDrawnArrayDefaultsToRequestedValue()
  {
    Map_Job_Spec spec = new Map_Job_Spec();

    bool[,] drawn = spec.drawn_array(2, 2, true);

    Assert.IsTrue(drawn[0, 0]);
    Assert.IsTrue(drawn[1, 1]);
  }

  [TestMethod]
  public void RejectsLockAndTerrainConstraintOnSameCell()
  {
    Map_Job_Spec spec = new Map_Job_Spec()
    {
      Locked = new bool[][] { new bool[] { true } },
      Terrain = new int[][] { new int[] { 1 } }
    };

    Assert.Throws<InvalidOperationException>(() => spec.validate_constraints(1, 1));
  }

  [TestMethod]
  public void RejectsNonRectangularConstraintRows()
  {
    Map_Job_Spec spec = new Map_Job_Spec()
    {
      Drawn = new bool[][]
      {
        new bool[] { true, false },
        new bool[] { true }
      }
    };

    Assert.Throws<InvalidOperationException>(() => spec.drawn_array(2, 2, false));
  }

  [TestMethod]
  public void ReaderRejectsUnsupportedVersion()
  {
    string directory = create_temp_directory();
    try
    {
      string filename = Path.Combine(directory, "job.json");
      File.WriteAllText(filename,
        """
        {
          "version": 2,
          "width": 1,
          "height": 1
        }
        """);

      Assert.Throws<NotSupportedException>(() => new Map_Job_Spec_Reader().read_job(filename));
    }
    finally
    {
      Directory.Delete(directory, true);
    }
  }

  [TestMethod]
  public void ReaderRejectsConflictingConstraintMatrices()
  {
    string directory = create_temp_directory();
    try
    {
      string filename = Path.Combine(directory, "job.json");
      File.WriteAllText(filename,
        """
        {
          "version": 1,
          "drawn": [[true, false]],
          "locked": [[false]],
          "terrain": [[0, 0]]
        }
        """);

      InvalidOperationException ex =
        Assert.Throws<InvalidOperationException>(() => new Map_Job_Spec_Reader().read_job(filename));
      StringAssert.Contains(ex.Message, "do not match");
    }
    finally
    {
      Directory.Delete(directory, true);
    }
  }

  [TestMethod]
  public void ReaderRejectsLockAndTerrainConflict()
  {
    string directory = create_temp_directory();
    try
    {
      string filename = Path.Combine(directory, "job.json");
      File.WriteAllText(filename,
        """
        {
          "version": 1,
          "locked": [[true]],
          "terrain": [[1]]
        }
        """);

      InvalidOperationException ex =
        Assert.Throws<InvalidOperationException>(() => new Map_Job_Spec_Reader().read_job(filename));
      StringAssert.Contains(ex.Message, "locked and terrain-constrained");
    }
    finally
    {
      Directory.Delete(directory, true);
    }
  }

  [TestMethod]
  public void ReaderRejectsWidthWithoutHeight()
  {
    string directory = create_temp_directory();
    try
    {
      string filename = Path.Combine(directory, "job.json");
      File.WriteAllText(filename,
        """
        {
          "version": 1,
          "width": 2
        }
        """);

      Assert.Throws<InvalidOperationException>(() => new Map_Job_Spec_Reader().read_job(filename));
    }
    finally
    {
      Directory.Delete(directory, true);
    }
  }

  [TestMethod]
  public void ReaderRejectsUnknownAlgorithm()
  {
    string directory = create_temp_directory();
    try
    {
      string filename = Path.Combine(directory, "job.json");
      File.WriteAllText(filename,
        """
        {
          "version": 1,
          "algorithm": "future"
        }
        """);

      InvalidOperationException ex =
        Assert.Throws<InvalidOperationException>(() => new Map_Job_Spec_Reader().read_job(filename));
      StringAssert.Contains(ex.Message, "legacy");
      StringAssert.Contains(ex.Message, "experimental");
      StringAssert.Contains(ex.Message, "hybrid");
    }
    finally
    {
      Directory.Delete(directory, true);
    }
  }

  [TestMethod]
  public void ReaderRejectsNonPositiveExperimentalSearchNodeLimit()
  {
    string directory = create_temp_directory();
    try
    {
      string filename = Path.Combine(directory, "job.json");
      File.WriteAllText(filename,
        """
        {
          "version": 1,
          "experimentalSearchNodeLimit": 0
        }
        """);

      InvalidOperationException ex =
        Assert.Throws<InvalidOperationException>(() => new Map_Job_Spec_Reader().read_job(filename));
      StringAssert.Contains(ex.Message, "ExperimentalSearchNodeLimit");
    }
    finally
    {
      Directory.Delete(directory, true);
    }
  }

  [TestMethod]
  public void ReaderRejectsInvalidExperimentalSearchSettings()
  {
    string directory = create_temp_directory();
    try
    {
      string filename = Path.Combine(directory, "job.json");
      File.WriteAllText(filename,
        """
        {
          "version": 1,
          "experimentalRestartCount": 0,
          "experimentalNogoodLimit": -1
        }
        """);

      InvalidOperationException ex =
        Assert.Throws<InvalidOperationException>(() => new Map_Job_Spec_Reader().read_job(filename));
      StringAssert.Contains(ex.Message, "ExperimentalRestartCount");
      InvalidOperationException nogood_ex = Assert.Throws<InvalidOperationException>(() =>
        new Map_Job_Spec() { ExperimentalNogoodLimit = -1 }.validate());
      StringAssert.Contains(nogood_ex.Message, "ExperimentalNogoodLimit");
      InvalidOperationException halo_ex = Assert.Throws<InvalidOperationException>(() =>
        new Map_Job_Spec() { HybridInitialHalo = 2, HybridMaxHalo = 1 }.validate());
      StringAssert.Contains(halo_ex.Message, "HybridMaxHalo");
    }
    finally
    {
      Directory.Delete(directory, true);
    }
  }

  [TestMethod]
  public void ReaderLoadsBranchArcConsistencyAndDefaultsToNull()
  {
    string directory = create_temp_directory();
    try
    {
      string filename = Path.Combine(directory, "job.json");
      string omitted_filename = Path.Combine(directory, "omitted.json");
      File.WriteAllText(filename,
        """
        {
          "version": 1,
          "experimentalEnableBranchArcConsistency": true
        }
        """);
      File.WriteAllText(omitted_filename,
        """
        {
          "version": 1
        }
        """);

      Map_Job_Spec enabled = new Map_Job_Spec_Reader().read_job(filename);
      Map_Job_Spec omitted = new Map_Job_Spec_Reader().read_job(omitted_filename);

      Assert.IsTrue(enabled.ExperimentalEnableBranchArcConsistency);
      Assert.IsNull(omitted.ExperimentalEnableBranchArcConsistency);
    }
    finally
    {
      Directory.Delete(directory, true);
    }
  }

  [TestMethod]
  public void JobSpecRoundTripPreservesVersionAndOrientation()
  {
    string directory = create_temp_directory();
    try
    {
      string filename = Path.Combine(directory, "job.json");
      Map_Job_Spec_Reader reader = new Map_Job_Spec_Reader();
      Map_Job_Spec expected = new Map_Job_Spec()
      {
        Version = 1,
        Operation = "generate",
        Width = 2,
        Height = 2,
        Tileset = "01020304",
        Algorithm = "experimental",
        ExperimentalSearchNodeLimit = 1234,
        ExperimentalRestartCount = 5,
        ExperimentalNogoodLimit = 64,
        ExperimentalEnableConflictLearning = false,
        ExperimentalEnableBranchArcConsistency = true,
        HybridInitialHalo = 0,
        HybridMaxHalo = 2,
        Drawn = new bool[][]
        {
          new bool[] { true, false },
          new bool[] { false, true }
        },
        Locked = new bool[][]
        {
          new bool[] { false, true },
          new bool[] { false, false }
        },
        Terrain = new int[][]
        {
          new int[] { 1, 0 },
          new int[] { -16, 2 }
        },
        Edits = new FE_Map_Creator.Editing.Map_Edit_Operation[]
        {
          new FE_Map_Creator.Editing.Map_Edit_Operation
          {
            Action = "set-tile",
            Shape = "rectangle",
            X = 0,
            Y = 0,
            EndX = 1,
            EndY = 1,
            Tile = 7
          }
        },
      };

      reader.write_job(filename, expected);
      Map_Job_Spec actual = reader.read_job(filename);

      Assert.AreEqual(1, actual.Version);
      Assert.AreEqual("generate", actual.Operation);
      Assert.AreEqual("01020304", actual.Tileset);
      Assert.AreEqual("experimental", actual.Algorithm);
      Assert.AreEqual(1234, actual.ExperimentalSearchNodeLimit);
      Assert.AreEqual(5, actual.ExperimentalRestartCount);
      Assert.AreEqual(64, actual.ExperimentalNogoodLimit);
      Assert.IsFalse(actual.ExperimentalEnableConflictLearning);
      Assert.IsTrue(actual.ExperimentalEnableBranchArcConsistency);
      Assert.AreEqual(0, actual.HybridInitialHalo);
      Assert.AreEqual(2, actual.HybridMaxHalo);
      Assert.IsTrue(actual.Drawn[0][0]);
      Assert.IsFalse(actual.Drawn[0][1]);
      Assert.IsTrue(actual.Locked[0][1]);
      Assert.AreEqual(-16, actual.Terrain[1][0]);
      Assert.AreEqual(2, actual.Terrain[1][1]);
      Assert.AreEqual("set-tile", actual.Edits[0].Action);
      Assert.AreEqual("rectangle", actual.Edits[0].Shape);
      Assert.AreEqual(7, actual.Edits[0].Tile);
    }
    finally
    {
      Directory.Delete(directory, true);
    }
  }

  [TestMethod]
  public void ReaderRejectsInvalidEditOperation()
  {
    string directory = create_temp_directory();
    try
    {
      string filename = Path.Combine(directory, "job.json");
      File.WriteAllText(filename,
        """
        {
          "version": 1,
          "edits": [
            {
              "action": "set-tile",
              "x": 0,
              "y": 0,
              "tile": -1
            }
          ]
        }
        """);

      InvalidOperationException exception =
        Assert.Throws<InvalidOperationException>(() => new Map_Job_Spec_Reader().read_job(filename));

      StringAssert.Contains(exception.Message, "Edits[0]");
      StringAssert.Contains(exception.Message, "non-negative tile");
    }
    finally
    {
      Directory.Delete(directory, true);
    }
  }

  private static string create_temp_directory()
  {
    string directory = Path.Combine(Path.GetTempPath(), $"FEMapCreator-{Guid.NewGuid():N}");
    Directory.CreateDirectory(directory);
    return directory;
  }
}
