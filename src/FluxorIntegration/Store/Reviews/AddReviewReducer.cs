using System;
using System.Collections.Generic;
using System.Linq;
using Blazor.Fluxor;
using FluxorIntegration.Models;

namespace FluxorIntegration.Store.Reviews
{
    public class AddReviewReducer : IReducer<ReviewsState, AddReviewAction>
    {
        private Context Context { get; set; }

        public AddReviewReducer(Context context)
        {
            Context = context;
        }
        public ReviewsState Reduce(ReviewsState state, AddReviewAction action)
        {
            var query = from m in Context.Movies
                where m.Id == action.MovieId
                select m;
            var movie = query.Single();
            if (movie.Reviews == null) movie.Reviews = new List<Review>();
            movie.Reviews.Add(new Review { Stars = action.Stars, Comment = action.Comment });
            Context.SaveChanges();
            return new ReviewsState(action.MovieId, movie.Reviews);
        }
    }
}
