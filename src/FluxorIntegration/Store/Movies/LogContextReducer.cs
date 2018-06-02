using Blazor.Fluxor;
using FluxorIntegration.Models;

namespace FluxorIntegration.Store.Movies
{
    public class LogContextReducer : IReducer<MovieState, LogContextAction>
    {
        private Context Context { get; set; }

        public LogContextReducer(Context context)
        {
            Context = context;
        }
        public MovieState Reduce(MovieState state, LogContextAction action)
        {
            Context.LogToConsole();
            return state;
        }
    }
}
