using Blazor.Fluxor;

namespace FluxorIntegration.Store.Movies
{
    public class AddMovieAction : IAction
    {
        public string Name { get; private set; }

        public AddMovieAction(string name)
        {
            Name = name;
        }
    }
}
