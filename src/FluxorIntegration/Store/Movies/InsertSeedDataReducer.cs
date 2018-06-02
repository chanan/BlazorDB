using System.Collections.Generic;
using System.Linq;
using Blazor.Fluxor;
using FluxorIntegration.Models;

namespace FluxorIntegration.Store.Movies
{
    public class InsertSeedDataReducer : IReducer<MovieState, InsertSeedMoviesAction>
    {
        private Context Context { get; set; }

        public InsertSeedDataReducer(Context context)
        {
            Context = context;
        }
        public MovieState Reduce(MovieState state, InsertSeedMoviesAction action)
        {
            var query = from m in Context.Movies
                where m.Name == "Solo: A Star Wars Story"
                select m;
            if (!query.Any())
            {
                var movie = new Movie
                {
                    Name = "Solo: A Star Wars Story",
                    Reviews = new List<Review>
                    {
                        new Review {Stars = 4, Comment = "Pretty good!"},
                        new Review {Stars = 3, Comment = "Not as good as Rogue One"}
                    }
                };
                Context.Movies.Add(movie);
            }

            query = from m in Context.Movies
                where m.Name == "Isle of Dog"
                select m;
            if (!query.Any())
            {
                var movie = new Movie
                {
                    Name = "Isle of Dog",
                    Reviews = new List<Review>
                    {
                        new Review {Stars = 5, Comment = "Awesome stop motion movie!"}
                    }
                };
                Context.Movies.Add(movie);
            }

            Context.SaveChanges();

            return new MovieState(Context.Movies);
        }
    }
}
