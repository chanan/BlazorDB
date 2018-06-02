using Blazor.Fluxor;

namespace FluxorIntegration.Store.Reviews
{
    public class AddReviewAction : IAction
    {
        public int MovieId { get; private set; }
        public int Stars { get; private set; }
        public string Comment { get; private set; }

        public AddReviewAction(int movieId, int stars, string comment)
        {
            MovieId = movieId;
            Stars = stars;
            Comment = comment;
        }
    }
}
