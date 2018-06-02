using System;
using System.Collections.Generic;
using FluxorIntegration.Models;

namespace FluxorIntegration.Store.Reviews
{
    public class ReviewsState
    {
        public int MovieId { get; private set; }
        public IList<Review> Reviews { get; private set; }


        [Obsolete("For deserialization purposes only. Use the constructor with parameters")]
        public ReviewsState() { }
        public ReviewsState(int movieId, IList<Review> reviews)
        {
            MovieId = movieId;
            Reviews = reviews;
        }
    }
}
