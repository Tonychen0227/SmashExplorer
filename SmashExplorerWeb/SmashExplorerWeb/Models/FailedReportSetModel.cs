using System.Collections.Generic;

public static class FailedReportSetErrorMessages
{
    public static string InternalServerError = "INTERNAL_SERVER_ERROR";
    public static string SetAlreadyCompleted = "SET_COMPLETED";
    public static string Unauthorized = "UNAUTHORIZED";
    public static string SetNotFound = "NOT_FOUND";
}

public class FailedReportSetModel
{
    public FailedReportSetModel(string errorMessage, CompletedSetInformation completedSet = null)
    {
        ErrorMessage = errorMessage;
        CompletedSet = completedSet;
    }

    public string ErrorMessage { get; set; }

    public CompletedSetInformation CompletedSet { get; set; }
}

public class CompletedSetInformation
{
    public string Id { get; set; }
    public Dictionary<string, string> DetailedScore { get; set; }
    public string WinnerId { get; set; }
}