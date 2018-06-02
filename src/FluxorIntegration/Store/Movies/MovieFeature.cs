using System.Collections.Generic;
using Blazor.Fluxor;
using FluxorIntegration.Models;

namespace FluxorIntegration.Store.Movies
{
    public class MovieFeature : Feature<MovieState>
    {
        public override string GetName() => "Movies";

        protected override MovieState GetInitialState()
        {
            return new MovieState(new List<Movie>());
        }
    }
}
