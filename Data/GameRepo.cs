#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.ComponentModel;

namespace Ur.Data {
    public class GameRepo : INotifyPropertyChanged {
        readonly ConcurrentDictionary<string, Game> games = new();

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string? propertyName = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public Game NewGame(User user, int playerIndex) {
            var game = new Game();
            game.Players[playerIndex].UserId = user.Id;
            game.Players[playerIndex].InitialUserName = user.Name;
            game.Title = playerIndex == 0 ? $"{user.Name} vs ?" : $"? vs {user.Name}";
            games.TryAdd(game.Id, game);
            OnPropertyChanged(nameof(AllGames));
            return game;
        }

        public Game[] AllGames => games.Values.ToArray();
        public Game[] ActiveGames => games.Values.Where(x => !x.IsCompleted && x.IsActive).ToArray();
        public Game[] PendingGames => games.Values.Where(x => !x.IsCompleted && !x.IsActive).ToArray();
        public Game[] CompletedGames => games.Values.Where(x => x.IsCompleted).ToArray();

        public Game? FindGame(string id) {
            return games.TryGetValue(id, out var game) ? game : null;
        }
    }
}
