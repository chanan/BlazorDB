using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Blazor.Fluxor;

namespace FluxorIntegration.Store.Movies
{
    public class DeleteMovieAction : IAction
    {
        public int MovieId { get; private set; }

        public DeleteMovieAction(int movieId)
        {
            MovieId = movieId;
        }
    }
}
