using System;
using System.Threading;
using System.Threading.Tasks;

#nullable disable
namespace FE_Map_Creator.Cli.Execution;

/// <summary>
/// Shared bookkeeping for the three batch orchestration modes (<c>generate --count</c>,
/// <c>repair --input-dir</c>, and <c>batch --manifest</c>): counts per-job outcomes and
/// derives the aggregate exit code/summary described in plan.md ("continue after job
/// failures by default ... return nonzero if any job failed or produced an incomplete
/// result").
/// </summary>
internal sealed class Batch_Progress
{
  internal int Total { get; }
  internal int Success { get; private set; }
  internal int Incomplete { get; private set; }
  internal int Failed { get; private set; }
  internal bool Cancelled { get; set; }

  internal Batch_Progress(int total)
  {
    this.Total = total;
  }

  internal int Attempted => this.Success + this.Incomplete + this.Failed;

  internal void record_success() => ++this.Success;
  internal void record_incomplete() => ++this.Incomplete;
  internal void record_failure() => ++this.Failed;

  internal void record(Cli_Execution_Result result)
  {
    if (result.Exit_Code == Cli_Exit_Codes.Success)
      this.record_success();
    else if (result.Exit_Code == Cli_Exit_Codes.Incomplete)
      this.record_incomplete();
    else
      this.record_failure();
  }

  /// <summary>Nonzero (batch-failed) whenever any job failed/was incomplete, or the run
  /// stopped early (fail-fast or cancellation) before every job was attempted.</summary>
  internal int exit_code()
  {
    bool all_attempted_and_clean = this.Attempted == this.Total && this.Failed == 0 && this.Incomplete == 0;
    return all_attempted_and_clean ? Cli_Exit_Codes.Success : Cli_Exit_Codes.Batch_Failed;
  }

  internal string summary(string noun)
  {
    int not_attempted = this.Total - this.Attempted;
    string not_attempted_part = not_attempted > 0 ? $", {not_attempted} not attempted" : "";
    string cancelled_part = this.Cancelled ? " (cancelled)" : "";
    return $"{noun}: {this.Success} succeeded, {this.Incomplete} incomplete, {this.Failed} failed{not_attempted_part} of {this.Total} job(s){cancelled_part}.";
  }
}

/// <summary>
/// Runs one batch job, printing a deterministic per-job line and folding the outcome
/// into <see cref="Batch_Progress"/>, translating cancellation/exceptions into the same
/// "continue past failures, stop on cancellation" contract for every batch mode.
/// </summary>
internal static class Batch_Job_Runner
{
  internal readonly struct Outcome
  {
    internal bool Cancelled { get; }
    internal bool Succeeded { get; }

    internal Outcome(bool cancelled, bool succeeded)
    {
      this.Cancelled = cancelled;
      this.Succeeded = succeeded;
    }
  }

  /// <summary>Runs one job, always returning whether it was cancelled and whether it
  /// succeeded (exit code 0), so callers that support --fail-fast can stop on either.</summary>
  internal static async Task<Outcome> run(
    Func<Task<Cli_Execution_Result>> job,
    string label,
    Cli_Output output,
    Batch_Progress progress,
    CancellationToken cancellation_token)
  {
    try
    {
      Cli_Execution_Result result = await job().ConfigureAwait(false);
      progress.record(result);
      var writer = result.Exit_Code == Cli_Exit_Codes.Success ? output.Out : output.Error;
      writer.WriteLine($"{label}: {result.Summary}");
      return new Outcome(cancelled: false, succeeded: result.Exit_Code == Cli_Exit_Codes.Success);
    }
    catch (OperationCanceledException) when (cancellation_token.IsCancellationRequested)
    {
      progress.Cancelled = true;
      output.Error.WriteLine($"{label}: Cancelled.");
      return new Outcome(cancelled: true, succeeded: false);
    }
    catch (Exception ex)
    {
      progress.record_failure();
      output.Error.WriteLine($"{label}: Failed: {ex.Message}");
      return new Outcome(cancelled: false, succeeded: false);
    }
  }
}
