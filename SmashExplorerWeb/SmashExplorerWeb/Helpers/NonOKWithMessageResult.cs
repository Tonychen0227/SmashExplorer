using System.Web.Mvc;

public class NonOKWithMessageResult : ActionResult
{
    private readonly string _message;
    private readonly int _statusCode;

    public NonOKWithMessageResult(string message, int statusCode)
    {
        _message = message;
        _statusCode = statusCode;
    }

    public override void ExecuteResult(ControllerContext context)
    {
        // you need to do this before setting the body content
        context.HttpContext.Response.StatusCode = _statusCode;

        context.HttpContext.Response.TrySkipIisCustomErrors = true;
        context.HttpContext.Response.Output.Write(_message);
        context.HttpContext.Response.ContentType = "application/json";
        context.HttpContext.Response.Output.Flush();
    }
}