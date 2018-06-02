using System.Collections.Generic;
using Blazor.Fluxor;
using FluxorIntegration.Models;

namespace FluxorIntegration.Store.Reviews
{
    public class ReviewsFeature : Feature<ReviewsState>
    {
        public override string GetName() => "Reviews";

        protected override ReviewsState GetInitialState()
        {
            return new ReviewsState(0, new List<Review>());
        }
    }
}
