namespace RentalApp.Models.Api;

/// <summary>POST /reviews request body. Rating 1–5; comment max 500 chars.</summary>
public record CreateReviewRequest(int RentalId, int Rating, string? Comment);

/// <summary>
/// Review row returned by GET /items/{id}/reviews and embedded in
/// GET /items/{id} and GET /users/{id}/profile.
/// </summary>
public class ReviewDto
{
    public int      Id           { get; set; }
    public int?     RentalId     { get; set; }
    public int?     ItemId       { get; set; }
    public string?  ItemTitle    { get; set; }
    public int      ReviewerId   { get; set; }
    public string   ReviewerName { get; set; } = string.Empty;
    public int      Rating       { get; set; }
    public string?  Comment      { get; set; }
    public DateTime CreatedAt    { get; set; }
}

/// <summary>Wrapper for the GET /items/{id}/reviews response.</summary>
public class ReviewListResponse
{
    public List<ReviewDto> Reviews        { get; set; } = new();
    public double          AverageRating  { get; set; }
    public int             TotalReviews   { get; set; }
    public int             Page           { get; set; }
    public int             PageSize       { get; set; }
    public int             TotalPages     { get; set; }
}
