using Blazor.Fluxor;

namespace FluxorIntegration.Store.Reviews
{
    public class LoadReviewsAction : IAction
    {
        public int MovieId { get; private set; }
        public LoadReviewsAction(int movieId)
        {
            MovieId = movieId;
        }
    }
}
