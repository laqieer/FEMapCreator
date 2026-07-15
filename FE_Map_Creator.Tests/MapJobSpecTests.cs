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
      Assert.IsTrue(actual.Drawn[0][0]);
      Assert.IsFalse(actual.Drawn[0][1]);
      Assert.IsTrue(actual.Locked[0][1]);
      Assert.AreEqual(-16, actual.Terrain[1][0]);
      Assert.AreEqual(2, actual.Terrain[1][1]);
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
