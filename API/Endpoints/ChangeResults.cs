using BusinessEsbjerg.Holiday.Service;
using Microsoft.AspNetCore.Http.HttpResults;

namespace BusinessEsbjerg.Holiday.Api.Endpoints;

public static class ChangeResults
{
    // 204 when it worked, 404 when something doesn't exist, 400 with the reason when a rule says no
    public static Results<NoContent, NotFound, ProblemHttpResult> ToHttp(ChangeResult result)
    {
        if (result.NotFound) return TypedResults.NotFound();
        if (result.Error is not null)
            return TypedResults.Problem(
                statusCode: StatusCodes.Status400BadRequest, title: "Not allowed", detail: result.Error);
        return TypedResults.NoContent();
    }
}
